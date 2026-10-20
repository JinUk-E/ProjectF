#if UNITY_EDITOR
using System.Collections;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

namespace Fortress.Tests
{
    public sealed class TankSelectionTests
    {
        LoadoutMenu picker;
        [UnitySetUp] public IEnumerator Load()
        {
            BattleLoadoutSelection.Player=BattleLoadoutSelection.Opponent=0;
            Time.timeScale=1;AudioListener.pause=false;
            EditorSceneManager.LoadSceneInPlayMode(SessionMenu.MainScene,new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;yield return null;
            picker=Object.FindFirstObjectByType<LoadoutMenu>();
            Assert.That(picker,Is.Not.Null);
        }
        [UnityTearDown] public IEnumerator Unload()
        {
            var shots=Object.FindFirstObjectByType<FlatShotController>();if(shots!=null) shots.CancelShot();
            Time.timeScale=1;AudioListener.pause=false;
            BattleLoadoutSelection.Player=BattleLoadoutSelection.Opponent=0;
            var old=SceneManager.GetActiveScene();SceneManager.SetActiveScene(SceneManager.CreateScene("TankSelectionCleanup"));
            yield return SceneManager.UnloadSceneAsync(old);
        }
        [UnityTest] public IEnumerator EveryCard_SelectsItsOwnRolePortraitAndExclusiveMarker()
        {
            Assert.That(picker.playerCards.Length,Is.EqualTo(4));
            for(int i=0;i<4;i++)
            {
                picker.playerCards[i].onClick.Invoke();
                Assert.That(BattleLoadoutSelection.Player,Is.EqualTo(i));
                var role=picker.roles[i];Assert.That(role.appearance,Is.Not.Null);
                Assert.That(role.appearance.portrait,Is.Not.Null);
                Assert.That(picker.selectedPortrait.sprite,Is.SameAs(role.appearance.portrait));
                Assert.That(picker.playerLabel.text,Is.EqualTo(role.displayName));
                Assert.That(picker.roleDescription.text,Is.EqualTo(role.appearance.description));
                Assert.That(picker.ammunitionLabel.text,Is.EqualTo(LoadoutMenu.AmmoDescription(role)));
                for(int j=0;j<4;j++)
                {
                    Assert.That(picker.selectionMarkers[j].activeSelf,Is.EqualTo(i==j));
                    Assert.That(picker.cardBorders[j].color,Is.EqualTo(i==j?picker.selectedColor:picker.idleColor));
                }
            }
            yield return null;
        }
        [UnityTest] public IEnumerator Selection_InvalidIndicesIgnoredAndOpponentDoesNotChangePlayer()
        {
            picker.SelectPlayer(2);var portrait=picker.selectedPortrait.sprite;
            picker.SelectPlayer(-1);picker.SelectPlayer(4);picker.NextOpponent();
            Assert.That(BattleLoadoutSelection.Player,Is.EqualTo(2));
            Assert.That(BattleLoadoutSelection.Opponent,Is.EqualTo(1));
            Assert.That(picker.selectedPortrait.sprite,Is.SameAs(portrait));
            picker.gameObject.SetActive(false);picker.gameObject.SetActive(true);
            Assert.That(picker.selectionMarkers[2].activeSelf,Is.True);
            yield return null;
        }
        [UnityTest] public IEnumerator DirectSelection_DeploysIntoAiMatch()
        {
            picker.playerCards[3].onClick.Invoke();picker.NextOpponent();picker.NextOpponent();
            var role=picker.roles[3];var opponent=picker.roles[2];
            Object.FindFirstObjectByType<SessionMenu>().StartAi();
            yield return WaitForScene(SessionMenu.AiScene);
            var shots=Object.FindFirstObjectByType<FlatShotController>();
            Assert.That(shots.ActiveLoadout,Is.SameAs(role));Assert.That(shots.SpecialRemaining,Is.EqualTo(2));
            Assert.That(Object.FindFirstObjectByType<AiOpponent>(),Is.Not.Null);
            Assert.That(shots.SetActiveTank(1),Is.True);Assert.That(shots.ActiveLoadout,Is.SameAs(opponent));
        }
        [UnityTest] public IEnumerator DirectSelection_DeploysLocalAndSurvivesReturnToMenu()
        {
            picker.playerCards[1].onClick.Invoke();picker.NextOpponent();picker.NextOpponent();picker.NextOpponent();
            var role=picker.roles[1];var opponent=picker.roles[3];
            Object.FindFirstObjectByType<SessionMenu>().StartLocal();
            yield return WaitForScene(SessionMenu.LocalScene);
            var shots=Object.FindFirstObjectByType<FlatShotController>();
            Assert.That(shots.ActiveLoadout,Is.SameAs(role));Assert.That(shots.SetActiveTank(1),Is.True);
            Assert.That(shots.ActiveLoadout,Is.SameAs(opponent));
            var menu=Object.FindFirstObjectByType<SessionMenu>();menu.RequestMainMenu();menu.ConfirmExit();
            yield return WaitForScene(SessionMenu.MainScene);
            picker=Object.FindFirstObjectByType<LoadoutMenu>();
            Assert.That(picker.selectionMarkers[1].activeSelf,Is.True);
            Assert.That(picker.selectedPortrait.sprite,Is.SameAs(role.appearance.portrait));
            Assert.That(picker.opponentLabel.text,Does.Contain(opponent.displayName));
        }
        static IEnumerator WaitForScene(string path)
        {
            float until=Time.realtimeSinceStartup+15;
            while(SceneManager.GetActiveScene().path!=path && Time.realtimeSinceStartup<until) yield return null;
            Assert.That(SceneManager.GetActiveScene().path,Is.EqualTo(path));yield return null;yield return null;
        }
    }
}
#endif
