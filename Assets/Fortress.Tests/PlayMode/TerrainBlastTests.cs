#if UNITY_EDITOR
using System.Collections;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
namespace Fortress.Tests
{
    public sealed class TerrainBlastTests
    {
        MatchController match;
        [UnitySetUp] public IEnumerator Load()
        {
            EditorSceneManager.LoadSceneInPlayMode("Assets/Fortress/Scenes/MilitaryMatch.unity",new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;match=Object.FindAnyObjectByType<MatchController>();
        }
        [UnityTearDown] public IEnumerator Unload()
        {
            if(match!=null)match.shots.CancelShot();var scene=SceneManager.GetActiveScene();
            SceneManager.SetActiveScene(SceneManager.CreateScene("BlastCleanup"));yield return SceneManager.UnloadSceneAsync(scene);
        }
        [UnityTest] public IEnumerator Soil_PoolIsBoundedStaysOnGroundAndRestartClears()
        {
            var debris=match.GetComponent<TerrainBlastDebris>();Assert.That(debris,Is.Not.Null);
            var field=match.terrainMap.Field;field.TrySample(0,0,out float height);
            for(int i=0;i<30;i++)debris.Emit(new Vector3(0,height,0),4);
            Assert.That(debris.ActiveCount,Is.InRange(1,debris.settings.capacity));
            yield return new WaitForSeconds(1.1f);
            foreach(Transform fragment in debris.transform)
            {
                if(fragment.name!="Soil fragment" || !fragment.gameObject.activeSelf)continue;
                Assert.That(fragment.GetComponent<Collider>(),Is.Null);
                var p=fragment.position;Assert.That(field.TrySample(p.x,p.z,out float h),Is.True);
                Assert.That(p.y-h,Is.InRange(0,.15f));
            }
            match.RestartMatch(match.Seed);Assert.That(debris.ActiveCount,Is.Zero);
        }
        [UnityTest] public IEnumerator RoadsHaveHeightVariationAndInitialPadsResistFourShallowCraters()
        {
            var field=match.terrainMap.Field;float min=float.PositiveInfinity,max=float.NegativeInfinity;
            for(int x=-25;x<=25;x++){field.TrySample(x,0,out float h);min=Mathf.Min(min,h);max=Mathf.Max(max,h);}
            Assert.That(max-min,Is.GreaterThan(1));
            foreach(var tank in match.Tanks)
            {
                var p=tank.Rig.transform.position;
                for(int i=0;i<4;i++)field.ApplyCrater(p,4,.55f,.1f);
                field.TrySample(p.x,p.z,out float h);Assert.That(h,Is.GreaterThan(field.WaterHeight+2));
            }
            yield return null;
        }
    }
}
#endif
