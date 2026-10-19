using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace Fortress.Editor
{
    public static class BattlefieldExteriorBuilder
    {
        const string Root="Assets/Fortress/Art/Exterior/";
        [MenuItem("Fortress/Upgrade Battlefield Surroundings")]
        public static void Upgrade()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop play first.");
            Directory.CreateDirectory(Root);AssetDatabase.Refresh();
            var ground=Material("SurroundingGround",new Color(.88f,.94f,.83f));
            ground.shader=Shader.Find("Fortress/ExteriorFog");
            ground.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/ThirdParty/PolyHavenBattlefield/aerial_grass_rock_diff_2k.jpg"));
            ground.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/ThirdParty/PolyHavenBattlefield/aerial_grass_rock_nor_gl_2k.jpg"));
            ground.EnableKeyword("_NORMALMAP");ground.SetFloat("_BumpScale",.4f);EditorUtility.SetDirty(ground);
            var boundary=Material("BoundaryMarkers",new Color(.48f,.39f,.18f));
            var bark=Material("DistantBark",new Color(.18f,.15f,.10f));
            var foliage=Material("DistantPine",new Color(.13f,.21f,.13f));
            bark.shader=foliage.shader=Shader.Find("Fortress/ExteriorFog");EditorUtility.SetDirty(bark);EditorUtility.SetDirty(foliage);
            var wall=Material("PerimeterConcrete",new Color(.55f,.54f,.48f));
            wall.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/ThirdParty/PolyHavenBattlefield/concrete_wall_009_diff_2k.jpg"));EditorUtility.SetDirty(wall);
            var tree=Tree(bark,foliage);
            var settings=AssetDatabase.LoadAssetAtPath<ExteriorSettings>(Root+"Surroundings.asset");
            if(settings==null){settings=ScriptableObject.CreateInstance<ExteriorSettings>();AssetDatabase.CreateAsset(settings,Root+"Surroundings.asset");}
            Undo.RecordObject(settings,"Surrounding landscape");settings.apronWidth=180;settings.ridgeHeight=27;settings.ringSegments=12;
            settings.groundMaterial=ground;settings.boundaryMaterial=boundary;settings.boundaryPostSpacing=10;settings.treePrefab=tree;settings.treeCount=160;EditorUtility.SetDirty(settings);
            settings.boundaryWallHeight=2.25f;settings.boundaryWallThickness=1;settings.boundaryWallBurial=1;settings.boundaryWallMaterial=wall;settings.fogStart=2;settings.fogEnd=35;
            var cover=AssetDatabase.LoadAssetAtPath<CoverSettings>("Assets/Fortress/Data/DepotCover.asset");
            Undo.RecordObject(cover,"Low rubble cover");cover.rubbleHeight=1.1f;cover.rubbleFootprintFraction=.55f;EditorUtility.SetDirty(cover);
            foreach(var path in new[]{MatchSceneBuilder.ScenePath,AiMatchBuilder.ScenePath})
            {
                var scene=EditorSceneManager.OpenScene(path);var match=UnityEngine.Object.FindAnyObjectByType<MatchController>();
                var exterior=match.GetComponent<BattlefieldExterior>();if(exterior==null)exterior=Undo.AddComponent<BattlefieldExterior>(match.gameObject);
                Undo.RecordObject(exterior,"Connect surrounding landscape");exterior.match=match;exterior.settings=settings;EditorUtility.SetDirty(exterior);
                var camera=Camera.main;Undo.RecordObject(camera,"Hazy horizon");camera.farClipPlane=Mathf.Max(camera.farClipPlane,500);
                var orbit=camera.GetComponent<BattlefieldOrbitCamera>();
                if(orbit!=null){Undo.RecordObject(orbit,"Surrounding landscape clearance");orbit.exterior=exterior;EditorUtility.SetDirty(orbit);}
                RenderSettings.fog=true;RenderSettings.fogMode=FogMode.Linear;RenderSettings.fogStartDistance=105;RenderSettings.fogEndDistance=250;camera.backgroundColor=RenderSettings.fogColor;
                EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            }
            AssetDatabase.SaveAssets();
        }
        static Material Material(string name,Color color)
        {
            var value=AssetDatabase.LoadAssetAtPath<Material>(Root+name+".mat");
            if(value==null){value=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(value,Root+name+".mat");}
            Undo.RecordObject(value,"Landscape material");value.SetColor("_BaseColor",color);value.SetFloat("_Smoothness",.04f);value.enableInstancing=true;EditorUtility.SetDirty(value);return value;
        }
        static GameObject Tree(Material bark,Material leaves)
        {
            var root=new GameObject("Distant mountain pine");
            try
            {
                Part(root.transform,"Trunk",new Vector3(0,2.5f,0),new Vector3(.22f,5,.22f),bark);
                // Clustered crowns are intended for the distant woodland only.
                for(int i=0;i<5;i++)
                {
                    var p=Part(root.transform,"Needle crown",new Vector3(Mathf.Sin(i*2.4f)*.25f,2.2f+i*.65f,Mathf.Cos(i*2.4f)*.25f),new Vector3(2.7f-i*.38f,1.4f,2.5f-i*.34f),leaves);
                    p.localRotation=Quaternion.Euler(0,i*37,0);
                }
                var filters=root.GetComponentsInChildren<MeshFilter>();var crowns=new CombineInstance[filters.Length-1];
                for(int i=1;i<filters.Length;i++)crowns[i-1]=new CombineInstance{mesh=filters[i].sharedMesh,transform=filters[i].transform.localToWorldMatrix};
                var crownMesh=new Mesh();crownMesh.CombineMeshes(crowns,true,true);
                var merged=new Mesh{name="Distant pine clustered mesh"};
                merged.CombineMeshes(new[]{new CombineInstance{mesh=filters[0].sharedMesh,transform=filters[0].transform.localToWorldMatrix},new CombineInstance{mesh=crownMesh,transform=Matrix4x4.identity}},false,true);
                UnityEngine.Object.DestroyImmediate(crownMesh);
                var saved=AssetDatabase.LoadAssetAtPath<Mesh>(Root+"DistantPine.asset");
                if(saved==null){saved=merged;AssetDatabase.CreateAsset(saved,Root+"DistantPine.asset");}else{EditorUtility.CopySerialized(merged,saved);UnityEngine.Object.DestroyImmediate(merged);}
                for(int i=root.transform.childCount-1;i>=0;i--)UnityEngine.Object.DestroyImmediate(root.transform.GetChild(i).gameObject);
                root.AddComponent<MeshFilter>().sharedMesh=saved;root.AddComponent<MeshRenderer>().sharedMaterials=new[]{bark,leaves};
                return PrefabUtility.SaveAsPrefabAsset(root,Root+"DistantPine.prefab");
            }
            finally{UnityEngine.Object.DestroyImmediate(root);}
        }
        static Transform Part(Transform parent,string name,Vector3 position,Vector3 scale,Material material)
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Sphere);go.name=name;go.transform.SetParent(parent,false);go.transform.localPosition=position;go.transform.localScale=scale;
            UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());go.GetComponent<MeshRenderer>().sharedMaterial=material;return go.transform;
        }
    }
}
