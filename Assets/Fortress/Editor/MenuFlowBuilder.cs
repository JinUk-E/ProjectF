using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Fortress.Editor
{
    public static class MenuFlowBuilder
    {
        static TMP_FontAsset font;
        static readonly Color Ink = new Color(.035f,.045f,.055f);
        static readonly Color Panel = new Color(.065f,.08f,.095f,.98f);
        static readonly Color Accent = new Color(1,.49f,.19f);
        static readonly Color Text = new Color(.94f,.95f,.96f);
        static readonly Color Muted = new Color(.56f,.63f,.69f);
        [MenuItem("Fortress/Upgrade Main Menu and Session Flow")]
        public static void Upgrade()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode) throw new System.InvalidOperationException("Stop Play mode before upgrading menus.");
            font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
            foreach(var path in new[]{SessionMenu.AiScene,SessionMenu.LocalScene})
            {
                var scene=EditorSceneManager.OpenScene(path);
                foreach(var old in Object.FindObjectsByType<SessionMenu>(FindObjectsInactive.Include,FindObjectsSortMode.None)) Object.DestroyImmediate(old.gameObject);
                CreateSession(Object.FindFirstObjectByType<MatchController>());
                EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            }
            CreateMain();
            var scenes=new List<EditorBuildSettingsScene>{new EditorBuildSettingsScene(SessionMenu.MainScene,true),new EditorBuildSettingsScene(SessionMenu.AiScene,true),new EditorBuildSettingsScene(SessionMenu.LocalScene,true)};
            foreach(var existing in EditorBuildSettings.scenes)
                if(existing.path!=SessionMenu.MainScene && existing.path!=SessionMenu.AiScene && existing.path!=SessionMenu.LocalScene) scenes.Add(existing);
            EditorBuildSettings.scenes=scenes.ToArray();AssetDatabase.SaveAssets();
        }
        [MenuItem("Fortress/Play From Main Menu")]
        public static void Play()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode) return;
            if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.OpenScene(SessionMenu.MainScene);
            EditorApplication.ExecuteMenuItem("Window/General/Game");EditorApplication.EnterPlaymode();
        }
        static SessionMenu Canvas(string name)
        {
            var go=new GameObject(name,typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster),typeof(CanvasGroup));
            go.SetActive(false);
            go.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;
            go.GetComponent<Canvas>().sortingOrder=100;
            var scaler=go.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution=new Vector2(1920,1080);scaler.matchWidthOrHeight=.5f;
            var menu=go.AddComponent<SessionMenu>();menu.navigationGroup=go.GetComponent<CanvasGroup>();
            if(Object.FindFirstObjectByType<EventSystem>()==null)
            {
                var events=new GameObject("Menu Events",typeof(EventSystem),typeof(InputSystemUIInputModule));
                events.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
            }
            return menu;
        }
        static void CreateMain()
        {
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var camera=new GameObject("Main Camera",typeof(Camera),typeof(AudioListener));camera.tag="MainCamera";
            camera.GetComponent<Camera>().clearFlags=CameraClearFlags.SolidColor;camera.GetComponent<Camera>().backgroundColor=Ink;
            var menu=Canvas("Command menu");
            Stretch(Box("Backdrop",menu.transform,0,0,1920,1080,Ink));
            var composition=new GameObject("Command composition",typeof(RectTransform));composition.transform.SetParent(menu.transform,false);
            var root=composition.GetComponent<RectTransform>();root.anchorMin=root.anchorMax=root.pivot=new Vector2(.5f,.5f);root.sizeDelta=new Vector2(1920,1080);
            composition.AddComponent<MenuDesignViewport>();
            var map=Box("Tactical map",root,980,120,820,790,new Color(.045f,.078f,.07f));
            for(int i=0;i<16;i++) Box("Grid vertical",map,40+i*48,35,1,720,new Color(.1f,.15f,.13f));
            for(int i=0;i<15;i++) Box("Grid horizontal",map,35,40+i*48,750,1,new Color(.1f,.15f,.13f));
            for(int ring=0;ring<5;ring++)
            {
                float radius=100+ring*47;
                for(int j=0;j<28;j++)
                {
                    float a=j*Mathf.PI*2/28,b=(j+1)*Mathf.PI*2/28;
                    Vector2 p=new Vector2(410+Mathf.Cos(a)*radius,365+Mathf.Sin(a)*radius*.73f);
                    Vector2 q=new Vector2(410+Mathf.Cos(b)*radius,365+Mathf.Sin(b)*radius*.73f);
                    Line(map,p,q,new Color(.24f,.33f,.24f),2);
                }
            }
            Line(map,new Vector2(135,550),new Vector2(385,400),Accent,3);
            Line(map,new Vector2(385,400),new Vector2(660,210),Accent,3);
            Box("Friendly marker",map,124,539,22,22,Accent);
            Box("Target marker",map,649,199,22,22,new Color(.72f,.39f,.27f));
            Label(map,48,40,700,35,"SECTOR 07  /  MOUNTAIN OPERATIONS",19,Muted);
            Label(map,48,690,700,60,"READ THE TERRAIN.\nMAKE EVERY ROUND COUNT.",23,Text);
            Label(root,120,100,800,32,"FIELD COMMAND  /  TACTICAL ARTILLERY",20,Accent);
            Label(root,112,172,850,120,"PROJECT F",96,Text);
            Label(root,120,312,760,72,"Take position. Break cover. Control the battlefield.",25,Muted);
            menu.primaryButton=Button(root,120,432,720,90,"DEPLOY  /  VS AI",menu.StartAi,true);
            Button(root,120,542,720,76,"LOCAL DUEL  /  2 PLAYERS",menu.StartLocal,false);
            Button(root,120,648,345,64,"FIELD MANUAL",()=>menu.ShowControls(true),false,menu,"OpenControls");
            Button(root,495,648,345,64,"EXIT GAME",menu.RequestQuit,false);
            Label(root,120,760,750,75,"OFFLINE ENGAGEMENTS\nAI opponent or two players sharing this PC.",20,Muted);
            menu.statusLabel=Label(root,120,934,1600,34,"COMMAND READY  /  PC PLAYTEST",18,Accent);
            AddControls(menu);AddConfirmation(menu);menu.gameObject.SetActive(true);
            EditorSceneManager.SaveScene(scene,SessionMenu.MainScene);
        }
        static void CreateSession(MatchController match)
        {
            var menu=Canvas("Session navigation");menu.match=match;
            var entry=Button(menu.transform,0,0,230,48,"ESC  /  MENU",menu.Pause,false);
            var er=entry.GetComponent<RectTransform>();er.anchorMin=er.anchorMax=new Vector2(1,1);er.pivot=new Vector2(1,1);er.anchoredPosition=new Vector2(-36,-158);
            var hud=Object.FindFirstObjectByType<MatchHud>();
            if(hud!=null && hud.resultPanel!=null)
            {
                menu.combatHudGroup=hud.GetComponent<CanvasGroup>();
                if(menu.combatHudGroup==null) menu.combatHudGroup=hud.gameObject.AddComponent<CanvasGroup>();
                // The return button is a child of the existing result panel and follows its visibility.
                var panel=hud.resultPanel.GetComponent<RectTransform>();panel.sizeDelta=new Vector2(580,420);
                var old=panel.Find("Return to command");if(old!=null) Object.DestroyImmediate(old.gameObject);
                Button(panel,140,330,300,54,"MAIN MENU",menu.RequestMainMenu,false).name="Return to command";
                EditorUtility.SetDirty(hud);
            }
            var overlay=Modal(menu.transform,"Paused");menu.pausePanel=overlay.gameObject;
            var card=Card(overlay,650,540);
            Label(card,44,38,560,35,"ENGAGEMENT SUSPENDED",18,Accent);
            Label(card,40,89,570,68,"PAUSED",54,Text);
            Label(card,44,167,560,40,"Your position and current shot are held.",21,Muted);
            menu.resumeButton=Button(card,44,230,562,60,"RESUME ENGAGEMENT",menu.Resume,true);
            Button(card,44,308,562,56,"RETURN TO MAIN MENU",menu.RequestMainMenu,false);
            Button(card,44,382,562,56,"EXIT GAME",menu.RequestQuit,false);
            Label(card,44,471,560,30,"ESC  /  RESUME",17,Muted);
            overlay.gameObject.SetActive(false);
            menu.statusLabel=Label(menu.transform,36,212,800,35,"",20,Accent);
            AddConfirmation(menu);menu.gameObject.SetActive(true);
        }
        static void AddControls(SessionMenu menu)
        {
            var overlay=Modal(menu.transform,"Field manual");menu.controlsPanel=overlay.gameObject;
            var card=Card(overlay,900,660);
            Label(card,46,35,800,60,"FIELD MANUAL",42,Text);
            Label(card,46,126,800,400,"ARROW KEYS     Move during your turn\nA / D                Traverse the turret\nW / S                Raise / lower the barrel\nQ / E                 Adjust firing power\nSPACE              Fire\nRMB DRAG       Orbit camera\nMOUSE WHEEL  Zoom      F  Focus tank\nESC                   Pause / resume\n\nUse hills and rubble as cover. Avoid the water.",25,Text);
            Button(card,46,560,808,60,"BACK TO COMMAND",()=>menu.ShowControls(false),true,menu,"CloseControls");
            overlay.gameObject.SetActive(false);
        }
        static void AddConfirmation(SessionMenu menu)
        {
            var overlay=Modal(menu.transform,"Confirm departure");menu.confirmationPanel=overlay.gameObject;
            var card=Card(overlay,720,350);
            menu.confirmationTitle=Label(card,40,44,640,60,"LEAVE ENGAGEMENT?",36,Text);
            menu.confirmationBody=Label(card,40,126,640,64,"",23,Muted);
            menu.cancelButton=Button(card,40,240,300,64,"CANCEL",menu.CancelConfirmation,true);
            Button(card,380,240,300,64,"CONFIRM",menu.ConfirmExit,false);
            overlay.gameObject.SetActive(false);
        }
        static RectTransform Modal(Transform root,string name)
        {
            var rect=Box(name,root,0,0,0,0,new Color(0,0,0,.78f));Stretch(rect);rect.GetComponent<Image>().raycastTarget=true;return rect;
        }
        static RectTransform Card(Transform parent,float w,float h)
        {
            var r=Box("Panel",parent,0,0,w,h,Panel);r.anchorMin=r.anchorMax=new Vector2(.5f,.5f);r.pivot=new Vector2(.5f,.5f);
            Box("Accent",r,0,0,w,4,Accent);return r;
        }
        static void Stretch(RectTransform rect) { rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero; }
        static RectTransform Box(string name,Transform parent,float x,float y,float w,float h,Color color)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(Image));go.transform.SetParent(parent,false);
            var r=go.GetComponent<RectTransform>();r.anchorMin=r.anchorMax=r.pivot=Vector2.up;r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);
            var image=go.GetComponent<Image>();image.color=color;image.raycastTarget=false;return r;
        }
        static TMP_Text Label(Transform parent,float x,float y,float w,float h,string text,float size,Color color)
        {
            var go=new GameObject(text.Length>24?text.Substring(0,24):text,typeof(RectTransform),typeof(TextMeshProUGUI));go.transform.SetParent(parent,false);
            var r=go.GetComponent<RectTransform>();r.anchorMin=r.anchorMax=r.pivot=Vector2.up;r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);
            var t=go.GetComponent<TextMeshProUGUI>();t.font=font;t.fontSize=size;t.text=text;t.color=color;t.raycastTarget=false;t.textWrappingMode=TextWrappingModes.Normal;return t;
        }
        static Button Button(Transform parent,float x,float y,float w,float h,string caption,UnityAction action,bool primary,SessionMenu target=null,string method=null)
        {
            var r=Box(caption,parent,x,y,w,h,primary?Accent:new Color(.105f,.13f,.155f));var image=r.GetComponent<Image>();image.raycastTarget=true;
            var b=r.gameObject.AddComponent<Button>();b.targetGraphic=image;
            var colors=b.colors;colors.highlightedColor=new Color(1.15f,1.15f,1.15f);colors.selectedColor=new Color(1.2f,1.2f,1.2f);colors.pressedColor=new Color(.7f,.7f,.7f);b.colors=colors;
            if(method!=null) UnityEventTools.AddVoidPersistentListener(b.onClick,(UnityAction)System.Delegate.CreateDelegate(typeof(UnityAction),target,method));
            else UnityEventTools.AddPersistentListener(b.onClick,action);
            var label=Label(r,22,0,w-44,h,caption,23,primary?Ink:Text);label.alignment=TextAlignmentOptions.MidlineLeft;
            return b;
        }
        static void Line(Transform parent,Vector2 a,Vector2 b,Color color,float width)
        {
            var delta=b-a;var r=Box("Contour",parent,a.x,a.y,delta.magnitude,width,color);
            r.localRotation=Quaternion.Euler(0,0,-Mathf.Atan2(delta.y,delta.x)*Mathf.Rad2Deg);
        }
    }
}
