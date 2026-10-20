using UnityEngine;
namespace Fortress
{
    [CreateAssetMenu(menuName="Fortress/Terrain Blast Settings")]
    public sealed class TerrainBlastSettings : ScriptableObject
    {
        public Mesh fragmentMesh;
        public Material fragmentMaterial;
        [Range(16,512)] public int capacity=192;
        [Range(3,32)] public int piecesPerBlast=14;
        public Vector2 fragmentSize=new Vector2(.12f,.38f);
        public Vector2 airtime=new Vector2(.45f,.9f);
        [Min(0)] public float arcHeight=2.2f;
        [Min(1)] public float scatterRadius=1.6f;
    }
}
