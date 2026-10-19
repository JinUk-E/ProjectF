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
    public sealed class BuildingCollapseTests
    {
        MatchController match;
        BattlefieldCover cover;
        [UnitySetUp] public IEnumerator Setup()
        {
            EditorSceneManager.LoadSceneInPlayMode("Assets/Fortress/Scenes/MilitaryAI.unity",new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;match=Object.FindFirstObjectByType<MatchController>();cover=match.cover;
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            if(match!=null)match.shots.CancelShot();
            var scene=SceneManager.GetActiveScene();SceneManager.SetActiveScene(SceneManager.CreateScene("Collapse cleanup"));
            yield return SceneManager.UnloadSceneAsync(scene);
        }
        Transform Root()
        {
            foreach(Transform child in cover.transform)if(child.name=="Battlefield buildings (runtime)" && child.gameObject.activeSelf)return child;
            Assert.Fail("No live building views");return null;
        }
        [UnityTest] public IEnumerator NonlethalDamage_ShowsStagesWithoutChangingCoverBounds()
        {
            var b=cover.Buildings[0];var bounds=b.Bounds;var intact=Root().GetChild(0);
            cover.ApplyExplosion(bounds.center,4,10);
            Assert.That(b.Health,Is.EqualTo(cover.settings.health-10));Assert.That(cover.DamageStage(0),Is.EqualTo(1));
            Assert.That(intact.Find("Impact scars").gameObject.activeSelf,Is.True);
            Assert.That(intact.Find("Severe concrete cracks").gameObject.activeSelf,Is.False);
            cover.ApplyExplosion(bounds.center,4,20);
            Assert.That(cover.DamageStage(0),Is.EqualTo(2));Assert.That(intact.Find("Severe concrete cracks").gameObject.activeSelf,Is.True);
            Assert.That(b.Bounds,Is.EqualTo(bounds));Assert.That(cover.IsCollapsing(0),Is.False);
            Assert.That(cover.TryHit(bounds.center+Vector3.forward*10,bounds.center,out _),Is.True);
            yield return null;
        }
        [UnityTest] public IEnumerator Collapse_StartMatchesPanelsThenFallsWithoutBlockingOrRestarting()
        {
            var root=Root();var intact=root.GetChild(0);var ruins=root.GetChild(1);
            Vector3 initial=intact.GetChild(0).position;Quaternion rotation=intact.GetChild(0).rotation;
            var bounds=cover.Buildings[0].Bounds;
            cover.ApplyExplosion(bounds.center,4,1000);
            Assert.That(cover.Buildings[0].Destroyed,Is.True);Assert.That(cover.IsCollapsing(0),Is.True);
            Assert.That(intact.gameObject.activeSelf,Is.False);Assert.That(ruins.gameObject.activeSelf,Is.True);
            Assert.That(ruins.childCount,Is.GreaterThan(30));
            Assert.That(ruins.GetChild(0).position,Is.EqualTo(initial));Assert.That(ruins.GetChild(0).rotation,Is.EqualTo(rotation));
            Assert.That(cover.BlocksMovement(bounds.center,bounds.center,1),Is.False);
            cover.TickPresentation(.35f);float progress=cover.CollapseProgress(0);
            Assert.That(progress,Is.GreaterThan(0));Assert.That(ruins.GetChild(0).position,Is.Not.EqualTo(initial));
            cover.ApplyExplosion(bounds.center,4,1000);Assert.That(cover.CollapseProgress(0),Is.EqualTo(progress));
            cover.TickPresentation(2);Assert.That(cover.IsCollapsing(0),Is.False);Assert.That(cover.CollapseProgress(0),Is.EqualTo(1));
            foreach(var collider in ruins.GetComponentsInChildren<Collider>())Assert.That(collider.enabled,Is.False);
            foreach(var renderer in ruins.GetComponentsInChildren<MeshRenderer>())
            {
                Assert.That(renderer.bounds.size.y,Is.LessThan(.6f),renderer.name);
                match.terrainMap.Field.TrySample(renderer.transform.position.x,renderer.transform.position.z,out float height);
                Assert.That(renderer.transform.position.y-height,Is.InRange(0,.6f));
            }
            yield return null;
        }
        [UnityTest] public IEnumerator RestartDuringCollapse_RemovesOldViewsAndRestoresIntactState()
        {
            var oldRoot=Root();cover.ApplyExplosion(cover.Buildings[0].Bounds.center,4,1000);cover.TickPresentation(.35f);
            match.RestartMatch(match.Seed);
            Assert.That(oldRoot.gameObject.activeSelf,Is.False);Assert.That(cover.Buildings[0].Destroyed,Is.False);
            Assert.That(cover.IsCollapsing(0),Is.False);Assert.That(Root().GetChild(0).gameObject.activeSelf,Is.True);
            Assert.That(Root().GetChild(1).gameObject.activeSelf,Is.False);
            yield return null;Assert.That(oldRoot==null,Is.True);
        }
        [UnityTest] public IEnumerator FoundationCollapse_RetargetsNewCraterAndDisableFinishesPresentation()
        {
            var field=match.terrainMap.Field;var bounds=cover.Buildings[0].Bounds;
            var region=field.ApplyCrater(bounds.center,20,6);match.terrainMap.UpdateRegion(region);cover.SettleFoundations();
            Assert.That(cover.Buildings[0].Destroyed,Is.True);Assert.That(cover.IsCollapsing(0),Is.True);
            cover.TickPresentation(.35f);
            region=field.ApplyCrater(bounds.center,20,1);match.terrainMap.UpdateRegion(region);cover.SettleFoundations();
            cover.enabled=false;Assert.That(cover.IsCollapsing(0),Is.False);
            foreach(var renderer in Root().GetChild(1).GetComponentsInChildren<MeshRenderer>())
            {
                var p=renderer.transform.position;field.TrySample(p.x,p.z,out float height);
                Assert.That(p.y-height,Is.InRange(0,.6f),renderer.name);
            }
            yield return null;
        }
    }
}
#endif
