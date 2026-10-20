using UnityEngine;
using UnityProjectile;
namespace Fortress
{
    public enum AmmoPurpose { HighExplosive, Excavation, Demolition, Cluster }
    [CreateAssetMenu(menuName="Fortress/Ammunition")]
    public sealed class AmmoDefinition : ScriptableObject
    {
        public string displayName="HIGH EXPLOSIVE";
        public AmmoPurpose purpose;
        public ProjectileRunner projectilePrefab;
        [Min(0)] public float damage=30, buildingDamage=30, radius=4, depth=.55f, rimHeight=.15f;
        public ShotDamage Snapshot()=>new ShotDamage(damage,buildingDamage,radius,depth,rimHeight);
    }
    public readonly struct ShotDamage
    {
        public readonly float damage,buildingDamage,radius,depth,rimHeight;
        public ShotDamage(float damage,float buildingDamage,float radius,float depth,float rimHeight)
        { this.damage=damage;this.buildingDamage=buildingDamage;this.radius=radius;this.depth=depth;this.rimHeight=rimHeight; }
    }
}
