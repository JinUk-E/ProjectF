using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Fortress
{
    /// <summary>Touch commands adapt to the existing simulation; JavaScript never owns combat state.</summary>
    public sealed class MobileWebControls : MonoBehaviour
    {
        [Min(.1f)] public float inputLeaseSeconds=.5f;
        [Min(.05f)] public float stateIntervalSeconds=.1f;
        [Min(1)] public float cameraPixelsPerSecond=100f,zoomUnitsPerSecond=120f;
        [Min(1)] public int mobileFrameRate=30;
        public bool hideDesktopUi=true;
        public bool MobileEnabled { get; private set; }
        public int InputEpoch { get; private set; }
        SessionMenu menu;
        MatchController match;
        FlatShotController shots;
        LoadoutMenu loadouts;
        BattlefieldOrbitCamera orbit;
        Transform movementCamera;
        CanvasGroup navigation,hud;
        GroupState navigationBefore,hudBefore;
        Held held;
        float leaseRemaining,publishAt;
        int generation=-1,turn=-1;
        bool editableBefore,pausedBefore,confirmBefore,loadingBefore,endedBefore,dirty=true;
        bool performanceApplied;
        bool uiApplied;
        int oldFrameRate;
        string lastJson;
        readonly MobileState snapshot=new MobileState();
        public event Action<MobileState> StatePublished;

        [Serializable] public sealed class Held
        {
            public float x,y,yaw,pitch,power,orbitX,orbitY,zoom;
            public int epoch;
        }
        [Serializable] public sealed class MobileState
        {
            public bool enabled,ready,main,loading,paused,confirm,ended,editable,cameraEditable,aiTurn,specialSelected;
            public int inputEpoch,turn,generation,activePlayer,specialRemaining;
            public float yaw,pitch,power,move,windX,windZ;
            public float[] hp=new float[2],maxHp=new float[2];
            public string wind="",basicName="",specialName="",roleName="",opponentName="",roleDescription="",confirmationTitle="",confirmationBody="",resultText="",stateName="";
        }
        struct GroupState { public float alpha;public bool raycasts; }
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] static extern void FortressMobileState(string json);
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Install()
        {
            var root=new GameObject("Mobile web controls");DontDestroyOnLoad(root);root.AddComponent<MobileWebControls>();
        }
#endif
        void OnEnable() { SceneManager.sceneLoaded+=SceneLoaded;BindScene(); }
        void OnDisable() { SceneManager.sceneLoaded-=SceneLoaded;Unsubscribe();RestoreUi();RestorePerformance();MobileEnabled=false;ClearInput();menu=null;match=null;shots=null;orbit=null;loadouts=null; }
        void OnApplicationFocus(bool focused) { if(!focused)ClearInput(); }
        void OnApplicationPause(bool paused) { if(paused)ClearInput(); }
        void SceneLoaded(Scene scene,LoadSceneMode mode) { BindScene(); }
        void BindScene()
        {
            Unsubscribe();RestoreUi();ClearInput();
            menu=FindAnyObjectByType<SessionMenu>();match=menu!=null?menu.match:null;shots=match!=null?match.shots:null;
            loadouts=FindAnyObjectByType<LoadoutMenu>();orbit=FindAnyObjectByType<BattlefieldOrbitCamera>();
            var camera=Camera.main;movementCamera=camera!=null?camera.transform:null;
            navigation=menu!=null?menu.navigationGroup:null;hud=menu!=null?menu.combatHudGroup:null;
            navigationBefore=Capture(navigation);hudBefore=Capture(hud);
            if(match!=null)match.StateChanged+=Changed;
            if(shots!=null)shots.StateChanged+=Changed;
            generation=turn=-1;dirty=true;lastJson=null;ObserveBoundary();ApplyUi();
        }
        void Unsubscribe() { if(match!=null)match.StateChanged-=Changed;if(shots!=null)shots.StateChanged-=Changed; }
        void Changed() { dirty=true;ObserveBoundary(); }
        bool Ready=>isActiveAndEnabled && menu!=null && !menu.IsLoading && (menu.IsMainMenu || (match!=null && match.Tanks!=null));
        bool Editable=>MobileEnabled && Ready && match!=null && match.isActiveAndEnabled && match.State==MatchState.Aim &&
            !menu.IsPaused && !menu.IsConfirming && shots!=null && shots.isActiveAndEnabled && shots.ActiveTank!=null &&
            shots.InputEnabled && !shots.GameplaySuspended && !shots.IsFiring && (shots.ai==null || !shots.ai.OwnsCurrentTurn);
        bool CameraEditable=>MobileEnabled && Ready && match!=null && match.State!=MatchState.Ended && !menu.IsPaused && !menu.IsConfirming &&
            shots!=null && !shots.GameplaySuspended && orbit!=null && !orbit.CinematicOverride;
        void ObserveBoundary()
        {
            int nextGeneration=match!=null?match.Generation:-1,nextTurn=match!=null?match.TurnNumber:-1;
            bool editable=Editable,paused=menu!=null&&menu.IsPaused,confirm=menu!=null&&menu.IsConfirming,
                loading=menu!=null&&menu.IsLoading,ended=match!=null&&match.State==MatchState.Ended;
            if(nextGeneration!=generation || nextTurn!=turn || editable!=editableBefore || paused!=pausedBefore ||
                confirm!=confirmBefore || loading!=loadingBefore || ended!=endedBefore)
            {
                generation=nextGeneration;turn=nextTurn;editableBefore=editable;pausedBefore=paused;confirmBefore=confirm;
                loadingBefore=loading;endedBefore=ended;ClearInput();
            }
        }
        public void Configure(string enabled)
        {
            if(!isActiveAndEnabled)return;
            bool next=enabled=="1";if(next==MobileEnabled){dirty=true;return;}
            MobileEnabled=next;ClearInput();
            if(next)ApplyPerformance();else RestorePerformance();
            ObserveBoundary();ApplyUi();dirty=true;
        }
        public void ClearInput() { held=null;leaseRemaining=0;InputEpoch++;dirty=true; }
        public void Hold(string json)
        {
            if(!isActiveAndEnabled || !MobileEnabled || string.IsNullOrEmpty(json))return;
            ObserveBoundary();
            Held value;
            try {value=JsonUtility.FromJson<Held>(json);}catch(ArgumentException){return;}
            if(value==null || value.epoch!=InputEpoch || !Finite(value.x)||!Finite(value.y)||!Finite(value.yaw)||!Finite(value.pitch)||
                !Finite(value.power)||!Finite(value.orbitX)||!Finite(value.orbitY)||!Finite(value.zoom))return;
            if(!Editable){value.x=value.y=value.yaw=value.pitch=value.power=0;}
            if(!CameraEditable){value.orbitX=value.orbitY=value.zoom=0;}
            value.x=Axis(value.x);value.y=Axis(value.y);value.yaw=Axis(value.yaw);value.pitch=Axis(value.pitch);value.power=Axis(value.power);
            value.orbitX=Axis(value.orbitX);value.orbitY=Axis(value.orbitY);value.zoom=Axis(value.zoom);
            held=value;leaseRemaining=inputLeaseSeconds;
        }
        void Update()
        {
            ObserveBoundary();ApplyUi();TickInput(Time.deltaTime,Time.unscaledDeltaTime);
            if(dirty && Time.unscaledTime>=publishAt){publishAt=Time.unscaledTime+stateIntervalSeconds;Publish();}
        }
        public void TickInput(float deltaTime,float unscaledDeltaTime)
        {
            ObserveBoundary();if(held==null)return;
            if(!Finite(deltaTime)||!Finite(unscaledDeltaTime)||deltaTime<0||unscaledDeltaTime<0)return;
            leaseRemaining-=unscaledDeltaTime;if(leaseRemaining<=0){ClearInput();return;}
            float dt=Mathf.Min(deltaTime,.1f);
            if(Editable)
            {
                var input=held;
                var forward=movementCamera!=null?movementCamera.forward:Vector3.forward;forward.y=0;
                if(forward.sqrMagnitude<.000001f)forward=Vector3.forward;forward.Normalize();
                var world=Vector3.Cross(Vector3.up,forward)*input.x+forward*input.y;
                if(world.sqrMagnitude>0)match.TryMoveActiveTank(new Vector2(world.x,world.z),dt);
                if(input.yaw!=0||input.pitch!=0||input.power!=0)
                    shots.SetAim(shots.ActiveTank.Yaw+input.yaw*shots.tuning.yawSpeed*dt,
                        shots.ActiveTank.Pitch+input.pitch*shots.tuning.pitchSpeed*dt,shots.Power+input.power*shots.tuning.powerSpeed*dt);
            }
            if(held!=null && CameraEditable)
            {
                if(held.orbitX!=0||held.orbitY!=0){orbit.Orbit(new Vector2(held.orbitX,held.orbitY)*cameraPixelsPerSecond*dt);dirty=true;}
                if(held.zoom!=0){orbit.Zoom(held.zoom*zoomUnitsPerSecond*dt);dirty=true;}
            }
        }
        public void Orbit(string json)
        {
            if(!CameraEditable || string.IsNullOrEmpty(json))return;
            Held value;try{value=JsonUtility.FromJson<Held>(json);}catch(ArgumentException){return;}
            if(value!=null && Finite(value.x)&&Finite(value.y)){orbit.Orbit(new Vector2(Mathf.Clamp(value.x,-200,200),Mathf.Clamp(value.y,-200,200)));dirty=true;}
        }
        public void Zoom(float units) { if(CameraEditable && Finite(units)){orbit.Zoom(Mathf.Clamp(units,-240,240));dirty=true;} }
        public void Action(string command)
        {
            ObserveBoundary();if(!MobileEnabled || !Ready)return;
            bool editable=Editable;ClearInput();
            switch(command)
            {
                case "fire":if(editable)shots.TryFire();break;
                case "basic":if(editable)shots.SelectAmmo(false);break;
                case "special":if(editable)shots.SelectAmmo(true);break;
                case "pause":menu.PauseFromBrowser();break;
                case "resume":if(menu.IsPaused&&!menu.IsConfirming)menu.Resume();break;
                case "main":menu.RequestMainMenu();break;
                case "confirm":if(menu.IsConfirming)menu.ConfirmExit();break;
                case "cancel":if(menu.IsConfirming)menu.CancelConfirmation();break;
                case "restart":if(match!=null&&match.State==MatchState.Ended&&!menu.IsConfirming){menu.Resume();match.RestartMatch();}break;
                case "startAi":if(menu.IsMainMenu&&!menu.IsConfirming)menu.StartAi();break;
                case "startLocal":if(menu.IsMainMenu&&!menu.IsConfirming)menu.StartLocal();break;
                case "nextPlayer":if(menu.IsMainMenu&&!menu.IsConfirming&&loadouts!=null)loadouts.NextPlayer();break;
                case "nextOpponent":if(menu.IsMainMenu&&!menu.IsConfirming&&loadouts!=null)loadouts.NextOpponent();break;
                case "viewOwn":if(CameraEditable)orbit.ViewOwnTank();break;
                case "viewEnemy":if(CameraEditable)orbit.ViewOpponent();break;
            }
            ObserveBoundary();dirty=true;
        }
        public MobileState GetState()
        {
            snapshot.enabled=MobileEnabled;snapshot.ready=Ready;snapshot.main=menu!=null&&menu.IsMainMenu;
            snapshot.loading=menu==null||menu.IsLoading;snapshot.paused=menu!=null&&menu.IsPaused;snapshot.confirm=menu!=null&&menu.IsConfirming;
            snapshot.ended=match!=null&&match.State==MatchState.Ended;snapshot.editable=Editable;snapshot.cameraEditable=CameraEditable;
            snapshot.aiTurn=shots!=null&&shots.ai!=null&&shots.ai.OwnsCurrentTurn;snapshot.inputEpoch=InputEpoch;
            snapshot.generation=match!=null?match.Generation:0;snapshot.turn=match!=null?match.TurnNumber:0;
            snapshot.activePlayer=match!=null?match.ActivePlayerIndex:0;snapshot.move=match!=null?match.MovementRemaining:0;
            for(int i=0;i<2;i++){var tank=match!=null&&match.Tanks!=null?match.Tanks[i]:null;snapshot.hp[i]=tank!=null?tank.Health:0;snapshot.maxHp[i]=tank!=null?tank.MaxHealth:0;}
            var rig=shots!=null?shots.ActiveTank:null;snapshot.yaw=rig!=null?rig.Yaw:0;snapshot.pitch=rig!=null?rig.Pitch:0;snapshot.power=shots!=null?shots.Power:0;
            var role=shots!=null?shots.ActiveLoadout:loadouts!=null&&loadouts.roles!=null&&loadouts.roles.Length>0?loadouts.roles[Mathf.Clamp(BattleLoadoutSelection.Player,0,loadouts.roles.Length-1)]:null;
            var opponent=shots!=null?shots.GetLoadout(1):loadouts!=null&&loadouts.roles!=null&&loadouts.roles.Length>0?loadouts.roles[Mathf.Clamp(BattleLoadoutSelection.Opponent,0,loadouts.roles.Length-1)]:null;
            snapshot.roleName=KoreanUi.RoleName(role);snapshot.opponentName=KoreanUi.RoleName(opponent);snapshot.roleDescription=KoreanUi.RoleDescription(role);
            snapshot.basicName=role!=null?KoreanUi.AmmoName(role.basic):"";snapshot.specialName=role!=null&&role.special!=null?KoreanUi.AmmoName(role.special):"";
            snapshot.specialRemaining=shots!=null?shots.SpecialRemaining:role!=null?role.specialRounds:0;snapshot.specialSelected=shots!=null&&shots.SpecialSelected;
            snapshot.confirmationTitle=menu!=null&&menu.confirmationTitle!=null?menu.confirmationTitle.text:"";
            snapshot.confirmationBody=menu!=null&&menu.confirmationBody!=null?menu.confirmationBody.text:"";
            snapshot.resultText=snapshot.ended?(match.IsDraw?"무승부":match.WinnerIndex==0?"플레이어 1 승리":shots!=null&&shots.ai!=null&&shots.ai.isActiveAndEnabled?"AI 승리":"플레이어 2 승리"):"";
            var wind=shots!=null?shots.Wind:Vector3.zero;snapshot.windX=wind.x;snapshot.windZ=wind.z;
            var forward=movementCamera!=null?movementCamera.forward:Vector3.forward;forward.y=0;forward.Normalize();
            var cross=Vector3.Dot(wind,Vector3.Cross(Vector3.up,forward));var along=Vector3.Dot(wind,forward);
            snapshot.wind=wind.magnitude<.05f?"무풍":$"바람 {wind.magnitude:0.0} · {(cross>=0?"오른쪽":"왼쪽")} {Mathf.Abs(cross):0.0} / {(along>=0?"화면 위":"화면 아래")} {Mathf.Abs(along):0.0}";
            snapshot.stateName=match!=null?match.State.ToString():"Menu";return snapshot;
        }
        void Publish()
        {
            dirty=false;var state=GetState();var json=JsonUtility.ToJson(state);if(json==lastJson)return;lastJson=json;
            StatePublished?.Invoke(state);
#if UNITY_WEBGL && !UNITY_EDITOR
            FortressMobileState(json);
#endif
        }
        static GroupState Capture(CanvasGroup group)=>group!=null?new GroupState{alpha=group.alpha,raycasts=group.blocksRaycasts}:default;
        void ApplyUi()
        {
            if(!MobileEnabled||!hideDesktopUi){RestoreUi();return;}
            uiApplied=true;
            // SessionMenu exclusively owns interactable and its pause/resume snapshot.
            if(navigation!=null){navigation.alpha=0;navigation.blocksRaycasts=false;}
            if(hud!=null){hud.alpha=0;hud.blocksRaycasts=false;}
        }
        void RestoreUi()
        {
            if(!uiApplied)return;uiApplied=false;
            if(navigation!=null){navigation.alpha=navigationBefore.alpha;navigation.blocksRaycasts=navigationBefore.raycasts;}
            if(hud!=null){hud.alpha=hudBefore.alpha;hud.blocksRaycasts=hudBefore.raycasts;}
        }
        void ApplyPerformance()
        {
            if(performanceApplied)return;performanceApplied=true;oldFrameRate=Application.targetFrameRate;
            Application.targetFrameRate=mobileFrameRate;
        }
        void RestorePerformance() { if(!performanceApplied)return;Application.targetFrameRate=oldFrameRate;performanceApplied=false; }
        static bool Finite(float value)=>!float.IsNaN(value)&&!float.IsInfinity(value);
        static float Axis(float value)=>Mathf.Clamp(value,-1,1);
    }
}
