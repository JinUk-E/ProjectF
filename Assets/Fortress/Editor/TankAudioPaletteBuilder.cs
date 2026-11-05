using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;
namespace Fortress.Editor
{
    public static class TankAudioPaletteBuilder
    {
        const string Root="Assets/Fortress/Art/Atmosphere/";
        public static Color TeamColor(int side)=>side==0?new Color(.32f,.35f,.20f):new Color(.57f,.48f,.32f);
        [MenuItem("Fortress/Upgrade Tank Audio And Palette")]
        public static void Upgrade()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play first.");
            Directory.CreateDirectory(Root);
            var settings=AssetDatabase.LoadAssetAtPath<TankAudioSettings>("Assets/Fortress/Data/TankAudioSettings.asset");
            if(settings==null){settings=ScriptableObject.CreateInstance<TankAudioSettings>();AssetDatabase.CreateAsset(settings,"Assets/Fortress/Data/TankAudioSettings.asset");}
            Undo.RecordObject(settings,"Tank movement audio");
            settings.engineLoop=Sound("TankEngine",2,0);settings.trackLoop=Sound("TankTracks",2,1);EditorUtility.SetDirty(settings);
            var feedback=AssetDatabase.LoadAssetAtPath<CombatFeedbackSettings>(CombatFeedbackBuilder.SettingsPath);
            Undo.RecordObject(feedback,"Impact body and debris");feedback.impactAudioInterval=.055f;
            for(int i=0;i<feedback.impacts.Length;i++)
            {
                var item=feedback.impacts[i];string clipName=item.purpose==AmmoPurpose.Excavation?"Drill":item.purpose==AmmoPurpose.Demolition?"Shatter":item.purpose==AmmoPurpose.Cluster?"Cluster":"HE";
                item.clip=Sound("Feedback"+clipName,item.purpose==AmmoPurpose.Cluster?.7f:1.7f,2+(int)item.purpose);
                item.volume=item.purpose==AmmoPurpose.Cluster?.43f:.78f;item.pitch=1;feedback.impacts[i]=item;
            }
            EditorUtility.SetDirty(feedback);
            ApplyPalette();
            foreach(var path in new[]{SessionMenu.LocalScene,SessionMenu.AiScene})
            {
                var scene=EditorSceneManager.OpenScene(path);var match=Object.FindFirstObjectByType<MatchController>();
                var view=match.GetComponent<TankMovementAudio>();if(view==null)view=Undo.AddComponent<TankMovementAudio>(match.gameObject);
                Undo.RecordObject(view,"Movement sound connection");view.match=match;view.settings=settings;
                view.engine=Source(match.transform,"Engine movement audio",settings.engineLoop);
                view.tracks=Source(match.transform,"Track movement audio",settings.trackLoop);
                EditorUtility.SetDirty(view);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            }
            AssetDatabase.SaveAssets();EditorSceneManager.OpenScene(SessionMenu.MainScene);
        }
        public static void ApplyPalette()
        {
            foreach(var folder in new[]{"Assets/Fortress/Art/TankRoles","Assets/Fortress/Art/Military"})
                foreach(var guid in AssetDatabase.FindAssets("t:Material",new[]{folder}))
                {
                    var material=AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));
                    string n=material.name;if(!n.StartsWith("Side")&&!n.StartsWith("Tank0_")&&!n.StartsWith("Tank1_"))continue;
                    int side=n.StartsWith("Side0")||n.StartsWith("Tank0")?0:1;Color color=TeamColor(side);
                    if(n.Contains("Lens"))continue;
                    if(n.Contains("Details")||n.Contains("Wheels"))color=new Color(.105f,.11f,.095f);
                    else if(n.Contains("Mark"))color=new Color(.78f,.73f,.52f);
                    else if(n.Contains("Dark"))color*=.65f;
                    else if(n.Contains("Light"))color=Color.Lerp(color,new Color(.75f,.73f,.62f),.23f);
                    color.a=1;Undo.RecordObject(material,"Olive and sand armor");material.SetColor("_BaseColor",color);material.SetColor("_Color",color);EditorUtility.SetDirty(material);
                }
            var names=new[]{"Main","Siege","Excavator","Support","Heavy"};
            for(int i=0;i<names.Length;i++)
            {
                var appearance=AssetDatabase.LoadAssetAtPath<TankAppearance>("Assets/Fortress/Art/TankRoles/"+names[i]+".asset");
                if(appearance==null)continue;
                Undo.RecordObject(appearance,"Updated armor portrait");appearance.portrait=TankAppearanceBuilder.Portrait(i,appearance);EditorUtility.SetDirty(appearance);
            }
        }
        static AudioSource Source(Transform parent,string name,AudioClip clip)
        {
            var child=parent.Find(name);if(child==null){var go=new GameObject(name);Undo.RegisterCreatedObjectUndo(go,"Movement sound");go.transform.SetParent(parent,false);child=go.transform;}
            var source=child.GetComponent<AudioSource>();if(source==null)source=Undo.AddComponent<AudioSource>(child.gameObject);
            Undo.RecordObject(source,"Movement loop");source.playOnAwake=false;source.loop=true;source.spatialBlend=0;source.volume=0;source.clip=clip;EditorUtility.SetDirty(source);return source;
        }
        public static AudioClip Sound(string name,float seconds,int kind)
        {
            // Original deterministic synthesis. Loops use integer-cycle partials, so the seam is periodic.
            const int rate=22050;int count=(int)(rate*seconds);var samples=new float[count];var random=new System.Random(715+kind);float low=0,mid=0;
            var phases=new float[32];for(int j=0;j<phases.Length;j++)phases[j]=(float)random.NextDouble()*Mathf.PI*2;
            for(int i=0;i<count;i++)
            {
                float t=(float)i/rate,v;
                if(kind<2)
                {
                    float noise=0;for(int j=0;j<32;j++)noise+=Mathf.Sin(2*Mathf.PI*(173+j*83)*t+phases[j])/32;
                    if(kind==0)v=(Mathf.Sin(2*Mathf.PI*62*t)*.19f+Mathf.Sin(2*Mathf.PI*124*t)*.13f+Mathf.Sin(2*Mathf.PI*186*t)*.07f+noise*.12f)*( .85f+.15f*Mathf.Sin(2*Mathf.PI*14*t));
                    else{float pulse=Mathf.Pow(.5f+.5f*Mathf.Cos(2*Mathf.PI*16*t),14);v=noise*(.5f+2*pulse)+pulse*(Mathf.Sin(2*Mathf.PI*640*t)*.15f+Mathf.Sin(2*Mathf.PI*1380*t)*.08f);}
                }
                else
                {
                    float noise=(float)random.NextDouble()*2-1;low=Mathf.Lerp(low,noise,.075f);mid=Mathf.Lerp(mid,noise,.42f);
                    float frequency=kind==3?48:kind==4?92:kind==5?110:64;
                    float body=(Mathf.Sin(2*Mathf.PI*frequency*t)*.36f+Mathf.Sin(2*Mathf.PI*frequency*2*t)*.21f)*Mathf.Exp(-t*8);
                    float crack=(noise-mid)*.55f*Mathf.Exp(-t*28);
                    float debris=mid*.75f*Mathf.Exp(-t*4)*( .65f+.35f*Mathf.Sin(2*Mathf.PI*23*t));
                    v=(body+crack+debris+low*.7f*Mathf.Exp(-t*3))*Mathf.Min(1,t*1400)*Mathf.Clamp01((seconds-t)*25);
                }
                samples[i]=Mathf.Clamp(v,-.9f,.9f);
            }
            string path=Root+name+".wav";
            using(var writer=new BinaryWriter(File.Create(path)))
            {
                writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));writer.Write(36+count*2);writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));writer.Write(16);writer.Write((short)1);writer.Write((short)1);writer.Write(rate);writer.Write(rate*2);writer.Write((short)2);writer.Write((short)16);writer.Write(System.Text.Encoding.ASCII.GetBytes("data"));writer.Write(count*2);
                foreach(float v in samples)writer.Write((short)(v*32767));
            }
            AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
            var importer=(AudioImporter)AssetImporter.GetAtPath(path);var sample=importer.defaultSampleSettings;sample.loadType=AudioClipLoadType.DecompressOnLoad;sample.compressionFormat=AudioCompressionFormat.PCM;sample.preloadAudioData=true;importer.defaultSampleSettings=sample;importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        }
    }
}
