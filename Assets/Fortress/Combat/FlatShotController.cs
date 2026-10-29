using System;
using System.Collections.Generic;
using UnityEngine;
using UnityProjectile;

namespace Fortress
{
    /// <summary>Owns a complete projectile family and per-tank ammunition, separate from shared assets.</summary>
    public sealed class FlatShotController : MonoBehaviour
    {
        public GameTuning tuning;
        public TankRig[] tanks;
        public ProjectileRunner projectilePrefab;
        public ProjectilePool pool;
        public ParticleSystem impactEffect;
        public Transform predictionMarker;
        public HeightFieldTerrain terrainMap;
        public bool externalResolution;
        public TankLoadout[] availableLoadouts;
        public AiOpponent ai;
        TankLoadout[] loadouts;
        int[] specialRemaining;
        bool[] specialSelected;
        public ShotDamage CurrentShotDamage { get; private set; }
        public event Action<Vector3,float,int> ImpactRecorded;
        public TankLoadout ActiveLoadout => loadouts!=null && ActiveTankIndex<loadouts.Length?loadouts[ActiveTankIndex]:null;
        public bool SpecialSelected => specialSelected!=null && specialSelected[ActiveTankIndex];
        public int SpecialRemaining => specialRemaining!=null?specialRemaining[ActiveTankIndex]:0;
        public AmmoDefinition SelectedAmmo => ActiveLoadout==null?null:SpecialSelected?ActiveLoadout.special:ActiveLoadout.basic;
        public ProjectileRunner SelectedPrefab => SelectedAmmo!=null?SelectedAmmo.projectilePrefab:projectilePrefab;
        public int ActiveFamilyCount => family.Count;
        public TankLoadout GetLoadout(int tankIndex) => loadouts!=null && tankIndex>=0 && tankIndex<loadouts.Length ? loadouts[tankIndex] : null;
        public TankLoadout SelectedLoadoutForNewMatch(int tankIndex)
        {
            if(availableLoadouts==null || availableLoadouts.Length==0 || tankIndex<0 || tankIndex>1)return null;
            int selection=tankIndex==0?BattleLoadoutSelection.Player:BattleLoadoutSelection.Opponent;
            return availableLoadouts[Mathf.Clamp(selection,0,availableLoadouts.Length-1)];
        }
        struct Member { public ProjectileRunner runner; public float start; public int index; }
        readonly List<Member> family=new List<Member>(8);
        int nextMemberIndex;
        bool scatterShot;
        bool gameplaySuspended;
        int resumeFrame = -1;
        public bool GameplaySuspended
        {
            get => gameplaySuspended || Time.frameCount == resumeFrame;
            set { if(gameplaySuspended && !value) resumeFrame=Time.frameCount;gameplaySuspended=value; }
        }
        bool inputEnabled = true;
        public bool InputEnabled
        {
            get => inputEnabled;
            set { if (inputEnabled == value) return; inputEnabled = value; StateChanged?.Invoke(); }
        }
        public int ActiveTankIndex { get; private set; }
        public event Action BeforeResolution;
        public event Action ShotCancelled;
        public event Action StateChanged;
        public event Action<ProjectileRunner> ShotLaunched;
        public event Action<ProjectileRunner> ShotImpacted;
        public event Action ShotFinished;
        public bool IsFiring { get; private set; }
        public ProjectileRunner ActiveRunner { get; private set; }
        public ProjectilePrediction LastPrediction { get; private set; }
        public float LastImpactError { get; private set; } = float.NaN;
        public bool LastSolveHit { get; private set; }
        public float Power { get; private set; }
        public Vector3 Wind { get; private set; }
        public TankRig ActiveTank => tanks != null && ActiveTankIndex >= 0 && ActiveTankIndex < tanks.Length ? tanks[ActiveTankIndex] : null;
        bool initialized;
        public IShotSurface Surface => surface;
        IShotSurface surface;
        HeightField shotField;
        bool pendingImpact;
        Vector3 pendingImpactPosition;

        public void ResetLoadouts()
        {
            if(IsFiring) return;
            if(availableLoadouts==null || availableLoadouts.Length==0) { loadouts=null;specialRemaining=null;specialSelected=null;return; }
            loadouts=new[]{SelectedLoadoutForNewMatch(0),SelectedLoadoutForNewMatch(1)};
            specialRemaining=new int[2];specialSelected=new bool[2];
            for(int i=0;i<2;i++) specialRemaining[i]=loadouts[i]!=null?Mathf.Max(0,loadouts[i].specialRounds):0;
        }
        public bool SelectAmmo(bool special,bool aiCommand=false)
        {
            if(GameplaySuspended || !InputEnabled || IsFiring || ActiveLoadout==null || (ai!=null && ai.OwnsCurrentTurn && !aiCommand) ||
                (special && (ActiveLoadout.special==null || SpecialRemaining<=0))) return false;
            specialSelected[ActiveTankIndex]=special;RefreshPrediction();StateChanged?.Invoke();return true;
        }

