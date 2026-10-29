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
    public sealed class AiMovementTests
    {
        MatchController match;AiOpponent ai;AiSettings settings;FlatShotController shots;ProjectileFollowCamera camera;
        [UnitySetUp] public IEnumerator Load()
        {
            BattleLoadoutSelection.Player=BattleLoadoutSelection.Opponent=0;
            EditorSceneManager.LoadSceneInPlayMode(SessionMenu.AiScene,new LoadSceneParameters(LoadSceneMode.Single));yield return null;
            ai=Object.FindFirstObjectByType<AiOpponent>();match=ai.match;shots=match.shots;camera=Object.FindFirstObjectByType<ProjectileFollowCamera>();
            settings=Object.Instantiate(ai.settings);settings.enableMovement=true;settings.rangePreference=.08f;ai.Configure(match,settings);
            shots.SetWind(Vector3.zero);shots.SetAim(0,85,20);Assert.That(shots.TryFire(),Is.True);shots.ActiveRunner.Tick(30);
            var values=new float[257,257];for(int z=0;z<257;z++)for(int x=0;x<257;x++)values[z,x]=4;
            var field=new HeightField(values,new Vector3(-80,-8,-80),new Vector2(160,160),-8,-4);match.terrainMap.InstallTerrain(field);
            match.cover=null;match.Tanks[0].MoveOnGround(new Vector3(-30,4,0));match.Tanks[1].MoveOnGround(new Vector3(30,4,0));
            shots.SetSurface(new CombatShotSurface(field,match.Tanks,1));ai.Configure(match,settings);
        }
        [UnityTearDown] public IEnumerator Unload()
        {
            Time.timeScale=1;if(shots!=null)shots.CancelShot();if(settings!=null)Object.Destroy(settings);
            var old=SceneManager.GetActiveScene();SceneManager.SetActiveScene(SceneManager.CreateScene("AiMoveCleanup"));yield return SceneManager.UnloadSceneAsync(old);
        }
        void FinishCamera() { camera.TickPresentation(10);camera.TickPresentation(10); }
        [UnityTest] public IEnumerator Plan_IsDeterministicSafeAndDoesNotMutateLiveWorld()
        {
            var before=match.Tanks[1].Rig.transform.position;float budget=match.MovementRemaining,power=shots.Power;
            var first=AiMovementPlanner.Plan(match,settings);var second=AiMovementPlanner.Plan(match,settings);
            Assert.That(first.Distance,Is.GreaterThan(0));Assert.That(first.Distance,Is.LessThanOrEqualTo(6.001f));
            Assert.That(second.Destination,Is.EqualTo(first.Destination));Assert.That(match.Tanks[1].Rig.transform.position,Is.EqualTo(before));
            Assert.That(match.MovementRemaining,Is.EqualTo(budget));Assert.That(shots.Power,Is.EqualTo(power));
            float preview=match.PreviewMoveActiveTank(first.Direction,first.Distance,out var endpoint);
            Assert.That(endpoint,Is.EqualTo(first.Destination));Assert.That(preview,Is.EqualTo(first.Distance));yield return null;
        }
        [UnityTest] public IEnumerator Ai_WaitsForCamera_MovesWithinBudget_ThenThinksFullDelayAndFires()
        {
            var before=match.Tanks[1].Rig.transform.position;float budget=match.MovementRemaining;
            ai.TickThinking(100);ai.TickMovement(100);Assert.That(match.Tanks[1].Rig.transform.position,Is.EqualTo(before));
            FinishCamera();ai.TickThinking(0);Assert.That(ai.IsMoving,Is.True);
            for(int i=0;i<40 && ai.IsMoving;i++)ai.TickMovement(.1f);
            Assert.That(ai.IsMoving,Is.False);Assert.That(shots.IsFiring,Is.False);
            float distance=Vector3.Distance(before,match.Tanks[1].Rig.transform.position);
            Assert.That(distance,Is.InRange(.1f,6.01f));Assert.That(budget-match.MovementRemaining,Is.EqualTo(distance).Within(.01));
            Assert.That(ai.RemainingThinkSeconds,Is.EqualTo(settings.thinkSeconds));
            ai.TickThinking(settings.thinkSeconds-.1f);Assert.That(shots.IsFiring,Is.False);ai.TickThinking(.11f);Assert.That(shots.IsFiring,Is.True);
            yield return null;
        }
        [UnityTest] public IEnumerator Movement_PauseAndRestartCancelWithoutTeleportOrDelayedShot()
        {
            FinishCamera();ai.TickThinking(0);ai.TickMovement(.1f);var position=match.Tanks[1].Rig.transform.position;
            shots.GameplaySuspended=true;ai.TickMovement(10);ai.TickThinking(10);Assert.That(match.Tanks[1].Rig.transform.position,Is.EqualTo(position));
            shots.GameplaySuspended=false;yield return null;match.RestartMatch(11);position=match.Tanks[1].Rig.transform.position;
            ai.TickMovement(10);ai.TickThinking(10);Assert.That(ai.IsMoving,Is.False);Assert.That(shots.IsFiring,Is.False);Assert.That(match.Tanks[1].Rig.transform.position,Is.EqualTo(position));
        }
        [UnityTest] public IEnumerator SharedMovementProbe_StopsAtMapEdgeExactlyLikeActualMovement()
        {
            match.Tanks[1].MoveOnGround(new Vector3(78,4,0));float budget=match.MovementRemaining;
            float preview=match.PreviewMoveActiveTank(Vector2.right,6,out var endpoint);
            Assert.That(preview,Is.LessThan(6));Assert.That(match.TryMoveActiveTank(Vector2.right,6/match.tuning.moveSpeed),Is.True);
            Assert.That(match.Tanks[1].Rig.transform.position,Is.EqualTo(endpoint));Assert.That(budget-match.MovementRemaining,Is.EqualTo(preview));yield return null;
        }
        [UnityTest] public IEnumerator Planner_PrefersReachableCoverOverUnprotectedApproach()
        {
            var cover=Object.FindFirstObjectByType<BattlefieldCover>();Assert.That(cover,Is.Not.Null);
            cover.Install(match.terrainMap.Field,new[]{new CoverBuilding(new Bounds(new Vector3(20,6,4),new Vector3(4,4,2)),45)});
            match.cover=cover;
            Assert.That(cover.Shields(match.Tanks[0].Center,match.Tanks[1].Center),Is.False);
            var plan=AiMovementPlanner.Plan(match,settings);Assert.That(plan.Distance,Is.GreaterThan(0));
            Assert.That(cover.Shields(match.Tanks[0].Center,plan.Destination+Vector3.up*match.Tanks[1].Height*.5f),Is.True);
            Assert.That(cover.BlocksMovement(match.Tanks[1].Rig.transform.position,plan.Destination,match.Tanks[1].Radius),Is.False);
            yield return null;
        }
    }
}
#endif
