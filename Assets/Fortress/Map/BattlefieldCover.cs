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
        GameObject[][] damageViews;
        CoverCollapseView[] collapses;
        public bool IsCollapsing(int index) => collapses!=null && collapses[index].Playing;
        public float CollapseProgress(int index) => collapses==null?0:collapses[index].Progress;
        public int DamageStage(int index) => Buildings[index].Destroyed?3:Buildings[index].Health<=settings.health*.35f?2:Buildings[index].Health<settings.health?1:0;
        void Update() { TickPresentation(Time.deltaTime); }
        public void TickPresentation(float deltaTime) { if(collapses!=null)foreach(var view in collapses)if(view!=null)view.Tick(deltaTime); }
        void OnDisable() { if(collapses!=null)foreach(var view in collapses)if(view!=null)view.FinishAndClearDust(); }
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
            collapses=new CoverCollapseView[Buildings.Length];damageViews=new GameObject[Buildings.Length][];
            for(int i=0;i<Buildings.Length;i++) BuildView(i);
        }
        public bool TryHit(Vector3 from,Vector3 to,out Vector3 hit)
        {
            hit=default;bool found=false;float best=float.PositiveInfinity;
            foreach(var b in Buildings)
            {
                if(b.Destroyed)
                {
                    if(!b.HasDryRubble || !CoverBuilding.Intersect(b.RubbleBounds,from,to,out _))continue;
                    foreach(var piece in b.Rubble) if(piece.TryHit(from,to,out var rubbleHit))
                    {float d=(rubbleHit-from).sqrMagnitude;if(d<best){found=true;best=d;hit=rubbleHit;}}
                    continue;
                }
                if(!CoverBuilding.Intersect(b.Bounds,from,to,out var p)) continue;
                float distance=(p-from).sqrMagnitude;if(distance<best) {found=true;best=distance;hit=p;}
            }
            return found;
        }
        public bool BlocksMovement(Vector3 from,Vector3 to,float radius)
        {
            foreach(var b in Buildings)
            {
                if(b.Destroyed)
                {
                    foreach(var piece in b.Rubble)if(piece.BlocksMovement(from,to,radius))return true;
                    continue;
                }
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
                if(Buildings[i].Destroyed)
                {
                    Buildings[i].SettleRubble(field);
                    collapses[i].Begin(Buildings[i].Bounds.center);collapses[i].RefreshGround();
                }
                else
                {
                    int stage=DamageStage(i);
                    for(int mark=0;mark<damageViews[i].Length;mark++)damageViews[i][mark].SetActive(stage>mark);
                }
            }
        }
        void BuildView(int i)
        {
            var bounds=Buildings[i].Bounds;
            intact[i]=new GameObject("Depot "+(i+1));intact[i].transform.SetParent(viewRoot.transform,false);
            var root=intact[i].transform;
            int variant=i%3;root.name=variant==0?"Maintenance hangar":variant==1?"Ammunition store":"Communications post";
            // Every intact detail stays inside the authoritative AABB. The roof reserves space
            // for vents and aerials so visual cover never promises protection outside the bounds.
            var c=bounds.center;var size=bounds.size;float top=bounds.max.y,baseY=bounds.min.y;
            float roofSpace=Mathf.Min(.8f,size.y*.2f),wallHeight=size.y-roofSpace;
            for(int side=-1;side<=1;side+=2)for(int row=0;row<2;row++)for(int column=0;column<3;column++)
            {
                float y=baseY+(row+.5f)*wallHeight/2;
                Cube(root,"Concrete wall panel",new Vector3(c.x+(column-1)*size.x*.94f/3,y,c.z+side*size.z*.45f),new Vector3(size.x*.94f/3,wallHeight/2,size.z*.04f),settings.wallMaterial);
                Cube(root,"Concrete side panel",new Vector3(c.x+side*size.x*.45f,y,c.z+(column-1)*size.z*.86f/3),new Vector3(size.x*.04f,wallHeight/2,size.z*.86f/3),settings.wallMaterial);
            }
            for(int rx=0;rx<3;rx++)for(int rz=0;rz<2;rz++)
                Cube(root,"Steel roof panel",new Vector3(c.x+(rx-1)*size.x/3,top-roofSpace+.1f,c.z+(rz-.5f)*size.z/2),new Vector3(size.x/3,.2f,size.z/2),settings.roofMaterial);
            for(int rib=0;rib<7;rib++)
            {
                float x=Mathf.Lerp(bounds.min.x+.12f,bounds.max.x-.12f,rib/6f);
                Cube(root,"Roof seam",new Vector3(x,top-roofSpace+.23f,c.z),new Vector3(.045f,.06f,size.z*.94f),settings.roofMaterial);
            }
            for(int side=-1;side<=1;side+=2)
            {
                float faceZ=c.z+side*(bounds.extents.z-.04f);
                for(int column=-1;column<=1;column++)
                    Cube(root,"Concrete buttress",new Vector3(c.x+column*(bounds.extents.x-.12f),baseY+wallHeight*.5f,faceZ-side*.04f),new Vector3(.24f,wallHeight,.16f),settings.wallMaterial);
                float doorHeight=Mathf.Min(2.25f,wallHeight*.75f),doorWidth=size.x*(variant==0?.44f:variant==1?.24f:.18f);
                Cube(root,"Steel rolling shutter",new Vector3(c.x,baseY+doorHeight*.5f,faceZ),new Vector3(doorWidth,doorHeight,.06f),settings.roofMaterial);
                for(int slat=1;slat<9;slat++)
                    Cube(root,"Shutter horizontal seam",new Vector3(c.x,baseY+slat*doorHeight/9,faceZ+side*.015f),new Vector3(doorWidth*.96f,.025f,.025f),settings.windowMaterial);
                for(int window=-1;window<=1;window+=2)
                {
                    if(variant==0 || (variant==1 && window<0))continue;
                    float x=c.x+window*size.x*.29f,y=baseY+wallHeight*.65f;
                    Cube(root,"Recessed dark window",new Vector3(x,y,faceZ),new Vector3(size.x*(variant==1?.09f:.18f),wallHeight*.24f,.05f),settings.windowMaterial);
                    Cube(root,"Window mullion",new Vector3(x,y,faceZ+side*.015f),new Vector3(.045f,wallHeight*.24f,.025f),settings.roofMaterial);
                    Cube(root,"Window lintel",new Vector3(x,y+wallHeight*.13f,faceZ-side*.01f),new Vector3(size.x*.2f,.09f,.07f),settings.wallMaterial);
                }
                Cube(root,"Rain downpipe",new Vector3(c.x+size.x*.4f,baseY+wallHeight*.5f,faceZ),new Vector3(.065f,wallHeight,.065f),settings.roofMaterial);
            }
            float roofBase=top-roofSpace+.2f;
            Cube(root,"Vent housing",new Vector3(c.x+size.x*.22f,roofBase+roofSpace*.22f,c.z+size.z*.18f),new Vector3(size.x*.16f,roofSpace*.4f,size.z*.2f),settings.roofMaterial);
            for(int louver=0;louver<4;louver++)
                Cube(root,"Vent louver",new Vector3(c.x+size.x*.22f,roofBase+roofSpace*.44f,c.z+size.z*(.11f+louver*.04f)),new Vector3(size.x*.14f,.025f,.035f),settings.windowMaterial);
            if(variant==2) Cube(root,"Radio aerial",new Vector3(c.x-size.x*.32f,roofBase+(top-roofBase)*.45f,c.z-size.z*.3f),new Vector3(.035f,(top-roofBase)*.9f,.035f),settings.roofMaterial);
            // Distinct low-cost identification plates: one/two/three bright bars, no extra text atlas.
            float plateX=c.x+size.x*.32f,plateY=baseY+wallHeight*.25f,plateZ=bounds.min.z+.025f;
            Cube(root,"Facility identification plate",new Vector3(plateX,plateY,plateZ),new Vector3(size.x*.18f,wallHeight*.15f,.04f),settings.windowMaterial);
            for(int bar=0;bar<=variant;bar++)
                Cube(root,"Facility identification stripe",new Vector3(plateX+(bar-variant*.5f)*size.x*.04f,plateY,plateZ-.015f),new Vector3(size.x*.02f,wallHeight*.11f,.015f),settings.wallMaterial);
            ruins[i]=new GameObject("Depot "+(i+1)+" rubble (low cover)");ruins[i].transform.SetParent(viewRoot.transform,false);
            Buildings[i].PrepareRubble(field,settings,i);
            collapses[i]=new CoverCollapseView(intact[i],ruins[i],field,settings,i,Buildings[i].Rubble);
            damageViews[i]=new GameObject[2];
            for(int stage=0;stage<2;stage++)
            {
                var marks=new GameObject(stage==0?"Impact scars":"Severe concrete cracks");marks.transform.SetParent(root,false);damageViews[i][stage]=marks;
                for(int side=-1;side<=1;side+=2)for(int mark=0;mark<4;mark++)
                {
                    float x=c.x+(mark-1.5f)*size.x*.19f,y=baseY+wallHeight*(.2f+.16f*((mark+stage)%4));
                    Cube(marks.transform,"Spalled concrete",new Vector3(x,y,c.z+side*(size.z*.47f+.002f)),new Vector3(size.x*.09f,.13f+.08f*stage,.012f),settings.damageMaterial!=null?settings.damageMaterial:settings.windowMaterial);
                    for(int crack=0;crack<3;crack++)
                        Cube(marks.transform,"Concrete fracture",new Vector3(x+(crack-1)*.09f,y+(crack-1)*.11f,c.z+side*(size.z*.47f+.009f)),new Vector3(.04f,.17f,.009f),settings.damageMaterial!=null?settings.damageMaterial:settings.windowMaterial);
                }
                marks.SetActive(false);
            }
        }
        static void Cube(Transform parent,string label,Vector3 position,Vector3 size,Material material)
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=label;go.transform.SetParent(parent,false);go.transform.position=position;go.transform.localScale=size;
            var collider=go.GetComponent<Collider>();collider.enabled=false;Destroy(collider);
            if(material!=null) go.GetComponent<Renderer>().sharedMaterial=material;
        }
    }
}
