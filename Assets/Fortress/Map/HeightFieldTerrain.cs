using UnityEngine;

namespace Fortress
{
    /// <summary>Owns a private TerrainData view; simulation reads Field exclusively.</summary>
    public sealed class HeightFieldTerrain : MonoBehaviour
    {
        public Terrain terrain;
        public GameTuning tuning;
        public HeightField Field { get; private set; }
        public TerrainShotSurface Surface { get; private set; }
        TerrainData originalData;
        TerrainData runtimeData;
        void Start() { Initialize(); }
        public void Configure(Terrain view, GameTuning settings)
        {
            if (Field != null) throw new System.InvalidOperationException("Terrain already initialized.");
            terrain=view;tuning=settings;Initialize();
        }
        public void Initialize()
        {
            if (Field != null || terrain == null || tuning == null || terrain.terrainData == null) return;
            var baseHeight = terrain.transform.position.y;
            var topHeight = baseHeight + terrain.terrainData.size.y;
            if (!(tuning.terrainLowerHeight >= baseHeight && tuning.terrainLowerHeight <= topHeight))
                throw new System.InvalidOperationException("Terrain lower bound must lie within the Terrain vertical range.");
            if (float.IsNaN(tuning.waterHeight) || float.IsInfinity(tuning.waterHeight))
                throw new System.InvalidOperationException("Water height must be finite.");
            originalData=terrain.terrainData;
            runtimeData=Instantiate(originalData);
            runtimeData.name=originalData.name+" (Runtime)";
            terrain.terrainData=runtimeData;
            terrain.heightmapPixelError=1f;
            int resolution=runtimeData.heightmapResolution;
            var heights=runtimeData.GetHeights(0,0,resolution,resolution);
            var origin=terrain.transform.position;
            for(int z=0;z<resolution;z++) for(int x=0;x<resolution;x++)
                heights[z,x]=origin.y+heights[z,x]*runtimeData.size.y;
            Field=new HeightField(heights,origin,new Vector2(runtimeData.size.x,runtimeData.size.z),tuning.terrainLowerHeight,tuning.waterHeight);
            Surface=new TerrainShotSurface(Field);
            // Constructor lower-bound clamping must also be reflected by the view.
            UpdateRegion(new RectInt(0,0,resolution,resolution));
        }
        public HeightField PrepareSeededTerrain(int seed,GameTuning settings,SpawnArea[] spawnAreas)
        {
            Initialize();
            if(Field==null) throw new System.InvalidOperationException("Terrain is not initialized.");
            var candidate=SeededTerrainGenerator.Generate(Field,settings,seed,spawnAreas);
            ValidateCandidate(candidate);
            return candidate;
        }
        void ValidateCandidate(HeightField candidate)
        {
            if(candidate==null || runtimeData==null || Field==null || candidate.IsFrozen ||
                candidate.Width!=Field.Width || candidate.Depth!=Field.Depth || candidate.Origin!=Field.Origin || candidate.Size!=Field.Size ||
                candidate.LowerHeight!=Field.LowerHeight || candidate.WaterHeight!=Field.WaterHeight)
                throw new System.ArgumentException("Candidate terrain must match the initialized map geometry and height limits.");
            float bottom=terrain.transform.position.y,top=bottom+runtimeData.size.y;
            for(int z=0;z<candidate.Depth;z++) for(int x=0;x<candidate.Width;x++)
            {
                float height=candidate.GetHeight(x,z);
                if(!(height>=bottom && height<=top))
                    throw new System.ArgumentException("Generated heights exceed the Terrain vertical range.");
            }
        }
        public void InstallTerrain(HeightField candidate)
        {
            if(Field!=null && Field.IsFrozen) throw new System.InvalidOperationException("Cannot replace terrain during a shot.");
            ValidateCandidate(candidate);
            Field=candidate;
            Surface=new TerrainShotSurface(Field);
            UpdateRegion(new RectInt(0,0,Field.Width,Field.Depth));
        }
        public void ResetTerrain()
        {
            Initialize();
            if (Field == null) return;
            Field.Reset();
            UpdateRegion(new RectInt(0,0,Field.Width,Field.Depth));
        }
        public void UpdateRegion(RectInt region)
        {
            if(runtimeData==null || Field==null || region.width<=0 || region.height<=0) return;
            var patch=new float[region.height,region.width];
            for(int z=0;z<region.height;z++) for(int x=0;x<region.width;x++)
                patch[z,x]=Mathf.Clamp01((Field.GetHeight(region.x+x,region.y+z)-terrain.transform.position.y)/runtimeData.size.y);
            // Only the affected rectangle is uploaded. Collider updates are not used for gameplay.
            runtimeData.SetHeightsDelayLOD(region.x,region.y,patch);
            runtimeData.SyncHeightmap();
        }
        void OnDestroy()
        {
            if(terrain!=null && terrain.terrainData==runtimeData) terrain.terrainData=originalData;
            if(runtimeData!=null)
            {
                if(Application.isPlaying) Destroy(runtimeData); else DestroyImmediate(runtimeData);
            }
        }
    }
}
