using System;
using UnityEngine;
namespace Fortress
{
    /// <summary>Local match measurements, never predicted hits and never uploaded or persisted.</summary>
    public sealed class MatchStatistics : MonoBehaviour
    {
        public MatchController match;
        public float ElapsedSeconds { get; private set; }
        public float AimSeconds { get; private set; }
        public int CompletedShots { get; private set; }
        public int EnemyHitShots { get; private set; }
        public int DirectHitShots { get; private set; }
        public int DrownedDefeats { get; private set; }
        public bool Finished { get; private set; }
        public int[] ShotsByPlayer { get; }=new int[2];
        public int[] HitsByPlayer { get; }=new int[2];
        public float[] DamageByPlayer { get; }=new float[2];
        public event Action Changed;
        MatchController subscribedMatch;
        int generation;
        void OnEnable()
        {
            if(match==null)return;
            subscribedMatch=match;match.ShotResolved+=Resolved;match.StateChanged+=MatchChanged;
            generation=match.Generation;Reset();
        }
        void OnDisable()
        {
            if(subscribedMatch!=null) { subscribedMatch.ShotResolved-=Resolved;subscribedMatch.StateChanged-=MatchChanged; }
            subscribedMatch=null;
        }
        void Reset()
        {
            ElapsedSeconds=0;AimSeconds=0;CompletedShots=EnemyHitShots=DirectHitShots=DrownedDefeats=0;Finished=false;
            Array.Clear(ShotsByPlayer,0,2);Array.Clear(HitsByPlayer,0,2);Array.Clear(DamageByPlayer,0,2);Changed?.Invoke();
        }
        void MatchChanged()
        {
            if(match==null)return;
            if(generation!=match.Generation) { generation=match.Generation;Reset(); }
            if(match.State!=MatchState.Ended || Finished)return;
            Finished=true;DrownedDefeats=0;
            if(match.Tanks!=null)foreach(var tank in match.Tanks)if(tank.Drowned)DrownedDefeats++;
            Changed?.Invoke();
        }
        void Resolved(ShotResolution resolution)
        {
            if(resolution.Generation!=generation)return;
            int shooter=resolution.ShooterIndex;if(shooter<0 || shooter>1)return;
            CompletedShots++;ShotsByPlayer[shooter]++;
            var enemy=resolution.ForTank(1-shooter);DamageByPlayer[shooter]+=enemy.TotalDamage;
            if(enemy.TotalDamage>0) { EnemyHitShots++;HitsByPlayer[shooter]++; }
            if(enemy.DirectHit)DirectHitShots++;
            Changed?.Invoke();
        }
        void Update() { Tick(Time.deltaTime); }
        public void Tick(float deltaTime)
        {
            if(!isActiveAndEnabled || match==null || !match.isActiveAndEnabled || match.Tanks==null || Finished || match.State==MatchState.Ended ||
                match.shots==null || match.shots.GameplaySuspended || Time.timeScale<=0 || float.IsNaN(deltaTime) || float.IsInfinity(deltaTime) || deltaTime<=0)return;
            ElapsedSeconds+=deltaTime;if(match.State==MatchState.Aim)AimSeconds+=deltaTime;
        }
    }
}
