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
        public BattlefieldCover cover;
        public SpawnArea[] spawnAreas;
        public int initialSeed = 1;
        public bool autoStart = true;
        public MatchState State { get; private set; }
        public int ActivePlayerIndex { get; private set; }
        public int TurnNumber { get; private set; }
        public float MovementRemaining { get; private set; }
        public int WinnerIndex { get; private set; } = -1;
        public bool IsDraw { get; private set; }
        public int Seed { get; private set; }
        public int Generation { get; private set; }
        public TankCombatant[] Tanks { get; private set; }
        public event Action StateChanged;
        System.Random windRandom;
        bool initialized, restarting;
        FlatShotController subscribedShots;
        readonly List<Impact> impacts = new List<Impact>(8);
        struct Impact { public Vector3 position; public float time; public int index; }
        static readonly Comparison<Impact> ImpactOrder = (a,b) => { int time=a.time.CompareTo(b.time);return time!=0?time:a.index.CompareTo(b.index); };

        void OnEnable()
        {
#if UNITY_EDITOR
            if(initialized && Tanks==null)
            {
                if(shots!=null) shots.InputEnabled=false;
                return;
            }
#endif
            Subscribe();
            if(initialized && shots!=null && Tanks!=null) { shots.InputEnabled=State==MatchState.Aim; StateChanged?.Invoke(); }
        }
#if UNITY_EDITOR
        // Hot reload retains simple fields but drops the non-serializable combat model.
        // Recreate the whole scene so terrain, cover, AI and pooled shells reset together.
        void Update()
        {
            if(!initialized || Tanks!=null) return;
            initialized=false;
            Debug.Log("Scripts reloaded: restarting the match because runtime combat data was reset.",this);
            UnityEngine.SceneManagement.SceneManager.LoadScene(gameObject.scene.path);
        }
