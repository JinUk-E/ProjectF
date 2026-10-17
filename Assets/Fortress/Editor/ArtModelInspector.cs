using System;
using UnityEditor;
using UnityEngine;
namespace Fortress.Editor
{
    public static class ArtModelInspector
    {
        public static void InspectBatch()
        {
            try
            {
                foreach(var path in AssetDatabase.FindAssets("t:Model",new[]{"Assets/ThirdParty/QuaterniusTanks"}))
                {
                    var model=AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(path));
                    var instance=UnityEngine.Object.Instantiate(model);
                    Debug.Log("ART MODEL "+model.name);
                    foreach(var t in instance.GetComponentsInChildren<Transform>())
                        Debug.Log("ART NODE "+t.name+" parent="+(t.parent!=null?t.parent.name:"none")+" local="+t.localPosition+" world="+t.position+" rot="+t.localEulerAngles+" scale="+t.localScale);
                    foreach(var r in instance.GetComponentsInChildren<Renderer>())
                    {
                        string materials="";foreach(var m in r.sharedMaterials)materials+=(m!=null?m.name:"null")+",";
                        Debug.Log("ART RENDERER "+r.name+" type="+r.GetType().Name+" bounds="+r.bounds+" materials="+materials);
                    }
                    UnityEngine.Object.DestroyImmediate(instance);
                }
                EditorApplication.Exit(0);
            }
            catch(Exception e) { Debug.LogException(e);EditorApplication.Exit(1); }
        }
    }
}
