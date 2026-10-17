using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityProjectile;
using Object = UnityEngine.Object;

namespace Fortress.Tests
{
    public sealed class FlatShotControllerTests
    {
        readonly List<Object> created = new();
        FlatShotController controller;
        GameTuning tuning;
        TankRig tank;
        ProjectilePool pool;
        ProjectileRunner prefab;

        [SetUp]
        public void SetUp()
        {
            tuning = Asset<GameTuning>();
            var flight = Asset<FlightProfile>();
            flight.segments = new List<FlightSegment> { new PhysicsArcSegment { gravity = tuning.gravity } };
            var profile = Asset<ProjectileProfile>();
            profile.flight = flight;
            profile.fuse = Asset<ImpactFuse>();
            profile.payload = Asset<SingleImpactPayload>();
            profile.lingerTime = 0;
            prefab = Go("ShellTemplate").AddComponent<ProjectileRunner>();
            prefab.Profile = profile;
            prefab.gameObject.SetActive(false);
            pool = Go("ShellPool").AddComponent<ProjectilePool>();
            tank = Go("Tank").AddComponent<TankRig>();
            tank.transform.SetPositionAndRotation(new Vector3(3, 1, -4), Quaternion.Euler(0, 123, 0));
            tank.turret = Child("Turret", tank.transform);
            tank.barrel = Child("Barrel", tank.turret);
            tank.muzzle = Child("Muzzle", tank.barrel);
            tank.muzzle.localPosition = Vector3.forward * 2;
            controller = Go("ShotController").AddComponent<FlatShotController>();
            controller.Configure(tuning, new[] { tank }, prefab, pool, null);
        }

        [TearDown]
        public void TearDown()
        {
            if (controller != null) controller.CancelShot();
            for (int i = created.Count - 1; i >= 0; i--)
                if (created[i] != null) Object.DestroyImmediate(created[i]);
            created.Clear();
        }

        [TestCase(0f, 45f, 22f, 0f)]
        [TestCase(90f, 25f, 30f, 2f)]
        [TestCase(237f, 70f, 15f, -3f)]
        [TestCase(359f, 0f, 5f, 0f)]
        public void TryFire_FlatSurface_ActualImpactMatchesPredictionWithinOneCentimetre(
            float yaw, float pitch, float power, float windX)
        {
            Assert.That(controller.SetAim(yaw, pitch, power), Is.True);
            Assert.That(controller.SetWind(new Vector3(windX, 0, 0)), Is.True);
            Vector3 preview = controller.LastPrediction.ImpactPosition;
            int impactCount = 0, finishCount = 0;
            Vector3 actual = default;
            controller.ShotImpacted += r => { actual = r.Position; impactCount++; };
            controller.ShotFinished += () => finishCount++;
            Assert.That(controller.TryFire(), Is.True);
            Assert.That(controller.LastSolveHit, Is.True);
            var runner = controller.ActiveRunner;
            Assert.That(runner.ShotSurface, Is.Not.Null, "surface must propagate to the package runner");
            Assert.That(Vector3.Distance(runner.Params.wind, new Vector3(windX, 0, 0)), Is.LessThan(0.00001f));
            Finish(runner);
            Assert.That(impactCount, Is.EqualTo(1));
            Assert.That(finishCount, Is.EqualTo(1));
            Assert.That(Vector3.Distance(preview, actual), Is.LessThanOrEqualTo(0.01f));
            Assert.That(controller.LastImpactError, Is.LessThanOrEqualTo(0.01f));
            Assert.That(actual.y, Is.EqualTo(tuning.groundHeight).Within(0.01f));
            Assert.That(controller.ActiveRunner, Is.Null);
            Assert.That(pool.ActiveCount, Is.Zero);
        }

        [Test]
        public void SetAim_OutOfRange_ClampsPitchPowerAndUsesWorldYaw()
        {
            controller.SetAim(450, 100, 1000);
            Assert.That(tank.Yaw, Is.EqualTo(90).Within(0.0001));
            Assert.That(tank.Pitch, Is.EqualTo(tuning.maxPitch));
            Assert.That(controller.Power, Is.EqualTo(tuning.maxPower));
            Assert.That(Vector3.Distance(tank.Muzzle.forward, ArtillerySolver.Aim(90, tuning.maxPitch)), Is.LessThan(0.0001f));
            controller.SetAim(-90, -50, -10);
            Assert.That(tank.Pitch, Is.EqualTo(tuning.minPitch));
            Assert.That(controller.Power, Is.EqualTo(tuning.minPower));
            Assert.That(Vector3.Distance(tank.Muzzle.forward, ArtillerySolver.Aim(270, tuning.minPitch)), Is.LessThan(0.0001f));
        }

