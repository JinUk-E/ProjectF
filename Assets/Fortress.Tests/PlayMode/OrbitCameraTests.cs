#if UNITY_EDITOR
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using Object=UnityEngine.Object;
namespace Fortress.Tests
{
    public sealed class OrbitCameraTests
    {
        BattlefieldOrbitCamera orbit;
        [UnitySetUp] public IEnumerator Load()
        {
            EditorSceneManager.LoadSceneInPlayMode("Assets/Fortress/Scenes/MilitaryAI.unity",new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;orbit=Object.FindAnyObjectByType<BattlefieldOrbitCamera>();Assert.That(orbit,Is.Not.Null);
        }
        [UnityTearDown] public IEnumerator Unload()
        {
            orbit.match.shots.CancelShot();var scene=SceneManager.GetActiveScene();SceneManager.SetActiveScene(SceneManager.CreateScene("CameraCleanup"));
            yield return SceneManager.UnloadSceneAsync(scene);
        }
        [UnityTest] public IEnumerator Orbit_RotatesZoomsClampsAndDoesNotChangeAim()
        {
            var shots=orbit.match.shots;float aimYaw=shots.ActiveTank.Yaw,pitch=shots.ActiveTank.Pitch,power=shots.Power;
            var before=orbit.transform.position;
            orbit.Orbit(new Vector2(150,50));orbit.ApplyPose();
            Assert.That(Vector3.Distance(before,orbit.transform.position),Is.GreaterThan(1));
            float distance=orbit.Distance;orbit.Zoom(120);Assert.That(orbit.Distance,Is.LessThan(distance));
            orbit.Zoom(100000);Assert.That(orbit.Distance,Is.GreaterThanOrEqualTo(orbit.settings.minDistance));
            orbit.Zoom(-100000);Assert.That(orbit.Distance,Is.LessThanOrEqualTo(orbit.settings.maxDistance));
            orbit.Orbit(new Vector2(0,100000));Assert.That(orbit.Pitch,Is.InRange(orbit.settings.minPitch,orbit.settings.maxPitch));
            orbit.Orbit(new Vector2(0,-100000));orbit.ApplyPose();
            Assert.That(orbit.Pitch,Is.InRange(orbit.settings.minPitch,orbit.settings.maxPitch));
            Assert.That(shots.ActiveTank.Yaw,Is.EqualTo(aimYaw));Assert.That(shots.ActiveTank.Pitch,Is.EqualTo(pitch));Assert.That(shots.Power,Is.EqualTo(power));
            yield return null;
        }
        [UnityTest] public IEnumerator FocusAndTurn_RespectManualViewAndStayAboveTerrain()
        {
            orbit.FocusActiveTank();
            var tank=orbit.match.shots.ActiveTank;
            Assert.That(orbit.Target.x,Is.EqualTo(tank.transform.position.x).Within(.01));
            Assert.That(orbit.Target.z,Is.EqualTo(tank.transform.position.z).Within(.01));
            orbit.Orbit(new Vector2(200,0));orbit.ApplyPose();float yaw=orbit.Yaw;
            var shots=orbit.match.shots;shots.SetAim(0,85,30);Assert.That(shots.TryFire(),Is.True);
            shots.ActiveRunner.Tick(shots.tuning.maxFlightTime+1);
            yield return null;
            Assert.That(orbit.Yaw,Is.EqualTo(yaw));
            orbit.FocusActiveTank();orbit.Zoom(100000);orbit.ApplyPose();
            var p=orbit.transform.position;
            if(orbit.match.terrainMap.Field.TrySample(p.x,p.z,out float h)) Assert.That(p.y,Is.GreaterThanOrEqualTo(h+orbit.settings.groundClearance-.01f));
        }
        [UnityTest] public IEnumerator Mouse_RightDragAndWheel_RouteThroughInputSystem()
        {
            var oldFocus=InputSystem.settings.backgroundBehavior;var oldEditor=InputSystem.settings.editorInputBehaviorInPlayMode;
            InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            var mouse=InputSystem.AddDevice<Mouse>();
            orbit.SendMessage("OnApplicationFocus",true);
            try
            {
                var state=new MouseState { position=new Vector2(Screen.width*.5f,Screen.height*.5f) }.WithButton(MouseButton.Right);
                InputSystem.QueueStateEvent(mouse,state);yield return null;yield return null;
                float yaw=orbit.Yaw,distance=orbit.Distance;
                InputSystem.QueueDeltaStateEvent(mouse.delta,new Vector2(120,0));
                InputSystem.QueueDeltaStateEvent(mouse.scroll,new Vector2(0,120));
                yield return null;yield return null;
                Assert.That(Mathf.Abs(Mathf.DeltaAngle(yaw,orbit.Yaw)),Is.GreaterThan(1));
                Assert.That(orbit.Distance,Is.LessThan(distance));
                orbit.SendMessage("OnApplicationFocus",false);Assert.That(orbit.IsDragging,Is.False);
            }
            finally
            {
                InputSystem.RemoveDevice(mouse);InputSystem.settings.backgroundBehavior=oldFocus;InputSystem.settings.editorInputBehaviorInPlayMode=oldEditor;
            }
        }
    }
}
#endif
