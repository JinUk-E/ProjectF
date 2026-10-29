using UnityEngine;
namespace Fortress
{
    [CreateAssetMenu(menuName="Fortress/Tank Loadout")]
    public sealed class TankLoadout : ScriptableObject
    {
        public string displayName="MAIN BATTLE";
        public AmmoDefinition basic, special;
        public TankAppearance appearance;
        [Min(0)] public int specialRounds=3;
        [Min(.1f)] public float healthMultiplier=1, movementMultiplier=1, speedMultiplier=1;
    }
    public static class BattleLoadoutSelection
    {
        public static int Player, Opponent;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset() { Player=0;Opponent=0; }
    }
}
