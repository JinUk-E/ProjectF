using UnityEngine;
namespace Fortress
{
    [CreateAssetMenu(menuName="Fortress/Tank Appearance")]
    public sealed class TankAppearance : ScriptableObject
    {
        public Sprite portrait;
        [TextArea] public string description;
        public string sourceModel;
        public TankVisualPiece[] pieces=System.Array.Empty<TankVisualPiece>();
    }
    [System.Serializable]
    public struct TankVisualPiece
    {
        public Mesh mesh;
        // 0 hull, 1 turret, 2 recoiling gun. Geometry is already in the existing rig's local coordinates.
        [Range(0,2)] public int mount;
        public Material[] playerMaterials,opponentMaterials;
    }
}
