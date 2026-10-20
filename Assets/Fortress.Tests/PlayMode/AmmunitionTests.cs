#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityProjectile;
using Object=UnityEngine.Object;
namespace Fortress.Tests
{
    public sealed class AmmunitionTests
    {
        MatchController match;FlatShotController shots;
        [UnitySetUp] public IEnumerator Setup()
        {
            BattleLoadoutSelection.Player=3;BattleLoadoutSelection.Opponent=2;
            EditorSceneManager.LoadSceneInPlayMode(SessionMenu.LocalScene,new LoadSceneParameters(LoadSceneMode.Single));yield return null;
            match=Object.FindFirstObjectByType<MatchController>();shots=match.shots;
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            if(shots!=null) shots.CancelShot();Time.timeScale=1;AudioListener.pause=false;
            BattleLoadoutSelection.Player=BattleLoadoutSelection.Opponent=0;
            var old=SceneManager.GetActiveScene();SceneManager.SetActiveScene(SceneManager.CreateScene("AmmoCleanup"));yield return SceneManager.UnloadSceneAsync(old);
        }
        void Aim()
        {
            shots.SetWind(Vector3.zero);shots.SetAim(0,65,22);
        }
        [UnityTest] public IEnumerator Loadouts_SelectAndConsumeOnlySuccessfulLaunch_RestartRefills()
        {
            Assert.That(shots.ActiveLoadout.displayName,Is.EqualTo("SUPPORT"));Assert.That(shots.SpecialRemaining,Is.EqualTo(2));
            Assert.That(shots.SelectAmmo(true),Is.True);
            shots.GameplaySuspended=true;Assert.That(shots.TryFire(),Is.False);Assert.That(shots.SelectAmmo(false),Is.False);Assert.That(shots.SpecialRemaining,Is.EqualTo(2));
            shots.GameplaySuspended=false;yield return null;
            Aim();Assert.That(shots.TryFire(),Is.True);Assert.That(shots.SpecialRemaining,Is.EqualTo(1));
            Assert.That(shots.TryFire(),Is.False);Assert.That(shots.SelectAmmo(false),Is.False);Assert.That(shots.SpecialRemaining,Is.EqualTo(1));
            shots.CancelShot();match.RestartMatch(match.Seed);
            Assert.That(shots.SpecialRemaining,Is.EqualTo(2));Assert.That(shots.SpecialSelected,Is.False);
            Assert.That(shots.SelectAmmo(false),Is.True);Aim();Assert.That(shots.TryFire(),Is.True);Assert.That(shots.SpecialRemaining,Is.EqualTo(2));shots.CancelShot();
        }
        [UnityTest] public IEnumerator Cluster_SixPhysicalChildren_HoldWorldUntilLastFinish_ResolveOrdered()
        {
            Assert.That(shots.SelectAmmo(true),Is.True);Aim();
            var children=new List<ProjectileRunner>();var records=new List<(Vector3 position,float time,int index)>();var applied=new List<Vector3>();
            shots.ImpactRecorded+=(p,t,i)=>records.Add((p,t,i));match.TerrainImpactApplied+=(p,r)=>applied.Add(p);
            Assert.That(shots.TryFire(),Is.True);var root=shots.ActiveRunner;root.SubmunitionLaunched+=(p,c)=>children.Add(c);
            root.Tick(root.Prediction.PayloadTime+.01f);
            Assert.That(children.Count,Is.EqualTo(6));Assert.That(shots.ActiveFamilyCount,Is.EqualTo(6));
            Assert.That(shots.IsFiring,Is.True);Assert.That(match.terrainMap.Field.IsFrozen,Is.True);Assert.That(match.TurnNumber,Is.EqualTo(1));
            children[0].Tick(20);
            Assert.That(match.terrainMap.Field.IsFrozen,Is.True);Assert.That(applied.Count,Is.Zero);Assert.That(shots.SelectAmmo(false),Is.False);
            for(int i=children.Count-1;i>=1;i--) children[i].Tick(20);
            Assert.That(shots.IsFiring,Is.False);Assert.That(match.terrainMap.Field.IsFrozen,Is.False);Assert.That(match.TurnNumber,Is.EqualTo(2));
            Assert.That(records.Count,Is.EqualTo(6));Assert.That(applied.Count,Is.EqualTo(6));
            records.Sort((a,b)=>a.time!=b.time?a.time.CompareTo(b.time):a.index.CompareTo(b.index));
            for(int i=0;i<records.Count;i++) Assert.That(applied[i],Is.EqualTo(records[i].position));
            yield return null;
        }
        [UnityTest] public IEnumerator Cluster_CancelAfterSplit_DiscardsAllDamageAndUnlocks()
        {
            shots.SelectAmmo(true);Aim();Assert.That(shots.TryFire(),Is.True);
            var root=shots.ActiveRunner;var field=match.terrainMap.Field;int applied=0;match.TerrainImpactApplied+=(p,r)=>applied++;
            root.Tick(root.Prediction.PayloadTime+.01f);Assert.That(shots.ActiveFamilyCount,Is.EqualTo(6));
            shots.CancelShot();Assert.That(shots.ActiveFamilyCount,Is.Zero);Assert.That(shots.IsFiring,Is.False);Assert.That(field.IsFrozen,Is.False);
            Assert.That(match.State,Is.EqualTo(MatchState.Aim));Assert.That(match.TurnNumber,Is.EqualTo(1));Assert.That(applied,Is.Zero);
            yield return null;Assert.That(applied,Is.Zero);
        }
        [UnityTest] public IEnumerator Launch_SnapshotsDamageAndKeepsTankAmmunitionIndependent()
        {
            var original=shots.ActiveLoadout.special;var copy=Object.Instantiate(original);
            var role=Object.Instantiate(shots.ActiveLoadout);role.special=copy;
            var roles=(TankLoadout[])shots.availableLoadouts.Clone();roles[3]=role;shots.availableLoadouts=roles;shots.ResetLoadouts();
            try
            {
                shots.SelectAmmo(true);Aim();Assert.That(shots.TryFire(),Is.True);float value=shots.CurrentShotDamage.damage;
                copy.damage=999;copy.depth=99;Assert.That(shots.CurrentShotDamage.damage,Is.EqualTo(value));Assert.That(shots.CurrentShotDamage.depth,Is.EqualTo(.18f).Within(.001));
                shots.CancelShot();Assert.That(shots.SetActiveTank(1),Is.True);Assert.That(shots.ActiveLoadout.displayName,Is.EqualTo("EXCAVATOR"));Assert.That(shots.SpecialRemaining,Is.EqualTo(3));
                Assert.That(shots.SetActiveTank(0),Is.True);Assert.That(shots.SpecialRemaining,Is.EqualTo(1));
            }
            finally { Object.Destroy(copy);Object.Destroy(role); }
            yield return null;
        }
        [UnityTest] public IEnumerator AiTurn_BlocksHumanAmmoSelection()
        {
            var ai=match.gameObject.AddComponent<AiOpponent>();ai.match=match;shots.ai=ai;
            // Component ownership is determined by the active player, independent of its thinking timer.
            Aim();Assert.That(shots.TryFire(),Is.True);shots.ActiveRunner.Tick(30);
            Assert.That(match.ActivePlayerIndex,Is.EqualTo(1));Assert.That(ai.OwnsCurrentTurn,Is.True);
            Assert.That(shots.SelectAmmo(true),Is.False);Assert.That(shots.SelectAmmo(true,true),Is.True);
            yield return null;
        }
        sealed class TestSurface : IHeightFieldSurface
        {
            public HeightField Field { get; }
            readonly bool empty;readonly float wall,ground;
            public TestSurface(HeightField field,bool empty,float wall,float ground) { Field=field;this.empty=empty;this.wall=wall;this.ground=ground; }
            public bool TryHit(Vector3 from,Vector3 to,out Vector3 hit)
            {
                hit=default;if(empty) return false;
                if(from.x<wall && to.x>=wall) { hit=Vector3.Lerp(from,to,(wall-from.x)/(to.x-from.x));return true; }
                return new PlaneSurface(ground).TryHit(from,to,out hit);
            }
        }
        [UnityTest] public IEnumerator Cluster_CollisionFallback_OnlySixChildrenDealDamage()
        {
            shots.SelectAmmo(true);shots.SetWind(Vector3.zero);shots.SetAim(90,35,22);
            var muzzle=shots.ActiveTank.Muzzle.position;
            shots.SetSurface(new TestSurface(match.terrainMap.Field,false,muzzle.x+2,match.Tanks[0].GroundHeight));
            int children=0,carrierImpacts=0,childImpacts=0;
            shots.ImpactRecorded+=(p,t,i)=>{ if(i==0) carrierImpacts++;else childImpacts++; };
            Assert.That(shots.TryFire(),Is.True);var root=shots.ActiveRunner;
            Assert.That(root.Prediction.PayloadTime,Is.EqualTo(root.Prediction.ArrivalTime).Within(.001));
            root.SubmunitionLaunched+=(p,c)=>children++;root.Tick(30);
            Assert.That(children,Is.EqualTo(6));Assert.That(carrierImpacts,Is.Zero);Assert.That(childImpacts,Is.EqualTo(6));
            Assert.That(shots.IsFiring,Is.False);Assert.That(match.terrainMap.Field.IsFrozen,Is.False);
            yield return null;
        }
        [UnityTest] public IEnumerator Cluster_NoSurfaceHit_TimeoutDoesNotDamageTerrain()
        {
            shots.SelectAmmo(true);Aim();shots.SetSurface(new TestSurface(match.terrainMap.Field,true,0,0));
            int applied=0,impacts=0;match.TerrainImpactApplied+=(p,r)=>applied++;shots.ImpactRecorded+=(p,t,i)=>impacts++;
            Assert.That(shots.TryFire(),Is.True);shots.ActiveRunner.Tick(100);
            Assert.That(shots.IsFiring,Is.False);Assert.That(match.terrainMap.Field.IsFrozen,Is.False);Assert.That(impacts,Is.Zero);Assert.That(applied,Is.Zero);
            yield return null;
        }
        [UnityTest] public IEnumerator Special_DepletionFallsBackToUnlimitedHE()
        {
            for(int i=0;i<2;i++) { Assert.That(shots.SelectAmmo(true),Is.True);Aim();Assert.That(shots.TryFire(),Is.True);shots.CancelShot(); }
            Assert.That(shots.SpecialRemaining,Is.Zero);Assert.That(shots.SpecialSelected,Is.False);Assert.That(shots.SelectAmmo(true),Is.False);
            Assert.That(shots.CurrentShotDamage.damage,Is.EqualTo(9));
            Aim();Assert.That(shots.TryFire(),Is.True);Assert.That(shots.CurrentShotDamage.damage,Is.EqualTo(30));Assert.That(shots.SpecialRemaining,Is.Zero);
            shots.CancelShot();yield return null;
        }
        [UnityTest] public IEnumerator Menu_ChosenRolesReachBothMatchSeats()
        {
            BattleLoadoutSelection.Player=BattleLoadoutSelection.Opponent=0;
            EditorSceneManager.LoadSceneInPlayMode(SessionMenu.MainScene,new LoadSceneParameters(LoadSceneMode.Single));yield return null;
            var picker=Object.FindFirstObjectByType<LoadoutMenu>();picker.NextPlayer();picker.NextOpponent();picker.NextOpponent();picker.NextOpponent();
            Assert.That(picker.playerLabel.text,Does.Contain("SIEGE"));Assert.That(picker.opponentLabel.text,Does.Contain("SUPPORT"));
            Object.FindFirstObjectByType<SessionMenu>().StartLocal();
            for(int i=0;i<120 && SceneManager.GetActiveScene().path!=SessionMenu.LocalScene;i++) yield return null;
            yield return null;match=Object.FindFirstObjectByType<MatchController>();shots=match.shots;
            Assert.That(shots.ActiveLoadout.displayName,Is.EqualTo("SIEGE"));Assert.That(shots.SpecialRemaining,Is.EqualTo(3));
            shots.SetActiveTank(1);Assert.That(shots.ActiveLoadout.displayName,Is.EqualTo("SUPPORT"));Assert.That(shots.SpecialRemaining,Is.EqualTo(2));
        }
        [UnityTest] public IEnumerator Ammo_ActualShotsApplyDistinctHealthDamageAndExcavation()
        {
            var oldTuning=match.tuning;var tuning=Object.Instantiate(oldTuning);tuning.fallDamagePerMeter=0;match.tuning=shots.tuning=tuning;
            match.cover=null;
            var damage=new float[3];var excavation=new float[3];
            try
            {
                for(int role=0;role<3;role++)
                {
                    BattleLoadoutSelection.Player=role;match.RestartMatch(1);
                    // Flat, isolated arena keeps hit location and fall damage identical across ammunition.
                    var values=new float[257,257];for(int z=0;z<257;z++)for(int x=0;x<257;x++)values[z,x]=4;
                    var field=new HeightField(values,new Vector3(-80,-8,-80),new Vector2(160,160),-8,-4);match.terrainMap.InstallTerrain(field);
                    match.Tanks[0].MoveOnGround(new Vector3(-10,4,0));match.Tanks[1].MoveOnGround(new Vector3(0,4,0));
                    shots.SetSurface(new CombatShotSurface(field,match.Tanks,0));shots.SetWind(Vector3.zero);if(role>0) shots.SelectAmmo(true);
                    shots.SetAim(90,0,35);
                    Vector3 impact=shots.LastPrediction.ImpactPosition;float health=match.Tanks[1].Health;
                    Assert.That(Vector3.Distance(impact,match.Tanks[1].Center),Is.LessThan(2));
                    Assert.That(shots.TryFire(),Is.True);shots.ActiveRunner.Tick(30);
                    damage[role]=health-match.Tanks[1].Health;field.TrySample(impact.x,impact.z,out float height);excavation[role]=4-height;
                }
                // Roles are main, siege, excavator. HE hurts armor most; drill moves most earth.
                Assert.That(damage[0],Is.GreaterThan(damage[1]));Assert.That(damage[1],Is.GreaterThan(damage[2]));
                Assert.That(excavation[2],Is.GreaterThan(excavation[0]*2));Assert.That(excavation[0],Is.GreaterThan(excavation[1]));
            }
            finally { match.tuning=shots.tuning=oldTuning;Object.Destroy(tuning); }
            yield return null;
        }
    }
}
#endif
