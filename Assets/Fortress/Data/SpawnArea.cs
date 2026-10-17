using UnityEngine;

namespace Fortress
{
    [CreateAssetMenu(menuName="Fortress/Spawn Area")]
    public sealed class SpawnArea : ScriptableObject
    {
        public Vector2 min;
        public Vector2 max;
        public Vector2 Sample(System.Random random) => new Vector2(
            Mathf.Lerp(min.x,max.x,(float)random.NextDouble()),Mathf.Lerp(min.y,max.y,(float)random.NextDouble()));
    }
}
