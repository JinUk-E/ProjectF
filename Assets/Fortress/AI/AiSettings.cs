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
        [Range(1,2)] public float thinkSeconds=1.5f;
    }
}
