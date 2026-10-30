using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Fortress
{
    /// <summary>Owns menu navigation and suspension, without changing combat rules or AI ownership.</summary>
    public sealed class SessionMenu : MonoBehaviour
    {
        public const string MainScene = "Assets/Fortress/Scenes/MainMenu.unity";
        public const string AiScene = "Assets/Fortress/Scenes/MilitaryAI.unity";
        public const string LocalScene = "Assets/Fortress/Scenes/MilitaryMatch.unity";
        public MatchController match;
        public GameObject pausePanel, confirmationPanel, controlsPanel;
        public TMP_Text confirmationTitle, confirmationBody, statusLabel;
        public Button primaryButton, resumeButton, cancelButton;
        public CanvasGroup navigationGroup;
        public CanvasGroup combatHudGroup;
        public bool IsPaused { get; private set; }
        public bool IsLoading { get; private set; }
        public bool IsConfirming => confirmationPanel != null && confirmationPanel.activeSelf;
        public bool IsMainMenu => match == null;
        enum Destination { None, Main, Quit }
        Destination pending;
        float previousScale = 1;
        bool previousAudioPause;
        bool previousHudInteractable;
        int ignoreEscapeThroughFrame=-1;
        Selectable[] menuSelectables;

        void Awake() { menuSelectables=GetComponentsInChildren<Selectable>(true); }
        void Start() { RefreshNavigation();Select(primaryButton); }
        void Update()
        {
            if(IsLoading || Time.frameCount<=ignoreEscapeThroughFrame || Keyboard.current == null || !Keyboard.current.escapeKey.wasPressedThisFrame) return;
            if(IsConfirming) CancelConfirmation();
            else if(controlsPanel != null && controlsPanel.activeSelf) ShowControls(false);
            else if(!IsMainMenu) { if(IsPaused) Resume(); else Pause(); }
        }
        public void PauseFromBrowser()
        {
            // Fullscreen exit may deliver its Esc key in this frame or the next input update.
            // Keep that event from immediately toggling the just-opened pause menu closed.
            ignoreEscapeThroughFrame=Mathf.Max(ignoreEscapeThroughFrame,Time.frameCount+1);
            Pause();
        }
        public void Pause()
        {
            if(IsMainMenu || IsLoading) return;
            // Browser blur/visibility/fullscreen events can arrive together. Preserve the
            // original resume state, but reassert suspension after a restart initialized time.
            if(IsPaused)
            {
                if(match.shots != null) match.shots.GameplaySuspended = true;
                Time.timeScale = 0;AudioListener.pause = true;
                return;
            }
            previousScale = Time.timeScale;
            previousAudioPause = AudioListener.pause;
            IsPaused = true;
            if(match.shots != null) match.shots.GameplaySuspended = true;
            Time.timeScale = 0;
            AudioListener.pause = true;
            if(combatHudGroup!=null) { previousHudInteractable=combatHudGroup.interactable;combatHudGroup.interactable=false; }
            if(pausePanel != null) pausePanel.SetActive(true);
            RefreshNavigation();
            Select(resumeButton);
        }
        public void Resume()
        {
            if(IsLoading) return;
            pending = Destination.None;
            if(confirmationPanel != null) confirmationPanel.SetActive(false);
            if(controlsPanel != null) controlsPanel.SetActive(false);
            if(pausePanel != null) pausePanel.SetActive(false);
            RestoreSuspension();
            RefreshNavigation();
            if(EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
        }
        void RestoreSuspension()
        {
            if(!IsPaused) return;
            IsPaused = false;
            if(match != null && match.shots != null) match.shots.GameplaySuspended = false;
            Time.timeScale = previousScale;
            AudioListener.pause = previousAudioPause;
            if(combatHudGroup!=null) combatHudGroup.interactable=previousHudInteractable;
        }
        void OnDisable() { RestoreSuspension(); }
        public void StartAi() { Load(AiScene); }
        public void StartLocal() { Load(LocalScene); }
        public void RequestMainMenu()
        {
            if(IsLoading || IsMainMenu) return;
            if(match.State == MatchState.Ended) { Load(MainScene);return; }
            Pause(); Confirm(Destination.Main, "대전에서 나갈까요?", "진행 중인 대전은 저장되지 않습니다.\n메인 화면으로 돌아갈까요?");
        }
        public void RequestQuit()
        {
            if(IsLoading) return;
            if(!IsMainMenu) Pause();
            Confirm(Destination.Quit, "게임을 종료할까요?", IsMainMenu ? "제5병기창을 종료합니다." : "진행 중인 대전은 저장되지 않습니다.\n게임을 종료할까요?");
        }
        void Confirm(Destination destination, string title, string body)
        {
            pending = destination;
            if(confirmationTitle != null) confirmationTitle.text = title;
            if(confirmationBody != null) confirmationBody.text = body;
            if(confirmationPanel != null) confirmationPanel.SetActive(true);
            RefreshNavigation();
            Select(cancelButton);
        }
        public void CancelConfirmation()
        {
            pending = Destination.None;
            if(confirmationPanel != null) confirmationPanel.SetActive(false);
            RefreshNavigation();
            Select(IsMainMenu ? primaryButton : resumeButton);
        }
        public void ConfirmExit()
        {
            if(IsLoading) return;
            var destination = pending;
            if(destination == Destination.Main) Load(MainScene);
            else if(destination == Destination.Quit)
            {
                IsLoading = true;
                PrepareDeparture();
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
            }
        }
        public void ShowControls(bool show)
        {
            if(IsLoading) return;
            if(show && !IsMainMenu) Pause();
            if(controlsPanel != null) controlsPanel.SetActive(show);
            RefreshNavigation();
            if(show && controlsPanel!=null) Select(controlsPanel.GetComponentInChildren<Button>());
            if(!show) Select(IsMainMenu ? primaryButton : resumeButton);
        }
        public void OpenControls() { ShowControls(true); }
        public void CloseControls() { ShowControls(false); }
        void RefreshNavigation()
        {
            Transform active = IsConfirming ? confirmationPanel.transform :
                controlsPanel!=null && controlsPanel.activeSelf ? controlsPanel.transform :
                IsPaused && pausePanel!=null ? pausePanel.transform : null;
            if(menuSelectables==null) return;
            foreach(var selectable in menuSelectables)
                if(selectable!=null) selectable.interactable=active==null || selectable.transform.IsChildOf(active);
        }
        void PrepareDeparture()
        {
            if(match != null && match.shots != null)
            {
                // Stop AI callbacks before cancelling the shot and unloading its scene.
                match.shots.GameplaySuspended = true;
                match.shots.CancelShot();
            }
            IsPaused = false;
            Time.timeScale = 1;
            AudioListener.pause = false;
        }
        void Load(string scene)
        {
            if(IsLoading) return;
            if(!Application.CanStreamedLevelBeLoaded(scene))
            {
                if(statusLabel != null) statusLabel.text = "대전 화면을 불러올 수 없습니다. 메인 화면으로 돌아가 주세요.";
                Debug.LogError("Menu destination is not registered in Build Settings: " + scene, this);
                return;
            }
            IsLoading = true;
            if(statusLabel != null) statusLabel.text = "이동 중...";
            if(navigationGroup != null) navigationGroup.interactable = false;
            PrepareDeparture();
            SceneManager.LoadSceneAsync(scene, LoadSceneMode.Single);
        }
        static void Select(Button button)
        {
            if(button != null && EventSystem.current != null) EventSystem.current.SetSelectedGameObject(button.gameObject);
        }
    }
}
