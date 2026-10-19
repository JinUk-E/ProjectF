#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;
namespace Fortress.Tests
{
    public sealed class BoundaryWallTests
    {
        MatchController match;
        BattlefieldExterior exterior;
        [UnitySetUp] public IEnumerator Setup()
        {
            EditorSceneManager.LoadSceneInPlayMode("Assets/Fortress/Scenes/MilitaryAI.unity",new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;match=Object.FindFirstObjectByType<MatchController>();exterior=Object.FindFirstObjectByType<BattlefieldExterior>();
            Assert.That(exterior.BoundaryCollider,Is.Not.Null);
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            if(match!=null)match.shots.CancelShot();
            var scene=SceneManager.GetActiveScene();SceneManager.SetActiveScene(SceneManager.CreateScene("Boundary cleanup"));
            yield return SceneManager.UnloadSceneAsync(scene);
        }
        [UnityTest] public IEnumerator Wall_IsClosedAndInnerFaceMatchesPlayableEdgeOnAllSides()
        {
            var mesh=exterior.BoundaryWallMesh;Assert.That(exterior.BoundaryCollider.sharedMesh,Is.SameAs(mesh));
            var vertices=mesh.vertices;var field=match.terrainMap.Field;
            int n=exterior.SeamVertexCount;
            for(int i=0;i<n;i++)
            {
                Vector3 inner=exterior.transform.TransformPoint(vertices[i]),crest=exterior.transform.TransformPoint(vertices[2*n+i]);
                Assert.That(field.TrySample(inner.x,inner.z,out float ground),Is.True);
                Assert.That(inner.y,Is.EqualTo(ground).Within(.001));
                Assert.That(crest.y-ground,Is.InRange(exterior.settings.boundaryRidgeMinHeight-.001f,exterior.settings.boundaryRidgeMaxHeight+.001f));
                Assert.That(field.TrySample(crest.x,crest.z,out _),Is.False);
            }
            var edges=new Dictionary<(int,int),int>();var triangles=mesh.triangles;
            for(int i=0;i<triangles.Length;i+=3)for(int j=0;j<3;j++)
            {
                int a=triangles[i+j],b=triangles[i+(j+1)%3];var key=a<b?(a,b):(b,a);
                edges.TryGetValue(key,out int count);edges[key]=count+1;
            }
            int perimeterEdges=0;
            foreach(var count in edges.Values) {Assert.That(count,Is.InRange(1,2));if(count==1)perimeterEdges++;}
            Assert.That(perimeterEdges,Is.EqualTo(n*2),"Only the inner/outer loops are open; all corner and strip joins are continuous.");
            Physics.SyncTransforms();
            var center=new Vector3(field.Origin.x+field.Size.x*.5f,0,field.Origin.z+field.Size.y*.5f);
            foreach(var direction in new[]{Vector3.left,Vector3.right,Vector3.forward,Vector3.back})
            {
                var boundary=center+new Vector3(direction.x*field.Size.x*.5f,0,direction.z*field.Size.y*.5f);
                field.TrySample(boundary.x,boundary.z,out float height);boundary.y=height+1;
                Assert.That(exterior.BoundaryCollider.Raycast(new Ray(boundary-direction*.5f,direction),out var hit,exterior.settings.boundaryRidgeWidth+1),Is.True);
                Assert.That(Vector3.Distance(hit.point,boundary),Is.InRange(.01f,exterior.settings.boundaryShoulderDistance));
                Assert.That(exterior.TrySampleVisualHeight(hit.point.x,hit.point.z,out float surface),Is.True);Assert.That(hit.point.y,Is.EqualTo(surface).Within(.002f));
            }
            yield return null;
        }
        [UnityTest] public IEnumerator Wall_DoesNotExtendMovementAreaOrAlterAnalyticShotSurface()
        {
            var field=match.terrainMap.Field;float x=field.Origin.x+field.Size.x,z=field.Origin.z+field.Size.y*.5f;
            field.TrySample(x,z,out float h);var boundary=new Vector3(x,h+1,z);
            Physics.SyncTransforms();Assert.That(exterior.BoundaryCollider.Raycast(new Ray(boundary-Vector3.right*.5f,Vector3.right),out _,exterior.settings.boundaryRidgeWidth+1),Is.True);
            Assert.That(match.shots.Surface.TryHit(boundary-Vector3.right*.5f,boundary+Vector3.right*1.5f,out _),Is.False,"Boundary visuals are not a new projectile surface.");
            Assert.That(field.TrySample(x+.1f,z,out _),Is.False);
            var tank=match.Tanks[match.ActivePlayerIndex];var start=new Vector3(x-tank.Radius-.05f,h,z);tank.MoveOnGround(start);
            float budget=match.MovementRemaining;Assert.That(match.TryMoveActiveTank(Vector2.right,.25f),Is.False);
            Assert.That(tank.Rig.transform.position,Is.EqualTo(start));Assert.That(match.MovementRemaining,Is.EqualTo(budget));
            yield return null;
        }
        [UnityTest] public IEnumerator Wall_EdgeCraterRebuildsAfterThawAndRestartRestoresGeometry()
        {
            var original=exterior.BoundaryWallMesh;var before=original.vertices;var field=match.terrainMap.Field;
            var center=new Vector3(field.Origin.x,0,field.Origin.z+field.Size.y*.5f);
            var region=field.ApplyCrater(center,4,2);match.terrainMap.UpdateRegion(region);field.BeginShot();exterior.Refresh();
            Assert.That(exterior.BoundaryWallMesh,Is.SameAs(original));field.EndShot();exterior.Refresh();
            Assert.That(exterior.BoundaryWallMesh,Is.Not.SameAs(original));Assert.That(exterior.BoundaryCollider.sharedMesh,Is.SameAs(exterior.BoundaryWallMesh));
            bool changed=false;var after=exterior.BoundaryWallMesh.vertices;
            for(int i=0;i<before.Length;i++)if(Mathf.Abs(before[i].y-after[i].y)>.1f){changed=true;break;}
            Assert.That(changed,Is.True);match.RestartMatch(match.Seed);
            var reset=exterior.BoundaryWallMesh.vertices;Assert.That(reset.Length,Is.EqualTo(before.Length));
            for(int i=0;i<before.Length;i++)Assert.That(Vector3.Distance(before[i],reset[i]),Is.LessThan(.001));
            yield return null;
        }
        [UnityTest] public IEnumerator NaturalBoundary_HeightQueriesMatchVisibleRockWithoutArtificialProps()
        {
            var mesh=exterior.BoundaryWallMesh;var vertices=mesh.vertices;var indices=mesh.triangles;
            int stride=Mathf.Max(1,indices.Length/3/32);
            for(int triangle=0;triangle<indices.Length/3;triangle+=stride)
            {
                int i=triangle*3;var p=exterior.transform.TransformPoint((vertices[indices[i]]+vertices[indices[i+1]]+vertices[indices[i+2]])/3);
                Assert.That(exterior.TrySampleVisualHeight(p.x,p.z,out float h),Is.True);Assert.That(h,Is.EqualTo(p.y).Within(.002f));
            }
            foreach(var child in exterior.GetComponentsInChildren<Transform>())
            {Assert.That(child.name,Is.Not.EqualTo("Boundary marker"));Assert.That(child.name,Is.Not.EqualTo("Playable boundary cable"));}
            yield return null;
        }
        [UnityTest] public IEnumerator CentralPeaks_AreReducedByConfiguredQuarterWithoutRemovingSideRelief()
        {
            var current=match.tuning.mountainTerrain;var baseline=Object.Instantiate(current);baseline.centralHillScale=1;baseline.peripheralHillHeight=0;
            try
            {
                for(int seed=1;seed<=8;seed++)
                {
                    float x=current.hillSpacing,z=current.hillSpacing;
                    float before=TacticalTerrain.Height(x,z,seed,baseline,0)-current.valleyFloor;
                    float after=TacticalTerrain.Height(x,z,seed,current,0)-current.valleyFloor;
                    Assert.That(after,Is.EqualTo(before*current.centralHillScale).Within(.001f));
                }
            }
            finally {Object.Destroy(baseline);}
            yield return null;
        }
        [UnityTest] public IEnumerator ExteriorFog_PropertiesApplyOnlyToGroundAndTrees()
        {
            var field=match.terrainMap.Field;var expected=new Vector4(field.Origin.x,field.Origin.z,field.Origin.x+field.Size.x,field.Origin.z+field.Size.y);
            var block=new MaterialPropertyBlock();int fogged=0;
            Transform scenery=null;
            foreach(Transform child in exterior.transform)
                if(child.name=="Exterior scenery (non-combat)" && child.gameObject.activeSelf)scenery=child;
            Assert.That(scenery,Is.Not.Null);
            // The component may share the match root with unrelated buildings and playable vegetation.
            foreach(var renderer in scenery.GetComponentsInChildren<MeshRenderer>())
            {
                renderer.GetPropertyBlock(block);var rect=block.GetVector("_PlayableRect");

                Assert.That(rect,Is.EqualTo(expected));var range=block.GetVector("_ExteriorFogRange");
                Assert.That(range.x,Is.EqualTo(exterior.settings.fogStart));Assert.That(range.y,Is.EqualTo(exterior.settings.fogEnd));fogged++;
            }
            Assert.That(fogged,Is.GreaterThan(1));yield return null;
        }
    }
}
#endif
