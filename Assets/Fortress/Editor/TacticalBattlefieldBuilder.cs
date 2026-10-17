using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace Fortress.Editor
{
    public static class TacticalBattlefieldBuilder
    {
        const string Root="Assets/Fortress/Data/";
        [MenuItem("Fortress/Upgrade Tactical Battlefield")]
        public static void Upgrade()
        {
            if(EditorApplication.isPlaying) throw new System.InvalidOperationException("Stop play before upgrade.");
            var profile=AssetDatabase.LoadAssetAtPath<MountainTerrainSettings>(Root+"KoreanMountainStyle.asset");
            Undo.RecordObject(profile,"Switch to hills and road network");
            profile.tacticalHills=true;profile.hillHeight=14;profile.hillRadius=21;profile.hillSpacing=32;profile.hillJitter=7;
            profile.roadHalfWidth=3;profile.roadBlend=5;profile.flankRoadOffset=22;profile.roadJunctionX=26;profile.detailHeight=.55f;
            EditorUtility.SetDirty(profile);
            var settings=AssetDatabase.LoadAssetAtPath<CoverSettings>(Root+"DepotCover.asset");
            if(settings==null) {settings=ScriptableObject.CreateInstance<CoverSettings>();AssetDatabase.CreateAsset(settings,Root+"DepotCover.asset");}
            Undo.RecordObject(settings,"Building materials");
            settings.wallMaterial=Material("DepotConcrete",new Color(.43f,.46f,.39f));
            settings.roofMaterial=Material("DepotRoof",new Color(.22f,.25f,.22f));
            settings.windowMaterial=Material("DepotWindows",new Color(.075f,.115f,.13f));
            settings.rubbleMaterial=Material("DepotRubble",new Color(.30f,.29f,.25f));EditorUtility.SetDirty(settings);
            var grass=Layer("TacticalGrass",new Color(.25f,.32f,.16f),new Color(.37f,.40f,.24f));
            var road=Layer("TacticalDirt",new Color(.39f,.32f,.23f),new Color(.53f,.45f,.32f));
            foreach(var path in new[]{MatchSceneBuilder.ScenePath,AiMatchBuilder.ScenePath})
            {
                var scene=EditorSceneManager.OpenScene(path);var match=Object.FindAnyObjectByType<MatchController>();
                var cover=match.GetComponent<BattlefieldCover>();if(cover==null) cover=Undo.AddComponent<BattlefieldCover>(match.gameObject);
                Undo.RecordObject(cover,"Cover settings");cover.settings=settings;
                Undo.RecordObject(match,"Connect cover");match.cover=cover;
                Undo.RecordObject(match.tuning,"Tactical terrain tuning");match.tuning.spawnPadBlend=8;match.tuning.mountainTerrain=profile;
                var data=match.terrainMap.terrain.terrainData;Undo.RecordObject(data,"Road terrain layers");data.terrainLayers=new[]{grass,road};
                var camera=Camera.main;Undo.RecordObject(camera.transform,"Tactical overview");camera.transform.position=new Vector3(-48,44,-52);camera.transform.LookAt(new Vector3(0,2,0));
                var orbit=camera.GetComponent<BattlefieldOrbitCamera>();Undo.RecordObject(orbit,"Orbit target");orbit.initialTarget=new Vector3(0,2,0);
                EditorUtility.SetDirty(orbit);EditorUtility.SetDirty(match.tuning);EditorUtility.SetDirty(data);EditorUtility.SetDirty(match);EditorUtility.SetDirty(cover);
                EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            }
            AssetDatabase.SaveAssets();Debug.Log("Hills, destructible roads and cover depots installed.");
        }
        static Material Material(string name,Color color)
        {
            var material=AssetDatabase.LoadAssetAtPath<Material>(Root+name+".mat");
            if(material==null) {material=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(material,Root+name+".mat");}
            Undo.RecordObject(material,"Depot material");material.SetColor("_BaseColor",color);material.SetFloat("_Smoothness",.15f);EditorUtility.SetDirty(material);return material;
        }
        static TerrainLayer Layer(string name,Color dark,Color light)
        {
            var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(Root+name+".asset");
            if(texture==null)
            {
                texture=new Texture2D(64,64,TextureFormat.RGB24,true);texture.name=name;texture.wrapMode=TextureWrapMode.Repeat;
                var random=new System.Random(73);var pixels=new Color[64*64];
                for(int i=0;i<pixels.Length;i++) pixels[i]=Color.Lerp(dark,light,(float)random.NextDouble());
                texture.SetPixels(pixels);texture.Apply();AssetDatabase.CreateAsset(texture,Root+name+".asset");
            }
            var layer=AssetDatabase.LoadAssetAtPath<TerrainLayer>(Root+name+".terrainlayer");
            if(layer==null) {layer=new TerrainLayer();AssetDatabase.CreateAsset(layer,Root+name+".terrainlayer");}
            Undo.RecordObject(layer,"Terrain texture");layer.diffuseTexture=texture;layer.tileSize=new Vector2(6,6);layer.smoothness=0;EditorUtility.SetDirty(layer);return layer;
        }
    }
}
