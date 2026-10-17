using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace Fortress.Editor
{
    // editor-tooling-codex: a presentation-only derivative of the validated terrain range.
    public static class MilitaryArtBuilder
    {
        public const string ScenePath = "Assets/Fortress/Scenes/MilitaryRange.unity";
        const string Data = "Assets/Fortress/Art/Military/";
        const string Ground = "Assets/ThirdParty/PolyHavenRockyTerrain/rocky_terrain_";

        public static void CreateBatch()
        {
            try { Create(); EditorApplication.Exit(0); }
            catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
        }

        // Explicit, narrowly scoped revision of the generated art scene; never rebuilds its rig.
        public static void AdjustPresentationBatch()
        {
            try
            {
                if (!File.Exists(ScenePath)) throw new InvalidOperationException("Create MilitaryRange first.");
                var scene = EditorSceneManager.OpenScene(ScenePath);
                CompactPresentation();
                var layer = Require<TerrainLayer>(Data + "RockyGround.terrainlayer");
                layer.tileSize = new Vector2(24, 24);
                EditorUtility.SetDirty(layer);
                AssetDatabase.SaveAssets();
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene, ScenePath);
                Debug.Log("Military presentation framing adjusted.");
                EditorApplication.Exit(0);
            }
            catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
        }

        [MenuItem("Fortress/Open Military Range")]
        public static void Open()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            if (File.Exists(ScenePath)) EditorSceneManager.OpenScene(ScenePath);
            else Create();
        }

        public static void Create()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            if (File.Exists(ScenePath)) { EditorSceneManager.OpenScene(ScenePath); return; }
            Require<GameObject>("Assets/ThirdParty/QuaterniusTanks/Tank.fbx");
            Require<Texture2D>(Ground + "diff_2k.jpg");
            Require<Texture2D>("Assets/ThirdParty/KenneyParticles/smoke_01.png");
            Directory.CreateDirectory(Data);
            AssetDatabase.Refresh();
            var scene = EditorSceneManager.OpenScene(TerrainRangeBuilder.ScenePath);
            // Save a separate scene before recording any changes to scene objects.
            EditorSceneManager.SaveScene(scene, ScenePath);
            try
            {
                EditorUtility.DisplayProgressBar("Military range", "Preparing tank meshes", .2f);
                var controller = UnityEngine.Object.FindFirstObjectByType<FlatShotController>();
                for (int i = 0; i < controller.tanks.Length; i++) ApplyTank(controller.tanks[i], i);
                EditorUtility.DisplayProgressBar("Military range", "Ground and atmosphere", .6f);
                ApplyGround(controller.terrainMap.terrain);
                ApplySmoke(controller.impactEffect);
                ApplyPresentation();
                AssetDatabase.SaveAssets();
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene, ScenePath);
                Debug.Log("Military presentation created: " + ScenePath);
            }
            finally { EditorUtility.ClearProgressBar(); }
        }

        static void ApplyTank(TankRig rig, int side)
        {
            Undo.RegisterFullObjectHierarchyUndo(rig.gameObject, "Military tank presentation");
            foreach (var renderer in rig.GetComponentsInChildren<Renderer>()) renderer.enabled = false;
            var source = UnityEngine.Object.Instantiate(Require<GameObject>("Assets/ThirdParty/QuaterniusTanks/Tank.fbx"));
            source.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            var pieces = new List<Piece>();
            try
            {
                foreach (var renderer in source.GetComponentsInChildren<Renderer>())
                {
                    Mesh mesh;
                    if (renderer is SkinnedMeshRenderer skin)
                    {
                        mesh = new Mesh();
                        skin.BakeMesh(mesh);
                    }
                    else
                    {
                        var filter = renderer.GetComponent<MeshFilter>();
                        if (filter == null || filter.sharedMesh == null) continue;
                        mesh = UnityEngine.Object.Instantiate(filter.sharedMesh);
                    }
                    var matrix = Matrix4x4.Rotate(Quaternion.Euler(0, -90, 0)) * renderer.localToWorldMatrix;
                    var vertices = mesh.vertices;
                    for (int n = 0; n < vertices.Length; n++) vertices[n] = matrix.MultiplyPoint3x4(vertices[n]);
                    mesh.vertices = vertices;
                    mesh.RecalculateBounds();
                    int group = renderer.name.Contains("Gun") ? 2 : renderer.name.Contains("Turret") ? 1 : 0;
                    pieces.Add(new Piece { mesh = mesh, name = renderer.name, group = group, materials = renderer.sharedMaterials });
                }
                for (int group = 0; group < 3; group++)
                {
                    bool found = false;
                    var bounds = new Bounds();
                    foreach (var piece in pieces) if (piece.group == group)
                    { if (!found) { bounds = piece.mesh.bounds; found = true; } else bounds.Encapsulate(piece.mesh.bounds); }
                    if (!found) throw new InvalidOperationException("Missing tank geometry group " + group);
                    var size = group == 0 ? new Vector3(3.8f, 1.5f, 4.4f) : group == 1 ? new Vector3(2.6f, 1.1f, 2.8f) : new Vector3(.32f, .32f, rig.muzzle.localPosition.z);
                    var center = group == 0 ? new Vector3(0, .75f, 0) : group == 1 ? new Vector3(0, .05f, 0) : new Vector3(0, 0, size.z / 2);
                    var scale = new Vector3(size.x / bounds.size.x, size.y / bounds.size.y, size.z / bounds.size.z);
                    foreach (var piece in pieces) if (piece.group == group)
                    {
                        var vertices = piece.mesh.vertices;
                        for (int n = 0; n < vertices.Length; n++) vertices[n] = Vector3.Scale(vertices[n] - bounds.center, scale) + center;
                        piece.mesh.vertices = vertices;
                        piece.mesh.RecalculateNormals(); piece.mesh.RecalculateTangents(); piece.mesh.RecalculateBounds();
                        var path = Data + "Tank_" + piece.name + ".asset";
                        var saved = AssetDatabase.LoadAssetAtPath<Mesh>(path);
                        if (saved == null) { saved = piece.mesh; AssetDatabase.CreateAsset(saved, path); }
                        else { EditorUtility.CopySerialized(piece.mesh, saved); UnityEngine.Object.DestroyImmediate(piece.mesh); }
                        piece.mesh = null;
                        var obj = new GameObject("Military " + piece.name, typeof(MeshFilter), typeof(MeshRenderer));
                        Undo.RegisterCreatedObjectUndo(obj, "Military tank mesh");
                        obj.transform.SetParent(group == 0 ? rig.transform : group == 1 ? rig.turret : rig.barrel, false);
                        obj.GetComponent<MeshFilter>().sharedMesh = saved;
                        var materials = new Material[piece.materials.Length];
                        for (int m = 0; m < materials.Length; m++) materials[m] = TankMaterial(side, piece.materials[m] == null ? "Main" : piece.materials[m].name);
                        obj.GetComponent<MeshRenderer>().sharedMaterials = materials;
                    }
                }
            }
            finally
            {
                foreach (var piece in pieces) if (piece.mesh != null) UnityEngine.Object.DestroyImmediate(piece.mesh);
                UnityEngine.Object.DestroyImmediate(source);
            }
        }

        sealed class Piece { public Mesh mesh; public string name; public int group; public Material[] materials; }

        static Material TankMaterial(int side, string slot)
        {
            var color = side == 0 ? new Color(.25f, .29f, .17f) : new Color(.48f, .41f, .28f);
            bool metal = slot.Contains("Details") || slot.Contains("Wheels");
            if (metal) color = new Color(.10f, .115f, .10f);
            else if (slot.Contains("Dark")) color *= .65f;
            else if (slot.Contains("Light")) color *= 1.18f;
            color.a = 1;
            var material = Material("Tank" + side + "_" + slot, "Universal Render Pipeline/Lit", color);
            material.SetFloat("_Metallic", metal ? .65f : .25f);
            material.SetFloat("_Smoothness", metal ? .32f : .18f);
            return material;
        }

        static void ApplyGround(Terrain terrain)
        {
            var normalPath = Ground + "nor_gl_2k.jpg";
            var importer = AssetImporter.GetAtPath(normalPath) as TextureImporter;
            if (importer != null && importer.textureType != TextureImporterType.NormalMap)
            { importer.textureType = TextureImporterType.NormalMap; importer.SaveAndReimport(); }
            var layer = AssetDatabase.LoadAssetAtPath<TerrainLayer>(Data + "RockyGround.terrainlayer");
            if (layer == null) { layer = new TerrainLayer(); AssetDatabase.CreateAsset(layer, Data + "RockyGround.terrainlayer"); }
            layer.diffuseTexture = Require<Texture2D>(Ground + "diff_2k.jpg");
            layer.normalMapTexture = Require<Texture2D>(normalPath);
            layer.tileSize = new Vector2(24, 24); layer.normalScale = .6f; layer.smoothness = .05f; layer.metallic = 0;
            var data = AssetDatabase.LoadAssetAtPath<TerrainData>(Data + "MilitaryTerrain.asset");
            if (data == null) { data = UnityEngine.Object.Instantiate(terrain.terrainData); AssetDatabase.CreateAsset(data, Data + "MilitaryTerrain.asset"); }
            else EditorUtility.CopySerialized(terrain.terrainData, data);
            data.terrainLayers = new[] { layer };
            terrain.terrainData = data;
            EditorUtility.SetDirty(layer); EditorUtility.SetDirty(data);
        }

        static void ApplySmoke(ParticleSystem effect)
        {
            effect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var material = Material("DustSmoke", "Universal Render Pipeline/Particles/Unlit", Color.white);
            material.SetTexture("_BaseMap", Require<Texture2D>("Assets/ThirdParty/KenneyParticles/smoke_01.png"));
            material.SetFloat("_Surface", 1); material.SetFloat("_Blend", 0);
            material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha); material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_ZWrite", 0); material.SetFloat("_Cull", 0);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); material.renderQueue = (int)RenderQueue.Transparent;
            var main = effect.main; main.startLifetime = new ParticleSystem.MinMaxCurve(1.3f, 2.7f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.5f, 4.5f); main.startSize = new ParticleSystem.MinMaxCurve(1.1f, 2.5f);
            main.startColor = new Color(.35f, .31f, .25f, .8f); main.gravityModifier = -.08f;
            var emission = effect.emission; emission.SetBursts(new[] { new ParticleSystem.Burst(0, 24) });
            var size = effect.sizeOverLifetime; size.enabled = true; size.size = new ParticleSystem.MinMaxCurve(1, AnimationCurve.Linear(0, .4f, 1, 1.8f));
            var color = effect.colorOverLifetime; color.enabled = true;
            var gradient = new Gradient(); gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) }, new[] { new GradientAlphaKey(.8f, 0), new GradientAlphaKey(0, 1) });
            color.color = gradient;
            effect.GetComponent<ParticleSystemRenderer>().sharedMaterial = material;
        }

        static void ApplyPresentation()
        {
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(.46f, .49f, .50f);
            RenderSettings.ambientEquatorColor = new Color(.28f, .29f, .26f);
            RenderSettings.ambientGroundColor = new Color(.14f, .13f, .11f);
            RenderSettings.fog = true; RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(.39f, .43f, .44f); RenderSettings.fogStartDistance = 95; RenderSettings.fogEndDistance = 240;
            foreach (var light in UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
            { if (light.type != LightType.Directional) continue; light.color = new Color(1, .91f, .76f); light.intensity = 2; light.transform.rotation = Quaternion.Euler(38, -32, 0); light.shadowStrength = .85f; }
            var camera = UnityEngine.Camera.main;
            if (camera != null) camera.backgroundColor = RenderSettings.fogColor;
            var hud = UnityEngine.Object.FindFirstObjectByType<FlatShotHud>();
            foreach (var label in hud.GetComponentsInChildren<TextMeshProUGUI>())
            {
                label.color = new Color(.86f, .87f, .79f); label.fontSize = 23;
                if (label.name == "Instructions") label.text = "FORTRESS  /  FIELD TRIAL 01\nA/D  TRAVERSE     W/S  ELEVATION     Q/E  CHARGE     SPACE  FIRE\nWind-free aim guide   /   Green marker: predicted impact";
            }
            hud.status.color = new Color(.83f, .72f, .43f);
            var panel = new GameObject("Field Console", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(hud.transform, false); panel.transform.SetAsFirstSibling();
            var rect = panel.GetComponent<RectTransform>(); rect.anchorMin = new Vector2(0, 1); rect.anchorMax = new Vector2(1, 1); rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = Vector2.zero; rect.sizeDelta = new Vector2(0, 310);
            var image = panel.GetComponent<Image>(); image.color = new Color(.035f, .045f, .038f, .88f); image.raycastTarget = false;
            CompactPresentation();
        }

        static void CompactPresentation()
        {
            var hud = UnityEngine.Object.FindFirstObjectByType<FlatShotHud>();
            if (hud == null) throw new InvalidOperationException("Military HUD missing.");
            foreach (var rect in hud.GetComponentsInChildren<RectTransform>())
            {
                if (rect.name == "Aim Values") rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, -115);
                else if (rect.name == "Shot Status") rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, -155);
                else if (rect.name == "Field Console") rect.sizeDelta = new Vector2(rect.sizeDelta.x, 195);
            }
            foreach (var light in UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
                if (light.type == LightType.Directional) light.color = new Color(1, .98f, .94f);
            var camera = UnityEngine.Camera.main;
            if (camera != null)
            {
                camera.transform.position = new Vector3(0, 32, -48);
                camera.transform.LookAt(new Vector3(0, 3, 0));
                camera.fieldOfView = 55;
            }
        }

        static T Require<T>(string path) where T : UnityEngine.Object
        { var asset = AssetDatabase.LoadAssetAtPath<T>(path); if (asset == null) throw new InvalidOperationException("Missing art: " + path); return asset; }
        static Material Material(string name, string shaderName, Color color)
        {
            var path = Data + name + ".mat";
            var result = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (result == null)
            { var shader = Shader.Find(shaderName); if (shader == null) throw new InvalidOperationException(shaderName); result = new Material(shader) { name = name }; AssetDatabase.CreateAsset(result, path); }
            result.SetColor("_BaseColor", color); EditorUtility.SetDirty(result); return result;
        }
    }
}
