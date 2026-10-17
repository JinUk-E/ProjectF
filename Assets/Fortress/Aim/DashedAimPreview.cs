using UnityEngine;
using UnityProjectile;

namespace Fortress
{
    /// <summary>Player preview intentionally ignores wind. Actual impact is a separate debug marker.</summary>
    public sealed class DashedAimPreview : MonoBehaviour
    {
        public FlatShotController controller;
        public Material lineMaterial;
        LineRenderer[] lines;
        public void Bind(FlatShotController target, Material material)
        {
            if(controller!=null) controller.StateChanged-=Refresh;
            controller=target;lineMaterial=material;
            if(isActiveAndEnabled && controller!=null) controller.StateChanged+=Refresh;
            Refresh();
        }
        void OnEnable() { if(controller!=null) controller.StateChanged+=Refresh; if(lines!=null) Refresh(); }
        void Start() { Refresh(); }
        void OnDisable()
        {
            if(controller!=null) controller.StateChanged-=Refresh;
            if(lines!=null) foreach(var line in lines) if(line!=null) line.enabled=false;
        }
        public void Refresh()
        {
            if(controller==null || controller.tuning==null || controller.ActiveTank==null || controller.ActiveTank.Muzzle==null) return;
            var settings=controller.tuning;
            int count=Mathf.Clamp(settings.previewSegments,1,128);
            if(lines==null || lines.Length!=count) Rebuild(count);
            var muzzle=controller.ActiveTank.Muzzle;
            var shot=new ArtilleryShot(muzzle.position,muzzle.forward,controller.Power,settings.gravity,Vector3.zero);
            float step=Mathf.Max(0,settings.previewSeconds)/count;
            for(int i=0;i<count;i++)
            {
                var line=lines[i];
                line.enabled=isActiveAndEnabled && !controller.IsFiring && controller.InputEnabled;
                line.sharedMaterial=lineMaterial;
                line.startWidth=line.endWidth=settings.previewWidth;
                line.SetPosition(0,shot.PositionAt(i*step));
                line.SetPosition(1,shot.PositionAt((i+Mathf.Clamp01(settings.previewDashFraction))*step));
            }
        }
        void Rebuild(int count)
        {
            if(lines!=null) foreach(var line in lines) if(line!=null)
            {
                if(Application.isPlaying) Destroy(line.gameObject);else DestroyImmediate(line.gameObject);
            }
            lines=new LineRenderer[count];
            for(int i=0;i<count;i++)
            {
                var go=new GameObject("Aim Dash");go.transform.SetParent(transform,false);
                var line=go.AddComponent<LineRenderer>();line.positionCount=2;line.useWorldSpace=true;
                line.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;line.receiveShadows=false;
                lines[i]=line;
            }
        }
    }
}
