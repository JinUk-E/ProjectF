using UnityEngine;
using UnityEngine.InputSystem;

namespace Fortress
{
    public sealed class AimInput : MonoBehaviour
    {
        public FlatShotController controller;
        public AiOpponent ai;
        public MatchController match;
        public Transform movementCamera;
        InputAction yaw;
        InputAction pitch;
        InputAction power;
        InputAction fire;
        InputAction movement;

        void Awake()
        {
            if(movementCamera==null && UnityEngine.Camera.main!=null) movementCamera=UnityEngine.Camera.main.transform;
            movement=new InputAction("Move",InputActionType.Value);
            movement.AddCompositeBinding("2DVector").With("Up","<Keyboard>/upArrow").With("Down","<Keyboard>/downArrow").With("Left","<Keyboard>/leftArrow").With("Right","<Keyboard>/rightArrow");
            yaw = Axis("Yaw", "<Keyboard>/a", "<Keyboard>/d");
            pitch = Axis("Pitch", "<Keyboard>/s", "<Keyboard>/w");
            power = Axis("Power", "<Keyboard>/q", "<Keyboard>/e");
            fire = new InputAction("Fire", InputActionType.Button, "<Keyboard>/space");
        }
        static InputAction Axis(string name, string negative, string positive)
        {
            var action = new InputAction(name, InputActionType.Value);
            action.AddCompositeBinding("1DAxis").With("Negative", negative).With("Positive", positive);
            return action;
        }
        void OnEnable()
        {
            fire.performed += OnFire;
            yaw.Enable(); pitch.Enable(); power.Enable(); fire.Enable(); movement.Enable();
        }
        void OnDisable()
        {
            if (fire != null) fire.performed -= OnFire;
            yaw?.Disable(); pitch?.Disable(); power?.Disable(); fire?.Disable(); movement?.Disable();
        }
        void OnDestroy()
        {
            yaw?.Dispose(); pitch?.Dispose(); power?.Dispose(); fire?.Dispose(); movement?.Dispose();
        }
        void OnFire(InputAction.CallbackContext context)
        {
            if (controller != null && (ai == null || !ai.OwnsCurrentTurn)) controller.TryFire();
        }
        void Update()
        {
            if ((ai != null && ai.OwnsCurrentTurn) || controller == null || !controller.InputEnabled || controller.IsFiring || controller.ActiveTank == null || controller.tuning == null) return;
            Vector2 move=movement.ReadValue<Vector2>();
            if(match!=null && move.sqrMagnitude>0)
            {
                Vector3 forward=movementCamera!=null?movementCamera.forward:Vector3.forward;
                forward.y=0;
                if(forward.sqrMagnitude<=.000001f) forward=Vector3.forward;
                forward.Normalize();
                Vector3 right=Vector3.Cross(Vector3.up,forward);
                Vector3 world=right*move.x+forward*move.y;
                match.TryMoveActiveTank(new Vector2(world.x,world.z),Time.deltaTime);
            }
            float y = yaw.ReadValue<float>(), p = pitch.ReadValue<float>(), s = power.ReadValue<float>();
            if (y == 0 && p == 0 && s == 0) return;
            var tuning = controller.tuning;
            var tank = controller.ActiveTank;
            controller.SetAim(tank.Yaw + y * tuning.yawSpeed * Time.deltaTime,
                tank.Pitch + p * tuning.pitchSpeed * Time.deltaTime,
                controller.Power + s * tuning.powerSpeed * Time.deltaTime);
        }
    }
}
