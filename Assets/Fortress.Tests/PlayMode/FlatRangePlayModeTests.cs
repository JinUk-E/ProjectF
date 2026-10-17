#if UNITY_EDITOR
using System.Collections;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Fortress.Tests
{
    public sealed class FlatRangePlayModeTests
    {
        FlatShotController controller;

        [UnitySetUp]
        public IEnumerator LoadRange()
        {
            EditorSceneManager.LoadSceneInPlayMode("Assets/Fortress/Scenes/FlatRange.unity",
                new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            controller = Object.FindFirstObjectByType<FlatShotController>();
            Assert.That(controller, Is.Not.Null);
        }

        [UnityTearDown]
        public IEnumerator CleanRange()
        {
            if (controller != null) controller.CancelShot();
            var scene = SceneManager.GetActiveScene();
            SceneManager.SetActiveScene(SceneManager.CreateScene("TestCleanup"));
            yield return SceneManager.UnloadSceneAsync(scene);
        }

        [UnityTest]
        public IEnumerator FlatRange_NormalShot_FinishesThroughNativeUpdateWithinOneCentimetre()
        {
            Assert.That(controller.tanks.Length, Is.EqualTo(2));
            controller.SetAim(90, 0, controller.tuning.minPower);
            int impacts = 0, finishes = 0;
            Vector3 actual = default;
            controller.ShotImpacted += runner => { impacts++; actual = runner.Position; };
            controller.ShotFinished += () => finishes++;
            Vector3 predicted = controller.LastPrediction.ImpactPosition;
            Assert.That(controller.TryFire(), Is.True);
            // Real Update lifecycle is intentional here; deterministic trajectory cases are covered in EditMode.
            float deadline = Time.realtimeSinceStartup + 10f;
            while (controller.IsFiring && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(controller.IsFiring, Is.False);
            Assert.That(impacts, Is.EqualTo(1));
            Assert.That(finishes, Is.EqualTo(1));
            Assert.That(Vector3.Distance(actual, predicted), Is.LessThanOrEqualTo(0.01f));
            Assert.That(controller.pool.ActiveCount, Is.Zero);
        }

        [UnityTest]
        public IEnumerator FlatRange_DisableDuringFlight_NativeLifecycleCancelsAndAllowsRestart()
        {
            int impacts = 0, finishes = 0;
            controller.ShotImpacted += _ => impacts++;
            controller.ShotFinished += () => finishes++;
            Assert.That(controller.TryFire(), Is.True);
            controller.enabled = false;
            yield return null;
            Assert.That(controller.IsFiring, Is.False);
            Assert.That(controller.ActiveRunner, Is.Null);
            Assert.That(controller.pool.ActiveCount, Is.Zero);
            Assert.That(impacts, Is.Zero);
            Assert.That(finishes, Is.Zero);
            controller.enabled = true;
            yield return null;
            Assert.That(controller.TryFire(), Is.True);
            Assert.That(controller.pool.ActiveCount, Is.EqualTo(1));
        }
    }
}
#endif
