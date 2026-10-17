#if UNITY_EDITOR
using System.Collections;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;
namespace Fortress.Tests
{
    public sealed class MovementMountainTests
    {
        MatchController match;
        [UnitySetUp] public IEnumerator Load()
        {
            EditorSceneManager.LoadSceneInPlayMode("Assets/Fortress/Scenes/MilitaryMatch.unity",new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;match=Object.FindAnyObjectByType<MatchController>();
        }
        [UnityTearDown] public IEnumerator Unload()
        { match.shots.CancelShot();var scene=SceneManager.GetActiveScene();SceneManager.SetActiveScene(SceneManager.CreateScene("MovementCleanup"));yield return SceneManager.UnloadSceneAsync(scene); }
        [UnityTest] public IEnumerator Move_ConsumesBudgetPreservesHealthAndLocksInFlight()
        {
            var tank=match.Tanks[0];tank.ApplyDamage(10);float hp=tank.Health;
            var start=tank.Rig.transform.position;float budget=match.MovementRemaining;
            Assert.That(match.TryMoveActiveTank(Vector2.right,.25f),Is.True);
            Assert.That(tank.Rig.transform.position.x,Is.GreaterThan(start.x));
            Assert.That(match.MovementRemaining,Is.LessThan(budget));Assert.That(tank.Health,Is.EqualTo(hp));
            float remaining=match.MovementRemaining;var position=tank.Rig.transform.position;
            match.shots.SetAim(0,85,30);Assert.That(match.shots.TryFire(),Is.True);
            Assert.That(match.TryMoveActiveTank(Vector2.right,1),Is.False);Assert.That(tank.Rig.transform.position,Is.EqualTo(position));
            match.shots.CancelShot();Assert.That(match.MovementRemaining,Is.EqualTo(remaining));
            match.shots.TryFire();match.shots.ActiveRunner.Tick(match.tuning.maxFlightTime+1);
            Assert.That(match.MovementRemaining,Is.EqualTo(match.tuning.moveDistancePerTurn));
            yield return null;
        }
        [UnityTest] public IEnumerator Keyboard_MoveFollowsCameraPreservesAimAndExhaustsBudget()
        {
            var focus=InputSystem.settings.backgroundBehavior;
            var editor=InputSystem.settings.editorInputBehaviorInPlayMode;
            InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            var keyboard=InputSystem.AddDevice<Keyboard>();
            try
            {
                var tank=match.Tanks[0];var start=tank.Rig.transform.position;float yaw=tank.Rig.Yaw;
                var forward=Camera.main.transform.forward;forward.y=0;forward.Normalize();
                InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.UpArrow));yield return null;yield return null;
                InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return null;
                var delta=tank.Rig.transform.position-start;delta.y=0;
                Assert.That(delta.magnitude,Is.GreaterThan(0));
                Assert.That(Vector3.Dot(delta.normalized,forward),Is.GreaterThan(.99f));
                Assert.That(tank.Rig.Yaw,Is.EqualTo(yaw).Within(.001f));
                Assert.That(Object.FindAnyObjectByType<MatchHud>().movementLabel.text,Does.Contain("LEFT"));
                match.TryMoveActiveTank(Vector2.right,100);
                Assert.That(match.MovementRemaining,Is.Zero);
                Assert.That(match.TryMoveActiveTank(Vector2.right,1),Is.False);
            }
            finally { InputSystem.RemoveDevice(keyboard);InputSystem.settings.backgroundBehavior=focus;InputSystem.settings.editorInputBehaviorInPlayMode=editor; }
        }
        [UnityTest] public IEnumerator Move_RejectsWaterAndOtherTankWithoutSpendingFuel()
        {
            var tank=match.Tanks[0];var other=match.Tanks[1];var start=tank.Rig.transform.position;
            other.MoveOnGround(new Vector3(start.x+2.1f,tank.GroundHeight,start.z));
            float fuel=match.MovementRemaining;
            Assert.That(match.TryMoveActiveTank(Vector2.right,.5f),Is.False);
            Assert.That(match.MovementRemaining,Is.EqualTo(fuel));
            other.MoveOnGround(new Vector3(25,0,0));
            var field=match.terrainMap.Field;
            field.ApplyCrater(start,5,20);
            Assert.That(match.TryMoveActiveTank(Vector2.right,1),Is.False);
            Assert.That(match.MovementRemaining,Is.EqualTo(fuel));
            yield return null;
        }
        [UnityTest] public IEnumerator Mountain_MultipleSeeds_HaveStrongReliefAndDrySpawns()
        {
            float lowestRelief=float.PositiveInfinity;
            for(int seed=1;seed<=8;seed++)
            {
                match.RestartMatch(seed);var map=match.terrainMap.Field;
                float min=float.PositiveInfinity,max=float.NegativeInfinity;
                for(int z=0;z<map.Depth;z+=4) for(int x=0;x<map.Width;x+=4)
                { min=Mathf.Min(min,map.GetHeight(x,z));max=Mathf.Max(max,map.GetHeight(x,z)); }
                lowestRelief=Mathf.Min(lowestRelief,max-min);Assert.That(max-min,Is.GreaterThan(20));
                foreach(var tank in match.Tanks) Assert.That(tank.GroundHeight,Is.GreaterThan(-4));
            }
            TestContext.WriteLine("Minimum mountain relief across 8 seeds: "+lowestRelief+"m");
            yield return null;
        }
    }
}
#endif
