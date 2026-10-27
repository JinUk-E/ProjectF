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
    public sealed class CasualPresentationTests
    {
        MatchController match;FlatShotController shots;BattlefieldOrbitCamera orbit;ProjectileFollowCamera follow;DashedAimPreview preview;
        [UnitySetUp] public IEnumerator Load()
        {
            BattleLoadoutSelection.Player=1;BattleLoadoutSelection.Opponent=0;
            EditorSceneManager.LoadSceneInPlayMode(SessionMenu.LocalScene,new LoadSceneParameters(LoadSceneMode.Single));yield return null;
            match=Object.FindFirstObjectByType<MatchController>();shots=match.shots;orbit=Camera.main.GetComponent<BattlefieldOrbitCamera>();follow=Camera.main.GetComponent<ProjectileFollowCamera>();preview=Object.FindFirstObjectByType<DashedAimPreview>();
            Assert.That(orbit.settings.casualPresentation,Is.True);Assert.That(follow.settings.casualPresentation,Is.True);Assert.That(preview.style.showImpactGuide,Is.False);
            shots.SetWind(Vector3.zero);shots.SetAim(0,75,22);
        }
        [UnityTearDown] public IEnumerator Unload()
        {
            if(shots!=null) shots.CancelShot();Time.timeScale=1;BattleLoadoutSelection.Player=BattleLoadoutSelection.Opponent=0;
            var scene=SceneManager.GetActiveScene();SceneManager.SetActiveScene(SceneManager.CreateScene("CasualCleanup"));yield return SceneManager.UnloadSceneAsync(scene);
        }
        [UnityTest] public IEnumerator BasicShot_UsesFixedComposition_ThenReturnsBehindNextTank()
        {
            Assert.That(shots.TryFire(),Is.True);Assert.That(follow.IsWideView,Is.True);
            var focus=follow.Focus;float radius=follow.FamilyRadius;var runner=shots.ActiveRunner;
            runner.Tick(.2f);follow.TickPresentation(.2f);runner.Tick(.2f);follow.TickPresentation(.2f);
            Assert.That(follow.Focus,Is.EqualTo(focus));Assert.That(follow.FamilyRadius,Is.EqualTo(radius));
            runner.Tick(30);follow.TickPresentation(follow.settings.impactHoldSeconds+.01f);follow.TickPresentation(follow.settings.returnSeconds+.01f);
            Assert.That(follow.IsTracking,Is.False);
            var toward=shots.tanks[1-shots.ActiveTankIndex].transform.position-shots.ActiveTank.transform.position;toward.y=0;
            var expected=shots.ActiveTank.transform.position+toward.normalized*Mathf.Min(orbit.settings.aimLookAhead,toward.magnitude*.5f);
            Assert.That(orbit.Target.x,Is.EqualTo(expected.x).Within(.01));Assert.That(orbit.Target.z,Is.EqualTo(expected.z).Within(.01));
            yield return null;
        }
        [UnityTest] public IEnumerator SpecialShot_KeepsCloseTracking_AndPauseFreezesIt()
        {
            Assert.That(shots.SelectAmmo(true),Is.True);Assert.That(shots.TryFire(),Is.True);Assert.That(follow.IsWideView,Is.False);
            shots.ActiveRunner.Tick(.2f);follow.TickPresentation(.2f);var position=follow.transform.position;
            shots.GameplaySuspended=true;follow.TickPresentation(10);Assert.That(follow.transform.position,Is.EqualTo(position));yield return null;
        }
        [UnityTest] public IEnumerator ViewButtons_PreserveAim_AndRespectPauseAndShotLock()
        {
            float yaw=shots.ActiveTank.Yaw,pitch=shots.ActiveTank.Pitch,power=shots.Power;
            orbit.ViewOpponent();Assert.That(orbit.Target.x,Is.EqualTo(shots.tanks[1].transform.position.x).Within(.01));
            orbit.ViewOwnTank();Assert.That(Vector2.Distance(new Vector2(orbit.Target.x,orbit.Target.z),new Vector2(shots.tanks[0].transform.position.x,shots.tanks[0].transform.position.z)),Is.LessThanOrEqualTo(orbit.settings.aimLookAhead+.01f));
            Assert.That(shots.ActiveTank.Yaw,Is.EqualTo(yaw));Assert.That(shots.ActiveTank.Pitch,Is.EqualTo(pitch));Assert.That(shots.Power,Is.EqualTo(power));
            var target=orbit.Target;shots.GameplaySuspended=true;orbit.ViewOpponent();Assert.That(orbit.Target,Is.EqualTo(target));
            shots.GameplaySuspended=false;yield return null;Assert.That(shots.TryFire(),Is.True);orbit.ViewOpponent();Assert.That(orbit.Target,Is.EqualTo(target));
        }
        [UnityTest] public IEnumerator MatchAim_IsShortWindFreeAndHasNoImpactGuide()
        {
            preview.Refresh();Assert.That(preview.ImpactGuideVisible,Is.False);
            Assert.That(preview.style.windAwarePreview,Is.False);
            Assert.That(shots.tuning.previewSeconds,Is.LessThanOrEqualTo(.35f));
            var lines=preview.GetComponentsInChildren<LineRenderer>();
            int count=shots.tuning.previewSegments;
            Assert.That(count,Is.EqualTo(8));
            Vector3 before=lines[count-1].GetPosition(1);
            Assert.That(Vector3.Distance(shots.ActiveTank.Muzzle.position,before),Is.LessThan(8f));
            shots.SetWind(new Vector3(1,0,0));preview.Refresh();
            Assert.That(lines[count-1].GetPosition(1),Is.EqualTo(before));
            Assert.That(preview.ImpactGuideVisible,Is.False);
            shots.GameplaySuspended=true;yield return null;foreach(var line in lines)Assert.That(line.enabled,Is.False);
            shots.GameplaySuspended=false;yield return null;yield return null;Assert.That(lines[0].enabled,Is.True);
            shots.InputEnabled=false;foreach(var line in lines)Assert.That(line.enabled,Is.False);shots.InputEnabled=true;
            Assert.That(shots.TryFire(),Is.True);foreach(var line in lines)Assert.That(line.enabled,Is.False);
        }
    }
}
#endif
