using UnityEngine;
namespace Fortress
{
    [CreateAssetMenu(menuName="Fortress/Mountain Terrain Settings")]
    public sealed class MountainTerrainSettings : ScriptableObject
    {
        public float ridgeHeight=30;
        public float ridgeOffset=34;
        public float ridgeWidth=13;
        public float valleyBend=7;
        public float ridgeWavelength=65;
        public float valleyFloor=0;
        public float detailHeight=2;
        [Min(0)] public float rollingHeight;
        [Min(1)] public float rollingWavelength=45;
        public bool tacticalHills;
        public float hillHeight=14;
        public float hillRadius=21;
        public float hillSpacing=32;
        public float hillJitter=7;
        [Range(.5f,1)] public float centralHillScale=.75f;
        [Min(0)] public float peripheralHillHeight=7;
        [Min(1)] public float peripheralHillRadius=17;
        public float peripheralInnerOffset=40,peripheralOuterOffset=62;
        public float roadHalfWidth=3;
        public float roadBlend=5;
        public float flankRoadOffset=22;
        public float roadJunctionX=26;
    }
}