        [Test]
        public void TryFire_DuringFlight_RejectsSecondShotAndAimWindChangesThenUnlocks()
        {
            controller.SetAim(30, 40, 20);
            Assert.That(controller.TryFire(), Is.True);
            var runner = controller.ActiveRunner;
            Quaternion rotation = tank.Muzzle.rotation;
            Assert.That(controller.TryFire(), Is.False);
            Assert.That(controller.SetAim(80, 60, 30), Is.False);
            Assert.That(controller.SetWind(Vector3.right), Is.False);
            Assert.That(controller.ActiveRunner, Is.SameAs(runner));
            Assert.That(pool.ActiveCount, Is.EqualTo(1));
            Assert.That(Quaternion.Angle(rotation, tank.Muzzle.rotation), Is.LessThan(0.001f));
            Assert.That(controller.Power, Is.EqualTo(20));
            Finish(runner);
            Assert.That(controller.SetAim(80, 60, 30), Is.True);
            Assert.That(controller.TryFire(), Is.True);
            Finish(controller.ActiveRunner);
        }

        [Test]
        public void TryFire_NoSurfaceHit_FinishesAtConfiguredTimeLimit()
        {
            tuning.maxFlightTime = 0.5f;
            Assert.That(controller.SetSurface(new EmptySurface()), Is.True);
            controller.SetAim(0, 60, 20);
            Assert.That(controller.TryFire(), Is.True);
            Assert.That(controller.LastSolveHit, Is.False);
            Assert.That(controller.LastPrediction.ArrivalTime, Is.EqualTo(0.5f).Within(0.0001f));
            var runner = controller.ActiveRunner;
            runner.Tick(0.49f);
            Assert.That(controller.IsFiring, Is.True);
            runner.Tick(0.02f);
            Assert.That(controller.IsFiring, Is.False);
            Assert.That(pool.ActiveCount, Is.Zero);
        }

        [Test]
        public void CancelShot_WhileFiring_ReleasesRunnerAndRemovesShotCallbacks()
        {
            int impacts = 0, finishes = 0;
            controller.ShotImpacted += _ => impacts++;
            controller.ShotFinished += () => finishes++;
            Assert.That(controller.TryFire(), Is.True);
            var oldRunner = controller.ActiveRunner;
            // Exercise cancellation directly; PlayMode verifies the native OnDisable lifecycle.
            controller.CancelShot();
            Assert.That(controller.IsFiring, Is.False);
            Assert.That(controller.ActiveRunner, Is.Null);
            Assert.That(pool.ActiveCount, Is.Zero);
            var reused = pool.Get(prefab);
            Assert.That(reused, Is.SameAs(oldRunner));
            var shot = new ArtilleryShot(Vector3.up, ArtillerySolver.Aim(0, 45), 10, tuning.gravity);
            var surface = new PlaneSurface(0);
            Assert.That(ArtillerySolver.TrySolve(shot, surface, out var launch), Is.True);
            reused.Launch(launch, surface);
            reused.Tick(30);
            Assert.That(impacts, Is.Zero, "the old controller must not receive the reused runner's impact");
            Assert.That(finishes, Is.Zero, "cancellation and reuse must not produce a controller finish");
            Assert.That(pool.ActiveCount, Is.Zero);
        }

        void Finish(ProjectileRunner runner)
        {
            for (int i = 0; i < 1000 && controller.IsFiring; i++) runner.Tick(0.02f);
            Assert.That(controller.IsFiring, Is.False, "shot must complete within deterministic tick budget");
        }
        GameObject Go(string name)
        {
            var go = new GameObject(name);
            created.Add(go);
            return go;
        }
        Transform Child(string name, Transform parent)
        {
            var child = new GameObject(name).transform;
            child.SetParent(parent, false);
            return child;
        }
        T Asset<T>() where T : ScriptableObject
        {
            var asset = ScriptableObject.CreateInstance<T>();
            created.Add(asset);
            return asset;
        }
        sealed class EmptySurface : IShotSurface
        {
            public bool TryHit(Vector3 from, Vector3 to, out Vector3 hit) { hit = default; return false; }
        }
    }
}


