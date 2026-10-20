using TMPro;
using UnityEngine;
namespace Fortress
{
    public sealed class LoadoutMenu : MonoBehaviour
    {
        public TankLoadout[] roles;
        public TMP_Text playerLabel,opponentLabel;
        void OnEnable() { Refresh(); }
        public void NextPlayer() { BattleLoadoutSelection.Player=(BattleLoadoutSelection.Player+1)%roles.Length;Refresh(); }
        public void NextOpponent() { BattleLoadoutSelection.Opponent=(BattleLoadoutSelection.Opponent+1)%roles.Length;Refresh(); }
        void Refresh()
        {
            if(roles==null || roles.Length==0) return;
            BattleLoadoutSelection.Player=Mathf.Clamp(BattleLoadoutSelection.Player,0,roles.Length-1);
            BattleLoadoutSelection.Opponent=Mathf.Clamp(BattleLoadoutSelection.Opponent,0,roles.Length-1);
            playerLabel.text=Describe("PLAYER 1",roles[BattleLoadoutSelection.Player]);
            opponentLabel.text=Describe("OPPONENT / PLAYER 2",roles[BattleLoadoutSelection.Opponent]);
        }
        static string Describe(string seat,TankLoadout role)=>seat+"   >   "+role.displayName+"\n"+(role.special!=null?"HE + "+role.special.displayName+" x"+role.specialRounds:"HE / UNLIMITED")+"   [CHANGE]";
    }
}
