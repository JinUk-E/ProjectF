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
    public sealed class BattlefieldDetailTests
    {
        MatchController match;
        [UnitySetUp] public IEnumerator Setup()
        {
            EditorSceneManager.LoadSceneInPlayMode("Assets/Fortress/Scenes/MilitaryAI.unity",new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;match=Object.FindFirstObjectByType<MatchController>();
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            if(match!=null)match.shots.CancelShot();
            var scene=SceneManager.GetActiveScene();SceneManager.SetActiveScene(SceneManager.CreateScene("Field detail cleanup"));
            yield return SceneManager.UnloadSceneAsync(scene);
        }
        [UnityTest] public IEnumerator WindArrow_TracksScreenRotationPitchAndCalmWithoutChangingWind()
        {
            var hud=Object.FindFirstObjectByType<MatchHud>();Assert.That(hud.windCamera,Is.Not.Null);
            Vector3 wind=new Vector3(2,0,1);match.shots.SetWind(wind);
            foreach(float yaw in new[]{0f,90f,180f})foreach(float pitch in new[]{20f,70f,90f})
            {
                hud.windCamera.transform.rotation=Quaternion.Euler(pitch,yaw,0);hud.RefreshWindPresentation();
                Vector2 p=MatchHud.ProjectWindToScreen(wind,hud.windCamera.transform);
                Vector3 arrow=hud.windArrow.localRotation*Vector3.right;
                Assert.That(Vector2.Dot(new Vector2(arrow.x,arrow.y),p.normalized),Is.GreaterThan(.999f));
                Assert.That(match.shots.Wind,Is.EqualTo(wind));
            }
            Assert.That(hud.windDetailLabel.text,Does.StartWith("GUN:"));
            match.shots.SetWind(Vector3.zero);hud.RefreshWindPresentation();
            Assert.That(hud.windArrow.gameObject.activeSelf,Is.False);Assert.That(hud.windDetailLabel.text,Is.EqualTo("CALM"));
            yield return null;
        }
        Transform PlantRoot(BattlefieldVegetation vegetation)
        {
            foreach(Transform child in vegetation.transform)
                if(child.name=="Vegetation (decorative, no cover)" && child.gameObject.activeSelf)return child;
            Assert.Fail("Missing live vegetation root");return null;
        }
        [UnityTest] public IEnumerator Vegetation_SeedReproducesAndExcludesRoadsSpawnsBuildings()
        {
            var vegetation=Object.FindFirstObjectByType<BattlefieldVegetation>();Assert.That(vegetation,Is.Not.Null);
            Assert.That(vegetation.ActiveCount,Is.GreaterThan(0));var root=PlantRoot(vegetation);
            var positions=new Vector3[root.childCount];var scales=new Vector3[root.childCount];var rotations=new Quaternion[root.childCount];
            for(int i=0;i<root.childCount;i++)
            {
                var plant=root.GetChild(i);positions[i]=plant.position;scales[i]=plant.localScale;rotations[i]=plant.rotation;
                var p=plant.position;var settings=vegetation.settings;
                Assert.That(TacticalTerrain.RoadDistance(p.x,p.z,match.tuning.mountainTerrain),Is.GreaterThanOrEqualTo(match.tuning.mountainTerrain.roadHalfWidth+settings.roadClearance-.001f));
                foreach(var b in match.cover.Buildings)
                {
                    var bounds=b.Bounds;bounds.Expand(new Vector3(settings.buildingClearance*2,0,settings.buildingClearance*2));
                    Assert.That(bounds.Contains(new Vector3(p.x,bounds.center.y,p.z)),Is.False);
                }
                foreach(var spawn in match.spawnAreas)
                {
                    float margin=settings.spawnClearance;
                    Assert.That(p.x>=spawn.min.x-margin && p.x<=spawn.max.x+margin && p.z>=spawn.min.y-margin && p.z<=spawn.max.y+margin,Is.False);
                }
                foreach(var collider in plant.GetComponentsInChildren<Collider>())Assert.That(collider.enabled,Is.False);
            }
            match.RestartMatch(match.Seed);root=PlantRoot(vegetation);Assert.That(root.childCount,Is.EqualTo(positions.Length));
            for(int i=0;i<root.childCount;i++) { Assert.That(root.GetChild(i).position,Is.EqualTo(positions[i]));Assert.That(root.GetChild(i).localScale,Is.EqualTo(scales[i]));Assert.That(root.GetChild(i).rotation,Is.EqualTo(rotations[i])); }
            yield return null;
        }
        [UnityTest] public IEnumerator Vegetation_CraterClearsDecorationOnlyAfterWorldThaws()
        {
            var vegetation=Object.FindFirstObjectByType<BattlefieldVegetation>();var plant=PlantRoot(vegetation).GetChild(0);
            int count=vegetation.ActiveCount;var field=match.terrainMap.Field;
            var region=field.ApplyCrater(plant.position,4,6);match.terrainMap.UpdateRegion(region);field.BeginShot();
            vegetation.Refresh();Assert.That(plant.gameObject.activeSelf,Is.True);
            field.EndShot();vegetation.Refresh();Assert.That(plant.gameObject.activeSelf,Is.False);Assert.That(vegetation.ActiveCount,Is.LessThan(count));
            yield return null;
        }
        [UnityTest] public IEnumerator BuildingDetails_StayInBoundsAndHideWithDestroyedBuilding()
        {
            var cover=match.cover;Assert.That(cover,Is.Not.Null);
            Transform root=null;foreach(Transform child in cover.transform) if(child.gameObject.activeSelf && child.name=="Battlefield buildings (runtime)")root=child;
            Assert.That(root,Is.Not.Null);
            for(int i=0;i<cover.Buildings.Length;i++)
            {
                var intact=root.GetChild(i*2);var bounds=cover.Buildings[i].Bounds;bounds.Expand(.002f);
                foreach(var renderer in intact.GetComponentsInChildren<Renderer>())
                { Assert.That(bounds.Contains(renderer.bounds.min),Is.True,renderer.name);Assert.That(bounds.Contains(renderer.bounds.max),Is.True,renderer.name); }
            }
            var first=root.GetChild(0);cover.ApplyExplosion(cover.Buildings[0].Bounds.center,4,1000);
            Assert.That(cover.Buildings[0].Destroyed,Is.True);Assert.That(first.gameObject.activeSelf,Is.False);
            yield return null;
        }
    }
}
#endif
