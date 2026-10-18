using UnityEngine;
namespace Fortress
{
    [CreateAssetMenu(menuName="Fortress/Aim Preview Style")]
    public sealed class AimPreviewStyle : ScriptableObject
    {
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
