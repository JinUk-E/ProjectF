using System;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Fortress.Editor
{
    public static class TerrainRangeBuilder
    {
        public const string ScenePath = "Assets/Fortress/Scenes/TerrainRange.unity";
        const string DataPath = "Assets/Fortress/Data/";

        public static void CreateBatch()
        {
            try { Create(); EditorApplication.Exit(0); }
            catch (Exception exception) { Debug.LogException(exception); EditorApplication.Exit(1); }
        }

        [MenuItem("Fortress/Open Terrain Range")]
        public static void Create()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            if (File.Exists(ScenePath)) { EditorSceneManager.OpenScene(ScenePath); return; }
            var scene = EditorSceneManager.OpenScene(FlatRangeBuilder.ScenePath);
            foreach (var root in scene.GetRootGameObjects())
                if (root.name == "Ground" || root.name.StartsWith("Range line ") || root.name.StartsWith("Range cross "))
                    UnityEngine.Object.DestroyImmediate(root);

            var controller = UnityEngine.Object.FindFirstObjectByType<FlatShotController>();
            var settings = AssetDatabase.LoadAssetAtPath<GameTuning>(DataPath + "TerrainGameTuning.asset");
            if (settings == null)
            {
                settings = UnityEngine.Object.Instantiate(controller.tuning);
                settings.name = "TerrainGameTuning";
                AssetDatabase.CreateAsset(settings, DataPath + "TerrainGameTuning.asset");
            }
            controller.tuning = settings;
            var data = AssetDatabase.LoadAssetAtPath<TerrainData>(DataPath + "RangeTerrain.asset");
            if (data == null)
            {
                data = new TerrainData { name = "RangeTerrain", heightmapResolution = 257, size = new Vector3(160,16,160) };
                var heights = new float[257,257];
                for(int z=0;z<257;z++) for(int x=0;x<257;x++)
                {
                    float wx=x*160f/256-80, wz=z*160f/256-80;
                    float ridge=2f*Mathf.Exp(-(wx*wx/144f+wz*wz/1600f));
                    heights[z,x]=(8+ridge)/16;
                }
                data.SetHeights(0,0,heights);
                AssetDatabase.CreateAsset(data, DataPath + "RangeTerrain.asset");
            }
            var layer=AssetDatabase.LoadAssetAtPath<TerrainLayer>(DataPath+"RangeGround.terrainlayer");
            if(layer==null)
            {
                var texture=new Texture2D(64,64,TextureFormat.RGBA32,false) {name="RangeGrid",wrapMode=TextureWrapMode.Repeat};
                var pixels=new Color[64*64];
                for(int z=0;z<64;z++) for(int x=0;x<64;x++)
                    pixels[z*64+x]=(x<1 || z<1) ? new Color(.4f,.52f,.38f) : new Color(.22f,.32f,.19f);
                texture.SetPixels(pixels); texture.Apply();
                AssetDatabase.CreateAsset(texture,DataPath+"RangeGrid.asset");
                layer=new TerrainLayer {name="RangeGround",diffuseTexture=texture,tileSize=new Vector2(8,8)};
                AssetDatabase.CreateAsset(layer,DataPath+"RangeGround.terrainlayer");
            }
            data.terrainLayers=new[]{layer};
            var ground=new GameObject("Destructible Terrain").AddComponent<Terrain>();
            ground.transform.position=new Vector3(-80,-8,-80);
            ground.terrainData=data; ground.heightmapPixelError=1; ground.drawInstanced=true;
            var shader=Shader.Find("Universal Render Pipeline/Terrain/Lit");
            if(shader==null) throw new InvalidOperationException("URP Terrain shader missing");
            var material=AssetDatabase.LoadAssetAtPath<Material>(DataPath+"RangeTerrain.mat");
            if(material==null) { material=new Material(shader); AssetDatabase.CreateAsset(material,DataPath+"RangeTerrain.mat"); }
            ground.materialTemplate=material;
            var map=ground.gameObject.AddComponent<HeightFieldTerrain>();
            map.terrain=ground; map.tuning=settings;
            controller.terrainMap=map;
            foreach(var tank in controller.tanks)
            {
                var position=tank.transform.position;
                position.y=data.GetInterpolatedHeight((position.x+80)/160,(position.z+80)/160)-8;
                tank.transform.position=position;
            }
            var lineShader=Shader.Find("Universal Render Pipeline/Unlit");
            if(lineShader==null) throw new InvalidOperationException("URP unlit shader missing");
            var lineMaterial=AssetDatabase.LoadAssetAtPath<Material>(DataPath+"AimPreview.mat");
            if(lineMaterial==null)
            {
                lineMaterial=new Material(lineShader); lineMaterial.SetColor("_BaseColor",new Color(1,.88f,.42f));
                AssetDatabase.CreateAsset(lineMaterial,DataPath+"AimPreview.mat");
            }
            var preview=controller.gameObject.AddComponent<DashedAimPreview>();
            preview.controller=controller; preview.lineMaterial=lineMaterial;
            foreach(var label in UnityEngine.Object.FindObjectsByType<TextMeshProUGUI>(FindObjectsSortMode.None))
                if(label.gameObject.name=="Instructions")
                    label.text="FORTRESS / DESTRUCTIBLE RANGE\nA/D direction   W/S elevation   Q/E power   SPACE fire\nYellow dashes ignore wind. Green sphere: debug impact. Craters apply after flight.";
            EditorUtility.SetDirty(data); EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
            EditorSceneManager.SaveScene(scene,ScenePath);
            Debug.Log("Fortress terrain range created: "+ScenePath);
        }
    }
}
