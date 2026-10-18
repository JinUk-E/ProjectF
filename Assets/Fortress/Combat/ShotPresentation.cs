using UnityEngine;
using UnityProjectile;

namespace Fortress
{
    // shader-vfx-codex: recoil changes visual children only, never the aiming or muzzle transforms.
    public sealed class ShotPresentation : MonoBehaviour
    {
        public FlatShotController shots;
        public BattleVisualTuning tuning;
        public Transform[] gunVisuals = System.Array.Empty<Transform>();
        public AudioSource shotAudio,impactAudio;
        public ParticleSystem muzzleFlash, impactFlash;
        Vector3[] restPositions;
        int recoiling = -1;
        float elapsed;
        void Awake()
        {
            restPositions = new Vector3[gunVisuals.Length];
            for (int i=0;i<gunVisuals.Length;i++) if(gunVisuals[i]!=null) restPositions[i]=gunVisuals[i].localPosition;
        }
        void OnEnable()
        {
            if(shots==null) return;
            shots.ShotLaunched+=Launched;
            shots.ShotImpacted+=Impacted;
            shots.ShotCancelled+=Cancelled;
        }
        void OnDisable()
        {
            if(shots!=null) { shots.ShotLaunched-=Launched; shots.ShotImpacted-=Impacted; shots.ShotCancelled-=Cancelled; }
            Cancelled();
        }
        void Cancelled()
        {
            if(shotAudio!=null) shotAudio.Stop();
            if(impactAudio!=null) impactAudio.Stop();
            Restore();
            if(muzzleFlash!=null) muzzleFlash.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            if(impactFlash!=null) impactFlash.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
        }
        void Launched(ProjectileRunner runner)
        {
            Restore();
            if(shotAudio!=null) { shotAudio.transform.position=runner.Position;shotAudio.Play(); }
            if(tuning==null || shots.ActiveTank==null) return;
            for(int i=0;i<shots.tanks.Length && i<gunVisuals.Length;i++)
                if(shots.tanks[i]==shots.ActiveTank) { recoiling=i; elapsed=0; break; }
            var muzzle=shots.ActiveTank.Muzzle;
            if(muzzleFlash!=null)
            {
                muzzleFlash.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
                muzzleFlash.transform.SetPositionAndRotation(muzzle.position,muzzle.rotation);
                muzzleFlash.Play(true);
            }
        }
        void Impacted(ProjectileRunner runner)
        {
            if(!shots.LastSolveHit) return;
            if(impactAudio!=null) { impactAudio.transform.position=runner.Position;impactAudio.Play(); }
            if(impactFlash==null) return;
            impactFlash.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            impactFlash.transform.position=runner.Position;
            impactFlash.Play(true);
        }
        void LateUpdate()
        {
            if(recoiling<0 || tuning==null) return;
            elapsed+=Time.deltaTime;
            float t=Mathf.Clamp01(elapsed/Mathf.Max(.01f,tuning.recoilSeconds));
            if(gunVisuals[recoiling]!=null)
                gunVisuals[recoiling].localPosition=restPositions[recoiling]-Vector3.forward*(tuning.recoilDistance*(1-t)*(1-t));
            if(t>=1) Restore();
        }
        void Restore()
        {
            if(recoiling>=0 && restPositions!=null && gunVisuals[recoiling]!=null)
                gunVisuals[recoiling].localPosition=restPositions[recoiling];
            recoiling=-1;
        }
    }
}


