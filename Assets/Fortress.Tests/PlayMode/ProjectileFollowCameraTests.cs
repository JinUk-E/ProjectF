#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityProjectile;
using Object=UnityEngine.Object;
namespace Fortress.Tests
{
    public sealed class ProjectileFollowCameraTests
    {
        MatchController match;FlatShotController shots;BattlefieldOrbitCamera orbit;ProjectileFollowCamera follow;Camera camera;
        [UnitySetUp] public IEnumerator Load()
        {
            BattleLoadoutSelection.Player=BattleLoadoutSelection.Opponent=0;
            EditorSceneManager.LoadSceneInPlayMode(SessionMenu.LocalScene,new LoadSceneParameters(LoadSceneMode.Single));yield return null;
            match=Object.FindFirstObjectByType<MatchController>();shots=match.shots;camera=Camera.main;
            orbit=camera.GetComponent<BattlefieldOrbitCamera>();follow=camera.GetComponent<ProjectileFollowCamera>();
            Assert.That(follow,Is.Not.Null,"Saved battle scenes must install the projectile camera.");Assert.That(follow.settings,Is.Not.Null);
            Time.timeScale=1;AudioListener.pause=false;
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            if(shots!=null) shots.CancelShot();if(follow!=null) follow.RestoreImmediate();Time.timeScale=1;AudioListener.pause=false;
            BattleLoadoutSelection.Player=BattleLoadoutSelection.Opponent=0;
            var scene=SceneManager.GetActiveScene();SceneManager.SetActiveScene(SceneManager.CreateScene("FollowCameraCleanup"));yield return SceneManager.UnloadSceneAsync(scene);
        }
        ProjectileRunner Fire()
        {
            shots.SetWind(Vector3.zero);shots.SetAim(0,75,28);Assert.That(shots.TryFire(),Is.True);return shots.ActiveRunner;
        }
        void FinishPresentation()
        {
            follow.TickPresentation(follow.settings.impactHoldSeconds+.01f);
            follow.TickPresentation(follow.settings.returnSeconds+.01f);
        }
        static void InViewport(Camera view,Vector3 world)
        {
            var p=view.WorldToViewportPoint(world);Assert.That(p.z,Is.GreaterThan(0));Assert.That(p.x,Is.InRange(.01f,.99f));Assert.That(p.y,Is.InRange(.01f,.99f));
        }
        [UnityTest] public IEnumerator SingleShot_FollowsVisibleShell_ThenRestoresManualPoseAndFov()
        {
            orbit.FocusActiveTank();orbit.Orbit(new Vector2(125,-40));orbit.Zoom(120);orbit.ApplyPose();
            var position=camera.transform.position;var rotation=camera.transform.rotation;float fov=camera.fieldOfView;
            float yaw=orbit.Yaw,pitch=orbit.Pitch,distance=orbit.Distance;var target=orbit.Target;
            var runner=Fire();Assert.That(follow.IsTracking,Is.True);Assert.That(orbit.CinematicOverride,Is.True);
            orbit.Orbit(new Vector2(500,500));orbit.Zoom(600);orbit.FocusActiveTank();orbit.ApplyPose();
            Assert.That(orbit.Yaw,Is.EqualTo(yaw));Assert.That(orbit.Pitch,Is.EqualTo(pitch));Assert.That(orbit.Distance,Is.EqualTo(distance));Assert.That(orbit.Target,Is.EqualTo(target));
            for(int i=0;i<20;i++) { runner.Tick(.05f);follow.TickPresentation(.05f); }
            Assert.That(Vector3.Distance(camera.transform.position,position),Is.GreaterThan(1));InViewport(camera,runner.Position);
            runner.Tick(30);Assert.That(follow.Phase,Is.EqualTo(ProjectileFollowCamera.ViewPhase.ImpactHold));
            FinishPresentation();Assert.That(follow.IsTracking,Is.False);Assert.That(orbit.CinematicOverride,Is.False);
            Assert.That(Vector3.Distance(camera.transform.position,position),Is.LessThan(.01));Assert.That(Quaternion.Angle(camera.transform.rotation,rotation),Is.LessThan(.01));Assert.That(camera.fieldOfView,Is.EqualTo(fov).Within(.001));
            Assert.That(orbit.Yaw,Is.EqualTo(yaw));Assert.That(orbit.Target,Is.EqualTo(target));yield return null;
        }
        [UnityTest] public IEnumerator Pause_FreezesFlightHoldAndReturnPresentation()
        {
            var menu=Object.FindFirstObjectByType<SessionMenu>();var runner=Fire();runner.Tick(.5f);follow.TickPresentation(.2f);
            menu.Pause();var position=camera.transform.position;var rotation=camera.transform.rotation;float fov=camera.fieldOfView;
            follow.TickPresentation(10);Assert.That(camera.transform.position,Is.EqualTo(position));Assert.That(camera.transform.rotation,Is.EqualTo(rotation));Assert.That(camera.fieldOfView,Is.EqualTo(fov));
            menu.Resume();yield return null;runner.Tick(30);
            menu.Pause();follow.TickPresentation(10);Assert.That(follow.Phase,Is.EqualTo(ProjectileFollowCamera.ViewPhase.ImpactHold));
            menu.Resume();yield return null;follow.TickPresentation(follow.settings.impactHoldSeconds+.01f);
            Assert.That(follow.Phase,Is.EqualTo(ProjectileFollowCamera.ViewPhase.Returning));
            menu.Pause();position=camera.transform.position;follow.TickPresentation(10);Assert.That(camera.transform.position,Is.EqualTo(position));Assert.That(follow.Phase,Is.EqualTo(ProjectileFollowCamera.ViewPhase.Returning));
            menu.Resume();yield return null;follow.TickPresentation(2);Assert.That(follow.IsTracking,Is.False);
        }
        [UnityTest] public IEnumerator Cluster_FramesWholeFamily_AndWaitsForLastChild()
        {
            BattleLoadoutSelection.Player=3;match.RestartMatch(1);Assert.That(shots.SelectAmmo(true),Is.True);
            var root=Fire();var children=new List<ProjectileRunner>();root.SubmunitionLaunched+=(p,c)=>children.Add(c);
            root.Tick(root.Prediction.PayloadTime+.01f);Assert.That(children.Count,Is.EqualTo(6));Assert.That(follow.TrackedCount,Is.EqualTo(7));
            Assert.That(follow.Phase,Is.EqualTo(ProjectileFollowCamera.ViewPhase.Following));
            foreach(var child in children) child.Tick(.4f);
            for(int i=0;i<50;i++) follow.TickPresentation(.02f);
            foreach(var child in children) InViewport(camera,child.Position);
            children[0].Tick(30);follow.TickPresentation(.1f);
            Assert.That(follow.Phase,Is.EqualTo(ProjectileFollowCamera.ViewPhase.Following));Assert.That(follow.FamilyRadius,Is.GreaterThan(0));Assert.That(orbit.CinematicOverride,Is.True);
            for(int i=1;i<children.Count;i++) children[i].Tick(30);
            Assert.That(follow.Phase,Is.EqualTo(ProjectileFollowCamera.ViewPhase.ImpactHold));FinishPresentation();Assert.That(follow.IsTracking,Is.False);yield return null;
        }
        [UnityTest] public IEnumerator CancelRestartAndDisable_RestoreAndReleaseCameraOwnership()
        {
            var original=camera.transform.position;float fov=camera.fieldOfView;
            Fire().Tick(.5f);follow.TickPresentation(.2f);shots.CancelShot();Assert.That(follow.IsTracking,Is.False);Assert.That(orbit.CinematicOverride,Is.False);Assert.That(camera.fieldOfView,Is.EqualTo(fov));
            Assert.That(Vector3.Distance(camera.transform.position,original),Is.LessThan(.01));
            Fire().Tick(30);Assert.That(follow.Phase,Is.EqualTo(ProjectileFollowCamera.ViewPhase.ImpactHold));match.RestartMatch(2);
            Assert.That(follow.IsTracking,Is.False);Assert.That(orbit.CinematicOverride,Is.False);
            Fire().Tick(.4f);follow.TickPresentation(.3f);follow.enabled=false;Assert.That(orbit.CinematicOverride,Is.False);Assert.That(follow.IsTracking,Is.False);
            shots.ActiveRunner.Tick(30);Assert.That(follow.IsTracking,Is.False,"Disabled camera must not react to old family completion.");
            follow.enabled=true;Fire();Assert.That(follow.IsTracking,Is.True);shots.CancelShot();yield return null;
        }
        [UnityTest] public IEnumerator NewShotDuringReturn_PreservesOriginalUserView()
        {
            var position=camera.transform.position;var rotation=camera.transform.rotation;float fov=camera.fieldOfView;
            Fire().Tick(30);follow.TickPresentation(follow.settings.impactHoldSeconds+.01f);follow.TickPresentation(.2f);
            Assert.That(follow.Phase,Is.EqualTo(ProjectileFollowCamera.ViewPhase.Returning));
            Fire().Tick(.5f);follow.TickPresentation(.2f);shots.CancelShot();
            Assert.That(Vector3.Distance(camera.transform.position,position),Is.LessThan(.01));Assert.That(Quaternion.Angle(camera.transform.rotation,rotation),Is.LessThan(.01));Assert.That(camera.fieldOfView,Is.EqualTo(fov));yield return null;
        }
        sealed class NoHitSurface:IHeightFieldSurface
        {
            public HeightField Field {get;}
            public NoHitSurface(HeightField field){Field=field;}
            public bool TryHit(Vector3 a,Vector3 b,out Vector3 hit){hit=default;return false;}
        }
        [UnityTest] public IEnumerator EarlyImpactAndMissTimeout_BothFinishWithoutCameraLock()
        {
            shots.SetWind(Vector3.zero);shots.SetAim(0,0,shots.tuning.minPower);Assert.That(shots.TryFire(),Is.True);shots.ActiveRunner.Tick(30);
            Assert.That(follow.Phase,Is.EqualTo(ProjectileFollowCamera.ViewPhase.ImpactHold));FinishPresentation();Assert.That(orbit.CinematicOverride,Is.False);
            shots.SetSurface(new NoHitSurface(match.terrainMap.Field));var runner=Fire();Assert.That(shots.LastSolveHit,Is.False);runner.Tick(40);
            Assert.That(follow.Phase,Is.EqualTo(ProjectileFollowCamera.ViewPhase.ImpactHold));FinishPresentation();Assert.That(follow.IsTracking,Is.False);Assert.That(orbit.CinematicOverride,Is.False);yield return null;
        }
        [UnityTest] public IEnumerator TrackingAndReturning_NeverPlaceCameraBelowTerrainOrExterior()
        {
            orbit.FocusActiveTank();orbit.Zoom(100000);orbit.ApplyPose();var runner=Fire();
            for(int i=0;i<35;i++)
            {
                runner.Tick(.1f);follow.TickPresentation(.1f);AssertAboveGround();
            }
            if(shots.IsFiring) runner.Tick(30);
            for(int i=0;i<25;i++){follow.TickPresentation(.1f);AssertAboveGround();}
            yield return null;
        }
        void AssertAboveGround()
        {
            var p=camera.transform.position;
            if(match.terrainMap.Field.TrySample(p.x,p.z,out float ground) || orbit.exterior.TrySampleVisualHeight(p.x,p.z,out ground))
                Assert.That(p.y,Is.GreaterThanOrEqualTo(ground+Mathf.Min(orbit.settings.groundClearance,follow.settings.groundClearance)-.01f));
        }
    }
}
#endif
