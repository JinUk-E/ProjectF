using UnityEngine;
namespace Fortress
{
    [CreateAssetMenu(menuName="Fortress/Exterior Settings")]
    public sealed class ExteriorSettings : ScriptableObject
    {
        [Min(10)] public float apronWidth=110;
        [Min(0)] public float ridgeHeight=24;
        [Range(2,24)] public int ringSegments=8;
        public Material groundMaterial,boundaryMaterial;
        public Material boundaryWallMaterial;
        [Min(.1f)] public float ridgeToeDistance=4f;
        [Range(0,1)] public float toeHeightFactor=.15f;
        [Min(4)] public float boundaryRidgeWidth=32f;
        [Min(.5f)] public float boundaryShoulderDistance=14f;
        [Min(1)] public float boundaryRidgeMinHeight=3f,boundaryRidgeMaxHeight=14f;
        [Range(2,2.5f)] public float boundaryWallHeight=2.25f;
        [Min(.1f)] public float boundaryWallThickness=1f;
        [Min(.1f)] public float boundaryWallBurial=1f;
        [Min(0)] public float fogStart=2f,fogEnd=35f;
        [Min(1)] public float boundaryPostSpacing=12;
        [Min(.1f)] public float boundaryPostHeight=.9f;
        [Min(0)] public float boundaryCableHeight=.25f;
        [Min(.005f)] public float cableWidth=.035f;
        public GameObject treePrefab;
        [Range(0,500)] public int treeCount=120;
        [Min(0)] public float treeMinDistance=15,treeMaxDistance=80;
        [Min(.01f)] public float treeScaleMin=.8f,treeScaleMax=1.3f;
    }
}
