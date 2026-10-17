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
        public bool tacticalHills;
        public float hillHeight=14;
        public float hillRadius=21;
        public float hillSpacing=32;
        public float hillJitter=7;
        public float roadHalfWidth=3;
        public float roadBlend=5;
        public float flankRoadOffset=22;
        public float roadJunctionX=26;
    }
}
