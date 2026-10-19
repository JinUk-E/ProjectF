using UnityEngine;
namespace Fortress
{
    // Pure presentation state; constructed once per map, never consulted by combat or movement.
    internal sealed class CoverCollapseView
    {
        struct Fragment
        {
            public Transform transform;
            public Vector3 start,target,startScale,targetScale;
            public Quaternion startRotation,targetRotation;
            public float clearance;
        }
        readonly Fragment[] fragments;
        readonly RubblePiece[] rubble;
        readonly HeightField field;
        readonly CoverSettings settings;
        readonly GameObject intact,ruins;
        readonly ParticleSystem dust;
        float elapsed;
        public bool Started { get; private set; }
        public bool Playing { get; private set; }
        public float Progress { get; private set; }
        public CoverCollapseView(GameObject source,GameObject debris,HeightField map,CoverSettings tuning,int seed,RubblePiece[] structuralRubble)
        {
            intact=source;ruins=debris;field=map;settings=tuning;rubble=structuralRubble;
            var meshes=source.GetComponentsInChildren<MeshFilter>();fragments=new Fragment[meshes.Length];
            var random=new System.Random(unchecked(seed*397^0x53cf));
            for(int i=0;i<meshes.Length;i++)
            {
                var mesh=meshes[i];var renderer=mesh.GetComponent<MeshRenderer>();
                var piece=new GameObject(mesh.name+" fragment");piece.transform.SetParent(debris.transform,false);
                piece.transform.SetPositionAndRotation(mesh.transform.position,mesh.transform.rotation);piece.transform.localScale=mesh.transform.lossyScale;
                piece.AddComponent<MeshFilter>().sharedMesh=mesh.sharedMesh;
                var view=piece.AddComponent<MeshRenderer>();view.sharedMaterials=renderer.sharedMaterials;
                view.shadowCastingMode=renderer.shadowCastingMode;view.receiveShadows=renderer.receiveShadows;
                var size=renderer.bounds.size;float yaw=(float)random.NextDouble()*360;
                Quaternion flat=size.y<=size.x && size.y<=size.z?Quaternion.identity:size.z<=size.x?Quaternion.Euler(90,0,0):Quaternion.Euler(0,0,90);
                Vector3 target=mesh.transform.position;
                float spread=Mathf.Max(0,settings.collapseSpread);
                target.x+=((float)random.NextDouble()*2-1)*spread;target.z+=((float)random.NextDouble()*2-1)*spread;
                target.x=Mathf.Clamp(target.x,field.Origin.x,field.Origin.x+field.Size.x);
                target.z=Mathf.Clamp(target.z,field.Origin.z,field.Origin.z+field.Size.y);
                float clearance=Mathf.Min(size.x,Mathf.Min(size.y,size.z))*.5f+.04f+(float)random.NextDouble()*.06f;
                fragments[i]=new Fragment { transform=piece.transform,start=mesh.transform.position,startRotation=mesh.transform.rotation,
                    target=target,targetRotation=Quaternion.Euler(0,yaw,0)*flat,clearance=clearance,startScale=piece.transform.localScale,targetScale=piece.transform.localScale };
            }
            if(settings.collapseDustPrefab!=null)
            {
                dust=Object.Instantiate(settings.collapseDustPrefab,debris.transform);
                var main=dust.main;main.playOnAwake=false;dust.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            }
            debris.SetActive(false);
        }
        public void Begin(Vector3 center)
        {
            if(Started)return;
            Started=Playing=true;Progress=elapsed=0;
            RefreshGround();ruins.SetActive(true);intact.SetActive(false);
            if(dust!=null)
            {
                if(field.TrySample(center.x,center.z,out float h))center.y=h;
                dust.transform.position=center;dust.Play(true);
            }
        }
        public void RefreshGround()
        {
            if(!Started)return;
            for(int i=0;i<fragments.Length;i++)
            {
                var part=fragments[i];
                if(i<rubble.Length)
                {
                    var slab=rubble[i];part.target=slab.Position;part.targetRotation=slab.ViewRotation;part.targetScale=slab.ViewScale;
                    if(part.transform!=null)part.transform.gameObject.SetActive(slab.Active);
                }
                else if(field.TrySample(part.target.x,part.target.z,out float height))part.target.y=height+part.clearance;
                if(!Playing && part.transform!=null) {part.transform.SetPositionAndRotation(part.target,part.targetRotation);part.transform.localScale=part.targetScale;}
                fragments[i]=part;
            }
        }
        public void Tick(float deltaTime)
        {
            if(!Playing || !(deltaTime>=0) || float.IsInfinity(deltaTime))return;
            elapsed+=deltaTime;Progress=Mathf.Clamp01(elapsed/Mathf.Clamp(settings.collapseSeconds,.9f,1.4f));
            float t=Progress,fall=t*t;
            foreach(var part in fragments)
            {
                if(part.transform==null)continue;
                Vector3 p=Vector3.Lerp(part.start,part.target,t);p.y=Mathf.Lerp(part.start.y,part.target.y,fall);
                part.transform.SetPositionAndRotation(p,Quaternion.Slerp(part.startRotation,part.targetRotation,fall));
                part.transform.localScale=Vector3.Lerp(part.startScale,part.targetScale,fall);
            }
            if(t>=1)Playing=false;
        }
        public void FinishAndClearDust()
        {
            if(Playing)Tick(2);
            if(dust!=null)dust.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }
}
