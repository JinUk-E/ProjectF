using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityProjectile;

namespace Fortress
{
    // ui-developer-codex: event-driven hotseat presentation; rules belong to MatchController.
    [DefaultExecutionOrder(200)]
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
        public TMP_Text windDetailLabel;
        public Camera windCamera;
        [Min(0)] public float calmWindThreshold=.05f;
        void Awake() { if(windCamera==null) windCamera=Camera.main; }
        void LateUpdate() { RefreshWindPresentation(); }
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
        // Arrow points where the wind pushes a shell, in the current screen axes (+X right, +Y up).
        public static Vector2 ProjectWindToScreen(Vector3 wind,Transform cameraTransform)
        {
            if(cameraTransform==null) return new Vector2(wind.x,wind.z);
            return new Vector2(Vector3.Dot(wind,cameraTransform.right),Vector3.Dot(wind,cameraTransform.up));
        }
        public void RefreshWindPresentation()
        {
            if(windArrow==null || match==null || match.shots==null) return;
            var wind=match.shots.Wind;
            var projected=ProjectWindToScreen(wind,windCamera!=null?windCamera.transform:null);
            bool visible=wind.sqrMagnitude>calmWindThreshold*calmWindThreshold && projected.sqrMagnitude>.000001f;
            if(windArrow.gameObject.activeSelf!=visible) windArrow.gameObject.SetActive(visible);
            if(visible) windArrow.localRotation=Quaternion.Euler(0,0,Mathf.Atan2(projected.y,projected.x)*Mathf.Rad2Deg);
        }
        void RefreshWindDetails(Vector3 wind)
        {
            if(windDetailLabel==null) return;
            if(wind.magnitude<=calmWindThreshold) { windDetailLabel.SetText("무풍");return; }
            var tank=match.shots.ActiveTank;
            if(tank==null) { windDetailLabel.SetText("화살표 = 바람이 미는 방향");return; }
            float yaw=tank.Yaw*Mathf.Deg2Rad;
            var forward=new Vector3(Mathf.Sin(yaw),0,Mathf.Cos(yaw));
            var right=new Vector3(forward.z,0,-forward.x);
            float cross=Vector3.Dot(wind,right),along=Vector3.Dot(wind,forward);
            windDetailLabel.SetText(cross>=0
                ? (along>=0?"포신 기준: 오른쪽 {0:0.0} / 뒷바람 {1:0.0}":"포신 기준: 오른쪽 {0:0.0} / 맞바람 {1:0.0}")
                : (along>=0?"포신 기준: 왼쪽 {0:0.0} / 뒷바람 {1:0.0}":"포신 기준: 왼쪽 {0:0.0} / 맞바람 {1:0.0}"),Mathf.Abs(cross),Mathf.Abs(along));
        }
        public void Refresh()
        {
            if (match == null || match.shots == null || match.tuning == null) return;
            var shots = match.shots;
            if(movementLabel!=null) movementLabel.SetText("방향키 이동  /  남은 거리 {0:0.0}m",match.MovementRemaining);
            var tanks = match.Tanks;
            if (tanks == null || tanks.Length != 2) return;
            for (int i = 0; i < 2; i++)
            {
                if(ai!=null && i==1) healthLabels[i].SetText("AI  /  체력 {0:0}",tanks[i].Health);
                else healthLabels[i].SetText("플레이어 {0}  /  체력 {1:0}", i + 1, tanks[i].Health);
                healthFills[i].fillAmount = tanks[i].Health / tanks[i].MaxHealth;
                if (style != null) healthFills[i].color = healthFills[i].fillAmount <= style.dangerHealthFraction ? style.danger : style.accent;
                if (activeIndicators != null && i < activeIndicators.Length && activeIndicators[i] != null)
                    activeIndicators[i].enabled = match.State != MatchState.Ended && match.ActivePlayerIndex == i;
            }
            bool hasWinner = match.WinnerIndex >= 0 && match.WinnerIndex < tanks.Length;
            bool ended = match.State == MatchState.Ended && (match.IsDraw || hasWinner);
            bool aiTurn=ai!=null && match.ActivePlayerIndex==1;
            if(aiTurn) turnLabel.SetText("{0:0}턴  /  AI",match.TurnNumber);
            else turnLabel.SetText("{0:0}턴  /  플레이어 {1}", match.TurnNumber, match.ActivePlayerIndex + 1);
            var wind = shots.Wind;
            windLabel.SetText("화면 기준 바람  {0:0.0} m/s²", wind.magnitude);
            RefreshWindPresentation();
            RefreshWindDetails(wind);
            var tank = shots.ActiveTank;
            if (tank != null) aimLabel.SetText("방위 {0:0.0}°     고각 {1:0.0}°     화력 {2:0.0}", tank.Yaw, tank.Pitch, shots.Power);
            if (tank != null && elevationLabel != null) elevationLabel.SetText("{0:0.0}°", tank.Pitch);
            if (tank != null && headingLabel != null) headingLabel.SetText("방위  {0:0.0}°", tank.Yaw);
            if (powerFill != null) powerFill.fillAmount = Mathf.InverseLerp(match.tuning.minPower, match.tuning.maxPower, shots.Power);
            if (powerLabel != null) powerLabel.SetText("{0:0.0}", shots.Power);
            stateLabel.text = ended ? "대전 종료" : aiTurn && match.State==MatchState.Aim ? (ai.IsMoving ? "AI 이동 중...  /  사격 위치 변경" : "AI 조준 중...  /  잠시 기다려 주세요") : match.State == MatchState.Aim ? "사격 준비  /  SPACE 발사" : "포탄 비행 중  /  조작 잠금";
            if (resultPanel.activeSelf != ended) resultPanel.SetActive(ended);
            if (ended) ClearNotice();
            if (!ended && match.State == MatchState.Aim && (displayedTurn != match.TurnNumber || displayedSeed != match.Seed))
            {
                if (displayedSeed != match.Seed || match.TurnNumber < displayedTurn) pendingImpact = false;
                displayedTurn = match.TurnNumber; displayedSeed = match.Seed;
                if (notificationLabel != null)
                {
                    if(aiTurn) notificationLabel.text="AI 차례  /  조준 중";
                    else notificationLabel.SetText(pendingImpact ? "착탄 확인  /  플레이어 {0} 차례" : "플레이어 {0}  /  내 차례", match.ActivePlayerIndex + 1);
                }
                pendingImpact = false;
                if (notificationGroup != null && style != null) { notificationRemaining = style.notificationSeconds; notificationGroup.alpha = 1; }
            }
            if (ended)
            {
                if (match.IsDraw) resultLabel.SetText("무승부\n{0:0}턴", match.TurnNumber);
                else if(ai!=null)
                {
                    bool drowned=tanks[1-match.WinnerIndex].Drowned;
                    resultLabel.SetText(match.WinnerIndex==0
                        ? (drowned ? "승리\n상대 전차 침수\n{0:0}턴" : "승리\n{0:0}턴")
                        : (drowned ? "패배\n내 전차 침수\n{0:0}턴" : "패배\n{0:0}턴"),match.TurnNumber);
                }
                else if(tanks[1-match.WinnerIndex].Drowned)
                    resultLabel.SetText("플레이어 {0} 승리\n상대 전차 침수\n{1:0}턴",match.WinnerIndex+1,match.TurnNumber);
                else resultLabel.SetText("플레이어 {0} 승리\n{1:0}턴", match.WinnerIndex + 1, match.TurnNumber);
            }
        }
    }
}