        void Start() { Initialize(); }
        void OnEnable() { if (ActiveRunner != null) Subscribe(ActiveRunner); }
        void OnDisable() { CancelShot(); }

        public void Configure(GameTuning settings, TankRig[] rigs, ProjectileRunner prefab,
            ProjectilePool projectilePool, ParticleSystem effect)
        {
            CancelShot();
            tuning = settings; tanks = rigs; projectilePrefab = prefab;
            pool = projectilePool; impactEffect = effect;
            initialized = false;
            Initialize();
        }

        void Initialize()
        {
            if (initialized || tuning == null || ActiveTank == null) return;
            initialized = true;
            if (terrainMap != null) terrainMap.Initialize();
            surface = terrainMap != null && terrainMap.Surface != null
                ? (IShotSurface)terrainMap.Surface : new PlaneSurface(tuning.groundHeight);
            Wind = tuning.wind;
            SetAim(ActiveTank.transform.eulerAngles.y, tuning.initialPitch, tuning.initialPower);
        }

        public bool SetActiveTank(int index)
        {
            if (IsFiring || tanks == null || index < 0 || index >= tanks.Length) return false;
            ActiveTankIndex = index;
            RefreshPrediction();
            StateChanged?.Invoke();
            return true;
        }
        public bool SetSurface(IShotSurface shotSurface)
        {
            if (IsFiring || shotSurface == null) return false;
            Initialize();
            surface = shotSurface;
            RefreshPrediction();
            StateChanged?.Invoke();
            return true;
        }

        public bool SetWind(Vector3 wind)
        {
            if (IsFiring || !Finite(wind.x) || !Finite(wind.y) || !Finite(wind.z)) return false;
            Initialize();
            Wind = wind;
            RefreshPrediction();
            StateChanged?.Invoke();
            return true;
        }

        public bool SetAim(float yaw, float pitch, float power)
        {
            if (GameplaySuspended || !InputEnabled || IsFiring || tuning == null || ActiveTank == null || !Finite(yaw) || !Finite(pitch) || !Finite(power)) return false;
            ActiveTank.SetAim(yaw, Mathf.Clamp(pitch, tuning.minPitch, tuning.maxPitch));
            Power = Mathf.Clamp(power, tuning.minPower, tuning.maxPower);
            RefreshPrediction();
            StateChanged?.Invoke();
            return true;
        }

        public bool TryFire()
        {
            Initialize();
            if (GameplaySuspended || !InputEnabled || !isActiveAndEnabled || IsFiring || pool == null || !TryBuildLaunch(out var launch)) return false;
            var candidateField = (surface as IHeightFieldSurface)?.Field;
            if (candidateField != null && candidateField.IsFrozen) return false;
            var runner = pool.Get(SelectedPrefab);
            int firingTank=ActiveTankIndex;bool consumeSpecial=SpecialSelected;
            scatterShot=SelectedPrefab.Profile.payload is ScatterPayload;
            var ammo=SelectedAmmo;
            CurrentShotDamage=ammo!=null?ammo.Snapshot():new ShotDamage(tuning.explosionDamage,tuning.explosionDamage,tuning.craterRadius,tuning.craterDepth,0);
            shotField = candidateField;
            if (shotField != null) shotField.BeginShot();
            pendingImpact = false;
            ActiveRunner = runner;
            family.Clear();nextMemberIndex=1;family.Add(new Member{runner=runner,start=0,index=0});
            IsFiring = true;
            LastImpactError = float.NaN;
            Subscribe(runner);
            try { runner.Launch(launch, surface); }
            catch
            {
                CancelShot();
                throw;
            }
            if(consumeSpecial) { specialRemaining[firingTank]--;if(specialRemaining[firingTank]<=0) specialSelected[firingTank]=false; }
            StateChanged?.Invoke();
            return true;
        }

        bool TryBuildLaunch(out ProjectileLaunchParams launch)
        {
            launch = default;
            var tank = ActiveTank;
            var prefab=SelectedPrefab;
            if (tuning == null || tank == null || tank.Muzzle == null || prefab == null ||
                prefab.Profile == null || !(Power > 0)) return false;
            // Practice ranges have no family combat resolver; their legacy single-shell contract remains.
            if(prefab.Profile.payload!=null && !(prefab.Profile.payload is SingleImpactPayload) &&
                !(externalResolution && prefab.Profile.payload is ScatterPayload)) return false;
            if (surface == null) surface = new PlaneSurface(tuning.groundHeight);
            var shot = new ArtilleryShot(tank.Muzzle.position, tank.Muzzle.forward, Power, tuning.gravity, Wind);
            shot.maxFlightTime = tuning.maxFlightTime;
            LastSolveHit = ArtillerySolver.TrySolve(shot, surface, out launch);
            return true; // A miss still flies to the configured time limit and finishes.
        }

