using System;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Fortress.Editor
{
    public static class CasualPresentationBuilder
    {
        [MenuItem("Fortress/Upgrade Casual Presentation")]
        public static void Upgrade()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play first.");
            var orbitSettings = AssetDatabase.LoadAssetAtPath<OrbitCameraSettings>("Assets/Fortress/Data/OrbitCameraSettings.asset");
            var shotSettings = AssetDatabase.LoadAssetAtPath<ProjectileCameraSettings>("Assets/Fortress/Data/ProjectileCameraSettings.asset");
            var aim = AssetDatabase.LoadAssetAtPath<AimPreviewStyle>("Assets/Fortress/Art/Atmosphere/ReadableAim.asset");
            Undo.RecordObjects(new Object[] { orbitSettings, shotSettings, aim }, "Casual presentation");
            orbitSettings.casualPresentation = true; shotSettings.casualPresentation = true; aim.showImpactGuide = true; aim.windAwarePreview = true;
            orbitSettings.aimDistance = 45; orbitSettings.aimPitch = 38;
            aim.coreColor = new Color(.75f, 1, .94f); aim.widthMultiplier = 1.9f;
            EditorUtility.SetDirty(orbitSettings); EditorUtility.SetDirty(shotSettings); EditorUtility.SetDirty(aim);
            Layer("Ground", new Color(.12f, .19f, .07f), new Color(.84f, 1f, .68f));
            Layer("Road", new Color(.15f, .09f, .035f), new Color(1, .88f, .64f));
            Layer("Rock", new Color(.12f, .15f, .18f), new Color(.82f, .89f, .98f));
            Tint("Assets/Fortress/Art/Exterior/SurroundingGround.mat", new Color(.55f, .74f, .40f));
            Tint("Assets/Fortress/Art/Exterior/PerimeterRock.mat", new Color(.60f, .67f, .72f));
            Tint("Assets/Fortress/Art/FieldDetails/PineNeedles.mat", new Color(.20f, .43f, .22f));
            Tint("Assets/Fortress/Art/FieldDetails/ShrubLeaves.mat", new Color(.32f, .57f, .23f));
            Tint("Assets/Fortress/Art/Exterior/DistantPine.mat", new Color(.23f, .43f, .32f));
            for (int side = 0; side < 2; side++)
            {
                Color team = side == 0 ? new Color(.16f, .61f, .70f) : new Color(.90f, .36f, .20f);
                Tint("Assets/Fortress/Art/TankRoles/Side" + side + "_Main.mat", team);
                Tint("Assets/Fortress/Art/TankRoles/Side" + side + "_Main_Light.mat", Color.Lerp(team, Color.white, .25f));
                Tint("Assets/Fortress/Art/TankRoles/Side" + side + "_Main_Dark.mat", team * .65f);
            }
            foreach (var path in new[] { SessionMenu.LocalScene, SessionMenu.AiScene })
            {
                var scene = EditorSceneManager.OpenScene(path);
                RenderSettings.ambientMode = AmbientMode.Trilight;
                RenderSettings.ambientSkyColor = new Color(.66f, .78f, .91f);
                RenderSettings.ambientEquatorColor = new Color(.49f, .57f, .49f);
                RenderSettings.ambientGroundColor = new Color(.28f, .30f, .25f);
                RenderSettings.fogColor = new Color(.64f, .79f, .88f);
                foreach (var light in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
                    if (light.type == LightType.Directional)
                    {
                        Undo.RecordObject(light, "Clear daylight");
                        light.color = new Color(1, .97f, .89f); light.intensity = 1.45f; light.shadowStrength = .65f;
                    }
                var camera = Camera.main; Undo.RecordObject(camera, "Clear sky"); camera.backgroundColor = RenderSettings.fogColor;
                var orbit = camera.GetComponent<BattlefieldOrbitCamera>();
                var hud = Object.FindFirstObjectByType<MatchHud>();
                var root = hud.transform.Find("Combat composition");
                var existing = root.Find("View shortcuts"); if (existing != null) Object.DestroyImmediate(existing.gameObject);
                var holder = Rect(root, "View shortcuts", 44, 175, 490, 102);
                Button(holder, "Own view", 0, "내 탱크 보기", orbit.ViewOwnTank, new Color(.13f, .43f, .52f, .97f));
                Button(holder, "Opponent view", 250, "상대 보기", orbit.ViewOpponent, new Color(.65f, .28f, .16f, .97f));
                Label(holder, "Guide legend", 0, 64, 490, 30, "원: 첫 착탄 예상 · 피해 범위 아님", 19);
                var hint = root.Find("Camera controls").GetComponent<TMP_Text>();
                Undo.RecordObject(hint, "Camera controls"); hint.text = "우클릭  회전   ·   휠  확대/축소   ·   F  내 탱크";
                for (int i = 0; i < hud.healthFills.Length; i++)
                {
                    Undo.RecordObject(hud.healthFills[i], "Team health color");
                    hud.healthFills[i].color = i == 0 ? new Color(.28f, .82f, .89f) : new Color(1, .48f, .30f);
                }
                EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            }
            AssetDatabase.SaveAssets(); EditorSceneManager.OpenScene(SessionMenu.MainScene);
        }
        static void Layer(string name, Color minimum, Color maximum)
        {
            var layer = AssetDatabase.LoadAssetAtPath<TerrainLayer>("Assets/Fortress/Art/Atmosphere/" + name + ".terrainlayer");
            Undo.RecordObject(layer, "Readable terrain colors");
            Color tint = name == "Ground" ? new Color(.43f, .58f, .28f) : name == "Road" ? new Color(.64f, .49f, .32f) : new Color(.52f, .58f, .62f);
            layer.diffuseTexture = GroundTexture(name, tint);
            layer.diffuseRemapMin = Vector4.zero; layer.diffuseRemapMax = new Vector4(1, 1, 1, 0);
            layer.normalMapTexture = null; layer.normalScale = 0; layer.smoothness = 0; layer.metallic = 0; EditorUtility.SetDirty(layer);
        }
        static Texture2D GroundTexture(string name, Color tint)
        {
            string path = "Assets/Fortress/Data/Casual" + name + ".asset";
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (texture == null)
            {
                texture = new Texture2D(128, 128, TextureFormat.RGBA32, true) { name = "Casual " + name, wrapMode = TextureWrapMode.Repeat };
                AssetDatabase.CreateAsset(texture, path);
            }
            Undo.RecordObject(texture, "Soft terrain texture");
            var pixels = new Color[128 * 128];
            for (int y = 0; y < 128; y++) for (int x = 0; x < 128; x++)
            {
                float u = x / 127f, v = y / 127f;
                float a = Mathf.Lerp(Mathf.PerlinNoise(10 + u * 4, 10 + v * 4), Mathf.PerlinNoise(6 + u * 4, 10 + v * 4), u);
                float b = Mathf.Lerp(Mathf.PerlinNoise(10 + u * 4, 6 + v * 4), Mathf.PerlinNoise(6 + u * 4, 6 + v * 4), u);
                float noise = (Mathf.Lerp(a, b, v) - .5f) * .08f;
                // Terrain/Lit reads diffuse alpha as smoothness; keep the terrain matte.
                pixels[y * 128 + x] = new Color(tint.r + noise, tint.g + noise, tint.b + noise, 0);
            }
            texture.SetPixels(pixels); texture.Apply(); EditorUtility.SetDirty(texture); return texture;
        }
        static void Tint(string path, Color color)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null) throw new InvalidOperationException("Missing material " + path);
            Undo.RecordObject(material, "Casual palette"); material.SetColor("_BaseColor", color); EditorUtility.SetDirty(material);
        }
        static RectTransform Rect(Transform parent, string name, float x, float y, float width, float height)
        {
            var go = new GameObject(name, typeof(RectTransform)); var rect = (RectTransform)go.transform; rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1); rect.anchoredPosition = new Vector2(x, -y); rect.sizeDelta = new Vector2(width, height); return rect;
        }
        static TMP_Text Label(Transform parent, string name, float x, float y, float width, float height, string value, int size)
        {
            var label = Rect(parent, name, x, y, width, height).gameObject.AddComponent<TextMeshProUGUI>();
            label.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(CommandUiBuilder.FontPath); label.text = value; label.fontSize = size;
            label.color = Color.white; label.raycastTarget = false; label.alignment = TextAlignmentOptions.Center; return label;
        }
        static void Button(Transform parent, string name, float x, string title, UnityAction action, Color color)
        {
            var rect = Rect(parent, name, x, 0, 240, 54); var image = rect.gameObject.AddComponent<Image>(); image.color = color;
            image.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd"); image.type = Image.Type.Sliced;
            var button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = image; UnityEventTools.AddPersistentListener(button.onClick, action);
            Label(rect, "Label", 8, 8, 224, 38, title, 22);
        }
    }
}
