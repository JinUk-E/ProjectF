using System;
using NUnit.Framework;
using UnityEngine;
using Object=UnityEngine.Object;

namespace Fortress.Tests
{
    public sealed class SeededTerrainTests
    {
        GameTuning tuning;
        SpawnArea left,right;
        HeightField template;
        [SetUp] public void SetUp()
        {
            tuning=ScriptableObject.CreateInstance<GameTuning>();
            left=ScriptableObject.CreateInstance<SpawnArea>();
            right=ScriptableObject.CreateInstance<SpawnArea>();
            left.min=new Vector2(-29,-8);left.max=new Vector2(-21,8);
            right.min=new Vector2(21,-8);right.max=new Vector2(29,8);
            template=new HeightField(new float[129,129],new Vector3(-80,-8,-80),new Vector2(160,160),-8,-4);
        }
        [TearDown] public void TearDown()
        { Object.DestroyImmediate(tuning);Object.DestroyImmediate(left);Object.DestroyImmediate(right); }
        [Test]
        public void Generate_ManySeeds_HasFiniteVaryingHeightsAndDryFlatSpawnAreas()
        {
            for(int seed=0;seed<32;seed++)
            {
                var map=SeededTerrainGenerator.Generate(template,tuning,seed,new[]{left,right});
                float min=float.PositiveInfinity,max=float.NegativeInfinity;
                for(int z=0;z<map.Depth;z++) for(int x=0;x<map.Width;x++)
                {
                    float h=map.GetHeight(x,z);
                    Assert.That(float.IsNaN(h)||float.IsInfinity(h),Is.False);
                    min=Mathf.Min(min,h);max=Mathf.Max(max,h);
                    Assert.That(h,Is.GreaterThanOrEqualTo(-8));
                }
                Assert.That(max-min,Is.GreaterThan(1));
                foreach(var area in new[]{left,right})
                {
                    map.TrySample(area.min.x,area.min.y,out float first);
                    for(float z=area.min.y;z<=area.max.y;z+=2) for(float x=area.min.x;x<=area.max.x;x+=2)
                    {
                        Assert.That(map.TrySample(x,z,out float h),Is.True);
                        Assert.That(h,Is.GreaterThan(-4));
                        Assert.That(h,Is.EqualTo(first).Within(.001f));
                    }
                }
            }
        }
        [Test]
        public void Generate_SameSeedAfterCrater_RestoresPristineHeightsWithoutMutatingTemplate()
        {
            var a=SeededTerrainGenerator.Generate(template,tuning,73,new[]{left,right});
            a.ApplyCrater(Vector3.zero,10,5);
            var b=SeededTerrainGenerator.Generate(template,tuning,73,new[]{left,right});
            a.Reset();
            for(int z=0;z<a.Depth;z++) for(int x=0;x<a.Width;x++)
            {
                Assert.That(a.GetHeight(x,z),Is.EqualTo(b.GetHeight(x,z)));
                Assert.That(template.GetHeight(x,z),Is.Zero);
            }
        }
    }
}
