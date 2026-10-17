using System;
using System.Collections.Generic;
using UnityEngine;
using UnityProjectile;

namespace Fortress
{
    public enum MatchState { Aim, Firing, Resolving, Applying, Judging, Ended }

    /// <summary>Hotseat match. Standard shell is the only supported family until scatter is introduced.</summary>
    public sealed class MatchController : MonoBehaviour
    {
        public FlatShotController shots;
        public HeightFieldTerrain terrainMap;
        public GameTuning tuning;
        public SpawnArea[] spawnAreas;
        public int initialSeed = 1;
        public bool autoStart = true;
        public MatchState State { get; private set; }
        public int ActivePlayerIndex { get; private set; }
        public int TurnNumber { get; private set; }
        public int WinnerIndex { get; private set; } = -1;
        public bool IsDraw { get; private set; }
        public int Seed { get; private set; }
        public TankCombatant[] Tanks { get; private set; }
        public event Action StateChanged;
        System.Random windRandom;
        bool initialized, restarting;
        FlatShotController subscribedShots;
        readonly List<Impact> impacts = new List<Impact>(8);
        struct Impact { public Vector3 position; public float time; public int index; }
        static readonly Comparison<Impact> ImpactOrder = (a,b) => { int time=a.time.CompareTo(b.time);return time!=0?time:a.index.CompareTo(b.index); };

