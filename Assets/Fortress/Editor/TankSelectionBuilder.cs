using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Fortress.Editor
{
    /// <summary>Presentation-only upgrade; battle entry continues through SessionMenu.</summary>
    public static class TankSelectionBuilder
    {
        static TMP_FontAsset font;
        static readonly Color Accent=new Color(.72f,.78f,.51f),Text=new Color(.9f,.92f,.84f),Muted=new Color(.53f,.62f,.57f);
        [MenuItem("Fortress/Upgrade Tank Selection")]
        public static void Upgrade()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode) throw new System.InvalidOperationException("Stop Play mode first.");
            font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
            var scene=EditorSceneManager.OpenScene(SessionMenu.MainScene);
            var menu=Object.FindFirstObjectByType<SessionMenu>();
            var root=menu.transform.Find("Command composition");
            foreach(var name in new[]{"Tactical map","Unit selection"})
            { var old=root.Find(name);if(old!=null) Object.DestroyImmediate(old.gameObject); }
            var panel=Box(root,"Unit selection",980,120,820,790,new Color(.045f,.078f,.07f));
            panel.gameObject.SetActive(false);
            var picker=panel.gameObject.AddComponent<LoadoutMenu>();
            picker.roles=new[]{
                AssetDatabase.LoadAssetAtPath<TankLoadout>("Assets/Fortress/Data/Role_Main.asset"),
                AssetDatabase.LoadAssetAtPath<TankLoadout>("Assets/Fortress/Data/Role_Siege.asset"),
                AssetDatabase.LoadAssetAtPath<TankLoadout>("Assets/Fortress/Data/Role_Excavator.asset"),
                AssetDatabase.LoadAssetAtPath<TankLoadout>("Assets/Fortress/Data/Role_Support.asset"),AssetDatabase.LoadAssetAtPath<TankLoadout>("Assets/Fortress/Data/Role_Heavy.asset")};
            Label(panel,28,20,760,32,"01 / CHOOSE YOUR TANK",20,Accent);
            picker.playerLabel=Label(panel,28,62,760,48,"",36,Text);
            var portrait=Box(panel,"Selected tank portrait",28,116,764,274,Color.white).GetComponent<Image>();
            portrait.preserveAspect=true;picker.selectedPortrait=portrait;
            picker.roleDescription=Label(panel,28,402,764,50,"",22,Text);
            picker.ammunitionLabel=Label(panel,28,458,764,30,"",18,Accent);
            int count=picker.roles.Length;float width=(764-12*(count-1))/count;
            picker.playerCards=new Button[count];picker.cardBorders=new Image[count];picker.selectionMarkers=new GameObject[count];
            for(int i=0;i<count;i++)
            {
                var role=picker.roles[i];float x=28+i*(width+12);
                var border=Box(panel,"Tank card "+i,x,510,width,152,picker.idleColor);picker.cardBorders[i]=border.GetComponent<Image>();
                var card=Box(border,"Choose "+role.displayName,2,2,width-4,148,new Color(.075f,.115f,.095f));
                var image=card.GetComponent<Image>();image.raycastTarget=true;
                var button=card.gameObject.AddComponent<Button>();button.targetGraphic=image;picker.playerCards[i]=button;
                UnityEventTools.AddIntPersistentListener(button.onClick,picker.SelectPlayer,i);
                var thumbnail=Box(card,"Portrait",4,8,width-12,88,Color.white).GetComponent<Image>();thumbnail.preserveAspect=true;
                thumbnail.sprite=role.appearance!=null?role.appearance.portrait:null;thumbnail.enabled=thumbnail.sprite!=null;
                var caption=Label(card,6,95,width-16,25,role.displayName,17,Text);caption.alignment=TextAlignmentOptions.Center;
                var selected=Label(card,6,122,width-16,22,"SELECTED",14,Accent);selected.alignment=TextAlignmentOptions.Center;picker.selectionMarkers[i]=selected.gameObject;
            }
            var opponent=Box(panel,"Opponent selection",28,684,764,80,new Color(.11f,.16f,.13f));
            opponent.GetComponent<Image>().raycastTarget=true;
            var opponentButton=opponent.gameObject.AddComponent<Button>();opponentButton.targetGraphic=opponent.GetComponent<Image>();
            UnityEventTools.AddPersistentListener(opponentButton.onClick,picker.NextOpponent);
            picker.opponentLabel=Label(opponent,16,11,732,60,"",18,Muted);
            panel.gameObject.SetActive(true);
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        }
        static RectTransform Box(Transform parent,string name,float x,float y,float w,float h,Color color)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(Image));go.transform.SetParent(parent,false);
            var rect=go.GetComponent<RectTransform>();rect.anchorMin=rect.anchorMax=rect.pivot=Vector2.up;rect.anchoredPosition=new Vector2(x,-y);rect.sizeDelta=new Vector2(w,h);
            var image=go.GetComponent<Image>();image.color=color;image.raycastTarget=false;return rect;
        }
        static TMP_Text Label(Transform parent,float x,float y,float w,float h,string value,float size,Color color)
        {
            var go=new GameObject("Label",typeof(RectTransform),typeof(TextMeshProUGUI));go.transform.SetParent(parent,false);
            var text=go.GetComponent<TextMeshProUGUI>();text.rectTransform.anchorMin=text.rectTransform.anchorMax=text.rectTransform.pivot=Vector2.up;
            text.rectTransform.anchoredPosition=new Vector2(x,-y);text.rectTransform.sizeDelta=new Vector2(w,h);text.font=font;text.fontSize=size;
            text.color=color;text.text=value;text.raycastTarget=false;text.textWrappingMode=TextWrappingModes.Normal;return text;
        }
    }
}
