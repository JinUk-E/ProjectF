#if UNITY_EDITOR
using System;
using System.Collections;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
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
            var previousField=match.terrainMap.Field;
            hud.restartButton.onClick.Invoke();
            Assert.That(match.Seed,Is.Not.EqualTo(seed));
            Assert.That(match.State,Is.EqualTo(MatchState.Aim));
            Assert.That(match.TurnNumber,Is.EqualTo(1));
            Assert.That(hud.resultPanel.activeSelf,Is.False);
            foreach(var tank in match.Tanks) Assert.That(tank.Health,Is.EqualTo(match.tuning.maxHealth));
            Assert.That(match.terrainMap.Field,Is.Not.SameAs(previousField));
            Assert.That(match.terrainMap.Field.Revision,Is.Zero);
            match.RestartMatch(seed);
            Assert.That(match.terrainMap.Field.GetHeight(128,128),Is.EqualTo(original));
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
        [UnityTest]
        public IEnumerator Match_CraterReachesWater_DefeatsHealthyTankAndRestartClearsDrowning()
        {
            var original=match.tuning;
            var controlled=Object.Instantiate(original);
            // Isolate the approved water-loss path without mutating the shared settings asset.
            controlled.explosionDamage=0;controlled.fallDamagePerMeter=0;
            match.tuning=controlled;shots.tuning=controlled;
            try
            {
                Assert.That(match.terrainMap.Field.LowerHeight,Is.EqualTo(-8));
                Assert.That(match.terrainMap.Field.WaterHeight,Is.EqualTo(-4));
                var water=GameObject.Find("Match Water");
                Assert.That(water,Is.Not.Null);
                Assert.That(water.transform.position.y,Is.EqualTo(-4));
                Assert.That(water.GetComponent<Collider>(),Is.Null,"Water is a visual view, not an alternate projectile surface.");
                int fired=0;
                while(match.State!=MatchState.Ended && fired<12)
                {
                    AimAtOpponent();
                    int target=1-match.ActivePlayerIndex;
                    float height=match.Tanks[target].GroundHeight;
                    Assert.That(shots.TryFire(),Is.True);
                    shots.ActiveRunner.Tick(.01f);
                    Assert.That(match.Tanks[target].GroundHeight,Is.EqualTo(height));
                    Assert.That(match.Tanks[target].Drowned,Is.False);
                    shots.ActiveRunner.Tick(shots.tuning.maxFlightTime+1);
                    fired++;
                    yield return null;
                }
                Assert.That(match.State,Is.EqualTo(MatchState.Ended));
                var loser=match.Tanks[1-match.WinnerIndex];
                Assert.That(loser.Drowned,Is.True);
                Assert.That(loser.Health,Is.EqualTo(100),"Water alone must defeat a full-health tank.");
                Assert.That(loser.GroundHeight,Is.LessThanOrEqualTo(-4));
                var hud=Object.FindAnyObjectByType<MatchHud>();
                Assert.That(hud.resultLabel.text,Does.Contain("SUBMERGED"));
                Assert.That(shots.TryFire(),Is.False);
                TestContext.WriteLine("Water defeat at HP="+loser.Health+", ground="+loser.GroundHeight+", shots="+fired);
                if(CaptureEnabled())
                {
                    Capture("water-defeat.png",1280,720);
                    hud.resultPanel.SetActive(false);
                    var camera=Camera.main;
                    camera.transform.position=loser.Rig.transform.position+new Vector3(10,14,-10);
                    camera.transform.LookAt(loser.Rig.transform.position+Vector3.up*2);
                    Capture("water-crater-detail.png",1280,720);
                }
                hud.restartButton.onClick.Invoke();
                Assert.That(match.State,Is.EqualTo(MatchState.Aim));
                foreach(var tank in match.Tanks)
                {
                    Assert.That(tank.Drowned,Is.False);
                    Assert.That(tank.Health,Is.EqualTo(100));
                    Assert.That(tank.GroundHeight,Is.GreaterThan(-4));
                }
                Assert.That(hud.resultPanel.activeSelf,Is.False);
            }
            finally { match.tuning=original;shots.tuning=original;Object.Destroy(controlled); }
        }
        [UnityTest]
        public IEnumerator Hud_AimWindAndHealthChanges_UpdateGaugesAndActivePlayer()
        {
            var hud=Object.FindAnyObjectByType<MatchHud>();
            Assert.That(hud.activeIndicators[0].enabled,Is.True);
            Assert.That(hud.activeIndicators[1].enabled,Is.False);
            shots.SetAim(-37,62,match.tuning.minPower);
            Assert.That(hud.powerFill.fillAmount,Is.Zero.Within(.0001));
            Assert.That(hud.elevationLabel.text,Does.Contain("62"));
            Assert.That(hud.headingLabel.text,Does.Contain("37"));
            shots.SetAim(-37,62,match.tuning.maxPower);
            Assert.That(hud.powerFill.fillAmount,Is.EqualTo(1).Within(.0001));
            shots.SetWind(new Vector3(3,0,0));
            Assert.That(Mathf.Abs(Mathf.DeltaAngle(hud.windArrow.localEulerAngles.z,0)),Is.LessThan(.01));
            string windText=hud.windLabel.text;
            shots.SetWind(new Vector3(0,0,-2));
            Assert.That(Mathf.Abs(Mathf.DeltaAngle(hud.windArrow.localEulerAngles.z,-90)),Is.LessThan(.01));
            Assert.That(hud.windLabel.text,Is.Not.EqualTo(windText));
            match.Tanks[1].ApplyDamage(75);
            shots.SetAim(90,85,match.tuning.minPower);
            Assert.That(hud.healthFills[1].fillAmount,Is.EqualTo(.25f).Within(.0001));
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"-fortressHudCapture")>=0) Capture("hud-danger-720.png",1280,720);
            Assert.That(shots.TryFire(),Is.True);
            shots.ActiveRunner.Tick(shots.tuning.maxFlightTime+1);
            Assert.That(match.ActivePlayerIndex,Is.EqualTo(1));
            Assert.That(hud.activeIndicators[0].enabled,Is.False);
            Assert.That(hud.activeIndicators[1].enabled,Is.True);
            yield return null;
        }
        [UnityTest]
        public IEnumerator Hud_TurnNotice_ExpiresAndMissDoesNotClaimImpact()
        {
            var hud=Object.FindAnyObjectByType<MatchHud>();
            Assert.That(hud.notificationGroup.alpha,Is.GreaterThan(0));
            hud.TickPresentation(hud.style.notificationSeconds+hud.style.notificationFadeSeconds+1);
            Assert.That(hud.notificationGroup.alpha,Is.Zero);
            shots.SetAim(90,85,35);
            Assert.That(hud.notificationGroup.alpha,Is.Zero,"Adjusting aim must not replay the turn notification.");
            shots.SetWind(new Vector3(100,0,0));
            Assert.That(shots.TryFire(),Is.True);
            Assert.That(shots.LastSolveHit,Is.False);
            shots.ActiveRunner.Tick(shots.tuning.maxFlightTime+1);
            Assert.That(hud.notificationGroup.alpha,Is.GreaterThan(0));
            Assert.That(hud.notificationLabel.text.ToUpperInvariant(),Does.Not.Contain("IMPACT"));
            yield return null;
        }
        [UnityTest]
        public IEnumerator Hud_Presentation_CapturesReadyAndResultsAtTargetResolutions()
        {
            var hud=Object.FindAnyObjectByType<MatchHud>();
            Assert.That(Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None).Length,Is.EqualTo(1));
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"-fortressHudCapture")>=0)
            {
                Capture("hud-turn-720.png",1280,720);
                hud.TickPresentation(hud.style.notificationSeconds+hud.style.notificationFadeSeconds+1);
                Capture("hud-ready-720.png",1280,720);
                Capture("hud-ready-1080.png",1920,1080);
            }
            match.Tanks[1].ApplyDamage(match.Tanks[1].Health-1);
            AimAtOpponent();
            Assert.That(shots.TryFire(),Is.True);
            shots.ActiveRunner.Tick(shots.tuning.maxFlightTime+1);
            Assert.That(hud.resultPanel.activeSelf,Is.True);
            Assert.That(hud.activeIndicators[0].enabled || hud.activeIndicators[1].enabled,Is.False);
            yield return null;
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"-fortressHudCapture")>=0)
            {
                Capture("hud-result-720.png",1280,720);
                Capture("hud-result-1080.png",1920,1080);
            }
            hud.restartButton.onClick.Invoke();
            Assert.That(hud.resultPanel.activeSelf,Is.False);
            Assert.That(hud.activeIndicators[0].enabled,Is.True);
        }
        [UnityTest]
        public IEnumerator Keyboard_AimAndFireBindings_DriveMatchAndLockDuringFlight()
        {
            var oldFocus=InputSystem.settings.backgroundBehavior;
            var oldEditor=InputSystem.settings.editorInputBehaviorInPlayMode;
            // Batch mode has no focused Game view; route this synthetic device explicitly.
            InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            var keyboard=InputSystem.AddDevice<Keyboard>();
            try
            {
                shots.SetAim(90,45,22);
                InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.D,Key.W,Key.E));
                yield return null; yield return null;
                Assert.That(shots.ActiveTank.Yaw,Is.GreaterThan(90));
                Assert.That(shots.ActiveTank.Pitch,Is.GreaterThan(45));
                Assert.That(shots.Power,Is.GreaterThan(22));
                float yaw=shots.ActiveTank.Yaw,pitch=shots.ActiveTank.Pitch,power=shots.Power;
                InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.A,Key.S,Key.Q));
                yield return null; yield return null;
                Assert.That(shots.ActiveTank.Yaw,Is.LessThan(yaw));
                Assert.That(shots.ActiveTank.Pitch,Is.LessThan(pitch));
                Assert.That(shots.Power,Is.LessThan(power));
                InputSystem.QueueStateEvent(keyboard,new KeyboardState());
                yield return null;
                shots.SetAim(90,85,30);
                InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.Space));
                yield return null; yield return null;
                Assert.That(match.State,Is.EqualTo(MatchState.Firing));
                var runner=shots.ActiveRunner;
                yaw=shots.ActiveTank.Yaw;pitch=shots.ActiveTank.Pitch;power=shots.Power;
                InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.A,Key.S,Key.Q));
                yield return null;
                InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.Space,Key.D,Key.W,Key.E));
                yield return null; yield return null;
                Assert.That(shots.ActiveRunner,Is.SameAs(runner));
                Assert.That(shots.ActiveTank.Yaw,Is.EqualTo(yaw));
                Assert.That(shots.ActiveTank.Pitch,Is.EqualTo(pitch));
                Assert.That(shots.Power,Is.EqualTo(power));
            }
            finally
            {
                InputSystem.RemoveDevice(keyboard);
                InputSystem.settings.backgroundBehavior=oldFocus;
                InputSystem.settings.editorInputBehaviorInPlayMode=oldEditor;
            }
        }
        [UnityTest]
        public IEnumerator Terrain_NewSeedChangesMap_SameSeedRestoresMapAndSpawn()
        {
            Assert.That(match.tuning.randomTerrain,Is.True);
            match.RestartMatch(12345);
            var first=match.terrainMap.Field;
            var position=match.Tanks[0].Rig.transform.position;
            var wind=shots.Wind;
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"-fortressTerrainCapture")>=0) Capture("random-seed-12345.png",1280,720);
            match.RestartMatch(98765);
            var second=match.terrainMap.Field;
            int differences=0;
            for(int z=0;z<first.Depth;z+=8) for(int x=0;x<first.Width;x+=8)
                if(Mathf.Abs(first.GetHeight(x,z)-second.GetHeight(x,z))>.01f) differences++;
            Assert.That(differences,Is.GreaterThan(100));
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"-fortressTerrainCapture")>=0) Capture("random-seed-98765.png",1280,720);
            match.RestartMatch(12345);
            var restored=match.terrainMap.Field;
            for(int z=0;z<first.Depth;z++) for(int x=0;x<first.Width;x++)
                Assert.That(restored.GetHeight(x,z),Is.EqualTo(first.GetHeight(x,z)));
            Assert.That(match.Tanks[0].Rig.transform.position,Is.EqualTo(position));
            Assert.That(shots.Wind,Is.EqualTo(wind));
            foreach(var tank in match.Tanks) Assert.That(tank.GroundHeight,Is.GreaterThan(match.tuning.waterHeight));
            yield return null;
        }
        [UnityTest]
        public IEnumerator Terrain_InvalidGenerationDuringFlight_PreservesCurrentMatch()
        {
            shots.SetAim(90,85,30);
            Assert.That(shots.TryFire(),Is.True);
            var field=match.terrainMap.Field;var runner=shots.ActiveRunner;int seed=match.Seed;
            var original=match.tuning;
            var invalid=Object.Instantiate(original);
            invalid.terrainNoiseScale=0;
            match.tuning=invalid;
            try
            {
                Assert.Throws<ArgumentException>(()=>match.RestartMatch(99));
                Assert.That(match.terrainMap.Field,Is.SameAs(field));
                Assert.That(field.IsFrozen,Is.True);
                Assert.That(shots.ActiveRunner,Is.SameAs(runner));
                Assert.That(match.State,Is.EqualTo(MatchState.Firing));
                Assert.That(match.Seed,Is.EqualTo(seed));
            }
            finally { match.tuning=original;Object.Destroy(invalid); }
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
            // Search the same terrain AND cover surface used by the actual shot.
            var surface=shots.Surface;
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
