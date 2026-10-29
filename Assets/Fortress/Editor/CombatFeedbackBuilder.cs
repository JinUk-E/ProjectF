using System;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Fortress.Editor
{
    public static class CombatFeedbackBuilder
    {
        public const string SettingsPath = "Assets/Fortress/Data/CombatFeedbackSettings.asset";
        const string AudioRoot = "Assets/Fortress/Art/Atmosphere/";
        [MenuItem("Fortress/Upgrade Combat Feedback")]
        public static void Upgrade()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play first.");
            var settings = AssetDatabase.LoadAssetAtPath<CombatFeedbackSettings>(SettingsPath);
            if (settings == null) { settings = ScriptableObject.CreateInstance<CombatFeedbackSettings>(); AssetDatabase.CreateAsset(settings, SettingsPath); }
            Undo.RecordObject(settings, "Combat feedback settings");
            var clips = new[] { Sound("HE", 1.0f, 52, .16f), Sound("Drill", 1.3f, 34, .07f), Sound("Shatter", .85f, 95, .5f), Sound("Cluster", .48f, 125, .32f) };
            var colors = new[] { new Color(1,.58f,.12f), new Color(.85f,.65f,.32f), new Color(.8f,.9f,1), new Color(1,.77f,.25f) };
            var sizes = new[] { 1f, .8f, 1.25f, .6f };
            var serialized = new SerializedObject(settings);
            var impacts = serialized.FindProperty("impacts"); impacts.arraySize = 4;
            for (int i=0;i<4;i++)
            {
                var item=impacts.GetArrayElementAtIndex(i);
                item.FindPropertyRelative("purpose").enumValueIndex=i;
                item.FindPropertyRelative("clip").objectReferenceValue=clips[i];
                item.FindPropertyRelative("color").colorValue=colors[i];
                item.FindPropertyRelative("sizeMultiplier").floatValue=sizes[i];
                item.FindPropertyRelative("pitch").floatValue=1;
                item.FindPropertyRelative("volume").floatValue=i==3?.42f:.7f;
            }
            serialized.ApplyModifiedProperties(); EditorUtility.SetDirty(settings);
            var font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(CommandUiBuilder.FontPath);
            foreach (var path in new[] { SessionMenu.LocalScene, SessionMenu.AiScene })
            {
                var scene=EditorSceneManager.OpenScene(path);
                var match=Object.FindFirstObjectByType<MatchController>();
                var hud=Object.FindFirstObjectByType<MatchHud>();
                var canvas=hud.GetComponentInParent<Canvas>();
                var existing=canvas.transform.Find("Combat feedback");
                if(existing!=null) Object.DestroyImmediate(existing.gameObject);
                var go=new GameObject("Combat feedback",typeof(RectTransform));
                Undo.RegisterCreatedObjectUndo(go,"Combat feedback overlay");
                var root=(RectTransform)go.transform;root.SetParent(canvas.transform,false);
                root.anchorMin=Vector2.zero;root.anchorMax=Vector2.one;root.offsetMin=root.offsetMax=Vector2.zero;
                var view=go.AddComponent<CombatFeedback>(); view.match=match;view.settings=settings;view.viewCamera=Camera.main;view.canvasRect=root;
                view.damageLabels=new TMP_Text[2];
                for(int i=0;i<2;i++) view.damageLabels[i]=Label(root,"Damage "+i,font,36,new Vector2(300,100));
                view.hitLabel=Label(root,"Hit confirmation",font,30,new Vector2(650,65));
                var rect=view.hitLabel.rectTransform;rect.anchorMin=rect.anchorMax=new Vector2(.5f,.73f);rect.anchoredPosition=Vector2.zero;
                var presentation=Object.FindFirstObjectByType<ShotPresentation>();
                Undo.RecordObject(presentation,"Ammo impact styles");presentation.feedbackSettings=settings;EditorUtility.SetDirty(presentation);
                EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            }
            AssetDatabase.SaveAssets();EditorSceneManager.OpenScene(SessionMenu.MainScene);
        }
        static TMP_Text Label(Transform parent,string name,TMP_FontAsset font,int size,Vector2 dimensions)
        {
            var go=new GameObject(name,typeof(RectTransform));var rect=(RectTransform)go.transform;rect.SetParent(parent,false);
            rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(.5f,.5f);rect.sizeDelta=dimensions;
            var label=go.AddComponent<TextMeshProUGUI>();label.font=font;label.fontSize=size;label.fontStyle=FontStyles.Bold;
            label.alignment=TextAlignmentOptions.Center;label.color=Color.white;label.raycastTarget=false;label.text="";
            return label;
        }
        static AudioClip Sound(string name,float seconds,float frequency,float brightness)
        {
            string path=AudioRoot+"Feedback"+name+".wav";
            if (!File.Exists(path))
            {
                // Original synthesized transients; no external samples or redistribution dependency.
                const int rate=22050;int count=(int)(seconds*rate);var random=new System.Random(763+name.Length);float low=0;
                using(var writer=new BinaryWriter(File.Create(path)))
                {
                    writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));writer.Write(36+count*2);
                    writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));writer.Write(16);writer.Write((short)1);writer.Write((short)1);
                    writer.Write(rate);writer.Write(rate*2);writer.Write((short)2);writer.Write((short)16);writer.Write(System.Text.Encoding.ASCII.GetBytes("data"));writer.Write(count*2);
                    for(int i=0;i<count;i++)
                    {
                        float t=(float)i/rate,noise=(float)random.NextDouble()*2-1;low=Mathf.Lerp(low,noise,.1f);
                        float body=Mathf.Sin(2*Mathf.PI*frequency*t*(1-.14f*t))*Mathf.Exp(-t*8);
                        float value=(body*.65f+low*.9f+noise*brightness*Mathf.Exp(-t*12))*Mathf.Exp(-t*4);
                        value*=Mathf.Min(1,t*700)*Mathf.Clamp01((seconds-t)*30);writer.Write((short)(Mathf.Clamp(value,-.85f,.85f)*32767));
                    }
                }
            }
            AssetDatabase.ImportAsset(path);return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        }
    }
}
