using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Fortress.Editor
{
    public static class ShellExhaustBuilder
    {
        [MenuItem("Fortress/Upgrade Shell Exhaust")]
        public static void Upgrade()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop play before upgrading exhaust.");
            var smoke = AssetDatabase.LoadAssetAtPath<Material>("Assets/Fortress/Art/Military/DustSmoke.mat");
            const string materialPath = "Assets/Fortress/Data/ShellExhaust.mat";
            var fire = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (fire == null) { fire = new Material(smoke); AssetDatabase.CreateAsset(fire, materialPath); }
            Undo.RecordObject(fire, "Upgrade shell exhaust");
            fire.SetColor("_BaseColor", Color.white);
            fire.SetTexture("_BaseMap", FlameTexture());
            fire.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            fire.SetFloat("_DstBlend", (float)BlendMode.One);
            EditorUtility.SetDirty(fire);
            foreach (string name in new[] { "StandardShell", "ClusterCarrier", "ClusterBomblet" })
            {
                string path = "Assets/Fortress/Prefabs/" + name + ".prefab";
                var root = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    var view = root.GetComponent<ShellVisual>();
                    if (view == null || view.visualRoot == null) throw new InvalidOperationException("Missing shell view: " + name);
                    var old = view.visualRoot.Find("Exhaust");
                    if (old != null) UnityEngine.Object.DestroyImmediate(old.gameObject);
                    var exhaust = new GameObject("Exhaust").transform;
                    exhaust.SetParent(view.visualRoot, false);
                    exhaust.localPosition = new Vector3(0, 0, -.53f);
                    exhaust.localRotation = Quaternion.Euler(0, 180, 0);
                    // Child of the oriented model: disabling the model also clears pooled effects.
                    Plume(exhaust, "Hot core", fire, 85, .10f, 6, .4f, new Color(2.4f, 1.6f, .65f), false);
                    Plume(exhaust, "Rolling flame", fire, 65, .23f, 7, .85f, new Color(1.8f, .42f, .055f), false);
                    Plume(exhaust, "Exhaust smoke", smoke, 24, .48f, 6, .48f, new Color(.20f, .18f, .16f, .32f), true);
                    if (view.trail != null)
                    {
                        view.trail.time = .18f;
                        view.trail.widthCurve = AnimationCurve.Linear(0, .22f, 1, 0);
                    }
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                }
                finally { PrefabUtility.UnloadPrefabContents(root); }
            }
            AssetDatabase.SaveAssets();
            Debug.Log("Layered shell exhaust installed on all three projectile prefabs.");
        }

        static Texture2D FlameTexture()
        {
            const string path = "Assets/Fortress/Data/ShellFlame.asset";
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (texture != null) return texture;
            texture = new Texture2D(64, 64, TextureFormat.RGBA32, false) { name = "Soft turbulent flame", wrapMode = TextureWrapMode.Clamp };
            var pixels = new Color[64 * 64];
            for (int y = 0; y < 64; y++) for (int x = 0; x < 64; x++)
            {
                float radius = new Vector2((x - 31.5f) / 31.5f, (y - 31.5f) / 31.5f).magnitude;
                float alpha = Mathf.Pow(Mathf.Clamp01(1 - radius), 1.4f) * (.6f + .4f * Mathf.PerlinNoise(x * .17f, y * .17f));
                pixels[y * 64 + x] = new Color(1, 1, 1, alpha);
            }
            texture.SetPixels(pixels); texture.Apply(); AssetDatabase.CreateAsset(texture, path);
            return texture;
        }

        static void Plume(Transform parent, string name, Material material, float rate, float life, float speed, float size, Color tint, bool smoke)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var particles = go.AddComponent<ParticleSystem>();
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = particles.main;
            main.loop = true; main.playOnAwake = true; main.duration = 1;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.startLifetime = new ParticleSystem.MinMaxCurve(life * .7f, life);
            main.startSpeed = new ParticleSystem.MinMaxCurve(speed * .8f, speed);
            main.startSize = new ParticleSystem.MinMaxCurve(size * .65f, size);
            main.startRotation = new ParticleSystem.MinMaxCurve(0, Mathf.PI * 2);
            main.startColor = tint; main.maxParticles = 48;
            var emission = particles.emission; emission.rateOverTime = rate;
            var shape = particles.shape; shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = smoke ? 12 : 5; shape.radius = .06f;
            var scale = particles.sizeOverLifetime; scale.enabled = true;
            scale.size = new ParticleSystem.MinMaxCurve(1, AnimationCurve.Linear(0, smoke ? .5f : 1, 1, smoke ? 2.5f : .15f));
            var fade = particles.colorOverLifetime; fade.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(smoke ? Color.white : new Color(1, .35f, .08f), 1) },
                new[] { new GradientAlphaKey(0, 0), new GradientAlphaKey(1, .12f), new GradientAlphaKey(0, 1) });
            fade.color = gradient;
            var renderer = particles.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = material; renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false;
        }
    }
}
