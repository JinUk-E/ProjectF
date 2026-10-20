using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace Fortress.Editor
{
    public static class BattlefieldReliefBuilder
    {
        [MenuItem("Fortress/Upgrade Terrain Relief And Blast Debris")]
        public static void Upgrade()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop play before upgrading scenes.");
            const string folder="Assets/Fortress/Data/";
            var style=AssetDatabase.LoadAssetAtPath<MountainTerrainSettings>(folder+"KoreanMountainStyle.asset");
            Undo.RecordObject(style,"Rolling battlefield relief");
            style.valleyFloor=4;style.rollingHeight=3;style.rollingWavelength=38;
            style.hillHeight=16;style.hillRadius=25;style.peripheralHillHeight=9;style.peripheralHillRadius=21;
            EditorUtility.SetDirty(style);
            var settings=AssetDatabase.LoadAssetAtPath<TerrainBlastSettings>(folder+"TerrainBlast.asset");
            if(settings==null){settings=ScriptableObject.CreateInstance<TerrainBlastSettings>();AssetDatabase.CreateAsset(settings,folder+"TerrainBlast.asset");}
            Undo.RecordObject(settings,"Terrain fragments");
            var material=AssetDatabase.LoadAssetAtPath<Material>(folder+"BlastSoil.mat");
            if(material==null){material=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(material,folder+"BlastSoil.mat");}
            Undo.RecordObject(material,"Earth material");material.SetColor("_BaseColor",new Color(.24f,.19f,.12f));material.SetFloat("_Smoothness",.02f);material.enableInstancing=true;
            material.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/ThirdParty/PolyHavenRockyTerrain/rocky_terrain_diff_2k.jpg"));EditorUtility.SetDirty(material);
            var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(folder+"SoilFragment.asset");
            if(mesh==null)
            {
                mesh=new Mesh{name="Angular earth fragment"};
                var points=new[]{new Vector3(0,.8f,0),new Vector3(-.9f,0,0),new Vector3(0,0,.7f),new Vector3(.8f,0,.1f),new Vector3(.1f,0,-.8f),new Vector3(0,-.5f,0)};
                int[] indices={0,2,1,0,3,2,0,4,3,0,1,4,5,1,2,5,2,3,5,3,4,5,4,1};
                var verts=new Vector3[indices.Length];var triangles=new int[indices.Length];var uv=new Vector2[indices.Length];
                for(int i=0;i<indices.Length;i++){verts[i]=points[indices[i]];triangles[i]=i;uv[i]=new Vector2(verts[i].x,verts[i].z);}
                mesh.vertices=verts;mesh.triangles=triangles;mesh.uv=uv;mesh.RecalculateNormals();mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,folder+"SoilFragment.asset");
            }
            settings.fragmentMaterial=material;settings.fragmentMesh=mesh;EditorUtility.SetDirty(settings);
            foreach(var path in new[]{MatchSceneBuilder.ScenePath,AiMatchBuilder.ScenePath})
            {
                var scene=EditorSceneManager.OpenScene(path);var match=UnityEngine.Object.FindAnyObjectByType<MatchController>();
                Undo.RecordObject(match.tuning,"Thicker battlefield ground");match.tuning.terrainBaseHeight=4;match.tuning.spawnPadBlend=12;EditorUtility.SetDirty(match.tuning);
                var debris=match.GetComponent<TerrainBlastDebris>();if(debris==null)debris=Undo.AddComponent<TerrainBlastDebris>(match.gameObject);
                Undo.RecordObject(debris,"Connect soil fragments");debris.match=match;debris.settings=settings;EditorUtility.SetDirty(debris);
                EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            }
            AssetDatabase.SaveAssets();
        }
    }
}
