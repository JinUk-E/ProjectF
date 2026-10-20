using System.Collections.Generic;
using UnityEngine;
namespace Fortress
{
    /// <summary>Presentation replacement only. Keeps aim pivots, muzzle, recoil and combat capsule intact.</summary>
    public sealed class TankAppearanceView : MonoBehaviour
    {
        public FlatShotController shots;
        public TankRig rig;
        public Transform gunParent;
        public int tankIndex;
        public TankAppearance CurrentAppearance { get; private set; }
        Renderer[] original;
        bool[] originalEnabled;
        readonly List<GameObject> views=new List<GameObject>();
        void Awake()
        {
            original=GetComponentsInChildren<Renderer>(true);originalEnabled=new bool[original.Length];
            for(int i=0;i<original.Length;i++)originalEnabled[i]=original[i].enabled;
        }
        void OnEnable() { if(shots!=null)shots.StateChanged+=Refresh;Refresh(); }
        void Start() { Refresh(); }
        void OnDisable()
        {
            if(shots!=null)shots.StateChanged-=Refresh;
            Clear();RestoreOriginal();
        }
        public void Refresh()
        {
            if(shots==null || rig==null || rig.turret==null || rig.barrel==null || original==null)return;
            var role=shots.GetLoadout(tankIndex);var appearance=role!=null?role.appearance:null;
            if(appearance==CurrentAppearance)return;
            Clear();
            if(appearance==null || appearance.pieces==null || appearance.pieces.Length==0){RestoreOriginal();return;}
            foreach(var renderer in original)if(renderer!=null)renderer.enabled=false;
            foreach(var piece in appearance.pieces)
            {
                if(piece.mesh==null)continue;
                var go=new GameObject("Role visual "+piece.mesh.name,typeof(MeshFilter),typeof(MeshRenderer));
                go.transform.SetParent(piece.mount==0?rig.transform:piece.mount==1?rig.turret:gunParent!=null?gunParent:rig.barrel,false);
                go.GetComponent<MeshFilter>().sharedMesh=piece.mesh;
                go.GetComponent<MeshRenderer>().sharedMaterials=tankIndex==0?piece.playerMaterials:piece.opponentMaterials;
                views.Add(go);
            }
            CurrentAppearance=appearance;
        }
        void Clear()
        {
            foreach(var go in views)if(go!=null){go.SetActive(false);Destroy(go);}
            views.Clear();CurrentAppearance=null;
        }
        void RestoreOriginal()
        {
            if(original==null)return;
            for(int i=0;i<original.Length;i++)if(original[i]!=null)original[i].enabled=originalEnabled[i];
        }
    }
}
