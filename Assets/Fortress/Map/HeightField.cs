using System;
using UnityEngine;

namespace Fortress
{
    /// <summary>Authoritative world-space heights, indexed [z,x]. Input data is always cloned.</summary>
    public sealed class HeightField
    {
        readonly float[,] heights;
        readonly float[,] originalHeights;
        public int Width => heights.GetLength(1);
        public int Depth => heights.GetLength(0);
        public Vector3 Origin { get; }
        public Vector2 Size { get; }
        public float LowerHeight { get; }
        public float WaterHeight { get; }
        public int Revision { get; private set; }
        public bool IsFrozen { get; private set; }
        public float CellX => Size.x / (Width - 1);
        public float CellZ => Size.y / (Depth - 1);
        public HeightField(float[,] worldHeights, Vector3 origin, Vector2 size, float lowerHeight, float waterHeight)
        {
            if (worldHeights == null || worldHeights.GetLength(0) < 2 || worldHeights.GetLength(1) < 2)
                throw new ArgumentException("Height grid requires at least 2 by 2 samples.");
            if (!(size.x > 0) || !(size.y > 0) || float.IsInfinity(size.x) || float.IsInfinity(size.y))
                throw new ArgumentOutOfRangeException(nameof(size));
            if (float.IsNaN(waterHeight) || float.IsInfinity(waterHeight) || float.IsNaN(lowerHeight) || float.IsInfinity(lowerHeight))
                throw new ArgumentException("Water and terrain lower heights must be finite.");
            heights = (float[,])worldHeights.Clone();
            Origin = origin; Size = size; LowerHeight = lowerHeight; WaterHeight = waterHeight;
            for (int z = 0; z < Depth; z++) for (int x = 0; x < Width; x++)
            {
                if (float.IsNaN(heights[z,x]) || float.IsInfinity(heights[z,x])) throw new ArgumentException("Non-finite height.");
                heights[z,x] = Mathf.Max(lowerHeight, heights[z,x]);
            }
            originalHeights = (float[,])heights.Clone();
        }
        public void Reset()
        {
            if (IsFrozen) throw new InvalidOperationException("Cannot reset active shot terrain.");
            Array.Copy(originalHeights,heights,heights.Length);
            Revision++;
        }
        public float GetHeight(int x, int z) => heights[z,x];
        public void BeginShot() { if (IsFrozen) throw new InvalidOperationException("Shot already active."); IsFrozen = true; }
        public void EndShot() { IsFrozen = false; }
        public bool TrySample(float x, float z, out float height) => TrySampleGrid(heights,x,z,out height);
        public bool TrySampleOriginal(float x, float z, out float height) => TrySampleGrid(originalHeights,x,z,out height);
        bool TrySampleGrid(float[,] grid,float x,float z,out float height)
        {
            float gx = (x - Origin.x) / CellX, gz = (z - Origin.z) / CellZ;
            height = 0;
            if (!(gx >= 0 && gx <= Width - 1 && gz >= 0 && gz <= Depth - 1)) return false;
            int ix = Mathf.Min(Mathf.FloorToInt(gx), Width - 2), iz = Mathf.Min(Mathf.FloorToInt(gz), Depth - 2);
            float u=gx-ix,v=gz-iz;
            height = Mathf.LerpUnclamped(Mathf.LerpUnclamped(grid[iz,ix],grid[iz,ix+1],u),
                Mathf.LerpUnclamped(grid[iz+1,ix],grid[iz+1,ix+1],u),v);
            return true;
        }
        internal float SampleCell(int x, int z, float u, float v) =>
            Mathf.LerpUnclamped(Mathf.LerpUnclamped(heights[z,x], heights[z,x+1], u),
                Mathf.LerpUnclamped(heights[z+1,x], heights[z+1,x+1], u), v);
        public RectInt ApplyCrater(Vector3 center, float radius, float depth, float rimHeight = 0)
        {
            if (IsFrozen) throw new InvalidOperationException("Terrain is frozen until shot completion.");
            if (!(radius > 0) || !(depth > 0) || float.IsInfinity(radius) || float.IsInfinity(depth)) return new RectInt();
            if (float.IsNaN(center.x) || float.IsInfinity(center.x) || float.IsNaN(center.z) || float.IsInfinity(center.z)) return new RectInt();
            rimHeight = float.IsNaN(rimHeight) || float.IsInfinity(rimHeight) ? 0 : Mathf.Clamp(rimHeight,0,depth*.25f);
            float reach = rimHeight > 0 ? radius*1.5f : radius;
            if (center.x + radius < Origin.x || center.x - radius > Origin.x + Size.x ||
                center.z + radius < Origin.z || center.z - radius > Origin.z + Size.y) return new RectInt();
            int x0 = Mathf.Clamp(Mathf.FloorToInt((center.x-reach-Origin.x)/CellX), 0, Width-1);
            int x1 = Mathf.Clamp(Mathf.CeilToInt((center.x+reach-Origin.x)/CellX), 0, Width-1);
            int z0 = Mathf.Clamp(Mathf.FloorToInt((center.z-reach-Origin.z)/CellZ), 0, Depth-1);
            int z1 = Mathf.Clamp(Mathf.CeilToInt((center.z+reach-Origin.z)/CellZ), 0, Depth-1);
            bool changed = false;
            for (int z = z0; z <= z1; z++) for (int x = x0; x <= x1; x++)
            {
                float dx = Origin.x + x*CellX-center.x, dz = Origin.z+z*CellZ-center.z;
                float falloff = 1f - (dx*dx+dz*dz)/(radius*radius);
                if (falloff <= 0) continue;
                float next = Mathf.Max(LowerHeight, heights[z,x] - depth*falloff);
                changed |= next != heights[z,x]; heights[z,x] = next;
            }
            if (!changed) return new RectInt();
            // Deposited earth belongs to the same authoritative surface. Cap against the initial
            // ground so repeated shelling cannot build an ever-growing ring wall.
            if(rimHeight>0) for(int z=z0;z<=z1;z++) for(int x=x0;x<=x1;x++)
            {
                float dx=Origin.x+x*CellX-center.x,dz=Origin.z+z*CellZ-center.z;
                float distance=Mathf.Sqrt(dx*dx+dz*dz)/radius;
                if(distance<=1 || distance>=1.5f || heights[z,x]<=WaterHeight) continue;
                float t=(distance-1)*2;
                float deposit=rimHeight*Mathf.Sin(t*Mathf.PI);
                heights[z,x]=Mathf.Max(heights[z,x],Mathf.Min(heights[z,x]+deposit,originalHeights[z,x]+rimHeight));
            }
            Revision++;
            return new RectInt(x0,z0,x1-x0+1,z1-z0+1);
        }
    }
}
