using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;
namespace Fortress.Editor
{
    public static class TankIdentityBuilder
    {
        [MenuItem("Fortress/Upgrade Tank Identities")]
        public static void Upgrade()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play first.");
            Set("Main",1,1,1);Set("Siege",1.15f,.75f,.75f);
            Set("Excavator",.95f,14f/12,1.1f);Set("Support",.85f,16f/12,1.2f);Set("Heavy",1.3f,8f/12,.7f);
            var scene=EditorSceneManager.OpenScene(SessionMenu.MainScene);
            var picker=Object.FindFirstObjectByType<LoadoutMenu>();
            if(picker==null)throw new InvalidOperationException("Missing loadout menu.");
            Undo.RecordObject(picker,"Show tank statistics");
            picker.tuning=AssetDatabase.LoadAssetAtPath<GameTuning>("Assets/Fortress/Data/MatchTuning.asset");
            EditorUtility.SetDirty(picker);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
        }
        static void Set(string name,float health,float movement,float speed)
        {
            var role=AssetDatabase.LoadAssetAtPath<TankLoadout>("Assets/Fortress/Data/Role_"+name+".asset");
            if(role==null)throw new InvalidOperationException("Missing role "+name);
            Undo.RecordObject(role,"Tank identity");role.healthMultiplier=health;role.movementMultiplier=movement;role.speedMultiplier=speed;
            EditorUtility.SetDirty(role);
        }
    }
}
