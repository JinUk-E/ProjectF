using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
namespace Fortress.Editor
{
    public static class OrbitCameraBuilder
    {
        public static void Upgrade()
        {
            if(EditorApplication.isPlaying) throw new System.InvalidOperationException("Stop play mode before upgrading the camera.");
            var settings=AssetDatabase.LoadAssetAtPath<OrbitCameraSettings>("Assets/Fortress/Data/OrbitCameraSettings.asset");
            if(settings==null)
            {
                settings=ScriptableObject.CreateInstance<OrbitCameraSettings>();
                AssetDatabase.CreateAsset(settings,"Assets/Fortress/Data/OrbitCameraSettings.asset");
            }
            foreach(var path in new[]{MatchSceneBuilder.ScenePath,AiMatchBuilder.ScenePath})
            {
                var scene=EditorSceneManager.OpenScene(path);
                var camera=Camera.main;
                var orbit=camera.GetComponent<BattlefieldOrbitCamera>();
                if(orbit==null) orbit=Undo.AddComponent<BattlefieldOrbitCamera>(camera.gameObject);
                Undo.RecordObject(orbit,"Configure battlefield camera");
                orbit.match=Object.FindAnyObjectByType<MatchController>();orbit.settings=settings;
                orbit.initialTarget=new Vector3(0,3,0);
                var hud=Object.FindAnyObjectByType<MatchHud>();
                var existing=hud.transform.Find("Camera controls");
                if(existing==null)
                {
                    var label=new GameObject("Camera controls",typeof(RectTransform),typeof(TextMeshProUGUI));
                    label.transform.SetParent(hud.transform,false);
                    var rect=label.GetComponent<RectTransform>();rect.anchorMin=rect.anchorMax=new Vector2(0,0);rect.pivot=new Vector2(0,0);
                    rect.anchoredPosition=new Vector2(36,112);rect.sizeDelta=new Vector2(560,42);
                    var text=label.GetComponent<TextMeshProUGUI>();text.font=hud.stateLabel.font;text.fontSize=17;text.color=hud.style.text;
                    text.text="RMB DRAG  ORBIT   /   WHEEL  ZOOM\nF  FOCUS CURRENT TANK";text.raycastTarget=false;
                }
                EditorUtility.SetDirty(orbit);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("Manual orbit camera installed in MilitaryMatch and MilitaryAI.");
        }
    }
}
