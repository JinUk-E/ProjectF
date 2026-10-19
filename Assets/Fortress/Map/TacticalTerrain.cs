using System;
using UnityEngine;
namespace Fortress
{
    // Isolated rounded hills and a connected road loop, never two continuous mountain walls.
    public static class TacticalTerrain
    {
        public static void Validate(MountainTerrainSettings s)
        {
            foreach(float v in new[]{s.hillHeight,s.hillRadius,s.hillSpacing,s.roadHalfWidth,s.roadBlend,s.flankRoadOffset,s.roadJunctionX,s.peripheralHillRadius,s.peripheralInnerOffset,s.peripheralOuterOffset})
                if(float.IsNaN(v)||float.IsInfinity(v)||v<=0) throw new ArgumentException("Tactical terrain dimensions must be finite and positive.");
            if(float.IsNaN(s.hillJitter)||float.IsInfinity(s.hillJitter)||s.hillJitter<0) throw new ArgumentException("Invalid hill jitter.");
        }
        public static float Height(float x,float z,int seed,MountainTerrainSettings s,float detail)
        {
            float peak=0;
            for(int iz=-1;iz<=1;iz++) for(int ix=-1;ix<=1;ix++)
            {
                if(ix==0&&iz==0) continue;
                float cx=ix*s.hillSpacing+Hash(ix,iz,seed)*s.hillJitter;
                float cz=iz*s.hillSpacing+Hash(iz+7,ix,seed)*s.hillJitter;
                float radius=s.hillRadius*(1+.2f*Hash(ix+13,iz,seed));
                float d=new Vector2(x-cx,z-cz).magnitude/radius;
                float shape=d<1?.5f+.5f*Mathf.Cos(d*Mathf.PI):0;
                peak=Mathf.Max(peak,s.hillHeight*Mathf.Clamp(s.centralHillScale,.5f,1)*(1+.2f*Hash(ix,iz+29,seed))*shape);
            }
            // Separate side/outer hills: broad low relief rather than continuous canyon walls.
            for(int sx=-1;sx<=1;sx+=2)for(int sz=-1;sz<=1;sz+=2)for(int hill=0;hill<2;hill++)
            {
                float cx=sx*(hill==0?s.peripheralInnerOffset:s.peripheralOuterOffset)+Hash(sx+41,sz+hill,seed)*s.hillJitter*.45f;
                float cz=sz*(hill==0?s.peripheralOuterOffset:s.peripheralInnerOffset)+Hash(sz+53,sx+hill,seed)*s.hillJitter*.45f;
                float radius=s.peripheralHillRadius*(1+.12f*Hash(sx+hill,sz+71,seed));
                float d=new Vector2(x-cx,z-cz).magnitude/radius;
                if(d<1)peak=Mathf.Max(peak,Mathf.Max(0,s.peripheralHillHeight)*(1+.15f*Hash(sx+83,sz+hill,seed))*(.5f+.5f*Mathf.Cos(d*Mathf.PI)));
            }
            float height=s.valleyFloor+peak+s.detailHeight*detail;
            float road=Mathf.Clamp01((RoadDistance(x,z,s)-s.roadHalfWidth)/s.roadBlend);
            return Mathf.Lerp(s.valleyFloor,height,road*road*(3-2*road));
        }
        public static float RoadDistance(float x,float z,MountainTerrainSettings s)
        {
            float center=Mathf.Abs(z);
            float flank=Distance(new Vector2(x,z),new Vector2(-s.roadJunctionX,s.flankRoadOffset),new Vector2(s.roadJunctionX,s.flankRoadOffset));
            flank=Mathf.Min(flank,Distance(new Vector2(x,z),new Vector2(-s.roadJunctionX,-s.flankRoadOffset),new Vector2(s.roadJunctionX,-s.flankRoadOffset)));
            float link=Distance(new Vector2(x,z),new Vector2(-s.roadJunctionX,-s.flankRoadOffset),new Vector2(-s.roadJunctionX,s.flankRoadOffset));
            link=Mathf.Min(link,Distance(new Vector2(x,z),new Vector2(s.roadJunctionX,-s.flankRoadOffset),new Vector2(s.roadJunctionX,s.flankRoadOffset)));
            return Mathf.Min(center,Mathf.Min(flank,link));
        }
        static float Distance(Vector2 p,Vector2 a,Vector2 b)
        { var d=b-a;return (p-a-d*Mathf.Clamp01(Vector2.Dot(p-a,d)/d.sqrMagnitude)).magnitude; }
        static float Hash(int x,int z,int seed)
        { unchecked { uint h=(uint)seed^((uint)x*374761393u)^((uint)z*668265263u);h=(h^(h>>13))*1274126177u;h^=h>>16;return (h&0xffffffu)/16777215f*2-1; } }
    }
}
