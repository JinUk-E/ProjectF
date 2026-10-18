using System;
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityProjectile;
namespace Fortress.Editor
{
    public static class ShellArtBuilder
    {
        const string Data="Assets/Fortress/Data/";
        [Serializable] sealed class Gltf { public Accessor[] accessors;public BufferView[] bufferViews; }
        [Serializable] sealed class Accessor { public int bufferView,byteOffset,count,componentType;public string type; }
        [Serializable] sealed class BufferView { public int byteOffset,byteStride; }
        [MenuItem("Fortress/Upgrade Shell Visuals")]
        public static void Upgrade()
        {
            if(EditorApplication.isPlaying) throw new InvalidOperationException("Stop play before upgrading shell art.");
            var steel=Material("ShellSteel",new Color(.23f,.27f,.24f),.75f,.4f);
            var fuze=Material("ShellFuze",new Color(.53f,.56f,.57f),.85f,.55f);
            var brass=Material("ShellBrass",new Color(.69f,.47f,.18f),.8f,.45f);
            var tracer=AssetDatabase.LoadAssetAtPath<Material>(Data+"ShellTracer.mat");
            if(tracer==null) {tracer=new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));AssetDatabase.CreateAsset(tracer,Data+"ShellTracer.mat");}
            tracer.SetColor("_BaseColor",Color.white);Transparent(tracer);EditorUtility.SetDirty(tracer);
            var mesh=ExtractNose();
            const string path="Assets/Fortress/Prefabs/StandardShell.prefab";
            var root=PrefabUtility.LoadPrefabContents(path);
            try
            {
                // Retain runner/event relay identity and profile; replace only the old sphere view.
                var filter=root.GetComponent<MeshFilter>();if(filter!=null) UnityEngine.Object.DestroyImmediate(filter);
                var renderer=root.GetComponent<MeshRenderer>();if(renderer!=null) UnityEngine.Object.DestroyImmediate(renderer);
                var previous=root.transform.Find("Shell Visual");if(previous!=null) UnityEngine.Object.DestroyImmediate(previous.gameObject);
                var oldTrail=root.transform.Find("Shell Trail");if(oldTrail!=null) UnityEngine.Object.DestroyImmediate(oldTrail.gameObject);
                root.transform.localScale=Vector3.one;
                var visual=new GameObject("Shell Visual");visual.transform.SetParent(root.transform,false);
                var nose=new GameObject("Polyfork projectile nose",typeof(MeshFilter),typeof(MeshRenderer));nose.transform.SetParent(visual.transform,false);
                nose.GetComponent<MeshFilter>().sharedMesh=mesh;nose.GetComponent<MeshRenderer>().sharedMaterials=new[]{steel,fuze};
                Cylinder(visual.transform,"Steel body",-.28f,.26f,.153f,steel);
                Cylinder(visual.transform,"Brass driving band",-.34f,.075f,.163f,brass);
                var trailGo=new GameObject("Shell Trail",typeof(TrailRenderer));trailGo.transform.SetParent(root.transform,false);
                var trail=trailGo.GetComponent<TrailRenderer>();trail.sharedMaterial=tracer;trail.time=.14f;trail.minVertexDistance=.08f;
                trail.widthCurve=AnimationCurve.Linear(0,.15f,1,0);trail.numCapVertices=3;trail.emitting=false;trail.autodestruct=false;
                trail.shadowCastingMode=ShadowCastingMode.Off;trail.receiveShadows=false;
                var gradient=new Gradient();gradient.SetKeys(new[]{new GradientColorKey(new Color(1,.7f,.25f),0),new GradientColorKey(new Color(.8f,.24f,.04f),1)},new[]{new GradientAlphaKey(.8f,0),new GradientAlphaKey(0,1)});trail.colorGradient=gradient;
                var view=root.GetComponent<ShellVisual>();if(view==null)view=root.AddComponent<ShellVisual>();
                view.runner=root.GetComponent<ProjectileRunner>();view.visualRoot=visual.transform;view.trail=trail;
                PrefabUtility.SaveAsPrefabAsset(root,path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            foreach(var scenePath in new[]{MatchSceneBuilder.ScenePath,AiMatchBuilder.ScenePath})
            {
                var scene=EditorSceneManager.OpenScene(scenePath);var match=UnityEngine.Object.FindAnyObjectByType<MatchController>();
                var dust=match.shots.impactEffect;ConfigureDust(dust);
                var presentation=UnityEngine.Object.FindAnyObjectByType<ShotPresentation>();
                if(presentation!=null && presentation.impactFlash!=null)
                { var flash=presentation.impactFlash.main;flash.startLifetime=.12f;flash.startSize=1.8f; }
                EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            }
            AssetDatabase.SaveAssets();Debug.Log("Metal shell, short tracer and impact dust installed.");
        }
        static Mesh ExtractNose()
        {
            var bytes=File.ReadAllBytes("Assets/ThirdParty/PolyforkShell/artillery-shell.glb");
            int length=BitConverter.ToInt32(bytes,12),binary=28+length;
            var json=JsonUtility.FromJson<Gltf>(System.Text.Encoding.UTF8.GetString(bytes,20,length));
            var pos=json.accessors[0];var normals=json.accessors[2];
            if(pos.componentType!=5126||pos.type!="VEC3"||pos.count%3!=0) throw new InvalidDataException("Unexpected publisher mesh layout.");
            var vertices=new List<Vector3>();var directions=new List<Vector3>();var body=new List<int>();var tip=new List<int>();
            for(int i=0;i<pos.count;i+=3)
            {
                var a=Read(bytes,binary,json,pos,i);var b=Read(bytes,binary,json,pos,i+1);var c=Read(bytes,binary,json,pos,i+2);
                if(Mathf.Min(a.y,Mathf.Min(b.y,c.y))<.3219f) continue;
                var indices=(a.y+b.y+c.y)/3>.545f?tip:body;
                for(int j=0;j<3;j++)
                {
                    var p=Read(bytes,binary,json,pos,i+j);var n=Read(bytes,binary,json,normals,i+j);
                    vertices.Add(new Vector3(p.x,-p.z,p.y-.322f)*2.6f+Vector3.back*.15f);
                    directions.Add(new Vector3(n.x,-n.z,n.y));
                }
                int first=vertices.Count-3;indices.Add(first);indices.Add(first+1);indices.Add(first+2);
            }
            var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(Data+"ShellNose.asset");
            if(mesh==null) {mesh=new Mesh();AssetDatabase.CreateAsset(mesh,Data+"ShellNose.asset");} else mesh.Clear();
            mesh.name="Polyfork projectile nose (+Z)";mesh.SetVertices(vertices);mesh.SetNormals(directions);mesh.subMeshCount=2;mesh.SetTriangles(body,0);mesh.SetTriangles(tip,1);mesh.RecalculateBounds();EditorUtility.SetDirty(mesh);return mesh;
        }
        static Vector3 Read(byte[] bytes,int binary,Gltf gltf,Accessor a,int i)
        {var v=gltf.bufferViews[a.bufferView];int o=binary+v.byteOffset+a.byteOffset+i*(v.byteStride==0?12:v.byteStride);return new Vector3(BitConverter.ToSingle(bytes,o),BitConverter.ToSingle(bytes,o+4),BitConverter.ToSingle(bytes,o+8));}
        static void Cylinder(Transform parent,string name,float z,float length,float radius,Material material)
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Cylinder);go.name=name;go.transform.SetParent(parent,false);
            go.transform.localPosition=new Vector3(0,0,z);go.transform.localRotation=Quaternion.Euler(90,0,0);go.transform.localScale=new Vector3(radius*2,length*.5f,radius*2);
            UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());go.GetComponent<Renderer>().sharedMaterial=material;
        }
        static Material Material(string name,Color color,float metallic,float smoothness)
        {var m=AssetDatabase.LoadAssetAtPath<Material>(Data+name+".mat");if(m==null){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,Data+name+".mat");}m.SetColor("_BaseColor",color);m.SetFloat("_Metallic",metallic);m.SetFloat("_Smoothness",smoothness);EditorUtility.SetDirty(m);return m;}
        static void Transparent(Material material)
        {material.SetFloat("_Surface",1);material.SetFloat("_SrcBlend",(float)BlendMode.SrcAlpha);material.SetFloat("_DstBlend",(float)BlendMode.OneMinusSrcAlpha);material.SetFloat("_ZWrite",0);material.SetFloat("_Cull",0);material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");material.renderQueue=(int)RenderQueue.Transparent;}
        static void ConfigureDust(ParticleSystem effect)
        {
            if(effect==null) return;
            effect.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            var main=effect.main;main.loop=false;main.playOnAwake=false;main.simulationSpace=ParticleSystemSimulationSpace.World;
            main.startLifetime=new ParticleSystem.MinMaxCurve(.8f,1.6f);main.startSpeed=new ParticleSystem.MinMaxCurve(2,5);main.startSize=new ParticleSystem.MinMaxCurve(.5f,1.4f);main.startColor=new Color(.39f,.32f,.23f,.8f);main.gravityModifier=.18f;
            var emission=effect.emission;emission.rateOverTime=0;emission.SetBursts(new[]{new ParticleSystem.Burst(0,30)});
            var shape=effect.shape;shape.shapeType=ParticleSystemShapeType.Hemisphere;shape.radius=.5f;
            var size=effect.sizeOverLifetime;size.enabled=true;size.size=new ParticleSystem.MinMaxCurve(1,AnimationCurve.Linear(0,.6f,1,1.8f));
            var color=effect.colorOverLifetime;color.enabled=true;var gradient=new Gradient();gradient.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1)},new[]{new GradientAlphaKey(1,0),new GradientAlphaKey(0,1)});color.color=gradient;
            effect.GetComponent<ParticleSystemRenderer>().sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/Fortress/Art/Military/DustSmoke.mat");
            EditorUtility.SetDirty(effect);
        }
    }
}
