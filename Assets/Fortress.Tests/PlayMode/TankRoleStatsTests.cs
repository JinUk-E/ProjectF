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
    public sealed class TankRoleStatsTests
    {
        MatchController match;FlatShotController shots;
        [UnitySetUp] public IEnumerator Load()
        {
            BattleLoadoutSelection.Player=4;BattleLoadoutSelection.Opponent=3;
            EditorSceneManager.LoadSceneInPlayMode(SessionMenu.LocalScene,new LoadSceneParameters(LoadSceneMode.Single));yield return null;
            match=Object.FindFirstObjectByType<MatchController>();shots=match.shots;
        }
        [UnityTearDown] public IEnumerator Unload()
        {
            if(shots!=null)shots.CancelShot();Time.timeScale=1;BattleLoadoutSelection.Player=BattleLoadoutSelection.Opponent=0;
            var old=SceneManager.GetActiveScene();SceneManager.SetActiveScene(SceneManager.CreateScene("RoleStatsCleanup"));yield return SceneManager.UnloadSceneAsync(old);
        }
        void Flat()
        {
            match.cover=null;var values=new float[257,257];for(int z=0;z<257;z++)for(int x=0;x<257;x++)values[z,x]=4;
            var field=new HeightField(values,new Vector3(-80,-8,-80),new Vector2(160,160),-8,-4);match.terrainMap.InstallTerrain(field);
            match.Tanks[0].MoveOnGround(new Vector3(-20,4,0));match.Tanks[1].MoveOnGround(new Vector3(20,4,0));
            shots.SetSurface(new CombatShotSurface(field,match.Tanks,match.ActivePlayerIndex));shots.SetWind(Vector3.zero);
        }
        [UnityTest] public IEnumerator HeavyAndSupport_HaveActualDifferentHealthMovementAndHudCapacity()
        {
            Assert.That(match.Tanks[0].MaxHealth,Is.EqualTo(130).Within(.001));Assert.That(match.Tanks[1].MaxHealth,Is.EqualTo(85).Within(.001));
            Assert.That(match.Tanks[0].MoveDistance,Is.EqualTo(8).Within(.001));Assert.That(match.Tanks[1].MoveDistance,Is.EqualTo(16).Within(.001));
            Assert.That(match.Tanks[0].MoveSpeed,Is.EqualTo(2.8f).Within(.001));Assert.That(match.Tanks[1].MoveSpeed,Is.EqualTo(4.8f).Within(.001));
            Flat();float start=match.Tanks[0].Rig.transform.position.x;Assert.That(match.TryMoveActiveTank(Vector2.right,1),Is.True);
            Assert.That(match.Tanks[0].Rig.transform.position.x-start,Is.EqualTo(2.8f).Within(.001));Assert.That(match.MovementRemaining,Is.EqualTo(5.2f).Within(.001));
            var hud=Object.FindFirstObjectByType<MatchHud>();match.Tanks[0].ApplyDamage(40);hud.Refresh();
            Assert.That(hud.healthFills[0].fillAmount,Is.EqualTo(90f/130).Within(.001));Assert.That(hud.healthFills[1].fillAmount,Is.EqualTo(1).Within(.001));
            shots.SetAim(0,85,20);Assert.That(shots.TryFire(),Is.True);shots.ActiveRunner.Tick(30);
            Assert.That(match.ActivePlayerIndex,Is.EqualTo(1));Assert.That(match.MovementRemaining,Is.EqualTo(16).Within(.001));yield return null;
        }
        [UnityTest] public IEnumerator RoleSnapshot_DoesNotChangeMidMatch_AndRestartRefreshesIt()
        {
            var roles=shots.availableLoadouts;var copy=Object.Instantiate(roles[4]);var configured=(TankLoadout[])roles.Clone();configured[4]=copy;shots.availableLoadouts=configured;
            try
            {
                match.RestartMatch(15);float health=match.Tanks[0].MaxHealth,distance=match.Tanks[0].MoveDistance,speed=match.Tanks[0].MoveSpeed;
                copy.healthMultiplier=2;copy.movementMultiplier=2;copy.speedMultiplier=2;
                Assert.That(match.Tanks[0].MaxHealth,Is.EqualTo(health));Assert.That(match.Tanks[0].MoveDistance,Is.EqualTo(distance));Assert.That(match.Tanks[0].MoveSpeed,Is.EqualTo(speed));
                match.RestartMatch(15);Assert.That(match.Tanks[0].MaxHealth,Is.EqualTo(200));Assert.That(match.Tanks[0].MoveDistance,Is.EqualTo(24));Assert.That(match.Tanks[0].MoveSpeed,Is.EqualTo(8));
                Assert.That(match.Tanks[0].Health,Is.EqualTo(200));Assert.That(match.MovementRemaining,Is.EqualTo(24));
            }
            finally {shots.availableLoadouts=roles;Object.Destroy(copy);}
            yield return null;
        }
        [UnityTest] public IEnumerator AllRoles_UseConfiguredValuesAndLegacyFallbackRemainsNeutral()
        {
            var expectedHealth=new[]{100f,115,95,85,130};var expectedDistance=new[]{12f,9,14,16,8};
            for(int i=0;i<5;i++)
            {
                BattleLoadoutSelection.Player=i;match.RestartMatch(16);
                Assert.That(match.Tanks[0].MaxHealth,Is.EqualTo(expectedHealth[i]).Within(.001));Assert.That(match.MovementRemaining,Is.EqualTo(expectedDistance[i]).Within(.001));
            }
            var legacy=new TankCombatant(shots.tanks[0],match.tuning);Assert.That(legacy.MaxHealth,Is.EqualTo(match.tuning.maxHealth));Assert.That(legacy.MoveDistance,Is.EqualTo(match.tuning.moveDistancePerTurn));
            yield return null;
        }
        [UnityTest] public IEnumerator AiScene_ReceivesBothSelectedRoleSnapshots()
        {
            EditorSceneManager.LoadSceneInPlayMode(SessionMenu.AiScene,new LoadSceneParameters(LoadSceneMode.Single));yield return null;
            match=Object.FindFirstObjectByType<MatchController>();shots=match.shots;
            Assert.That(match.Tanks[0].MaxHealth,Is.EqualTo(130).Within(.001));Assert.That(match.Tanks[1].MaxHealth,Is.EqualTo(85).Within(.001));
            Assert.That(match.Tanks[1].MoveSpeed,Is.EqualTo(4.8f).Within(.001));
        }
    }
}
#endif
