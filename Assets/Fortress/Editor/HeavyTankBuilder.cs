using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Fortress.Editor
{
    /// <summary>Blender interchange uses Unity-local mount coordinates, avoiding FBX axis/pivot ambiguity.</summary>
    public static class HeavyTankBuilder
    {
        const string Folder="Assets/Fortress/Art/TankRoles/";
        const string Source="ArtSource/HeavyTank/";
        [Serializable] public class Geometry { public Piece[] pieces; }
        [Serializable] public class Piece
        {
            public string name; public int mount; public float[] vertices,normals,uv;
            public Faces[] submeshes; public string[] materials;
        }
        [Serializable] public class Faces { public int[] triangles; }

        [MenuItem("Fortress/Heavy Tank/1 Export Main For Blender")]
        public static void ExportBase()
        {
            Directory.CreateDirectory(Source);
            var source=AssetDatabase.LoadAssetAtPath<TankAppearance>(Folder+"Main.asset");
            var pieces=new List<Piece>();
            foreach(var p in source.pieces)
            {
                var m=p.mesh; var item=new Piece{name=m.name,mount=p.mount,vertices=Flatten(m.vertices),normals=Flatten(m.normals),uv=new float[m.uv.Length*2],submeshes=new Faces[m.subMeshCount],materials=new string[m.subMeshCount]};
                for(int i=0;i<m.uv.Length;i++){item.uv[i*2]=m.uv[i].x;item.uv[i*2+1]=m.uv[i].y;}
                for(int i=0;i<m.subMeshCount;i++){item.submeshes[i]=new Faces{triangles=m.GetTriangles(i)};item.materials[i]=p.playerMaterials[i].name.Replace("Side0_","");}
                pieces.Add(item);
            }
            File.WriteAllText(Source+"main-source.json",JsonUtility.ToJson(new Geometry{pieces=pieces.ToArray()}));
        }
        static float[] Flatten(Vector3[] vectors)
        {
            var result=new float[vectors.Length*3];for(int i=0;i<vectors.Length;i++){result[3*i]=vectors[i].x;result[3*i+1]=vectors[i].y;result[3*i+2]=vectors[i].z;}return result;
        }
        static Vector3[] Vectors(float[] values)
        {
            var result=new Vector3[values.Length/3];for(int i=0;i<result.Length;i++)result[i]=new Vector3(values[3*i],values[3*i+1],values[3*i+2]);return result;
        }
        [MenuItem("Fortress/Heavy Tank/2 Import Blender Derivative")]
        public static void Upgrade()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play first.");
            var data=JsonUtility.FromJson<Geometry>(File.ReadAllText(Source+"heavy-runtime.json"));
            var main=AssetDatabase.LoadAssetAtPath<TankLoadout>("Assets/Fortress/Data/Role_Main.asset");
            var appearance=AssetDatabase.LoadAssetAtPath<TankAppearance>(Folder+"Heavy.asset");
            if(appearance==null){appearance=ScriptableObject.CreateInstance<TankAppearance>();AssetDatabase.CreateAsset(appearance,Folder+"Heavy.asset");}
            Undo.RecordObject(appearance,"Import heavy tank appearance");
            var pieces=new List<TankVisualPiece>();
            try
            {
                for(int n=0;n<data.pieces.Length;n++)
                {
                    var p=data.pieces[n];
                    if(EditorUtility.DisplayCancelableProgressBar("Heavy tank",p.name,(float)n/data.pieces.Length))throw new OperationCanceledException();
                    var mesh=new Mesh{name=p.name};if(p.vertices.Length/3>65535)mesh.indexFormat=UnityEngine.Rendering.IndexFormat.UInt32;mesh.vertices=Vectors(p.vertices);mesh.normals=Vectors(p.normals);
                    var uv=new Vector2[p.uv.Length/2];for(int i=0;i<uv.Length;i++)uv[i]=new Vector2(p.uv[2*i],p.uv[2*i+1]);mesh.uv=uv;
                    mesh.subMeshCount=p.submeshes.Length;for(int i=0;i<p.submeshes.Length;i++)mesh.SetTriangles(p.submeshes[i].triangles,i);
                    mesh.RecalculateBounds();mesh.RecalculateTangents();
                    string path=Folder+p.name+".asset";var saved=AssetDatabase.LoadAssetAtPath<Mesh>(path);
                    if(saved==null){saved=mesh;AssetDatabase.CreateAsset(saved,path);}else{EditorUtility.CopySerialized(mesh,saved);UnityEngine.Object.DestroyImmediate(mesh);}
                    var a=new Material[p.materials.Length];var b=new Material[a.Length];
                    for(int i=0;i<a.Length;i++){a[i]=Material(0,p.materials[i]);b[i]=Material(1,p.materials[i]);}
                    pieces.Add(new TankVisualPiece{mesh=saved,mount=p.mount,playerMaterials=a,opponentMaterials=b});
                }
            }
            finally{EditorUtility.ClearProgressBar();}
            appearance.pieces=pieces.ToArray();appearance.sourceModel=Source+"HeavyTank.blend";
            appearance.description="주력 전차와 성능이 같은 중장갑 외형 파생형. 추가 장갑판·측면 스커트·포탑 보강.";
            var portraitPath=Folder+"HeavyPortrait.png";AssetDatabase.ImportAsset(portraitPath);
            var importer=(TextureImporter)AssetImporter.GetAtPath(portraitPath);importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.mipmapEnabled=false;importer.SaveAndReimport();
            appearance.portrait=AssetDatabase.LoadAssetAtPath<Sprite>(portraitPath);EditorUtility.SetDirty(appearance);
            var loadout=AssetDatabase.LoadAssetAtPath<TankLoadout>("Assets/Fortress/Data/Role_Heavy.asset");
            if(loadout==null){loadout=ScriptableObject.CreateInstance<TankLoadout>();AssetDatabase.CreateAsset(loadout,"Assets/Fortress/Data/Role_Heavy.asset");}
            Undo.RecordObject(loadout,"Configure heavy tank cosmetic variant");loadout.displayName="HEAVY";loadout.basic=main.basic;loadout.special=main.special;loadout.specialRounds=main.specialRounds;loadout.appearance=appearance;EditorUtility.SetDirty(loadout);
            AssetDatabase.SaveAssets();
            foreach(var path in new[]{SessionMenu.LocalScene,SessionMenu.AiScene})
            {
                var scene=EditorSceneManager.OpenScene(path);var shots=UnityEngine.Object.FindFirstObjectByType<FlatShotController>();
                var roles=new List<TankLoadout>(shots.availableLoadouts);if(!roles.Contains(loadout))roles.Add(loadout);
                Undo.RecordObject(shots,"Add heavy tank choice");shots.availableLoadouts=roles.ToArray();EditorUtility.SetDirty(shots);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            }
            CommandUiBuilder.Upgrade();
        }
        static Material Material(int side,string slot)
        {
            string path=Folder+"Side"+side+"_"+slot+".mat";var material=AssetDatabase.LoadAssetAtPath<Material>(path);if(material!=null)return material;
            material=new Material(Shader.Find("Universal Render Pipeline/Lit")){name="Side"+side+"_"+slot};
            var color=side==0?new Color(.25f,.29f,.17f):new Color(.48f,.41f,.28f);
            if(slot.Contains("Details"))color=new Color(.1f,.115f,.1f);
            else if(slot.Contains("Dark"))color*=.65f;
            else if(slot.Contains("Light"))color*=1.18f;
            else if(slot.Contains("Lens"))color=new Color(.12f,.32f,.36f);
            else if(slot.Contains("Mark"))color=new Color(.78f,.73f,.52f);
            color.a=1;material.color=color;material.SetFloat("_Metallic",.35f);material.SetFloat("_Smoothness",slot.Contains("Lens")?.7f:.22f);AssetDatabase.CreateAsset(material,path);return material;
        }
    }
}
