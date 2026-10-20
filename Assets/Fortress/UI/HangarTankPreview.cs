using UnityEngine;
namespace Fortress
{
    public sealed class HangarTankPreview : MonoBehaviour
    {
        public GameObject[] models;
        int selected=-1;
        void OnEnable() { selected=-1;Refresh(); }
        void Update() { Refresh(); }
        void Refresh()
        {
            if(models==null || models.Length==0)return;
            int next=Mathf.Clamp(BattleLoadoutSelection.Player,0,models.Length-1);
            if(next==selected)return;
            selected=next;
            for(int i=0;i<models.Length;i++)if(models[i]!=null)models[i].SetActive(i==selected);
        }
    }
}
