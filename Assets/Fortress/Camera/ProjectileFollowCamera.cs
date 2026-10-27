using System.Collections.Generic;
using UnityEngine;
using UnityProjectile;
namespace Fortress
{
    /// <summary>Event-owned shot presentation. Never changes projectile clocks, turns or orbit coordinates.</summary>
    [DefaultExecutionOrder(100)]
    [DisallowMultipleComponent]
    public sealed class ProjectileFollowCamera : MonoBehaviour
    {
        public FlatShotController shots;
        public BattlefieldOrbitCamera orbit;
        public ProjectileCameraSettings settings;
        public MatchController match;
        public BattlefieldExterior exterior;
        public Camera viewCamera;
        public enum ViewPhase { Idle, Following, ImpactHold, Returning }
        public ViewPhase Phase { get; private set; }
        public bool IsTracking => Phase!=ViewPhase.Idle;
        public Vector3 Focus { get; private set; }
        public float FamilyRadius { get; private set; }
        public int TrackedCount => members.Count;
        sealed class Member
        {
            public ProjectileRunner runner;
            public Vector3 last;
            public bool finished,hasChildren;
        }
        readonly List<Member> members=new List<Member>(8);
        FlatShotController subscribedShots;
        MatchController subscribedMatch;
        Transform cameraTransform;
        Vector3 savedPosition,returnStartPosition,heading,impactPoint;
        Quaternion savedRotation,returnStartRotation;
        float savedFov,returnStartFov,timer;
        int generation;
        bool impactApproach,wideView;
        public bool IsWideView => wideView && IsTracking;
        void Awake() { Cache(); }
        void Cache() { cameraTransform=transform;if(viewCamera==null) viewCamera=GetComponent<Camera>(); }
        void OnEnable()
        {
            Cache();Subscribe();
        }
        void OnDisable() { Unsubscribe();RestoreImmediate(); }
        public void Configure(FlatShotController controller,BattlefieldOrbitCamera manual,ProjectileCameraSettings tuning)
        {
            Unsubscribe();RestoreImmediate();shots=controller;orbit=manual;settings=tuning;
            match=manual!=null?manual.match:null;exterior=manual!=null?manual.exterior:null;
            Cache();if(isActiveAndEnabled) Subscribe();
        }
        void Subscribe()
        {
            if(shots!=null)
            {
                subscribedShots=shots;shots.ShotLaunched+=OnLaunch;shots.ShotFinished+=OnFamilyFinished;shots.ShotCancelled+=RestoreImmediate;
            }
            if(match!=null) { subscribedMatch=match;match.StateChanged+=OnMatchChanged; }
        }
        void Unsubscribe()
        {
            if(subscribedShots!=null) { subscribedShots.ShotLaunched-=OnLaunch;subscribedShots.ShotFinished-=OnFamilyFinished;subscribedShots.ShotCancelled-=RestoreImmediate; }
            if(subscribedMatch!=null) subscribedMatch.StateChanged-=OnMatchChanged;
            subscribedShots=null;subscribedMatch=null;
        }
        void OnMatchChanged()
        {
            if(IsTracking && match!=null && match.Generation!=generation) RestoreImmediate();
        }
        void OnLaunch(ProjectileRunner runner)
        {
            if(settings==null || orbit==null || !orbit.isActiveAndEnabled || viewCamera==null) return;
            // A new AI/human shot can begin during the previous return. Keep the original user view.
            if(!IsTracking) { savedPosition=cameraTransform.position;savedRotation=cameraTransform.rotation;savedFov=viewCamera.fieldOfView; }
            DetachMembers();members.Clear();Register(runner);
            generation=match!=null?match.Generation:0;
            heading=runner.Params.direction;heading.y=0;
            if(heading.sqrMagnitude<.00001f) heading=Vector3.forward;else heading.Normalize();
            Focus=runner.Position;FamilyRadius=0;impactApproach=false;timer=0;
            wideView=settings.casualPresentation && !shots.SpecialSelected;
            if(wideView)
            {
                var trajectory=new ArtilleryShot(runner.Params.origin,runner.Params.direction,shots.Power,shots.tuning.gravity,runner.Params.wind);
                var bounds=new Bounds(runner.Position,Vector3.zero);
                int samples=Mathf.Clamp(settings.wideSamples,8,128);
                float duration=shots.LastSolveHit?runner.Params.arrivalTime:Mathf.Min(runner.Params.arrivalTime,settings.wideMissSeconds);
                for(int i=1;i<=samples;i++) bounds.Encapsulate(trajectory.PositionAt(duration*i/samples));
                Focus=bounds.center;FamilyRadius=bounds.extents.magnitude;
                // Preserve the viewing side and frame the whole flight once instead of chasing it.
                heading=Focus-cameraTransform.position;heading.y=0;
                if(heading.sqrMagnitude<.001f) heading=Vector3.forward;else heading.Normalize();
            }
            orbit.SetCinematicOverride(true);Phase=ViewPhase.Following;
        }
        void Register(ProjectileRunner runner)
        {
            members.Add(new Member{runner=runner,last=runner.Position});
            runner.SubmunitionLaunched+=OnChild;runner.Finished+=OnMemberFinished;runner.ImpactSoon+=OnImpactSoon;
        }
        void OnChild(ProjectileRunner parent,ProjectileRunner child)
        {
            if(Phase!=ViewPhase.Following) return;
            foreach(var member in members) if(member.runner==parent) { member.hasChildren=true;break; }
            Register(child);
        }
        void OnMemberFinished(ProjectileRunner runner)
        {
            foreach(var member in members) if(member.runner==runner) { member.last=runner.Position;member.finished=true;break; }
        }
        void OnImpactSoon(ProjectileRunner runner)
        {
            if(Phase!=ViewPhase.Following) return;
            // Retain the first forecast; later children must not pull the camera from blast to blast.
            if(!impactApproach) { impactPoint=runner.Prediction.FirstVisibleImpactPosition;impactApproach=true; }
        }
        void OnFamilyFinished()
        {
            if(Phase!=ViewPhase.Following) return;
            UpdateBounds();DetachMembers();
            Phase=ViewPhase.ImpactHold;timer=0;
        }
        void DetachMembers()
        {
            foreach(var member in members)
            {
                if(member.runner==null) continue;
                member.runner.SubmunitionLaunched-=OnChild;member.runner.Finished-=OnMemberFinished;member.runner.ImpactSoon-=OnImpactSoon;
            }
        }
        void LateUpdate() { TickPresentation(Time.deltaTime); }
        public void TickPresentation(float deltaTime)
        {
            if(!IsTracking || settings==null || viewCamera==null || !(deltaTime>0) || float.IsInfinity(deltaTime) ||
                Time.timeScale<=0 || (shots!=null && shots.GameplaySuspended)) return;
            if(orbit==null || !orbit.isActiveAndEnabled) { RestoreImmediate();return; }
            if(Phase==ViewPhase.Following) UpdateBounds();
            if(Phase==ViewPhase.Following || Phase==ViewPhase.ImpactHold)
            {
                Follow(deltaTime);
                if(Phase==ViewPhase.ImpactHold)
                {
                    timer+=deltaTime;
                    if(timer>=settings.impactHoldSeconds)
                    {
                        if(orbit.PrepareCasualReturn(out var aimPosition,out var aimRotation)) { savedPosition=aimPosition;savedRotation=aimRotation; }
                        timer=0;returnStartPosition=cameraTransform.position;returnStartRotation=cameraTransform.rotation;returnStartFov=viewCamera.fieldOfView;Phase=ViewPhase.Returning;
                    }
                }
            }
            else if(Phase==ViewPhase.Returning)
            {
                timer+=deltaTime;float t=Mathf.Clamp01(timer/Mathf.Max(.01f,settings.returnSeconds));float smooth=t*t*(3-2*t);
                cameraTransform.SetPositionAndRotation(ProtectGround(Vector3.Lerp(returnStartPosition,savedPosition,smooth)),Quaternion.Slerp(returnStartRotation,savedRotation,smooth));
                viewCamera.fieldOfView=Mathf.Lerp(returnStartFov,savedFov,smooth);
                if(t>=1) RestoreImmediate();
            }
        }
        void UpdateBounds()
        {
            if(wideView) return;
            bool any=false;Bounds bounds=default;
            foreach(var member in members)
            {
                if(member.hasChildren) continue;
                if(!member.finished && member.runner!=null) member.last=member.runner.Position;
                if(!any) { bounds=new Bounds(member.last,Vector3.zero);any=true; } else bounds.Encapsulate(member.last);
            }
            if(!any) return;
            if(impactApproach) bounds.Encapsulate(impactPoint);
            Focus=bounds.center;FamilyRadius=bounds.extents.magnitude;
        }
        void Follow(float dt)
        {
            bool overview=wideView || impactApproach || members.Count>1 || Phase==ViewPhase.ImpactHold;
            float fov=overview?settings.overviewFieldOfView:settings.flightFieldOfView;
            float halfAngle=Mathf.Atan(Mathf.Tan(fov*Mathf.Deg2Rad*.5f)*Mathf.Min(1,viewCamera.aspect));
            float distance=Mathf.Max(wideView?settings.wideMinDistance:overview?settings.overviewDistance:settings.chaseDistance,
                (FamilyRadius+settings.framingMargin)*settings.framingPadding/Mathf.Max(.1f,Mathf.Sin(halfAngle)));
            Vector3 right=Vector3.Cross(Vector3.up,heading);
            Vector3 offset=-heading*distance+right*(wideView?0:settings.sideOffset)+Vector3.up*(wideView?settings.wideHeight:overview?settings.overviewHeight:settings.chaseHeight);
            var desired=ProtectSightLine(Focus,Focus+offset);
            float positionBlend=1-Mathf.Exp(-Mathf.Max(.1f,wideView?settings.wideResponse:settings.positionResponse)*dt);
            Vector3 position=ProtectGround(Vector3.Lerp(cameraTransform.position,desired,positionBlend));
            Vector3 toward=Focus-position;
            Quaternion rotation=toward.sqrMagnitude>.00001f?Quaternion.LookRotation(toward,Vector3.up):cameraTransform.rotation;
            float rotationBlend=1-Mathf.Exp(-Mathf.Max(.1f,wideView?settings.wideResponse:settings.rotationResponse)*dt);
            cameraTransform.SetPositionAndRotation(position,Quaternion.Slerp(cameraTransform.rotation,rotation,rotationBlend));
            viewCamera.fieldOfView=Mathf.Lerp(viewCamera.fieldOfView,fov,positionBlend);
        }
        Vector3 ProtectSightLine(Vector3 focus,Vector3 desired)
        {
            float required=desired.y;int count=Mathf.Clamp(settings.terrainSamples,4,64);
            // Ignore the immediate impact surface; otherwise a ground-level focus amplifies a tiny
            // height discrepancy into an excessive rise. Camera endpoint protection is unconditional.
            for(int i=2;i<=count;i++)
            {
                float t=(float)i/count;var sample=Vector3.Lerp(focus,desired,t);
                if(TryGround(sample.x,sample.z,out float ground)) required=Mathf.Max(required,focus.y+(ground+settings.groundClearance-focus.y)/t);
            }
            desired.y=Mathf.Min(required,desired.y+settings.maximumSightlineLift);return ProtectGround(desired);
        }
        Vector3 ProtectGround(Vector3 position)
        {
            if(settings!=null && TryGround(position.x,position.z,out float ground)) position.y=Mathf.Max(position.y,ground+settings.groundClearance+(viewCamera!=null?viewCamera.nearClipPlane:0));
            return position;
        }
        bool TryGround(float x,float z,out float height)
        {
            var map=shots!=null?shots.terrainMap:null;
            if(map!=null && map.Field!=null && map.Field.TrySample(x,z,out height)) return true;
            if(exterior!=null && exterior.TrySampleVisualHeight(x,z,out height)) return true;
            height=0;return false;
        }
        public void RestoreImmediate()
        {
            DetachMembers();members.Clear();
            if(!IsTracking) return;
            Phase=ViewPhase.Idle;timer=0;
            if(cameraTransform!=null) cameraTransform.SetPositionAndRotation(ProtectGround(savedPosition),savedRotation);
            if(viewCamera!=null) viewCamera.fieldOfView=savedFov;
            if(orbit!=null) orbit.SetCinematicOverride(false);
        }
    }
}
