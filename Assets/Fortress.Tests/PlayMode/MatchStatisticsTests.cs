#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;
namespace Fortress.Tests
{
    public sealed class MatchStatisticsTests
    {
        MatchController match;FlatShotController shots;MatchStatistics statistics;
        [UnitySetUp] public IEnumerator Load()
        {
            BattleLoadoutSelection.Player=BattleLoadoutSelection.Opponent=0;
            EditorSceneManager.LoadSceneInPlayMode(SessionMenu.LocalScene,new LoadSceneParameters(LoadSceneMode.Single));yield return null;
            match=Object.FindFirstObjectByType<MatchController>();shots=match.shots;statistics=Object.FindFirstObjectByType<MatchStatistics>();
            Assert.That(statistics,Is.Not.Null);Assert.That(statistics.match,Is.SameAs(match));
            var hud=Object.FindFirstObjectByType<MatchHud>();
            Assert.That(hud.resultPanel.GetComponentInChildren<MatchMetricsHud>(true).statistics,Is.SameAs(statistics));
        }
        [UnityTearDown] public IEnumerator Unload()
        {
            if(shots!=null)shots.CancelShot();Time.timeScale=1;
            var old=SceneManager.GetActiveScene();SceneManager.SetActiveScene(SceneManager.CreateScene("StatisticsCleanup"));yield return SceneManager.UnloadSceneAsync(old);
        }
        void PrepareFlat()
        {
            match.cover=null;var values=new float[257,257];for(int z=0;z<257;z++)for(int x=0;x<257;x++)values[z,x]=4;
            var field=new HeightField(values,new Vector3(-80,-8,-80),new Vector2(160,160),-8,-4);match.terrainMap.InstallTerrain(field);
            match.Tanks[0].MoveOnGround(new Vector3(-10,4,0));match.Tanks[1].MoveOnGround(new Vector3(0,4,0));
            shots.SetSurface(new CombatShotSurface(field,match.Tanks,0));shots.SetWind(Vector3.zero);shots.SetAim(90,0,35);
        }
        [UnityTest] public IEnumerator Statistics_UseCompletedRealDamage_ExcludeCancellationAndResetOnRestart()
        {
            PrepareFlat();Assert.That(shots.TryFire(),Is.True);shots.CancelShot();Assert.That(statistics.CompletedShots,Is.Zero);
            Assert.That(shots.TryFire(),Is.True);float before=match.Tanks[1].Health;shots.ActiveRunner.Tick(30);
            Assert.That(statistics.CompletedShots,Is.EqualTo(1));Assert.That(statistics.EnemyHitShots,Is.EqualTo(1));Assert.That(statistics.DirectHitShots,Is.EqualTo(1));
            Assert.That(statistics.DamageByPlayer[0],Is.EqualTo(before-match.Tanks[1].Health).Within(.001));Assert.That(statistics.ShotsByPlayer[1],Is.Zero);
            match.RestartMatch(123);Assert.That(statistics.CompletedShots,Is.Zero);Assert.That(statistics.DamageByPlayer[0],Is.Zero);Assert.That(statistics.ElapsedSeconds,Is.Zero);yield return null;
        }
        [UnityTest] public IEnumerator Clock_ExcludesPauseAndStopsAtResult()
        {
            PrepareFlat();float before=statistics.ElapsedSeconds;statistics.Tick(2);Assert.That(statistics.ElapsedSeconds,Is.EqualTo(before+2));
            shots.GameplaySuspended=true;statistics.Tick(10);Assert.That(statistics.ElapsedSeconds,Is.EqualTo(before+2));
            shots.GameplaySuspended=false;yield return null;match.Tanks[1].ApplyDamage(match.Tanks[1].Health-1);
            Assert.That(shots.TryFire(),Is.True);shots.ActiveRunner.Tick(30);Assert.That(statistics.Finished,Is.True);
            before=statistics.ElapsedSeconds;statistics.Tick(10);Assert.That(statistics.ElapsedSeconds,Is.EqualTo(before));
            var hud=Object.FindFirstObjectByType<MatchHud>();var metrics=hud.resultPanel.GetComponentInChildren<MatchMetricsHud>(true);
            Assert.That(metrics.summary.text,Does.Contain("1"));Assert.That(metrics.summary.enabled,Is.True);
            Assert.That(Object.FindFirstObjectByType<CombatFeedback>().hitLabel.enabled,Is.False,"Result title must not overlap a lingering hit banner.");
        }
        [Serializable] sealed class Sample
        {
            public int seed,shots,hitShots,directHits,turns,drownedDefeats;
            public string preset;
            public bool finished;
            public float simulatedFlightSeconds;
        }
        [Serializable] sealed class Report
        {
            public string actor="gameplay-programmer-codex";
            public string method="3 fixed seeds x noise-free/Easy, both seats same planner, HE only, <=40 shots, no human aiming time; not a 5–8 minute playtime measurement";
            public List<Sample> samples=new List<Sample>();
        }
        [UnityTest] public IEnumerator FixedSeedCalibration_RecordsOutcomesWithoutClaimingHumanPlaytime()
        {
            var original=AssetDatabase.LoadAssetAtPath<AiSettings>("Assets/Fortress/Data/AiEasy.asset");
            Assert.That(original,Is.Not.Null);var settings=Object.Instantiate(original);var report=new Report();
            try
            {
                foreach(bool noiseFree in new[]{true,false})foreach(int seed in new[]{7,19,31})
                {
                    settings.yawSigmaDegrees=noiseFree?0:original.yawSigmaDegrees;settings.pitchSigmaDegrees=noiseFree?0:original.pitchSigmaDegrees;settings.powerSigma=noiseFree?0:original.powerSigma;
                    match.RestartMatch(seed);var sample=new Sample{seed=seed,preset=noiseFree?"noise-free":"Easy"};
                    for(int i=0;i<40 && match.State!=MatchState.Ended;i++)
                    {
                        int player=match.ActivePlayerIndex;
                        var move=AiMovementPlanner.Plan(match,settings);
                        if(move.Distance>0)match.TryMoveActiveTank(move.Direction,move.Distance/Mathf.Max(.01f,match.Tanks[player].MoveSpeed));
                        var plan=AiShotPlanner.Plan(shots,match.Tanks[1-player].Center,settings,seed,(match.TurnNumber+1)/2);
                        shots.SetAim(plan.Yaw,plan.Pitch,plan.Power);Assert.That(shots.TryFire(),Is.True);
                        sample.simulatedFlightSeconds+=Mathf.Min(shots.tuning.maxFlightTime,shots.ActiveRunner.Prediction.MotionEndTime);
                        shots.ActiveRunner.Tick(shots.tuning.maxFlightTime+1);
                        var camera=Object.FindFirstObjectByType<ProjectileFollowCamera>();camera.TickPresentation(10);camera.TickPresentation(10);
                        yield return null;
                    }
                    sample.shots=statistics.CompletedShots;sample.hitShots=statistics.EnemyHitShots;sample.directHits=statistics.DirectHitShots;
                    sample.turns=match.TurnNumber;sample.drownedDefeats=statistics.DrownedDefeats;sample.finished=statistics.Finished;report.samples.Add(sample);
                    if(noiseFree && seed==7 && statistics.Finished)
                    { yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot("Docs/Validation/combat-result.png"); }
                    Debug.Log("Calibration "+sample.preset+" seed="+seed+" shots="+sample.shots+" hits="+sample.hitShots+" ended="+sample.finished+" drowned="+sample.drownedDefeats);
                }
                Directory.CreateDirectory("Docs/Validation");File.WriteAllText("Docs/Validation/combat-balance-sample.json",JsonUtility.ToJson(report,true));
                Assert.That(report.samples.Count,Is.EqualTo(6));
            }
            finally {Object.Destroy(settings);}
        }
    }
}
#endif
