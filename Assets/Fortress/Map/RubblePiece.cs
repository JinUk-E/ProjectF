using UnityEngine;
namespace Fortress
{
    /// <summary>Final structural slab shared by analytic cover and the settled fragment view.</summary>
    public sealed class RubblePiece
    {
        public Vector3 Position { get; private set; }
        public Vector3 Size { get; }
        public Quaternion Rotation { get; }
        public Vector3 ViewScale { get; }
        public Quaternion ViewRotation { get; }
        public bool Active { get; private set; }
        public Bounds WorldBounds
        {
            get { var x=Rotation*Vector3.right*(Size.x*.5f);var z=Rotation*Vector3.forward*(Size.z*.5f);return new Bounds(Position,new Vector3(2*(Mathf.Abs(x.x)+Mathf.Abs(z.x)),Size.y,2*(Mathf.Abs(x.z)+Mathf.Abs(z.z)))); }
        }
        readonly float elevation;
        internal RubblePiece(Vector3 position,Vector3 size,Quaternion rotation,Vector3 viewScale,Quaternion viewRotation,float offset)
        { Position=position;Size=size;Rotation=rotation;ViewScale=viewScale;ViewRotation=viewRotation;elevation=offset; }
        internal void Settle(HeightField field)
        {
            var p=Position;Active=field.TrySample(p.x,p.z,out float height) && height>field.WaterHeight;
            p.y=height+elevation;Position=p;
        }
        public bool TryHit(Vector3 from,Vector3 to,out Vector3 hit)
        {
            var inverse=Quaternion.Inverse(Rotation);
            if(Active && CoverBuilding.Intersect(new Bounds(Vector3.zero,Size),inverse*(from-Position),inverse*(to-Position),out var local))
            {hit=Position+Rotation*local;return true;}
            hit=default;return false;
        }
        public bool BlocksMovement(Vector3 from,Vector3 to,float radius)
        {
            if(!Active)return false;
            from.y=to.y=Position.y;var inverse=Quaternion.Inverse(Rotation);
            return CoverBuilding.Intersect(new Bounds(Vector3.zero,Size+new Vector3(radius*2,0,radius*2)),inverse*(from-Position),inverse*(to-Position),out _);
        }
    }
}

