using UnityEngine;
namespace Fortress
{
    [CreateAssetMenu(menuName="Fortress/Projectile Camera Settings")]
    public sealed class ProjectileCameraSettings : ScriptableObject
    {
        public bool casualPresentation;
        [Min(.1f)] public float wideResponse=2.5f,wideMinDistance=32,wideHeight=24;
        [Range(8,128)] public int wideSamples=48;
        [Min(.1f)] public float wideMissSeconds=4;
        [Min(.1f)] public float chaseDistance=11;
        [Min(0)] public float chaseHeight=4;
        public float sideOffset=3;
        [Min(.1f)] public float overviewDistance=19;
        [Min(0)] public float overviewHeight=18;
        [Range(20,100)] public float flightFieldOfView=52,overviewFieldOfView=58;
        [Min(.1f)] public float positionResponse=8,rotationResponse=12;
        [Min(1)] public float framingPadding=1.25f;
        [Min(0)] public float framingMargin=2;
        [Min(0)] public float impactHoldSeconds=.65f;
        [Min(.01f)] public float returnSeconds=.85f;
        [Min(0)] public float groundClearance=2;
        [Range(4,64)] public int terrainSamples=20;
        [Min(0)] public float maximumSightlineLift=40;
    }
}
