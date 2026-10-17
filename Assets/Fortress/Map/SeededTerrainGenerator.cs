using System;
using UnityEngine;

namespace Fortress
{
    /// <summary>Deterministic smooth value noise. No Unity/global random state is consumed.</summary>
    public static class SeededTerrainGenerator
    {
        public static HeightField Generate(HeightField template,GameTuning settings,int seed,SpawnArea[] spawnAreas)
        {
            if(template==null || settings==null) throw new ArgumentNullException("Terrain template/settings");
            if(!Finite(settings.terrainBaseHeight) || !Finite(settings.terrainAmplitude) || settings.terrainAmplitude<0 ||
                !Finite(settings.terrainNoiseScale) || settings.terrainNoiseScale<0.01f || settings.terrainOctaves<1 || settings.terrainOctaves>8 ||
                !Finite(settings.terrainPersistence) || settings.terrainPersistence<0 || settings.terrainPersistence>1 ||
                !Finite(settings.spawnPadMargin) || settings.spawnPadMargin<0 || !Finite(settings.spawnPadBlend) || settings.spawnPadBlend<=0 ||
                !Finite(settings.spawnPadClearance) || settings.spawnPadClearance<=0)
                throw new ArgumentException("Random terrain settings must be finite and within their supported ranges.");
            if(spawnAreas==null) throw new ArgumentNullException(nameof(spawnAreas));
            foreach(var area in spawnAreas)
            {
                if(area==null || !Finite(area.min.x) || !Finite(area.min.y) || !Finite(area.max.x) || !Finite(area.max.y) ||
                    area.min.x>area.max.x || area.min.y>area.max.y || area.min.x<template.Origin.x || area.min.y<template.Origin.z ||
                    area.max.x>template.Origin.x+template.Size.x || area.max.y>template.Origin.z+template.Size.y)
                    throw new ArgumentException("Spawn areas must be ordered finite rectangles inside the terrain.");
            }
            var heights=new float[template.Depth,template.Width];
            float padHeight=Mathf.Max(settings.terrainBaseHeight,template.WaterHeight+settings.spawnPadClearance);
            // One extra sample cell keeps interpolation at an area edge entirely inside its flat pad.
            float margin=settings.spawnPadMargin+Mathf.Max(template.CellX,template.CellZ);
            for(int z=0;z<template.Depth;z++) for(int x=0;x<template.Width;x++)
            {
                float wx=template.Origin.x+x*template.CellX,wz=template.Origin.z+z*template.CellZ;
                float amplitude=1,weight=0,value=0,frequency=1/settings.terrainNoiseScale;
                for(int octave=0;octave<settings.terrainOctaves;octave++)
                {
                    value+=Noise(wx*frequency,wz*frequency,unchecked(seed+octave*1013))*amplitude;
                    weight+=amplitude;amplitude*=settings.terrainPersistence;frequency*=2;
                }
                float height=settings.terrainBaseHeight+settings.terrainAmplitude*(value/weight);
                float pad=0;
                foreach(var area in spawnAreas)
                {
                    float dx=Mathf.Max(area.min.x-margin-wx,Mathf.Max(0,wx-area.max.x-margin));
                    float dz=Mathf.Max(area.min.y-margin-wz,Mathf.Max(0,wz-area.max.y-margin));
                    float distance=Mathf.Sqrt(dx*dx+dz*dz);
                    float t=Mathf.Clamp01(distance/settings.spawnPadBlend);
                    pad=Mathf.Max(pad,1-Smooth(t));
                }
                heights[z,x]=Mathf.Max(template.LowerHeight,Mathf.Lerp(height,padHeight,pad));
            }
            return new HeightField(heights,template.Origin,template.Size,template.LowerHeight,template.WaterHeight);
        }
        static bool Finite(float v)=>!float.IsNaN(v)&&!float.IsInfinity(v);
        static float Smooth(float t)=>t*t*(3-2*t);
        static float Noise(float x,float z,int seed)
        {
            int ix=Mathf.FloorToInt(x),iz=Mathf.FloorToInt(z);
            float u=Smooth(x-ix),v=Smooth(z-iz);
            return Mathf.Lerp(Mathf.Lerp(Hash(ix,iz,seed),Hash(ix+1,iz,seed),u),
                Mathf.Lerp(Hash(ix,iz+1,seed),Hash(ix+1,iz+1,seed),u),v);
        }
        static float Hash(int x,int z,int seed)
        {
            unchecked
            {
                uint h=(uint)seed ^ ((uint)x*374761393u) ^ ((uint)z*668265263u);
                h=(h^(h>>13))*1274126177u;h^=h>>16;
                return (h & 0x00ffffffu)/16777215f*2-1;
            }
        }
    }
}
