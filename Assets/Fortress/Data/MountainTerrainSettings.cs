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
    }
}
