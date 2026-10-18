using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityProjectile;
using Object=UnityEngine.Object;

namespace Fortress.Tests
{
    public sealed class ShellVisualTests
    {
        readonly List<Object> created=new();
        ProjectilePool pool;
        ProjectileRunner prefab;
        ProjectileProfile profile;
        T Asset<T>() where T:ScriptableObject { var value=ScriptableObject.CreateInstance<T>();created.Add(value);return value; }
        GameObject Go(string name) { var value=new GameObject(name);created.Add(value);return value; }
        [SetUp] public void Setup()
        {
            var flight=Asset<FlightProfile>();flight.segments=new List<FlightSegment>{new PhysicsArcSegment { gravity=9.81f }};
            profile=Asset<ProjectileProfile>();profile.flight=flight;profile.fuse=Asset<ImpactFuse>();profile.payload=Asset<SingleImpactPayload>();profile.lingerTime=1;
            var shell=Go("Shell visual template");shell.SetActive(false);
            prefab=shell.AddComponent<ProjectileRunner>();prefab.Profile=profile;
            var model=new GameObject("Nose +Z");model.transform.SetParent(shell.transform,false);
            var trailObject=new GameObject("Trail");trailObject.transform.SetParent(shell.transform,false);
            var trail=trailObject.AddComponent<TrailRenderer>();trail.time=.3f;trail.autodestruct=false;
            shell.AddComponent<ShellVisual>().Configure(prefab,model.transform,trail);
            pool=Go("Shell visual pool").AddComponent<ProjectilePool>();
        }
        ProjectileLaunchParams Shot(Vector3 origin) => new ProjectileLaunchParams(origin,new Vector3(1,1,0).normalized,new Vector3(origin.x+20,0,origin.z),2);
        [UnityTearDown] public IEnumerator Teardown()
        {
            for(int i=created.Count-1;i>=0;i--)if(created[i]!=null)Object.Destroy(created[i]);
            created.Clear();yield return null;
        }
        [UnityTest] public IEnumerator CancelAndReuse_ClearsOldTrailAndRestartsPresentation()
        {
            var runner=pool.Get(prefab);var visual=runner.GetComponent<ShellVisual>();
            runner.Launch(Shot(Vector3.up*2),new PlaneSurface(0));
            Assert.That(visual.IsFlying,Is.True);Assert.That(visual.trail.emitting,Is.True);
            Assert.That(visual.visualRoot.gameObject.activeSelf,Is.True);
            visual.trail.AddPosition(Vector3.zero);visual.trail.AddPosition(Vector3.one);
            runner.Stop();
            Assert.That(visual.IsFlying,Is.False);Assert.That(visual.trail.emitting,Is.False);
            Assert.That(visual.trail.positionCount,Is.Zero);Assert.That(visual.visualRoot.gameObject.activeSelf,Is.False);
            pool.Release(runner);
            var reused=pool.Get(prefab);Assert.That(reused,Is.SameAs(runner));
            reused.Launch(Shot(new Vector3(100,2,0)),new PlaneSurface(0));
            Assert.That(visual.trail.positionCount,Is.Zero);Assert.That(visual.trail.emitting,Is.True);
            Assert.That(visual.IsFlying,Is.True);Assert.That(visual.visualRoot.gameObject.activeSelf,Is.True);
            yield return null;
        }
        [UnityTest] public IEnumerator VisualDirection_TracksFlightWithoutChangingTrajectoryOrImpact()
        {
            var runner=pool.Get(prefab);var visual=runner.GetComponent<ShellVisual>();
            var launch=Shot(Vector3.up*2);var surface=new PlaneSurface(0);
            var expected=ProjectilePredictor.Predict(profile,launch,surface:surface);
            runner.Launch(launch,surface);
            var releaseDirection=visual.visualRoot.forward;
            runner.Tick(.75f);var position=runner.Position;var rootRotation=runner.transform.rotation;
            visual.RefreshPose();
            Vector3 direction=runner.Trajectory.StateAtTime(runner.LocalTime).Direction.normalized;
            Assert.That(Vector3.Dot(visual.visualRoot.forward,direction),Is.GreaterThan(.9999f));
            Assert.That(Vector3.Distance(releaseDirection,direction),Is.GreaterThan(.01f));
            Assert.That(runner.Position,Is.EqualTo(position));Assert.That(runner.transform.position,Is.EqualTo(position));
            Assert.That(runner.transform.rotation,Is.EqualTo(rootRotation));
            bool impacted=false;Vector3 actual=default;
            runner.Impacted+=r=> { impacted=true;actual=r.Position;Assert.That(visual.IsFlying,Is.False);Assert.That(visual.trail.emitting,Is.False); };
            runner.Tick(30);
            Assert.That(impacted,Is.True);Assert.That(Vector3.Distance(actual,expected.ImpactPosition),Is.LessThan(.0001f));
            Assert.That(visual.trail.positionCount,Is.Zero);yield return null;
        }
        [UnityTest] public IEnumerator DisableDuringFlight_ClearsTrailAndUnsubscribes()
        {
            var runner=pool.Get(prefab);var visual=runner.GetComponent<ShellVisual>();
            runner.Launch(Shot(Vector3.up*2),new PlaneSurface(0));
            visual.trail.AddPosition(Vector3.zero);visual.trail.AddPosition(Vector3.one);
            visual.enabled=false;
            Assert.That(visual.trail.positionCount,Is.Zero);Assert.That(visual.IsFlying,Is.False);
            Assert.That(visual.trail.emitting,Is.False);runner.Stop();
            visual.enabled=true;runner.Launch(Shot(Vector3.up*2),new PlaneSurface(0));
            Assert.That(visual.IsFlying,Is.True);Assert.That(visual.trail.emitting,Is.True);
            Assert.Throws<System.ArgumentException>(()=>visual.Configure(runner,runner.transform,visual.trail));
            yield return null;
        }
    }
}
