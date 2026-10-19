using UnityEngine;
namespace Fortress
{
    [CreateAssetMenu(menuName="Fortress/Cover Settings")]
    public sealed class CoverSettings : ScriptableObject
    {
        public Vector2[] sites={new Vector2(-14,-9),new Vector2(0,-9),new Vector2(14,-9),new Vector2(-14,9),new Vector2(0,9),new Vector2(14,9)};
        public Vector3 size=new Vector3(5,4.5f,4);
        public float positionJitter=1;
        public float health=45;
        public float foundationDrop=2;
        public Material wallMaterial,roofMaterial,windowMaterial,rubbleMaterial;
        public Material damageMaterial;
        public ParticleSystem collapseDustPrefab;
        [Range(.9f,1.4f)] public float collapseSeconds=1.2f;
        [Min(0)] public float collapseSpread=1.5f;
    }
}

