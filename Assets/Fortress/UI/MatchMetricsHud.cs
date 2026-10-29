using TMPro;
using UnityEngine;
namespace Fortress
{
    /// <summary>Local round summary; updates only when statistics publish a change.</summary>
    public sealed class MatchMetricsHud : MonoBehaviour
    {
        public MatchStatistics statistics;
        public TMP_Text summary;
        void OnEnable() { if(statistics!=null)statistics.Changed+=Refresh;Refresh(); }
        void OnDisable() { if(statistics!=null)statistics.Changed-=Refresh; }
        public void Refresh()
        {
            if(statistics==null || summary==null)return;
            int seconds=Mathf.Max(0,Mathf.FloorToInt(statistics.ElapsedSeconds));
            summary.text="대전 "+(seconds/60).ToString("00")+":"+(seconds%60).ToString("00")+"  ·  총 "+statistics.CompletedShots+"발\n"+
                Seat(0)+"\n"+Seat(1)+"\n침수 패배 "+statistics.DrownedDefeats+"대";
        }
        string Seat(int index)
        {
            int shots=statistics.ShotsByPlayer[index],hits=statistics.HitsByPlayer[index];
            int percent=shots>0?Mathf.RoundToInt(100f*hits/shots):0;
            string seat=index==0?"플레이어 1":statistics.match!=null && statistics.match.shots.ai!=null?"AI":"플레이어 2";
            return seat+"  명중 "+hits+"/"+shots+" ("+percent+"%)  ·  피해 "+statistics.DamageByPlayer[index].ToString("0.#");
        }
    }
}
