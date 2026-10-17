using UnityEngine;
namespace Fortress
{
    public sealed class CoverBuilding
    {
        public Bounds Bounds { get; }
        public float Health { get; private set; }
        public bool Destroyed => Health<=0;
        public CoverBuilding(Bounds bounds,float health) { Bounds=bounds;Health=health; }
        internal void Damage(float damage) { Health=Mathf.Max(0,Health-Mathf.Max(0,damage)); }
        internal void Collapse() { Health=0; }
        // Finite segment/AABB slabs, also handles segments starting inside the building.
        public static bool Intersect(Bounds bounds,Vector3 from,Vector3 to,out Vector3 hit)
        {
            Vector3 d=to-from,min=bounds.min,max=bounds.max;float enter=0,exit=1;
            for(int axis=0;axis<3;axis++)
            {
                if(Mathf.Abs(d[axis])<.000001f) { if(from[axis]<min[axis]||from[axis]>max[axis]) {hit=default;return false;} }
                else
                {
                    float a=(min[axis]-from[axis])/d[axis],b=(max[axis]-from[axis])/d[axis];
                    enter=Mathf.Max(enter,Mathf.Min(a,b));exit=Mathf.Min(exit,Mathf.Max(a,b));
                    if(enter>exit) {hit=default;return false;}
                }
            }
            hit=from+d*enter;return true;
        }
    }
}
