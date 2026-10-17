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
        [Min(1)] public float maxHealth = 100f;
        [Min(0)] public float explosionDamage = 30f;
        [Min(0)] public float fallDamagePerMeter = 5f;
        [Min(0)] public float maxWind = 4f;
        [Min(0.01f)] public float tankCapsuleRadius = 1f;
        [Min(0.02f)] public float tankCapsuleHeight = 2f;
        public float tankGroundOffset;
        [Min(1)] public int spawnAttempts = 32;
        public bool movementEnabled;
        [Min(0)] public float moveSpeed = 4f;
        [Min(0)] public float moveDistancePerTurn = 12f;
        [Range(0,89)] public float maxMoveSlopeDegrees = 30f;
        [Range(.01f,.25f)] public float moveStep = .25f;
        public MountainTerrainSettings mountainTerrain;
        public bool randomTerrain;
        public float terrainBaseHeight;
        [Min(0)] public float terrainAmplitude = 4f;
        [Min(0.01f)] public float terrainNoiseScale = 30f;
        [Range(1,8)] public int terrainOctaves = 3;
        [Range(0,1)] public float terrainPersistence = 0.5f;
        [Min(0)] public float spawnPadMargin = 2f;
        [Min(0.01f)] public float spawnPadBlend = 8f;
        [Min(0.01f)] public float spawnPadClearance = 1f;
        public float groundHeight;
        public float terrainLowerHeight = -8f;
        public float waterHeight = -4f;
        [Min(0.01f)] public float craterRadius = 4f;
        [Min(0.01f)] public float craterDepth = 2f;
        [Min(0)] public float previewSeconds = 1.5f;
        [Range(1,128)] public int previewSegments = 24;
        [Range(0.01f,1)] public float previewDashFraction = 0.55f;
        [Min(0.001f)] public float previewWidth = 0.08f;
        public Vector3 wind;
        public bool showDebugPrediction = true;
        [Min(0)] public float predictionMarkerRadius = 0.4f;
        [Min(0)] public float impactTolerance = 0.01f;
    }
}
