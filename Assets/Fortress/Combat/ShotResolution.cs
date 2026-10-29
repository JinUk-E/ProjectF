using UnityEngine;
namespace Fortress
{
    public readonly struct TankDamageResult
    {
        public readonly int TankIndex;
        public readonly Vector3 Position;
        public readonly float ExplosionDamage, FallDamage;
        public readonly bool DirectHit, Drowned;
        public float TotalDamage => ExplosionDamage + FallDamage;
        public TankDamageResult(int index, Vector3 position, float explosion, float fall, bool direct, bool drowned)
        { TankIndex=index; Position=position; ExplosionDamage=explosion; FallDamage=fall; DirectHit=direct; Drowned=drowned; }
    }
    public readonly struct ShotResolution
    {
        public readonly int ShooterIndex, Generation, Turn;
        public readonly AmmoPurpose Purpose;
        public readonly TankDamageResult First, Second;
        public ShotResolution(int shooter, int generation, int turn, AmmoPurpose purpose, TankDamageResult first, TankDamageResult second)
        { ShooterIndex=shooter; Generation=generation; Turn=turn; Purpose=purpose; First=first; Second=second; }
        public TankDamageResult ForTank(int index) => index==0 ? First : Second;
    }
}
