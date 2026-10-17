#if UNITY_EDITOR
using System.Collections;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;
namespace Fortress.Tests
{
    public sealed class TacticalCoverTests
    {
        MatchController match;
        [UnitySetUp] public IEnumerator Load()
        {
            EditorSceneManager.LoadSceneInPlayMode("Assets/Fortress/Scenes/MilitaryMatch.unity",new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;match=Object.FindAnyObjectByType<MatchController>();
        }
        [UnityTearDown] public IEnumerator Unload()
        { if(match!=null) match.shots.CancelShot();var scene=SceneManager.GetActiveScene();SceneManager.SetActiveScene(SceneManager.CreateScene("CoverCleanup"));yield return SceneManager.UnloadSceneAsync(scene); }
        [UnityTest] public IEnumerator Building_BlocksShotMovementAndBlast_HighShotPasses()
        {
            var cover=match.cover;Assert.That(cover.Remaining,Is.EqualTo(6));var b=cover.Buildings[0].Bounds;
            var from=b.center-Vector3.right*8;var to=b.center+Vector3.right*8;
            Assert.That(cover.TryHit(from,to,out var hit),Is.True);Assert.That(hit.x,Is.EqualTo(b.min.x).Within(.001f));
            Assert.That(cover.Shields(hit,to),Is.True);
            Assert.That(cover.Shields(hit,from),Is.False,"Explosion on near face must not shield the near side.");
            Assert.That(cover.BlocksMovement(from,to,1),Is.True);
            var high=Vector3.up*(b.extents.y+1);
            Assert.That(cover.TryHit(from+high,to+high,out _),Is.False);
            match.Tanks[0].MoveOnGround(new Vector3(b.min.x-1.1f,b.min.y,b.center.z));
            float budget=match.MovementRemaining;
            Assert.That(match.TryMoveActiveTank(Vector2.right,.25f),Is.False);Assert.That(match.MovementRemaining,Is.EqualTo(budget));
            yield return null;
        }
        [UnityTest] public IEnumerator Building_DestructionOpensPathAndRestartRestoresSameLayout()
        {
            var cover=match.cover;var b=cover.Buildings[0];var bounds=b.Bounds;
            cover.ApplyExplosion(bounds.center,4,30);Assert.That(b.Destroyed,Is.False);
            cover.ApplyExplosion(bounds.center,4,30);Assert.That(b.Destroyed,Is.True);Assert.That(cover.Remaining,Is.EqualTo(5));
            var from=bounds.center-Vector3.right*4;var to=bounds.center+Vector3.right*4;
            Assert.That(cover.BlocksMovement(from,to,1),Is.False);Assert.That(cover.TryHit(from,to,out _),Is.False);
            match.Tanks[0].MoveOnGround(new Vector3(bounds.min.x-1.1f,bounds.min.y,bounds.center.z));
            Assert.That(match.TryMoveActiveTank(Vector2.right,.25f),Is.True);
            match.RestartMatch(match.Seed);Assert.That(cover.Remaining,Is.EqualTo(6));Assert.That(cover.Buildings[0].Bounds,Is.EqualTo(bounds));
            Assert.That(cover.BlocksMovement(from,to,1),Is.True);
            yield return null;
        }
        [UnityTest] public IEnumerator Shot_RealProjectileHitsBuilding_OnlyDamagesAfterFinish()
        {
            var cover=match.cover;var b=cover.Buildings[0];var bounds=b.Bounds;
            float x=bounds.min.x-6,z=bounds.center.z;match.terrainMap.Field.TrySample(x,z,out float ground);
            match.Tanks[0].MoveOnGround(new Vector3(x,ground,z));
            match.shots.SetWind(Vector3.zero);match.shots.SetAim(90,8,22);
            Assert.That(match.shots.TryFire(),Is.True);float health=b.Health;
            Assert.Throws<System.InvalidOperationException>(()=>cover.ApplyExplosion(bounds.center,4,100));
            Assert.That(b.Health,Is.EqualTo(health));
            match.shots.ActiveRunner.Tick(match.tuning.maxFlightTime+1);
            Assert.That(b.Health,Is.LessThan(health));
            TestContext.WriteLine("Actual wall impact reduced building HP from "+health+" to "+b.Health);
            yield return null;
        }
        [UnityTest] public IEnumerator Road_CraterChangesGround_AlternateRoadRemainsAndRestartRestores()
        {
            var map=match.terrainMap.Field;map.TrySample(0,0,out float before);
            map.ApplyCrater(Vector3.zero,4,2);map.TrySample(0,0,out float after);Assert.That(after,Is.LessThan(before-1));
            map.TrySample(0,22,out float bypass);Assert.That(bypass,Is.EqualTo(0).Within(.08f));
            match.RestartMatch(match.Seed);match.terrainMap.Field.TrySample(0,0,out float reset);Assert.That(reset,Is.EqualTo(before));
            yield return null;
        }
        [UnityTest] public IEnumerator Shot_BuildingShieldsTankFromExplosionBeforeItCollapses()
        {
            var original=match.tuning;var testTuning=Object.Instantiate(original);
            match.tuning=match.shots.tuning=testTuning;testTuning.craterRadius=12;testTuning.craterDepth=.1f;testTuning.fallDamagePerMeter=0;
            try
            {
                var b=match.cover.Buildings[0].Bounds;float z=b.center.z;
                match.terrainMap.Field.TrySample(b.min.x-6,z,out float shooterGround);
                match.terrainMap.Field.TrySample(b.max.x+1.2f,z,out float targetGround);
                match.Tanks[0].MoveOnGround(new Vector3(b.min.x-6,shooterGround,z));
                match.Tanks[1].MoveOnGround(new Vector3(b.max.x+1.2f,targetGround,z));
                float hp=match.Tanks[1].Health;
                match.shots.SetWind(Vector3.zero);match.shots.SetAim(90,8,22);Assert.That(match.shots.TryFire(),Is.True);
                match.shots.ActiveRunner.Tick(testTuning.maxFlightTime+1);
                Assert.That(match.cover.Buildings[0].Health,Is.LessThan(match.cover.settings.health));
                Assert.That(match.Tanks[1].Health,Is.EqualTo(hp),"Tank within blast radius is shielded by the building at impact.");
            }
            finally { match.tuning=match.shots.tuning=original;Object.Destroy(testTuning); }
            yield return null;
        }
        [UnityTest] public IEnumerator Building_FoundationDestroyed_Collapses()
        {
            var cover=match.cover;var b=cover.Buildings[0];
            match.terrainMap.Field.ApplyCrater(b.Bounds.center,6,10);cover.SettleFoundations();
            Assert.That(b.Destroyed,Is.True);yield return null;
        }
    }
}
#endif
