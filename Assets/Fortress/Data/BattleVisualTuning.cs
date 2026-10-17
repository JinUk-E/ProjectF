using UnityEngine;

namespace Fortress
{
    [CreateAssetMenu(menuName = "Fortress/Battle Visual Tuning")]
    public sealed class BattleVisualTuning : ScriptableObject
    {
        [Min(0)] public float recoilDistance = .45f;
        [Min(.01f)] public float recoilSeconds = .35f;
        [Min(.01f)] public float muzzleLifetime = .12f;
        [Min(.01f)] public float muzzleSize = 1.4f;
        [Min(1)] public int muzzleParticles = 8;
        [Min(.01f)] public float flashLifetime = .22f;
        [Min(.01f)] public float flashSize = 2.8f;
        [Min(1)] public int flashParticles = 14;
        public Color flashColor = new Color(1, .65f, .18f, .95f);
    }
}
