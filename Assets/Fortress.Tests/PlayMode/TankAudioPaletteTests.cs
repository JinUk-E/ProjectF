#if UNITY_EDITOR
using System.Collections;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;
namespace Fortress.Tests
{
    public sealed class TankAudioPaletteTests
    {
        MatchController match;FlatShotController shots;TankMovementAudio movement;ShotPresentation presentation;
        [UnitySetUp] public IEnumerator Load()
        {
            BattleLoadoutSelection.Player=BattleLoadoutSelection.Opponent=0;
            EditorSceneManager.LoadSceneInPlayMode(SessionMenu.LocalScene,new LoadSceneParameters(LoadSceneMode.Single));yield return null;
            match=Object.FindFirstObjectByType<MatchController>();shots=match.shots;movement=match.GetComponent<TankMovementAudio>();presentation=Object.FindFirstObjectByType<ShotPresentation>();
            Assert.That(movement,Is.Not.Null);PrepareFlat();
        }
        [UnityTearDown] public IEnumerator Unload()
        {
            if(shots!=null)shots.CancelShot();Time.timeScale=1;AudioListener.pause=false;
            BattleLoadoutSelection.Player=BattleLoadoutSelection.Opponent=0;
            var old=SceneManager.GetActiveScene();SceneManager.SetActiveScene(SceneManager.CreateScene("TankAudioCleanup"));yield return SceneManager.UnloadSceneAsync(old);
        }
        void PrepareFlat()
        {
            match.cover=null;var values=new float[257,257];for(int z=0;z<257;z++)for(int x=0;x<257;x++)values[z,x]=4;
            var field=new HeightField(values,new Vector3(-80,-8,-80),new Vector2(160,160),-8,-4);match.terrainMap.InstallTerrain(field);
            match.Tanks[0].MoveOnGround(new Vector3(-20,4,0));match.Tanks[1].MoveOnGround(new Vector3(20,4,0));
            shots.SetSurface(new CombatShotSurface(field,match.Tanks,match.ActivePlayerIndex));shots.SetWind(Vector3.zero);movement.StopImmediate();
        }
        void Move()
        {
            Assert.That(match.TryMoveActiveTank(Vector2.up,.02f),Is.True);movement.TickAudio(.02f);
            Assert.That(movement.Level,Is.GreaterThan(0));Assert.That(movement.engine.volume,Is.GreaterThan(0));Assert.That(movement.tracks.volume,Is.GreaterThan(0));
        }
        [UnityTest] public IEnumerator CommittedMovementFadesIn_ThenBlockedMovementFadesOut()
        {
            Assert.That(movement.Level,Is.Zero);Move();
            // No movement event for zero input or a disabled movement budget, even while a button is held.
            Assert.That(match.TryMoveActiveTank(Vector2.zero,.1f),Is.False);
            shots.InputEnabled=false;Assert.That(match.TryMoveActiveTank(Vector2.up,.1f),Is.False);movement.TickAudio(.02f);
            Assert.That(movement.Level,Is.Zero);Assert.That(movement.engine.isPlaying,Is.False);
            shots.InputEnabled=true;Move();for(int i=0;i<30;i++)movement.TickAudio(.02f);
            Assert.That(movement.Level,Is.Zero);Assert.That(movement.tracks.isPlaying,Is.False);yield return null;
        }
        [UnityTest] public IEnumerator PauseRestartDisableAndSpawnNeverLeaveEngineRunning()
        {
            Move();shots.GameplaySuspended=true;movement.TickAudio(.02f);Assert.That(movement.Level,Is.Zero);
            shots.GameplaySuspended=false;yield return null;movement.TickAudio(.02f);Assert.That(movement.Level,Is.Zero);
            Move();match.RestartMatch(114);Assert.That(movement.Level,Is.Zero);PrepareFlat();
            Move();movement.enabled=false;Assert.That(movement.engine.isPlaying,Is.False);Assert.That(movement.tracks.isPlaying,Is.False);
            movement.enabled=true;movement.TickAudio(.02f);Assert.That(movement.Level,Is.Zero);
            Move();AudioListener.pause=true;movement.TickAudio(.02f);Assert.That(movement.Level,Is.Zero);AudioListener.pause=false;
        }
        [UnityTest] public IEnumerator ActualShotStopsMovement_PlaysImpactAndKeepsOriginalCannon()
        {
            Move();var cannon=presentation.shotAudio.clip;
            Assert.That(AssetDatabase.GetAssetPath(cannon),Is.EqualTo("Assets/Fortress/Art/Atmosphere/Cannon.wav"));
            shots.SetAim(90,35,16);int before=presentation.ImpactSoundsPlayed;
            Assert.That(shots.TryFire(),Is.True);Assert.That(movement.Level,Is.Zero);shots.ActiveRunner.Tick(100);
            Assert.That(presentation.ImpactSoundsPlayed,Is.EqualTo(before+1));Assert.That(presentation.shotAudio.clip,Is.SameAs(cannon));
            yield return null;
        }
        [UnityTest] public IEnumerator ClusterImpactsKeepGameplayButLimitSameFrameAudio()
        {
            BattleLoadoutSelection.Player=3;match.RestartMatch(7);PrepareFlat();shots.SelectAmmo(true);shots.SetAim(90,65,22);
            int impacts=0;shots.ShotImpacted+=r=>impacts++;int before=presentation.ImpactSoundsPlayed;
            Assert.That(shots.TryFire(),Is.True);shots.ActiveRunner.Tick(100);
            Assert.That(impacts,Is.GreaterThan(1));Assert.That(presentation.ImpactSoundsPlayed-before,Is.EqualTo(1));
            Assert.That(shots.IsFiring,Is.False);Assert.That(match.LastResolution.Purpose,Is.EqualTo(AmmoPurpose.Cluster));yield return null;
        }
        [UnityTest] public IEnumerator StoredLoopsAndArmorAreReadyInBothBattleScenes()
        {
            foreach(var clip in new[]{movement.settings.engineLoop,movement.settings.trackLoop})
            {
                Assert.That(clip,Is.Not.Null);var samples=new float[clip.samples*clip.channels];Assert.That(clip.GetData(samples,0),Is.True);
                float peak=0;foreach(float value in samples)peak=Mathf.Max(peak,Mathf.Abs(value));Assert.That(peak,Is.InRange(.1f,.91f));
                Assert.That(Mathf.Abs(samples[0]-samples[samples.Length-1]),Is.LessThan(.2f));
            }
            foreach(string name in new[]{"Main","Siege","Excavator","Support","Heavy"})
            {
                var appearance=AssetDatabase.LoadAssetAtPath<TankAppearance>("Assets/Fortress/Art/TankRoles/"+name+".asset");Assert.That(appearance.portrait,Is.Not.Null);
                foreach(var piece in appearance.pieces)foreach(var material in piece.playerMaterials)
                    if(material.name=="Side0_Main"){var color=material.GetColor("_BaseColor");Assert.That(color.b,Is.LessThan(color.r));Assert.That(color.g-color.r,Is.LessThan(.1f));Assert.That(color.a,Is.EqualTo(1));}
            }
            EditorSceneManager.LoadSceneInPlayMode(SessionMenu.AiScene,new LoadSceneParameters(LoadSceneMode.Single));yield return null;
            match=Object.FindFirstObjectByType<MatchController>();shots=match.shots;movement=match.GetComponent<TankMovementAudio>();
            Assert.That(movement,Is.Not.Null);Assert.That(movement.engine.clip,Is.SameAs(movement.settings.engineLoop));PrepareFlat();Move();
        }
    }
}
#endif
