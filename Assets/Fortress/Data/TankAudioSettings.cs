using UnityEngine;
namespace Fortress
{
    [CreateAssetMenu(menuName="Fortress/Tank Audio")]
    public sealed class TankAudioSettings : ScriptableObject
    {
        public AudioClip engineLoop,trackLoop;
        [Range(0,1)] public float engineVolume=.20f,trackVolume=.28f;
        [Min(.01f)] public float movementLease=.12f,fadeIn=.08f,fadeOut=.15f;
        [Min(.01f)] public float referenceSpeed=4;
        public Vector2 enginePitch=new Vector2(.8f,1.12f),trackPitch=new Vector2(.75f,1.2f);
    }
}
