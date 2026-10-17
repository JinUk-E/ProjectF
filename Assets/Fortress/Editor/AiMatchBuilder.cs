using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Fortress.Editor
{
    public static class AiMatchBuilder
    {
        public const string ScenePath="Assets/Fortress/Scenes/MilitaryAI.unity";
        public static void CreateBatch()
        {
            try { Create();EditorApplication.Exit(0); }
            catch(Exception e) { Debug.LogException(e);EditorApplication.Exit(1); }
        }
        static void Create()
        {
            var scene=EditorSceneManager.OpenScene(MatchSceneBuilder.ScenePath);
            EditorSceneManager.SaveScene(scene,ScenePath);
            var match=UnityEngine.Object.FindAnyObjectByType<MatchController>();
            var root=new GameObject("AI Opponent");root.SetActive(false);
            var ai=root.AddComponent<AiOpponent>();
            ai.match=match;
            ai.settings=Settings("Normal",5,2,2,.6123724f);
            Settings("Easy",8,3,3,.8f);
            Settings("Hard",1,.4f,.4f,.5f);
            var input=UnityEngine.Object.FindAnyObjectByType<AimInput>();
            input.ai=ai;
            var hud=UnityEngine.Object.FindAnyObjectByType<MatchHud>();hud.ai=ai;
            root.SetActive(true);
            EditorUtility.SetDirty(input);EditorUtility.SetDirty(hud);
            AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene,ScenePath);
            Debug.Log("MilitaryAI ready: player 1 versus normal AI, random terrain.");
        }
        static AiSettings Settings(string difficulty,float yaw,float pitch,float power,float decay)
        {
            string path="Assets/Fortress/Data/Ai"+difficulty+".asset";
            var settings=AssetDatabase.LoadAssetAtPath<AiSettings>(path);
            if(settings!=null) return settings;
            settings=ScriptableObject.CreateInstance<AiSettings>();settings.name="AI "+difficulty;
            settings.yawSigmaDegrees=yaw;settings.pitchSigmaDegrees=pitch;settings.powerSigma=power;settings.noiseDecay=decay;
            AssetDatabase.CreateAsset(settings,path);return settings;
        }
        [MenuItem("Fortress/Play vs AI")]
        public static void Play()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode) return;
            if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            if(!System.IO.File.Exists(ScenePath)) Create();
            else EditorSceneManager.OpenScene(ScenePath);
            EditorApplication.ExecuteMenuItem("Window/General/Game");EditorApplication.EnterPlaymode();
        }
    }
}
