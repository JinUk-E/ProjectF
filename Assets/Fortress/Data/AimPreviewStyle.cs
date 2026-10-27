using UnityEngine;
namespace Fortress
{
    [CreateAssetMenu(menuName="Fortress/Aim Preview Style")]
    public sealed class AimPreviewStyle : ScriptableObject
    {
        public bool showImpactGuide,windAwarePreview;
        [Min(.1f)] public float impactRadius=1.8f;
        [Min(.01f)] public float impactWidth=.16f,impactLift=.25f;
        [Range(12,128)] public int impactSegments=48;
        public Color impactColor=new Color(.35f,1f,.65f,1);
        public Material coreMaterial,outlineMaterial,occludedMaterial;
        public Color coreColor=new Color(1f,.91f,.62f,1f);
        public Color outlineColor=new Color(.025f,.035f,.04f,1f);
        public Color occludedColor=new Color(.3f,.5f,.6f,.35f);
        [Min(.1f)] public float widthMultiplier=1.5f;
        [Min(1f)] public float outlineWidthMultiplier=2.5f;
        [Range(.1f,1f)] public float endWidthMultiplier=.65f;
        [Range(2,32)] public int directionEvery=6;
        [Min(.01f)] public float directionSize=.45f;
    }
}
