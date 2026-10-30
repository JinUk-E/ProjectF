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
    public sealed class MobileWebControlsTests
    {
        MobileWebControls controls;
        SessionMenu menu;
        MatchController match;
        FlatShotController shots;
        int oldFrameRate;
        [UnitySetUp] public IEnumerator Setup()
        {
            oldFrameRate=Application.targetFrameRate;
            Time.timeScale=1;AudioListener.pause=false;BattleLoadoutSelection.Player=3;BattleLoadoutSelection.Opponent=0;
            yield return Load(SessionMenu.LocalScene);
            var go=new GameObject("Mobile web controls");Object.DontDestroyOnLoad(go);controls=go.AddComponent<MobileWebControls>();
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            if(shots!=null)shots.CancelShot();if(controls!=null)Object.Destroy(controls.gameObject);
            Time.timeScale=1;AudioListener.pause=false;BattleLoadoutSelection.Player=BattleLoadoutSelection.Opponent=0;
            Application.targetFrameRate=oldFrameRate;
            var old=SceneManager.GetActiveScene();SceneManager.SetActiveScene(SceneManager.CreateScene("MobileControlsCleanup"));yield return SceneManager.UnloadSceneAsync(old);
        }
        IEnumerator Load(string path)
        {
            EditorSceneManager.LoadSceneInPlayMode(path,new LoadSceneParameters(LoadSceneMode.Single));yield return null;yield return null;
            menu=Object.FindAnyObjectByType<SessionMenu>();match=menu.match;shots=match!=null?match.shots:null;
        }
        void Flat()
        {
            match.cover=null;var values=new float[257,257];for(int z=0;z<257;z++)for(int x=0;x<257;x++)values[z,x]=4;
            var field=new HeightField(values,new Vector3(-80,-8,-80),new Vector2(160,160),-8,-4);match.terrainMap.InstallTerrain(field);
            match.Tanks[0].MoveOnGround(new Vector3(-20,4,0));match.Tanks[1].MoveOnGround(new Vector3(20,4,0));
            shots.SetSurface(new CombatShotSurface(field,match.Tanks,match.ActivePlayerIndex));shots.SetWind(Vector3.zero);
        }
        void Hold(float x=0,float yaw=0,float pitch=0,float power=0,float orbitX=0,float zoom=0)
        {
            controls.Hold(JsonUtility.ToJson(new MobileWebControls.Held{x=x,yaw=yaw,pitch=pitch,power=power,orbitX=orbitX,zoom=zoom,epoch=controls.InputEpoch}));
        }
        [UnityTest] public IEnumerator MobileMoveAimAmmoAndShot_UseActualRulesAndClearAtTurnBoundary()
        {
            controls.Configure("1");Flat();var before=match.Tanks[0].Rig.transform.position;float budget=match.MovementRemaining;
            float yaw=shots.ActiveTank.Yaw,pitch=shots.ActiveTank.Pitch,power=shots.Power;
            Hold(1,1,1,1);controls.TickInput(.05f,.05f);
            Assert.That(Vector3.Distance(before,match.Tanks[0].Rig.transform.position),Is.GreaterThan(0));Assert.That(match.MovementRemaining,Is.LessThan(budget));
            Assert.That(shots.ActiveTank.Yaw,Is.Not.EqualTo(yaw));Assert.That(shots.ActiveTank.Pitch,Is.GreaterThan(pitch));Assert.That(shots.Power,Is.GreaterThan(power));
            controls.Action("special");Assert.That(shots.SpecialSelected,Is.True);controls.Action("basic");Assert.That(shots.SpecialSelected,Is.False);
            shots.SetAim(0,85,20);Hold(yaw:1);int epoch=controls.InputEpoch;controls.Action("fire");
            Assert.That(shots.IsFiring,Is.True);Assert.That(controls.InputEpoch,Is.GreaterThan(epoch));Assert.That(controls.GetState().editable,Is.False);
            controls.Action("fire");Assert.That(shots.ActiveFamilyCount,Is.EqualTo(1));shots.ActiveRunner.Tick(30);
            float nextYaw=shots.ActiveTank.Yaw;controls.TickInput(.1f,.1f);Assert.That(shots.ActiveTank.Yaw,Is.EqualTo(nextYaw));
            Assert.That(match.TurnNumber,Is.EqualTo(2));yield return null;
        }
        [UnityTest] public IEnumerator PauseResumeAndLeaseExpiry_NeverResumeHeldInput()
        {
            controls.Configure("1");Hold(yaw:1);controls.Action("pause");float yaw=shots.ActiveTank.Yaw;
            controls.TickInput(.1f,.1f);Assert.That(menu.IsPaused,Is.True);Assert.That(shots.ActiveTank.Yaw,Is.EqualTo(yaw));
            controls.Action("resume");yield return null;controls.TickInput(.1f,.1f);Assert.That(shots.ActiveTank.Yaw,Is.EqualTo(yaw));
            Hold(yaw:1);controls.TickInput(.05f,controls.inputLeaseSeconds+.01f);Assert.That(shots.ActiveTank.Yaw,Is.EqualTo(yaw));
            Hold(yaw:1);string stale=JsonUtility.ToJson(new MobileWebControls.Held{yaw=1,epoch=controls.InputEpoch});controls.ClearInput();controls.Hold(stale);
            controls.TickInput(.1f,.1f);Assert.That(shots.ActiveTank.Yaw,Is.EqualTo(yaw));
            controls.Action("main");Assert.That(menu.IsConfirming,Is.True);controls.Action("resume");Assert.That(menu.IsPaused,Is.True);Assert.That(menu.IsConfirming,Is.True);
            controls.Action("cancel");Assert.That(menu.IsPaused,Is.True);Assert.That(menu.IsConfirming,Is.False);
        }
        [UnityTest] public IEnumerator AiTurn_RejectsMoveAimFireAndAmmoCommands()
        {
            yield return Load(SessionMenu.AiScene);controls.Configure("1");Flat();shots.SetAim(0,85,20);controls.Action("fire");shots.ActiveRunner.Tick(30);
            Assert.That(shots.ai.OwnsCurrentTurn,Is.True);Assert.That(controls.GetState().editable,Is.False);
            var position=shots.ActiveTank.transform.position;float yaw=shots.ActiveTank.Yaw,budget=match.MovementRemaining;
            Hold(1,1,1,1);controls.TickInput(.1f,.1f);controls.Action("special");controls.Action("fire");
            Assert.That(shots.IsFiring,Is.False);Assert.That(shots.ActiveTank.transform.position,Is.EqualTo(position));Assert.That(match.MovementRemaining,Is.EqualTo(budget));Assert.That(shots.ActiveTank.Yaw,Is.EqualTo(yaw));
        }
        [UnityTest] public IEnumerator DesktopUiAndPerformance_AreRestoredWithoutOverwritingPause()
        {
            var group=menu.navigationGroup;float alpha=group.alpha;group.interactable=false;yield return null;Assert.That(group.interactable,Is.False,"Inactive adapter must not overwrite PC navigation.");group.interactable=true;
            controls.Configure("1");Assert.That(group.alpha,Is.Zero);Assert.That(group.blocksRaycasts,Is.False);Assert.That(Application.targetFrameRate,Is.EqualTo(30));
            controls.Action("pause");controls.Configure("0");Assert.That(group.alpha,Is.EqualTo(alpha));Assert.That(menu.combatHudGroup.interactable,Is.False);
            Assert.That(Application.targetFrameRate,Is.EqualTo(oldFrameRate));
            menu.Resume();Assert.That(menu.combatHudGroup.interactable,Is.True,"Mobile hiding must not poison the desktop pause snapshot.");
        }
        [UnityTest] public IEnumerator MainMenuSelectionAndAiEntry_AreAvailableWithoutCanvasClicks()
        {
            yield return Load(SessionMenu.MainScene);controls.Configure("1");var initial=BattleLoadoutSelection.Player;controls.Action("nextPlayer");
            Assert.That(BattleLoadoutSelection.Player,Is.Not.EqualTo(initial));Assert.That(controls.GetState().main,Is.True);Assert.That(controls.GetState().roleName,Is.Not.Empty);
            controls.Action("startAi");float deadline=Time.realtimeSinceStartup+15;
            while(SceneManager.GetActiveScene().path!=SessionMenu.AiScene&&Time.realtimeSinceStartup<deadline)yield return null;
            yield return null;yield return null;menu=Object.FindAnyObjectByType<SessionMenu>();match=menu.match;shots=match.shots;
            Assert.That(controls.GetState().ready,Is.True);Assert.That(controls.GetState().main,Is.False);Assert.That(controls.GetState().editable,Is.True);
        }
        [UnityTest] public IEnumerator MissingSceneAndMalformedInput_AreSafeAndDisabled()
        {
            controls.Configure("1");float yaw=shots.ActiveTank.Yaw;
            controls.Hold("invalid-json");controls.Hold("{}");controls.Orbit("invalid-json");controls.Zoom(float.NaN);controls.TickInput(.1f,.1f);
            Assert.That(shots.ActiveTank.Yaw,Is.EqualTo(yaw));
            var old=SceneManager.GetActiveScene();var empty=SceneManager.CreateScene("MobileNoScene");SceneManager.SetActiveScene(empty);yield return SceneManager.UnloadSceneAsync(old);
            controls.Action("fire");controls.Action("startAi");Assert.That(controls.GetState().ready,Is.False);
        }
        [UnityTest] public IEnumerator TouchCameraUsesExistingOrbitLimitsAndPauseGuard()
        {
            controls.Configure("1");var camera=Object.FindAnyObjectByType<BattlefieldOrbitCamera>();float yaw=camera.Yaw,distance=camera.Distance;
            Hold(orbitX:1,zoom:1);controls.TickInput(.1f,.1f);Assert.That(camera.Yaw,Is.Not.EqualTo(yaw));Assert.That(camera.Distance,Is.LessThan(distance));
            controls.Action("pause");yaw=camera.Yaw;distance=camera.Distance;Hold(orbitX:1,zoom:1);controls.TickInput(.1f,.1f);controls.Orbit("{\"x\":100,\"y\":0}");controls.Zoom(120);
            Assert.That(camera.Yaw,Is.EqualTo(yaw));Assert.That(camera.Distance,Is.EqualTo(distance));yield return null;
        }
        [UnityTest] public IEnumerator DisabledAdapterRejectsConfigurationAndCombatCommands()
        {
            controls.Configure("1");controls.enabled=false;float yaw=shots.ActiveTank.Yaw;controls.Configure("1");Hold(yaw:1);controls.TickInput(.1f,.1f);controls.Action("fire");
            Assert.That(controls.MobileEnabled,Is.False);Assert.That(controls.GetState().ready,Is.False);Assert.That(shots.IsFiring,Is.False);Assert.That(shots.ActiveTank.Yaw,Is.EqualTo(yaw));yield return null;
        }
        [UnityTest] public IEnumerator MatchResultRestartAndMainReturn_CompleteWithTouchCommands()
        {
            controls.Configure("1");Flat();match.Tanks[1].ApplyDamage(match.Tanks[1].MaxHealth);shots.SetAim(0,85,20);
            controls.Action("fire");shots.ActiveRunner.Tick(30);Assert.That(match.State,Is.EqualTo(MatchState.Ended));
            Assert.That(controls.GetState().ended,Is.True);Assert.That(controls.GetState().resultText,Does.Contain("플레이어 1 승리"));
            Assert.That(controls.GetState().editable,Is.False);Assert.That(controls.GetState().cameraEditable,Is.False);
            int generation=match.Generation;controls.Action("restart");yield return null;
            Assert.That(match.Generation,Is.GreaterThan(generation));Assert.That(match.Tanks[1].Health,Is.EqualTo(match.Tanks[1].MaxHealth));
            Assert.That(controls.GetState().editable,Is.True);controls.Action("main");Assert.That(menu.IsConfirming,Is.True);controls.Action("confirm");
            float deadline=Time.realtimeSinceStartup+15;
            while(SceneManager.GetActiveScene().path!=SessionMenu.MainScene&&Time.realtimeSinceStartup<deadline)yield return null;
            yield return null;yield return null;menu=Object.FindAnyObjectByType<SessionMenu>();match=menu.match;shots=match!=null?match.shots:null;
            Assert.That(controls.GetState().main,Is.True);Assert.That(controls.GetState().ready,Is.True);Assert.That(Time.timeScale,Is.EqualTo(1));
        }
    }
}
#endif
