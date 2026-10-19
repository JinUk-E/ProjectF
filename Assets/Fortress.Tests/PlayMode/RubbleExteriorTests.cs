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
    public sealed class RubbleExteriorTests
    {
        MatchController match;
        BattlefieldExterior exterior;
        [UnitySetUp] public IEnumerator Setup()
        {
            EditorSceneManager.LoadSceneInPlayMode("Assets/Fortress/Scenes/MilitaryAI.unity",new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;match=Object.FindFirstObjectByType<MatchController>();exterior=Object.FindFirstObjectByType<BattlefieldExterior>();
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            if(match!=null)match.shots.CancelShot();
            var scene=SceneManager.GetActiveScene();SceneManager.SetActiveScene(SceneManager.CreateScene("Rubble exterior cleanup"));
            yield return SceneManager.UnloadSceneAsync(scene);
        }
        void DestroyFirst() { match.cover.ApplyExplosion(match.cover.Buildings[0].Bounds.center,4,1000); }
        [UnityTest] public IEnumerator Rubble_BlocksLowShotBlastAndMovementButAllowsHighShotAndDetour()
        {
            DestroyFirst();var cover=match.cover;var building=cover.Buildings[0];var p=building.Rubble[0].Position;
            var from=p-Vector3.right*5;var to=p+Vector3.right*5;
            Assert.That(cover.TryHit(from,to,out _),Is.True);Assert.That(cover.Shields(from,to),Is.True);
            Assert.That(cover.BlocksMovement(from,to,1),Is.True);
            float high=building.RubbleBounds.max.y+1;from.y=to.y=high;
            Assert.That(cover.TryHit(from,to,out _),Is.False);
            from.z=to.z=building.Bounds.max.z+2;
            Assert.That(cover.BlocksMovement(from,to,1),Is.False,"A route around the compact pile remains clear.");
            Assert.That(building.RubbleBounds.size.x,Is.LessThan(building.Bounds.size.x));
            Assert.That(building.RubbleBounds.size.z,Is.LessThan(building.Bounds.size.z));
            yield return null;
        }
        [UnityTest] public IEnumerator Rubble_SettledVisibleSlabsMatchAnalyticOrientedBounds()
        {
            DestroyFirst();var cover=match.cover;cover.TickPresentation(2);Transform root=null;
            foreach(Transform child in cover.transform)if(child.name=="Battlefield buildings (runtime)" && child.gameObject.activeSelf)root=child;
            Assert.That(root,Is.Not.Null);var ruins=root.GetChild(1);var slabs=cover.Buildings[0].Rubble;
            for(int i=0;i<slabs.Length;i++)
            {
                var renderer=ruins.GetChild(i).GetComponent<MeshRenderer>();
                Assert.That(Vector3.Distance(renderer.bounds.center,slabs[i].WorldBounds.center),Is.LessThan(.001f));
                Assert.That(Vector3.Distance(renderer.bounds.size,slabs[i].WorldBounds.size),Is.LessThan(.001f));
                Assert.That(renderer.gameObject.activeSelf,Is.EqualTo(slabs[i].Active));
            }
            yield return null;
        }
        [UnityTest] public IEnumerator Rubble_FreezeWaterAndRestartMaintainAuthoritativeState()
        {
            DestroyFirst();var cover=match.cover;var field=match.terrainMap.Field;var old=cover.Buildings[0];var p=old.Rubble[0].Position;
            field.BeginShot();Assert.Throws<System.InvalidOperationException>(()=>cover.SettleFoundations());
            cover.TickPresentation(.35f);Assert.That(old.Rubble[0].Position,Is.EqualTo(p));field.EndShot();
            var region=field.ApplyCrater(old.Bounds.center,20,16);match.terrainMap.UpdateRegion(region);cover.SettleFoundations();
            Assert.That(old.HasDryRubble,Is.False);Assert.That(cover.BlocksMovement(p,p,1),Is.False);
            cover.TickPresentation(2);
            match.RestartMatch(match.Seed);Assert.That(cover.Buildings[0].Destroyed,Is.False);
            Assert.That(cover.Buildings[0].Rubble[0].Position,Is.EqualTo(p));
            yield return null;
        }
        void CheckSeam()
        {
            Assert.That(exterior,Is.Not.Null);var vertices=exterior.ApronMesh.vertices;var field=match.terrainMap.Field;
            for(int i=0;i<exterior.SeamVertexCount;i++)
            {
                var world=exterior.transform.TransformPoint(vertices[i]);Assert.That(field.TrySample(world.x,world.z,out float height),Is.True);
                Assert.That(world.y,Is.EqualTo(height).Within(.0001f));
            }
        }
        [UnityTest] public IEnumerator Exterior_SeamMatchesAndBoundaryRemainsNonCombat()
        {
            CheckSeam();var field=match.terrainMap.Field;float x=field.Origin.x+field.Size.x+10,z=field.Origin.z+field.Size.y*.5f;
            Assert.That(exterior.TrySampleVisualHeight(x,z,out _),Is.True);Assert.That(field.TrySample(x,z,out _),Is.False);
            foreach(var collider in exterior.GetComponentsInChildren<Collider>())Assert.That(collider.enabled,Is.False);
            var camera=Object.FindFirstObjectByType<BattlefieldOrbitCamera>();camera.exterior=exterior;
            var tank=match.shots.ActiveTank;float edgeX=field.Origin.x+field.Size.x-.5f;field.TrySample(edgeX,z,out float h);
            match.Tanks[match.ActivePlayerIndex].MoveOnGround(new Vector3(edgeX,h,z));camera.FocusActiveTank();
            camera.Orbit(new Vector2((90-camera.Yaw)/camera.settings.orbitDegreesPerPixel,(camera.Pitch-camera.settings.minPitch)/camera.settings.orbitDegreesPerPixel));
            camera.Zoom(-100000);camera.ApplyPose();var cp=camera.transform.position;
            Assert.That(exterior.TrySampleVisualHeight(cp.x,cp.z,out float ground),Is.True);Assert.That(cp.y,Is.GreaterThanOrEqualTo(ground+camera.settings.groundClearance-.001f));
            yield return null;
        }
        [UnityTest] public IEnumerator Exterior_EdgeCraterUpdatesSeamAfterThawButInteriorCraterSkipsRebuild()
        {
            var field=match.terrainMap.Field;int version=exterior.BuildVersion;
            var region=field.ApplyCrater(Vector3.zero,4,1);match.terrainMap.UpdateRegion(region);exterior.Refresh();
            Assert.That(exterior.BuildVersion,Is.EqualTo(version));
            var edge=new Vector3(field.Origin.x,0,field.Origin.z+field.Size.y*.5f);
            region=field.ApplyCrater(edge,4,2);match.terrainMap.UpdateRegion(region);field.BeginShot();exterior.Refresh();
            Assert.That(exterior.BuildVersion,Is.EqualTo(version));field.EndShot();exterior.Refresh();
            Assert.That(exterior.BuildVersion,Is.EqualTo(version+1));CheckSeam();yield return null;
        }
        [UnityTest] public IEnumerator Exterior_ConstantTimeHeightLookupMatchesTriangleCentroids()
        {
            var mesh=exterior.ApronMesh;var vertices=mesh.vertices;var indices=mesh.triangles;
            int stride=Mathf.Max(1,indices.Length/3/40);
            for(int triangle=0;triangle<indices.Length/3;triangle+=stride)
            {
                int i=triangle*3;var p=exterior.transform.TransformPoint((vertices[indices[i]]+vertices[indices[i+1]]+vertices[indices[i+2]])/3);
                Assert.That(exterior.TrySampleVisualHeight(p.x,p.z,out float h),Is.True);Assert.That(h,Is.EqualTo(p.y).Within(.001f));
            }
            yield return null;
        }
    }
}
#endif