#endif
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
            var candidateTerrain=tuning.randomTerrain
                ? terrainMap.PrepareSeededTerrain(seed,tuning,spawnAreas) : null;
            var spawnField=candidateTerrain ?? terrainMap.Field;
            var nextBuildings=cover!=null?cover.Prepare(spawnField,seed,spawnAreas):null;
            // Prepare every spawn against the candidate/original grid before touching a running match.
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
                    if(spawnField.TrySampleOriginal(p.x,p.y,out float height) && height>spawnField.WaterHeight)
                    { positions[i]=new Vector3(p.x,height,p.y);found=true;break; }
                }
                if(!found) throw new InvalidOperationException("Spawn area has no valid dry terrain sample.");
            }
            restarting=true;
            try
            {
                shots.CancelShot();impacts.Clear();
                if(candidateTerrain!=null) terrainMap.InstallTerrain(candidateTerrain);else terrainMap.ResetTerrain();
                if(cover!=null) cover.Install(terrainMap.Field,nextBuildings);
                shots.tuning=tuning;shots.terrainMap=terrainMap;
                Subscribe();
                Seed=seed;windRandom=new System.Random(unchecked(seed ^ 0x51f15e));
                Tanks=nextTanks;
                for(int i=0;i<Tanks.Length;i++) Tanks[i].Spawn(positions[i]);
                WinnerIndex=-1;IsDraw=false;ActivePlayerIndex=0;TurnNumber=1;initialized=true;
                // Commit coherent new-match state before BeginAim publishes any shot/UI events.
                Generation++;
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
            MovementRemaining=tuning.movementEnabled?Mathf.Max(0,tuning.moveDistancePerTurn):0;
            shots.InputEnabled=true;
            shots.SetActiveTank(ActivePlayerIndex);
            shots.SetSurface(new CombatShotSurface(terrainMap.Field,Tanks,ActivePlayerIndex,cover));
            float angle=(float)windRandom.NextDouble()*Mathf.PI*2;
            float magnitude=(float)windRandom.NextDouble()*Mathf.Max(0,tuning.maxWind);
            shots.SetWind(new Vector3(Mathf.Cos(angle)*magnitude,0,Mathf.Sin(angle)*magnitude));
            Vector3 toward=Tanks[1-ActivePlayerIndex].Rig.transform.position-Tanks[ActivePlayerIndex].Rig.transform.position;
            shots.SetAim(Mathf.Atan2(toward.x,toward.z)*Mathf.Rad2Deg,tuning.initialPitch,tuning.initialPower);
            SetState(MatchState.Aim);
        }
        static readonly Vector2[] FootprintDirections =
        {
            Vector2.right,Vector2.left,Vector2.up,Vector2.down,
            new Vector2(.70710678f,.70710678f),new Vector2(-.70710678f,.70710678f),
            new Vector2(.70710678f,-.70710678f),new Vector2(-.70710678f,-.70710678f)
        };
        public bool TryMoveActiveTank(Vector2 worldDirection,float deltaTime)
        {
            if(!initialized || !isActiveAndEnabled || State!=MatchState.Aim || tuning==null || !tuning.movementEnabled ||
                shots==null || shots.IsFiring || !shots.InputEnabled || terrainMap==null || terrainMap.Field==null ||
                terrainMap.Field.IsFrozen || MovementRemaining<=0 || !Finite(MovementRemaining) || !Finite(deltaTime) || deltaTime<=0 ||
                !Finite(worldDirection.sqrMagnitude) || worldDirection.sqrMagnitude<=0 ||
                !Finite(tuning.moveSpeed) || tuning.moveSpeed<=0 || !Finite(tuning.moveStep) || tuning.moveStep<=0 ||
                !Finite(tuning.maxMoveSlopeDegrees) || !Finite(tuning.maxMoveStepHeight) || tuning.maxMoveStepHeight<0) return false;
            var tank=Tanks[ActivePlayerIndex];
            if(tank.IsDefeated) return false;
            var field=terrainMap.Field;
            Vector2 direction=worldDirection.normalized;
            float requested=Mathf.Min(MovementRemaining,tuning.moveSpeed*deltaTime);
            float stepLimit=Mathf.Clamp(tuning.moveStep,.01f,.25f);
            float slope=Mathf.Tan(Mathf.Clamp(tuning.maxMoveSlopeDegrees,0,89)*Mathf.Deg2Rad);
            Vector3 ground=tank.Rig.transform.position;ground.y=tank.GroundHeight;
            float moved=0;
            MeasureFootprint(field,ground,tank.Radius,slope,tuning.maxMoveStepHeight,out int unsafeSamples,out float supportError);
            while(moved<requested)
            {
                float step=Mathf.Min(stepLimit,requested-moved);
                if(step<=0) break;
                Vector3 next=ground+new Vector3(direction.x*step,0,direction.y*step);
                if(!field.TrySample(next.x,next.z,out float height) || height<=field.WaterHeight) break;
                next.y=height;
                if(!CanTraverseHeight(field,ground,next,direction,step,tank.Radius,slope,tuning.maxMoveStepHeight)) break;
                MeasureFootprint(field,next,tank.Radius,slope,tuning.maxMoveStepHeight,out int nextUnsafe,out float nextError);
                // A shell may damage an already occupied footprint. Allow escape along a safe
                // centre path, but never add wet/out-of-bounds samples or worsen the support.
                if(nextUnsafe>unsafeSamples || (nextUnsafe==unsafeSamples && nextError>supportError+.00001f) ||
                    BlockedByTank(ground,next,tank.Radius)) break;
                ground=next;moved+=step;unsafeSamples=nextUnsafe;supportError=nextError;
            }
            if(moved<=0) return false;
            float yaw=tank.Rig.Yaw,pitch=tank.Rig.Pitch;
            tank.MoveOnGround(ground);
            tank.Rig.transform.rotation=Quaternion.Euler(0,Mathf.Atan2(direction.x,direction.y)*Mathf.Rad2Deg,0);
            tank.Rig.SetAim(yaw,pitch);
            MovementRemaining=Mathf.Max(0,MovementRemaining-moved);
            shots.SetAim(yaw,pitch,shots.Power);
            StateChanged?.Invoke();
            return true;
        }
        bool BlockedByTank(Vector3 from,Vector3 to,float radius)
        {
            if(cover!=null && cover.BlocksMovement(from,to,radius)) return true;
            var a=new Vector2(from.x,from.z);var d=new Vector2(to.x-from.x,to.z-from.z);
            float lengthSquared=d.sqrMagnitude;
            for(int i=0;i<Tanks.Length;i++)
            {
                if(i==ActivePlayerIndex) continue;
                var other=Tanks[i];var p=other.Rig.transform.position;
                var center=new Vector2(p.x,p.z);
                float t=lengthSquared>0?Mathf.Clamp01(Vector2.Dot(center-a,d)/lengthSquared):0;
                float combined=radius+other.Radius;
                if((a+d*t-center).sqrMagnitude<=combined*combined) return true;
            }
            return false;
        }
        static bool CanTraverseHeight(HeightField field,Vector3 from,Vector3 to,Vector2 direction,
            float step,float radius,float slope,float maxStepHeight)
        {
            float rise=Mathf.Abs(to.y-from.y);
            if(rise<=slope*step+.00001f) return true;
            if(rise>maxStepHeight) return false;
            // A small ledge is traversable only if a safe landing exists beyond it.
            // This prevents repeatedly spending the step allowance to scale a steep ramp.
            float probe=Mathf.Max(radius,step);
            float x=to.x+direction.x*probe,z=to.z+direction.y*probe;
            return field.TrySample(x,z,out float landing) && landing>field.WaterHeight &&
                Mathf.Abs(landing-from.y)<=maxStepHeight && Mathf.Abs(landing-to.y)<=slope*probe+.00001f;
        }
        static void MeasureFootprint(HeightField field,Vector3 center,float radius,float slope,float maxStepHeight,
            out int unsafeSamples,out float supportError)
        {
            unsafeSamples=0;supportError=0;
            float tolerance=Mathf.Max(slope*radius,maxStepHeight);
            foreach(var direction in FootprintDirections)
            {
                float x=center.x+direction.x*radius,z=center.z+direction.y*radius;
                if(!field.TrySample(x,z,out float height) || height<=field.WaterHeight) { unsafeSamples++;continue; }
                supportError+=Mathf.Max(0,Mathf.Abs(height-center.y)-tolerance);
            }
        }
        static bool Finite(float value)=>!float.IsNaN(value)&&!float.IsInfinity(value);
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
                // Evaluate shields against the intact world before this blast destroys cover.
                foreach(var tank in Tanks)
                    if(cover==null || !cover.Shields(impact.position,tank.Center)) tank.ApplyExplosion(impact.position,tuning.explosionDamage,tuning.craterRadius);
                if(cover!=null) cover.ApplyExplosion(impact.position,tuning.craterRadius,tuning.explosionDamage);
                // Projection is vertical: crater modifies the height grid at the impact XZ.
                var region=terrainMap.Field.ApplyCrater(impact.position,tuning.craterRadius,tuning.craterDepth);
                terrainMap.UpdateRegion(region);
            }
            impacts.Clear();
            if(cover!=null) cover.SettleFoundations();
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
