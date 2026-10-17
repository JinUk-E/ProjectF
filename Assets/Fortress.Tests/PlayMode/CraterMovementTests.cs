using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityProjectile;
using Object=UnityEngine.Object;

namespace Fortress.Tests
{
    public sealed class CraterMovementTests
    {
        readonly List<Object> created=new();
        GameTuning tuning;
        HeightFieldTerrain map;
        FlatShotController shots;
        MatchController match;
        T Asset<T>() where T:ScriptableObject { var a=ScriptableObject.CreateInstance<T>();created.Add(a);return a; }
        GameObject Go(string name) { var g=new GameObject(name);created.Add(g);return g; }
        Transform Child(Transform parent,string name,Vector3 position)
        { var g=new GameObject(name);g.transform.SetParent(parent,false);g.transform.localPosition=position;return g.transform; }
        [UnitySetUp] public IEnumerator Setup()
        {
            tuning=Asset<GameTuning>();tuning.movementEnabled=true;tuning.maxWind=0;
            var data=new TerrainData { heightmapResolution=257,size=new Vector3(64,16,64) };created.Add(data);
            var heights=new float[257,257];for(int z=0;z<257;z++)for(int x=0;x<257;x++)heights[z,x]=.5f;
            data.SetHeights(0,0,heights);
            var ground=Go("Movement terrain");ground.transform.position=new Vector3(-32,-8,-32);
            var terrain=ground.AddComponent<Terrain>();terrain.terrainData=data;
            map=ground.AddComponent<HeightFieldTerrain>();map.Configure(terrain,tuning);
            var flight=Asset<FlightProfile>();flight.segments=new List<FlightSegment>{new PhysicsArcSegment{gravity=tuning.gravity}};
            var profile=Asset<ProjectileProfile>();profile.flight=flight;profile.fuse=Asset<ImpactFuse>();profile.payload=Asset<SingleImpactPayload>();profile.lingerTime=0;
            var prefab=Go("Movement shell").AddComponent<ProjectileRunner>();prefab.Profile=profile;prefab.gameObject.SetActive(false);
            var pool=Go("Movement pool").AddComponent<ProjectilePool>();
            var rigs=new TankRig[2];
            for(int i=0;i<2;i++)
            {
                var rig=Go("Movement tank").AddComponent<TankRig>();rig.turret=Child(rig.transform,"Turret",Vector3.up);
                rig.barrel=Child(rig.turret,"Barrel",Vector3.zero);rig.muzzle=Child(rig.barrel,"Muzzle",Vector3.forward*2);rigs[i]=rig;
            }
            shots=Go("Movement shots").AddComponent<FlatShotController>();shots.terrainMap=map;shots.Configure(tuning,rigs,prefab,pool,null);
            match=Go("Movement match").AddComponent<MatchController>();match.autoStart=false;match.shots=shots;match.terrainMap=map;match.tuning=tuning;
            match.spawnAreas=new[]{Asset<SpawnArea>(),Asset<SpawnArea>()};
            match.spawnAreas[0].min=match.spawnAreas[0].max=new Vector2(-8,0);
            match.spawnAreas[1].min=match.spawnAreas[1].max=new Vector2(8,0);
            match.InitializeMatch(1234);
            yield return null;
        }
        [UnityTearDown] public IEnumerator Teardown()
        {
            if(shots!=null) shots.CancelShot();
            for(int i=created.Count-1;i>=0;i--) if(created[i]!=null) Object.Destroy(created[i]);
            created.Clear();yield return null;
        }
        void Install(Func<float,float,float> height)
        {
            var values=new float[257,257];
            for(int z=0;z<257;z++)for(int x=0;x<257;x++)values[z,x]=height(-32+x*.25f,-32+z*.25f);
            map.InstallTerrain(new HeightField(values,new Vector3(-32,-8,-32),new Vector2(64,64),-8,-4));
            shots.SetSurface(new CombatShotSurface(map.Field,match.Tanks,match.ActivePlayerIndex));
        }
        [UnityTest] public IEnumerator NearbyShell_DamagedFootprint_CanRetreatWithoutChangingHealthOrAim()
        {
            // A deeper shell reproduces an overlapping-crater edge without editing the field directly.
            tuning.craterDepth=4;
            Vector3 impact=new Vector3(8,0,4.3f);
            Vector3 delta=impact-match.Tanks[0].Rig.transform.position;
            float pitch=35*Mathf.Deg2Rad;
            float range=new Vector2(delta.x,delta.z).magnitude-2*Mathf.Cos(pitch);
            float height=1+2*Mathf.Sin(pitch);
            float power=Mathf.Sqrt(tuning.gravity*range*range/(2*Mathf.Cos(pitch)*Mathf.Cos(pitch)*(height+range*Mathf.Tan(pitch))));
            shots.SetAim(Mathf.Atan2(delta.x,delta.z)*Mathf.Rad2Deg,35,power);
            Assert.That(Vector3.Distance(shots.LastPrediction.ImpactPosition,impact),Is.LessThan(.05f));
            Assert.That(shots.TryFire(),Is.True);shots.ActiveRunner.Tick(30);
            Assert.That(match.ActivePlayerIndex,Is.EqualTo(1));Assert.That(match.State,Is.EqualTo(MatchState.Aim));
            var tank=match.Tanks[1];var start=tank.Rig.transform.position;
            Assert.That(map.Field.TrySample(start.x,start.z+.75f,out float damagedSupport),Is.True);
            Assert.That(tank.GroundHeight-damagedSupport,Is.GreaterThan(tuning.maxMoveStepHeight),"Even the first retreat candidate retains damaged support: the old all-or-nothing footprint check rejects it.");
            float hp=tank.Health,yaw=tank.Rig.Yaw,pitchBefore=tank.Rig.Pitch,budget=match.MovementRemaining;
            int revision=map.Field.Revision;
            Assert.That(match.TryMoveActiveTank(Vector2.down,.25f),Is.True);
            Assert.That(tank.Rig.transform.position.z,Is.EqualTo(start.z-1).Within(.001));
            Assert.That(match.MovementRemaining,Is.EqualTo(budget-1).Within(.001));
            Assert.That(tank.Health,Is.EqualTo(hp));Assert.That(tank.Rig.Yaw,Is.EqualTo(yaw));Assert.That(tank.Rig.Pitch,Is.EqualTo(pitchBefore));
            Assert.That(map.Field.Revision,Is.EqualTo(revision));
            yield return null;
        }
        [UnityTest] public IEnumerator SmallDryLedge_CanCrossBothWays()
        {
            Install((x,z)=>x>=-7.5f?.5f:0);
            var tank=match.Tanks[0];float hp=tank.Health;
            Assert.That(match.TryMoveActiveTank(Vector2.right,.5f),Is.True);
            Assert.That(tank.Rig.transform.position.x,Is.EqualTo(-6).Within(.001));
            Assert.That(tank.GroundHeight,Is.EqualTo(.5f).Within(.001));
            Assert.That(match.TryMoveActiveTank(Vector2.left,.5f),Is.True);
            Assert.That(tank.GroundHeight,Is.Zero.Within(.001));Assert.That(tank.Health,Is.EqualTo(hp));
            Assert.That(match.MovementRemaining,Is.EqualTo(8).Within(.001));yield return null;
        }
        [UnityTest] public IEnumerator SteepRamp_CannotReuseStepAllowanceToClimbOrDescend()
        {
            Install((x,z)=>Mathf.Clamp(x+8,0,8));
            float budget=match.MovementRemaining;
            Assert.That(match.TryMoveActiveTank(Vector2.right,1),Is.False);
            match.Tanks[0].MoveOnGround(new Vector3(-4,4,0));
            Assert.That(match.TryMoveActiveTank(Vector2.left,1),Is.False);
            Assert.That(match.MovementRemaining,Is.EqualTo(budget));yield return null;
        }
        [UnityTest] public IEnumerator Movement_RejectsNewWetOrOutOfBoundsFootprint()
        {
            Install((x,z)=>x>=-6?-5:0);
            match.TryMoveActiveTank(Vector2.right,1);
            var p=match.Tanks[0].Rig.transform.position;
            Assert.That(map.Field.TrySample(p.x+match.Tanks[0].Radius,p.z,out float support),Is.True);
            Assert.That(support,Is.GreaterThan(map.Field.WaterHeight));
            Install((x,z)=>0);match.Tanks[0].MoveOnGround(new Vector3(-30,0,0));
            Assert.That(match.TryMoveActiveTank(Vector2.left,1),Is.True);
            Assert.That(match.Tanks[0].Rig.transform.position.x,Is.GreaterThanOrEqualTo(-31));
            Assert.That(match.TryMoveActiveTank(Vector2.left,1),Is.False);yield return null;
        }
    }
}
