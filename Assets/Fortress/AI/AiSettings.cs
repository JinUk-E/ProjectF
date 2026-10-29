using UnityEngine;
namespace Fortress
{
    [CreateAssetMenu(menuName="Fortress/AI Settings")]
    public sealed class AiSettings : ScriptableObject
    {
        public int yawSamples=5,pitchSamples=7,powerSamples=5;
        [Range(1,200)] public int maxSolverCalls=200;
        public float yawSearchDegrees=18;
        public int refinementPasses=3;
        public float reachTolerance=4;
        public float yawSigmaDegrees=5,pitchSigmaDegrees=2,powerSigma=2;
        [Range(0,1)] public float noiseDecay=0.6123724f;
        [Range(0,1)] public float minimumNoiseScale;
        [Range(1,2)] public float thinkSeconds=1.5f;
        public bool enableMovement;
        [Range(4,32)] public int moveCandidates=16;
        [Min(0)] public float maxMoveDistance=6, maxMoveSeconds=3;
        [Min(0)] public float safeWaterClearance=5, coverPreference=4, safetyPreference=1, moveImprovement=.2f;
        [Min(0)] public float preferredRange=35, rangePreference=.08f, travelPenalty=.03f;
    }
}
