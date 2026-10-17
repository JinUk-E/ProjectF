using UnityEngine;

namespace Fortress
{
    [CreateAssetMenu(menuName = "Fortress/Game Tuning")]
    public sealed class GameTuning : ScriptableObject
    {
        [Min(0.01f)] public float gravity = 9.81f;
        public float minPitch = 0f;
        public float maxPitch = 85f;
        public float initialPitch = 45f;
        [Min(0.01f)] public float minPower = 5f;
        public float maxPower = 35f;
        public float initialPower = 22f;
        public float yawSpeed = 40f;
        public float pitchSpeed = 30f;
        public float powerSpeed = 12f;
        [Min(0.01f)] public float maxFlightTime = 15f;
        public float groundHeight;
        public Vector3 wind;
        [Min(0)] public float predictionMarkerRadius = 0.4f;
        [Min(0)] public float impactTolerance = 0.01f;
    }
}
