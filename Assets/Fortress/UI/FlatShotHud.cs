using TMPro;
using UnityEngine;
namespace Fortress
{
    public sealed class FlatShotHud : MonoBehaviour
    {
        public FlatShotController controller;
        public TMP_Text values;
        public TMP_Text status;
        void OnEnable()
        {
            if (controller != null) controller.StateChanged += Refresh;
            Refresh();
        }
        void OnDisable()
        {
            if (controller != null) controller.StateChanged -= Refresh;
        }
        void Refresh()
        {
            if (controller == null || controller.ActiveTank == null) return;
            if (values != null) values.SetText("DIRECTION {0:0.0}   ELEVATION {1:0.0}   POWER {2:0.0}",
                controller.ActiveTank.Yaw, controller.ActiveTank.Pitch, controller.Power);
            if (status != null) status.text = controller.IsFiring ? "SHELL IN FLIGHT / AIM LOCKED" : "READY / SPACE TO FIRE";
        }
    }
}
