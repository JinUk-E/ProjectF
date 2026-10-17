#if UNITY_EDITOR
using System;
using System.Collections;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityProjectile;
using Object=UnityEngine.Object;

namespace Fortress.Tests
{
    public sealed class MatchPlayModeTests
    {
        MatchController match;
        FlatShotController shots;
        [UnitySetUp]
        public IEnumerator LoadMatch()
        {
            EditorSceneManager.LoadSceneInPlayMode("Assets/Fortress/Scenes/MilitaryMatch.unity",new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            match=Object.FindAnyObjectByType<MatchController>();shots=match.shots;
            Assert.That(match.State,Is.EqualTo(MatchState.Aim));
        }
        [UnityTearDown]
        public IEnumerator Unload()
        {
            if(shots!=null) shots.CancelShot();
            var scene=SceneManager.GetActiveScene();
            SceneManager.SetActiveScene(SceneManager.CreateScene("MatchCleanup"));
            yield return SceneManager.UnloadSceneAsync(scene);
        }
        [UnityTest]
        public IEnumerator Match_Hit_ChangesHealthAndGroundOnlyAfterFlightThenSwitchesPlayer()
        {
            AimAtOpponent();
            float hp=match.Tanks[1].Health;
            Vector3 position=match.Tanks[1].Rig.transform.position;
            var preview=Object.FindAnyObjectByType<DashedAimPreview>();
            Assert.That(shots.TryFire(),Is.True);
            Assert.That(match.terrainMap.Field.IsFrozen,Is.True);
            Assert.That(match.State,Is.EqualTo(MatchState.Firing));
            var runner=shots.ActiveRunner;
            runner.Tick(.01f);
            Assert.That(match.Tanks[1].Health,Is.EqualTo(hp));
            Assert.That(match.Tanks[1].Rig.transform.position,Is.EqualTo(position));
            Assert.That(shots.TryFire(),Is.False);
            runner.Tick(shots.tuning.maxFlightTime+1);
            Assert.That(match.terrainMap.Field.IsFrozen,Is.False);
            Assert.That(match.Tanks[1].Health,Is.LessThan(hp));
            Assert.That(match.Tanks[1].Rig.transform.position.y,Is.LessThan(position.y));
            Assert.That(match.ActivePlayerIndex,Is.EqualTo(1));
            Assert.That(match.TurnNumber,Is.EqualTo(2));
            Assert.That(shots.LastImpactError,Is.LessThan(.01f));
            Assert.That(match.State,Is.EqualTo(MatchState.Aim));
            Assert.That(shots.ActiveTank,Is.SameAs(match.Tanks[1].Rig));
            Assert.That(shots.Wind.y,Is.Zero);
            Assert.That(shots.Wind.magnitude,Is.LessThanOrEqualTo(match.tuning.maxWind));
            yield return null;
            if(CaptureEnabled()) Capture("match-after-hit.png",1280,720);
        }
        [UnityTest]
        public IEnumerator Match_Victory_DisplaysResultAndRestartButtonRestoresTerrainAndHealth()
        {
            var hud=Object.FindAnyObjectByType<MatchHud>();
            if(CaptureEnabled()) { Capture("match-ready-720.png",1280,720); Capture("match-ready-1080.png",1920,1080); }
            float original=match.terrainMap.Field.GetHeight(128,128);
            match.Tanks[1].ApplyDamage(match.Tanks[1].Health-1);
            AimAtOpponent();
            Assert.That(shots.TryFire(),Is.True);
            shots.ActiveRunner.Tick(shots.tuning.maxFlightTime+1);
            Assert.That(match.State,Is.EqualTo(MatchState.Ended));
            Assert.That(match.WinnerIndex,Is.EqualTo(0));
            Assert.That(hud.resultPanel.activeSelf,Is.True);
            Assert.That(hud.resultLabel.text,Does.Contain("PLAYER 1 WINS"));
            Assert.That(shots.TryFire(),Is.False);
            Assert.That(shots.SetAim(0,45,20),Is.False);
            yield return null;
            if(CaptureEnabled()) Capture("match-result.png",1280,720);
            int seed=match.Seed;
            int revision=match.terrainMap.Field.Revision;
            hud.restartButton.onClick.Invoke();
            Assert.That(match.Seed,Is.Not.EqualTo(seed));
            Assert.That(match.State,Is.EqualTo(MatchState.Aim));
            Assert.That(match.TurnNumber,Is.EqualTo(1));
            Assert.That(hud.resultPanel.activeSelf,Is.False);
            foreach(var tank in match.Tanks) Assert.That(tank.Health,Is.EqualTo(match.tuning.maxHealth));
            Assert.That(match.terrainMap.Field.GetHeight(128,128),Is.EqualTo(original));
            Assert.That(match.terrainMap.Field.Revision,Is.GreaterThan(revision));
            yield return null;
        }
        [UnityTest]
        public IEnumerator Match_RestartDuringFlight_CancelsAndReplaysSeedWithoutChangingLaunchRig()
        {
            var left=match.Tanks[0].Rig.transform.position;var right=match.Tanks[1].Rig.transform.position;
            var wind=shots.Wind;int seed=match.Seed;
            var visual=Object.FindAnyObjectByType<ShotPresentation>();
            Vector3 muzzle=shots.ActiveTank.Muzzle.position;
            Vector3 gun=visual.gunVisuals[0].localPosition;
            Assert.That(shots.TryFire(),Is.True);
            yield return null;
            Assert.That(shots.ActiveTank.Muzzle.position,Is.EqualTo(muzzle));
            Assert.That(visual.gunVisuals[0].localPosition,Is.Not.EqualTo(gun));
            match.RestartMatch(seed);
            Assert.That(shots.IsFiring,Is.False);
            Assert.That(match.terrainMap.Field.IsFrozen,Is.False);
            Assert.That(match.Tanks[0].Rig.transform.position,Is.EqualTo(left));
            Assert.That(match.Tanks[1].Rig.transform.position,Is.EqualTo(right));
            Assert.That(shots.Wind,Is.EqualTo(wind));
            Assert.That(visual.gunVisuals[0].localPosition,Is.EqualTo(gun));
            Assert.That(match.State,Is.EqualTo(MatchState.Aim));
            Assert.That(shots.TryFire(),Is.True);
            shots.CancelShot();
        }
        [UnityTest]
        public IEnumerator Match_FullHealthHotseat_ReachesWinnerThroughRepeatedShots()
        {
            int fired=0;
            while(match.State!=MatchState.Ended && fired<20)
            {
                AimAtOpponent();
                Assert.That(shots.TryFire(),Is.True);
                shots.ActiveRunner.Tick(shots.tuning.maxFlightTime+1);
                fired++;
                yield return null;
            }
            Assert.That(match.State,Is.EqualTo(MatchState.Ended),"Natural HP match must terminate through shots, without editing health.");
            Assert.That(fired,Is.GreaterThan(1));
            Assert.That(match.Tanks[0].IsDefeated || match.Tanks[1].IsDefeated,Is.True);
            TestContext.WriteLine("Full-health hotseat completed in "+fired+" shots, winner="+match.WinnerIndex);
            if(CaptureEnabled()) Capture("match-completed.png",1280,720);
        }
        [UnityTest]
        public IEnumerator Match_DisableAndEnable_CancelsFlightAndRestoresAimHud()
        {
            var hud=Object.FindAnyObjectByType<MatchHud>();
            Assert.That(shots.TryFire(),Is.True);
            match.enabled=false;
            Assert.That(shots.IsFiring,Is.False);
            Assert.That(match.terrainMap.Field.IsFrozen,Is.False);
            Assert.That(shots.InputEnabled,Is.False);
            match.enabled=true;
            Assert.That(match.State,Is.EqualTo(MatchState.Aim));
            Assert.That(shots.InputEnabled,Is.True);
            Assert.That(hud.stateLabel.text,Does.Contain("READY"));
            Assert.That(shots.TryFire(),Is.True);
            shots.CancelShot();
            yield return null;
        }
        void AimAtOpponent()
        {
            // Controlled wind isolates saved-scene collision/damage wiring from seed randomness.
            shots.SetWind(Vector3.zero);
            var target=match.Tanks[1-match.ActivePlayerIndex].Center;
            var delta=target-shots.ActiveTank.transform.position;
            float yaw=Mathf.Atan2(delta.x,delta.z)*Mathf.Rad2Deg;
            float best=float.PositiveInfinity,bestPitch=45,bestPower=22;
            var surface=new CombatShotSurface(match.terrainMap.Field,match.Tanks,match.ActivePlayerIndex);
            for(float pitch=15;pitch<=70;pitch+=1)
            {
                shots.ActiveTank.SetAim(yaw,pitch);
                var muzzle=shots.ActiveTank.Muzzle;
                for(float power=10;power<=35;power+=.25f)
                {
                    var shot=new ArtilleryShot(muzzle.position,muzzle.forward,power,shots.tuning.gravity,Vector3.zero);
                    shot.maxFlightTime=shots.tuning.maxFlightTime;
                    if(!ArtillerySolver.TrySolve(shot,surface,out var launch)) continue;
                    float error=Vector3.Distance(launch.target,target);
                    if(error<best) { best=error;bestPitch=pitch;bestPower=power; }
                }
            }
            Assert.That(best,Is.LessThan(2));
            Assert.That(shots.SetAim(yaw,bestPitch,bestPower),Is.True);
        }
        static bool CaptureEnabled()=>Array.IndexOf(Environment.GetCommandLineArgs(),"-fortressCapture")>=0;
        static void Capture(string filename,int width,int height)
        {
            var camera=Camera.main; var canvas=Object.FindAnyObjectByType<Canvas>();
            var target=new RenderTexture(width,height,24);var pixels=new Texture2D(width,height,TextureFormat.RGB24,false);
            var oldMode=canvas.renderMode;var oldCamera=canvas.worldCamera;var previous=RenderTexture.active;
            float aspect=camera.aspect;float distance=canvas.planeDistance;
            try
            {
                target.Create();camera.aspect=(float)width/height;
                canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;
                Canvas.ForceUpdateCanvases();
                UnityEngine.Rendering.RenderPipeline.SubmitRenderRequest(camera,new UnityEngine.Rendering.RenderPipeline.StandardRequest{destination=target});
                RenderTexture.active=target;pixels.ReadPixels(new Rect(0,0,width,height),0,0);pixels.Apply();
                System.IO.File.WriteAllBytes("Docs/Validation/"+filename,pixels.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active=previous;camera.aspect=aspect;canvas.renderMode=oldMode;canvas.worldCamera=oldCamera;canvas.planeDistance=distance;
                target.Release();Object.Destroy(target);Object.Destroy(pixels);
            }
        }
    }
}
#endif
