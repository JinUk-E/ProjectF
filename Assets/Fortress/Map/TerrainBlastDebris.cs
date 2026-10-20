using UnityEngine;
namespace Fortress
{
    /// <summary>Bounded cosmetic soil fragments. The actual low berm lives in HeightField.</summary>
    public sealed class TerrainBlastDebris : MonoBehaviour
    {
        public MatchController match;
        public TerrainBlastSettings settings;
        public int ActiveCount { get; private set; }
        public int Generation { get; private set; }=-1;
        struct Fragment
        {
            public Transform view;
            public Vector3 start,end;
            public Quaternion rotation;
            public float elapsed,duration,arc,scale;
            public bool active;
        }
        Fragment[] fragments;
        MatchController subscribed;
        HeightField field;
        int cursor,sequence,revision=-1;
        void OnEnable() { Subscribe();Refresh(); }
        void Start() { Subscribe();Refresh(); }
        void OnDisable() { Unsubscribe(); }
        void Subscribe()
        {
            Unsubscribe();if(match==null)return;
            subscribed=match;subscribed.StateChanged+=Refresh;subscribed.TerrainImpactApplied+=Emit;
        }
        void Unsubscribe()
        {
            if(subscribed!=null){subscribed.StateChanged-=Refresh;subscribed.TerrainImpactApplied-=Emit;}
            subscribed=null;
        }
        void EnsurePool()
        {
            if(fragments!=null || settings==null || settings.fragmentMesh==null || settings.fragmentMaterial==null)return;
            fragments=new Fragment[Mathf.Clamp(settings.capacity,16,512)];
            for(int i=0;i<fragments.Length;i++)
            {
                var go=new GameObject("Soil fragment");go.transform.SetParent(transform,false);
                go.AddComponent<MeshFilter>().sharedMesh=settings.fragmentMesh;
                var renderer=go.AddComponent<MeshRenderer>();renderer.sharedMaterial=settings.fragmentMaterial;
                renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
                go.SetActive(false);fragments[i].view=go.transform;
            }
        }
        public void Refresh()
        {
            if(match==null || match.terrainMap==null || match.terrainMap.Field==null)return;
            var next=match.terrainMap.Field;if(next.IsFrozen)return;
            EnsurePool();
            if(Generation!=match.Generation || field!=next)
            {
                Clear();Generation=match.Generation;field=next;revision=next.Revision;return;
            }
            if(revision==next.Revision || fragments==null)return;
            revision=next.Revision;
            for(int i=0;i<fragments.Length;i++)
            {
                if(!fragments[i].active)continue;
                ref var p=ref fragments[i];
                if(!next.TrySample(p.end.x,p.end.z,out float h) || h<=next.WaterHeight)
                {p.active=false;p.view.gameObject.SetActive(false);ActiveCount--;continue;}
                p.end.y=h+p.scale*.2f;
                if(p.elapsed>=p.duration)p.view.position=p.end;
            }
        }
        public void Clear()
        {
            if(fragments!=null)for(int i=0;i<fragments.Length;i++)
            { fragments[i].active=false;if(fragments[i].view!=null)fragments[i].view.gameObject.SetActive(false); }
            ActiveCount=0;cursor=0;sequence=0;
        }
        public void Emit(Vector3 position,float radius)
        {
            Refresh();if(fragments==null || field==null || field.IsFrozen || !(radius>0))return;
            var random=new System.Random(unchecked(match.Seed*397+sequence++*7919));
            for(int i=0;i<Mathf.Clamp(settings.piecesPerBlast,3,32);i++)
            {
                float angle=(float)random.NextDouble()*Mathf.PI*2;
                float distance=radius*Mathf.Lerp(.85f,Mathf.Max(1,settings.scatterRadius),(float)random.NextDouble());
                var end=position+new Vector3(Mathf.Cos(angle)*distance,0,Mathf.Sin(angle)*distance);
                if(!field.TrySample(end.x,end.z,out float h) || h<=field.WaterHeight)continue;
                ref var p=ref fragments[cursor];cursor=(cursor+1)%fragments.Length;
                if(!p.active)ActiveCount++;
                p.active=true;p.scale=Mathf.Lerp(settings.fragmentSize.x,settings.fragmentSize.y,(float)random.NextDouble());
                end.y=h+p.scale*.2f;p.start=position;p.end=end;p.elapsed=0;
                p.duration=Mathf.Max(.05f,Mathf.Lerp(settings.airtime.x,settings.airtime.y,(float)random.NextDouble()));
                p.arc=Mathf.Max(0,settings.arcHeight)*Mathf.Lerp(.5f,1,(float)random.NextDouble());
                p.rotation=Quaternion.Euler((float)random.NextDouble()*180,angle*Mathf.Rad2Deg,(float)random.NextDouble()*180);
                p.view.localScale=new Vector3(p.scale,p.scale*.65f,p.scale*.85f);
                p.view.SetPositionAndRotation(position,p.rotation);p.view.gameObject.SetActive(true);
            }
        }
        void Update()
        {
            if(fragments==null || (match!=null && match.shots!=null && match.shots.GameplaySuspended))return;
            for(int i=0;i<fragments.Length;i++)
            {
                ref var p=ref fragments[i];if(!p.active || p.elapsed>=p.duration)continue;
                p.elapsed=Mathf.Min(p.duration,p.elapsed+Time.deltaTime);float t=p.elapsed/p.duration;
                p.view.SetPositionAndRotation(Vector3.Lerp(p.start,p.end,t)+Vector3.up*(4*p.arc*t*(1-t)),
                    p.rotation*Quaternion.Euler(180*t,90*t,0));
            }
        }
    }
}
