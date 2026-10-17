using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityProjectile;
using Object=UnityEngine.Object;

namespace Fortress.Tests
{
    public sealed class MatchControllerTests
    {
        readonly List<Object> created=new();
        MatchController match;
        FlatShotController shots;
        GameTuning settings;
        HeightFieldTerrain map;
        T Asset<T>() where T:ScriptableObject { var a=ScriptableObject.CreateInstance<T>();created.Add(a);return a; }
        GameObject Go(string name) { var g=new GameObject(name);created.Add(g);return g; }
        Transform Child(Transform parent,string name,Vector3 position)
        { var g=new GameObject(name);g.transform.SetParent(parent,false);g.transform.localPosition=position;return g.transform; }
        [SetUp] public void Setup()
        {
            settings=Asset<GameTuning>();
            var data=new TerrainData { heightmapResolution=33,size=new Vector3(64,16,64) };created.Add(data);
            var heights=new float[33,33];for(int z=0;z<33;z++)for(int x=0;x<33;x++)heights[z,x]=.5f;
            data.SetHeights(0,0,heights);
            var ground=Go("Terrain");ground.transform.position=new Vector3(-32,-8,-32);
            var terrain=ground.AddComponent<Terrain>();terrain.terrainData=data;
            map=ground.AddComponent<HeightFieldTerrain>();map.Configure(terrain,settings);
            var flight=Asset<FlightProfile>();flight.segments=new List<FlightSegment>{new PhysicsArcSegment{gravity=settings.gravity}};
            var profile=Asset<ProjectileProfile>();profile.flight=flight;profile.fuse=Asset<ImpactFuse>();profile.payload=Asset<SingleImpactPayload>();profile.lingerTime=0;
            var prefab=Go("Shell").AddComponent<ProjectileRunner>();prefab.Profile=profile;prefab.gameObject.SetActive(false);
            var pool=Go("Pool").AddComponent<ProjectilePool>();
            var rigs=new TankRig[2];
            for(int i=0;i<2;i++)
            {
                var rig=Go("Tank").AddComponent<TankRig>();rig.turret=Child(rig.transform,"Turret",Vector3.up);
                rig.barrel=Child(rig.turret,"Barrel",Vector3.zero);rig.muzzle=Child(rig.barrel,"Muzzle",Vector3.forward*2);rigs[i]=rig;
            }
            shots=Go("Shots").AddComponent<FlatShotController>();shots.terrainMap=map;shots.Configure(settings,rigs,prefab,pool,null);
            match=Go("Match").AddComponent<MatchController>();match.autoStart=false;match.shots=shots;match.terrainMap=map;match.tuning=settings;
            match.spawnAreas=new[]{Asset<SpawnArea>(),Asset<SpawnArea>()};
            match.spawnAreas[0].min=match.spawnAreas[0].max=new Vector2(-8,0);
            match.spawnAreas[1].min=match.spawnAreas[1].max=new Vector2(8,0);
            match.InitializeMatch(1234);
        }
        [TearDown] public void Teardown()
        {
            if(shots!=null)shots.CancelShot();
            for(int i=created.Count-1;i>=0;i--)if(created[i]!=null)Object.DestroyImmediate(created[i]);created.Clear();
        }
        void Finish() { var runner=shots.ActiveRunner;Assert.That(runner,Is.Not.Null);runner.Tick(30);Assert.That(shots.IsFiring,Is.False); }
        [Test] public void Miss_CompletesAndAlternatesWithoutChangingWorld()
        {
            settings.maxFlightTime=.1f;shots.SetAim(90,85,35);int revision=map.Field.Revision;
            Assert.That(shots.TryFire(),Is.True);Assert.That(shots.LastSolveHit,Is.False);
            Assert.That(match.State,Is.EqualTo(MatchState.Firing));Assert.That(map.Field.IsFrozen,Is.True);
            Assert.That(shots.SetActiveTank(1),Is.False);Finish();
            Assert.That(map.Field.IsFrozen,Is.False);Assert.That(map.Field.Revision,Is.EqualTo(revision));
            Assert.That(match.ActivePlayerIndex,Is.EqualTo(1));Assert.That(shots.ActiveTank,Is.SameAs(match.Tanks[1].Rig));
            Assert.That(match.TurnNumber,Is.EqualTo(2));Assert.That(match.State,Is.EqualTo(MatchState.Aim));
        }
        [Test] public void Impact_HoldsHealthAndTerrainUntilFinished_ThenJudgesWinner()
        {
            shots.SetWind(Vector3.zero);shots.SetAim(90,0,35);match.Tanks[1].ApplyDamage(99);
            int revision=map.Field.Revision;var targetPosition=match.Tanks[1].Rig.transform.position;int impacts=0;
            shots.ShotImpacted+=r=>
            {
                impacts++;Assert.That(map.Field.IsFrozen,Is.True);Assert.That(map.Field.Revision,Is.EqualTo(revision));
                Assert.That(match.Tanks[1].Health,Is.EqualTo(1));Assert.That(match.Tanks[1].Rig.transform.position,Is.EqualTo(targetPosition));
            };
            Assert.That(shots.TryFire(),Is.True);Finish();
            Assert.That(impacts,Is.EqualTo(1));Assert.That(match.Tanks[1].IsDefeated,Is.True);
            Assert.That(match.State,Is.EqualTo(MatchState.Ended));Assert.That(match.WinnerIndex,Is.EqualTo(0));
            Assert.That(shots.TryFire(),Is.False);Assert.That(shots.SetAim(90,45,22),Is.False);
            Assert.That(map.Field.Revision,Is.GreaterThan(revision));
        }
        [Test] public void Restart_RestoresOriginalHeightsHealthSeedAndWind()
        {
            var startWind=shots.Wind;var first=match.Tanks[0].Rig.transform.position;
            map.Field.ApplyCrater(Vector3.zero,4,2);match.Tanks[0].ApplyDamage(40);
            Assert.That(shots.TryFire(),Is.True);match.RestartMatch(1234);
            Assert.That(shots.pool.ActiveCount,Is.Zero);Assert.That(map.Field.IsFrozen,Is.False);
            Assert.That(map.Field.TrySample(0,0,out float h),Is.True);Assert.That(h,Is.Zero.Within(.001));
            Assert.That(match.Tanks[0].Health,Is.EqualTo(100));Assert.That(shots.Wind,Is.EqualTo(startWind));
            Assert.That(match.Tanks[0].Rig.transform.position,Is.EqualTo(first));Assert.That(match.TurnNumber,Is.EqualTo(1));
        }
        [Test] public void SeededSpawnAndWind_AreReproducibleAndUseSeparateStreams()
        {
            match.spawnAreas[0].min=new Vector2(-14,-4);match.spawnAreas[0].max=new Vector2(-8,4);
            match.RestartMatch(998);var point=match.Tanks[0].Rig.transform.position;var wind=shots.Wind;
            Assert.That(point.x,Is.InRange(-14,-8));Assert.That(point.z,Is.InRange(-4,4));Assert.That(wind.magnitude,Is.LessThanOrEqualTo(settings.maxWind));
            match.RestartMatch(998);Assert.That(match.Tanks[0].Rig.transform.position,Is.EqualTo(point));Assert.That(shots.Wind,Is.EqualTo(wind));
            match.spawnAreas[0].min=match.spawnAreas[0].max=new Vector2(-8,0);match.RestartMatch(998);Assert.That(shots.Wind,Is.EqualTo(wind));
            match.RestartMatch();Assert.That(match.Seed,Is.EqualTo(999));
        }
        [Test] public void TankDamage_LinearFalloffAndFallDamage()
        {
            var target=match.Tanks[0];target.ApplyExplosion(target.Center+Vector3.right*2,30,4);
            Assert.That(target.Health,Is.EqualTo(85).Within(.001));
            var position=target.Rig.transform.position;map.Field.ApplyCrater(position,4,2);target.Settle(map.Field,5);
            Assert.That(target.Health,Is.EqualTo(75).Within(.001));Assert.That(target.GroundHeight,Is.EqualTo(-2).Within(.001));
        }
        [Test] public void CapsuleIntersection_UsesEarliestEntryAndExcludesShooter()
        {
            var target=match.Tanks[0];Vector3 center=target.Center;
            Assert.That(CombatShotSurface.TryCapsule(center+Vector3.left*3,center+Vector3.right*3,target,out var point),Is.True);
            Assert.That(point.x,Is.EqualTo(center.x-1).Within(.001));
            var surface=new CombatShotSurface(map.Field,match.Tanks,0);
            Assert.That(surface.TryHit(center+Vector3.left*3,center+Vector3.right*3,out _),Is.False);
            var include=new CombatShotSurface(map.Field,match.Tanks,-1);
            Assert.That(include.TryHit(center+Vector3.left*3,center+Vector3.right*3,out point),Is.True);
            Assert.That(point.x,Is.EqualTo(center.x-1).Within(.001));
        }
        [Test] public void InvalidRestart_IsRejectedBeforeMutatingActiveMatch()
        {
            match.Tanks[0].ApplyDamage(10);int revision=map.Field.Revision;
            var oldTanks=match.Tanks;int seed=match.Seed;
            Assert.That(shots.TryFire(),Is.True);var runner=shots.ActiveRunner;
            match.spawnAreas[1].min=match.spawnAreas[1].max=new Vector2(999,999);
            Assert.Throws<System.InvalidOperationException>(()=>match.RestartMatch(999));
            Assert.That(match.Tanks,Is.SameAs(oldTanks));Assert.That(match.Tanks[0].Health,Is.EqualTo(90));
            Assert.That(match.Seed,Is.EqualTo(seed));Assert.That(map.Field.Revision,Is.EqualTo(revision));
            Assert.That(shots.ActiveRunner,Is.SameAs(runner));Assert.That(map.Field.IsFrozen,Is.True);
            Assert.That(match.State,Is.EqualTo(MatchState.Firing));
        }
        [Test] public void InputDisabled_ImmediatelyHidesPreview()
        {
            var preview=Go("Preview").AddComponent<DashedAimPreview>();preview.Bind(shots,null);
            shots.InputEnabled=false;
            foreach(var line in preview.GetComponentsInChildren<LineRenderer>()) Assert.That(line.enabled,Is.False);
        }
        [TestCase(3.999f,false)]
        [TestCase(4f,true)]
        [TestCase(4.001f,true)]
        public void WaterBoundary_SettlesOnlyAfterShotAndDefeatsWithHealthRemaining(float depth,bool drowned)
        {
            Assert.That(map.Field.WaterHeight,Is.EqualTo(-4));
            var target=match.Tanks[1];map.Field.ApplyCrater(target.Rig.transform.position,4,depth);
            settings.maxFlightTime=.1f;shots.SetAim(90,85,35);
            Assert.That(shots.TryFire(),Is.True);Assert.That(shots.LastSolveHit,Is.False);
            Assert.That(target.Drowned,Is.False);Assert.That(target.GroundHeight,Is.Zero);Assert.That(target.Health,Is.EqualTo(100));
            Finish();
            Assert.That(target.GroundHeight,Is.EqualTo(-depth).Within(.00001));
            Assert.That(target.Health,Is.GreaterThan(0));Assert.That(target.Drowned,Is.EqualTo(drowned));
            Assert.That(target.IsDefeated,Is.EqualTo(drowned));
            Assert.That(match.State,Is.EqualTo(drowned?MatchState.Ended:MatchState.Aim));
            if(drowned) Assert.That(match.WinnerIndex,Is.EqualTo(0));
        }
        [Test] public void Restart_AfterDrowning_RestoresDryTerrainAndClearsDefeat()
        {
            map.Field.ApplyCrater(match.Tanks[1].Rig.transform.position,4,4);
            settings.maxFlightTime=.1f;shots.SetAim(90,85,35);Assert.That(shots.TryFire(),Is.True);Finish();
            Assert.That(match.Tanks[1].Drowned,Is.True);Assert.That(match.Tanks[1].Health,Is.GreaterThan(0));
            match.RestartMatch(1234);
            Assert.That(match.Tanks[1].Drowned,Is.False);Assert.That(match.Tanks[1].IsDefeated,Is.False);
            Assert.That(match.Tanks[1].Health,Is.EqualTo(100));Assert.That(match.Tanks[1].GroundHeight,Is.Zero.Within(.001));
            Assert.That(map.Field.TrySample(8,0,out var ground),Is.True);Assert.That(ground,Is.Zero.Within(.001));
            Assert.That(match.State,Is.EqualTo(MatchState.Aim));Assert.That(match.WinnerIndex,Is.EqualTo(-1));Assert.That(shots.InputEnabled,Is.True);
        }
        [Test] public void Restart_AfterEnded_PublishesOnlyCoherentNewMatchState()
        {
            map.Field.ApplyCrater(match.Tanks[1].Rig.transform.position,4,4);
            settings.maxFlightTime=.1f;shots.SetAim(90,85,35);Assert.That(shots.TryFire(),Is.True);Finish();
            Assert.That(match.State,Is.EqualTo(MatchState.Ended));
            int observed=0;
            System.Action observe=()=>
            {
                observed++;
                Assert.That(match.State,Is.EqualTo(MatchState.Aim),"new-match data must never be published as an old ended result");
                Assert.That(match.WinnerIndex,Is.EqualTo(-1));Assert.That(match.IsDraw,Is.False);
                Assert.That(match.TurnNumber,Is.EqualTo(1));Assert.That(match.Seed,Is.EqualTo(5678));
                foreach(var tank in match.Tanks) { Assert.That(tank.Health,Is.EqualTo(100));Assert.That(tank.IsDefeated,Is.False); }
            };
            shots.StateChanged+=observe;
            try { match.RestartMatch(5678); }
            finally { shots.StateChanged-=observe; }
            Assert.That(observed,Is.GreaterThan(0));
        }
        [Test] public void CancelShot_DoesNotAdvanceTurnOrApplyDamage()
        {
            Assert.That(shots.TryFire(),Is.True);shots.CancelShot();
            Assert.That(match.State,Is.EqualTo(MatchState.Aim));Assert.That(match.TurnNumber,Is.EqualTo(1));
            Assert.That(match.Tanks[0].Health,Is.EqualTo(100));Assert.That(map.Field.IsFrozen,Is.False);
        }
    }
}
