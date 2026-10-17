#if UNITY_EDITOR
using System;
using System.Collections;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

namespace Fortress.Tests
{
    public sealed class AiMatchTests
    {
        MatchController match;AiOpponent ai;FlatShotController shots;
        [UnitySetUp] public IEnumerator Load()
        {
            EditorSceneManager.LoadSceneInPlayMode("Assets/Fortress/Scenes/MilitaryAI.unity",new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            ai=Object.FindAnyObjectByType<AiOpponent>();match=ai.match;shots=match.shots;
        }
        [UnityTearDown] public IEnumerator Unload()
        {
            if(shots!=null) shots.CancelShot();
            var scene=SceneManager.GetActiveScene();SceneManager.SetActiveScene(SceneManager.CreateScene("AiCleanup"));
            yield return SceneManager.UnloadSceneAsync(scene);
        }
        void FinishHumanShot()
        {
            Assert.That(match.ActivePlayerIndex,Is.Zero);
            shots.SetAim(0,85,30);Assert.That(shots.TryFire(),Is.True);
            shots.ActiveRunner.Tick(shots.tuning.maxFlightTime+1);
            Assert.That(match.ActivePlayerIndex,Is.EqualTo(1));
            Assert.That(ai.IsThinking,Is.True);
        }
        [UnityTest] public IEnumerator Ai_ThinksBlocksKeyboardFiresOnceAndReturnsHumanTurn()
        {
            FinishHumanShot();
            var hud=Object.FindAnyObjectByType<MatchHud>();
            Assert.That(hud.stateLabel.text,Does.Contain("AI AIMING"));
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"-fortressAiCapture")>=0) Capture("ai-thinking.png");
            Assert.That(ai.LastPlan.SolveCount,Is.LessThanOrEqualTo(200));
            var focus=InputSystem.settings.backgroundBehavior;var editor=InputSystem.settings.editorInputBehaviorInPlayMode;
            InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            var keyboard=InputSystem.AddDevice<Keyboard>();
            float yaw=shots.ActiveTank.Yaw,pitch=shots.ActiveTank.Pitch,power=shots.Power;
            try
            {
                InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.Space,Key.A,Key.W,Key.E));
                yield return null;yield return null;
                Assert.That(shots.IsFiring,Is.False);
                Assert.That(shots.ActiveTank.Yaw,Is.EqualTo(yaw));Assert.That(shots.ActiveTank.Pitch,Is.EqualTo(pitch));Assert.That(shots.Power,Is.EqualTo(power));
            }
            finally { InputSystem.RemoveDevice(keyboard);InputSystem.settings.backgroundBehavior=focus;InputSystem.settings.editorInputBehaviorInPlayMode=editor; }
            ai.TickThinking(2);
            Assert.That(shots.IsFiring,Is.True);Assert.That(ai.IsThinking,Is.False);
            var runner=shots.ActiveRunner;ai.TickThinking(100);Assert.That(shots.ActiveRunner,Is.SameAs(runner));
            runner.Tick(shots.tuning.maxFlightTime+1);
            Assert.That(match.ActivePlayerIndex,Is.Zero);Assert.That(match.State,Is.EqualTo(MatchState.Aim));
        }
        [UnityTest] public IEnumerator Ai_RestartSameSeedAndDisable_CancelPendingFire()
        {
            FinishHumanShot();int seed=match.Seed;
            match.RestartMatch(seed);ai.TickThinking(100);
            Assert.That(shots.IsFiring,Is.False);Assert.That(match.ActivePlayerIndex,Is.Zero);Assert.That(ai.IsThinking,Is.False);
            FinishHumanShot();ai.enabled=false;ai.TickThinking(100);
            Assert.That(shots.IsFiring,Is.False);Assert.That(ai.IsThinking,Is.False);
            ai.enabled=true;
            Assert.That(ai.IsThinking,Is.True);
            ai.TickThinking(2);Assert.That(shots.IsFiring,Is.True);
            match.RestartMatch(seed);ai.TickThinking(100);
            Assert.That(shots.IsFiring,Is.False);Assert.That(ai.IsThinking,Is.False);
            yield return null;
        }
        [UnityTest] public IEnumerator Planner_DeterministicBudgetedAndDoesNotMoveLiveRig()
        {
            var target=match.Tanks[1].Center;var muzzle=shots.ActiveTank.Muzzle.position;
            float yaw=shots.ActiveTank.Yaw,pitch=shots.ActiveTank.Pitch;
            var a=AiShotPlanner.Plan(shots,target,ai.settings,123,1);
            var b=AiShotPlanner.Plan(shots,target,ai.settings,123,1);
            Assert.That(a.SolveCount,Is.InRange(1,200));
            Assert.That(a.Yaw,Is.EqualTo(b.Yaw));Assert.That(a.Pitch,Is.EqualTo(b.Pitch));Assert.That(a.Power,Is.EqualTo(b.Power));
            Assert.That(shots.ActiveTank.Muzzle.position,Is.EqualTo(muzzle));
            Assert.That(shots.ActiveTank.Yaw,Is.EqualTo(yaw));Assert.That(shots.ActiveTank.Pitch,Is.EqualTo(pitch));
            Assert.That(a.Power,Is.InRange(match.tuning.minPower,match.tuning.maxPower));
            Assert.That(a.Pitch,Is.InRange(match.tuning.minPitch,match.tuning.maxPitch));
            var far=AiShotPlanner.Plan(shots,new Vector3(10000,0,10000),ai.settings,123,1);
            Assert.That(far.UsedFallback,Is.True);Assert.That(far.SolveCount,Is.LessThanOrEqualTo(200));
            yield return null;
        }
        [UnityTest] public IEnumerator Ai_RestartInsideAimEvent_DoesNotFireIntoNewMatch()
        {
            FinishHumanShot();
            Action restart=null;
            restart=()=>{ shots.StateChanged-=restart;match.RestartMatch(match.Seed); };
            shots.StateChanged+=restart;
            try { ai.TickThinking(2); }
            finally { shots.StateChanged-=restart; }
            Assert.That(match.ActivePlayerIndex,Is.Zero);
            Assert.That(match.State,Is.EqualTo(MatchState.Aim));
            Assert.That(shots.IsFiring,Is.False);Assert.That(ai.IsThinking,Is.False);
            Assert.That(ai.AiTurnNumber,Is.Zero);
            yield return null;
        }
        [UnityTest] public IEnumerator AiMatch_FullHealthCompletesAndResultRestartResetsAi()
        {
            var precise=Object.Instantiate(ai.settings);precise.yawSigmaDegrees=0;precise.pitchSigmaDegrees=0;precise.powerSigma=0;
            int count=0;
            try
            {
                while(match.State!=MatchState.Ended && count<40)
                {
                    if(match.ActivePlayerIndex==0)
                    {
                        var plan=AiShotPlanner.Plan(shots,match.Tanks[1].Center,precise,match.Seed,1);
                        shots.SetAim(plan.Yaw,plan.Pitch,plan.Power);Assert.That(shots.TryFire(),Is.True);
                    }
                    else { Assert.That(ai.IsThinking,Is.True);ai.TickThinking(2);Assert.That(shots.IsFiring,Is.True); }
                    shots.ActiveRunner.Tick(shots.tuning.maxFlightTime+1);count++;
                    yield return null;
                }
                TestContext.WriteLine("AI match finished after "+count+" shots; winner="+match.WinnerIndex);
                Assert.That(match.State,Is.EqualTo(MatchState.Ended));Assert.That(ai.IsThinking,Is.False);
                var hud=Object.FindAnyObjectByType<MatchHud>();Assert.That(hud.resultPanel.activeSelf,Is.True);
                if(Array.IndexOf(Environment.GetCommandLineArgs(),"-fortressAiCapture")>=0) Capture("ai-result.png");
                ai.TickThinking(100);Assert.That(shots.IsFiring,Is.False);
                hud.restartButton.onClick.Invoke();Assert.That(match.ActivePlayerIndex,Is.Zero);Assert.That(ai.IsThinking,Is.False);
            }
            finally { Object.Destroy(precise); }
        }
        [UnityTest] public IEnumerator EasyAi_RetainsLateTurnError_AndIsLessAccurateThanNormal()
        {
            shots.SetWind(Vector3.zero);
            var normal=UnityEditor.AssetDatabase.LoadAssetAtPath<AiSettings>("Assets/Fortress/Data/AiNormal.asset");
            double first=0,third=0,late=0,normalLate=0;
            int fallbacks=0;
            for(int seed=1;seed<=64;seed++)
            {
                var a=AiShotPlanner.Plan(shots,match.Tanks[1].Center,ai.settings,seed,1);
                var b=AiShotPlanner.Plan(shots,match.Tanks[1].Center,ai.settings,seed,3);
                late+=AiShotPlanner.Plan(shots,match.Tanks[1].Center,ai.settings,seed,20).DistanceToTarget;
                normalLate+=AiShotPlanner.Plan(shots,match.Tanks[1].Center,normal,seed,20).DistanceToTarget;
                first+=a.DistanceToTarget;third+=b.DistanceToTarget;
                if(a.UsedFallback) fallbacks++;
                Assert.That(a.SolveCount,Is.LessThanOrEqualTo(200));
            }
            first/=64;third/=64;late/=64;normalLate/=64;
            TestContext.WriteLine("Easy mountain calibration: first="+first+"m third="+third+"m late="+late+"m versus Normal late="+normalLate+"m fallback="+fallbacks);
            Assert.That(ai.settings.minimumNoiseScale,Is.GreaterThanOrEqualTo(.75f));
            Assert.That(late,Is.GreaterThan(normalLate+2));
            Assert.That(fallbacks,Is.Zero,"This opponent is reachable.");
            yield return null;
        }
        static void Capture(string filename)
        {
            var camera=Camera.main;var canvas=Object.FindAnyObjectByType<Canvas>();
            var target=new RenderTexture(1280,720,24);var pixels=new Texture2D(1280,720,TextureFormat.RGB24,false);
            var oldMode=canvas.renderMode;var oldCamera=canvas.worldCamera;var previous=RenderTexture.active;
            float aspect=camera.aspect,distance=canvas.planeDistance;
            try
            {
                target.Create();camera.aspect=1280f/720;
                canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;
                Canvas.ForceUpdateCanvases();
                UnityEngine.Rendering.RenderPipeline.SubmitRenderRequest(camera,new UnityEngine.Rendering.RenderPipeline.StandardRequest{destination=target});
                RenderTexture.active=target;pixels.ReadPixels(new Rect(0,0,1280,720),0,0);pixels.Apply();
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
