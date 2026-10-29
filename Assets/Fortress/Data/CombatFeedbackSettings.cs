using System;
using UnityEngine;
namespace Fortress
{
    [CreateAssetMenu(menuName="Fortress/Combat Feedback")]
    public sealed class CombatFeedbackSettings : ScriptableObject
    {
        [Min(.1f)] public float lifetimeSeconds=1.8f;
        [Min(0)] public float risePixels=65, worldHeight=3;
        [Min(0)] public float shakeSeconds=.18f, shakeDistance=.12f, shakeFrequency=35;
        public Color damageColor=new Color(1,.8f,.35f), directColor=new Color(1,.4f,.2f), fallColor=new Color(.7f,.8f,1);
        public ImpactStyle[] impacts=Array.Empty<ImpactStyle>();
        [Serializable] public struct ImpactStyle
        {
            public AmmoPurpose purpose;
            public AudioClip clip;
            public Color color;
            public float sizeMultiplier, pitch, volume;
        }
        public bool TryGetImpact(AmmoPurpose purpose,out ImpactStyle result)
        {
            if(impacts!=null) foreach(var item in impacts) if(item.purpose==purpose) { result=item;return true; }
            result=default;return false;
        }
    }
}