        void OnEnable() { Subscribe(); if(initialized && shots!=null) { shots.InputEnabled=State==MatchState.Aim; StateChanged?.Invoke(); } }
        void Start() { if(autoStart && !initialized) InitializeMatch(initialSeed); }
        void OnDisable()
        {
            restarting=true;
            if(shots!=null) { shots.InputEnabled=false;shots.CancelShot();shots.externalResolution=false; }
            impacts.Clear();
            if(initialized && State!=MatchState.Ended) State=MatchState.Aim;
            Unsubscribe();restarting=false;Time.timeScale=1;
        }
        void Subscribe()
        {
            Unsubscribe();
            if(shots==null) return;
            subscribedShots=shots;shots.externalResolution=true;
            shots.ShotLaunched+=OnLaunched;shots.ShotImpacted+=OnImpacted;
            shots.BeforeResolution+=ApplyImpacts;shots.ShotFinished+=OnFinished;shots.ShotCancelled+=OnCancelled;
        }
        void Unsubscribe()
        {
            if(subscribedShots==null) return;
            subscribedShots.ShotLaunched-=OnLaunched;subscribedShots.ShotImpacted-=OnImpacted;
            subscribedShots.BeforeResolution-=ApplyImpacts;subscribedShots.ShotFinished-=OnFinished;subscribedShots.ShotCancelled-=OnCancelled;
            subscribedShots=null;
        }
        public void InitializeMatch(int seed)
        {
            if(shots==null || terrainMap==null || tuning==null || shots.tanks==null || shots.tanks.Length!=2 ||
                shots.tanks[0]==null || shots.tanks[1]==null || spawnAreas==null || spawnAreas.Length!=2 || spawnAreas[0]==null || spawnAreas[1]==null)
                throw new InvalidOperationException("Match requires shots, terrain, tuning, two tanks and two spawn areas.");
            terrainMap.Initialize();
            if(terrainMap.Field==null) throw new InvalidOperationException("Terrain initialization failed.");
            // Prepare every spawn against the original grid before touching a running match.
            // An invalid configuration must leave the current shot, terrain, health and seed intact.
            var spawnRandom=new System.Random(unchecked(seed ^ 0x126e71));
            var nextTanks=new TankCombatant[2];
            var positions=new Vector3[2];
            for(int i=0;i<2;i++)
            {
                nextTanks[i]=new TankCombatant(shots.tanks[i],tuning);
                bool found=false;
                for(int attempt=0;attempt<Mathf.Max(1,tuning.spawnAttempts);attempt++)
                {
                    var p=spawnAreas[i].Sample(spawnRandom);
                    if(terrainMap.Field.TrySampleOriginal(p.x,p.y,out float height) && height>terrainMap.Field.WaterHeight)
                    { positions[i]=new Vector3(p.x,height,p.y);found=true;break; }
                }
                if(!found) throw new InvalidOperationException("Spawn area has no valid dry terrain sample.");
            }
            restarting=true;
            try
            {
                shots.CancelShot();impacts.Clear();terrainMap.ResetTerrain();
                shots.tuning=tuning;shots.terrainMap=terrainMap;
                Subscribe();
                Seed=seed;windRandom=new System.Random(unchecked(seed ^ 0x51f15e));
                Tanks=nextTanks;
                for(int i=0;i<Tanks.Length;i++) Tanks[i].Spawn(positions[i]);
                WinnerIndex=-1;IsDraw=false;ActivePlayerIndex=0;TurnNumber=1;initialized=true;
                // Commit coherent new-match state before BeginAim publishes any shot/UI events.
                State=MatchState.Aim;
                Time.timeScale=1;
            }
            finally { restarting=false; }
            BeginAim();
        }
        public void RestartMatch() { RestartMatch(unchecked(Seed+1)); }
        public void RestartMatch(int seed) { InitializeMatch(seed); }
        void BeginAim()
        {
            shots.InputEnabled=true;
            shots.SetActiveTank(ActivePlayerIndex);
            shots.SetSurface(new CombatShotSurface(terrainMap.Field,Tanks,ActivePlayerIndex));
            float angle=(float)windRandom.NextDouble()*Mathf.PI*2;
            float magnitude=(float)windRandom.NextDouble()*Mathf.Max(0,tuning.maxWind);
            shots.SetWind(new Vector3(Mathf.Cos(angle)*magnitude,0,Mathf.Sin(angle)*magnitude));
            Vector3 toward=Tanks[1-ActivePlayerIndex].Rig.transform.position-Tanks[ActivePlayerIndex].Rig.transform.position;
            shots.SetAim(Mathf.Atan2(toward.x,toward.z)*Mathf.Rad2Deg,tuning.initialPitch,tuning.initialPower);
            SetState(MatchState.Aim);
        }
        void SetState(MatchState value) { State=value;StateChanged?.Invoke(); }
        void OnLaunched(ProjectileRunner runner)
        {
            impacts.Clear();SetState(MatchState.Firing);
        }
        void OnImpacted(ProjectileRunner runner)
        {
            if(shots.LastSolveHit) impacts.Add(new Impact { position=runner.Position,time=runner.LocalTime,index=0 });
            SetState(MatchState.Resolving);
        }
        void ApplyImpacts()
        {
            if(!initialized || restarting) return;
            SetState(MatchState.Applying);
            impacts.Sort(ImpactOrder);
            foreach(var impact in impacts)
            {
                foreach(var tank in Tanks) tank.ApplyExplosion(impact.position,tuning.explosionDamage,tuning.craterRadius);
                // Projection is vertical: crater modifies the height grid at the impact XZ.
                var region=terrainMap.Field.ApplyCrater(impact.position,tuning.craterRadius,tuning.craterDepth);
                terrainMap.UpdateRegion(region);
            }
            impacts.Clear();
            foreach(var tank in Tanks) tank.Settle(terrainMap.Field,tuning.fallDamagePerMeter);
            SetState(MatchState.Judging);
        }
        void OnFinished()
        {
            if(!initialized || restarting) return;
            bool first=Tanks[0].IsDefeated,second=Tanks[1].IsDefeated;
            if(first || second)
            {
                IsDraw=first && second;WinnerIndex=IsDraw?-1:(first?1:0);
                shots.InputEnabled=false;SetState(MatchState.Ended);return;
            }
            ActivePlayerIndex=1-ActivePlayerIndex;TurnNumber++;
            BeginAim();
        }
        void OnCancelled()
        {
            impacts.Clear();
            if(!initialized || restarting || State==MatchState.Ended) return;
            shots.InputEnabled=true;SetState(MatchState.Aim);
        }
    }
}
