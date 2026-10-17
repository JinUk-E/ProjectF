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
        static TMP_FontAsset font;
        static readonly Color Ink = new Color(.045f, .055f, .045f, .94f);
        static readonly Color Text = new Color(.87f, .88f, .8f);
        static readonly Color Accent = new Color(.83f, .72f, .43f);

        public static void CreateBatch()
        {
            try { Create(); EditorApplication.Exit(0); }
            catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
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
        static void CreateHud(MatchController match)
        {
            var previous = UnityEngine.Object.FindFirstObjectByType<Canvas>();
            if (previous != null) UnityEngine.Object.DestroyImmediate(previous.gameObject);
            font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
            var canvas = new GameObject("Match HUD", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvas.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920,1080); scaler.matchWidthOrHeight = .5f;
            var hud = canvas.AddComponent<MatchHud>(); hud.match = match;
            var top = Panel("Command Bar",canvas.transform, new Vector2(0,1),new Vector2(1,1),new Vector2(0,1),Vector2.zero,new Vector2(0,132),Ink);
            Label("Title",top,new Vector2(26,-12),new Vector2(550,30),"FORTRESS  /  ARMORED DUEL",22,Accent);
            hud.turnLabel = Label("Turn",top,new Vector2(26,-49),new Vector2(550,40),"",30,Text);
            hud.healthLabels = new TMP_Text[2]; hud.healthFills = new Image[2];
            for(int i=0;i<2;i++)
            {
                float x=700+i*560;
                hud.healthLabels[i]=Label("Player "+(i+1),top,new Vector2(x,-16),new Vector2(400,38),"",28,Text);
                var track=Panel("Armor Bar",top,Vector2.up,Vector2.up,Vector2.up,new Vector2(x,-68),new Vector2(420,18),new Color(.16f,.18f,.15f));
                var fill=Panel("HP",track,Vector2.zero,Vector2.one,new Vector2(.5f,.5f),Vector2.zero,Vector2.zero,i==0?new Color(.44f,.59f,.32f):new Color(.72f,.56f,.33f));
                hud.healthFills[i]=fill.GetComponent<Image>();
                hud.healthFills[i].type=Image.Type.Filled; hud.healthFills[i].fillMethod=Image.FillMethod.Horizontal;
                // Filled Images require a sprite; the built-in UI sprite is shared, never mutated.
                hud.healthFills[i].sprite=AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            }
            var bottom=Panel("Fire Console",canvas.transform,Vector2.zero,Vector2.right,Vector2.zero,Vector2.zero,new Vector2(0,142),Ink);
            hud.aimLabel=Label("Aim",bottom,new Vector2(26,-12),new Vector2(1050,38),"",27,Text);
            hud.stateLabel=Label("State",bottom,new Vector2(26,-55),new Vector2(1050,30),"",23,Accent);
            Label("Controls",bottom,new Vector2(26,-97),new Vector2(1350,30),"A/D  DIRECTION     W/S  ELEVATION     Q/E  POWER     SPACE  FIRE     |     STANDARD SHELL  /  UNLIMITED",19,Text);
            hud.windLabel=Label("Wind",bottom,new Vector2(1250,-25),new Vector2(640,38),"",25,Text);
            var arrow=Label("Wind Direction",bottom,new Vector2(1190,-45),new Vector2(44,44),">",44,Accent).rectTransform;
            arrow.pivot=new Vector2(.5f,.5f); hud.windArrow=arrow;
            var result=Panel("Result",canvas.transform,new Vector2(.5f,.5f),new Vector2(.5f,.5f),new Vector2(.5f,.5f),Vector2.zero,new Vector2(620,340),Ink);
            hud.resultPanel=result.gameObject;
            hud.resultLabel=Label("Winner",result,new Vector2(30,-40),new Vector2(560,150),"",40,Text); hud.resultLabel.alignment=TextAlignmentOptions.Center;
            var buttonRect=Panel("Restart",result,Vector2.up,Vector2.up,Vector2.up,new Vector2(130,-220),new Vector2(360,64),new Color(.32f,.38f,.24f));
            hud.restartButton=buttonRect.gameObject.AddComponent<Button>(); hud.restartButton.targetGraphic=buttonRect.GetComponent<Image>();
            buttonRect.GetComponent<Image>().raycastTarget=true;
            var navigation=hud.restartButton.navigation; navigation.mode=Navigation.Mode.None; hud.restartButton.navigation=navigation;
            var restart=Label("Caption",buttonRect,new Vector2(0,-12),new Vector2(360,40),"NEW MATCH",28,Text); restart.alignment=TextAlignmentOptions.Center;
            result.gameObject.SetActive(false);
            var events=new GameObject("UI Events",typeof(EventSystem),typeof(InputSystemUIInputModule));
            events.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
        }
        static RectTransform Panel(string name,Transform parent,Vector2 min,Vector2 max,Vector2 pivot,Vector2 pos,Vector2 size,Color color)
        {
            var obj=new GameObject(name,typeof(RectTransform),typeof(Image)); obj.transform.SetParent(parent,false);
            var rect=obj.GetComponent<RectTransform>(); rect.anchorMin=min; rect.anchorMax=max; rect.pivot=pivot; rect.anchoredPosition=pos; rect.sizeDelta=size;
            var image=obj.GetComponent<Image>(); image.color=color; image.raycastTarget=false; return rect;
        }
        static TextMeshProUGUI Label(string name,Transform parent,Vector2 pos,Vector2 size,string text,float fontSize,Color color)
        {
            var obj=new GameObject(name,typeof(RectTransform),typeof(TextMeshProUGUI)); obj.transform.SetParent(parent,false);
            var rect=obj.GetComponent<RectTransform>(); rect.anchorMin=Vector2.up; rect.anchorMax=Vector2.up; rect.pivot=Vector2.up; rect.anchoredPosition=pos; rect.sizeDelta=size;
            var label=obj.GetComponent<TextMeshProUGUI>(); label.font=font; label.fontSize=fontSize; label.text=text; label.color=color; label.raycastTarget=false; return label;
        }
    }
}
