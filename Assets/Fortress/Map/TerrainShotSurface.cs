using System;
using UnityEngine;
using UnityProjectile;

namespace Fortress
{
    /// <summary>Clipped cell traversal; bilinear height along a segment is quadratic.
    /// Solves both roots in every traversed cell, including above-to-above ridge crossings.</summary>
    public sealed class TerrainShotSurface : IShotSurface
    {
        public HeightField Field { get; }
        public TerrainShotSurface(HeightField field) { Field = field ?? throw new ArgumentNullException(nameof(field)); }
        public bool TryHit(Vector3 from, Vector3 to, out Vector3 hit)
        {
            hit = default;
            Vector3 d = to-from;
            double enter = 0, exit = 1;
            if (!Clip(from.x,d.x,Field.Origin.x,Field.Origin.x+Field.Size.x,ref enter,ref exit) ||
                !Clip(from.z,d.z,Field.Origin.z,Field.Origin.z+Field.Size.y,ref enter,ref exit)) return false;
            double t = enter;
            int limit = Field.Width + Field.Depth + 4;
            for (int step=0; step<limit && t<=exit; step++)
            {
                double gx = (from.x+d.x*t-Field.Origin.x)/Field.CellX;
                double gz = (from.z+d.z*t-Field.Origin.z)/Field.CellZ;
                int ix = Cell(gx,d.x,Field.Width), iz=Cell(gz,d.z,Field.Depth);
                double tx = d.x == 0 ? double.PositiveInfinity :
                    (Field.Origin.x+(d.x>0?ix+1:ix)*Field.CellX-from.x)/d.x;
                double tz = d.z == 0 ? double.PositiveInfinity :
                    (Field.Origin.z+(d.z>0?iz+1:iz)*Field.CellZ-from.z)/d.z;
                double end = Math.Min(exit,Math.Min(tx,tz));
                // Rounding at a cell boundary must never skip a full cell or stall traversal.
                if (end < t) end = t;
                double q0 = Clearance(from,d,t,ix,iz), q1=Clearance(from,d,end,ix,iz);
                double qm=Clearance(from,d,(t+end)*0.5,ix,iz);
                double a=2*(q0+q1-2*qm), b=q1-q0-a;
                if (FirstRoot(a,b,q0,out double u))
                {
                    hit = from+d*(float)(t+(end-t)*u);
                    return true;
                }
                if (end>=exit) break;
                t = end;
                // Offset only the cell choice, not the tested geometric segment.
                if (tx<=tz && ix==(d.x>0?Field.Width-2:0) && end==tx && tx<exit) break;
                if (tz<=tx && iz==(d.z>0?Field.Depth-2:0) && end==tz && tz<exit) break;
            }
            return false;
        }
        static int Cell(double grid,float direction,int count)
        {
            double rounded=Math.Round(grid);
            if (Math.Abs(grid-rounded)<1e-6) grid=rounded+(direction<0?-1e-7:1e-7);
            return Math.Max(0,Math.Min(count-2,(int)Math.Floor(grid)));
        }
        double Clearance(Vector3 p,Vector3 d,double t,int x,int z)
        {
            double u=(p.x+d.x*t-Field.Origin.x)/Field.CellX-x;
            double v=(p.z+d.z*t-Field.Origin.z)/Field.CellZ-z;
            double h00=Field.GetHeight(x,z),h10=Field.GetHeight(x+1,z),h01=Field.GetHeight(x,z+1),h11=Field.GetHeight(x+1,z+1);
            double h=h00+(h10-h00)*u+(h01-h00)*v+(h11-h10-h01+h00)*u*v;
            return p.y+d.y*t-h;
        }
        static bool FirstRoot(double a,double b,double c,out double root)
        {
            root=0;
            if(c<=0) return true;
            if(Math.Abs(a)<1e-12)
            {
                if(Math.Abs(b)<1e-12) return false;
                root=-c/b; return root>=0 && root<=1;
            }
            double disc=b*b-4*a*c;
            if(disc<0) return false;
            double sqrt=Math.Sqrt(disc);
            double r0=(-b-sqrt)/(2*a),r1=(-b+sqrt)/(2*a);
            if(r0>r1) { double tmp=r0;r0=r1;r1=tmp; }
            if(r0>=0 && r0<=1) { root=r0;return true; }
            root=r1;return r1>=0 && r1<=1;
        }
        static bool Clip(double start,double delta,double min,double max,ref double enter,ref double exit)
        {
            if(delta==0) return start>=min && start<=max;
            double a=(min-start)/delta,b=(max-start)/delta;
            if(a>b) { double tmp=a;a=b;b=tmp; }
            enter=Math.Max(enter,a);exit=Math.Min(exit,b);return enter<=exit;
        }
    }
}
