using UnityEngine;
using UnityEngine.InputSystem;

namespace Fortress
{
    public sealed class AimInput : MonoBehaviour
    {
        public FlatShotController controller;
        public AiOpponent ai;
        InputAction yaw;
        InputAction pitch;
        InputAction power;
        InputAction fire;

        void Awake()
        {
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
            yaw.Enable(); pitch.Enable(); power.Enable(); fire.Enable();
        }
        void OnDisable()
        {
            if (fire != null) fire.performed -= OnFire;
            yaw?.Disable(); pitch?.Disable(); power?.Disable(); fire?.Disable();
        }
        void OnDestroy()
        {
            yaw?.Dispose(); pitch?.Dispose(); power?.Dispose(); fire?.Dispose();
        }
        void OnFire(InputAction.CallbackContext context)
        {
            if (controller != null && (ai == null || !ai.OwnsCurrentTurn)) controller.TryFire();
        }
        void Update()
        {
            if ((ai != null && ai.OwnsCurrentTurn) || controller == null || controller.IsFiring || controller.ActiveTank == null || controller.tuning == null) return;
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
