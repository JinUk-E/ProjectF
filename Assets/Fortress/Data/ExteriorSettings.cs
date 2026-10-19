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
