using UnityEngine;

namespace Fortress
{
    public sealed class TankRig : MonoBehaviour
    {
        public Transform turret;
        public Transform barrel;
        public Transform muzzle;
        public float Yaw { get; private set; }
        public float Pitch { get; private set; }
        public Transform Muzzle => muzzle;

        // Rig convention: turret and barrel forward are +Z; yaw is world space.
        public void SetAim(float worldYaw, float pitch)
        {
            Yaw = Mathf.Repeat(worldYaw + 180f, 360f) - 180f;
            Pitch = pitch;
            if (turret != null) turret.rotation = Quaternion.Euler(0f, Yaw, 0f);
            if (barrel != null) barrel.localRotation = Quaternion.Euler(-pitch, 0f, 0f);
        }
    }
}