        void RefreshPrediction()
        {
            if (!TryBuildLaunch(out var launch)) return;
            LastPrediction = ProjectilePredictor.Predict(SelectedPrefab.Profile, launch, surface: surface);
            UpdateDebugMarker();
        }

        void Subscribe(ProjectileRunner runner)
        {
            Unsubscribe(runner);
            runner.Launched += OnLaunched;
            runner.Impacted += OnImpacted;
            runner.Finished += OnFinished;
            runner.SubmunitionLaunched += OnSubmunition;
        }
        void Unsubscribe(ProjectileRunner runner)
        {
            runner.Launched -= OnLaunched;
            runner.Impacted -= OnImpacted;
            runner.Finished -= OnFinished;
            runner.SubmunitionLaunched -= OnSubmunition;
        }
        void OnLaunched(ProjectileRunner runner)
        {
            if(family.Count==0 || family[0].index!=0 || family[0].runner!=runner) return;
            LastPrediction = runner.Prediction;
            UpdateDebugMarker();
            ShotLaunched?.Invoke(runner);
        }
        void OnImpacted(ProjectileRunner runner)
        {
            Member member=default;bool found=false;
            foreach(var item in family) if(item.runner==runner) { member=item;found=true;break; }
            if(!found) return;
            // An impact fuse may open the casing on a wall. Only its six bomblets deal damage.
            if(member.index==0 && scatterShot) return;
            var direction=runner.State.Direction;
            bool valid=member.index==0?LastSolveHit:surface.TryHit(runner.Position-direction*.05f,runner.Position+direction*.05f,out _);
            if(!valid) return;
            LastImpactError = Vector3.Distance(runner.Prediction.ImpactPosition, runner.Position);
            pendingImpact = shotField != null;
            pendingImpactPosition = runner.Position;
            ImpactRecorded?.Invoke(runner.Position,member.start+runner.LocalTime,member.index);
            if (impactEffect != null)
            {
                impactEffect.transform.position = runner.Position;
                impactEffect.Play(true);
            }
            ShotImpacted?.Invoke(runner);
            StateChanged?.Invoke();
        }
        void OnFinished(ProjectileRunner runner)
        {
            Unsubscribe(runner);
            for(int i=family.Count-1;i>=0;i--) if(family[i].runner==runner) family.RemoveAt(i);
            if(family.Count>0) { ActiveRunner=family[0].runner;return; }
            ActiveRunner = null;
            // Every active member has finished before the authoritative world is released.
            if (shotField != null)
            {
                shotField.EndShot();
                if (pendingImpact && !externalResolution)
                {
                    var region = shotField.ApplyCrater(pendingImpactPosition, tuning.craterRadius, tuning.craterDepth);
                    if (terrainMap != null && terrainMap.Field == shotField) terrainMap.UpdateRegion(region);
                }
            }
            shotField = null; pendingImpact = false;
            BeforeResolution?.Invoke();
            RefreshPrediction();
            IsFiring = false;
            ShotFinished?.Invoke();
            StateChanged?.Invoke();
        }
        public void CancelShot()
        {
            bool wasFiring = IsFiring;
            if (shotField != null) shotField.EndShot();
            shotField = null; pendingImpact = false;
            ActiveRunner = null;
            IsFiring = false;
            foreach(var member in family)
            {
                var runner=member.runner;
                if(runner==null) continue;
                Unsubscribe(runner);
                runner.Stop();
                if (pool != null) pool.Release(runner);
            }
            family.Clear();
            if (wasFiring) { ShotCancelled?.Invoke(); StateChanged?.Invoke(); }
        }
        void OnSubmunition(ProjectileRunner parent,ProjectileRunner child)
        {
            float start=parent.LocalTime;
            foreach(var member in family) if(member.runner==parent) { start+=member.start;break; }
            family.Add(new Member{runner=child,start=start,index=nextMemberIndex++});Subscribe(child);
        }
        void UpdateDebugMarker()
        {
            if (predictionMarker == null || tuning == null) return;
            predictionMarker.gameObject.SetActive(tuning.showDebugPrediction);
            predictionMarker.position = LastPrediction.ImpactPosition;
        }
        void OnDrawGizmosSelected()
        {
            if (!initialized || tuning == null || !tuning.showDebugPrediction) return;
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(LastPrediction.ImpactPosition, tuning.predictionMarkerRadius);
        }
        static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
