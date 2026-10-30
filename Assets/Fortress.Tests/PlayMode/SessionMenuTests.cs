#if UNITY_EDITOR
using System.Collections;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

namespace Fortress.Tests
{
    public sealed class SessionMenuTests
    {
        SessionMenu menu;
        [UnitySetUp] public IEnumerator Load()
        {
            Time.timeScale=1;AudioListener.pause=false;
            EditorSceneManager.LoadSceneInPlayMode(SessionMenu.AiScene,new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;yield return null;
            menu=Object.FindFirstObjectByType<SessionMenu>();Assert.That(menu,Is.Not.Null);
        }
        [UnityTearDown] public IEnumerator Unload()
        {
            if(menu!=null) menu.Resume();Time.timeScale=1;AudioListener.pause=false;
            var scene=SceneManager.GetActiveScene();SceneManager.SetActiveScene(SceneManager.CreateScene("MenuCleanup"));
            yield return SceneManager.UnloadSceneAsync(scene);
        }
        [UnityTest] public IEnumerator Pause_BlocksAimMovementCameraAndRestoresExactScale()
        {
            var shots=menu.match.shots;var camera=Object.FindFirstObjectByType<BattlefieldOrbitCamera>();
            float yaw=camera.Yaw,scale=.45f;Time.timeScale=scale;AudioListener.pause=true;
            menu.Pause();Assert.That(Time.timeScale,Is.Zero);
            Assert.That(shots.SetAim(10,40,20),Is.False);Assert.That(shots.TryFire(),Is.False);
            Assert.That(menu.match.TryMoveActiveTank(Vector2.right,1),Is.False);
            camera.Orbit(new Vector2(100,0));Assert.That(camera.Yaw,Is.EqualTo(yaw));
            menu.RequestMainMenu();Assert.That(menu.IsConfirming,Is.True);
            Assert.That(menu.confirmationTitle.text,Is.EqualTo("대전에서 나갈까요?"));
            Assert.That(menu.confirmationBody.text,Does.Contain("저장되지 않습니다"));
            menu.CancelConfirmation();Assert.That(menu.IsPaused,Is.True);Assert.That(Time.timeScale,Is.Zero);
            Assert.That(menu.combatHudGroup.interactable,Is.False);
            menu.Resume();Assert.That(Time.timeScale,Is.EqualTo(scale));Assert.That(AudioListener.pause,Is.True);
            Assert.That(shots.TryFire(),Is.False,"Resume-frame Space cannot also fire.");
            yield return null;
            Assert.That(shots.GameplaySuspended,Is.False);Assert.That(menu.combatHudGroup.interactable,Is.True);
        }
        [UnityTest] public IEnumerator Pause_InFlightPreservesRunnerAndFrozenTerrainThenResumes()
        {
            var shots=menu.match.shots;shots.SetAim(0,85,30);Assert.That(shots.TryFire(),Is.True);
            var runner=shots.ActiveRunner;float time=runner.LocalTime;var position=runner.Position;
            menu.Pause();yield return new WaitForSecondsRealtime(.15f);
            Assert.That(shots.ActiveRunner,Is.SameAs(runner));Assert.That(runner.LocalTime,Is.EqualTo(time));
            Assert.That(runner.Position,Is.EqualTo(position));Assert.That(menu.match.terrainMap.Field.IsFrozen,Is.True);
            menu.Resume();yield return null;yield return null;
            Assert.That(runner.LocalTime,Is.GreaterThan(time));
        }
        [UnityTest] public IEnumerator Pause_AiKeepsPendingPlanWithoutDispatching()
        {
            var shots=menu.match.shots;var ai=Object.FindFirstObjectByType<AiOpponent>();
            shots.SetAim(0,85,30);Assert.That(shots.TryFire(),Is.True);shots.ActiveRunner.Tick(shots.tuning.maxFlightTime+1);
            Assert.That(ai.IsThinking,Is.True);float remaining=ai.RemainingThinkSeconds;
            menu.Pause();ai.TickThinking(100);yield return null;
            Assert.That(ai.IsThinking,Is.True);Assert.That(ai.RemainingThinkSeconds,Is.EqualTo(remaining));Assert.That(shots.IsFiring,Is.False);
            menu.Resume();yield return null;
            ai.TickThinking(100);Assert.That(shots.IsFiring,Is.False,"The resumed shot camera still owns the view.");
            var shotCamera=Object.FindAnyObjectByType<ProjectileFollowCamera>();
            shotCamera.TickPresentation(10);shotCamera.TickPresentation(10);
            ai.TickThinking(100);
            for(int i=0;i<40 && ai.IsMoving;i++)ai.TickMovement(.1f);
            if(!shots.IsFiring)ai.TickThinking(100);
            Assert.That(shots.IsFiring,Is.True);
        }
        [UnityTest] public IEnumerator MainMenu_RoundTripBothModesAndCancelsLiveShot()
        {
            var shots=menu.match.shots;shots.SetAim(0,85,30);Assert.That(shots.TryFire(),Is.True);
            var field=menu.match.terrainMap.Field;
            menu.RequestMainMenu();menu.ConfirmExit();
            Assert.That(menu.IsLoading,Is.True);Assert.That(shots.IsFiring,Is.False);Assert.That(field.IsFrozen,Is.False);
            yield return WaitForScene(SessionMenu.MainScene);
            menu=Object.FindFirstObjectByType<SessionMenu>();Assert.That(menu.IsMainMenu,Is.True);
            Assert.That(Time.timeScale,Is.EqualTo(1));Assert.That(AudioListener.pause,Is.False);
            Assert.That(Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None).Length,Is.EqualTo(1));
            menu.OpenControls();menu.CloseControls();menu.RequestQuit();
            Assert.That(menu.confirmationTitle.text,Is.EqualTo("게임을 종료할까요?"));
            Assert.That(menu.confirmationBody.text,Is.EqualTo("제5병기창을 종료합니다."));
            menu.CancelConfirmation();Assert.That(menu.IsLoading,Is.False);
            menu.StartLocal();menu.StartAi(); // Duplicate click must keep the first destination.
            yield return WaitForScene(SessionMenu.LocalScene);
            menu=Object.FindFirstObjectByType<SessionMenu>();Assert.That(Object.FindFirstObjectByType<AiOpponent>(),Is.Null);
            menu.RequestMainMenu();menu.ConfirmExit();yield return WaitForScene(SessionMenu.MainScene);
            menu=Object.FindFirstObjectByType<SessionMenu>();menu.StartAi();yield return WaitForScene(SessionMenu.AiScene);
            menu=Object.FindFirstObjectByType<SessionMenu>();Assert.That(Object.FindFirstObjectByType<AiOpponent>(),Is.Not.Null);
        }
        [UnityTest] public IEnumerator Pause_LateTurnEventDefersAiPlanningUntilResume()
        {
            var shots=menu.match.shots;var ai=Object.FindFirstObjectByType<AiOpponent>();
            shots.SetAim(0,85,30);Assert.That(shots.TryFire(),Is.True);menu.Pause();
            // Simulate a finish callback already queued during the frame pause was requested.
            shots.ActiveRunner.Tick(shots.tuning.maxFlightTime+1);
            Assert.That(menu.match.ActivePlayerIndex,Is.EqualTo(1));Assert.That(ai.IsThinking,Is.False);
            menu.Resume();yield return null;yield return null;
            Assert.That(ai.IsThinking,Is.True);Assert.That(shots.IsFiring,Is.False);
        }
        [UnityTest] public IEnumerator Escape_OpensPauseCancelsConfirmationThenResumes()
        {
            var focus=InputSystem.settings.backgroundBehavior;var editor=InputSystem.settings.editorInputBehaviorInPlayMode;
            InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            var keyboard=InputSystem.AddDevice<Keyboard>();
            try
            {
                yield return PressEscape(keyboard);Assert.That(menu.IsPaused,Is.True);
                menu.RequestQuit();yield return PressEscape(keyboard);Assert.That(menu.IsConfirming,Is.False);Assert.That(menu.IsPaused,Is.True);
                yield return PressEscape(keyboard);Assert.That(menu.IsPaused,Is.False);
            }
            finally { InputSystem.RemoveDevice(keyboard);InputSystem.settings.backgroundBehavior=focus;InputSystem.settings.editorInputBehaviorInPlayMode=editor; }
        }
        static IEnumerator PressEscape(Keyboard keyboard)
        {
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.Escape));yield return null;yield return null;
            InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return null;
        }
        static IEnumerator WaitForScene(string path)
        {
            float end=Time.realtimeSinceStartup+15;
            while(SceneManager.GetActiveScene().path!=path && Time.realtimeSinceStartup<end) yield return null;
            Assert.That(SceneManager.GetActiveScene().path,Is.EqualTo(path));yield return null;yield return null;
        }
    }
}
#endif
