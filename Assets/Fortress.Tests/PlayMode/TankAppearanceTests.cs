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
    public sealed class TankAppearanceTests
    {
        FlatShotController shots;
        [UnitySetUp] public IEnumerator Load()
        {
            BattleLoadoutSelection.Player=1;BattleLoadoutSelection.Opponent=2;
            EditorSceneManager.LoadSceneInPlayMode(SessionMenu.LocalScene,new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;yield return null;shots=Object.FindFirstObjectByType<FlatShotController>();
        }
        [UnityTearDown] public IEnumerator Unload()
        {
            if(shots!=null)shots.CancelShot();Time.timeScale=1;AudioListener.pause=false;
            BattleLoadoutSelection.Player=BattleLoadoutSelection.Opponent=0;
            var old=SceneManager.GetActiveScene();SceneManager.SetActiveScene(SceneManager.CreateScene("AppearanceCleanup"));yield return SceneManager.UnloadSceneAsync(old);
        }
        [UnityTest] public IEnumerator SelectedModels_UseCorrectMountsAndSideMaterials_RestartDoesNotDuplicate()
        {
            foreach(var view in Object.FindObjectsByType<TankAppearanceView>(FindObjectsSortMode.None))
            {
                var appearance=shots.GetLoadout(view.tankIndex).appearance;Assert.That(view.CurrentAppearance,Is.SameAs(appearance));
                var renderers=view.GetComponentsInChildren<MeshRenderer>().Where(r=>r.enabled).ToArray();Assert.That(renderers.Length,Is.EqualTo(appearance.pieces.Length));
                foreach(var piece in appearance.pieces)
                {
                    var renderer=renderers.Single(r=>r.GetComponent<MeshFilter>().sharedMesh==piece.mesh);
                    Assert.That(renderer.transform.parent,Is.SameAs(piece.mount==0?view.transform:piece.mount==1?view.rig.turret:view.gunParent));
                    CollectionAssert.AreEqual(view.tankIndex==0?piece.playerMaterials:piece.opponentMaterials,renderer.sharedMaterials);
                    Assert.That(renderer.GetComponent<Collider>(),Is.Null);
                }
                var muzzle=view.rig.muzzle.localPosition;var colliderCount=view.GetComponentsInChildren<Collider>().Length;
                view.enabled=false;yield return null;view.enabled=true;yield return null;
                Assert.That(view.GetComponentsInChildren<MeshRenderer>().Count(r=>r.enabled),Is.EqualTo(appearance.pieces.Length));
                Assert.That(view.rig.muzzle.localPosition,Is.EqualTo(muzzle));Assert.That(view.GetComponentsInChildren<Collider>().Length,Is.EqualTo(colliderCount));
            }
            var match=Object.FindFirstObjectByType<MatchController>();match.RestartMatch(match.Seed);yield return null;
            foreach(var view in Object.FindObjectsByType<TankAppearanceView>(FindObjectsSortMode.None))
                Assert.That(view.GetComponentsInChildren<MeshRenderer>().Count(r=>r.enabled),Is.EqualTo(view.CurrentAppearance.pieces.Length));
            Assert.That(shots.GetLoadout(0).appearance.sourceModel,Is.Not.EqualTo(shots.GetLoadout(1).appearance.sourceModel));
        }
        [UnityTest] public IEnumerator NewCannon_FollowsAimAndRecoilWithoutMovingMuzzle()
        {
            var rig=shots.tanks[0];var view=rig.GetComponent<TankAppearanceView>();
            var gun=view.gunParent.GetComponentsInChildren<MeshRenderer>().Single(r=>r.enabled);
            shots.SetWind(Vector3.zero);shots.SetAim(35,65,22);
            var muzzle=rig.muzzle.localPosition;var rest=view.gunParent.localPosition;
            Assert.That(gun.transform.IsChildOf(rig.barrel),Is.True);
            Assert.That(shots.TryFire(),Is.True);yield return null;yield return new WaitForEndOfFrame();
            Assert.That(view.gunParent.localPosition.z,Is.LessThan(rest.z));Assert.That(rig.muzzle.localPosition,Is.EqualTo(muzzle));
            shots.CancelShot();Assert.That(view.gunParent.localPosition,Is.EqualTo(rest));
        }
    }
}
#endif
