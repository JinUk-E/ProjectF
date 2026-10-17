using System;
using NUnit.Framework;
using UnityEngine;

namespace Fortress.Tests
{
    public sealed class WaterHeightValidationTests
    {
        [TestCase(-10f)]
        [TestCase(-8f)]
        [TestCase(-4f)]
        public void FiniteWater_AcceptsLegacyBelowLowerAndReachableWater(float water)
        {
            var field=new HeightField(new float[2,2],Vector3.zero,Vector2.one,-8,water);
            Assert.That(field.WaterHeight,Is.EqualTo(water));Assert.That(field.LowerHeight,Is.EqualTo(-8));
        }
        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        [TestCase(float.NegativeInfinity)]
        public void NonFiniteWater_RejectsHeightFieldAndTerrainBeforeClone(float water)
        {
            Assert.Throws<ArgumentException>(()=>new HeightField(new float[2,2],Vector3.zero,Vector2.one,-8,water));
            var settings=ScriptableObject.CreateInstance<GameTuning>();settings.waterHeight=water;
            var data=new TerrainData{heightmapResolution=33,size=new Vector3(32,16,32)};
            var go=new GameObject("InvalidWater");go.transform.position=Vector3.down*8;
            try
            {
                var terrain=go.AddComponent<Terrain>();terrain.terrainData=data;
                var map=go.AddComponent<HeightFieldTerrain>();
                Assert.Throws<InvalidOperationException>(()=>map.Configure(terrain,settings));
                Assert.That(terrain.terrainData,Is.SameAs(data));Assert.That(map.Field,Is.Null);
            }
            finally { UnityEngine.Object.DestroyImmediate(go);UnityEngine.Object.DestroyImmediate(data);UnityEngine.Object.DestroyImmediate(settings); }
        }
        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        [TestCase(float.NegativeInfinity)]
        public void NonFiniteLower_RejectsHeightField(float lower)
        {
            Assert.Throws<ArgumentException>(()=>new HeightField(new float[2,2],Vector3.zero,Vector2.one,lower,-4));
        }
    }
}
