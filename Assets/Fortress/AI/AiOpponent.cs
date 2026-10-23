using System;
using UnityEngine;
namespace Fortress
{
    /// <summary>Plans once on an AI Aim event; Update advances only the cached thinking timer.</summary>
    public sealed class AiOpponent : MonoBehaviour
    {
        public MatchController match;
        public AiSettings settings;
        public bool IsThinking { get; private set; }
        public float RemainingThinkSeconds { get; private set; }
        public int AiTurnNumber { get; private set; }
        public AiShotPlan LastPlan { get; private set; }
        public event Action StateChanged;
        public bool OwnsCurrentTurn => isActiveAndEnabled && match!=null && match.isActiveAndEnabled && match.ActivePlayerIndex==1;
        MatchController subscribedMatch;
        ProjectileFollowCamera shotCamera;
        int plannedGeneration,plannedTurn;
        int observedGeneration = int.MinValue;
        bool dispatching, deferredPlanning;
        void Awake() { CacheCamera(); }
        void OnEnable() { Subscribe(); HandleMatchState(); }
        void Start() { Subscribe(); HandleMatchState(); }
        void OnDisable() { Unsubscribe();CancelThinking(); }
        public void Configure(MatchController target,AiSettings difficulty)
        {
            Unsubscribe();CancelThinking();match=target;settings=difficulty;CacheCamera();
            if(isActiveAndEnabled) { Subscribe();HandleMatchState(); }
        }
        void CacheCamera()
        {
            shotCamera=null;
            if(match==null) return;
            foreach(var candidate in FindObjectsByType<ProjectileFollowCamera>(FindObjectsSortMode.None))
                if(candidate.shots==match.shots) { shotCamera=candidate;break; }
        }
        void Subscribe()
        {
            Unsubscribe();if(match==null) return;
            subscribedMatch=match;subscribedMatch.StateChanged+=HandleMatchState;
        }
        void Unsubscribe()
        {
            if(subscribedMatch!=null) subscribedMatch.StateChanged-=HandleMatchState;
            subscribedMatch=null;
        }
        void CancelThinking()
        {
            bool notify=IsThinking;IsThinking=false;RemainingThinkSeconds=0;deferredPlanning=false;
            if(notify) StateChanged?.Invoke();
        }
        void HandleMatchState()
        {
            if(match!=null && observedGeneration!=match.Generation) { observedGeneration=match.Generation;AiTurnNumber=0; }
            if(!OwnsCurrentTurn || match.State!=MatchState.Aim || match.Tanks==null || match.Tanks.Length!=2 ||
                match.shots==null || !match.shots.isActiveAndEnabled || settings==null ||
                match.Tanks[0]==null || match.Tanks[1]==null || match.Tanks[0].Rig==null || match.Tanks[1].Rig==null)
            { CancelThinking();return; }
            // Scene unload can cancel a shot after tanks are destroyed but before AI OnDisable.
            // Leaving/paused sessions must not plan again from that cancellation callback.
            if(match.shots.GameplaySuspended) { deferredPlanning=true;return; }
            if(dispatching) return;
            if(IsThinking && plannedGeneration==match.Generation && plannedTurn==match.TurnNumber) return;
            CancelThinking();
            plannedGeneration=match.Generation;plannedTurn=match.TurnNumber;
            AiTurnNumber=(match.TurnNumber+1)/2;
            var role=match.shots.ActiveLoadout;
            if(role!=null)
            {
                var special=role.special;
                bool useful=special!=null && (special.purpose==AmmoPurpose.Cluster ||
                    (special.purpose==AmmoPurpose.Demolition && match.cover!=null && match.cover.Shields(match.Tanks[1].Center,match.Tanks[0].Center)) ||
                    (special.purpose==AmmoPurpose.Excavation && match.Tanks[0].GroundHeight-match.terrainMap.Field.WaterHeight<=special.depth*role.specialRounds));
                match.shots.SelectAmmo(useful && AiTurnNumber%2==0,true);
            }
            LastPlan=AiShotPlanner.Plan(match.shots,match.Tanks[0].Center,settings,match.Seed,AiTurnNumber);
            RemainingThinkSeconds=Mathf.Clamp(settings.thinkSeconds,1,2);
            IsThinking=true;StateChanged?.Invoke();
        }
        void Update()
        {
            if(deferredPlanning && match!=null && match.shots!=null && !match.shots.GameplaySuspended)
            { deferredPlanning=false;HandleMatchState(); }
            TickThinking(Time.deltaTime);
        }
        public void TickThinking(float deltaTime)
        {
            if(match!=null && match.shots!=null && match.shots.GameplaySuspended) return;
            if(!IsThinking) return;
            if(!OwnsCurrentTurn || match.State!=MatchState.Aim || plannedGeneration!=match.Generation || plannedTurn!=match.TurnNumber)
            { CancelThinking();return; }
            // Impact observation and the return movement must finish before the full thinking delay.
            // Match enters Aim before that presentation ends; spending the delay there causes back-to-back pans.
            if(Time.timeScale<=0 || (shotCamera!=null && shotCamera.isActiveAndEnabled && shotCamera.IsTracking)) return;
            if(float.IsNaN(deltaTime) || float.IsInfinity(deltaTime) || deltaTime<0) return;
            RemainingThinkSeconds=Mathf.Max(0,RemainingThinkSeconds-deltaTime);
            if(RemainingThinkSeconds>0) return;
            int expectedGeneration=plannedGeneration,expectedTurn=plannedTurn;
            IsThinking=false;
            dispatching=true;
            try
            {
                match.shots.SetAim(LastPlan.Yaw,LastPlan.Pitch,LastPlan.Power);
                // A shot/UI callback may restart the match synchronously during SetAim.
                if(OwnsCurrentTurn && match.State==MatchState.Aim && match.Generation==expectedGeneration && match.TurnNumber==expectedTurn)
                    match.shots.TryFire();
            }
            finally { dispatching=false;StateChanged?.Invoke(); }
        }
    }
}
