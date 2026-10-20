using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
namespace Fortress
{
    public sealed class AmmoHud : MonoBehaviour
    {
        public FlatShotController shots;
        public Button basicButton,specialButton;
        public TMP_Text basicLabel,specialLabel,roleLabel;
        void OnEnable()
        {
            if(shots!=null) shots.StateChanged+=Refresh;
            if(basicButton!=null) basicButton.onClick.AddListener(Basic);
            if(specialButton!=null) specialButton.onClick.AddListener(Special);
            Refresh();
        }
        void OnDisable()
        {
            if(shots!=null) shots.StateChanged-=Refresh;
            if(basicButton!=null) basicButton.onClick.RemoveListener(Basic);
            if(specialButton!=null) specialButton.onClick.RemoveListener(Special);
        }
        void Update()
        {
            var keys=Keyboard.current;if(keys==null) return;
            if(keys.digit1Key.wasPressedThisFrame) Basic();
            if(keys.digit2Key.wasPressedThisFrame) Special();
        }
        public void Basic() { if(shots!=null) shots.SelectAmmo(false); }
        public void Special() { if(shots!=null) shots.SelectAmmo(true); }
        public void Refresh()
        {
            if(shots==null || shots.ActiveLoadout==null) return;
            var role=shots.ActiveLoadout;
            bool editable=shots.InputEnabled && !shots.IsFiring && !shots.GameplaySuspended && (shots.ai==null || !shots.ai.OwnsCurrentTurn);
            basicButton.interactable=editable;
            specialButton.interactable=editable && role.special!=null && shots.SpecialRemaining>0;
            roleLabel.text=role.displayName+"  /  AMMUNITION";
            basicLabel.text=(shots.SpecialSelected?"":"[READY] ")+"1  HE / UNLIMITED";
            specialLabel.text=role.special==null?"NO SPECIAL AMMUNITION":(shots.SpecialSelected?"[READY] ":"")+"2  "+role.special.displayName+" / "+shots.SpecialRemaining;
        }
    }
}
