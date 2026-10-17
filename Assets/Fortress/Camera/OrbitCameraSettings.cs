using UnityEngine;
namespace Fortress
{
    [CreateAssetMenu(menuName="Fortress/Orbit Camera Settings")]
    public sealed class OrbitCameraSettings : ScriptableObject
    {
        [Min(0)] public float orbitDegreesPerPixel=.15f;
        [Min(0)] public float zoomSpeed=.08f;
        public float minPitch=20,maxPitch=80;
        [Min(.1f)] public float minDistance=12,maxDistance=100;
        [Min(0)] public float groundClearance=1.5f;
        public float focusHeight=1.5f;
        [Range(1,128)] public int terrainSamples=32;
    }
}
