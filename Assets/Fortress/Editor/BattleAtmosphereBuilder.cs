using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace Fortress.Editor
{
    public static class BattleAtmosphereBuilder
    {
        const string Root="Assets/Fortress/Art/Atmosphere/";
        const string Photo="Assets/ThirdParty/PolyHavenRockyTerrain/rocky_terrain_";
        const string NewPhoto="Assets/ThirdParty/PolyHavenBattlefield/";
        [MenuItem("Fortress/Upgrade Battlefield Atmosphere")]
        public static void Upgrade()
        {
            if(EditorApplication.isPlaying) throw new InvalidOperationException("Stop play before applying atmosphere.");
            Directory.CreateDirectory(Root);AssetDatabase.Refresh();
            foreach(var id in new[]{"aerial_grass_rock","brown_mud_leaves_01","concrete_wall_009"})
            {var importer=(TextureImporter)AssetImporter.GetAtPath(NewPhoto+id+"_nor_gl_2k.jpg");if(importer.textureType!=TextureImporterType.NormalMap){importer.textureType=TextureImporterType.NormalMap;importer.SaveAndReimport();}}
            var grass=Layer("Ground",new Color(.88f,.94f,.83f),12,NewPhoto+"aerial_grass_rock_");
            var road=Layer("Road",new Color(1f,.94f,.83f),5,NewPhoto+"brown_mud_leaves_01_");
            var rock=Layer("Rock",new Color(.77f,.79f,.77f),5,Photo);
            var aim=AssetDatabase.LoadAssetAtPath<AimPreviewStyle>(Root+"ReadableAim.asset");
            if(aim==null){aim=ScriptableObject.CreateInstance<AimPreviewStyle>();AssetDatabase.CreateAsset(aim,Root+"ReadableAim.asset");}
            aim.outlineMaterial=AimMaterial("AimOutline",CompareFunction.LessEqual,3000);
            aim.coreMaterial=AimMaterial("AimCore",CompareFunction.LessEqual,3002);
            aim.occludedMaterial=AimMaterial("AimOccluded",CompareFunction.Greater,3001);EditorUtility.SetDirty(aim);
            var cover=AssetDatabase.LoadAssetAtPath<CoverSettings>("Assets/Fortress/Data/DepotCover.asset");
            Undo.RecordObject(cover,"Weathered depot materials");
            cover.wallMaterial=Surface("Concrete",new Color(.65f,.64f,.57f),false);
            cover.roofMaterial=Surface("SteelRoof",new Color(.30f,.34f,.32f),true);
            cover.rubbleMaterial=Surface("Rubble",new Color(.43f,.40f,.34f),false);EditorUtility.SetDirty(cover);
            var tuning=AssetDatabase.LoadAssetAtPath<BattleVisualTuning>("Assets/Fortress/Data/BattleVisualTuning.asset");
            Undo.RecordObject(tuning,"Heavier recoil");tuning.recoilDistance=.65f;tuning.recoilSeconds=.55f;EditorUtility.SetDirty(tuning);
            MakeSound("Cannon",1.25f,65);MakeSound("Impact",2.2f,43);
            foreach(var path in new[]{MatchSceneBuilder.ScenePath,AiMatchBuilder.ScenePath})
            {
                var scene=EditorSceneManager.OpenScene(path);var match=UnityEngine.Object.FindAnyObjectByType<MatchController>();
                var preview=UnityEngine.Object.FindAnyObjectByType<DashedAimPreview>();Undo.RecordObject(preview,"Readable aim");preview.style=aim;EditorUtility.SetDirty(preview);
                var data=match.terrainMap.terrain.terrainData;Undo.RecordObject(data,"Photographic terrain layers");data.terrainLayers=new[]{grass,road,rock};EditorUtility.SetDirty(data);
                RenderSettings.ambientMode=AmbientMode.Trilight;
                RenderSettings.ambientSkyColor=new Color(.50f,.55f,.60f);RenderSettings.ambientEquatorColor=new Color(.34f,.36f,.33f);RenderSettings.ambientGroundColor=new Color(.18f,.17f,.15f);
                RenderSettings.fog=true;RenderSettings.fogMode=FogMode.Linear;RenderSettings.fogColor=new Color(.48f,.53f,.56f);RenderSettings.fogStartDistance=110;RenderSettings.fogEndDistance=290;
                foreach(var light in UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
                    if(light.type==LightType.Directional){Undo.RecordObject(light,"Overcast daylight");light.color=new Color(1,.96f,.89f);light.intensity=1.65f;light.shadows=LightShadows.Soft;light.shadowStrength=.8f;}
                var camera=Camera.main;camera.backgroundColor=RenderSettings.fogColor;
                var effect=match.shots.impactEffect;effect.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
                var main=effect.main;main.startLifetime=new ParticleSystem.MinMaxCurve(1.4f,2.8f);main.startSize=new ParticleSystem.MinMaxCurve(.7f,1.8f);main.startSpeed=new ParticleSystem.MinMaxCurve(2,6);main.gravityModifier=.12f;main.startColor=new Color(.37f,.33f,.27f,.65f);
                var emission=effect.emission;emission.SetBursts(new[]{new ParticleSystem.Burst(0,42)});
                var shape=effect.shape;shape.rotation=new Vector3(-90,0,0);
                var presentation=UnityEngine.Object.FindAnyObjectByType<ShotPresentation>();
                var flash=presentation.impactFlash.main;flash.startLifetime=.11f;flash.startSize=2.2f;
                AttachAudio(presentation,"shotAudio","Cannon",.6f);AttachAudio(presentation,"impactAudio","Impact",.7f);
                EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            }
            AssetDatabase.SaveAssets();
        }
        static TerrainLayer Layer(string name,Color tint,float tile,string source)
        {
            var layer=AssetDatabase.LoadAssetAtPath<TerrainLayer>(Root+name+".terrainlayer");
            if(layer==null){layer=new TerrainLayer();AssetDatabase.CreateAsset(layer,Root+name+".terrainlayer");}
            Undo.RecordObject(layer,"Detailed ground");layer.diffuseTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(source+"diff_2k.jpg");layer.normalMapTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(source+"nor_gl_2k.jpg");
            layer.diffuseRemapMax=new Vector4(tint.r,tint.g,tint.b,1);layer.normalScale=.65f;layer.tileSize=new Vector2(tile,tile);layer.smoothness=.08f;EditorUtility.SetDirty(layer);return layer;
        }
        static Material AimMaterial(string name,CompareFunction depth,int queue)
        {
            var material=AssetDatabase.LoadAssetAtPath<Material>(Root+name+".mat");if(material==null){material=new Material(Shader.Find("Fortress/AimDepthLine"));AssetDatabase.CreateAsset(material,Root+name+".mat");}
            material.SetColor("_BaseColor",Color.white);material.SetFloat("_ZTest",(float)depth);material.renderQueue=queue;EditorUtility.SetDirty(material);return material;
        }
        static Material Surface(string name,Color tint,bool metal)
        {
            var mat=AssetDatabase.LoadAssetAtPath<Material>(Root+name+".mat");if(mat==null){mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(mat,Root+name+".mat");}
            Undo.RecordObject(mat,"Weathered surface");mat.SetColor("_BaseColor",tint);mat.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(NewPhoto+"concrete_wall_009_diff_2k.jpg"));mat.SetTextureScale("_BaseMap",new Vector2(2,2));
            mat.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(NewPhoto+"concrete_wall_009_nor_gl_2k.jpg"));mat.SetFloat("_BumpScale",metal?.18f:.35f);mat.EnableKeyword("_NORMALMAP");mat.SetFloat("_Metallic",metal?.65f:0);mat.SetFloat("_Smoothness",metal?.28f:.08f);EditorUtility.SetDirty(mat);return mat;
        }
        static void MakeSound(string name,float seconds,float frequency)
        {
            // Original synthesized low-frequency transient, no third-party audio dependency.
            const int rate=22050;int count=(int)(seconds*rate);var random=new System.Random(431);float filtered=0;
            using(var writer=new BinaryWriter(File.Create(Root+name+".wav")))
            {
                writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));writer.Write(36+count*2);writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));writer.Write(16);writer.Write((short)1);writer.Write((short)1);writer.Write(rate);writer.Write(rate*2);writer.Write((short)2);writer.Write((short)16);writer.Write(System.Text.Encoding.ASCII.GetBytes("data"));writer.Write(count*2);
                for(int i=0;i<count;i++){float t=(float)i/rate;float noise=(float)random.NextDouble()*2-1;filtered=Mathf.Lerp(filtered,noise,.12f);float body=Mathf.Sin(2*Mathf.PI*frequency*t)*Mathf.Exp(-t*7);float value=(body*.45f+filtered*.9f)*Mathf.Exp(-t*3)+noise*.15f*Mathf.Exp(-t*35);value*=Mathf.Min(1,t*500)*Mathf.Clamp01((seconds-t)*20);writer.Write((short)(Mathf.Clamp(value,-.9f,.9f)*32767));}
            }
            AssetDatabase.ImportAsset(Root+name+".wav");
        }
        static void AttachAudio(ShotPresentation presentation,string property,string clip,float volume)
        {
            var child=presentation.transform.Find(clip+" Audio");if(child==null){var go=new GameObject(clip+" Audio");go.transform.SetParent(presentation.transform,false);child=go.transform;}
            var source=child.GetComponent<AudioSource>();if(source==null)source=Undo.AddComponent<AudioSource>(child.gameObject);source.playOnAwake=false;source.spatialBlend=0;source.volume=volume;source.clip=AssetDatabase.LoadAssetAtPath<AudioClip>(Root+clip+".wav");
            var serialized=new SerializedObject(presentation);var field=serialized.FindProperty(property);if(field==null)throw new InvalidOperationException("Missing audio hook: "+property);field.objectReferenceValue=source;serialized.ApplyModifiedProperties();
        }
    }
}
