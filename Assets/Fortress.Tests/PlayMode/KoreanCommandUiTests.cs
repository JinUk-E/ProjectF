#if UNITY_EDITOR
using System.Collections;
using NUnit.Framework;
using TMPro;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

namespace Fortress.Tests
{
    public sealed class KoreanCommandUiTests
    {
        [UnitySetUp] public IEnumerator Setup()
        {
            Time.timeScale=1;AudioListener.pause=false;
            BattleLoadoutSelection.Player=BattleLoadoutSelection.Opponent=0;
            yield return Load(SessionMenu.MainScene);
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            var shots=Object.FindFirstObjectByType<FlatShotController>();if(shots!=null)shots.CancelShot();
            var menu=Object.FindFirstObjectByType<SessionMenu>();if(menu!=null)menu.Resume();
            Time.timeScale=1;AudioListener.pause=false;
            BattleLoadoutSelection.Player=BattleLoadoutSelection.Opponent=0;
            var old=SceneManager.GetActiveScene();SceneManager.SetActiveScene(SceneManager.CreateScene("KoreanCommandCleanup"));
            yield return SceneManager.UnloadSceneAsync(old);
        }
        [UnityTest] public IEnumerator Main_AllCardsSwitchSingleShowroomModelAndReadableKoreanBriefing()
        {
            var picker=Object.FindFirstObjectByType<LoadoutMenu>();
            var preview=Object.FindFirstObjectByType<HangarTankPreview>();
            Assert.That(preview,Is.Not.Null);Assert.That(preview.models.Length,Is.EqualTo(picker.roles.Length));
            for(int i=0;i<picker.roles.Length;i++)
            {
                picker.playerCards[i].onClick.Invoke();yield return null;
                for(int j=0;j<picker.roles.Length;j++)
                {
                    Assert.That(preview.models[j],Is.Not.Null);
                    Assert.That(preview.models[j].activeSelf,Is.EqualTo(i==j),"Showroom model "+j);
                }
                Assert.That(picker.playerLabel.text,Is.EqualTo(KoreanUi.RoleName(picker.roles[i])));
                Readable(picker.playerLabel);Readable(picker.roleDescription);Readable(picker.ammunitionLabel);
                picker.NextOpponent();Readable(picker.opponentLabel);
            }
            AssertKoreanGlyphs(Object.FindFirstObjectByType<SessionMenu>().gameObject);
            yield return new WaitForEndOfFrame();
            System.IO.Directory.CreateDirectory("Docs/Validation");
            ScreenCapture.CaptureScreenshot("Docs/Validation/combat-tank-menu.png");
            yield return null;
        }
        [UnityTest] public IEnumerator Main_ManualAndConfirmationUseAvailableKoreanGlyphs()
        {
            var menu=Object.FindFirstObjectByType<SessionMenu>();
            menu.OpenControls();yield return null;
            Assert.That(menu.controlsPanel.activeSelf,Is.True);
            AssertKoreanGlyphs(menu.controlsPanel);
            foreach(var label in menu.controlsPanel.GetComponentsInChildren<TMP_Text>())Readable(label);
            menu.CloseControls();menu.RequestQuit();yield return null;
            Readable(menu.confirmationTitle);Readable(menu.confirmationBody);
            Assert.That(menu.confirmationTitle.text,Does.Contain("종료"));
            AssertKoreanGlyphs(menu.confirmationPanel);menu.CancelConfirmation();
        }
        [UnityTest] public IEnumerator Battle_ButtonClicksAdjustAimPowerAndFireOnce()
        {
            BattleLoadoutSelection.Player=3;yield return Load(SessionMenu.LocalScene);
            var controls=Object.FindFirstObjectByType<CombatPanelControls>();Assert.That(controls,Is.Not.Null);
            var shots=controls.shots;Assert.That(shots.SetAim(0,45,25),Is.True);
            float pitch=shots.ActiveTank.Pitch,power=shots.Power;
            Assert.That(controls.buttons.Length,Is.EqualTo(5));
            controls.buttons[1].onClick.Invoke();Assert.That(shots.ActiveTank.Pitch,Is.EqualTo(pitch+1));
            controls.buttons[0].onClick.Invoke();Assert.That(shots.ActiveTank.Pitch,Is.EqualTo(pitch));
            controls.buttons[3].onClick.Invoke();Assert.That(shots.Power,Is.EqualTo(power+1));
            controls.buttons[2].onClick.Invoke();Assert.That(shots.Power,Is.EqualTo(power));
            var hud=Object.FindFirstObjectByType<MatchHud>();
            var ammo=Object.FindFirstObjectByType<AmmoHud>();
            Readable(hud.turnLabel);Readable(hud.windLabel);Readable(hud.windDetailLabel);
            Readable(hud.movementLabel);Readable(hud.stateLabel);
            Readable(ammo.basicLabel);Readable(ammo.specialLabel);
            AssertKoreanGlyphs(hud.gameObject);
            int changes=0;
            System.Action changed=()=>changes++;
            shots.StateChanged+=changed;
            ammo.specialButton.onClick.Invoke();
            Assert.That(changes,Is.EqualTo(1),"One click must issue one selection command.");
            Assert.That(shots.SpecialSelected,Is.True);
            ammo.basicButton.onClick.Invoke();
            Assert.That(changes,Is.EqualTo(2));
            Assert.That(shots.SpecialSelected,Is.False);
            shots.StateChanged-=changed;
            controls.buttons[4].onClick.Invoke();Assert.That(shots.IsFiring,Is.True);
            var runner=shots.ActiveRunner;
            controls.buttons[4].onClick.Invoke();Assert.That(shots.ActiveRunner,Is.SameAs(runner));
            yield return null;
            foreach(var button in controls.buttons)Assert.That(button.interactable,Is.False);
        }
        [UnityTest] public IEnumerator Battle_PauseAndAiOwnershipBlockButtonAndDirectCommands()
        {
            yield return Load(SessionMenu.AiScene);
            var controls=Object.FindFirstObjectByType<CombatPanelControls>();
            var shots=controls.shots;var menu=Object.FindFirstObjectByType<SessionMenu>();
            shots.SetAim(0,85,30);
            menu.Pause();yield return null;
            AssertCommandsBlocked(controls);
            menu.Resume();yield return null;yield return null;
            controls.Fire();Assert.That(shots.IsFiring,Is.True);
            shots.ActiveRunner.Tick(shots.tuning.maxFlightTime+1);
            Assert.That(shots.ai.OwnsCurrentTurn,Is.True);
            // Freeze the AI timer without suspending gameplay: ownership must be the guard.
            Time.timeScale=0;yield return null;
            Assert.That(shots.GameplaySuspended,Is.False);
            AssertCommandsBlocked(controls);
            Assert.That(Object.FindFirstObjectByType<MatchHud>().stateLabel.text,Does.Contain("AI 조준"));
        }
        static void AssertCommandsBlocked(CombatPanelControls controls)
        {
            var shots=controls.shots;float pitch=shots.ActiveTank.Pitch,power=shots.Power;
            foreach(var button in controls.buttons){Assert.That(button.interactable,Is.False);button.onClick.Invoke();}
            controls.ElevationUp();controls.PowerUp();controls.Fire();
            Assert.That(shots.ActiveTank.Pitch,Is.EqualTo(pitch));Assert.That(shots.Power,Is.EqualTo(power));
            Assert.That(shots.IsFiring,Is.False);
        }
        static void Readable(TMP_Text label)
        {
            Assert.That(label,Is.Not.Null);
            Canvas.ForceUpdateCanvases();label.ForceMeshUpdate();
            Assert.That(label.isTextOverflowing,Is.False,label.name+": "+label.text);
        }
        static void AssertKoreanGlyphs(GameObject root)
        {
            int count=0;
            foreach(var label in root.GetComponentsInChildren<TMP_Text>())
                foreach(char c in label.text)
                    if(c>='\uac00' && c<='\ud7a3')
                    {
                        count++;
                        Assert.That(label.font.HasCharacter((int)c),Is.True,label.name+" missing "+c);
                    }
            Assert.That(count,Is.GreaterThan(0),"Expected Korean text in "+root.name);
        }
        static IEnumerator Load(string path)
        {
            EditorSceneManager.LoadSceneInPlayMode(path,new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;yield return null;
        }
    }
}
#endif
