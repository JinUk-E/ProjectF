using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityProjectile;

namespace Fortress.Editor
{
    public static class FlatRangeBuilder
    {
        public const string ScenePath = "Assets/Fortress/Scenes/FlatRange.unity";
        const string Data = "Assets/Fortress/Data/";
        const string Prefabs = "Assets/Fortress/Prefabs/";

        public static void CreateBatch()
        {
            if (AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset") != null)
            { FinishBatch(); return; }
            AssetDatabase.importPackageCompleted += OnPackageImported;
            AssetDatabase.importPackageFailed += OnPackageFailed;
            var packages=Directory.GetDirectories("Library/PackageCache","com.unity.ugui@*");
            AssetDatabase.ImportPackage(Path.GetFullPath(Path.Combine(packages[0],"Package Resources/TMP Essential Resources.unitypackage")),false);
        }
        static void OnPackageImported(string name)
        {
            AssetDatabase.importPackageCompleted -= OnPackageImported;
            AssetDatabase.importPackageFailed -= OnPackageFailed;
            EditorApplication.delayCall += FinishBatch;
        }
        static void OnPackageFailed(string name,string error)
        { Debug.LogError(error); EditorApplication.Exit(1); }
        static void FinishBatch()
        {
            try { Create(); EditorApplication.Exit(0); }
            catch(Exception exception) { Debug.LogException(exception); EditorApplication.Exit(1); }
        }
        [MenuItem("Fortress/Open Flat Range")]
        public static void Create()
        {
            if (File.Exists(ScenePath))
            {
                if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
                EditorSceneManager.OpenScene(ScenePath);
                return;
            }
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Directory.CreateDirectory(Data);
            Directory.CreateDirectory(Prefabs);
            Directory.CreateDirectory("Assets/Fortress/Scenes");
            AssetDatabase.Refresh();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var tuning = Asset<GameTuning>("GameTuning");
            var flight = Asset<FlightProfile>("FlightProfile_Arc");
            flight.segments = new List<FlightSegment> { new PhysicsArcSegment { gravity = tuning.gravity } };
            flight.speedCurve = AnimationCurve.Linear(0, 0, 1, 1);
            flight.wobbleAmplitude = 0;
            var fuse = Asset<ImpactFuse>("Fuse_Impact");
            var payload = Asset<SingleImpactPayload>("Payload_Single");
            var profile = Asset<ProjectileProfile>("Profile_Standard");
            profile.flight = flight; profile.fuse = fuse; profile.payload = payload;
            profile.impactSoonLead = 0.5f; profile.lingerTime = 0;
            EditorUtility.SetDirty(flight); EditorUtility.SetDirty(profile);

            var blue = Material("TankBlue", new Color(.1f,.55f,.85f));
            var red = Material("TankRed", new Color(.9f,.3f,.22f));
            var dark = Material("Tracks", new Color(.08f,.12f,.17f));
            var ground = Material("Ground", new Color(.17f,.25f,.24f));
            var stripe = Material("Markings", new Color(.42f,.57f,.5f));
            var gold = Material("Shell", new Color(1f,.72f,.16f));
            var markerMaterial = Material("Prediction", new Color(.4f,1f,.4f));
            Primitive("Ground", PrimitiveType.Plane, null, new Vector3(0,tuning.groundHeight,0), new Vector3(20,1,20), ground);
            for (int x=-80; x<=80; x+=10)
                Primitive("Range line " + x, PrimitiveType.Cube, null, new Vector3(x,.015f,0), new Vector3(.08f,.02f,160), stripe);
            for (int z=-80; z<=80; z+=10)
                Primitive("Range cross " + z, PrimitiveType.Cube, null, new Vector3(0,.016f,z), new Vector3(160,.02f,.08f), stripe);

            var template = Tank("Tank", blue, dark);
            var tankPrefab = PrefabUtility.SaveAsPrefabAsset(template.gameObject, Prefabs + "Tank.prefab");
            UnityEngine.Object.DestroyImmediate(template.gameObject);
            var left = (GameObject)PrefabUtility.InstantiatePrefab(tankPrefab);
            left.name = "Player Tank"; left.transform.position = new Vector3(-25,0,0); left.transform.rotation=Quaternion.Euler(0,90,0);
            var right = (GameObject)PrefabUtility.InstantiatePrefab(tankPrefab);
            right.name = "Target Tank"; right.transform.position = new Vector3(25,0,0); right.transform.rotation=Quaternion.Euler(0,-90,0);
            foreach(var renderer in right.GetComponentsInChildren<MeshRenderer>())
                if(renderer.sharedMaterial==blue) renderer.sharedMaterial=red;

            var shell = Primitive("Standard Shell",PrimitiveType.Sphere,null,Vector3.zero,Vector3.one*.45f,gold);
            var runner = shell.AddComponent<ProjectileRunner>(); runner.Profile=profile;
            shell.AddComponent<ProjectileEventRelay>();
            var shellPrefab=PrefabUtility.SaveAsPrefabAsset(shell,Prefabs+"StandardShell.prefab").GetComponent<ProjectileRunner>();
            UnityEngine.Object.DestroyImmediate(shell);
            var pool = new GameObject("Projectile Pool").AddComponent<ProjectilePool>();
            var effect = new GameObject("Impact Burst").AddComponent<ParticleSystem>();
            effect.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            var main=effect.main; main.loop=false; main.playOnAwake=false; main.duration=.5f; main.startLifetime=.65f;
            main.startSpeed=7; main.startSize=.6f; main.startColor=new Color(1,.6f,.15f); main.gravityModifier=1;
            var emission=effect.emission; emission.rateOverTime=0; emission.SetBursts(new[]{new ParticleSystem.Burst(0,35)});
            var shape=effect.shape; shape.shapeType=ParticleSystemShapeType.Hemisphere; shape.radius=.5f;
            var particleShader=Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if(particleShader==null) throw new InvalidOperationException("URP particle shader missing");
            var particleMaterial=AssetDatabase.LoadAssetAtPath<Material>(Data+"ImpactParticles.mat");
            if(particleMaterial==null) { particleMaterial=new Material(particleShader){name="ImpactParticles"}; AssetDatabase.CreateAsset(particleMaterial,Data+"ImpactParticles.mat"); }
            effect.GetComponent<ParticleSystemRenderer>().sharedMaterial=particleMaterial;
            var marker=Primitive("Debug Prediction Marker",PrimitiveType.Sphere,null,Vector3.zero,Vector3.one*.8f,markerMaterial);
            var controller=new GameObject("Flat Range Controller").AddComponent<FlatShotController>();
            controller.Configure(tuning,new[]{left.GetComponent<TankRig>(),right.GetComponent<TankRig>()},shellPrefab,pool,effect);
            controller.predictionMarker=marker.transform;
            controller.gameObject.AddComponent<AimInput>().controller=controller;

            var camera=new GameObject("Main Camera",typeof(Camera),typeof(AudioListener)).GetComponent<Camera>();
            camera.tag="MainCamera"; camera.transform.position=new Vector3(0,45,-65);
            camera.transform.LookAt(new Vector3(0,8,0)); camera.fieldOfView=55; camera.farClipPlane=400;
            camera.clearFlags=CameraClearFlags.SolidColor; camera.backgroundColor=new Color(.035f,.065f,.11f);
            var light=new GameObject("Sun").AddComponent<Light>(); light.type=LightType.Directional; light.intensity=2;
            light.transform.rotation=Quaternion.Euler(50,-35,0); light.shadows=LightShadows.Soft;
            RenderSettings.ambientLight=new Color(.5f,.55f,.65f);
            CreateHud(controller);
            EditorUtility.SetDirty(controller); EditorUtility.SetDirty(tuning);
            AssetDatabase.SaveAssets();
            EditorSceneManager.SaveScene(scene,ScenePath);
            Debug.Log("Fortress flat range created: " + ScenePath);
        }

        static T Asset<T>(string name) where T:ScriptableObject
        {
            var path=Data+name+".asset";
            var value=AssetDatabase.LoadAssetAtPath<T>(path);
            if(value!=null) return value;
            value=ScriptableObject.CreateInstance<T>(); value.name=name; AssetDatabase.CreateAsset(value,path); return value;
        }
        static Material Material(string name,Color color)
        {
            var path=Data+name+".mat";
            var result=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(result!=null) return result;
            var shader=Shader.Find("Universal Render Pipeline/Lit");
            if(shader==null) throw new InvalidOperationException("URP Lit shader missing");
            result=new Material(shader){name=name}; result.SetColor("_BaseColor",color); AssetDatabase.CreateAsset(result,path); return result;
        }
        static GameObject Primitive(string name,PrimitiveType type,Transform parent,Vector3 position,Vector3 scale,Material material)
        {
            var go=GameObject.CreatePrimitive(type); go.name=name; go.transform.SetParent(parent,false);
            go.transform.localPosition=position; go.transform.localScale=scale;
            go.GetComponent<MeshRenderer>().sharedMaterial=material;
            UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
            return go;
        }
        static TankRig Tank(string name,Material body,Material tracks)
        {
            var go=new GameObject(name); var rig=go.AddComponent<TankRig>();
            Primitive("Hull",PrimitiveType.Cube,go.transform,new Vector3(0,.9f,0),new Vector3(2.8f,1.2f,3.4f),body);
            Primitive("Track L",PrimitiveType.Cube,go.transform,new Vector3(-1.5f,.6f,0),new Vector3(.7f,1,3.8f),tracks);
            Primitive("Track R",PrimitiveType.Cube,go.transform,new Vector3(1.5f,.6f,0),new Vector3(.7f,1,3.8f),tracks);
            rig.turret=new GameObject("Turret").transform; rig.turret.SetParent(go.transform,false); rig.turret.localPosition=new Vector3(0,1.8f,0);
            Primitive("Turret Mesh",PrimitiveType.Cylinder,rig.turret,Vector3.zero,new Vector3(2.1f,.45f,2.1f),body);
            rig.barrel=new GameObject("Barrel").transform; rig.barrel.SetParent(rig.turret,false);
            Primitive("Barrel Mesh",PrimitiveType.Cube,rig.barrel,new Vector3(0,0,1.8f),new Vector3(.35f,.35f,3.6f),body);
            rig.muzzle=new GameObject("Muzzle").transform; rig.muzzle.SetParent(rig.barrel,false); rig.muzzle.localPosition=new Vector3(0,0,3.8f);
            return rig;
        }
        static void CreateHud(FlatShotController controller)
        {
            var canvasObject=new GameObject("Range HUD",typeof(Canvas),typeof(CanvasScaler));
            canvasObject.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;
            var scaler=canvasObject.GetComponent<CanvasScaler>(); scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution=new Vector2(1920,1080);
            var font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
            if(font==null)
            {
                var packages=Directory.GetDirectories("Library/PackageCache","com.unity.ugui@*");
                if(packages.Length!=1) throw new InvalidOperationException("Expected one uGUI package");
                AssetDatabase.ImportPackage(Path.Combine(packages[0],"Package Resources/TMP Essential Resources.unitypackage"),false);
                font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
            }
            if(font==null) throw new InvalidOperationException("TMP essentials font import failed");
            var panel=new GameObject("Instructions",typeof(RectTransform),typeof(TextMeshProUGUI)); panel.transform.SetParent(canvasObject.transform,false);
            var rect=panel.GetComponent<RectTransform>(); rect.anchorMin=new Vector2(0,1); rect.anchorMax=new Vector2(1,1); rect.pivot=new Vector2(0,1); rect.anchoredPosition=new Vector2(32,-26); rect.sizeDelta=new Vector2(-64,150);
            var label=panel.GetComponent<TextMeshProUGUI>(); label.font=font; label.fontSize=26; label.color=Color.white; label.raycastTarget=false;
            label.text="FORTRESS / FLAT RANGE\nA/D direction   W/S elevation   Q/E power   SPACE fire\nGreen marker: debug impact prediction. Target tank is visual only in Epic 1.";
            var valuesObject=UnityEngine.Object.Instantiate(panel,canvasObject.transform);
            valuesObject.name="Aim Values";
            valuesObject.GetComponent<RectTransform>().anchoredPosition=new Vector2(32,-200);
            var values=valuesObject.GetComponent<TextMeshProUGUI>(); values.text="";
            var statusObject=UnityEngine.Object.Instantiate(panel,canvasObject.transform);
            statusObject.name="Shot Status";
            statusObject.GetComponent<RectTransform>().anchoredPosition=new Vector2(32,-245);
            var status=statusObject.GetComponent<TextMeshProUGUI>(); status.text="READY / SPACE TO FIRE"; status.color=new Color(.4f,1f,.7f);
            var hud=canvasObject.AddComponent<FlatShotHud>(); hud.controller=controller; hud.values=values; hud.status=status;
        }
    }
}
