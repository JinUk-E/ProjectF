using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using TMPro;
namespace Fortress.Editor
{
    public static class MountainMatchBuilder
    {
        public static void Upgrade()
        {
            if(EditorApplication.isPlaying) throw new System.InvalidOperationException("Stop play before upgrading.");
            const string root="Assets/Fortress/Data/";
            var profile=AssetDatabase.LoadAssetAtPath<MountainTerrainSettings>(root+"KoreanMountainStyle.asset");
            if(profile==null) { profile=ScriptableObject.CreateInstance<MountainTerrainSettings>();AssetDatabase.CreateAsset(profile,root+"KoreanMountainStyle.asset"); }
            var easy=AssetDatabase.LoadAssetAtPath<AiSettings>(root+"AiEasy.asset");
            Undo.RecordObject(easy,"Reduce AI accuracy");
            easy.yawSigmaDegrees=9;easy.pitchSigmaDegrees=4;easy.powerSigma=3.5f;easy.noiseDecay=.92f;easy.minimumNoiseScale=.75f;EditorUtility.SetDirty(easy);
            foreach(var path in new[]{MatchSceneBuilder.ScenePath,AiMatchBuilder.ScenePath})
            {
                var scene=EditorSceneManager.OpenScene(path);var match=Object.FindAnyObjectByType<MatchController>();
                var tuning=match.tuning;Undo.RecordObject(tuning,"Movement and mountain profile");
                tuning.movementEnabled=true;tuning.moveDistancePerTurn=12;tuning.mountainTerrain=profile;
                // Broad, shallow spawn terraces connect to the central valley.
                tuning.spawnPadBlend=14;EditorUtility.SetDirty(tuning);
                var terrain=match.terrainMap.terrain;
                var data=AssetDatabase.LoadAssetAtPath<TerrainData>(root+"MountainTerrain.asset");
                if(data==null)
                {
                    data=Object.Instantiate(terrain.terrainData);
                    float oldHeight=data.size.y;var heights=data.GetHeights(0,0,data.heightmapResolution,data.heightmapResolution);
                    var size=data.size;size.y=64;data.size=size;
                    for(int z=0;z<data.heightmapResolution;z++) for(int x=0;x<data.heightmapResolution;x++) heights[z,x]*=oldHeight/64;
                    data.SetHeights(0,0,heights);AssetDatabase.CreateAsset(data,root+"MountainTerrain.asset");
                }
                terrain.terrainData=data;
                var input=Object.FindAnyObjectByType<AimInput>();input.match=match;input.movementCamera=Camera.main.transform;
                var camera=Camera.main;camera.transform.position=new Vector3(-62,42,-12);camera.transform.LookAt(new Vector3(0,3,0));
                var ai=Object.FindAnyObjectByType<AiOpponent>();if(ai!=null) { ai.settings=easy;EditorUtility.SetDirty(ai); }
                var hud=Object.FindAnyObjectByType<MatchHud>();
                if(hud.movementLabel==null)
                {
                    var go=new GameObject("Movement budget",typeof(RectTransform),typeof(TextMeshProUGUI));go.transform.SetParent(hud.transform,false);
                    var rect=go.GetComponent<RectTransform>();rect.anchorMin=rect.anchorMax=rect.pivot=Vector2.zero;rect.anchoredPosition=new Vector2(36,167);rect.sizeDelta=new Vector2(560,30);
                    var text=go.GetComponent<TextMeshProUGUI>();text.font=hud.stateLabel.font;text.fontSize=18;text.color=hud.style.accent;text.raycastTarget=false;text.text="ARROW KEYS  MOVE  /  12m LEFT";hud.movementLabel=text;
                }
                EditorUtility.SetDirty(input);EditorUtility.SetDirty(terrain);EditorUtility.SetDirty(hud);
                EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            }
            AssetDatabase.SaveAssets();Debug.Log("Mountain terrain, movement and easier AI installed.");
        }
    }
}
