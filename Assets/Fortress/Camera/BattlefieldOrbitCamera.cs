using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
namespace Fortress
{
    /// <summary>User orbit only. Turn changes and projectile events never reset the viewing pose.</summary>
    [DisallowMultipleComponent]
    public sealed class BattlefieldOrbitCamera : MonoBehaviour
    {
        public MatchController match;
        public BattlefieldExterior exterior;
        public OrbitCameraSettings settings;
        public Vector3 initialTarget=new Vector3(0,3,0);
        public bool initializeFromCurrentPose=true;
        public Vector3 Target { get; private set; }
        public float Yaw { get; private set; }
        public float Pitch { get; private set; }
        public float Distance { get; private set; }
        public bool IsDragging => dragging;
        Transform cameraTransform;
        bool initialized,dragging,hasFocus=true;
        bool Suspended => match!=null && match.shots!=null && match.shots.GameplaySuspended;
        void Awake() { cameraTransform=transform;Initialize(); }
        public void Initialize()
        {
            if(initialized || settings==null) return;
            if(cameraTransform==null) cameraTransform=transform;
            Target=initialTarget;
            var offset=cameraTransform.position-Target;
            Distance=Mathf.Clamp(offset.magnitude,MinimumDistance,MaximumDistance);
            if(initializeFromCurrentPose && offset.sqrMagnitude>0)
            {
                Yaw=Mathf.Atan2(offset.x,offset.z)*Mathf.Rad2Deg;
                Pitch=Mathf.Asin(Mathf.Clamp(offset.normalized.y,-1,1))*Mathf.Rad2Deg;
            }
            else { Yaw=0;Pitch=MinimumPitch; }
            Pitch=Mathf.Clamp(Pitch,MinimumPitch,MaximumPitch);
            initialized=true;ApplyPose();
        }
        float MinimumPitch=>Mathf.Clamp(settings.minPitch,1,89);
        float MaximumPitch=>Mathf.Clamp(settings.maxPitch,MinimumPitch,89);
        float MinimumDistance=>Mathf.Max(.1f,settings.minDistance);
        float MaximumDistance=>Mathf.Max(MinimumDistance,settings.maxDistance);
        public void Orbit(Vector2 pixelDelta)
        {
            if(Suspended) return;
            Initialize();if(!initialized || !Finite(pixelDelta.x) || !Finite(pixelDelta.y)) return;
            Yaw=Mathf.Repeat(Yaw+pixelDelta.x*settings.orbitDegreesPerPixel+180,360)-180;
            Pitch=Mathf.Clamp(Pitch-pixelDelta.y*settings.orbitDegreesPerPixel,MinimumPitch,MaximumPitch);
        }
        /// <param name="wheelDelta">Raw Input System scroll y units (120 is a conventional wheel notch).</param>
        public void Zoom(float wheelDelta)
        {
            if(Suspended) return;
            Initialize();if(!initialized || !Finite(wheelDelta)) return;
            Distance=Mathf.Clamp(Distance*Mathf.Exp(-wheelDelta/120f*settings.zoomSpeed),MinimumDistance,MaximumDistance);
        }
        public void FocusActiveTank()
        {
            if(Suspended) return;
            Initialize();
            if(!initialized || match==null || match.shots==null || match.shots.ActiveTank==null) return;
            Target=match.shots.ActiveTank.transform.position+Vector3.up*settings.focusHeight;
        }
        void LateUpdate()
        {
            if(Suspended) { dragging=false;return; }
            Initialize();if(!initialized) return;
            ReadInput();ApplyPose();
        }
        void ReadInput()
        {
            if(!hasFocus) return;
            var mouse=Mouse.current;
            if(mouse!=null)
            {
                bool pressed=mouse.rightButton.wasPressedThisFrame;
                if(pressed) dragging=!PointerOverUi();
                if(mouse.rightButton.wasReleasedThisFrame || !mouse.rightButton.isPressed) dragging=false;
                if(dragging && !pressed) Orbit(mouse.delta.ReadValue());
                float scroll=mouse.scroll.ReadValue().y;
                if(scroll!=0 && !PointerOverUi())
                {
                    // Input System now defaults to one unit per notch. Zoom keeps its
                    // existing 120-unit API for callers using legacy Windows deltas.
                    bool nativeWindows=InputSystem.settings.scrollDeltaBehavior==InputSettings.ScrollDeltaBehavior.KeepPlatformSpecificInputRange &&
                        (Application.platform==RuntimePlatform.WindowsEditor || Application.platform==RuntimePlatform.WindowsPlayer);
                    Zoom(nativeWindows?scroll:scroll*120f);
                }
            }
            else dragging=false;
            var keyboard=Keyboard.current;
            if(keyboard!=null && keyboard.fKey.wasPressedThisFrame && !PointerOverUi()) FocusActiveTank();
        }
        static bool PointerOverUi() => EventSystem.current!=null && EventSystem.current.IsPointerOverGameObject();
        void OnApplicationFocus(bool focused) { hasFocus=focused;if(!focused) dragging=false; }
        void OnDisable() { dragging=false; }
        public void ApplyPose()
        {
            if(!initialized || settings==null) return;
            float yaw=Yaw*Mathf.Deg2Rad,pitch=Pitch*Mathf.Deg2Rad;
            Vector3 position=Target+new Vector3(Mathf.Sin(yaw)*Mathf.Cos(pitch),Mathf.Sin(pitch),Mathf.Cos(yaw)*Mathf.Cos(pitch))*Distance;
            var field=match!=null && match.terrainMap!=null?match.terrainMap.Field:null;
            if(field!=null)
            {
                float clearance=Mathf.Max(0,settings.groundClearance);
                int samples=Mathf.Clamp(settings.terrainSamples,1,128);
                // Raise the viewing end of the segment when hills would cover the target/camera.
                // Camera-end sample always enforces exact local ground clearance.
                float requiredY=position.y;
                for(int i=1;i<=samples;i++)
                {
                    float t=(float)i/samples;
                    var sample=Vector3.Lerp(Target,position,t);
                    if(field.TrySample(sample.x,sample.z,out float ground) ||
                        (exterior!=null && exterior.TrySampleVisualHeight(sample.x,sample.z,out ground)))
                        requiredY=Mathf.Max(requiredY,Target.y+(ground+clearance-Target.y)/t);
                }
                position.y=requiredY;
            }
            cameraTransform.SetPositionAndRotation(position,Quaternion.LookRotation(Target-position,Vector3.up));
        }
        static bool Finite(float value)=>!float.IsNaN(value)&&!float.IsInfinity(value);
    }
}
