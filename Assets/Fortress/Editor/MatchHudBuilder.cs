using System;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace Fortress.Editor
{
    // Original uGUI geometry and typography; no copied game artwork.
    public static class MatchHudBuilder
    {
        static TMP_FontAsset font;
        static HudStyleSO style;
        static Sprite white;
        public static void UpgradeHudBatch()
        {
            try
            {
                var scene = EditorSceneManager.OpenScene(MatchSceneBuilder.ScenePath);
                var match = UnityEngine.Object.FindFirstObjectByType<MatchController>();
                if (match == null) throw new InvalidOperationException("MilitaryMatch controller missing.");
                Create(match);
                AssetDatabase.SaveAssets(); EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene, MatchSceneBuilder.ScenePath);
                Debug.Log("MilitaryMatch HUD upgraded; gameplay and camera unchanged.");
                EditorApplication.Exit(0);
            }
            catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
        }

        public static void Create(MatchController match)
        {
            foreach (var old in UnityEngine.Object.FindObjectsByType<MatchHud>(FindObjectsSortMode.None)) UnityEngine.Object.DestroyImmediate(old.gameObject);
            foreach (var old in UnityEngine.Object.FindObjectsByType<FlatShotHud>(FindObjectsSortMode.None)) UnityEngine.Object.DestroyImmediate(old.gameObject);
            font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
            style = AssetDatabase.LoadAssetAtPath<HudStyleSO>("Assets/Fortress/Data/CombatHudStyle.asset");
            if (style == null) { style = ScriptableObject.CreateInstance<HudStyleSO>(); AssetDatabase.CreateAsset(style, "Assets/Fortress/Data/CombatHudStyle.asset"); }
            white = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            var canvas = new GameObject("Match HUD", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Undo.RegisterCreatedObjectUndo(canvas, "Create combat HUD");
            canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvas.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080); scaler.matchWidthOrHeight = .5f;
            // Build inactive so subscriptions see every serialized reference on activation.
            canvas.SetActive(false);
            var hud = canvas.AddComponent<MatchHud>(); hud.match = match; hud.style = style;
            hud.healthLabels = new TMP_Text[2]; hud.healthFills = new Image[2]; hud.activeIndicators = new Image[2];
            for (int i = 0; i < 2; i++)
            {
                var card = Box("Player " + (i + 1), canvas.transform, new Vector2(i, 1), new Vector2(i, 1), new Vector2(i == 0 ? 36 : -36, -30), new Vector2(390, 112), style.panel);
                hud.activeIndicators[i] = Box("Active edge", card, Vector2.up, Vector2.up, Vector2.zero, new Vector2(4, 112), style.accent).GetComponent<Image>();
                Label("Unit", card, 20, 12, 350, 22, i == 0 ? "01  /  IRON COMPANY" : "02  /  DUNE COMPANY", 17, style.muted);
                hud.healthLabels[i] = Label("Armor", card, 20, 38, 350, 32, "", 25, style.text);
                var track = Box("Armor track", card, Vector2.up, Vector2.up, new Vector2(20, -86), new Vector2(350, 5), new Color(.22f, .26f, .25f));
                hud.healthFills[i] = Fill("Armor fill", track, style.accent);
            }
            var center = Box("Turn compass", canvas.transform, new Vector2(.5f, 1), new Vector2(.5f, 1), new Vector2(0, -30), new Vector2(330, 98), style.panel);
            hud.turnLabel = Label("Turn", center, 10, 12, 310, 30, "", 24, style.text); hud.turnLabel.alignment = TextAlignmentOptions.Center;
            hud.windLabel = Label("Wind", center, 78, 55, 230, 28, "", 19, style.muted);
            var arrow = Label("Wind arrow", center, 30, 47, 36, 36, ">", 30, style.accent).rectTransform;
            arrow.pivot = new Vector2(.5f, .5f); arrow.anchoredPosition += new Vector2(18, -18); hud.windArrow = arrow;

            var fire = Box("Gunnery", canvas.transform, new Vector2(.5f, 0), new Vector2(.5f, 0), new Vector2(0, 32), new Vector2(670, 158), style.panel);
            Label("Elevation caption", fire, 24, 15, 170, 22, "ELEVATION  /  W S", 16, style.muted);
            hud.elevationLabel = Label("Elevation", fire, 22, 40, 175, 68, "45.0°", 48, style.text);
            hud.headingLabel = Label("Bearing", fire, 24, 113, 200, 24, "", 16, style.muted);
            hud.aimLabel = hud.headingLabel;
            Box("Divider", fire, Vector2.up, Vector2.up, new Vector2(220, -22), new Vector2(1, 114), new Color(.28f, .32f, .30f));
            Label("Charge caption", fire, 246, 15, 250, 24, "FIRING POWER  /  Q E", 16, style.muted);
            hud.powerLabel = Label("Power value", fire, 540, 11, 106, 45, "22.0", 34, style.text); hud.powerLabel.alignment = TextAlignmentOptions.Right;
            var charge = Box("Charge track", fire, Vector2.up, Vector2.up, new Vector2(246, -64), new Vector2(400, 20), new Color(.18f, .22f, .21f));
            hud.powerFill = Fill("Charge fill", charge, style.accent);
            for (int tick = 1; tick < 10; tick++) Box("Scale " + tick, charge, Vector2.up, Vector2.up, new Vector2(tick * 40, 0), new Vector2(2, 20), style.panel);
            hud.stateLabel = Label("Readiness", fire, 246, 103, 400, 38, "", 17, style.accent);

            var ammo = Box("Ammunition", canvas.transform, Vector2.right, Vector2.right, new Vector2(-36, 32), new Vector2(255, 158), style.panel);
            Label("Ammo caption", ammo, 20, 14, 210, 22, "ORDNANCE  /  01", 16, style.muted);
            // A shell silhouette built from ordinary UI rectangles and a rotated square.
            var icon = Box("Shell casing", ammo, Vector2.up, Vector2.up, new Vector2(28, -63), new Vector2(20, 44), style.text);
            var tip = Box("Shell tip", ammo, Vector2.up, new Vector2(.5f,.5f), new Vector2(38, -59), new Vector2(14,14), style.text); tip.localRotation = Quaternion.Euler(0,0,45);
            Box("Shell rim", icon, Vector2.zero, Vector2.zero, new Vector2(-3,0), new Vector2(26,5), style.muted);
            Label("Shell", ammo, 70, 49, 170, 30, "STANDARD", 24, style.text);
            Label("Supply", ammo, 70, 82, 165, 23, "UNLIMITED", 17, style.muted);
            Label("Fire key", ammo, 20, 123, 215, 24, "[ SPACE ]   FIRE", 18, style.accent);
            var controls = Label("Controls", canvas.transform, 0, 0, 320, 60, "A / D   TRAVERSE\nW / S   ELEVATE    Q / E   POWER", 17, style.text);
            controls.rectTransform.anchorMin = controls.rectTransform.anchorMax = Vector2.zero; controls.rectTransform.pivot = Vector2.zero;
            controls.rectTransform.anchoredPosition = new Vector2(36, 37);
            var notice = Box("Turn notification", canvas.transform, new Vector2(.5f,.7f), new Vector2(.5f,.5f), Vector2.zero, new Vector2(570,48), style.panel);
            hud.notificationGroup = notice.gameObject.AddComponent<CanvasGroup>(); hud.notificationGroup.blocksRaycasts = false; hud.notificationGroup.alpha = 0;
            hud.notificationLabel = Label("Notification", notice, 10, 8, 550, 32, "", 22, style.text); hud.notificationLabel.alignment = TextAlignmentOptions.Center;

            var result = Box("Result", canvas.transform, new Vector2(.5f,.5f), new Vector2(.5f,.5f), Vector2.zero, new Vector2(580,340), style.panel);
            hud.resultPanel = result.gameObject;
            Box("Result accent", result, Vector2.up, Vector2.up, Vector2.zero, new Vector2(580,4), style.accent);
            Label("Debrief", result, 30, 25, 520, 28, "ENGAGEMENT COMPLETE", 18, style.muted).alignment = TextAlignmentOptions.Center;
            hud.resultLabel = Label("Outcome", result, 30, 75, 520, 155, "", 34, style.text); hud.resultLabel.alignment = TextAlignmentOptions.Center;
            var button = Box("Restart", result, Vector2.up, Vector2.up, new Vector2(140,-258), new Vector2(300,54), style.accent);
            hud.restartButton = button.gameObject.AddComponent<Button>(); hud.restartButton.targetGraphic = button.GetComponent<Image>(); button.GetComponent<Image>().raycastTarget = true;
            var navigation = hud.restartButton.navigation; navigation.mode = Navigation.Mode.None; hud.restartButton.navigation = navigation;
            Label("Restart caption", button, 0, 10, 300, 34, "NEW MATCH", 22, new Color(.08f,.10f,.08f)).alignment = TextAlignmentOptions.Center;
            result.gameObject.SetActive(false);
            var events = UnityEngine.Object.FindFirstObjectByType<EventSystem>();
            if (events == null) { var obj = new GameObject("UI Events", typeof(EventSystem), typeof(InputSystemUIInputModule)); obj.GetComponent<InputSystemUIInputModule>().AssignDefaultActions(); }
            canvas.SetActive(true);
        }

        static Image Fill(string name, Transform parent, Color color)
        {
            var rect = Box(name, parent, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero, color);
            rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero;
            var image = rect.GetComponent<Image>(); image.sprite = white; image.type = Image.Type.Filled; image.fillMethod = Image.FillMethod.Horizontal; image.fillOrigin = 0; return image;
        }
        static RectTransform Box(string name, Transform parent, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image)); go.transform.SetParent(parent,false);
            var rect = go.GetComponent<RectTransform>(); rect.anchorMin = rect.anchorMax = anchor; rect.pivot = pivot; rect.anchoredPosition = pos; rect.sizeDelta = size;
            var image = go.GetComponent<Image>(); image.color = color; image.raycastTarget = false; return rect;
        }
        static TextMeshProUGUI Label(string name, Transform parent, float x, float y, float width, float height, string text, float size, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI)); go.transform.SetParent(parent,false);
            var rect = go.GetComponent<RectTransform>(); rect.anchorMin = rect.anchorMax = Vector2.up; rect.pivot = Vector2.up; rect.anchoredPosition = new Vector2(x,-y); rect.sizeDelta = new Vector2(width,height);
            var label = go.GetComponent<TextMeshProUGUI>(); label.font = font; label.fontSize = size; label.text = text; label.color = color; label.raycastTarget = false;
            label.textWrappingMode = TextWrappingModes.NoWrap; return label;
        }
    }
}
