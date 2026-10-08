using System.IO;
using Falah.RovSim.Core;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Falah.RovSim.UI.Editor
{
    /// <summary>
    /// Adds the pilot camera HUD (design "5a. Simulasi: ROV kecil") to the open simulation scene as an overlay canvas on
    /// Display 1 (the ROV camera display). The existing canvases are kept; only the old Header and Compass of
    /// CanvasROV, which the new HUD replaces, are hidden. Menu: Falah/UI/Build Pilot HUD.
    /// </summary>
    public static class PilotHudBuilder
    {
        const string CanvasName = "CanvasHUD_Pilot";
        const string LegacyCanvas = "CanvasROV";
        static readonly string[] LegacyChildren = { "Header", "Compass" };

        static readonly Color Glass = new Color(0.024f, 0.055f, 0.094f, 0.72f);
        static readonly Color GlassDeep = new Color(0.024f, 0.055f, 0.094f, 0.78f);
        static readonly Color Hole = UiTheme.Hex("0A1420");

        [MenuItem("Falah/UI/Build Pilot HUD")]
        public static string Build()
        {
            var report = new System.Text.StringBuilder();
            var existing = GameObject.Find(CanvasName);
            if (existing != null) Object.DestroyImmediate(existing);

            var go = new GameObject(CanvasName, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.targetDisplay = 1;
            canvas.sortingOrder = 5;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600, 900);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            var root = (RectTransform)go.transform;

            // The adapter wraps RoVPhysics, which lives in Assembly-CSharp; asmdefs cannot reference that assembly,
            // so it is added by type name.
            var adapterType = System.Type.GetType("Falah.RovSim.Integration.RoVTelemetryAdapter, Assembly-CSharp");
            if (adapterType == null) throw new System.InvalidOperationException("RoVTelemetryAdapter not found (Assets/_Falah/Integration).");
            var adapter = (MonoBehaviour)go.AddComponent(adapterType);
            var hud = go.AddComponent<PilotHud>();
            UiKit.Bind(hud, "telemetrySource", adapter);

            BuildTopBar(root, hud);
            BuildAlert(root, hud);
            BuildLeftStack(root, hud);
            BuildRightStack(root, hud);
            BuildBottomLeft(root, hud);
            BuildBottomCenter(root);
            BuildBottomRight(root, hud);

            // Live sonar when the scene ROV has a scanner (found by type name: it lives in Assembly-CSharp).
            var scannerType = System.Type.GetType("Falah.RovSim.Integration.SonarScanner, Assembly-CSharp");
            var scanner = scannerType != null ? Object.FindFirstObjectByType(scannerType) as MonoBehaviour : null;
            report.AppendLine(AttachSonar(go, scanner));

            // Hide only what the new HUD replaces; record the old state so it can be restored.
            var legacy = GameObject.Find(LegacyCanvas);
            if (legacy != null)
                foreach (var name in LegacyChildren)
                {
                    var child = legacy.transform.Find(name);
                    if (child == null) continue;
                    report.AppendLine(LegacyCanvas + "/" + name + " activeSelf " + child.gameObject.activeSelf + " -> false");
                    child.gameObject.SetActive(false);
                }

            EditorSceneManager.MarkSceneDirty(go.scene);
            report.AppendLine("built " + CanvasName + " in scene " + go.scene.name);
            return report.ToString();
        }

        // ---------- pieces ----------

        static TMP_Text Label(Transform parent, string text) =>
            UiKit.Text(parent, text, 11f, UiTheme.TextMuted, FontStyles.Normal, TextAlignmentOptions.TopLeft, 10f, false);

        static TMP_Text Value(Transform parent, float size, string text = "--") =>
            UiKit.Mono(UiKit.Text(parent, text, size, UiTheme.Text, FontStyles.Normal, TextAlignmentOptions.TopLeft, 0f, false));

        static Box Card(Transform parent, string name, int padH = 16, int padV = 12, float spacing = 2f, Color? fill = null)
        {
            var box = UiKit.GlassBox(parent, name, fill ?? Glass, UiTheme.Border, 10f);
            UiKit.VGroup(box.Outer, spacing, padH, padV, padH, padV);
            return box;
        }

        static void BuildTopBar(RectTransform root, PilotHud hud)
        {
            var bar = UiKit.Rect(root, "TopBar");
            bar.anchorMin = new Vector2(0, 1); bar.anchorMax = new Vector2(1, 1); bar.pivot = new Vector2(0.5f, 1); bar.sizeDelta = new Vector2(0, 60);
            UiKit.Img(bar, new Color(0.024f, 0.055f, 0.094f, 0.72f));
            UiKit.HGroup(bar, 20, 28, 0, 28, 0, TextAnchor.MiddleLeft);
            var line = UiKit.Rect(bar, "Border");
            line.anchorMin = new Vector2(0, 0); line.anchorMax = new Vector2(1, 0); line.pivot = new Vector2(0.5f, 0); line.sizeDelta = new Vector2(0, 1);
            UiKit.IgnoreLayout(line);
            UiKit.Img(line, UiTheme.Border);

            var chip = UiKit.GlassBox(bar, "RovChip", UiTheme.CardSelected, UiTheme.Accent, 6f);
            UiKit.VGroup(chip.Outer, 0, 12, 5, 12, 5, TextAnchor.MiddleCenter);
            var rovName = UiKit.Text(chip.Outer, "TORTUGA", 14f, UiTheme.Text, FontStyles.Bold, TextAlignmentOptions.Center, 6f, false);
            var scenario = UiKit.Text(bar, "Investigasi target sonar", 15f, UiTheme.TextPale, FontStyles.Normal, TextAlignmentOptions.MidlineLeft, 0f, false);

            var timer = UiKit.Rect(bar, "Timer");
            UiKit.IgnoreLayout(timer);
            UiKit.Anchor(timer, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(220, 40));
            UiKit.HGroup(timer, 8, 0, 0, 0, 0, TextAnchor.MiddleCenter);
            var elapsed = UiKit.Mono(UiKit.Text(timer, "00:00", 24f, UiTheme.Text, FontStyles.Normal, TextAlignmentOptions.MidlineLeft, 0f, false));
            var limit = UiKit.Mono(UiKit.Text(timer, "/ --:--", 14f, UiTheme.TextMuted, FontStyles.Normal, TextAlignmentOptions.MidlineLeft, 0f, false));

            UiKit.Spacer(bar);
            var tag = UiKit.GlassBox(bar, "SampleDataTag", Color.clear, UiTheme.Warn, 6f);
            UiKit.VGroup(tag.Outer, 0, 10, 4, 10, 4, TextAnchor.MiddleCenter);
            UiKit.Text(tag.Outer, "Data contoh", 12f, UiTheme.Warn, FontStyles.Normal, TextAlignmentOptions.Center, 0f, false);
            var rec = UiKit.Rect(bar, "Rec");
            UiKit.HGroup(rec, 8, 0, 0, 0, 0, TextAnchor.MiddleLeft);
            var dot = UiKit.Circle(rec, "Dot", 10, 10, UiTheme.Danger);
            UiKit.Size(dot.rectTransform, 10, 10);
            UiKit.Text(rec, "REC", 14f, UiTheme.Danger, FontStyles.Bold, TextAlignmentOptions.MidlineLeft, 0f, false);

            UiKit.Bind(hud, "rovName", rovName);
            UiKit.Bind(hud, "scenarioName", scenario);
            UiKit.Bind(hud, "timerElapsed", elapsed);
            UiKit.Bind(hud, "timerLimit", limit);
            UiKit.Bind(hud, "sampleDataTag", tag.Outer.gameObject);
            UiKit.Bind(hud, "recDot", dot);
        }

        static void BuildAlert(RectTransform root, PilotHud hud)
        {
            var banner = UiKit.GlassBox(root, "Alert", new Color(0.227f, 0.18f, 0.07f, 0.92f), UiTheme.Warn, 8f);
            UiKit.Anchor(banner.Outer, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -78), Vector2.zero);
            UiKit.HGroup(banner.Outer, 14, 20, 10, 20, 10, TextAnchor.MiddleCenter);
            var fitter = banner.Outer.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var icon = UiKit.Circle(banner.Outer, "Icon", 24, 24, UiTheme.Warn);
            UiKit.Size(icon.rectTransform, 24, 24);
            var bang = UiKit.Text(icon.rectTransform, "!", 14f, UiTheme.OnAccent, FontStyles.Bold, TextAlignmentOptions.Center, 0f, false);
            UiKit.Stretch(bang.rectTransform);
            var text = UiKit.Text(banner.Outer, "Peringatan", 15f, UiTheme.Hex("FFE2A8"), FontStyles.Bold, TextAlignmentOptions.MidlineLeft, 0f, false);
            banner.Outer.gameObject.SetActive(false);
            UiKit.Bind(hud, "alertRoot", banner.Outer.gameObject);
            UiKit.Bind(hud, "alertText", text);
        }

        static void BuildLeftStack(RectTransform root, PilotHud hud)
        {
            var stack = UiKit.Rect(root, "LeftStack");
            UiKit.Anchor(stack, new Vector2(0, 1), new Vector2(0, 1), new Vector2(28, -84), new Vector2(230, 0));
            UiKit.VGroup(stack, 10);
            UiKit.Size(stack, 230);
            stack.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            TMP_Text MakeCard(string label)
            {
                var c = Card(stack, label);
                Label(c.Outer, label);
                return Value(c.Outer, 34f);
            }
            var depth = MakeCard("KEDALAMAN");
            var altitude = MakeCard("KETINGGIAN DARI DASAR");
            var heading = MakeCard("HEADING");

            var attitude = Card(stack, "Attitude");
            var row = UiKit.Rect(attitude.Outer, "Row");
            UiKit.HGroup(row, 0, 0, 0, 0, 0, TextAnchor.MiddleLeft);
            var pitchBlock = UiKit.Rect(row, "Pitch");
            UiKit.HGroup(pitchBlock, 4, 0, 0, 0, 0, TextAnchor.MiddleLeft);
            UiKit.Text(pitchBlock, "P", 15f, UiTheme.TextMuted, FontStyles.Normal, TextAlignmentOptions.MidlineLeft, 0f, false);
            var pitch = UiKit.Mono(UiKit.Text(pitchBlock, "--", 15f, UiTheme.Text, FontStyles.Normal, TextAlignmentOptions.MidlineLeft, 0f, false));
            UiKit.Spacer(row);
            var rollBlock = UiKit.Rect(row, "Roll");
            UiKit.HGroup(rollBlock, 4, 0, 0, 0, 0, TextAnchor.MiddleLeft);
            UiKit.Text(rollBlock, "R", 15f, UiTheme.TextMuted, FontStyles.Normal, TextAlignmentOptions.MidlineLeft, 0f, false);
            var roll = UiKit.Mono(UiKit.Text(rollBlock, "--", 15f, UiTheme.Text, FontStyles.Normal, TextAlignmentOptions.MidlineLeft, 0f, false));

            UiKit.Bind(hud, "depth", depth);
            UiKit.Bind(hud, "altitude", altitude);
            UiKit.Bind(hud, "heading", heading);
            UiKit.Bind(hud, "pitch", pitch);
            UiKit.Bind(hud, "roll", roll);
        }

        static void BuildRightStack(RectTransform root, PilotHud hud)
        {
            var stack = UiKit.Rect(root, "RightStack");
            UiKit.Anchor(stack, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-28, -84), new Vector2(320, 0));
            UiKit.VGroup(stack, 12);
            UiKit.Size(stack, 320);
            stack.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // sonar
            var sonar = UiKit.GlassBox(stack, "Sonar", GlassDeep, UiTheme.Border, 10f);
            UiKit.VGroup(sonar.Outer, 8, 12, 12, 12, 12);
            var header = UiKit.Rect(sonar.Outer, "Header");
            UiKit.HGroup(header, 0, 0, 0, 0, 0, TextAnchor.MiddleLeft);
            Label(header, "SONAR IMAGING");
            UiKit.Spacer(header);
            UiKit.Mono(UiKit.Text(header, "-- m", 11f, UiTheme.TextMuted, FontStyles.Normal, TextAlignmentOptions.MidlineRight, 0f, false)).name = "SonarRange";
            var area = UiKit.Rect(sonar.Outer, "Area");
            UiKit.Size(area, -1, 200);
            var areaImg = area.gameObject.AddComponent<Image>();
            areaImg.sprite = UiKit.RoundedSprite(6, false);
            areaImg.type = Image.Type.Sliced;
            areaImg.color = UiTheme.Hex("031018");
            area.gameObject.AddComponent<RectMask2D>();
            Color green = UiTheme.Ok;
            foreach (var (size, alpha) in new[] { (344f, 0.10f), (212f, 0.12f), (120f, 0.14f) })
            {
                var ring = UiKit.Circle(area, "Range", size, size, new Color(green.r, green.g, green.b, alpha));
                UiKit.Anchor(ring.rectTransform, new Vector2(0.5f, 0), new Vector2(0.5f, 0.5f), new Vector2(0, -4), new Vector2(size, size));
            }
            var halo = UiKit.Circle(area, "Halo", 60, 30, new Color(green.r, green.g, green.b, 0.25f));
            UiKit.Anchor(halo.rectTransform, new Vector2(0, 1), new Vector2(0.5f, 0.5f), new Vector2(164, -82), new Vector2(60, 30));
            var blob = UiKit.Circle(area, "Contact", 36, 18, UiTheme.Hex("9BE8C4"));
            UiKit.Anchor(blob.rectTransform, new Vector2(0, 1), new Vector2(0.5f, 0.5f), new Vector2(164, -82), new Vector2(36, 18));
            var sweep = UiKit.Rect(area, "Heading");
            UiKit.Anchor(sweep, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 4), new Vector2(3, 20));
            UiKit.Img(sweep, UiTheme.Accent);

            // thrusters
            var thr = Card(stack, "Thrusters", 14, 12, 10f, GlassDeep);
            Label(thr.Outer, "THRUSTER AZIMUTH (KGF)");
            var grid = UiKit.Rect(thr.Outer, "Grid");
            UiKit.HGroup(grid, 8, 0, 0, 0, 0, TextAnchor.UpperCenter, true, false);
            var needles = new RectTransform[4];
            var rings = new Graphic[4];
            var values = new TMP_Text[4];
            var labels = new TMP_Text[4];
            for (int i = 0; i < 4; i++)
            {
                var col = UiKit.Rect(grid, "T" + (i + 1));
                UiKit.VGroup(col, 6, 0, 0, 0, 0, TextAnchor.UpperCenter, false, false);
                UiKit.Size(col, 0, -1, 1);
                var dial = UiKit.Rect(col, "Dial");
                UiKit.Size(dial, 52, 52);
                rings[i] = UiKit.Circle(dial, "Ring", 52, 52, UiTheme.BorderStrong);
                UiKit.Stretch(rings[i].rectTransform);
                var inner = UiKit.Circle(dial, "Inner", 48, 48, Hole);
                UiKit.Anchor(inner.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(48, 48));
                var needle = UiKit.Rect(dial, "Needle");
                UiKit.Anchor(needle, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0f), Vector2.zero, new Vector2(3, 20));
                UiKit.Img(needle, UiTheme.Accent, 1.5f);
                needles[i] = needle;
                var center = UiKit.Circle(dial, "Center", 6, 6, UiTheme.Accent);
                UiKit.Anchor(center.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(6, 6));
                values[i] = UiKit.Mono(UiKit.Text(col, "0.0", 13f, UiTheme.Text, FontStyles.Normal, TextAlignmentOptions.Center, 0f, false));
                labels[i] = UiKit.Text(col, "T" + (i + 1), 11f, UiTheme.TextMuted, FontStyles.Normal, TextAlignmentOptions.Center, 0f, false);
            }
            UiKit.BindList(hud, "needles", needles);
            UiKit.BindList(hud, "rings", rings);
            UiKit.BindList(hud, "thrustValues", values);
            UiKit.BindList(hud, "thrusterLabels", labels);
        }

        static void BuildBottomLeft(RectTransform root, PilotHud hud)
        {
            var row = UiKit.Rect(root, "BottomLeft");
            UiKit.Anchor(row, new Vector2(0, 0), new Vector2(0, 0), new Vector2(28, 28), Vector2.zero);
            UiKit.HGroup(row, 12, 0, 0, 0, 0, TextAnchor.LowerLeft);
            var fit = row.gameObject.AddComponent<ContentSizeFitter>();
            fit.horizontalFit = fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var current = UiKit.GlassBox(row, "Current", Glass, UiTheme.Border, 10f);
            UiKit.HGroup(current.Outer, 14, 16, 12, 16, 12, TextAnchor.MiddleLeft);
            var dial = UiKit.Rect(current.Outer, "Dial");
            UiKit.Size(dial, 40, 40);
            var ring = UiKit.Circle(dial, "Ring", 40, 40, UiTheme.BorderStrong);
            UiKit.Stretch(ring.rectTransform);
            var hole = UiKit.Circle(dial, "Inner", 36, 36, Hole);
            UiKit.Anchor(hole.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(36, 36));
            var arrow = UiKit.Rect(dial, "Arrow");
            UiKit.Anchor(arrow, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0f), Vector2.zero, new Vector2(3, 14));
            UiKit.Img(arrow, UiTheme.Accent, 1.5f);
            arrow.localEulerAngles = new Vector3(0, 0, -45f);
            var cblock = UiKit.Rect(current.Outer, "Text");
            UiKit.VGroup(cblock, 2);
            Label(cblock, "ARUS");
            var currentText = Value(cblock, 20f, "0.0 kn");

            var tether = Card(row, "Tether", 16, 12);
            Label(tether.Outer, "TETHER KELUAR");
            var tetherText = Value(tether.Outer, 20f, "[___] m");

            UiKit.Bind(hud, "current", currentText);
            UiKit.Bind(hud, "tether", tetherText);
        }

        static void BuildBottomCenter(RectTransform root)
        {
            var row = UiKit.Rect(root, "Modes");
            UiKit.Anchor(row, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 28), Vector2.zero);
            UiKit.HGroup(row, 10, 0, 0, 0, 0, TextAnchor.MiddleCenter);
            var fitter = row.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            foreach (var (label, on) in new[] { ("DP HOLD", true), ("AUTO DEPTH", false), ("AUTO HDG", true) })
            {
                var box = UiKit.GlassBox(row, label, Glass, UiTheme.BorderStrong, 8f, true);
                UiKit.VGroup(box.Outer, 0, 18, 10, 18, 10, TextAnchor.MiddleCenter);
                var text = UiKit.Text(box.Outer, label, 14f, UiTheme.TextSoft, FontStyles.Bold, TextAlignmentOptions.Center, 5f, false);
                var button = UiKit.MakeButton(box.Outer.gameObject, box.Fill);
                var chip = box.Outer.gameObject.AddComponent<ModeChip>();
                UiKit.Bind(chip, "button", button);
                UiKit.Bind(chip, "border", box.Border);
                UiKit.Bind(chip, "fill", box.Fill);
                UiKit.Bind(chip, "label", text);
                UiKit.Bind(chip, "startsOn", on);
            }
        }

        static void BuildBottomRight(RectTransform root, PilotHud hud)
        {
            var row = UiKit.Rect(root, "BottomRight");
            UiKit.Anchor(row, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-28, 28), Vector2.zero);
            UiKit.HGroup(row, 12, 0, 0, 0, 0, TextAnchor.LowerRight);
            var fit = row.gameObject.AddComponent<ContentSizeFitter>();
            fit.horizontalFit = fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            Meter(row, "LAMPU", UiTheme.Hex("F4D58A"), out var lightValue, out var lightFill);
            Meter(row, "TILT KAMERA", UiTheme.Accent, out var tiltValue, out var tiltFill);
            UiKit.Bind(hud, "lightValue", lightValue);
            UiKit.Bind(hud, "lightFill", lightFill);
            UiKit.Bind(hud, "tiltValue", tiltValue);
            UiKit.Bind(hud, "tiltFill", tiltFill);
        }

        static void Meter(Transform parent, string label, Color fillColor, out TMP_Text value, out RectTransform fill)
        {
            var card = Card(parent, label, 16, 12, 10f);
            UiKit.Size(card.Outer, 150);
            var top = UiKit.Rect(card.Outer, "Top");
            UiKit.HGroup(top, 0, 0, 0, 0, 0, TextAnchor.MiddleLeft);
            Label(top, label);
            UiKit.Spacer(top);
            value = UiKit.Mono(UiKit.Text(top, "--", 11f, UiTheme.TextMuted, FontStyles.Normal, TextAlignmentOptions.MidlineRight, 0f, false));
            var track = UiKit.Rect(card.Outer, "Track");
            UiKit.Size(track, -1, 6);
            UiKit.Img(track, UiTheme.Border, 3f);
            fill = UiKit.Rect(track, "Fill");
            UiKit.Stretch(fill);
            UiKit.Img(fill, fillColor, 3f);
        }

        // ---------- sonar ----------

        /// <summary>
        /// Replaces the static sonar drawing of the HUD with a live <see cref="SonarDisplay"/> fed by
        /// <paramref name="sonarSource"/> (an <see cref="ISonarSource"/>). Works on an existing HUD, so the objects that
        /// reference other parts of the canvas keep their references.
        /// </summary>
        public static string AttachSonar(GameObject hudCanvas, MonoBehaviour sonarSource)
        {
            var area = hudCanvas.transform.Find("RightStack/Sonar/Area");
            if (area == null) return "Sonar area not found in " + hudCanvas.name;
            for (int i = area.childCount - 1; i >= 0; i--) Object.DestroyImmediate(area.GetChild(i).gameObject);

            var go = UiKit.Rect(area, "SonarImage");
            UiKit.Stretch(go);
            var raw = go.gameObject.AddComponent<UnityEngine.UI.RawImage>();
            raw.raycastTarget = false;
            var display = go.gameObject.AddComponent<SonarDisplay>();
            UiKit.Bind(display, "sonarSource", sonarSource);
            var label = hudCanvas.transform.Find("RightStack/Sonar/Header/SonarRange");
            if (label != null) UiKit.Bind(display, "rangeLabel", label.GetComponent<TMP_Text>());
            return "sonar display attached" + (sonarSource != null ? " (source " + sonarSource.GetType().Name + ")" : " (no source yet)");
        }

        // ---------- review capture ----------

        /// <summary>Renders the HUD over a dark-blue backdrop to a 1600x900 PNG (works in edit and Play mode).</summary>
        public static string Capture(string path, string canvasName = null)
        {
            canvasName = canvasName ?? CanvasName;
            var go = GameObject.Find(canvasName);
            if (go == null) return "no " + canvasName + " in the open scene";
            var canvas = go.GetComponent<Canvas>();
            int prevDisplay = canvas.targetDisplay;
            var camGo = new GameObject("HudCaptureCamera");
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = UiTheme.Hex("0A3555");
            cam.orthographic = true;
            cam.cullingMask = 0xFFFF;
            cam.targetDisplay = 7;
            var rt = RenderTexture.GetTemporary(1600, 900, 24, RenderTextureFormat.ARGB32);
            var prevMode = canvas.renderMode;
            var prevCamera = canvas.worldCamera;
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = cam;
            canvas.planeDistance = 1f;
            cam.targetTexture = rt;
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)go.transform);
            Canvas.ForceUpdateCanvases();
            // Pixel-positioned content (track dots) redraws in LateUpdate; run it for the capture size.
            foreach (var map in go.GetComponentsInChildren<TrackMap>(true))
                typeof(TrackMap).GetMethod("LateUpdate", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)?.Invoke(map, null);
            cam.Render();
            var tex = new Texture2D(1600, 900, TextureFormat.RGB24, false);
            var active = RenderTexture.active;
            RenderTexture.active = rt;
            tex.ReadPixels(new Rect(0, 0, 1600, 900), 0, 0);
            tex.Apply();
            RenderTexture.active = active;
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            RenderTexture.ReleaseTemporary(rt);
            canvas.renderMode = prevMode;
            canvas.worldCamera = prevCamera;
            canvas.targetDisplay = prevDisplay;
            Object.DestroyImmediate(camGo);
            return path;
        }
    }
}
