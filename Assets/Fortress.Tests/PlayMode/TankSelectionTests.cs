#if UNITY_EDITOR
using System.Collections;
using System.Linq;
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
            Assert.That(picker.playerCards.Length,Is.EqualTo(5));
            for(int i=0;i<picker.roles.Length;i++)
            {
                picker.playerCards[i].onClick.Invoke();
                Assert.That(BattleLoadoutSelection.Player,Is.EqualTo(i));
                var role=picker.roles[i];Assert.That(role.appearance,Is.Not.Null);
                Assert.That(role.appearance.portrait,Is.Not.Null);
                Assert.That(picker.selectedPortrait.sprite,Is.SameAs(role.appearance.portrait));
                Assert.That(picker.playerLabel.text,Is.EqualTo(KoreanUi.RoleName(role)));
                Assert.That(picker.roleDescription.text,Is.EqualTo(KoreanUi.RoleDescription(role)));
                Assert.That(picker.ammunitionLabel.text,Is.EqualTo(LoadoutMenu.AmmoDescription(role)));
                for(int j=0;j<picker.roles.Length;j++)
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
            picker.SelectPlayer(-1);picker.SelectPlayer(picker.roles.Length);picker.NextOpponent();
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
            Assert.That(picker.opponentLabel.text,Does.Contain(KoreanUi.RoleName(opponent)));
        }
        [UnityTest] public IEnumerator HeavySelection_DeploysAiWithMainBattleAmmoAndHeavyMounts()
        { yield return DeployHeavy(true); }
        [UnityTest] public IEnumerator HeavySelection_DeploysLocalWithMainBattleAmmoAndHeavyMounts()
        { yield return DeployHeavy(false); }
        IEnumerator DeployHeavy(bool againstAi)
        {
            int index=System.Array.FindIndex(picker.roles,role=>role!=null && role.displayName=="HEAVY");
            Assert.That(index,Is.EqualTo(4));
            picker.playerCards[index].onClick.Invoke();
            var heavy=picker.roles[index];var standard=picker.roles[0];
            Assert.That(picker.playerLabel.text,Is.EqualTo("중장갑 전차"));
            Assert.That(picker.roleDescription.text,Does.Contain("성능은 동일"));
            Assert.That(heavy.basic,Is.SameAs(standard.basic));Assert.That(heavy.special,Is.SameAs(standard.special));
            Assert.That(heavy.specialRounds,Is.EqualTo(standard.specialRounds));
            Assert.That(heavy.appearance,Is.Not.Null);Assert.That(heavy.appearance,Is.Not.SameAs(standard.appearance));
            Assert.That(heavy.appearance.portrait,Is.Not.Null);
            Assert.That(heavy.appearance.portrait,Is.Not.SameAs(standard.appearance.portrait));
            var menu=Object.FindFirstObjectByType<SessionMenu>();
            if(againstAi)menu.StartAi();else menu.StartLocal();
            yield return WaitForScene(againstAi?SessionMenu.AiScene:SessionMenu.LocalScene);
            var shots=Object.FindFirstObjectByType<FlatShotController>();
            Assert.That(shots.ActiveLoadout,Is.SameAs(heavy));Assert.That(shots.SpecialSelected,Is.False);
            Assert.That(shots.SelectAmmo(true),Is.False,"Heavy appearance must not grant a new ammunition type.");
            var view=shots.tanks[0].GetComponent<TankAppearanceView>();
            Assert.That(view.CurrentAppearance,Is.SameAs(heavy.appearance));
            var renderers=view.GetComponentsInChildren<MeshRenderer>().Where(r=>r.enabled).ToArray();
            Assert.That(renderers.Length,Is.EqualTo(heavy.appearance.pieces.Length));
            foreach(var piece in heavy.appearance.pieces)
            {
                var renderer=renderers.Single(r=>r.GetComponent<MeshFilter>().sharedMesh==piece.mesh);
                Assert.That(renderer.transform.parent,Is.SameAs(piece.mount==0?view.transform:piece.mount==1?view.rig.turret:view.gunParent));
                CollectionAssert.AreEqual(piece.playerMaterials,renderer.sharedMaterials);
                Assert.That(renderer.GetComponent<Collider>(),Is.Null,"Cosmetic armor cannot add combat colliders.");
            }
            var muzzle=view.rig.muzzle.localPosition;
            Assert.That(shots.SetAim(30,55,25),Is.True);
            Assert.That(view.rig.muzzle.localPosition,Is.EqualTo(muzzle));
            var match=Object.FindFirstObjectByType<MatchController>();
            Assert.That(match.Tanks[0].Health,Is.EqualTo(match.Tanks[1].Health));
            var rest=view.gunParent.localPosition;
            Assert.That(shots.TryFire(),Is.True);yield return null;yield return new WaitForEndOfFrame();
            Assert.That(view.gunParent.localPosition.z,Is.LessThan(rest.z));
            Assert.That(view.rig.muzzle.localPosition,Is.EqualTo(muzzle));
            shots.CancelShot();Assert.That(view.gunParent.localPosition,Is.EqualTo(rest));
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
