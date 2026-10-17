using System;
using UnityEngine;
using UnityProjectile;

namespace Fortress
{
    /// <summary>Epic 1/2 standard shell only. Runtime state is separate from shared tuning assets.</summary>
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
            if (!InputEnabled || IsFiring || tuning == null || ActiveTank == null || !Finite(yaw) || !Finite(pitch) || !Finite(power)) return false;
            ActiveTank.SetAim(yaw, Mathf.Clamp(pitch, tuning.minPitch, tuning.maxPitch));
            Power = Mathf.Clamp(power, tuning.minPower, tuning.maxPower);
            RefreshPrediction();
            StateChanged?.Invoke();
            return true;
        }

        public bool TryFire()
        {
            Initialize();
            if (!InputEnabled || !isActiveAndEnabled || IsFiring || pool == null || !TryBuildLaunch(out var launch)) return false;
            var candidateField = (surface as IHeightFieldSurface)?.Field;
            if (candidateField != null && candidateField.IsFrozen) return false;
            var runner = pool.Get(projectilePrefab);
            shotField = candidateField;
            if (shotField != null) shotField.BeginShot();
            pendingImpact = false;
            ActiveRunner = runner;
            IsFiring = true;
            LastImpactError = float.NaN;
            Subscribe(runner);
            try { runner.Launch(launch, surface); }
            catch
            {
                Unsubscribe(runner);
                ActiveRunner = null; IsFiring = false;
                if (shotField != null) shotField.EndShot();
                shotField = null; pendingImpact = false;
                pool.Release(runner);
                StateChanged?.Invoke();
                throw;
            }
            StateChanged?.Invoke();
            return true;
        }

        bool TryBuildLaunch(out ProjectileLaunchParams launch)
        {
            launch = default;
            var tank = ActiveTank;
            if (tuning == null || tank == null || tank.Muzzle == null || projectilePrefab == null ||
                projectilePrefab.Profile == null || (projectilePrefab.Profile.payload != null && !(projectilePrefab.Profile.payload is SingleImpactPayload)) || !(Power > 0)) return false;
            if (surface == null) surface = new PlaneSurface(tuning.groundHeight);
            var shot = new ArtilleryShot(tank.Muzzle.position, tank.Muzzle.forward, Power, tuning.gravity, Wind);
            shot.maxFlightTime = tuning.maxFlightTime;
            LastSolveHit = ArtillerySolver.TrySolve(shot, surface, out launch);
            return true; // A miss still flies to the configured time limit and finishes.
        }

        void RefreshPrediction()
        {
            if (!TryBuildLaunch(out var launch)) return;
            LastPrediction = ProjectilePredictor.Predict(projectilePrefab.Profile, launch, surface: surface);
            UpdateDebugMarker();
        }

        void Subscribe(ProjectileRunner runner)
        {
            Unsubscribe(runner);
            runner.Launched += OnLaunched;
            runner.Impacted += OnImpacted;
            runner.Finished += OnFinished;
        }
        void Unsubscribe(ProjectileRunner runner)
        {
            runner.Launched -= OnLaunched;
            runner.Impacted -= OnImpacted;
            runner.Finished -= OnFinished;
        }
        void OnLaunched(ProjectileRunner runner)
        {
            LastPrediction = runner.Prediction;
            UpdateDebugMarker();
            ShotLaunched?.Invoke(runner);
        }
        void OnImpacted(ProjectileRunner runner)
        {
            LastImpactError = Vector3.Distance(LastPrediction.ImpactPosition, runner.Position);
            pendingImpact = LastSolveHit && shotField != null;
            pendingImpactPosition = runner.Position;
            if (impactEffect != null && LastSolveHit)
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
            ActiveRunner = null;
            // Standard shell is the whole shot family in Epics 1/2. Scatter remains rejected.
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
            var runner = ActiveRunner;
            bool wasFiring = IsFiring;
            if (shotField != null) shotField.EndShot();
            shotField = null; pendingImpact = false;
            ActiveRunner = null;
            IsFiring = false;
            if (runner != null)
            {
                Unsubscribe(runner);
                runner.Stop();
                if (pool != null) pool.Release(runner);
            }
            if (wasFiring) { ShotCancelled?.Invoke(); StateChanged?.Invoke(); }
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
