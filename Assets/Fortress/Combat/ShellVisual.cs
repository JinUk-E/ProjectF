using System;
using UnityEngine;
using UnityProjectile;

namespace Fortress
{
    /// <summary>Presentation only. The model's local +Z is its nose; the runner owns all motion.</summary>
    [DisallowMultipleComponent]
    public sealed class ShellVisual : MonoBehaviour
    {
        public ProjectileRunner runner;
        public Transform visualRoot;
        public TrailRenderer trail;
        public bool IsFlying { get; private set; }
        ProjectileRunner subscribedRunner;

        void Awake() { if(runner==null) runner=GetComponent<ProjectileRunner>(); }
        void OnEnable() { ResetVisual();Subscribe(); }
        void OnDisable() { Unsubscribe();ResetVisual(); }

        // Configure before launch. Rebinding deliberately clears the old presentation.
        public void Configure(ProjectileRunner source,Transform model,TrailRenderer trajectoryTrail)
        {
            if(source==null) throw new ArgumentNullException(nameof(source));
            if(model==null || model==source.transform || !model.IsChildOf(source.transform) ||
                transform==model || transform.IsChildOf(model))
                throw new ArgumentException("Model must be a runner child and cannot contain ShellVisual.",nameof(model));
            if(trajectoryTrail!=null && !trajectoryTrail.transform.IsChildOf(source.transform))
                throw new ArgumentException("Trail must belong to the runner.",nameof(trajectoryTrail));
            Unsubscribe();ResetVisual();
            runner=source;visualRoot=model;trail=trajectoryTrail;
            ResetVisual();if(isActiveAndEnabled) Subscribe();
        }
        void Subscribe()
        {
            Unsubscribe();
            if(runner==null) return;
            subscribedRunner=runner;
            subscribedRunner.Launched+=OnLaunched;
            subscribedRunner.Impacted+=OnImpacted;
            subscribedRunner.Finished+=OnFinished;
        }
        void Unsubscribe()
        {
            if(subscribedRunner==null) return;
            subscribedRunner.Launched-=OnLaunched;
            subscribedRunner.Impacted-=OnImpacted;
            subscribedRunner.Finished-=OnFinished;
            subscribedRunner=null;
        }
        bool HasSafeModel => runner!=null && visualRoot!=null && visualRoot!=runner.transform &&
            visualRoot.IsChildOf(runner.transform) && transform!=visualRoot && !transform.IsChildOf(visualRoot);
        void OnLaunched(ProjectileRunner source)
        {
            ResetVisual();IsFlying=true;
            if(HasSafeModel) visualRoot.gameObject.SetActive(true);
            RefreshPose();
            if(trail!=null) trail.emitting=true;
        }
        void OnImpacted(ProjectileRunner source)
        {
            IsFlying=false;
            if(trail!=null) trail.emitting=false;
            if(HasSafeModel) visualRoot.gameObject.SetActive(false);
        }
        void OnFinished(ProjectileRunner source) { ResetVisual(); }
        void ResetVisual()
        {
            IsFlying=false;
            if(trail!=null) { trail.emitting=false;trail.Clear(); }
            if(HasSafeModel) visualRoot.gameObject.SetActive(false);
        }
        void LateUpdate() { RefreshPose(); }
        public void RefreshPose()
        {
            if(!IsFlying || !HasSafeModel || runner.Trajectory==null) return;
            // Read only the kinematic direction. Never poll lifecycle/impact flags or move the runner.
            Vector3 direction=runner.Trajectory.StateAtTime(runner.LocalTime).Direction;
            float length=direction.sqrMagnitude;
            if(!(length>0.000001f) || float.IsInfinity(length)) return;
            direction/=Mathf.Sqrt(length);
            Vector3 up=Mathf.Abs(Vector3.Dot(direction,Vector3.up))>.999f?Vector3.right:Vector3.up;
            visualRoot.rotation=Quaternion.LookRotation(direction,up);
        }
    }
}
