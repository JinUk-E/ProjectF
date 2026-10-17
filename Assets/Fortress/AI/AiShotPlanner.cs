using System;
using UnityEngine;
using UnityProjectile;
namespace Fortress
{
    public readonly struct AiShotPlan
    {
        public readonly float Yaw,Pitch,Power,DistanceToTarget;
        public readonly int SolveCount;
        public readonly Vector3 PredictedImpact;
        public readonly bool UsedFallback;
        public AiShotPlan(float yaw,float pitch,float power,int count,Vector3 impact,float distance,bool fallback)
        { Yaw=yaw;Pitch=pitch;Power=power;SolveCount=count;PredictedImpact=impact;DistanceToTarget=distance;UsedFallback=fallback; }
    }
    public static class AiShotPlanner
    {
        struct Sample { public float yaw,pitch,power,error;public Vector3 impact; }
        sealed class Search
        {
            public FlatShotController shots;public Vector3 target,pivot,barrelOffset,muzzleOffset,forward;
            public int count,limit;
            public Sample Solve(float yaw,float pitch,float power)
            {
                var tuning=shots.tuning;
                yaw=Mathf.Repeat(yaw+180,360)-180;pitch=Mathf.Clamp(pitch,tuning.minPitch,tuning.maxPitch);power=Mathf.Clamp(power,tuning.minPower,tuning.maxPower);
                var y=Quaternion.Euler(0,yaw,0);var rotation=y*Quaternion.Euler(-pitch,0,0);
                var shot=new ArtilleryShot(pivot+y*barrelOffset+rotation*muzzleOffset,rotation*forward,power,tuning.gravity,shots.Wind);
                shot.maxFlightTime=tuning.maxFlightTime;
                ArtillerySolver.TrySolve(shot,shots.Surface,out var launch);count++;
                return new Sample{yaw=yaw,pitch=pitch,power=power,impact=launch.target,error=Vector3.Distance(launch.target,target)};
            }
        }
        public static AiShotPlan Plan(FlatShotController shots,Vector3 target,AiSettings settings,int seed,int aiTurnNumber)
        {
            if(shots==null || settings==null || shots.Surface==null || shots.ActiveTank==null || shots.tuning==null)
                throw new ArgumentException("AI requires a configured shot controller and settings.");
            if(settings.yawSamples<1 || settings.pitchSamples<2 || settings.powerSamples<2 || settings.maxSolverCalls<2 || settings.maxSolverCalls>200 ||
                settings.refinementPasses<0 || settings.refinementPasses>8 || !Finite(settings.yawSearchDegrees) || settings.yawSearchDegrees<0 ||
                !Finite(settings.noiseDecay) || settings.noiseDecay<0 || settings.noiseDecay>1 || !Finite(settings.minimumNoiseScale) || settings.minimumNoiseScale<0 || settings.minimumNoiseScale>1 || !Finite(settings.reachTolerance) || settings.reachTolerance<0 ||
                !Finite(settings.yawSigmaDegrees) || settings.yawSigmaDegrees<0 || !Finite(settings.pitchSigmaDegrees) || settings.pitchSigmaDegrees<0 ||
                !Finite(settings.powerSigma) || settings.powerSigma<0 || !Finite(settings.thinkSeconds))
                throw new ArgumentException("AI search settings are outside supported bounds.");
            var tank=shots.ActiveTank;
            if(tank.turret==null || tank.barrel==null || tank.Muzzle==null) throw new ArgumentException("AI requires turret, barrel and muzzle transforms.");
            var tuning=shots.tuning;
            var search=new Search{shots=shots,target=target,limit=settings.maxSolverCalls-1,pivot=tank.turret.position,
                barrelOffset=Quaternion.Inverse(tank.turret.rotation)*(tank.barrel.position-tank.turret.position),
                muzzleOffset=Quaternion.Inverse(tank.barrel.rotation)*(tank.Muzzle.position-tank.barrel.position),
                forward=Quaternion.Inverse(tank.barrel.rotation)*tank.Muzzle.forward};
            var delta=target-search.pivot;float distance=new Vector2(delta.x,delta.z).magnitude;
            float estimatedTime=distance/Mathf.Max(tuning.maxPower*Mathf.Cos(Mathf.PI*.25f),.01f);
            var compensated=delta-shots.Wind*(.5f*estimatedTime*estimatedTime);
            float yaw=Mathf.Atan2(compensated.x,compensated.z)*Mathf.Rad2Deg;
            var toward=new Vector3(delta.x,0,delta.z).normalized;
            Sample best=default,fallback=default;best.error=float.PositiveInfinity;
            float farthest=float.NegativeInfinity;
            // Power descends so even a restricted budget evaluates maximum-range options.
            for(int p=0;p<settings.powerSamples && search.count<search.limit;p++)
            for(int a=0;a<settings.pitchSamples && search.count<search.limit;a++)
            for(int y=0;y<settings.yawSamples && search.count<search.limit;y++)
            {
                float candidateYaw=yaw+(settings.yawSamples==1?0:Mathf.Lerp(-settings.yawSearchDegrees,settings.yawSearchDegrees,(float)y/(settings.yawSamples-1)));
                var sample=search.Solve(candidateYaw,Mathf.Lerp(tuning.minPitch,tuning.maxPitch,(float)a/(settings.pitchSamples-1)),
                    Mathf.Lerp(tuning.maxPower,tuning.minPower,(float)p/(settings.powerSamples-1)));
                if(sample.error<best.error) best=sample;
                float range=Vector3.Dot(sample.impact-search.pivot,toward);
                if(p==0 && range>farthest) { farthest=range;fallback=sample; }
            }
            float ys=settings.yawSamples>1?settings.yawSearchDegrees/(settings.yawSamples-1):settings.yawSearchDegrees;
            float ps=(tuning.maxPitch-tuning.minPitch)/(settings.pitchSamples-1)*.5f;
            float ss=(tuning.maxPower-tuning.minPower)/(settings.powerSamples-1)*.5f;
            for(int pass=0;pass<settings.refinementPasses && search.count<search.limit;pass++)
            {
                var center=best;
                for(int n=0;n<8 && search.count<search.limit;n++)
                {
                    float dy=n==0||n==6?-ys:n==1||n==7?ys:0;
                    float dp=n==2?-ps:n==3||n>=6?ps:0;
                    float ds=n==4?-ss:n==5?ss:0;
                    var sample=search.Solve(center.yaw+dy,center.pitch+dp,center.power+ds);
                    if(sample.error<best.error) best=sample;
                }
                ys*=.5f;ps*=.5f;ss*=.5f;
            }
            bool useFallback=distance>farthest+settings.reachTolerance;
            var selected=useFallback?fallback:best;
            var random=new System.Random(unchecked(seed ^ (aiTurnNumber*486187739)));
            float decay=Mathf.Max(settings.minimumNoiseScale,Mathf.Pow(settings.noiseDecay,Mathf.Max(0,aiTurnNumber-1)));
            var noisy=search.Solve(selected.yaw+Gaussian(random)*settings.yawSigmaDegrees*decay,
                selected.pitch+Gaussian(random)*settings.pitchSigmaDegrees*decay,
                useFallback?tuning.maxPower:selected.power+Gaussian(random)*settings.powerSigma*decay);
            return new AiShotPlan(noisy.yaw,noisy.pitch,noisy.power,search.count,noisy.impact,noisy.error,useFallback);
        }
        static bool Finite(float value)=>!float.IsNaN(value)&&!float.IsInfinity(value);
        static float Gaussian(System.Random random)=>(float)(Math.Sqrt(-2*Math.Log(1-random.NextDouble()))*Math.Cos(2*Math.PI*random.NextDouble()));
    }
}
