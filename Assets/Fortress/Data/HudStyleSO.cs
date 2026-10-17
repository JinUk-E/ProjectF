using UnityEngine;

namespace Fortress
{
    [CreateAssetMenu(menuName = "Fortress/HUD Style")]
    public sealed class HudStyleSO : ScriptableObject
    {
        public Color panel = new Color(.035f, .043f, .045f, .88f);
        public Color text = new Color(.94f, .95f, .91f);
        public Color muted = new Color(.58f, .64f, .62f);
        public Color accent = new Color(.68f, .76f, .44f);
        public Color danger = new Color(.93f, .29f, .23f);
        [Range(0, 1)] public float dangerHealthFraction = .25f;
        [Min(.1f)] public float notificationSeconds = 2.2f;
        [Min(.01f)] public float notificationFadeSeconds = .4f;
    }
}
