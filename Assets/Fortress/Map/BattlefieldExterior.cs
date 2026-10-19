using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
namespace Fortress
{
    /// <summary>Outside scenery only. No collider and no change to the authoritative HeightField rectangle.</summary>
    public sealed class BattlefieldExterior : MonoBehaviour
    {
        public MatchController match;
        public ExteriorSettings settings;
        public Mesh ApronMesh { get; private set; }
        public int Generation { get; private set; }=-1;
        public int BuildVersion { get; private set; }
        public int SeamVertexCount => edge!=null?edge.Length:0;
        MatchController subscribed;
        HeightField field;
        int revision=-1,builtRings;
        float builtWidth;
        Vector3[] edge,worldVertices;
        int[] triangles;
        GameObject root;
        readonly List<Mesh> ownedMeshes=new List<Mesh>();
        void OnEnable() { Subscribe();Refresh(); }
        void Start() { Subscribe();Refresh(); }
        void OnDisable() { Unsubscribe(); }
        void OnDestroy() { Clear(); }
        public void Configure(MatchController target,ExteriorSettings style)
        { Unsubscribe();Clear();match=target;settings=style;field=null;Generation=-1;revision=-1;if(isActiveAndEnabled)Subscribe();Refresh(); }
        void Subscribe() { Unsubscribe();if(match!=null) {subscribed=match;subscribed.StateChanged+=Refresh;} }
        void Unsubscribe() { if(subscribed!=null)subscribed.StateChanged-=Refresh;subscribed=null; }
        public void Refresh()
        {
            if(!isActiveAndEnabled || match==null || settings==null || match.Tanks==null || match.terrainMap==null || match.terrainMap.Field==null)return;
            var current=match.terrainMap.Field;if(current.IsFrozen)return;
            bool newMap=field!=current || Generation!=match.Generation;
            if(!newMap && revision==current.Revision)return;
            field=current;revision=field.Revision;Generation=match.Generation;
            if(!newMap && edge!=null)
            {
                bool changed=false;
                foreach(var p in edge)if(field.TrySample(p.x,p.z,out float h) && Mathf.Abs(h-p.y)>.00001f) {changed=true;break;}
                if(!changed)return;
            }
            Build();
        }
        void Clear()
        {
            if(root!=null) {root.SetActive(false);Destroy(root);root=null;}
            foreach(var mesh in ownedMeshes)if(mesh!=null)Destroy(mesh);ownedMeshes.Clear();ApronMesh=null;
        }
        void Build()
        {
            Clear();BuildVersion++;
            root=new GameObject("Exterior scenery (non-combat)");root.transform.SetParent(transform,false);
            int w=field.Width,d=field.Depth;
            edge=new Vector3[2*(w-1)+2*(d-1)];int at=0;
            for(int x=0;x<w-1;x++)edge[at++]=Point(x,0);
            for(int z=0;z<d-1;z++)edge[at++]=Point(w-1,z);
            for(int x=w-1;x>0;x--)edge[at++]=Point(x,d-1);
            for(int z=d-1;z>0;z--)edge[at++]=Point(0,z);
            int count=edge.Length,rings=Mathf.Clamp(settings.ringSegments,2,24);
            worldVertices=new Vector3[count*(rings+1)];var vertices=new Vector3[worldVertices.Length];var uv=new Vector2[vertices.Length];
            float width=Mathf.Max(10,settings.apronWidth);builtWidth=width;builtRings=rings;
            Vector2 center=new Vector2(field.Origin.x+field.Size.x*.5f,field.Origin.z+field.Size.y*.5f);
            for(int ring=0;ring<=rings;ring++)for(int i=0;i<count;i++)
            {
                float t=(float)ring/rings;var p=edge[i];
                p.x+=(p.x-center.x)/(field.Size.x*.5f)*t*width;
                p.z+=(p.z-center.y)/(field.Size.y*.5f)*t*width;
                if(ring>0)
                {
                    float noise=Mathf.PerlinNoise((p.x+match.Seed*.013f)*.012f,(p.z-match.Seed*.017f)*.012f);
                    float target=Mathf.Max(field.WaterHeight+2,settings.ridgeHeight*(.2f+.8f*noise));
                    p.y=Mathf.Lerp(edge[i].y,target,Mathf.SmoothStep(0,1,t));
                }
                int index=ring*count+i;worldVertices[index]=p;vertices[index]=transform.InverseTransformPoint(p);uv[index]=new Vector2(p.x,p.z)/12f;
            }
            triangles=new int[count*rings*6];at=0;
            for(int ring=0;ring<rings;ring++)for(int i=0;i<count;i++)
            {
                int next=(i+1)%count,a=ring*count+i,b=ring*count+next,c=(ring+1)*count+i,e=(ring+1)*count+next;
                triangles[at++]=a;triangles[at++]=c;triangles[at++]=b;triangles[at++]=b;triangles[at++]=c;triangles[at++]=e;
            }
            ApronMesh=new Mesh {name="Seam-matched exterior apron",indexFormat=vertices.Length>65535?IndexFormat.UInt32:IndexFormat.UInt16};
            ownedMeshes.Add(ApronMesh);ApronMesh.vertices=vertices;ApronMesh.uv=uv;ApronMesh.triangles=triangles;ApronMesh.RecalculateNormals();ApronMesh.RecalculateBounds();
            // Correct winding if the perimeter traversal convention changes; scenery must face upward.
            if(ApronMesh.normals.Length>0 && ApronMesh.normals[0].y<0)
            {
                for(int i=0;i<triangles.Length;i+=3){int swap=triangles[i+1];triangles[i+1]=triangles[i+2];triangles[i+2]=swap;}
                ApronMesh.triangles=triangles;ApronMesh.RecalculateNormals();
            }
            var ground=new GameObject("Apron mesh");ground.transform.SetParent(root.transform,false);
            ground.AddComponent<MeshFilter>().sharedMesh=ApronMesh;ground.AddComponent<MeshRenderer>().sharedMaterial=settings.groundMaterial;
            BuildBoundary();BuildTrees(center,width);
        }
        Vector3 Point(int x,int z)=>new Vector3(field.Origin.x+x*field.CellX,field.GetHeight(x,z),field.Origin.z+z*field.CellZ);
        void BuildBoundary()
        {
            var cableObject=new GameObject("Playable boundary cable");cableObject.transform.SetParent(root.transform,false);
            var line=cableObject.AddComponent<LineRenderer>();line.useWorldSpace=true;line.positionCount=edge.Length+1;
            line.startWidth=line.endWidth=Mathf.Max(.005f,settings.cableWidth);line.sharedMaterial=settings.boundaryMaterial;
            line.shadowCastingMode=ShadowCastingMode.Off;line.receiveShadows=false;
            float distance=Mathf.Max(1,settings.boundaryPostSpacing);
            for(int i=0;i<edge.Length;i++)
            {
                line.SetPosition(i,edge[i]+Vector3.up*Mathf.Max(0,settings.boundaryCableHeight));
                if(i>0)distance+=new Vector2(edge[i].x-edge[i-1].x,edge[i].z-edge[i-1].z).magnitude;
                if(distance<Mathf.Max(1,settings.boundaryPostSpacing))continue;distance=0;
                var post=GameObject.CreatePrimitive(PrimitiveType.Cube);post.name="Boundary marker";post.transform.SetParent(root.transform,false);
                float h=Mathf.Max(.1f,settings.boundaryPostHeight);post.transform.position=edge[i]+Vector3.up*h*.5f;post.transform.localScale=new Vector3(.14f,h,.14f);
                var collider=post.GetComponent<Collider>();collider.enabled=false;Destroy(collider);
                post.GetComponent<MeshRenderer>().sharedMaterial=settings.boundaryMaterial;
            }
            line.SetPosition(edge.Length,edge[0]+Vector3.up*Mathf.Max(0,settings.boundaryCableHeight));
        }
        void BuildTrees(Vector2 center,float width)
        {
            if(settings.treePrefab==null)return;
            var random=new System.Random(unchecked(match.Seed^0x7a38d));
            float min=Mathf.Clamp(settings.treeMinDistance,0,width),max=Mathf.Clamp(settings.treeMaxDistance,min,width);
            for(int i=0;i<Mathf.Clamp(settings.treeCount,0,500);i++)
            {
                float distance=Mathf.Lerp(min,max,(float)random.NextDouble());int side=random.Next(4);
                float nx=side==1?1:side==3?-1:(float)random.NextDouble()*2-1;
                float nz=side==0?-1:side==2?1:(float)random.NextDouble()*2-1;
                float x=center.x+nx*(field.Size.x*.5f+distance),z=center.y+nz*(field.Size.y*.5f+distance);
                if(!TrySampleVisualHeight(x,z,out float height))continue;
                var tree=Instantiate(settings.treePrefab,new Vector3(x,height,z),Quaternion.Euler(0,(float)random.NextDouble()*360,0),root.transform);
                float small=Mathf.Max(.01f,settings.treeScaleMin),large=Mathf.Max(small,settings.treeScaleMax);
                tree.transform.localScale*=Mathf.Lerp(small,large,(float)random.NextDouble());
                foreach(var collider in tree.GetComponentsInChildren<Collider>(true)){collider.enabled=false;Destroy(collider);}
                tree.SetActive(true);
            }
        }
        // Constant-time quad lookup, also safe for the orbit camera's terrain clearance samples.
        public bool TrySampleVisualHeight(float x,float z,out float height)
        {
            height=0;if(worldVertices==null || field==null)return false;
            float cx=field.Origin.x+field.Size.x*.5f,cz=field.Origin.z+field.Size.y*.5f;
            float ox=Mathf.Abs(x-cx)-field.Size.x*.5f,oz=Mathf.Abs(z-cz)-field.Size.y*.5f;
            float distance=Mathf.Max(ox,oz);
            if(distance<-.00001f || distance>builtWidth)return false;
            distance=Mathf.Max(0,distance);
            int w=field.Width-1,d=field.Depth-1,index;
            if(oz>=ox)
            {
                float nx=(x-cx)/(field.Size.x*.5f+distance);
                index=z<cz?Mathf.Clamp(Mathf.FloorToInt((nx+1)*.5f*w),0,w-1)
                    :w+d+Mathf.Clamp(Mathf.FloorToInt((1-nx)*.5f*w),0,w-1);
            }
            else
            {
                float nz=(z-cz)/(field.Size.y*.5f+distance);
                index=x>cx?w+Mathf.Clamp(Mathf.FloorToInt((nz+1)*.5f*d),0,d-1)
                    :2*w+d+Mathf.Clamp(Mathf.FloorToInt((1-nz)*.5f*d),0,d-1);
            }
            int ring=Mathf.Clamp(Mathf.FloorToInt(distance/builtWidth*builtRings),0,builtRings-1);
            int count=edge.Length,next=(index+1)%count,a=ring*count+index,b=ring*count+next,c=(ring+1)*count+index,e=(ring+1)*count+next;
            return TriangleHeight(x,z,a,c,b,out height)||TriangleHeight(x,z,b,c,e,out height);
        }
        bool TriangleHeight(float x,float z,int ia,int ib,int ic,out float height)
        {
            height=0;var a=worldVertices[ia];var b=worldVertices[ib];var c=worldVertices[ic];
            var v0=new Vector2(b.x-a.x,b.z-a.z);var v1=new Vector2(c.x-a.x,c.z-a.z);var v2=new Vector2(x-a.x,z-a.z);
            float det=v0.x*v1.y-v1.x*v0.y;if(Mathf.Abs(det)<.000001f)return false;
            float u=(v2.x*v1.y-v1.x*v2.y)/det,v=(v0.x*v2.y-v2.x*v0.y)/det;
            if(u<-.00001f || v<-.00001f || u+v>1.00001f)return false;
            height=a.y+(b.y-a.y)*u+(c.y-a.y)*v;return true;
        }
    }
}
