using UnityEngine;
using UnityEngine.UI;
namespace Fortress
{
    public sealed class CombatPanelControls : MonoBehaviour
    {
        public FlatShotController shots;
        public Button[] buttons;
        bool enabledLast;
        bool Editable=>shots!=null && shots.isActiveAndEnabled && shots.ActiveTank!=null && shots.InputEnabled && !shots.GameplaySuspended && !shots.IsFiring && (shots.ai==null || !shots.ai.OwnsCurrentTurn);
        void OnEnable() { enabledLast=!Editable;Refresh(); }
        void Update() { Refresh(); }
        void Refresh()
        {
            bool enabledNow=Editable;if(enabledNow==enabledLast)return;enabledLast=enabledNow;
            if(buttons!=null)foreach(var button in buttons)if(button!=null)button.interactable=enabledNow;
        }
        public void ElevationUp() { Adjust(1,0); }
        public void ElevationDown() { Adjust(-1,0); }
        public void PowerUp() { Adjust(0,1); }
        public void PowerDown() { Adjust(0,-1); }
        void Adjust(float pitch,float power) { if(Editable)shots.SetAim(shots.ActiveTank.Yaw,shots.ActiveTank.Pitch+pitch,shots.Power+power); }
        public void Fire() { if(Editable)shots.TryFire(); }
    }
}
