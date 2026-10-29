#if UNITY_EDITOR
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
    public sealed class WebSessionTests
    {
        SessionMenu menu;
        WebSessionBridge bridge;
        [UnitySetUp] public IEnumerator Load()
        {
            Time.timeScale=1;AudioListener.pause=false;
            EditorSceneManager.LoadSceneInPlayMode(SessionMenu.AiScene,new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;yield return null;
            menu=Object.FindFirstObjectByType<SessionMenu>();
            var root=new GameObject("Web session");Object.DontDestroyOnLoad(root);bridge=root.AddComponent<WebSessionBridge>();
        }
        [UnityTearDown] public IEnumerator Unload()
        {
            if(bridge!=null)Object.Destroy(bridge.gameObject);
            menu=Object.FindFirstObjectByType<SessionMenu>();
            if(menu!=null) { menu.Resume();if(menu.match!=null)menu.match.shots.CancelShot(); }
            Time.timeScale=1;AudioListener.pause=false;
            var old=SceneManager.GetActiveScene();SceneManager.SetActiveScene(SceneManager.CreateScene("WebSessionCleanup"));
            yield return SceneManager.UnloadSceneAsync(old);
        }
        [UnityTest] public IEnumerator BrowserPause_DuplicateSignalsPreserveShotAndResumeState()
        {
            var shots=menu.match.shots;shots.SetAim(0,85,30);Assert.That(shots.TryFire(),Is.True);
            var runner=shots.ActiveRunner;float time=runner.LocalTime;Time.timeScale=.5f;
            bridge.PauseForBrowser();bridge.PauseForBrowser();
            Assert.That(menu.IsPaused,Is.True);Assert.That(Time.timeScale,Is.Zero);Assert.That(AudioListener.pause,Is.True);
            yield return new WaitForSecondsRealtime(.1f);
            Assert.That(runner.LocalTime,Is.EqualTo(time));Assert.That(shots.ActiveRunner,Is.SameAs(runner));
            Assert.That(menu.IsPaused,Is.True,"Returning frames must not automatically resume.");
            menu.Resume();Assert.That(Time.timeScale,Is.EqualTo(.5f));Assert.That(AudioListener.pause,Is.False);
            yield return null;yield return null;Assert.That(runner.LocalTime,Is.GreaterThan(time));
        }
        [UnityTest] public IEnumerator BrowserPause_AfterRestartReassertsPauseWithoutLosingResumeScale()
        {
            Time.timeScale=.75f;bridge.PauseForBrowser();menu.match.RestartMatch(55);
            bridge.PauseForBrowser();Assert.That(Time.timeScale,Is.Zero);Assert.That(menu.match.shots.GameplaySuspended,Is.True);
            Assert.That(menu.match.shots.TryFire(),Is.False);menu.Resume();Assert.That(Time.timeScale,Is.EqualTo(.75f));
            yield return null;
        }
        [UnityTest] public IEnumerator BrowserPause_FullscreenExitEscapeCannotResume_ButLaterEscapeCan()
        {
            var focus=InputSystem.settings.backgroundBehavior;var editor=InputSystem.settings.editorInputBehaviorInPlayMode;
            InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            var keyboard=InputSystem.AddDevice<Keyboard>();
            try
            {
                bridge.PauseForBrowser();
                InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.Escape));yield return null;yield return null;
                Assert.That(menu.IsPaused,Is.True,"Esc queued by fullscreen exit must not undo the browser pause.");
                Assert.That(Time.timeScale,Is.Zero);
                InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return null;
                InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.Escape));yield return null;yield return null;
                Assert.That(menu.IsPaused,Is.False,"A later intentional Esc must still resume.");
                Assert.That(Time.timeScale,Is.EqualTo(1));
            }
            finally { InputSystem.RemoveDevice(keyboard);InputSystem.settings.backgroundBehavior=focus;InputSystem.settings.editorInputBehaviorInPlayMode=editor; }
        }
        [UnityTest] public IEnumerator BrowserPause_DuringNavigationUsesNewSceneAndDoesNotFreezeMainMenu()
        {
            menu.RequestMainMenu();menu.ConfirmExit();bridge.PauseForBrowser();
            yield return WaitForScene(SessionMenu.MainScene);
            menu=Object.FindFirstObjectByType<SessionMenu>();Assert.That(menu.IsMainMenu,Is.True);
            Assert.That(Time.timeScale,Is.EqualTo(1));bridge.PauseForBrowser();Assert.That(Time.timeScale,Is.EqualTo(1));
            menu.StartLocal();bridge.PauseForBrowser();yield return WaitForScene(SessionMenu.LocalScene);
            menu=Object.FindFirstObjectByType<SessionMenu>();
            Assert.That(menu.IsPaused,Is.True);Assert.That(Time.timeScale,Is.Zero);Assert.That(menu.match.shots.GameplaySuspended,Is.True);
            menu.Resume();Assert.That(Time.timeScale,Is.EqualTo(1));
        }
        static IEnumerator WaitForScene(string path)
        {
            float deadline=Time.realtimeSinceStartup+15;
            while(SceneManager.GetActiveScene().path!=path && Time.realtimeSinceStartup<deadline)yield return null;
            Assert.That(SceneManager.GetActiveScene().path,Is.EqualTo(path));yield return null;yield return null;
        }
    }
}
#endif
