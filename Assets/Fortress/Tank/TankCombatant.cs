using UnityEngine;

namespace Fortress
{
    public sealed class TankCombatant
    {
        public TankRig Rig { get; }
        public float Health { get; private set; }
        public float MaxHealth { get; }
        public bool IsDefeated => Health <= 0 || Drowned;
        public bool Drowned { get; private set; }
        public float GroundHeight { get; private set; }
        public float Radius { get; }
        public float Height { get; }
        public float RootOffset { get; }
        public Vector3 Center => Rig.transform.position + Vector3.up*(Height*.5f-RootOffset);
        public TankCombatant(TankRig rig,GameTuning tuning)
        {
            Rig=rig;MaxHealth=tuning.maxHealth;Health=MaxHealth;
            Radius=tuning.tankCapsuleRadius;Height=Mathf.Max(tuning.tankCapsuleHeight,Radius*2);
            RootOffset=tuning.tankGroundOffset;
        }
        public void Spawn(Vector3 ground)
        {
            Health=MaxHealth;Drowned=false;GroundHeight=ground.y;
            Rig.transform.position=ground+Vector3.up*RootOffset;
        }
        /// <summary>Ground locomotion preserves health and defeat flags; this is not a respawn.</summary>
        public void MoveOnGround(Vector3 ground)
        {
            GroundHeight=ground.y;
            Rig.transform.position=ground+Vector3.up*RootOffset;
        }
        public void ApplyDamage(float amount) { Health=Mathf.Max(0,Health-Mathf.Max(0,amount)); }
        public void ApplyExplosion(Vector3 point,float damage,float radius)
        {
            if(radius>0) ApplyDamage(damage*Mathf.Clamp01(1-Vector3.Distance(Center,point)/radius));
        }
        public void Settle(HeightField field,float fallDamagePerMeter)
        {
            var p=Rig.transform.position;
            if(!field.TrySample(p.x,p.z,out float ground)) { Drowned=true;return; }
            float fall=Mathf.Max(0,GroundHeight-ground);
            GroundHeight=ground;p.y=ground+RootOffset;Rig.transform.position=p;
            ApplyDamage(fall*fallDamagePerMeter);
            if(ground<=field.WaterHeight) Drowned=true;
        }
    }
}
