using System.Collections.Generic;
using UnityEngine;
namespace Fortress
{
    /// <summary>Seeded decoration only: no projectile surface, movement blocker or explosion cover.</summary>
    public sealed class BattlefieldVegetation : MonoBehaviour
    {
        public MatchController match;
        public VegetationSettings settings;
        public int Generation { get; private set; }=-1;
        public int ActiveCount { get; private set; }
        readonly List<Plant> plants=new List<Plant>();
        struct Plant { public GameObject view;public Vector3 position; }
        MatchController subscribedMatch;
        HeightField field;
        int revision=-1;
        GameObject root;
        void OnEnable() { Subscribe();Refresh(); }
        void Start() { Subscribe();Refresh(); }
        void OnDisable() { Unsubscribe(); }
        public void Configure(MatchController target,VegetationSettings style)
        {
            Unsubscribe();Clear();match=target;settings=style;Generation=-1;field=null;revision=-1;
            if(isActiveAndEnabled) Subscribe();Refresh();
        }
        void Subscribe()
        {
            Unsubscribe();if(match==null)return;subscribedMatch=match;subscribedMatch.StateChanged+=Refresh;
        }
        void Unsubscribe()
        {
            if(subscribedMatch!=null)subscribedMatch.StateChanged-=Refresh;subscribedMatch=null;
        }
        public void Refresh()
        {
            if(!isActiveAndEnabled || match==null || settings==null || match.Tanks==null || match.terrainMap==null || match.terrainMap.Field==null) return;
            var current=match.terrainMap.Field;if(current.IsFrozen)return;
            if(Generation!=match.Generation || field!=current)
            {
                Clear();field=current;Generation=match.Generation;revision=field.Revision;
                root=new GameObject("Vegetation (decorative, no cover)");root.transform.SetParent(transform,false);
                var random=new System.Random(unchecked(match.Seed^0x42f51b));
                Scatter(settings.treePrefab,Mathf.Clamp(settings.treeCount,0,500),random);
                Scatter(settings.shrubPrefab,Mathf.Clamp(settings.shrubCount,0,500),random);
                return;
            }
            if(revision==field.Revision)return;
            revision=field.Revision;
            for(int i=0;i<plants.Count;i++)
            {
                var plant=plants[i];if(plant.view==null || !plant.view.activeSelf)continue;
                var p=plant.position;
                if(!field.TrySample(p.x,p.z,out float h) || h<=field.WaterHeight || p.y-h>=Mathf.Max(.01f,settings.destructionDrop) || !Gentle(p.x,p.z))
                { plant.view.SetActive(false);ActiveCount--;continue; }
                p.y=h;plant.view.transform.position=p;plant.position=p;plants[i]=plant;
            }
        }
        void Scatter(GameObject prefab,int count,System.Random random)
        {
            if(prefab==null)return;
            float spacing=Mathf.Max(.1f,settings.minSpacing),margin=spacing*.5f;
            if(field.Size.x<=margin*2 || field.Size.y<=margin*2)return;
            for(int i=0;i<count;i++)for(int attempt=0;attempt<Mathf.Clamp(settings.attemptsPerInstance,1,100);attempt++)
            {
                float x=field.Origin.x+margin+(float)random.NextDouble()*(field.Size.x-margin*2);
                float z=field.Origin.z+margin+(float)random.NextDouble()*(field.Size.y-margin*2);
                if(!field.TrySample(x,z,out float h) || h<=field.WaterHeight || !Allowed(x,z) || !Gentle(x,z))continue;
                var position=new Vector3(x,h,z);bool crowded=false;
                foreach(var plant in plants)
                    if(new Vector2(plant.position.x-x,plant.position.z-z).sqrMagnitude<spacing*spacing) { crowded=true;break; }
                if(crowded)continue;
                var view=Instantiate(prefab,position,Quaternion.Euler(0,(float)random.NextDouble()*360,0),root.transform);
                float min=Mathf.Max(.01f,settings.minScale),max=Mathf.Max(min,settings.maxScale);
                view.transform.localScale*=Mathf.Lerp(min,max,(float)random.NextDouble());
                // Defensive sanitation for replacement art; renderers are the only intended authority.
                foreach(var collider in view.GetComponentsInChildren<Collider>(true)) { collider.enabled=false;Destroy(collider); }
                foreach(var body in view.GetComponentsInChildren<Rigidbody>(true)) { body.isKinematic=true;body.detectCollisions=false; }
                view.SetActive(true);plants.Add(new Plant{view=view,position=position});ActiveCount++;break;
            }
        }
        bool Allowed(float x,float z)
        {
            var mountain=match.tuning!=null?match.tuning.mountainTerrain:null;
            if(mountain!=null && mountain.tacticalHills && TacticalTerrain.RoadDistance(x,z,mountain)<mountain.roadHalfWidth+Mathf.Max(0,settings.roadClearance))return false;
            if(match.spawnAreas!=null)foreach(var spawn in match.spawnAreas)
            {
                if(spawn==null)continue;float clearance=Mathf.Max(0,settings.spawnClearance);
                if(x>=spawn.min.x-clearance && x<=spawn.max.x+clearance && z>=spawn.min.y-clearance && z<=spawn.max.y+clearance)return false;
            }
            if(match.cover!=null)foreach(var building in match.cover.Buildings)
            {
                var b=building.Bounds;float clearance=Mathf.Max(0,settings.buildingClearance);
                if(x>=b.min.x-clearance && x<=b.max.x+clearance && z>=b.min.z-clearance && z<=b.max.z+clearance)return false;
            }
            return true;
        }
        bool Gentle(float x,float z)
        {
            float dx=field.CellX,dz=field.CellZ;
            if(!field.TrySample(x-dx,z,out float left)||!field.TrySample(x+dx,z,out float right)||
                !field.TrySample(x,z-dz,out float back)||!field.TrySample(x,z+dz,out float front))return false;
            float sx=(right-left)/(2*dx),sz=(front-back)/(2*dz);
            return Mathf.Atan(Mathf.Sqrt(sx*sx+sz*sz))*Mathf.Rad2Deg<=Mathf.Clamp(settings.maxSlopeDegrees,0,80);
        }
        void Clear()
        {
            if(root!=null) { root.SetActive(false);Destroy(root); }
            plants.Clear();ActiveCount=0;
        }
    }
}
