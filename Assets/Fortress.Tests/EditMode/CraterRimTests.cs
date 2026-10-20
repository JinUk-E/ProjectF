using NUnit.Framework;
using UnityEngine;
namespace Fortress.Tests
{
    public sealed class CraterRimTests
    {
        static HeightField Flat()
        {
            var grid=new float[81,81];for(int z=0;z<81;z++)for(int x=0;x<81;x++)grid[z,x]=4;
            return new HeightField(grid,new Vector3(-10,0,-10),new Vector2(20,20),-8,-4);
        }
        [Test] public void Rim_DepositsOutsideCraterAndBoundsRepeatedAccumulation()
        {
            var map=Flat();var region=map.ApplyCrater(Vector3.zero,4,.8f,.2f);
            map.TrySample(0,0,out float center);map.TrySample(5,0,out float rim);map.TrySample(7,0,out float outside);
            Assert.That(center,Is.EqualTo(3.2f).Within(.001));Assert.That(rim,Is.GreaterThan(4));Assert.That(outside,Is.EqualTo(4));
            Assert.That(region.xMax,Is.GreaterThan(60));
            for(int i=0;i<25;i++)map.ApplyCrater(Vector3.zero,4,.8f,.2f);
            map.TrySample(5,0,out rim);Assert.That(rim,Is.LessThanOrEqualTo(4.201f));
            map.Reset();map.TrySample(5,0,out rim);Assert.That(rim,Is.EqualTo(4));
        }
        [Test] public void FrozenRimDoesNotChangeWorld_AndLegacyCraterNeverRaisesGround()
        {
            var map=Flat();map.BeginShot();Assert.Throws<System.InvalidOperationException>(()=>map.ApplyCrater(Vector3.zero,4,1,.2f));
            Assert.That(map.Revision,Is.Zero);map.EndShot();map.ApplyCrater(Vector3.zero,4,1);
            for(int z=0;z<map.Depth;z++)for(int x=0;x<map.Width;x++)Assert.That(map.GetHeight(x,z),Is.LessThanOrEqualTo(4));
        }
    }
}
