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
    public sealed class CombatFeedbackTests
    {
        MatchController match;FlatShotController shots;CombatFeedback feedback;
        [UnitySetUp] public IEnumerator Load()
        {
            BattleLoadoutSelection.Player=BattleLoadoutSelection.Opponent=0;
            EditorSceneManager.LoadSceneInPlayMode(SessionMenu.LocalScene,new LoadSceneParameters(LoadSceneMode.Single));yield return null;
            match=Object.FindFirstObjectByType<MatchController>();shots=match.shots;feedback=Object.FindFirstObjectByType<CombatFeedback>();
            Assert.That(feedback,Is.Not.Null);Assert.That(feedback.match,Is.SameAs(match));Assert.That(feedback.settings,Is.Not.Null);
        }
        [UnityTearDown] public IEnumerator Unload()
        {
            if(shots!=null)shots.CancelShot();Time.timeScale=1;AudioListener.pause=false;
            BattleLoadoutSelection.Player=BattleLoadoutSelection.Opponent=0;
            var old=SceneManager.GetActiveScene();SceneManager.SetActiveScene(SceneManager.CreateScene("FeedbackCleanup"));yield return SceneManager.UnloadSceneAsync(old);
        }
        void PrepareFlat()
        {
            match.cover=null;
            var values=new float[257,257];for(int z=0;z<257;z++)for(int x=0;x<257;x++)values[z,x]=4;
            var field=new HeightField(values,new Vector3(-80,-8,-80),new Vector2(160,160),-8,-4);match.terrainMap.InstallTerrain(field);
            match.Tanks[0].MoveOnGround(new Vector3(-10,4,0));match.Tanks[1].MoveOnGround(new Vector3(0,4,0));
            shots.SetSurface(new CombatShotSurface(field,match.Tanks,0));shots.SetWind(Vector3.zero);shots.SetAim(90,0,35);
        }
        [UnityTest] public IEnumerator ActualHit_ReportsHpDeltaOnce_AndPauseRestartClearPresentation()
        {
            PrepareFlat();float before=match.Tanks[1].Health;int count=0;match.ShotResolved+=r=>count++;
            Assert.That(shots.TryFire(),Is.True);Assert.That(count,Is.Zero);shots.ActiveRunner.Tick(30);
            Assert.That(count,Is.EqualTo(1));var damage=match.LastResolution.Second;
            Assert.That(damage.TotalDamage,Is.EqualTo(before-match.Tanks[1].Health).Within(.001f));
            Assert.That(damage.ExplosionDamage,Is.GreaterThan(0));Assert.That(damage.DirectHit,Is.True);
            Assert.That(feedback.damageLabels[1].text,Does.Contain("−"));Assert.That(feedback.hitLabel.text,Does.Contain("직격"));
            var camera=Camera.main;camera.GetComponent<ProjectileFollowCamera>().enabled=false;camera.GetComponent<BattlefieldOrbitCamera>().enabled=false;
            var pose=camera.transform.position;feedback.TickPresentation(.04f);feedback.Clear();Assert.That(camera.transform.position,Is.EqualTo(pose));
            // A second real shot creates feedback again, without invoking a presentation-only test hook.
            match.RestartMatch(2);PrepareFlat();Assert.That(shots.TryFire(),Is.True);shots.ActiveRunner.Tick(30);
            float remaining=feedback.RemainingSeconds;shots.GameplaySuspended=true;feedback.TickPresentation(10);
            Assert.That(feedback.RemainingSeconds,Is.EqualTo(remaining));
            shots.GameplaySuspended=false;yield return null;match.RestartMatch(3);
            Assert.That(feedback.RemainingSeconds,Is.Zero);Assert.That(feedback.hitLabel.enabled,Is.False);
        }
        [UnityTest] public IEnumerator NearbyBlast_IsNotDirect_AndCancelledShotHasNoDamageNotice()
        {
            PrepareFlat();match.Tanks[1].MoveOnGround(new Vector3(0,4,2));
            shots.SetSurface(new CombatShotSurface(match.terrainMap.Field,match.Tanks,0));shots.SetAim(90,0,15);
            Assert.That(shots.TryFire(),Is.True);shots.ActiveRunner.Tick(30);
            Assert.That(match.LastResolution.Second.DirectHit,Is.False);
            match.RestartMatch(4);PrepareFlat();int count=0;match.ShotResolved+=r=>count++;
            Assert.That(shots.TryFire(),Is.True);shots.CancelShot();Assert.That(count,Is.Zero);Assert.That(feedback.RemainingSeconds,Is.Zero);
            yield return null;
        }
        [UnityTest] public IEnumerator Cluster_ResolvesOneFamilyNotice_AndImpactStylesAreDistinct()
        {
            BattleLoadoutSelection.Player=3;match.RestartMatch(5);PrepareFlat();shots.SelectAmmo(true);shots.SetAim(90,65,22);
            int count=0;match.ShotResolved+=r=>count++;Assert.That(shots.TryFire(),Is.True);shots.ActiveRunner.Tick(100);
            Assert.That(count,Is.EqualTo(1));Assert.That(match.LastResolution.Purpose,Is.EqualTo(AmmoPurpose.Cluster));
            var presentation=Object.FindFirstObjectByType<ShotPresentation>();Assert.That(presentation.feedbackSettings,Is.SameAs(feedback.settings));
            Assert.That(feedback.settings.impacts.Length,Is.EqualTo(4));
            foreach(var style in feedback.settings.impacts)Assert.That(style.clip,Is.Not.Null);
            Assert.That(feedback.settings.impacts[0].clip,Is.Not.SameAs(feedback.settings.impacts[3].clip));
            yield return null;
        }
        [UnityTest] public IEnumerator ActualHit_CaptureReadableFeedback()
        {
            PrepareFlat();Assert.That(shots.TryFire(),Is.True);shots.ActiveRunner.Tick(30);
            var camera=Camera.main;var follow=camera.GetComponent<ProjectileFollowCamera>();follow.TickPresentation(.2f);
            // Render the saved scene's actual UI and the real damage event while impact feedback is visible.
            yield return null;yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot("Docs/Validation/combat-feedback.png");
        }
    }
}
#endif
