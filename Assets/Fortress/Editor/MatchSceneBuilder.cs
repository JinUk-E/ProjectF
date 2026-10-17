using System;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace Fortress.Editor
{
    public static class MatchSceneBuilder
    {
        public const string ScenePath = "Assets/Fortress/Scenes/MilitaryMatch.unity";
        const string Data = "Assets/Fortress/Data/";

        public static void UpgradeRandomTerrainBatch()
        {
            try
            {
                var scene=EditorSceneManager.OpenScene(ScenePath);
                var match=UnityEngine.Object.FindAnyObjectByType<MatchController>();
                if(match==null) throw new InvalidOperationException("Military match is missing its controller.");
                Undo.RecordObject(match.tuning,"Enable seeded match terrain");
                match.tuning.randomTerrain=true;
                EditorUtility.SetDirty(match.tuning);
                AssetDatabase.SaveAssets();
                EditorSceneManager.SaveScene(scene,ScenePath);
                Debug.Log("MilitaryMatch now generates terrain from each match seed.");
                EditorApplication.Exit(0);
            }
            catch(Exception e) { Debug.LogException(e);EditorApplication.Exit(1); }
        }
        [MenuItem("Fortress/Play Military Match")]
        public static void PlayMatch()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode) return;
            if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.OpenScene(ScenePath);
            EditorApplication.ExecuteMenuItem("Window/General/Game");
            EditorApplication.EnterPlaymode();
        }

        public static void CreateBatch()
        {
            try { Create(); EditorApplication.Exit(0); }
            catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
        }
        public static void UpgradeWaterBatch()
        {
            try
            {
                if(!File.Exists(ScenePath)) Create();
                var scene=EditorSceneManager.OpenScene(ScenePath);
                var match=UnityEngine.Object.FindAnyObjectByType<MatchController>();
                if(match==null) throw new InvalidOperationException("Military match is missing its controller.");
                Undo.RecordObject(match.tuning,"Approved water level");
                match.tuning.waterHeight=-4f;
                EditorUtility.SetDirty(match.tuning);
                ApplyWater(match);
                AssetDatabase.SaveAssets();
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene,ScenePath);
                Debug.Log("Approved water level applied: -4m; terrain lower bound unchanged.");
                EditorApplication.Exit(0);
            }
            catch(Exception e) { Debug.LogException(e);EditorApplication.Exit(1); }
        }
        [MenuItem("Fortress/Open Military Match")]
        public static void Create()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            if (File.Exists(ScenePath)) { EditorSceneManager.OpenScene(ScenePath); return; }
            var scene = EditorSceneManager.OpenScene(MilitaryArtBuilder.ScenePath);
            EditorSceneManager.SaveScene(scene, ScenePath);
            var shots = UnityEngine.Object.FindFirstObjectByType<FlatShotController>();
            var tuning = UnityEngine.Object.Instantiate(shots.tuning);
            tuning.name = "MatchTuning";
            tuning.showDebugPrediction = false;
            tuning.waterHeight = -4f;
            tuning.randomTerrain = true;
            AssetDatabase.CreateAsset(tuning, Data + "MatchTuning.asset");
            shots.tuning = tuning;
            shots.terrainMap.tuning = tuning;
            var match = new GameObject("Hotseat Match").AddComponent<MatchController>();
            match.shots = shots;
            match.terrainMap = shots.terrainMap;
            match.tuning = tuning;
            match.initialSeed = 1;
            match.spawnAreas = new[] { Spawn("SpawnLeft", new Vector2(-29,-8), new Vector2(-21,8)), Spawn("SpawnRight", new Vector2(21,-8), new Vector2(29,8)) };
            CreateHud(match);
            CreateShotPresentation(shots);
            ApplyWater(match);
            EditorUtility.SetDirty(shots); EditorUtility.SetDirty(tuning);
            EditorUtility.SetDirty(match); EditorUtility.SetDirty(shots.terrainMap);
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log("Military hotseat match created: " + ScenePath);
        }
        static SpawnArea Spawn(string name, Vector2 min, Vector2 max)
        {
            var area = ScriptableObject.CreateInstance<SpawnArea>();
            area.name = name; area.min = min; area.max = max;
            AssetDatabase.CreateAsset(area, Data + name + ".asset"); return area;
        }
        static void ApplyWater(MatchController match)
        {
            var terrain=match.terrainMap.terrain;
            var water=GameObject.Find("Match Water");
            if(water==null)
            {
                water=GameObject.CreatePrimitive(PrimitiveType.Plane);water.name="Match Water";
                Undo.RegisterCreatedObjectUndo(water,"Create match water view");
                UnityEngine.Object.DestroyImmediate(water.GetComponent<Collider>());
            }
            Undo.RecordObject(water.transform,"Position match water");
            var origin=terrain.transform.position;var size=terrain.terrainData.size;
            water.transform.position=new Vector3(origin.x+size.x*.5f,match.tuning.waterHeight,origin.z+size.z*.5f);
            water.transform.localScale=new Vector3(size.x/10,1,size.z/10);
            var material=AssetDatabase.LoadAssetAtPath<Material>(Data+"MatchWater.mat");
            if(material==null)
            {
                material=new Material(Shader.Find("Universal Render Pipeline/Lit"));material.name="Match Water";
                material.SetColor("_BaseColor",new Color(.055f,.22f,.27f));
                material.SetFloat("_Smoothness",.8f);material.SetFloat("_Metallic",.25f);
                AssetDatabase.CreateAsset(material,Data+"MatchWater.mat");
            }
            var renderer=water.GetComponent<MeshRenderer>();renderer.sharedMaterial=material;
            renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
        }
        static void CreateShotPresentation(FlatShotController shots)
        {
            var settings=ScriptableObject.CreateInstance<BattleVisualTuning>();
            AssetDatabase.CreateAsset(settings,Data+"BattleVisualTuning.asset");
            var presentation=shots.gameObject.AddComponent<ShotPresentation>();
            presentation.shots=shots; presentation.tuning=settings;
            presentation.gunVisuals=new Transform[shots.tanks.Length];
            for(int i=0;i<shots.tanks.Length;i++)
                foreach(var renderer in shots.tanks[i].barrel.GetComponentsInChildren<MeshRenderer>())
                    if(renderer.enabled) { presentation.gunVisuals[i]=renderer.transform; break; }
            var material=new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
            material.name="Battle Flash";
            material.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/ThirdParty/KenneyParticles/smoke_01.png"));
            material.SetFloat("_Surface",1); material.SetFloat("_Blend",2);
            material.SetFloat("_SrcBlend",(float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend",(float)UnityEngine.Rendering.BlendMode.One);
            material.SetFloat("_ZWrite",0); material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); material.renderQueue=3000;
            AssetDatabase.CreateAsset(material,Data+"BattleFlash.mat");
            presentation.muzzleFlash=Flash("Muzzle Flash",settings.muzzleLifetime,settings.muzzleSize,settings.muzzleParticles,settings.flashColor,material);
            presentation.impactFlash=Flash("Impact Flash",settings.flashLifetime,settings.flashSize,settings.flashParticles,settings.flashColor,material);
        }
        static ParticleSystem Flash(string name,float lifetime,float size,int count,Color color,Material material)
        {
            var effect=new GameObject(name).AddComponent<ParticleSystem>();
            effect.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            var main=effect.main; main.loop=false; main.playOnAwake=false; main.duration=lifetime;
            main.startLifetime=lifetime; main.startSize=size; main.startSpeed=size; main.startColor=color;
            main.simulationSpace=ParticleSystemSimulationSpace.World;
            var emission=effect.emission; emission.rateOverTime=0; emission.SetBursts(new[]{new ParticleSystem.Burst(0,(short)count)});
            var shape=effect.shape; shape.shapeType=ParticleSystemShapeType.Sphere; shape.radius=size*.1f;
            var fade=effect.colorOverLifetime; fade.enabled=true;
            var gradient=new Gradient(); gradient.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1)},new[]{new GradientAlphaKey(1,0),new GradientAlphaKey(0,1)});
            fade.color=gradient;
            effect.GetComponent<ParticleSystemRenderer>().sharedMaterial=material;
            return effect;
        }
        static void CreateHud(MatchController match) { MatchHudBuilder.Create(match); }
    }
}
