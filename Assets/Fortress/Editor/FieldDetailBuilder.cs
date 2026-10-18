using System;
using System.IO;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Fortress.Editor
{
    public static class FieldDetailBuilder
    {
        const string Root="Assets/Fortress/Art/FieldDetails/";
        [MenuItem("Fortress/Upgrade Vegetation and Facilities")]
        public static void Upgrade()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop play first.");
            Directory.CreateDirectory(Root);AssetDatabase.Refresh();
            var bark=Material("Bark",new Color(.20f,.15f,.10f));
            var needles=Material("PineNeedles",new Color(.14f,.23f,.12f));needles.SetFloat("_Cull",0);
            var leaves=Material("ShrubLeaves",new Color(.26f,.32f,.13f));leaves.SetFloat("_Cull",0);
            var tree=Prefab("MountainPine",CreateTree(),bark,needles);
            var shrub=Prefab("WildShrub",CreateShrub(),bark,leaves);
            var settings=AssetDatabase.LoadAssetAtPath<VegetationSettings>(Root+"Vegetation.asset");
            if(settings==null){settings=ScriptableObject.CreateInstance<VegetationSettings>();AssetDatabase.CreateAsset(settings,Root+"Vegetation.asset");}
            Undo.RecordObject(settings,"Vegetation settings");settings.treePrefab=tree;settings.shrubPrefab=shrub;
            settings.treeCount=65;settings.shrubCount=90;settings.minSpacing=3.5f;settings.roadClearance=3;settings.spawnClearance=6;settings.buildingClearance=3;settings.maxSlopeDegrees=48;settings.minScale=.75f;settings.maxScale=1.25f;EditorUtility.SetDirty(settings);
            foreach(var path in new[]{MatchSceneBuilder.ScenePath,AiMatchBuilder.ScenePath})
            {
                var scene=EditorSceneManager.OpenScene(path);var match=UnityEngine.Object.FindAnyObjectByType<MatchController>();
                var vegetation=match.GetComponent<BattlefieldVegetation>();if(vegetation==null)vegetation=Undo.AddComponent<BattlefieldVegetation>(match.gameObject);
                Undo.RecordObject(vegetation,"Connect vegetation");vegetation.match=match;vegetation.settings=settings;EditorUtility.SetDirty(vegetation);
                var hud=UnityEngine.Object.FindAnyObjectByType<MatchHud>();Undo.RecordObject(hud,"Camera-relative wind");hud.windCamera=Camera.main;
                var panel=hud.windLabel.transform.parent.GetComponent<RectTransform>();Undo.RecordObject(panel,"Wind detail room");panel.sizeDelta=new Vector2(470,138);
                var turn=hud.turnLabel.rectTransform;turn.sizeDelta=new Vector2(450,30);
                var wind=hud.windLabel.rectTransform;wind.sizeDelta=new Vector2(360,28);
                var detail=panel.Find("Wind relative to gun");
                if(detail==null){var go=new GameObject("Wind relative to gun",typeof(RectTransform),typeof(TextMeshProUGUI));go.transform.SetParent(panel,false);detail=go.transform;}
                var label=detail.GetComponent<TextMeshProUGUI>();label.font=hud.windLabel.font;label.fontSize=16;label.color=new Color(.78f,.83f,.75f);label.raycastTarget=false;label.alignment=TextAlignmentOptions.Center;
                var rect=label.rectTransform;rect.anchorMin=rect.anchorMax=Vector2.up;rect.pivot=Vector2.up;rect.anchoredPosition=new Vector2(10,-96);rect.sizeDelta=new Vector2(450,30);hud.windDetailLabel=label;
                EditorUtility.SetDirty(hud);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            }
            AssetDatabase.SaveAssets();
        }
        static Material Material(string name,Color color)
        {
            var value=AssetDatabase.LoadAssetAtPath<Material>(Root+name+".mat");if(value==null){value=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(value,Root+name+".mat");}
            Undo.RecordObject(value,"Plant material");value.SetColor("_BaseColor",color);value.SetFloat("_Smoothness",.08f);value.enableInstancing=true;EditorUtility.SetDirty(value);return value;
        }
        static GameObject Prefab(string name,Mesh mesh,Material bark,Material green)
        {
            var saved=AssetDatabase.LoadAssetAtPath<Mesh>(Root+name+".asset");if(saved==null){saved=mesh;AssetDatabase.CreateAsset(saved,Root+name+".asset");}else{EditorUtility.CopySerialized(mesh,saved);UnityEngine.Object.DestroyImmediate(mesh);}
            var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));
            try{go.GetComponent<MeshFilter>().sharedMesh=saved;go.GetComponent<MeshRenderer>().sharedMaterials=new[]{bark,green};return PrefabUtility.SaveAsPrefabAsset(go,Root+name+".prefab");}
            finally{UnityEngine.Object.DestroyImmediate(go);}
        }
        sealed class Geometry
        {
            public readonly List<Vector3> v=new List<Vector3>();public readonly List<int>[] triangles={new List<int>(),new List<int>()};
            public void Triangle(Vector3 a,Vector3 b,Vector3 c,int material){int n=v.Count;v.Add(a);v.Add(b);v.Add(c);triangles[material].Add(n);triangles[material].Add(n+1);triangles[material].Add(n+2);}
            public void Branch(Vector3 a,Vector3 b,float radius,int sides=6)
            {
                var forward=(b-a).normalized;var right=Vector3.Cross(forward,Vector3.forward).normalized;if(right.sqrMagnitude<.01f)right=Vector3.right;var up=Vector3.Cross(forward,right);
                for(int i=0;i<sides;i++){float t=i*Mathf.PI*2/sides,u=(i+1)*Mathf.PI*2/sides;var p=(right*Mathf.Cos(t)+up*Mathf.Sin(t))*radius;var q=(right*Mathf.Cos(u)+up*Mathf.Sin(u))*radius;Triangle(a+p,a+q,b+q*.45f,0);Triangle(a+p,b+q*.45f,b+p*.45f,0);}
            }
            public Mesh Finish(string name){var mesh=new Mesh{name=name};if(v.Count>65535)mesh.indexFormat=UnityEngine.Rendering.IndexFormat.UInt32;mesh.SetVertices(v);mesh.subMeshCount=2;mesh.SetTriangles(triangles[0],0);mesh.SetTriangles(triangles[1],1);mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;}
        }
        static Mesh CreateTree()
        {
            var geo=new Geometry();var random=new System.Random(981);geo.Branch(Vector3.zero,new Vector3(.13f,6,0),.14f,9);
            for(int tier=0;tier<9;tier++)for(int arm=0;arm<6;arm++)
            {
                float angle=arm*Mathf.PI*2/6+tier*1.71f+(float)random.NextDouble()*.3f;var outward=new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle));var side=Vector3.Cross(outward,Vector3.up);
                float length=(1-tier/11f)*(1.1f+(float)random.NextDouble()*.6f);var start=new Vector3(0,1.4f+tier*.48f,0);var end=start+outward*length+Vector3.up*.3f;geo.Branch(start,end,.035f);
                // Narrow needle sprays follow irregular side shoots, avoiding stacked cone silhouettes.
                for(int twig=1;twig<=5;twig++)
                {
                    var center=Vector3.Lerp(start,end,twig/6f);float span=(1-twig/7f)*.55f;
                    for(int sign=-1;sign<=1;sign+=2){var tip=center+side*sign*span+outward*.25f+Vector3.up*.1f;geo.Branch(center,tip,.012f,4);
                        for(int needle=0;needle<6;needle++){var p=Vector3.Lerp(center,tip,needle/6f);var d=(outward+side*sign*.35f+Vector3.up*((float)random.NextDouble()-.15f)).normalized;float size=.22f+(float)random.NextDouble()*.18f;geo.Triangle(p-side*.045f,p+d*size,p+side*.045f,1);geo.Triangle(p-Vector3.up*.035f,p+d*size,p+Vector3.up*.035f,1);}}
                }
            }
            return geo.Finish("Original branching mountain pine");
        }
        static Mesh CreateShrub()
        {
            var geo=new Geometry();var random=new System.Random(744);
            for(int branch=0;branch<14;branch++)
            {
                float a=branch*2.399f;var tip=new Vector3(Mathf.Cos(a)*.55f,.45f+(float)random.NextDouble()*.5f,Mathf.Sin(a)*.55f);geo.Branch(Vector3.zero,tip,.025f,5);
                for(int leaf=0;leaf<22;leaf++){float t=.25f+(float)random.NextDouble()*.75f;var p=tip*t;float b=(float)random.NextDouble()*Mathf.PI*2;var d=new Vector3(Mathf.Cos(b),.2f,Mathf.Sin(b))*.2f;var side=Vector3.Cross(d,Vector3.up).normalized*.075f;geo.Triangle(p,p+d*.5f+side,p+d,1);geo.Triangle(p,p+d,p+d*.5f-side,1);}
            }
            return geo.Finish("Original wild shrub");
        }
    }
}
