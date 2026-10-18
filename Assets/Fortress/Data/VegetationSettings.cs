using UnityEngine;
namespace Fortress
{
    [CreateAssetMenu(menuName="Fortress/Vegetation Settings")]
    public sealed class VegetationSettings : ScriptableObject
    {
        public GameObject treePrefab,shrubPrefab;
        [Range(0,500)] public int treeCount=65,shrubCount=100;
        [Range(1,100)] public int attemptsPerInstance=20;
        [Min(.1f)] public float minSpacing=5;
        [Min(0)] public float roadClearance=3,spawnClearance=5,buildingClearance=3;
        [Range(0,80)] public float maxSlopeDegrees=50;
        [Min(.01f)] public float minScale=.8f,maxScale=1.2f;
        [Min(.01f)] public float destructionDrop=.4f;
    }
}
