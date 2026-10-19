using UnityEngine;
namespace Fortress
{
    public sealed class CoverBuilding
    {
        public Bounds Bounds { get; }
        public float Health { get; private set; }
        public bool Destroyed => Health<=0;
        public RubblePiece[] Rubble { get; private set; }=System.Array.Empty<RubblePiece>();
        public CoverBuilding(Bounds bounds,float health) { Bounds=bounds;Health=health; }
        internal void PrepareRubble(HeightField field,CoverSettings settings,int seed)
        {
            Rubble=new RubblePiece[30];var random=new System.Random(unchecked(seed*397^0x1c4ab));
            var size=Bounds.size;float wallHeight=size.y-Mathf.Min(.8f,size.y*.2f);
            float footprint=Mathf.Clamp(settings.rubbleFootprintFraction,.4f,.8f);
            for(int i=0;i<Rubble.Length;i++)
            {
                bool roof=i>=24,side=!roof && i%2==1;
                float width=roof?size.x/3:side?wallHeight/2:size.x*.94f/3;
                float depth=roof?size.z/2:side?size.z*.86f/3:wallHeight/2;
                float thickness=roof?.2f:side?size.x*.04f:size.z*.04f;
                width*=.75f;depth*=.75f;
                float yaw=((float)random.NextDouble()*2-1)*25;
                var rotation=Quaternion.Euler(0,yaw,0);float radians=yaw*Mathf.Deg2Rad;
                float ex=(Mathf.Abs(Mathf.Cos(radians))*width+Mathf.Abs(Mathf.Sin(radians))*depth)*.5f;
                float ez=(Mathf.Abs(Mathf.Sin(radians))*width+Mathf.Abs(Mathf.Cos(radians))*depth)*.5f;
                float x=Bounds.center.x+((float)random.NextDouble()*2-1)*Mathf.Max(0,size.x*footprint*.5f-ex);
                float z=Bounds.center.z+((float)random.NextDouble()*2-1)*Mathf.Max(0,size.z*footprint*.5f-ez);
                float elevation=thickness*.5f+.025f+(i%5)/4f*Mathf.Max(0,Mathf.Clamp(settings.rubbleHeight,.5f,1.5f)-thickness-.025f);
                var cubeSize=new Vector3(width,thickness,depth);
                var viewSize=roof?cubeSize:side?new Vector3(thickness,width,depth):new Vector3(width,depth,thickness);
                var flat=roof?Quaternion.identity:side?Quaternion.Euler(0,0,90):Quaternion.Euler(90,0,0);
                Rubble[i]=new RubblePiece(new Vector3(x,0,z),cubeSize,rotation,viewSize,rotation*flat,elevation);
                Rubble[i].Settle(field);
            }
        }
        public Bounds RubbleBounds { get; private set; }
        public bool HasDryRubble { get; private set; }
        internal void SettleRubble(HeightField field)
        {
            HasDryRubble=false;var bounds=new Bounds();
            foreach(var piece in Rubble)
            {
                piece.Settle(field);if(!piece.Active)continue;
                if(!HasDryRubble)bounds=piece.WorldBounds;else bounds.Encapsulate(piece.WorldBounds);
                HasDryRubble=true;
            }
            RubbleBounds=bounds;
        }
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
