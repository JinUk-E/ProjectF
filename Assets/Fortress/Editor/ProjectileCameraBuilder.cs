using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;

namespace Fortress.Editor
{
    public static class ProjectileCameraBuilder
    {
        public const string SettingsPath="Assets/Fortress/Data/ProjectileCameraSettings.asset";

        [MenuItem("Fortress/Upgrade Projectile Follow Camera")]
        public static void Upgrade()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Stop Play before installing the projectile camera.");
            var settings=AssetDatabase.LoadAssetAtPath<ProjectileCameraSettings>(SettingsPath);
            if(settings==null)
            {
                settings=ScriptableObject.CreateInstance<ProjectileCameraSettings>();
                AssetDatabase.CreateAsset(settings,SettingsPath);
            }
            var paths=new[]{SessionMenu.LocalScene,SessionMenu.AiScene};
            try
            {
                for(int i=0;i<paths.Length;i++)
                {
                    if(EditorUtility.DisplayCancelableProgressBar("Projectile camera",paths[i],(float)i/paths.Length))
                        throw new OperationCanceledException();
                    var scene=EditorSceneManager.OpenScene(paths[i]);
                    var camera=Camera.main;
                    var match=Object.FindFirstObjectByType<MatchController>();
                    var orbit=camera!=null?camera.GetComponent<BattlefieldOrbitCamera>():null;
                    if(camera==null || match==null || match.shots==null || orbit==null)
                        throw new InvalidOperationException("Battle camera, orbit and match must exist: "+paths[i]);
                    var follow=camera.GetComponent<ProjectileFollowCamera>();
                    if(follow==null)follow=Undo.AddComponent<ProjectileFollowCamera>(camera.gameObject);
                    Undo.RecordObject(follow,"Configure projectile follow camera");
                    follow.shots=match.shots;follow.match=match;follow.orbit=orbit;
                    follow.exterior=Object.FindFirstObjectByType<BattlefieldExterior>();
                    follow.settings=settings;follow.viewCamera=camera;
                    EditorUtility.SetDirty(follow);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
                }
                AssetDatabase.SaveAssets();EditorSceneManager.OpenScene(SessionMenu.MainScene);
            }
            finally{EditorUtility.ClearProgressBar();}
        }
    }
}
