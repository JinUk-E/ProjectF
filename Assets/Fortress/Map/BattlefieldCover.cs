using System;
using UnityEngine;
namespace Fortress
{
    // Rules use analytic bounds; meshes are a replaceable view and have no Physics authority.
    public sealed class BattlefieldCover : MonoBehaviour
    {
        public CoverSettings settings;
        public CoverBuilding[] Buildings { get; private set; }=Array.Empty<CoverBuilding>();
        HeightField field;
        GameObject viewRoot;
        GameObject[] intact,ruins;
        public int Remaining { get { int count=0;foreach(var b in Buildings) if(!b.Destroyed) count++;return count; } }
        public CoverBuilding[] Prepare(HeightField map,int seed,SpawnArea[] spawns)
        {
            if(settings==null || settings.sites==null || !(settings.health>0) || !(settings.foundationDrop>0) ||
                !Finite(settings.health) || !Finite(settings.foundationDrop) || !Finite(settings.positionJitter) || settings.positionJitter<0 ||
                !Finite(settings.size.x)||!Finite(settings.size.y)||!Finite(settings.size.z)||
                settings.size.x<=0||settings.size.y<=0||settings.size.z<=0)
                throw new ArgumentException("Cover requires finite positive dimensions and health.");
            var random=new System.Random(unchecked(seed^0x371f));var result=new CoverBuilding[settings.sites.Length];
            for(int i=0;i<result.Length;i++)
            {
                var p=settings.sites[i]+new Vector2((float)random.NextDouble()*2-1,(float)random.NextDouble()*2-1)*settings.positionJitter;
                float min=float.PositiveInfinity,max=float.NegativeInfinity;
                for(int x=-1;x<=1;x++) for(int z=-1;z<=1;z++)
                {
                    if(!map.TrySample(p.x+x*settings.size.x*.5f,p.y+z*settings.size.z*.5f,out float h)||h<=map.WaterHeight)
                        throw new ArgumentException("Building site must be inside dry terrain.");
                    min=Mathf.Min(min,h);max=Mathf.Max(max,h);
                }
                var bounds=new Bounds(new Vector3(p.x,(min+max+settings.size.y)*.5f,p.y),new Vector3(settings.size.x,max-min+settings.size.y,settings.size.z));
                foreach(var spawn in spawns)
                    if(bounds.max.x+2>=spawn.min.x && bounds.min.x-2<=spawn.max.x && bounds.max.z+2>=spawn.min.y && bounds.min.z-2<=spawn.max.y)
                        throw new ArgumentException("Building overlaps a tank spawn zone.");
                for(int previous=0;previous<i;previous++) if(bounds.Intersects(result[previous].Bounds)) throw new ArgumentException("Buildings overlap.");
                result[i]=new CoverBuilding(bounds,settings.health);
            }
            return result;
        }
        static bool Finite(float v)=>!float.IsNaN(v)&&!float.IsInfinity(v);
        public void Install(HeightField map,CoverBuilding[] buildings)
        {
            if(field!=null&&field.IsFrozen) throw new InvalidOperationException("Cannot replace cover during a shot.");
            if(viewRoot!=null) {viewRoot.SetActive(false);Destroy(viewRoot);}
            field=map;Buildings=buildings;
            viewRoot=new GameObject("Battlefield buildings (runtime)");viewRoot.transform.SetParent(transform,false);
            intact=new GameObject[Buildings.Length];ruins=new GameObject[Buildings.Length];
            for(int i=0;i<Buildings.Length;i++) BuildView(i);
        }
        public bool TryHit(Vector3 from,Vector3 to,out Vector3 hit)
        {
            hit=default;bool found=false;float best=float.PositiveInfinity;
            foreach(var b in Buildings)
            {
                if(b.Destroyed||!CoverBuilding.Intersect(b.Bounds,from,to,out var p)) continue;
                float distance=(p-from).sqrMagnitude;if(distance<best) {found=true;best=distance;hit=p;}
            }
            return found;
        }
        public bool BlocksMovement(Vector3 from,Vector3 to,float radius)
        {
            foreach(var b in Buildings)
            {
                if(b.Destroyed) continue;
                var bounds=b.Bounds;bounds.Expand(new Vector3(radius*2,0,radius*2));
                from.y=to.y=bounds.center.y;
                if(CoverBuilding.Intersect(bounds,from,to,out _)) return true;
            }
            return false;
        }
        public bool Shields(Vector3 explosion,Vector3 target)
        {
            var direction=target-explosion;
            if(direction.sqrMagnitude<.0001f) return false;
            return TryHit(explosion+direction.normalized*.05f,target,out _);
        }
        public void ApplyExplosion(Vector3 point,float radius,float damage)
        {
            EnsureMutable();
            if(radius<=0||damage<=0) return;
            foreach(var b in Buildings)
            {
                if(b.Destroyed) continue;
                float distance=Vector3.Distance(point,b.Bounds.ClosestPoint(point));
                b.Damage(damage*Mathf.Clamp01(1-distance/radius));
            }
            RefreshViews();
        }
        public void SettleFoundations()
        {
            EnsureMutable();
            foreach(var b in Buildings)
                if(!b.Destroyed && field.TrySample(b.Bounds.center.x,b.Bounds.center.z,out float h) &&
                    (h<=field.WaterHeight || b.Bounds.min.y-h>settings.foundationDrop)) b.Collapse();
            RefreshViews();
        }
        void EnsureMutable()
        { if(field!=null&&field.IsFrozen) throw new InvalidOperationException("Cover stays fixed until the shot finishes."); }
        void RefreshViews()
        {
            for(int i=0;i<Buildings.Length;i++)
            {
                intact[i].SetActive(!Buildings[i].Destroyed);ruins[i].SetActive(Buildings[i].Destroyed);
                if(Buildings[i].Destroyed && field.TrySample(Buildings[i].Bounds.center.x,Buildings[i].Bounds.center.z,out float h))
                {var p=ruins[i].transform.position;p.y=h+.12f;ruins[i].transform.position=p;}
            }
        }
        void BuildView(int i)
        {
            var bounds=Buildings[i].Bounds;
            intact[i]=new GameObject("Depot "+(i+1));intact[i].transform.SetParent(viewRoot.transform,false);
            var root=intact[i].transform;
            Cube(root,"Concrete walls",bounds.center,bounds.size,settings.wallMaterial);
            Cube(root,"Roof",bounds.center+Vector3.up*(bounds.extents.y+.15f),new Vector3(bounds.size.x+.3f,.3f,bounds.size.z+.3f),settings.roofMaterial);
            float ground=bounds.max.y-settings.size.y;
            for(int side=-1;side<=1;side+=2)
            {
                for(int window=-1;window<=1;window+=2)
                    Cube(root,"Window",new Vector3(bounds.center.x+window*bounds.size.x*.28f,ground+2.6f,bounds.center.z+side*(bounds.extents.z+.02f)),new Vector3(.85f,.9f,.05f),settings.windowMaterial);
                Cube(root,"Door",new Vector3(bounds.center.x,ground+1.1f,bounds.center.z+side*(bounds.extents.z+.03f)),new Vector3(1.1f,2.2f,.06f),settings.roofMaterial);
            }
            Cube(root,"Roof vent",new Vector3(bounds.center.x+.8f,bounds.max.y+.5f,bounds.center.z),new Vector3(1,.7f,1),settings.roofMaterial);
            ruins[i]=new GameObject("Depot "+(i+1)+" rubble (passable)");ruins[i].transform.SetParent(viewRoot.transform,false);
            ruins[i].transform.position=new Vector3(bounds.center.x,bounds.min.y+.12f,bounds.center.z);
            for(int piece=0;piece<5;piece++)
            {
                var offset=new Vector3((piece%3-1)*1.2f,0,(piece/3-.5f)*1.4f);
                Cube(ruins[i].transform,"Debris",ruins[i].transform.position+offset,new Vector3(1,.24f,.7f),settings.rubbleMaterial);
            }
            ruins[i].SetActive(false);
        }
        static void Cube(Transform parent,string label,Vector3 position,Vector3 size,Material material)
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=label;go.transform.SetParent(parent,false);go.transform.position=position;go.transform.localScale=size;
            var collider=go.GetComponent<Collider>();collider.enabled=false;Destroy(collider);
            if(material!=null) go.GetComponent<Renderer>().sharedMaterial=material;
        }
    }
}
