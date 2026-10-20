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
            roleLabel.text=KoreanUi.RoleName(role)+"  /  탄약";
            basicLabel.text=(shots.SpecialSelected?"":"[선택] ")+"1  "+KoreanUi.AmmoName(role.basic)+" / 무제한";
            specialLabel.text=role.special==null?"전용 특수탄 없음":(shots.SpecialSelected?"[선택] ":"")+"2  "+KoreanUi.AmmoName(role.special)+" / "+shots.SpecialRemaining+"발";
        }
    }
}
