using UnityEngine;
using UnityProjectile;

namespace Fortress
{
    public interface IHeightFieldSurface : IShotSurface { HeightField Field { get; } }

    /// <summary>Analytic vertical tank capsules plus authoritative terrain. Shooter is excluded.</summary>
    public sealed class CombatShotSurface : IHeightFieldSurface
    {
        readonly TerrainShotSurface terrain;
        readonly TankCombatant[] tanks;
        readonly int shooter;
        readonly BattlefieldCover cover;
        public HeightField Field => terrain.Field;
        public CombatShotSurface(HeightField field,TankCombatant[] combatants,int excludedPlayer,BattlefieldCover buildings=null)
        { terrain=new TerrainShotSurface(field);tanks=combatants;shooter=excludedPlayer;cover=buildings; }
        public bool TryHit(Vector3 from,Vector3 to,out Vector3 hit)
        {
            bool found=terrain.TryHit(from,to,out hit);
            float best=found?(hit-from).sqrMagnitude:float.PositiveInfinity;
            if(cover!=null && cover.TryHit(from,to,out var wall) && (wall-from).sqrMagnitude<best)
            { found=true;best=(wall-from).sqrMagnitude;hit=wall; }
            for(int i=0;i<tanks.Length;i++)
            {
                if(i==shooter || tanks[i].IsDefeated) continue;
                if(TryCapsule(from,to,tanks[i],out var point))
                {
                    float distance=(point-from).sqrMagnitude;
                    if(distance<best) { found=true;best=distance;hit=point; }
                }
            }
            return found;
        }
        public static bool TryCapsule(Vector3 from,Vector3 to,TankCombatant tank,out Vector3 hit)
        {
            Vector3 center=tank.Center;
            float half=tank.Height*.5f-tank.Radius;
            Vector3 low=center-Vector3.up*half,high=center+Vector3.up*half;
            Vector3 closest=new Vector3(center.x,Mathf.Clamp(from.y,low.y,high.y),center.z);
            if((from-closest).sqrMagnitude<=tank.Radius*tank.Radius) { hit=from;return true; }
            Vector3 d=to-from;
            double best=double.PositiveInfinity;
            Sphere(from,d,low,tank.Radius,ref best);Sphere(from,d,high,tank.Radius,ref best);
            double ox=from.x-center.x,oz=from.z-center.z;
            double a=d.x*d.x+d.z*d.z,b=2*(ox*d.x+oz*d.z),c=ox*ox+oz*oz-tank.Radius*tank.Radius;
            if(a>0)
            {
                double disc=b*b-4*a*c;
                if(disc>=0)
                {
                    double root=System.Math.Sqrt(disc);
                    CylinderRoot((-b-root)/(2*a),from,d,low.y,high.y,ref best);
                    CylinderRoot((-b+root)/(2*a),from,d,low.y,high.y,ref best);
                }
            }
            hit=default;
            if(double.IsPositiveInfinity(best)) return false;
            hit=from+d*(float)best;return true;
        }
        static void CylinderRoot(double t,Vector3 from,Vector3 d,float min,float max,ref double best)
        { double y=from.y+d.y*t;if(t>=0 && t<=1 && y>=min && y<=max && t<best) best=t; }
        static void Sphere(Vector3 from,Vector3 d,Vector3 center,float radius,ref double best)
        {
            Vector3 o=from-center;double a=Vector3.Dot(d,d);
            if(a==0) return;
            double b=2*Vector3.Dot(o,d),c=Vector3.Dot(o,o)-radius*radius;
            double disc=b*b-4*a*c;if(disc<0) return;
            double t=(-b-System.Math.Sqrt(disc))/(2*a);
            if(t>=0 && t<=1 && t<best) best=t;
        }
    }
}
