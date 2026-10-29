using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Fortress
{
    /// <summary>Browser lifecycle messages use the existing pause flow; returning never resumes play.</summary>
    public sealed class WebSessionBridge : MonoBehaviour
    {
        SessionMenu menu;
        bool pauseRequested;
        Coroutine deferredPause;

#if UNITY_WEBGL && !UNITY_EDITOR
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Install()
        {
            WebGLInput.captureAllKeyboardInput=false;
            var root=new GameObject("Web session");
            DontDestroyOnLoad(root);
            root.AddComponent<WebSessionBridge>();
        }
        void OnApplicationFocus(bool focused) { if(!focused)PauseForBrowser(); }
        void OnApplicationPause(bool paused) { if(paused)PauseForBrowser(); }
#endif
        void OnEnable()
        {
            SceneManager.sceneLoaded+=SceneLoaded;
            CacheMenu();
        }
        void OnDisable()
        {
            SceneManager.sceneLoaded-=SceneLoaded;
            if(deferredPause!=null)StopCoroutine(deferredPause);
            deferredPause=null;menu=null;pauseRequested=false;
        }
        void CacheMenu() { menu=FindFirstObjectByType<SessionMenu>(); }
        void SceneLoaded(Scene scene,LoadSceneMode mode)
        {
            CacheMenu();
#if UNITY_WEBGL && !UNITY_EDITOR
            if(!Application.isFocused)pauseRequested=true;
#endif
            if(pauseRequested)DeferPause();
        }
        // Called by the Web template on blur, hidden visibility, and leaving fullscreen.
        public void PauseForBrowser()
        {
            pauseRequested=true;
            if(menu==null)CacheMenu();
            if(menu==null || menu.IsLoading)return;
            if(menu.IsMainMenu) { pauseRequested=false;return; }
            if(menu.match.Tanks==null) { DeferPause();return; }
            menu.PauseFromBrowser();pauseRequested=false;
        }
        void DeferPause()
        {
            if(deferredPause==null)deferredPause=StartCoroutine(PauseAfterSceneStart());
        }
        IEnumerator PauseAfterSceneStart()
        {
            yield return null;
            deferredPause=null;
            if(menu!=null && !menu.IsLoading && (menu.IsMainMenu || menu.match.Tanks!=null))PauseForBrowser();
        }
    }
}
