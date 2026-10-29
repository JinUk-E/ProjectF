using System;
using UnityEditor;
using UnityEngine;
namespace Fortress.Editor
{
    public static class TacticalAiBuilder
    {
        [MenuItem("Fortress/Upgrade Tactical AI")]
        public static void Upgrade()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play first.");
            foreach(var name in new[]{"Easy","Normal","Hard"})
            {
                var settings=AssetDatabase.LoadAssetAtPath<AiSettings>("Assets/Fortress/Data/Ai"+name+".asset");
                if(settings==null)throw new InvalidOperationException("Missing AI settings "+name);
                Undo.RecordObject(settings,"Tactical AI movement");
                settings.enableMovement=true;settings.moveCandidates=16;settings.maxMoveDistance=6;settings.maxMoveSeconds=3;
                settings.rangePreference=.08f;EditorUtility.SetDirty(settings);
            }
            AssetDatabase.SaveAssets();
        }
    }
}
