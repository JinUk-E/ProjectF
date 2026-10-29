using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Fortress.Editor
{
    public static class TankAppearanceBuilder
    {
        const string Folder="Assets/Fortress/Art/TankRoles/";
        static readonly string[] Roles={"Main","Siege","Excavator","Support"};
        static readonly string[] Models={"Tank2","Tank4","Tank3","Tank"};
        static readonly string[] Descriptions={"Versatile frontline tank. Reliable HE fire with no ammunition limit.","Heavy siege cannon. SHATTER rounds break cover and buildings.","Terrain specialist. DRILL rounds dig deeper to disrupt enemy footing.","Area support tank. CLUSTER rounds spread lighter blasts over a wide area."};
        sealed class Piece { public Mesh mesh; public int mount; public Material[] materials; }
        [MenuItem("Fortress/Rebuild Tank Visual Assets Only")]
        public static void RebuildVisuals()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play first.");
            for(int i=0;i<4;i++)Build(i);
            AssetDatabase.SaveAssets();
        }
        [MenuItem("Fortress/Upgrade Tank Appearances")]
        public static void Upgrade()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play first.");
            Directory.CreateDirectory(Folder);AssetDatabase.Refresh();
            for(int i=0;i<4;i++)Build(i);
            foreach(var path in new[]{SessionMenu.LocalScene,SessionMenu.AiScene})
            {
                var scene=EditorSceneManager.OpenScene(path);
                var shots=UnityEngine.Object.FindFirstObjectByType<FlatShotController>();
                var presentation=UnityEngine.Object.FindFirstObjectByType<ShotPresentation>();
                for(int i=0;i<shots.tanks.Length;i++)
                {
                    var rig=shots.tanks[i];var view=rig.GetComponent<TankAppearanceView>();
                    if(view==null)view=rig.gameObject.AddComponent<TankAppearanceView>();
                    view.shots=shots;view.rig=rig;view.tankIndex=i;view.gunParent=presentation.gunVisuals[i];
                }
                EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            }
            AssetDatabase.SaveAssets();TankSelectionBuilder.Upgrade();
        }
        static void Build(int role)
        {
            var sourcePath="Assets/ThirdParty/QuaterniusTanks/"+Models[role]+".fbx";
            var source=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(sourcePath));
            source.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);
            var pieces=new List<Piece>();
            try
            {
                foreach(var renderer in source.GetComponentsInChildren<Renderer>())
                {
                    Mesh mesh;
                    if(renderer is SkinnedMeshRenderer skin){mesh=new Mesh();skin.BakeMesh(mesh);}
                    else {var filter=renderer.GetComponent<MeshFilter>();if(filter==null)continue;mesh=UnityEngine.Object.Instantiate(filter.sharedMesh);}
                    // BakeMesh already includes the FBX renderer's scale. Applying it again
                    // makes geometry 100x too large relative to the left/right track offsets.
                    var toWorld=renderer is SkinnedMeshRenderer
                        ?Matrix4x4.TRS(renderer.transform.position,renderer.transform.rotation,Vector3.one)
                        :renderer.localToWorldMatrix;
                    var matrix=Matrix4x4.Rotate(Quaternion.Euler(0,-90,0))*toWorld;
                    var vertices=mesh.vertices;for(int v=0;v<vertices.Length;v++)vertices[v]=matrix.MultiplyPoint3x4(vertices[v]);
                    mesh.vertices=vertices;mesh.RecalculateBounds();mesh.name=Roles[role]+"_"+renderer.name;
                    pieces.Add(new Piece{mesh=mesh,mount=renderer.name.Contains("Gun")?2:renderer.name.Contains("Turret")?1:0,materials=renderer.sharedMaterials});
                }
                for(int group=0;group<3;group++)
                {
                    bool first=true;var bounds=new Bounds();
                    foreach(var p in pieces)if(p.mount==group){if(first){bounds=p.mesh.bounds;first=false;}else bounds.Encapsulate(p.mesh.bounds);}
                    var turretSizes=new[]{new Vector3(2.8f,.75f,3),new Vector3(1.7f,1.15f,2.1f),new Vector3(2.4f,1.3f,2.6f),new Vector3(2.6f,1.1f,2.8f)};
                    float diameter=role==1?.58f:role==2?.4f:.32f;
                    var size=group==0?new Vector3(3.8f,1.5f,4.4f):group==1?turretSizes[role]:new Vector3(diameter,diameter,3.8f);
                    var center=group==0?new Vector3(0,.75f,0):group==1?new Vector3(0,.05f,0):new Vector3(0,0,1.9f);
                    var scale=new Vector3(size.x/bounds.size.x,size.y/bounds.size.y,size.z/bounds.size.z);
                    foreach(var p in pieces)if(p.mount==group){var vertices=p.mesh.vertices;for(int v=0;v<vertices.Length;v++)vertices[v]=Vector3.Scale(vertices[v]-bounds.center,scale)+center;p.mesh.vertices=vertices;p.mesh.RecalculateNormals();p.mesh.RecalculateTangents();p.mesh.RecalculateBounds();}
                }
                string path=Folder+Roles[role]+".asset";
                var appearance=AssetDatabase.LoadAssetAtPath<TankAppearance>(path);
                if(appearance==null){appearance=ScriptableObject.CreateInstance<TankAppearance>();AssetDatabase.CreateAsset(appearance,path);}
                appearance.description=Descriptions[role];appearance.sourceModel=sourcePath;
                var output=new List<TankVisualPiece>();
                foreach(var p in pieces)
                {
                    var meshPath=Folder+p.mesh.name+".asset";var saved=AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
                    if(saved==null){saved=p.mesh;AssetDatabase.CreateAsset(saved,meshPath);}else {EditorUtility.CopySerialized(p.mesh,saved);saved.vertices=p.mesh.vertices;EditorUtility.SetDirty(saved);UnityEngine.Object.DestroyImmediate(p.mesh);}
                    p.mesh=null;
                    var a=new Material[p.materials.Length];var b=new Material[a.Length];
                    for(int m=0;m<a.Length;m++){string slot=p.materials[m].name;a[m]=Material(0,slot);b[m]=Material(1,slot);}
                    output.Add(new TankVisualPiece{mesh=saved,mount=p.mount,playerMaterials=a,opponentMaterials=b});
                }
                appearance.pieces=output.ToArray();appearance.portrait=Portrait(role,appearance);EditorUtility.SetDirty(appearance);
                var loadout=AssetDatabase.LoadAssetAtPath<TankLoadout>("Assets/Fortress/Data/Role_"+Roles[role]+".asset");loadout.appearance=appearance;EditorUtility.SetDirty(loadout);
            }
            finally {foreach(var p in pieces)if(p.mesh!=null)UnityEngine.Object.DestroyImmediate(p.mesh);UnityEngine.Object.DestroyImmediate(source);}
        }
        static Material Material(int side,string slot)
        {
            string path=Folder+"Side"+side+"_"+slot+".mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(mat!=null)return mat; // Preserve the current art direction when rebuilding geometry.
            if(mat==null){mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(mat,path);}
            var color=side==0?new Color(.25f,.29f,.17f):new Color(.48f,.41f,.28f);
            bool metal=slot.Contains("Details")||slot.Contains("Wheels");
            if(metal)color=new Color(.1f,.115f,.1f);else if(slot.Contains("Dark"))color*=.65f;else if(slot.Contains("Light"))color*=1.18f;
            color.a=1;mat.color=color;mat.SetFloat("_Metallic",metal?.65f:.25f);mat.SetFloat("_Smoothness",metal?.32f:.18f);EditorUtility.SetDirty(mat);return mat;
        }
        static Sprite Portrait(int role,TankAppearance appearance)
        {
            var scene=EditorSceneManager.NewPreviewScene();var old=RenderTexture.active;
            var rt=new RenderTexture(1024,512,24);var texture=new Texture2D(1024,512,TextureFormat.RGB24,false);
            try
            {
                var root=new GameObject("Tank portrait");SceneManager.MoveGameObjectToScene(root,scene);
                foreach(var piece in appearance.pieces)
                {
                    var go=new GameObject(piece.mesh.name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(root.transform,false);
                    go.transform.localPosition=piece.mount==0?Vector3.zero:new Vector3(0,1.8f,0);
                    go.GetComponent<MeshFilter>().sharedMesh=piece.mesh;go.GetComponent<MeshRenderer>().sharedMaterials=piece.playerMaterials;
                }
                var cameraGo=new GameObject("Portrait camera",typeof(Camera));SceneManager.MoveGameObjectToScene(cameraGo,scene);
                var camera=cameraGo.GetComponent<Camera>();camera.scene=scene;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.075f,.105f,.095f);
                camera.transform.position=new Vector3(8,5,10);camera.transform.LookAt(new Vector3(0,1.1f,.4f));camera.orthographic=true;camera.orthographicSize=2.35f;camera.nearClipPlane=.1f;camera.farClipPlane=50;camera.targetTexture=rt;
                foreach(var angle in new[]{new Vector3(40,-35,0),new Vector3(30,140,0)})
                {var lightGo=new GameObject("Studio light",typeof(Light));SceneManager.MoveGameObjectToScene(lightGo,scene);var light=lightGo.GetComponent<Light>();light.type=LightType.Directional;light.intensity=1.5f;lightGo.transform.eulerAngles=angle;}
                // The first render after a domain reload can only initialize the URP pipeline.
                camera.Render();camera.Render();RenderTexture.active=rt;texture.ReadPixels(new Rect(0,0,1024,512),0,0);texture.Apply();
                string path=Folder+Roles[role]+"Portrait.png";File.WriteAllBytes(path,texture.EncodeToPNG());AssetDatabase.ImportAsset(path);
                var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.mipmapEnabled=false;importer.alphaIsTransparency=false;importer.SaveAndReimport();
                return AssetDatabase.LoadAssetAtPath<Sprite>(path);
            }
            finally {RenderTexture.active=old;rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(texture);EditorSceneManager.ClosePreviewScene(scene);}
        }
    }
}
