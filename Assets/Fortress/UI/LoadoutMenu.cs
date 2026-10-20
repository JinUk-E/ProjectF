using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace Fortress
{
    public sealed class LoadoutMenu : MonoBehaviour
    {
        public TankLoadout[] roles;
        public TMP_Text playerLabel,opponentLabel;
        public TMP_Text roleDescription, ammunitionLabel;
        public Image selectedPortrait;
        public Button[] playerCards;
        public GameObject[] selectionMarkers;
        public Image[] cardBorders;
        public Color selectedColor=new Color(.72f,.78f,.51f);
        public Color idleColor=new Color(.18f,.25f,.21f);
        void OnEnable() { Refresh(); }
        public void NextPlayer() { if(roles!=null && roles.Length>0) SelectPlayer((BattleLoadoutSelection.Player+1)%roles.Length); }
        public void SelectPlayer(int index)
        {
            if(roles==null || index<0 || index>=roles.Length || roles[index]==null) return;
            BattleLoadoutSelection.Player=index;Refresh();
        }
        public void NextOpponent() { if(roles!=null && roles.Length>0) { BattleLoadoutSelection.Opponent=(BattleLoadoutSelection.Opponent+1)%roles.Length;Refresh(); } }
        void Refresh()
        {
            if(roles==null || roles.Length==0) return;
            BattleLoadoutSelection.Player=Mathf.Clamp(BattleLoadoutSelection.Player,0,roles.Length-1);
            BattleLoadoutSelection.Opponent=Mathf.Clamp(BattleLoadoutSelection.Opponent,0,roles.Length-1);
            var role=roles[BattleLoadoutSelection.Player];
            if(playerLabel!=null) playerLabel.text=KoreanUi.RoleName(role);
            if(opponentLabel!=null) opponentLabel.text=Describe("상대 / 플레이어 2",roles[BattleLoadoutSelection.Opponent]);
            var appearance=role!=null?role.appearance:null;
            if(selectedPortrait!=null) { selectedPortrait.sprite=appearance!=null?appearance.portrait:null;selectedPortrait.enabled=selectedPortrait.sprite!=null; }
            if(roleDescription!=null) roleDescription.text=KoreanUi.RoleDescription(role);
            if(ammunitionLabel!=null) ammunitionLabel.text=AmmoDescription(role);
            if(selectionMarkers!=null) for(int i=0;i<selectionMarkers.Length;i++)
                if(selectionMarkers[i]!=null) selectionMarkers[i].SetActive(i==BattleLoadoutSelection.Player);
            if(cardBorders!=null) for(int i=0;i<cardBorders.Length;i++)
                if(cardBorders[i]!=null) cardBorders[i].color=i==BattleLoadoutSelection.Player?selectedColor:idleColor;
        }
        public static string AmmoDescription(TankLoadout role)
        {
            return KoreanUi.AmmoDescription(role);
        }
        static string Describe(string seat,TankLoadout role)=>seat+"   >   "+KoreanUi.RoleName(role)+"\n"+AmmoDescription(role)+"   [변경]";
    }
}
