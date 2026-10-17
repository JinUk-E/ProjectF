using System;
using NUnit.Framework;
using UnityEngine;

namespace Fortress.Tests
{
    public sealed class TerrainShotSurfaceTests
    {
        [Test]
        public void Initialize_LowerBelowTerrainRepresentation_RejectsBeforeCloning()
        {
            var go=new GameObject("InvalidTerrainTest");
            var data=new TerrainData {heightmapResolution=33,size=new Vector3(32,16,32)};
            var settings=ScriptableObject.CreateInstance<GameTuning>();
            try
            {
                var terrain=go.AddComponent<Terrain>();terrain.terrainData=data;
                var map=go.AddComponent<HeightFieldTerrain>();
                settings.terrainLowerHeight=-8;
                Assert.Throws<InvalidOperationException>(()=>map.Configure(terrain,settings));
                Assert.That(terrain.terrainData,Is.SameAs(data));
                Assert.That(map.Field,Is.Null);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
                UnityEngine.Object.DestroyImmediate(data);
                UnityEngine.Object.DestroyImmediate(settings);
            }
        }
        static HeightField Flat(float height = 0)
        {
            var heights = new float[5,5];
            for (int z=0; z<5; z++) for (int x=0; x<5; x++) heights[z,x]=height;
            return new HeightField(heights, new Vector3(-2,-8,-2), new Vector2(4,4), -8, -10);
        }

        [Test]
        public void HeightField_SourceChanges_DoNotMutateOwnedHeights()
        {
            var source=new float[,] {{0,2},{2,4}};
            var field=new HeightField(source,Vector3.zero,new Vector2(2,2),-8,-10);
            source[0,0]=99;
            Assert.That(field.GetHeight(0,0),Is.Zero);
            Assert.That(field.TrySample(1,1,out var height),Is.True);
            Assert.That(height,Is.EqualTo(2).Within(.0001));
            Assert.That(field.TrySample(-.01f,1,out _),Is.False);
        }

        [TestCase(0f,0f)]
        [TestCase(-2f,-2f)]
        [TestCase(2f,2f)]
        public void TryHit_VerticalAndBoundarySegments_ReturnGround(float x,float z)
        {
            var surface=new TerrainShotSurface(Flat());
            Assert.That(surface.TryHit(new Vector3(x,3,z),new Vector3(x,-3,z),out var hit),Is.True);
            Assert.That(Vector3.Distance(hit,new Vector3(x,0,z)),Is.LessThan(.0001));
        }

        [Test]
        public void TryHit_FullyOutsideMap_DoesNotExtendGroundPlane()
        {
            var surface=new TerrainShotSurface(Flat());
            Assert.That(surface.TryHit(new Vector3(3,3,0),new Vector3(3,-3,0),out _),Is.False);
            Assert.That(surface.TryHit(new Vector3(-3,0,-3),new Vector3(3,0,-3),out _),Is.False);
        }

        [Test]
        public void TryHit_EntersMapFromOutside_FindsInsideIntersection()
        {
            var surface=new TerrainShotSurface(Flat());
            Assert.That(surface.TryHit(new Vector3(-4,2,0),new Vector3(0,-2,0),out var hit),Is.True);
            Assert.That(Vector3.Distance(hit,new Vector3(-2,0,0)),Is.LessThan(.0001));
        }

        [Test]
        public void TryHit_BilinearRidgeBetweenAboveEndpoints_ReturnsFirstRoot()
        {
            var field=new HeightField(new float[,] {{0,4},{4,0}},Vector3.zero,Vector2.one,-8,-10);
            var surface=new TerrainShotSurface(field);
            Assert.That(surface.TryHit(new Vector3(0,1,0),new Vector3(1,1,1),out var hit),Is.True);
            float expected=(1-Mathf.Sqrt(.5f))*.5f;
            Assert.That(hit.x,Is.EqualTo(expected).Within(.0001));
            Assert.That(hit.z,Is.EqualTo(expected).Within(.0001));
        }

        [Test]
        public void TryHit_LongSegmentAcrossMultipleCells_DoesNotSkipRidge()
        {
            var heights=new float[2,5] {{0,0,3,0,0},{0,0,3,0,0}};
            var field=new HeightField(heights,Vector3.zero,new Vector2(4,1),-8,-10);
            var surface=new TerrainShotSurface(field);
            Assert.That(surface.TryHit(new Vector3(0,1,.5f),new Vector3(4,1,.5f),out var hit),Is.True);
            Assert.That(hit.x,Is.EqualTo(1+1f/3).Within(.0001));
        }

        [Test]
        public void ApplyCrater_ClipsBoundsAndLowerHeight_LeavesOutsideRadiusUnchanged()
        {
            var field=Flat();
            var region=field.ApplyCrater(new Vector3(-2,0,-2),1.5f,20);
            Assert.That(region.width,Is.GreaterThan(0));
            Assert.That(region.xMin,Is.GreaterThanOrEqualTo(0));
            Assert.That(region.yMin,Is.GreaterThanOrEqualTo(0));
            Assert.That(field.GetHeight(0,0),Is.EqualTo(-8));
            Assert.That(field.GetHeight(4,4),Is.Zero);
            for(int z=0;z<5;z++) for(int x=0;x<5;x++) Assert.That(field.GetHeight(x,z),Is.InRange(-8f,0f));
        }

        [Test]
        public void ApplyCrater_DuringShot_RejectsMutationUntilEndShot()
        {
            var field=Flat();
            field.BeginShot();
            Assert.That(field.IsFrozen,Is.True);
            Assert.Throws<InvalidOperationException>(()=>field.ApplyCrater(Vector3.zero,2,1));
            Assert.That(field.GetHeight(2,2),Is.Zero);
            field.EndShot();
            field.ApplyCrater(Vector3.zero,2,1);
            Assert.That(field.GetHeight(2,2),Is.LessThan(0));
        }
    }
}
