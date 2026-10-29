using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.TextCore.LowLevel;
using Object=UnityEngine.Object;
namespace Fortress.Editor
{
    public static class CommandUiBuilder
    {
        public const string FontPath="Assets/Fortress/Art/UI/Korean UI.asset";
        static TMP_FontAsset font;
        static readonly Color Ink=new Color(.035f,.045f,.055f),Panel=new Color(.065f,.08f,.095f,.96f),Line=new Color(.19f,.23f,.27f),Text=new Color(.94f,.95f,.96f),Muted=new Color(.56f,.63f,.69f),Accent=new Color(1,.49f,.19f);
        [MenuItem("Fortress/Upgrade Korean Command UI")]
        public static void Upgrade()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play first.");
            PrepareFont();
            MenuFlowBuilder.Upgrade();
            Main();
            foreach(var path in new[]{SessionMenu.LocalScene,SessionMenu.AiScene})Battle(path);
            EditorSceneManager.OpenScene(SessionMenu.MainScene);AssetDatabase.SaveAssets();
        }
        static void PrepareFont()
        {
            Directory.CreateDirectory("Assets/Fortress/Art/UI");AssetDatabase.Refresh();
            font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            if(font==null)
            {
                var source=AssetDatabase.LoadAssetAtPath<Font>("Assets/ThirdParty/NanumGothic/NanumGothic-Regular.ttf");
                font=TMP_FontAsset.CreateFontAsset(source,64,8,GlyphRenderMode.SDFAA,2048,2048,AtlasPopulationMode.Dynamic,true);
                font.name="Korean UI";font.isMultiAtlasTexturesEnabled=true;AssetDatabase.CreateAsset(font,FontPath);
                AssetDatabase.AddObjectToAsset(font.material,font);
            }
            var chars=new HashSet<char>();for(int i=32;i<127;i++)chars.Add((char)i);
            foreach(var file in Directory.GetFiles("Assets/Fortress","*.cs",SearchOption.AllDirectories))foreach(var c in File.ReadAllText(file))if(c>127 && !char.IsControl(c))chars.Add(c);
            font.TryAddCharacters(new string(new List<char>(chars).ToArray()),out string missing);
            foreach(var texture in font.atlasTextures)if(texture!=null && !AssetDatabase.Contains(texture))AssetDatabase.AddObjectToAsset(texture,font);
            EditorUtility.SetDirty(font);AssetDatabase.SaveAssets();
        }
        static RectTransform Root(Transform parent,string name)
        {
            var go=new GameObject(name,typeof(RectTransform));go.transform.SetParent(parent,false);
            var rect=(RectTransform)go.transform;rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(.5f,.5f);rect.sizeDelta=new Vector2(1920,1080);go.AddComponent<MenuDesignViewport>();return rect;
        }
        static void Main()
        {
            var scene=EditorSceneManager.OpenScene(SessionMenu.MainScene);var menu=Object.FindFirstObjectByType<SessionMenu>();
            var backdrop=menu.transform.Find("Backdrop").GetComponent<Image>();backdrop.color=Color.clear;
            var root=menu.transform.Find("Command composition");foreach(Transform child in new List<Transform>(Children(root)))Object.DestroyImmediate(child.gameObject);
            var pickerGo=new GameObject("Unit selection");pickerGo.transform.SetParent(root,false);pickerGo.SetActive(false);
            var picker=pickerGo.AddComponent<LoadoutMenu>();picker.roles=Roles();picker.selectedColor=Accent;picker.idleColor=Line;
            picker.tuning=AssetDatabase.LoadAssetAtPath<GameTuning>("Assets/Fortress/Data/MatchTuning.asset");
            Box(root,"Top shade",0,0,1920,240,new Color(.025f,.035f,.045f,.85f));
            Box(root,"Bottom shade",0,786,1920,294,new Color(.025f,.035f,.045f,.85f));
            Label(root,"Brand",72,50,900,64,"프로젝트 F",48,Text);
            Label(root,"Subtitle",75,125,1000,40,"전술 포격전  /  출격 준비",23,Muted);
            Box(root,"Brand accent",73,206,58,4,Accent);
            Label(root,"Hangar number",146,193,650,32,"제 01 격납고     ·     전차를 선택하세요",18,Muted);
            Label(root,"Preview note",78,733,1050,36,"고저차를 읽고, 엄폐를 무너뜨리고, 한 발로 전황을 바꾸세요.",21,Muted);
            var detail=Box(root,"Deployment",1390,56,450,930,Panel);
            Box(detail,"Accent",0,0,450,4,Accent);
            Label(detail,"Brief",32,32,386,28,"출격 브리핑",19,Accent);
            picker.playerLabel=Label(detail,"Role",30,88,390,70,"",42,Text);
            picker.roleDescription=Label(detail,"Description",32,180,386,140,"",25,Muted);
            Box(detail,"Rule",32,340,386,1,Line);
            Label(detail,"Armament",32,368,386,30,"탑재 무장",20,Muted);
            picker.ammunitionLabel=Label(detail,"Ammo",32,416,386,100,"",25,Text);
            var enemy=Button(detail,"Opponent",32,545,386,100,"",picker.NextOpponent,false);
            picker.opponentLabel=enemy.GetComponentInChildren<TMP_Text>();picker.opponentLabel.fontSize=20;
            menu.primaryButton=Button(detail,"Deploy",32,680,386,86,"AI 대전 출격   >",menu.StartAi,true);
            Button(detail,"Local duel",32,784,386,68,"로컬 2인 대전",menu.StartLocal,false);
            Label(detail,"Offline note",32,872,386,30,"같은 PC에서 번갈아 조작합니다",17,Muted);
            int roleCount=picker.roles.Length;float cardWidth=(1232-18*(roleCount-1))/roleCount;
            picker.playerCards=new Button[roleCount];picker.cardBorders=new Image[roleCount];picker.selectionMarkers=new GameObject[roleCount];
            for(int i=0;i<picker.roles.Length;i++)
            {
                float x=76+i*(cardWidth+18);var role=picker.roles[i];var card=Box(root,"Tank card "+i,x,804,cardWidth,180,Line);picker.cardBorders[i]=card.GetComponent<Image>();
                var face=Box(card,"Choose tank",2,2,cardWidth-4,176,Panel);face.GetComponent<Image>().raycastTarget=true;
                var button=face.gameObject.AddComponent<Button>();button.targetGraphic=face.GetComponent<Image>();picker.playerCards[i]=button;UnityEventTools.AddIntPersistentListener(button.onClick,picker.SelectPlayer,i);
                var portrait=Box(face,"Portrait",10,6,cardWidth-24,112,Color.white).GetComponent<Image>();portrait.sprite=role.appearance.portrait;portrait.preserveAspect=true;
                Label(face,"Name",18,122,cardWidth-36,32,KoreanUi.RoleName(role),23,Text);
                var marker=Box(face,"Selected",cardWidth-54,144,32,4,Accent);picker.selectionMarkers[i]=marker.gameObject;
            }
            // The large model is the hero preview; keep the sprite binding for existing menu clients.
            var bound=Box(picker.transform,"Selected portrait binding",0,0,0,0,Color.clear).GetComponent<Image>();picker.selectedPortrait=bound;
            Button(root,"Manual",75,1013,170,42,"조작 안내",menu.OpenControls,false);
            Button(root,"Quit",265,1013,170,42,"게임 종료",menu.RequestQuit,false);
            menu.statusLabel=Label(root,"Status",1390,1016,450,38,"출격 준비 완료",18,Muted);
            pickerGo.SetActive(true);Hangar(picker);
            LocalizeOverlays(menu);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        }
        static IEnumerable<Transform> Children(Transform root) {for(int i=0;i<root.childCount;i++)yield return root.GetChild(i);}
        static TankLoadout[] Roles() {var names=new[]{"Main","Siege","Excavator","Support","Heavy"};var result=new TankLoadout[names.Length];for(int i=0;i<names.Length;i++)result[i]=AssetDatabase.LoadAssetAtPath<TankLoadout>("Assets/Fortress/Data/Role_"+names[i]+".asset");return result;}
        static void Hangar(LoadoutMenu picker)
        {
            var camera=Camera.main;camera.backgroundColor=Ink;camera.rect=new Rect(0,0,1,1);camera.fieldOfView=34;
            camera.transform.position=new Vector3(9,5.2f,12);camera.transform.LookAt(new Vector3(-1.8f,.2f,0));
            var root=new GameObject("Hangar showroom");var preview=root.AddComponent<HangarTankPreview>();preview.models=new GameObject[picker.roles.Length];
            for(int i=0;i<picker.roles.Length;i++)
            {
                var tank=new GameObject("Showroom "+i);tank.transform.SetParent(root.transform,false);preview.models[i]=tank;
                foreach(var piece in picker.roles[i].appearance.pieces)
                {
                    var go=new GameObject(piece.mesh.name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(tank.transform,false);go.transform.localPosition=piece.mount==0?Vector3.zero:new Vector3(0,1.8f,0);
                    go.GetComponent<MeshFilter>().sharedMesh=piece.mesh;go.GetComponent<MeshRenderer>().sharedMaterials=piece.playerMaterials;
                }
                tank.SetActive(i==0);
            }
            var floor=Material("Hangar floor",new Color(.045f,.06f,.075f),.5f);var frame=Material("Hangar frame",new Color(.04f,.055f,.07f),.25f);var lamp=Material("Hangar light",new Color(.4f,.65f,.8f),.1f,true);
            Cube(root.transform,"Platform",new Vector3(0,-.18f,0),new Vector3(7,.35f,7),frame);
            Cube(root.transform,"Floor",new Vector3(0,-.4f,0),new Vector3(60,.2f,60),floor);
            for(int i=-3;i<=3;i++)
            {
                Cube(root.transform,"Floor seam",new Vector3(i*4,-.29f,0),new Vector3(.025f,.015f,40),frame);
                Cube(root.transform,"Floor seam",new Vector3(0,-.29f,i*4),new Vector3(40,.015f,.025f),frame);
                Cube(root.transform,"Structural pillar",new Vector3(i*5,4,-9),new Vector3(.3f,9,.5f),frame);
            }
            Cube(root.transform,"Back wall",new Vector3(0,4,-11),new Vector3(50,10,.3f),floor);
            for(int i=-2;i<=2;i++)Cube(root.transform,"Light strip",new Vector3(i*6,5,-10.7f),new Vector3(2,.08f,.04f),lamp);
            Cube(root.transform,"Platform light",new Vector3(0,.015f,3.4f),new Vector3(6,.025f,.035f),Material("Amber light",Accent,.1f,true));
            var key=new GameObject("Showroom key",typeof(Light));key.transform.SetParent(root.transform);key.transform.eulerAngles=new Vector3(45,-35,0);key.GetComponent<Light>().type=LightType.Directional;key.GetComponent<Light>().intensity=2.2f;key.GetComponent<Light>().shadows=LightShadows.Soft;
            var fill=new GameObject("Showroom fill",typeof(Light));fill.transform.SetParent(root.transform);fill.transform.position=new Vector3(-4,5,2);var l=fill.GetComponent<Light>();l.type=LightType.Point;l.range=20;l.intensity=20;l.color=new Color(.5f,.68f,1);
            RenderSettings.ambientLight=new Color(.3f,.34f,.4f);RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;RenderSettings.fog=false;
        }
        static Material Material(string name,Color color,float metal,bool emission=false)
        {
            string path="Assets/Fortress/Art/UI/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(m==null){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}m.color=color;m.SetFloat("_Metallic",metal);m.SetFloat("_Smoothness",.45f);if(emission){m.EnableKeyword("_EMISSION");m.SetColor("_EmissionColor",color*3);}EditorUtility.SetDirty(m);return m;
        }
        static void Cube(Transform parent,string name,Vector3 position,Vector3 scale,Material material)
        {var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(parent,false);go.transform.localPosition=position;go.transform.localScale=scale;go.GetComponent<Renderer>().sharedMaterial=material;Object.DestroyImmediate(go.GetComponent<Collider>());}
        static void Battle(string path)
        {
            var scene=EditorSceneManager.OpenScene(path);var hud=Object.FindFirstObjectByType<MatchHud>();var menu=Object.FindFirstObjectByType<SessionMenu>();
            hud.gameObject.SetActive(false);foreach(var child in new List<Transform>(Children(hud.transform)))Object.DestroyImmediate(child.gameObject);
            var style=hud.style;style.panel=Panel;style.text=Text;style.muted=Muted;style.accent=Accent;EditorUtility.SetDirty(style);
            var root=Root(hud.transform,"Combat composition");hud.healthLabels=new TMP_Text[2];hud.healthFills=new Image[2];hud.activeIndicators=new Image[2];
            for(int i=0;i<2;i++)
            {
                var card=Box(root,"Unit "+i,i==0?44:1496,36,380,112,Panel);
                hud.activeIndicators[i]=Box(card,"Active",0,0,4,112,Accent).GetComponent<Image>();
                Label(card,"Callsign",20,12,340,24,i==0?"01  /  아군 전차":"02  /  상대 전차",17,Muted);
                hud.healthLabels[i]=Label(card,"Health",20,40,340,34,"",25,Text);
                var track=Box(card,"Hull integrity",20,90,340,5,Line);hud.healthFills[i]=Fill(track,Accent);
            }
            var status=Box(root,"Turn and wind",722,36,476,112,Panel);
            hud.turnLabel=Label(status,"Turn",16,9,444,36,"",25,Text);hud.turnLabel.alignment=TextAlignmentOptions.Center;
            hud.windLabel=Label(status,"Wind",64,54,390,26,"",18,Muted);
            hud.windDetailLabel=Label(status,"Wind detail",16,84,444,22,"",16,Muted);hud.windDetailLabel.alignment=TextAlignmentOptions.Center;
            hud.windArrow=Label(status,"Wind arrow",24,48,30,32,">",27,Accent).rectTransform;hud.windArrow.pivot=new Vector2(.5f,.5f);hud.windArrow.anchoredPosition+=new Vector2(15,-16);hud.windCamera=Camera.main;
            var bar=Box(root,"Fire control",44,862,1832,176,Panel);Box(bar,"Accent",0,0,1832,2,Line);
            var ammo=bar.gameObject.AddComponent<AmmoHud>();ammo.shots=hud.match.shots;
            ammo.roleLabel=Label(bar,"Weapon label",24,16,450,25,"",18,Muted);
            ammo.basicButton=Button(bar,"Basic ammo",24,54,210,66,"",null,false);ammo.basicLabel=ammo.basicButton.GetComponentInChildren<TMP_Text>();ammo.basicLabel.fontSize=20;
            ammo.specialButton=Button(bar,"Special ammo",246,54,260,66,"",null,false);ammo.specialLabel=ammo.specialButton.GetComponentInChildren<TMP_Text>();ammo.specialLabel.fontSize=20;
            Label(bar,"Ammo hotkeys",24,135,480,25,"1 · 2  탄종 선택",16,Muted);
            var controls=bar.gameObject.AddComponent<CombatPanelControls>();controls.shots=hud.match.shots;
            Divider(bar,538);Label(bar,"Angle title",568,18,260,25,"포신 각도   W / S",18,Muted);
            hud.elevationLabel=Label(bar,"Angle",568,56,200,68,"",45,Text);hud.headingLabel=Label(bar,"Heading",568,137,280,24,"",16,Muted);hud.aimLabel=hud.headingLabel;
            var down=Button(bar,"Angle down",790,59,42,50,"−",controls.ElevationDown,false);var up=Button(bar,"Angle up",838,59,42,50,"+",controls.ElevationUp,false);
            Divider(bar,906);Label(bar,"Power title",936,18,350,25,"발사 위력   Q / E",18,Muted);
            hud.powerLabel=Label(bar,"Power",936,51,155,60,"",40,Text);
            var pd=Button(bar,"Power down",1170,59,42,50,"−",controls.PowerDown,false);var pu=Button(bar,"Power up",1218,59,42,50,"+",controls.PowerUp,false);
            hud.powerFill=Fill(Box(bar,"Power track",936,135,330,7,Line),Accent);
            Divider(bar,1300);var fire=Button(bar,"Fire",1330,36,474,84,"발사    SPACE",controls.Fire,true);
            controls.buttons=new[]{down,up,pd,pu,fire};hud.stateLabel=Label(bar,"Readiness",1330,135,474,28,"",18,Muted);
            hud.movementLabel=Label(root,"Movement",50,812,660,30,"",19,Text);
            Label(root,"Camera controls",945,813,930,30,"우클릭 드래그  시점 회전   ·   휠  확대/축소   ·   F  내 전차",18,Muted).alignment=TextAlignmentOptions.Right;
            var notice=Box(root,"Turn notification",650,214,620,54,Panel);hud.notificationGroup=notice.gameObject.AddComponent<CanvasGroup>();hud.notificationGroup.blocksRaycasts=false;hud.notificationGroup.alpha=0;
            hud.notificationLabel=Label(notice,"Notice",16,8,588,38,"",23,Text);hud.notificationLabel.alignment=TextAlignmentOptions.Center;
            var result=Box(root,"Result",650,310,620,420,Panel);hud.resultPanel=result.gameObject;Box(result,"Result accent",0,0,620,4,Accent);
            Label(result,"Debrief",30,27,560,36,"전투 결과",22,Muted).alignment=TextAlignmentOptions.Center;
            hud.resultLabel=Label(result,"Outcome",30,90,560,160,"",34,Text);hud.resultLabel.alignment=TextAlignmentOptions.Center;
            hud.restartButton=Button(result,"Restart",50,278,520,52,"다시 출격",null,true);
            Button(result,"Return to command",50,344,520,52,"메인화면",menu.RequestMainMenu,false);result.gameObject.SetActive(false);
            menu.combatHudGroup=hud.GetComponent<CanvasGroup>();hud.gameObject.SetActive(true);
            LocalizeOverlays(menu);
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        }
        static void Divider(Transform root,float x) {Box(root,"Divider",x,24,1,128,Line);}
        static Image Fill(Transform parent,Color color)
        {var r=Box(parent,"Fill",0,0,0,0,color);r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;var image=r.GetComponent<Image>();image.sprite=AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");image.type=Image.Type.Filled;image.fillMethod=Image.FillMethod.Horizontal;return image;}
        static void LocalizeOverlays(SessionMenu menu)
        {
            var translations=new Dictionary<string,string>{
                {"ESC  /  MENU","메뉴  ESC"},{"ENGAGEMENT SUSPENDED","작전 일시정지"},{"PAUSED","잠시 멈춤"},{"Your position and current shot are held.","현재 위치와 진행 중인 포격이 유지됩니다."},
                {"RESUME ENGAGEMENT","계속하기"},{"RETURN TO MAIN MENU","메인화면으로"},{"EXIT GAME","게임 종료"},{"ESC  /  RESUME","ESC  계속하기"},{"FIELD MANUAL","조작 안내"},{"BACK TO COMMAND","돌아가기"},{"LEAVE ENGAGEMENT?","전투에서 나갈까요?"},{"CANCEL","취소"},{"CONFIRM","확인"}};
            foreach(var text in menu.GetComponentsInChildren<TMP_Text>(true))
            {
                text.font=font;
                if(translations.TryGetValue(text.text,out string translated))text.text=translated;
                if(text.text.StartsWith("ARROW KEYS"))text.text="방향키     내 턴에 전차 이동\nA / D       포탑 좌우 조준\nW / S       포신 각도 조절\nQ / E       발사 위력 조절\n1 / 2        탄종 선택     SPACE  발사\n우클릭 드래그   카메라 회전\n마우스 휠   확대 / 축소     F  내 전차 보기\nESC          일시정지 / 계속하기\n\n언덕과 잔해를 엄폐로 활용하고 물을 피하세요.";

            }
            foreach(var button in menu.GetComponentsInChildren<Button>(true))StyleButton(button);
            if(!menu.IsMainMenu)
            {
                var entry=menu.transform.Find("ESC  /  MENU") as RectTransform;
                if(entry!=null){entry.anchoredPosition=new Vector2(-44,-168);entry.sizeDelta=new Vector2(180,44);}
            }
        }
        static RectTransform Box(Transform parent,string name,float x,float y,float w,float h,Color color)
        {var go=new GameObject(name,typeof(RectTransform),typeof(Image));go.transform.SetParent(parent,false);var r=(RectTransform)go.transform;r.anchorMin=r.anchorMax=r.pivot=Vector2.up;r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);go.GetComponent<Image>().color=color;go.GetComponent<Image>().raycastTarget=false;return r;}
        static TMP_Text Label(Transform parent,string name,float x,float y,float w,float h,string value,float size,Color color)
        {var go=new GameObject(name,typeof(RectTransform),typeof(TextMeshProUGUI));go.transform.SetParent(parent,false);var t=go.GetComponent<TextMeshProUGUI>();t.rectTransform.anchorMin=t.rectTransform.anchorMax=t.rectTransform.pivot=Vector2.up;t.rectTransform.anchoredPosition=new Vector2(x,-y);t.rectTransform.sizeDelta=new Vector2(w,h);t.font=font;t.fontSize=size;t.color=color;t.text=value;t.raycastTarget=false;t.textWrappingMode=TextWrappingModes.Normal;t.alignment=TextAlignmentOptions.MidlineLeft;return t;}
        static Button Button(Transform parent,string name,float x,float y,float w,float h,string caption,UnityAction action,bool primary)
        {var r=Box(parent,name,x,y,w,h,primary?Accent:new Color(.105f,.13f,.155f));var image=r.GetComponent<Image>();image.raycastTarget=true;var button=r.gameObject.AddComponent<Button>();button.targetGraphic=image;StyleButton(button);if(action!=null)UnityEventTools.AddPersistentListener(button.onClick,action);var label=Label(r,"Caption",w<80?0:18,0,w<80?w:w-36,h,caption,23,primary?Ink:Text);if(w<80)label.alignment=TextAlignmentOptions.Center;return button;}
        static void StyleButton(Button button)
        {var colors=button.colors;colors.normalColor=Color.white;colors.highlightedColor=new Color(1.25f,1.25f,1.25f);colors.selectedColor=colors.highlightedColor;colors.pressedColor=new Color(.65f,.65f,.65f);colors.disabledColor=new Color(.4f,.4f,.4f,.6f);button.colors=colors;}
    }
}
