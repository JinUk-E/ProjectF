using System;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;
namespace Fortress.Editor
{
    public static class MatchMetricsBuilder
    {
        [MenuItem("Fortress/Upgrade Match Statistics")]
        public static void Upgrade()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play first.");
            foreach(var path in new[]{SessionMenu.LocalScene,SessionMenu.AiScene})
            {
                var scene=EditorSceneManager.OpenScene(path);
                var match=Object.FindFirstObjectByType<MatchController>();
                var stats=match.GetComponent<MatchStatistics>();if(stats==null)stats=Undo.AddComponent<MatchStatistics>(match.gameObject);
                Undo.RecordObject(stats,"Record local match statistics");stats.match=match;EditorUtility.SetDirty(stats);
                var hud=Object.FindFirstObjectByType<MatchHud>();
                var panel=(RectTransform)hud.resultPanel.transform;Undo.RecordObject(panel,"Make room for statistics");
                panel.sizeDelta=new Vector2(620,550);panel.anchoredPosition=new Vector2(650,-265);
                Move(panel.Find("Restart"),395);Move(panel.Find("Return to command"),465);
                var child=panel.Find("Match statistics");
                if(child==null)
                {
                    var go=new GameObject("Match statistics",typeof(RectTransform));Undo.RegisterCreatedObjectUndo(go,"Match statistics label");
                    go.transform.SetParent(panel,false);child=go.transform;
                }
                var rect=(RectTransform)child;rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0,1);
                rect.anchoredPosition=new Vector2(30,-258);rect.sizeDelta=new Vector2(560,112);
                var label=child.GetComponent<TextMeshProUGUI>();if(label==null)label=Undo.AddComponent<TextMeshProUGUI>(child.gameObject);
                label.font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(CommandUiBuilder.FontPath);label.fontSize=20;
                label.color=new Color(.8f,.86f,.91f);label.alignment=TextAlignmentOptions.Center;label.raycastTarget=false;label.text="";
                var view=child.GetComponent<MatchMetricsHud>();if(view==null)view=Undo.AddComponent<MatchMetricsHud>(child.gameObject);
                view.statistics=stats;view.summary=label;EditorUtility.SetDirty(view);
                EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            }
            AssetDatabase.SaveAssets();EditorSceneManager.OpenScene(SessionMenu.MainScene);
        }
        static void Move(Transform target,float top)
        {
            if(target==null)throw new InvalidOperationException("Missing result action");
            var rect=(RectTransform)target;Undo.RecordObject(rect,"Move result action");rect.anchoredPosition=new Vector2(rect.anchoredPosition.x,-top);
        }
    }
}
