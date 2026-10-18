using UnityEngine;
using UnityProjectile;

namespace Fortress
{
    /// <summary>Wind-free partial trajectory only; depth styling adds no solved impact information.</summary>
    public sealed class DashedAimPreview : MonoBehaviour
    {
        public FlatShotController controller;
        public Material lineMaterial;
        public AimPreviewStyle style;
        LineRenderer[] lines,outlines,occluded,arrows,arrowOutlines,arrowOccluded;
        bool styled;
        int directionEvery;
        public void Bind(FlatShotController target,Material material)
        {
            if(controller!=null) controller.StateChanged-=Refresh;
            controller=target;lineMaterial=material;
            if(isActiveAndEnabled && controller!=null) controller.StateChanged+=Refresh;
            Refresh();
        }
        void OnEnable() { if(controller!=null) controller.StateChanged+=Refresh;if(lines!=null) Refresh(); }
        void Start() { Refresh(); }
        void OnDisable() { if(controller!=null) controller.StateChanged-=Refresh;HideAll(); }
        void HideAll() { SetHidden(lines);SetHidden(outlines);SetHidden(occluded);SetHidden(arrows);SetHidden(arrowOutlines);SetHidden(arrowOccluded); }
        static void SetHidden(LineRenderer[] group) { if(group!=null) foreach(var line in group) if(line!=null)line.enabled=false; }
        public void Refresh()
        {
            if(controller==null || controller.tuning==null || controller.ActiveTank==null || controller.ActiveTank.Muzzle==null)
            { HideAll();return; }
            var settings=controller.tuning;
            int count=Mathf.Clamp(settings.previewSegments,1,128);
            int every=style!=null?Mathf.Clamp(style.directionEvery,2,32):0;
            if(lines==null || lines.Length!=count || styled!=(style!=null) || every!=directionEvery) Rebuild(count,every);
            var muzzle=controller.ActiveTank.Muzzle;
            var shot=new ArtilleryShot(muzzle.position,muzzle.forward,controller.Power,settings.gravity,Vector3.zero);
            float step=Mathf.Max(0,settings.previewSeconds)/count;
            bool visible=isActiveAndEnabled && !controller.IsFiring && controller.InputEnabled && step>0;
            for(int i=0;i<count;i++)
            {
                var a=shot.PositionAt(i*step);var b=shot.PositionAt((i+Mathf.Clamp01(settings.previewDashFraction))*step);
                float width=settings.previewWidth;
                if(styled) width*=Mathf.Max(.1f,style.widthMultiplier)*Mathf.Lerp(1,Mathf.Clamp(style.endWidthMultiplier,.1f,1f),(float)i/count);
                Draw(lines[i],a,b,width,styled?style.coreMaterial:lineMaterial,styled?style.coreColor:Color.white,visible);
                if(!styled) continue;
                Draw(outlines[i],a,b,width*Mathf.Max(1,style.outlineWidthMultiplier),style.outlineMaterial,style.outlineColor,visible && style.outlineMaterial!=null);
                Draw(occluded[i],a,b,width,style.occludedMaterial,style.occludedColor,visible && style.occludedMaterial!=null);
                if((i+1)%every!=0) continue;
                int index=(i+1)/every-1;
                // Arrow tip lies on the existing partial arc, never on a solved target or ground projection.
                Vector3 forward=(b-a).normalized;
                Vector3 side=Vector3.Cross(forward,Vector3.up);
                if(side.sqrMagnitude<.0001f) side=Vector3.Cross(forward,Vector3.right);
                side.Normalize();
                float size=Mathf.Min(Mathf.Max(.01f,style.directionSize),Vector3.Distance(a,b));
                var left=b-forward*size+side*size*.5f;var right=b-forward*size-side*size*.5f;
                DrawArrow(arrows[index],left,b,right,width,style.coreMaterial,style.coreColor,visible);
                DrawArrow(arrowOutlines[index],left,b,right,width*Mathf.Max(1,style.outlineWidthMultiplier),style.outlineMaterial,style.outlineColor,visible && style.outlineMaterial!=null);
                DrawArrow(arrowOccluded[index],left,b,right,width,style.occludedMaterial,style.occludedColor,visible && style.occludedMaterial!=null);
            }
        }
        void Draw(LineRenderer line,Vector3 a,Vector3 b,float width,Material material,Color color,bool visible)
        {
            line.enabled=visible;line.sharedMaterial=material!=null?material:lineMaterial;
            line.startWidth=line.endWidth=width;line.startColor=line.endColor=color;
            line.SetPosition(0,a);line.SetPosition(1,b);
        }
        void DrawArrow(LineRenderer line,Vector3 a,Vector3 tip,Vector3 b,float width,Material material,Color color,bool visible)
        { Draw(line,a,tip,width,material,color,visible);line.SetPosition(2,b); }
        void Rebuild(int count,int every)
        {
            Remove(lines);Remove(outlines);Remove(occluded);Remove(arrows);Remove(arrowOutlines);Remove(arrowOccluded);
            styled=style!=null;directionEvery=every;
            lines=Create(count,"Aim Core",2,1);
            outlines=styled?Create(count,"Aim Outline",2,0):null;
            occluded=styled?Create(count,"Aim Occluded",2,2):null;
            int arrowCount=styled?count/every:0;
            arrows=Create(arrowCount,"Aim Direction",3,1);
            arrowOutlines=Create(arrowCount,"Aim Direction Outline",3,0);
            arrowOccluded=Create(arrowCount,"Aim Direction Occluded",3,2);
        }
        void Remove(LineRenderer[] group)
        {
            if(group==null)return;
            foreach(var line in group) if(line!=null)
            { line.enabled=false;if(Application.isPlaying)Destroy(line.gameObject);else DestroyImmediate(line.gameObject); }
        }
        LineRenderer[] Create(int count,string name,int points,int order)
        {
            var group=new LineRenderer[count];
            for(int i=0;i<count;i++)
            {
                var go=new GameObject(name);go.transform.SetParent(transform,false);
                var line=go.AddComponent<LineRenderer>();line.positionCount=points;line.useWorldSpace=true;line.sortingOrder=order;
                line.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;line.receiveShadows=false;
                line.numCapVertices=2;line.numCornerVertices=2;group[i]=line;
            }
            return group;
        }
    }
}
