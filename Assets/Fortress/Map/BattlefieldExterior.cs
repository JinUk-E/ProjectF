using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
namespace Fortress
{
    /// <summary>Scenery never expands the HeightField. Only the boundary wall has a physical collider, outside the playable rectangle.</summary>
    public sealed class BattlefieldExterior : MonoBehaviour
    {
        public MatchController match;
        public ExteriorSettings settings;
        public Mesh ApronMesh { get; private set; }
        public Mesh BoundaryWallMesh { get; private set; }
        public MeshCollider BoundaryCollider { get; private set; }
        static readonly int PlayableRectId=Shader.PropertyToID("_PlayableRect"),FogColorId=Shader.PropertyToID("_ExteriorFogColor"),FogRangeId=Shader.PropertyToID("_ExteriorFogRange");
        MaterialPropertyBlock fogProperties;
        public int Generation { get; private set; }=-1;
        public int BuildVersion { get; private set; }
        public int SeamVertexCount => edge!=null?edge.Length:0;
        MatchController subscribed;
        HeightField field;
        int revision=-1,builtRings;
        float builtWidth;
        float[] ringDistances;
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
            foreach(var mesh in ownedMeshes)if(mesh!=null)Destroy(mesh);ownedMeshes.Clear();ApronMesh=null;BoundaryWallMesh=null;BoundaryCollider=null;
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
            int count=edge.Length,rings=Mathf.Clamp(settings.ringSegments,2,24)+3;
            worldVertices=new Vector3[count*(rings+1)];var vertices=new Vector3[worldVertices.Length];var uv=new Vector2[vertices.Length];
            float ridgeWidth=Mathf.Max(4,settings.boundaryRidgeWidth);
            float shoulder=Mathf.Clamp(settings.boundaryShoulderDistance,.5f,ridgeWidth-.5f);
            float width=Mathf.Max(ridgeWidth+10,settings.apronWidth);builtWidth=width;builtRings=rings;
            ringDistances=new float[rings+1];ringDistances[0]=0;ringDistances[1]=Mathf.Clamp(settings.ridgeToeDistance,.1f,shoulder-.1f);ringDistances[2]=shoulder;ringDistances[3]=ridgeWidth;
            for(int r=4;r<=rings;r++)ringDistances[r]=Mathf.Lerp(ridgeWidth,width,(float)(r-3)/(rings-3));
            Vector2 center=new Vector2(field.Origin.x+field.Size.x*.5f,field.Origin.z+field.Size.y*.5f);
            for(int ring=0;ring<=rings;ring++)for(int i=0;i<count;i++)
            {
                float distance=ringDistances[ring],t=distance/width;var p=edge[i];
                p.x+=(p.x-center.x)/(field.Size.x*.5f)*t*width;
                p.z+=(p.z-center.y)/(field.Size.y*.5f)*t*width;
                if(ring>0)
                {
                    float broad=Mathf.PerlinNoise((edge[i].x+match.Seed*.013f)*.055f,(edge[i].z-match.Seed*.017f)*.055f);
                    float broken=Mathf.PerlinNoise((edge[i].x+213)*.23f,(edge[i].z-match.Seed*.01f)*.23f);
                    float minimum=Mathf.Max(1,settings.boundaryRidgeMinHeight),maximum=Mathf.Max(minimum,settings.boundaryRidgeMaxHeight);
                    float peak=Mathf.Lerp(minimum,maximum,Mathf.Clamp01((broad*.75f+broken*.25f-.28f)/.44f));
                    if(ring==1)p.y=edge[i].y+peak*Mathf.Clamp01(settings.toeHeightFactor);
                    else if(ring==2)p.y=edge[i].y+peak;
                    else if(ring==3)p.y=edge[i].y+peak*.65f;
                    else
                    {
                        float noise=Mathf.PerlinNoise((p.x+match.Seed*.013f)*.012f,(p.z-match.Seed*.017f)*.012f);
                        float target=Mathf.Max(field.WaterHeight+2,settings.ridgeHeight*(.2f+.8f*noise));
                        p.y=Mathf.Lerp(edge[i].y+peak*.65f,target,Mathf.SmoothStep(0,1,(distance-ridgeWidth)/(width-ridgeWidth)));
                    }
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
            // First three rings are rendered once as the rock boundary; the apron starts beyond them.
            var apronIndices=new int[triangles.Length-count*18];System.Array.Copy(triangles,count*18,apronIndices,0,apronIndices.Length);
            ApronMesh.triangles=apronIndices;
            var ground=new GameObject("Apron mesh");ground.transform.SetParent(root.transform,false);
            ground.AddComponent<MeshFilter>().sharedMesh=ApronMesh;var groundRenderer=ground.AddComponent<MeshRenderer>();groundRenderer.sharedMaterial=settings.groundMaterial;ApplyExteriorFog(groundRenderer);
            BuildBoundaryWall();BuildTrees(center,width);
        }
        Vector3 Point(int x,int z)=>new Vector3(field.Origin.x+x*field.CellX,field.GetHeight(x,z),field.Origin.z+z*field.CellZ);
        void ApplyExteriorFog(Renderer renderer)
        {
            if(fogProperties==null)fogProperties=new MaterialPropertyBlock();
            fogProperties.Clear();renderer.GetPropertyBlock(fogProperties);
            fogProperties.SetVector(PlayableRectId,new Vector4(field.Origin.x,field.Origin.z,field.Origin.x+field.Size.x,field.Origin.z+field.Size.y));
            fogProperties.SetColor(FogColorId,RenderSettings.fogColor);
            fogProperties.SetVector(FogRangeId,new Vector4(Mathf.Max(0,settings.fogStart),Mathf.Max(settings.fogStart+.01f,settings.fogEnd),0,0));
            renderer.SetPropertyBlock(fogProperties);
        }
        void BuildBoundaryWall()
        {
            int count=edge.Length;var vertices=new Vector3[count*4];var uv=new Vector2[vertices.Length];
            for(int i=0;i<vertices.Length;i++) {vertices[i]=transform.InverseTransformPoint(worldVertices[i]);uv[i]=new Vector2(worldVertices[i].x,worldVertices[i].z)/12f;}
            var indices=new int[count*18];System.Array.Copy(triangles,indices,indices.Length);
            BoundaryWallMesh=new Mesh {name="Irregular natural rock boundary",indexFormat=vertices.Length>65535?IndexFormat.UInt32:IndexFormat.UInt16};
            ownedMeshes.Add(BoundaryWallMesh);BoundaryWallMesh.vertices=vertices;BoundaryWallMesh.uv=uv;BoundaryWallMesh.triangles=indices;
            BoundaryWallMesh.RecalculateNormals();BoundaryWallMesh.RecalculateBounds();
            var rock=new GameObject("Natural rock boundary");rock.transform.SetParent(root.transform,false);
            rock.AddComponent<MeshFilter>().sharedMesh=BoundaryWallMesh;
            var renderer=rock.AddComponent<MeshRenderer>();renderer.sharedMaterial=settings.boundaryWallMaterial!=null?settings.boundaryWallMaterial:settings.groundMaterial;
            ApplyExteriorFog(renderer);BoundaryCollider=rock.AddComponent<MeshCollider>();BoundaryCollider.sharedMesh=BoundaryWallMesh;
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
                foreach(var renderer in tree.GetComponentsInChildren<Renderer>(true))ApplyExteriorFog(renderer);
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
            int ring=distance<ringDistances[1]?0:distance<ringDistances[2]?1:distance<ringDistances[3]?2:
                Mathf.Clamp(3+Mathf.FloorToInt((distance-ringDistances[3])/(builtWidth-ringDistances[3])*(builtRings-3)),3,builtRings-1);
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
