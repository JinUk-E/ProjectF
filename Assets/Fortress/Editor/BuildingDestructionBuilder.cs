using System;
using System.IO;
using UnityEditor;
using UnityEngine;
namespace Fortress.Editor
{
    public static class BuildingDestructionBuilder
    {
        const string Root="Assets/Fortress/Art/Destruction/";
        [MenuItem("Fortress/Upgrade Building Destruction")]
        public static void Upgrade()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop play before changing destruction assets.");
            Directory.CreateDirectory(Root);AssetDatabase.Refresh();
            var settings=AssetDatabase.LoadAssetAtPath<CoverSettings>("Assets/Fortress/Data/DepotCover.asset");
            var damage=AssetDatabase.LoadAssetAtPath<Material>(Root+"ScorchedConcrete.mat");
            if(damage==null){damage=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(damage,Root+"ScorchedConcrete.mat");}
            Undo.RecordObject(damage,"Scorched concrete");damage.SetColor("_BaseColor",new Color(.12f,.105f,.085f));damage.SetFloat("_Smoothness",.03f);EditorUtility.SetDirty(damage);
            var go=new GameObject("Building collapse dust",typeof(ParticleSystem));
            GameObject prefab;
            try
            {
                var ps=go.GetComponent<ParticleSystem>();ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
                var main=ps.main;main.loop=false;main.playOnAwake=false;main.duration=2.4f;main.startLifetime=new ParticleSystem.MinMaxCurve(1.1f,2.4f);
                main.startSpeed=new ParticleSystem.MinMaxCurve(.8f,2.6f);main.startSize=new ParticleSystem.MinMaxCurve(.7f,1.4f);main.startColor=new Color(.48f,.44f,.37f,.55f);main.gravityModifier=-.035f;main.simulationSpace=ParticleSystemSimulationSpace.World;main.maxParticles=64;
                var emission=ps.emission;emission.rateOverTime=0;emission.SetBursts(new[]{new ParticleSystem.Burst(0,20),new ParticleSystem.Burst(.35f,20)});
                var shape=ps.shape;shape.shapeType=ParticleSystemShapeType.Hemisphere;shape.radius=1.8f;shape.rotation=new Vector3(-90,0,0);
                var size=ps.sizeOverLifetime;size.enabled=true;size.size=new ParticleSystem.MinMaxCurve(1,AnimationCurve.Linear(0,.6f,1,2.2f));
                var alpha=ps.colorOverLifetime;alpha.enabled=true;var gradient=new Gradient();gradient.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1)},new[]{new GradientAlphaKey(.65f,0),new GradientAlphaKey(0,1)});alpha.color=gradient;
                var renderer=go.GetComponent<ParticleSystemRenderer>();renderer.sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/Fortress/Art/Military/DustSmoke.mat");renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
                prefab=PrefabUtility.SaveAsPrefabAsset(go,Root+"CollapseDust.prefab");
            }
            finally{UnityEngine.Object.DestroyImmediate(go);}
            Undo.RecordObject(settings,"Building collapse presentation");settings.collapseSeconds=1.2f;settings.collapseSpread=1.25f;settings.damageMaterial=damage;settings.collapseDustPrefab=prefab.GetComponent<ParticleSystem>();EditorUtility.SetDirty(settings);AssetDatabase.SaveAssets();
        }
    }
}
