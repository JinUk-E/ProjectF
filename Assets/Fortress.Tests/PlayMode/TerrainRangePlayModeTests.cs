#if UNITY_EDITOR
using System;
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
    [TestFixture(false)]
    [TestFixture(true)]
    public sealed class TerrainRangePlayModeTests
    {
        readonly bool military;
        public TerrainRangePlayModeTests(bool military) { this.military=military; }
        FlatShotController controller;
        HeightFieldTerrain map;
        [UnitySetUp]
        public IEnumerator LoadRange()
        {
            EditorSceneManager.LoadSceneInPlayMode(military ? "Assets/Fortress/Scenes/MilitaryRange.unity" : "Assets/Fortress/Scenes/TerrainRange.unity",new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            controller=Object.FindFirstObjectByType<FlatShotController>();
            map=controller.terrainMap;
            Assert.That(map.Field,Is.Not.Null);
        }
        [UnityTearDown]
        public IEnumerator CleanRange()
        {
            if(controller!=null) controller.CancelShot();
            var scene=SceneManager.GetActiveScene();
            SceneManager.SetActiveScene(SceneManager.CreateScene("TerrainTestCleanup"));
            yield return SceneManager.UnloadSceneAsync(scene);
        }

        [UnityTest]
        public IEnumerator TerrainRange_Shot_ChangesPrivateViewAndReaimsWithinOneCentimetre()
        {
            var original=UnityEditor.AssetDatabase.LoadAssetAtPath<TerrainData>("Assets/Fortress/Data/RangeTerrain.asset");
            Assert.That(map.terrain.terrainData,Is.Not.SameAs(original));
            float originalHeight=original.GetHeight(128,128);
            Vector3 previous=default;
            for(int shot=0;shot<2;shot++)
            {
                Vector3 predicted=controller.LastPrediction.ImpactPosition;
                if(shot>0) Assert.That(Vector3.Distance(predicted,previous),Is.GreaterThan(.1f));
                previous=predicted;
                Assert.That(controller.TryFire(),Is.True);
                controller.ActiveRunner.Tick(.01f);
                Assert.That(map.Field.Revision,Is.EqualTo(shot));
                controller.ActiveRunner.Tick(controller.tuning.maxFlightTime+1);
                Assert.That(controller.IsFiring,Is.False);
                Assert.That(map.Field.Revision,Is.EqualTo(shot+1));
                Assert.That(controller.LastImpactError,Is.LessThanOrEqualTo(.01f));
                map.Field.TrySample(predicted.x,predicted.z,out var worldHeight);
                float u=(predicted.x-map.Field.Origin.x)/map.Field.Size.x;
                float v=(predicted.z-map.Field.Origin.z)/map.Field.Size.y;
                float viewHeight=map.terrain.terrainData.GetInterpolatedHeight(u,v)+map.terrain.transform.position.y;
                Assert.That(Mathf.Abs(viewHeight-worldHeight),Is.LessThan(.01f));
                yield return null;
            }
            Assert.That(original.GetHeight(128,128),Is.EqualTo(originalHeight));
            // Bilinear versus either triangulation differs by at most |cross coefficient|/4 in each cell.
            float worst=0;
            for(int z=0;z<map.Field.Depth-1;z++) for(int x=0;x<map.Field.Width-1;x++)
            {
                float cross=map.Field.GetHeight(x+1,z+1)-map.Field.GetHeight(x+1,z)-map.Field.GetHeight(x,z+1)+map.Field.GetHeight(x,z);
                worst=Mathf.Max(worst,Mathf.Abs(cross)*.25f);
            }
            Assert.That(worst,Is.LessThanOrEqualTo(.3f));
            TestContext.WriteLine("Worst bilinear/triangle deviation bound: "+worst+" m");
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"-fortressCapture")>=0)
            {
                if(military) { controller.impactEffect.Simulate(.5f,true,true,true); TestContext.WriteLine("Smoke capture particles: "+controller.impactEffect.particleCount); yield return null; }
                CaptureRange(military ? "military-after-shot.png" : "terrain-after-shot.png");
                if(military)
                {
                    var camera=Camera.main;
                    camera.transform.position=controller.ActiveTank.transform.position+new Vector3(10,7,-10);
                    camera.transform.LookAt(controller.ActiveTank.transform.position+Vector3.up*1.5f);
                    CaptureRange("military-tank-detail.png");
                }
            }
        }

        [UnityTest]
        public IEnumerator TerrainRange_Preview_IgnoresWindAndHidesOnlyDuringFlight()
        {
            var preview=Object.FindFirstObjectByType<DashedAimPreview>();
            preview.Refresh();
            var lines=preview.GetComponentsInChildren<LineRenderer>();
            Assert.That(lines.Length,Is.EqualTo(controller.tuning.previewSegments));
            Vector3 before=lines[lines.Length-1].GetPosition(1);
            bool debugEnabled=controller.tuning.showDebugPrediction;
            try
            {
                controller.tuning.showDebugPrediction=false;
                controller.SetWind(Vector3.zero);
                Assert.That(controller.predictionMarker.gameObject.activeSelf,Is.False);
                controller.tuning.showDebugPrediction=true;
                controller.SetWind(new Vector3(3,0,0));
                Assert.That(controller.predictionMarker.gameObject.activeSelf,Is.True);
            }
            finally { controller.tuning.showDebugPrediction=debugEnabled; }
            Assert.That(lines[lines.Length-1].GetPosition(1),Is.EqualTo(before));
            var muzzle=controller.ActiveTank.Muzzle;
            var shot=new ArtilleryShot(muzzle.position,muzzle.forward,controller.Power,controller.tuning.gravity,Vector3.zero);
            float end=(lines.Length-1+controller.tuning.previewDashFraction)*controller.tuning.previewSeconds/lines.Length;
            Assert.That(Vector3.Distance(before,shot.PositionAt(end)),Is.LessThan(.0001f));
            Assert.That(controller.TryFire(),Is.True);
            foreach(var line in lines) Assert.That(line.enabled,Is.False);
            controller.CancelShot();
            foreach(var line in lines) Assert.That(line.enabled,Is.True);
            preview.enabled=false;
            foreach(var line in lines) Assert.That(line.enabled,Is.False);
            preview.enabled=true;
            foreach(var line in lines) Assert.That(line.enabled,Is.True);
            yield return null;
        }

        static void CaptureRange(string filename)
        {
            var camera=Camera.main;
            var target=new RenderTexture(1280,720,24);
            var pixels=new Texture2D(1280,720,TextureFormat.RGB24,false);
            var canvas=Object.FindFirstObjectByType<Canvas>();
            var oldMode=canvas.renderMode;
            var oldCamera=canvas.worldCamera;
            var previous=RenderTexture.active;
            float oldAspect=camera.aspect;
            try
            {
                target.Create();
                camera.aspect=1280f/720;
                canvas.renderMode=RenderMode.ScreenSpaceCamera; canvas.worldCamera=camera;
                canvas.planeDistance=1;
                Canvas.ForceUpdateCanvases();
                UnityEngine.Rendering.RenderPipeline.SubmitRenderRequest(camera,
                    new UnityEngine.Rendering.RenderPipeline.StandardRequest {destination=target});
                RenderTexture.active=target;
                pixels.ReadPixels(new Rect(0,0,1280,720),0,0); pixels.Apply();
                System.IO.File.WriteAllBytes("Docs/Validation/"+filename,pixels.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active=previous; camera.aspect=oldAspect;
                canvas.renderMode=oldMode;canvas.worldCamera=oldCamera;
                target.Release();Object.Destroy(target);Object.Destroy(pixels);
            }
        }

        [UnityTest]
        public IEnumerator TerrainRange_CraterPatch_Reports257GridUpdateTiming()
        {
            var watch=new System.Diagnostics.Stopwatch();
            double max=0,total=0;
            // Warm up engine upload path outside measured samples.
            map.UpdateRegion(map.Field.ApplyCrater(new Vector3(-60,0,-60),4,2));
            for(int i=0;i<32;i++)
            {
                var center=new Vector3(-60+(i%8)*16,0,-40+(i/8)*20);
                watch.Restart();
                map.UpdateRegion(map.Field.ApplyCrater(center,4,2));
                watch.Stop();
                double ms=watch.Elapsed.TotalMilliseconds;
                max=Math.Max(max,ms);total+=ms;
                yield return null;
            }
            TestContext.WriteLine("257x257 crater + partial upload + SyncHeightmap: max="+max.ToString("F3")+"ms mean="+(total/32).ToString("F3")+"ms");
            Assert.That(max,Is.LessThanOrEqualTo(16),"Crater update acceptance budget on this machine");
        }
    }
}
#endif
