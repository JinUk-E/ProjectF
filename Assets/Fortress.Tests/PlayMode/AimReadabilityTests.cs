#if UNITY_EDITOR
using System.Collections;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityProjectile;
using Object=UnityEngine.Object;
namespace Fortress.Tests
{
    public sealed class AimReadabilityTests
    {
        FlatShotController shots;
        AimPreviewStyle style;
        Material material;
        [UnitySetUp] public IEnumerator Setup()
        {
            EditorSceneManager.LoadSceneInPlayMode("Assets/Fortress/Scenes/TerrainRange.unity",new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;shots=Object.FindFirstObjectByType<FlatShotController>();
            style=ScriptableObject.CreateInstance<AimPreviewStyle>();
            var shader=Shader.Find("Fortress/AimDepthLine");Assert.That(shader,Is.Not.Null);
            material=new Material(shader);style.coreMaterial=style.outlineMaterial=style.occludedMaterial=material;
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            if(shots!=null) shots.CancelShot();
            Object.Destroy(style);Object.Destroy(material);
            var scene=SceneManager.GetActiveScene();SceneManager.SetActiveScene(SceneManager.CreateScene("Readability cleanup"));
            yield return SceneManager.UnloadSceneAsync(scene);
        }
        [UnityTest] public IEnumerator StyledPreview_RetainsWindFreePartialArcAndDirection()
        {
            var go=new GameObject("Styled preview");var preview=go.AddComponent<DashedAimPreview>();preview.style=style;preview.Bind(shots,material);
            var lines=preview.GetComponentsInChildren<LineRenderer>();
            int count=shots.tuning.previewSegments;Assert.That(lines.Length,Is.EqualTo(3*count+3*(count/style.directionEvery)));
            var shot=new ArtilleryShot(shots.ActiveTank.Muzzle.position,shots.ActiveTank.Muzzle.forward,shots.Power,shots.tuning.gravity,Vector3.zero);
            float end=(count-1+shots.tuning.previewDashFraction)*shots.tuning.previewSeconds/count;
            Assert.That(Vector3.Distance(lines[count-1].GetPosition(1),shot.PositionAt(end)),Is.LessThan(.0001f));
            Vector3 endpoint=lines[count-1].GetPosition(1);shots.SetWind(new Vector3(5,0,2));
            Assert.That(lines[count-1].GetPosition(1),Is.EqualTo(endpoint));
            Assert.That(lines[count].startWidth,Is.GreaterThan(lines[0].startWidth));
            foreach(var line in lines) if(line.positionCount==3)
            {
                Vector3 tip=line.GetPosition(1),tail=(line.GetPosition(0)+line.GetPosition(2))*.5f;
                Assert.That(Vector3.Distance(tip,tail),Is.GreaterThan(0));
            }
            Assert.That(shots.TryFire(),Is.True);foreach(var line in lines)Assert.That(line.enabled,Is.False);
            shots.CancelShot();foreach(var line in lines)Assert.That(line.enabled,Is.True);
            preview.enabled=false;foreach(var line in lines)Assert.That(line.enabled,Is.False);
            yield return null;
        }
        [UnityTest] public IEnumerator RockLayer_RepaintsCraterWithoutChangingSimulation()
        {
            var map=shots.terrainMap;var data=map.terrain.terrainData;
            var oldTuning=map.tuning;var settings=Object.Instantiate(oldTuning);
            var mountain=ScriptableObject.CreateInstance<MountainTerrainSettings>();mountain.tacticalHills=true;
            settings.mountainTerrain=mountain;map.tuning=settings;
            var ground=new TerrainLayer();var road=new TerrainLayer();var rock=new TerrainLayer();
            try
            {
                data.terrainLayers=new[]{ground,road,rock};
                var region=map.Field.ApplyCrater(new Vector3(20,0,20),4,6);
                int revision=map.Field.Revision;map.Field.TrySample(20,20,out float before);
                map.UpdateRegion(region);
                Assert.That(map.Field.Revision,Is.EqualTo(revision));
                map.Field.TrySample(20,20,out float after);Assert.That(after,Is.EqualTo(before));
                var alpha=data.GetAlphamaps(0,0,data.alphamapWidth,data.alphamapHeight);float maximum=0;
                for(int z=0;z<data.alphamapHeight;z++)for(int x=0;x<data.alphamapWidth;x++)
                {
                    maximum=Mathf.Max(maximum,alpha[z,x,2]);
                    // Terrain splat channels are stored at 8-bit precision.
                    Assert.That(alpha[z,x,0]+alpha[z,x,1]+alpha[z,x,2],Is.EqualTo(1).Within(2f/255));
                    if(alpha[z,x,1]>.99f) Assert.That(alpha[z,x,2],Is.LessThan(.011f));
                }
                Assert.That(maximum,Is.GreaterThan(.1f));
                data.terrainLayers=new[]{ground,road};
                Assert.DoesNotThrow(()=>map.UpdateRegion(region));
            }
            finally
            {
                map.tuning=oldTuning;Object.Destroy(settings);Object.Destroy(mountain);
                Object.Destroy(ground);Object.Destroy(road);Object.Destroy(rock);
            }
            yield return null;
        }
        [UnityTest] public IEnumerator StyledPreview_InvalidBindingHidesExistingGeometry()
        {
            var go=new GameObject("Styled preview");var preview=go.AddComponent<DashedAimPreview>();preview.style=style;preview.Bind(shots,material);
            var lines=preview.GetComponentsInChildren<LineRenderer>();preview.Bind(null,material);
            foreach(var line in lines) Assert.That(line.enabled,Is.False);
            yield return null;
        }
    }
}
#endif

