using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityProjectile;

namespace Fortress
{
    // ui-developer-codex: event-driven hotseat presentation; rules belong to MatchController.
    public sealed class MatchHud : MonoBehaviour
    {
        public MatchController match;
        public AiOpponent ai;
        public TMP_Text turnLabel, windLabel, aimLabel, stateLabel, resultLabel;
        public TMP_Text[] healthLabels;
        public Image[] healthFills;
        public RectTransform windArrow;
        public GameObject resultPanel;
        public Button restartButton;
        public HudStyleSO style;
        public Image powerFill;
        public TMP_Text powerLabel, elevationLabel, headingLabel, notificationLabel;
        public Image[] activeIndicators;
        public CanvasGroup notificationGroup;
        public TMP_Text movementLabel;
        int displayedTurn = -1, displayedSeed;
        float notificationRemaining;
        bool pendingImpact;

        void OnEnable()
        {
            if(ai!=null) ai.StateChanged+=Refresh;
            if (match != null)
            {
                match.StateChanged += Refresh;
                if (match.shots != null)
                {
                    match.shots.StateChanged += Refresh;
                    match.shots.ShotLaunched += OnShotLaunched;
                    match.shots.ShotImpacted += OnShotImpacted;
                    match.shots.ShotCancelled += ClearNotice;
                }
            }
            if (restartButton != null) restartButton.onClick.AddListener(Restart);
            Refresh();
        }
        void OnDisable()
        {
            if(ai!=null) ai.StateChanged-=Refresh;
            if (match != null)
            {
                match.StateChanged -= Refresh;
                if (match.shots != null)
                {
                    match.shots.StateChanged -= Refresh;
                    match.shots.ShotLaunched -= OnShotLaunched;
                    match.shots.ShotImpacted -= OnShotImpacted;
                    match.shots.ShotCancelled -= ClearNotice;
                }
            }
            if (restartButton != null) restartButton.onClick.RemoveListener(Restart);
            ClearNotice();
        }
        void Restart() { ClearNotice(); if (match != null) match.RestartMatch(); }
        void ClearNotice() { pendingImpact = false; notificationRemaining = 0; if (notificationGroup != null) notificationGroup.alpha = 0; }
        void OnShotLaunched(ProjectileRunner runner) { ClearNotice(); }
        void OnShotImpacted(ProjectileRunner runner) { pendingImpact = match != null && match.shots != null && match.shots.LastSolveHit; }
        void Update() { TickPresentation(Time.unscaledDeltaTime); }
        public void TickPresentation(float deltaTime)
        {
            if (notificationRemaining <= 0 || notificationGroup == null || style == null) return;
            notificationRemaining = Mathf.Max(0, notificationRemaining - Mathf.Max(0, deltaTime));
            notificationGroup.alpha = Mathf.Clamp01(notificationRemaining / Mathf.Max(.01f, style.notificationFadeSeconds));
        }
        public void Refresh()
        {
            if (match == null || match.shots == null || match.tuning == null) return;
            var shots = match.shots;
            if(movementLabel!=null) movementLabel.SetText("ARROW KEYS  MOVE  /  {0:0.0}m LEFT",match.MovementRemaining);
            var tanks = match.Tanks;
            if (tanks == null || tanks.Length != 2) return;
            for (int i = 0; i < 2; i++)
            {
                if(ai!=null && i==1) healthLabels[i].SetText("AI  /  {0:0} HP",tanks[i].Health);
                else healthLabels[i].SetText("PLAYER {0}  /  {1:0} HP", i + 1, tanks[i].Health);
                healthFills[i].fillAmount = tanks[i].Health / match.tuning.maxHealth;
                if (style != null) healthFills[i].color = healthFills[i].fillAmount <= style.dangerHealthFraction ? style.danger : style.accent;
                if (activeIndicators != null && i < activeIndicators.Length && activeIndicators[i] != null)
                    activeIndicators[i].enabled = match.State != MatchState.Ended && match.ActivePlayerIndex == i;
            }
            bool hasWinner = match.WinnerIndex >= 0 && match.WinnerIndex < tanks.Length;
            bool ended = match.State == MatchState.Ended && (match.IsDraw || hasWinner);
            bool aiTurn=ai!=null && match.ActivePlayerIndex==1;
            if(aiTurn) turnLabel.SetText("TURN {0:0}  /  AI",match.TurnNumber);
            else turnLabel.SetText("TURN {0:0}  /  PLAYER {1}", match.TurnNumber, match.ActivePlayerIndex + 1);
            var wind = shots.Wind;
            windLabel.SetText("WIND  {0:0.0} m/s²", wind.magnitude);
            windArrow.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(wind.z, wind.x) * Mathf.Rad2Deg);
            var tank = shots.ActiveTank;
            if (tank != null) aimLabel.SetText("YAW {0:0.0}°     ELEVATION {1:0.0}°     POWER {2:0.0}", tank.Yaw, tank.Pitch, shots.Power);
            if (tank != null && elevationLabel != null) elevationLabel.SetText("{0:0.0}°", tank.Pitch);
            if (tank != null && headingLabel != null) headingLabel.SetText("BEARING  {0:0.0}°", tank.Yaw);
            if (powerFill != null) powerFill.fillAmount = Mathf.InverseLerp(match.tuning.minPower, match.tuning.maxPower, shots.Power);
            if (powerLabel != null) powerLabel.SetText("{0:0.0}", shots.Power);
            stateLabel.text = ended ? "MATCH COMPLETE" : aiTurn && match.State==MatchState.Aim ? "AI AIMING...  /  PLEASE WAIT" : match.State == MatchState.Aim ? "READY  /  SPACE TO FIRE" : "SHOT IN PROGRESS  /  CONTROLS LOCKED";
            if (resultPanel.activeSelf != ended) resultPanel.SetActive(ended);
            if (ended) ClearNotice();
            if (!ended && match.State == MatchState.Aim && (displayedTurn != match.TurnNumber || displayedSeed != match.Seed))
            {
                if (displayedSeed != match.Seed || match.TurnNumber < displayedTurn) pendingImpact = false;
                displayedTurn = match.TurnNumber; displayedSeed = match.Seed;
                if (notificationLabel != null)
                {
                    if(aiTurn) notificationLabel.text="AI TURN  /  AIMING";
                    else notificationLabel.SetText(pendingImpact ? "IMPACT CONFIRMED  /  PLAYER {0} TURN" : "PLAYER {0}  /  YOUR TURN", match.ActivePlayerIndex + 1);
                }
                pendingImpact = false;
                if (notificationGroup != null && style != null) { notificationRemaining = style.notificationSeconds; notificationGroup.alpha = 1; }
            }
            if (ended)
            {
                if (match.IsDraw) resultLabel.SetText("DRAW\n{0:0} TURNS", match.TurnNumber);
                else if(ai!=null)
                {
                    bool drowned=tanks[1-match.WinnerIndex].Drowned;
                    resultLabel.SetText(match.WinnerIndex==0
                        ? (drowned ? "VICTORY\nOPPONENT SUBMERGED\n{0:0} TURNS" : "VICTORY\n{0:0} TURNS")
                        : (drowned ? "DEFEAT\nYOUR TANK SUBMERGED\n{0:0} TURNS" : "DEFEAT\n{0:0} TURNS"),match.TurnNumber);
                }
                else if(tanks[1-match.WinnerIndex].Drowned)
                    resultLabel.SetText("PLAYER {0} WINS\nOPPONENT SUBMERGED\n{1:0} TURNS",match.WinnerIndex+1,match.TurnNumber);
                else resultLabel.SetText("PLAYER {0} WINS\n{1:0} TURNS", match.WinnerIndex + 1, match.TurnNumber);
            }
        }
    }
}
