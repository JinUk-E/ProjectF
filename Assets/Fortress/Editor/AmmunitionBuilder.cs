using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityProjectile;
namespace Fortress.Editor
{
    public static class AmmunitionBuilder
    {
        const string Data="Assets/Fortress/Data/";
        static TMP_FontAsset font;
        static readonly Color Panel=new Color(.07f,.095f,.087f,.97f),Accent=new Color(.72f,.78f,.51f);
        [MenuItem("Fortress/Upgrade Ammunition and Tank Roles")]
        public static void Upgrade()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode) throw new System.InvalidOperationException("Stop Play mode first.");
            font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
            var standard=AssetDatabase.LoadAssetAtPath<ProjectileRunner>("Assets/Fortress/Prefabs/StandardShell.prefab");
            var child=Prefab("ClusterBomblet",standard,standard.Profile,.65f);
            var fuse=Asset<AltitudeFuse>("ClusterAltitudeFuse");fuse.height=12;EditorUtility.SetDirty(fuse);
            var scatter=Asset<ScatterPayload>("ClusterScatter");scatter.submunitionPrefab=child;scatter.count=6;scatter.mode=ScatterPayload.ScatterMode.Physical;
            scatter.spreadAngle=110;scatter.ejectDirection=Vector3.up;scatter.ejectSpeed=new Vector2(5,9);scatter.ejectOffset=.5f;scatter.maxFlightTime=10;scatter.fallbackGravity=9.81f;EditorUtility.SetDirty(scatter);
            var profile=Asset<ProjectileProfile>("Profile_Cluster");profile.flight=standard.Profile.flight;profile.fuse=fuse;profile.payload=scatter;profile.lingerTime=.2f;profile.impactSoonLead=standard.Profile.impactSoonLead;EditorUtility.SetDirty(profile);
            var carrier=Prefab("ClusterCarrier",standard,profile,1.2f);
            var he=Ammo("Ammo_HE","HIGH EXPLOSIVE",AmmoPurpose.HighExplosive,standard,30,28,4,.55f,.15f);
            var drill=Ammo("Ammo_Drill","DRILL",AmmoPurpose.Excavation,standard,12,12,3.5f,1.8f,.25f);
            var shatter=Ammo("Ammo_Shatter","SHATTER",AmmoPurpose.Demolition,standard,18,90,3,.3f,.1f);
            var cluster=Ammo("Ammo_Cluster","CLUSTER",AmmoPurpose.Cluster,carrier,9,9,1.5f,.18f,.08f);
            var roles=new[]{Role("Role_Main","MAIN BATTLE",he,null,0),Role("Role_Siege","SIEGE",he,shatter,3),Role("Role_Excavator","EXCAVATOR",he,drill,3),Role("Role_Support","SUPPORT",he,cluster,2)};
            foreach(var path in new[]{SessionMenu.AiScene,SessionMenu.LocalScene})
            {
                var scene=EditorSceneManager.OpenScene(path);
                var match=Object.FindFirstObjectByType<MatchController>();match.shots.availableLoadouts=roles;match.shots.ai=Object.FindFirstObjectByType<AiOpponent>();
                var hud=Object.FindFirstObjectByType<MatchHud>();var old=hud.transform.Find("Ammunition");if(old!=null) Object.DestroyImmediate(old.gameObject);
                var panel=Box(hud.transform,"Ammunition",0,0,390,190);panel.anchorMin=panel.anchorMax=panel.pivot=Vector2.right;panel.anchoredPosition=new Vector2(-36,32);
                panel.gameObject.SetActive(false);var ui=panel.gameObject.AddComponent<AmmoHud>();ui.shots=match.shots;
                ui.roleLabel=Label(panel,14,10,360,24,"AMMUNITION",17);
                ui.basicButton=Button(panel,14,42,362,48,out ui.basicLabel);
                ui.specialButton=Button(panel,14,98,362,48,out ui.specialLabel);
                Label(panel,14,157,360,24,"[ 1 / 2 ] SELECT    [ SPACE ] FIRE",17);
                panel.gameObject.SetActive(true);EditorUtility.SetDirty(match.shots);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            }
            var main=EditorSceneManager.OpenScene(SessionMenu.MainScene);
            var menu=Object.FindFirstObjectByType<SessionMenu>();var composition=menu.transform.Find("Command composition");
            var previous=composition.Find("Unit selection");if(previous!=null) Object.DestroyImmediate(previous.gameObject);
            var picker=Box(composition,"Unit selection",1010,548,760,340);picker.gameObject.SetActive(false);
            var selection=picker.gameObject.AddComponent<LoadoutMenu>();selection.roles=roles;
            Label(picker,24,18,710,40,"CHOOSE YOUR UNIT",25);
            var player=Button(picker,24,74,712,100,out selection.playerLabel);selection.playerLabel.fontSize=22;
            var opponent=Button(picker,24,194,712,100,out selection.opponentLabel);selection.opponentLabel.fontSize=22;
            UnityEventTools.AddPersistentListener(player.onClick,selection.NextPlayer);UnityEventTools.AddPersistentListener(opponent.onClick,selection.NextOpponent);
            picker.gameObject.SetActive(true);EditorSceneManager.MarkSceneDirty(main);EditorSceneManager.SaveScene(main);AssetDatabase.SaveAssets();
        }
        static T Asset<T>(string name) where T:ScriptableObject
        { var result=AssetDatabase.LoadAssetAtPath<T>(Data+name+".asset");if(result==null) { result=ScriptableObject.CreateInstance<T>();AssetDatabase.CreateAsset(result,Data+name+".asset"); }return result; }
        static AmmoDefinition Ammo(string file,string title,AmmoPurpose purpose,ProjectileRunner prefab,float damage,float building,float radius,float depth,float rim)
        { var a=Asset<AmmoDefinition>(file);a.displayName=title;a.purpose=purpose;a.projectilePrefab=prefab;a.damage=damage;a.buildingDamage=building;a.radius=radius;a.depth=depth;a.rimHeight=rim;EditorUtility.SetDirty(a);return a; }
        static TankLoadout Role(string file,string title,AmmoDefinition basic,AmmoDefinition special,int rounds)
        { var a=Asset<TankLoadout>(file);a.displayName=title;a.basic=basic;a.special=special;a.specialRounds=rounds;EditorUtility.SetDirty(a);return a; }
        static ProjectileRunner Prefab(string name,ProjectileRunner source,ProjectileProfile profile,float scale)
        { var go=Object.Instantiate(source.gameObject);go.name=name;go.transform.localScale=source.transform.localScale*scale;go.GetComponent<ProjectileRunner>().Profile=profile;var saved=PrefabUtility.SaveAsPrefabAsset(go,"Assets/Fortress/Prefabs/"+name+".prefab");Object.DestroyImmediate(go);return saved.GetComponent<ProjectileRunner>(); }
        static RectTransform Box(Transform parent,string name,float x,float y,float w,float h)
        { var go=new GameObject(name,typeof(RectTransform),typeof(Image));go.transform.SetParent(parent,false);var r=go.GetComponent<RectTransform>();r.anchorMin=r.anchorMax=r.pivot=Vector2.up;r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);go.GetComponent<Image>().color=Panel;return r; }
        static TMP_Text Label(Transform parent,float x,float y,float w,float h,string text,float size)
        { var go=new GameObject("Label",typeof(RectTransform),typeof(TextMeshProUGUI));go.transform.SetParent(parent,false);var t=go.GetComponent<TextMeshProUGUI>();t.rectTransform.anchorMin=t.rectTransform.anchorMax=t.rectTransform.pivot=Vector2.up;t.rectTransform.anchoredPosition=new Vector2(x,-y);t.rectTransform.sizeDelta=new Vector2(w,h);t.font=font;t.fontSize=size;t.text=text;t.color=Accent;t.raycastTarget=false;t.alignment=TextAlignmentOptions.MidlineLeft;return t; }
        static Button Button(Transform parent,float x,float y,float w,float h,out TMP_Text label)
        { var r=Box(parent,"Select ammunition",x,y,w,h);var image=r.GetComponent<Image>();image.color=new Color(.15f,.2f,.16f);var b=r.gameObject.AddComponent<Button>();b.targetGraphic=image;label=Label(r,12,0,w-24,h,"",17);return b; }
    }
}
