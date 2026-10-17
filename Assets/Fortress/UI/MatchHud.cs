using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Fortress
{
    // ui-developer-codex: event-driven hotseat presentation; rules belong to MatchController.
    public sealed class MatchHud : MonoBehaviour
    {
        public MatchController match;
        public TMP_Text turnLabel, windLabel, aimLabel, stateLabel, resultLabel;
        public TMP_Text[] healthLabels;
        public Image[] healthFills;
        public RectTransform windArrow;
        public GameObject resultPanel;
        public Button restartButton;

        void OnEnable()
        {
            if (match != null)
            {
                match.StateChanged += Refresh;
                if (match.shots != null) match.shots.StateChanged += Refresh;
            }
            if (restartButton != null) restartButton.onClick.AddListener(Restart);
            Refresh();
        }
        void OnDisable()
        {
            if (match != null)
            {
                match.StateChanged -= Refresh;
                if (match.shots != null) match.shots.StateChanged -= Refresh;
            }
            if (restartButton != null) restartButton.onClick.RemoveListener(Restart);
        }
        void Restart() { if (match != null) match.RestartMatch(); }
        public void Refresh()
        {
            if (match == null || match.shots == null || match.tuning == null) return;
            var shots = match.shots;
            var tanks = match.Tanks;
            if (tanks == null || tanks.Length != 2) return;
            for (int i = 0; i < 2; i++)
            {
                healthLabels[i].SetText("PLAYER {0}  /  {1:0} HP", i + 1, tanks[i].Health);
                healthFills[i].fillAmount = tanks[i].Health / match.tuning.maxHealth;
            }
            bool hasWinner = match.WinnerIndex >= 0 && match.WinnerIndex < tanks.Length;
            bool ended = match.State == MatchState.Ended && (match.IsDraw || hasWinner);
            turnLabel.SetText("TURN {0:0}  /  PLAYER {1}", match.TurnNumber, match.ActivePlayerIndex + 1);
            var wind = shots.Wind;
            windLabel.SetText("WIND {0:0.0} m/s²   X {1:0.0}  Z {2:0.0}", wind.magnitude, wind.x, wind.z);
            windArrow.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(wind.z, wind.x) * Mathf.Rad2Deg);
            var tank = shots.ActiveTank;
            if (tank != null) aimLabel.SetText("YAW {0:0.0}°     ELEVATION {1:0.0}°     POWER {2:0.0}", tank.Yaw, tank.Pitch, shots.Power);
            stateLabel.text = ended ? "MATCH COMPLETE" : match.State == MatchState.Aim ? "READY  /  SPACE TO FIRE" : "SHOT IN PROGRESS  /  CONTROLS LOCKED";
            if (resultPanel.activeSelf != ended) resultPanel.SetActive(ended);
            if (ended)
            {
                if (match.IsDraw) resultLabel.SetText("DRAW\n{0:0} TURNS", match.TurnNumber);
                else if(tanks[1-match.WinnerIndex].Drowned)
                    resultLabel.SetText("PLAYER {0} WINS\nOPPONENT SUBMERGED\n{1:0} TURNS",match.WinnerIndex+1,match.TurnNumber);
                else resultLabel.SetText("PLAYER {0} WINS\n{1:0} TURNS", match.WinnerIndex + 1, match.TurnNumber);
            }
        }
    }
}
