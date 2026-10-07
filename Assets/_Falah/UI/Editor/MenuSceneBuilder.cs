using System.IO;
using System.Linq;
using Falah.RovSim.Core;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace Falah.RovSim.UI.Editor
{
    /// <summary>
    /// Builds the menu scene (Login, ROV, Scenario, Briefing) with uGUI from the UI design.
    /// Menu: Falah/UI/Build Menu Scene. Re-running rebuilds Assets/_Falah/Scenes/Menu.unity from scratch.
    /// </summary>
    public static class MenuSceneBuilder
    {
        const string ScenePath = "Assets/_Falah/Scenes/Menu.unity";
        const string SimulationScenePath = "Assets/Scenes/RoVGameplay.unity";
        static readonly string[] StepNames = { "Peran", "ROV", "Skenario", "Briefing" };

        [MenuItem("Falah/UI/Build Menu Scene")]
        public static void Build()
        {
            Directory.CreateDirectory("Assets/_Falah/Scenes");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var camera = CreateCamera();
            CreateEventSystem();
            var canvas = CreateCanvas(camera);

            var login = BuildLogin(canvas);
            var rov = BuildRovSelect(canvas);
            var scenario = BuildScenario(canvas);
            var briefing = BuildBriefing(canvas);

            var flow = canvas.gameObject.AddComponent<MenuFlow>();
            UiKit.BindList(flow, "screens", new Object[] { login, rov, scenario, briefing });
            UiKit.Bind(flow, "simulationScene", "RoVGameplay");

            // Only the first screen is visible at design time; MenuFlow.Start shows the right one at runtime.
            rov.gameObject.SetActive(false);
            scenario.gameObject.SetActive(false);
            briefing.gameObject.SetActive(false);

            EditorSceneManager.SaveScene(scene, ScenePath);
            EnsureBuildSettings();
            Debug.Log("Menu scene built: " + ScenePath);
        }

        // ---------- scene scaffolding ----------

        internal static Camera CreateCamera()
        {
            var go = new GameObject("Main Camera") { tag = "MainCamera" };
            var cam = go.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = UiTheme.Bg;
            cam.orthographic = true;
            return cam;
        }

        internal static void CreateEventSystem()
        {
            var go = new GameObject("EventSystem", typeof(EventSystem));
            var module = go.AddComponent<InputSystemUIInputModule>();
            if (module.actionsAsset == null) module.AssignDefaultActions();
        }

        internal static RectTransform CreateCanvas(Camera camera)
        {
            var go = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = go.GetComponent<Canvas>();
            // Screen Space - Camera (not Overlay) so the menu can be captured from the camera in tests and screenshots.
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = 1f;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600, 900);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            return (RectTransform)go.transform;
        }

        internal const string DebriefScenePath = "Assets/_Falah/Scenes/Debrief.unity";

        /// <summary>Build order: Menu, RoVGameplay, Debrief; any other scene already listed keeps its place after them.</summary>
        internal static void EnsureBuildSettings()
        {
            var ordered = new[] { ScenePath, SimulationScenePath, DebriefScenePath };
            var list = new System.Collections.Generic.List<EditorBuildSettingsScene>();
            foreach (var path in ordered)
                if (File.Exists(path))
                    list.Add(new EditorBuildSettingsScene(path, EditorBuildSettings.scenes.FirstOrDefault(s => s.path == path)?.enabled ?? true));
            list.AddRange(EditorBuildSettings.scenes.Where(s => !ordered.Contains(s.path)));
            EditorBuildSettings.scenes = list.ToArray();
        }

        // ---------- shared chrome ----------

        internal static RectTransform NewScreen(Transform canvas, string name)
        {
            var r = UiKit.Rect(canvas, name);
            UiKit.Stretch(r);
            UiKit.Img(r, UiTheme.Bg, 0f, true);
            return r;
        }

        static ScreenHeader BuildHeader(RectTransform screen, int step, bool showRovChip)
        {
            var bar = UiKit.Rect(screen, "Header");
            UiKit.Img(bar, UiTheme.Panel);
            UiKit.Size(bar, -1, 68);
            UiKit.HGroup(bar, 40, 40, 0, 40, 0, TextAnchor.MiddleLeft);

            var line = UiKit.Rect(bar, "BottomBorder");
            UiKit.Anchor(line, new Vector2(0.5f, 0), new Vector2(0.5f, 0), Vector2.zero, Vector2.zero);
            line.anchorMin = new Vector2(0, 0); line.anchorMax = new Vector2(1, 0); line.sizeDelta = new Vector2(0, 1);
            UiKit.IgnoreLayout(line);
            UiKit.Img(line, UiTheme.Border);

            BuildBrand(bar, 15f, 11f, 30f, UiTheme.Panel);

            var stepper = UiKit.Rect(bar, "Stepper");
            UiKit.HGroup(stepper, 14, 0, 0, 0, 0, TextAnchor.MiddleLeft);
            for (int i = 1; i <= 4; i++)
            {
                if (i > 1)
                {
                    var c = UiKit.Rect(stepper, "Connector");
                    UiKit.Size(c, 28, 1);
                    UiKit.Img(c, UiTheme.BorderStrong);
                }
                BuildStep(stepper, i, StepNames[i - 1], i < step ? 0 : i == step ? 1 : 2);
            }

            UiKit.Spacer(bar);

            var rovChip = UiKit.Chip(bar, "Tortuga", UiTheme.Panel, UiTheme.Accent, UiTheme.Text, 14, 12, 6);
            rovChip.Outer.gameObject.SetActive(showRovChip);
            var roleChip = UiKit.Chip(bar, "Trainee", UiTheme.Panel, UiTheme.BorderStrong, UiTheme.TextSoft, 14, 12, 6);
            var user = UiKit.Text(bar, "[Nama pengguna]", 14f, UiTheme.TextSoft, FontStyles.Normal, TextAlignmentOptions.MidlineLeft, 0f, false);

            var header = bar.gameObject.AddComponent<ScreenHeader>();
            UiKit.Bind(header, "rovChip", rovChip.Outer.gameObject);
            UiKit.Bind(header, "rovText", rovChip.Outer.GetComponentInChildren<TMP_Text>());
            UiKit.Bind(header, "roleText", roleChip.Outer.GetComponentInChildren<TMP_Text>());
            UiKit.Bind(header, "userText", user);
            return header;
        }

        static void BuildBrand(RectTransform parent, float titleSize, float subSize, float logoSize, Color surface)
        {
            var brand = UiKit.Rect(parent, "Brand");
            UiKit.HGroup(brand, 12, 0, 0, 0, 0, TextAnchor.MiddleLeft);
            var logo = UiKit.MakeBox(brand, "Logo", surface, UiTheme.Accent, 1.5f, 8f);
            UiKit.Size(logo.Outer, logoSize, logoSize * 0.6f);
            var dot = UiKit.Rect(logo.Outer, "Lens");
            UiKit.Anchor(dot, new Vector2(0.72f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(logoSize * 0.28f, logoSize * 0.28f));
            UiKit.Img(dot, UiTheme.Accent, 999f);
            UiKit.IgnoreLayout(dot);
            var text = UiKit.Rect(brand, "Title");
            UiKit.VGroup(text, 2);
            UiKit.Text(text, "ROV TRAINER", titleSize, UiTheme.Text, FontStyles.Bold, TextAlignmentOptions.TopLeft, 12f, false);
            UiKit.Text(text, "PUSHIDROSAL TNI AL", subSize, UiTheme.TextMuted, FontStyles.Normal, TextAlignmentOptions.TopLeft, 8f, false);
        }

        /// <param name="state">0 done, 1 current, 2 todo</param>
        static void BuildStep(RectTransform parent, int number, string label, int state)
        {
            var item = UiKit.Rect(parent, "Step" + number);
            UiKit.HGroup(item, 8, 0, 0, 0, 0, TextAnchor.MiddleLeft);
            Color fill = state == 0 ? UiTheme.Ok : state == 1 ? UiTheme.Accent : UiTheme.Panel;
            Color border = state == 2 ? UiTheme.BorderStrong : fill;
            var circle = UiKit.MakeBox(item, "Circle", fill, border, state == 2 ? 1f : 0f, 999f);
            UiKit.Size(circle.Outer, 24, 24);
            UiKit.VGroup(circle.Outer, 0, 0, 0, 0, 0, TextAnchor.MiddleCenter);
            UiKit.Text(circle.Outer, number.ToString(), 13f, state == 2 ? UiTheme.TextDim : UiTheme.OnAccent, FontStyles.Bold, TextAlignmentOptions.Center, 0f, false);
            Color labelColor = state == 0 ? UiTheme.Ok : state == 1 ? UiTheme.Text : UiTheme.TextDim;
            UiKit.Text(item, label, 14f, labelColor, state == 1 ? FontStyles.Bold : FontStyles.Normal, TextAlignmentOptions.MidlineLeft, 0f, false);
        }

        static void BuildFooter(RectTransform screen, out RectTransform left, out RectTransform right)
        {
            var bar = UiKit.Rect(screen, "Footer");
            UiKit.Img(bar, UiTheme.Panel);
            UiKit.Size(bar, -1, 88);
            UiKit.HGroup(bar, 0, 40, 0, 40, 0, TextAnchor.MiddleLeft);
            var line = UiKit.Rect(bar, "TopBorder");
            line.anchorMin = new Vector2(0, 1); line.anchorMax = new Vector2(1, 1); line.pivot = new Vector2(0.5f, 1); line.sizeDelta = new Vector2(0, 1);
            UiKit.IgnoreLayout(line);
            UiKit.Img(line, UiTheme.Border);
            left = UiKit.Rect(bar, "Left");
            UiKit.HGroup(left, 16, 0, 0, 0, 0, TextAnchor.MiddleLeft);
            UiKit.Spacer(bar);
            right = UiKit.Rect(bar, "Right");
            UiKit.HGroup(right, 12, 0, 0, 0, 0, TextAnchor.MiddleRight);
        }

        static RectTransform NewScreenWithBody(Transform canvas, string name, int step, bool rovChip, out ScreenHeader header, out RectTransform body)
        {
            var screen = NewScreen(canvas, name);
            UiKit.VGroup(screen, 0, 0, 0, 0, 0, TextAnchor.UpperLeft, true, false);
            header = BuildHeader(screen, step, rovChip);
            body = UiKit.Rect(screen, "Body");
            UiKit.Size(body, -1, -1, -1, 1);
            return screen;
        }

        static void SetScreenFields(MenuScreen screen, Button next, Button back, ScreenHeader header)
        {
            UiKit.Bind(screen, "nextButton", next);
            UiKit.Bind(screen, "backButton", back);
            UiKit.Bind(screen, "header", header);
        }

        internal static TMP_Text SectionLabel(Transform parent, string text) =>
            UiKit.Text(parent, text, 13f, UiTheme.TextMuted, FontStyles.Bold, TextAlignmentOptions.TopLeft, 10f, false);

        // ---------- 1. Login ----------

        static LoginScreen BuildLogin(RectTransform canvas)
        {
            var screen = NewScreen(canvas, "LoginScreen");
            UiKit.HGroup(screen, 0, 0, 0, 0, 0, TextAnchor.UpperLeft, false, true);

            // hero
            var hero = UiKit.Rect(screen, "Hero");
            var heroImg = UiKit.Img(hero, Color.white);
            heroImg.sprite = UiKit.GradientSprite("hero_gradient", UiTheme.Hex("0B3050"), UiTheme.Hex("082038"), 0.38f, UiTheme.Hex("050E1A"));
            UiKit.Size(hero, -1, -1, 1);
            UiKit.VGroup(hero, 0, 80, 72, 80, 72, TextAnchor.UpperLeft, false, false);
            BuildBrand(hero, 20f, 12f, 40f, UiTheme.Hex("0B3050"));
            UiKit.Spacer(hero);
            var headline = UiKit.Rect(hero, "Headline");
            UiKit.Size(headline, 620);
            UiKit.VGroup(headline, 0);
            UiKit.Text(headline, "Latihan operasi ROV tanpa kapal dan tanpa risiko.", 52f, UiTheme.Text, FontStyles.Bold);
            UiKit.Spacer(headline, 0, 24);
            UiKit.Text(headline, "Satu simulator untuk empat ROV Pushidrosal: ECA Hytec, Teledyne, Tortuga, dan Mariner XL.", 19f, UiTheme.TextSoft);
            UiKit.Spacer(headline, 0, 32);
            var chips = UiKit.Rect(headline, "Chips");
            UiKit.HGroup(chips, 10, 0, 0, 0, 0, TextAnchor.MiddleLeft);
            foreach (var label in new[] { "Offline penuh", "On-premise", "Rekaman dan penilaian" })
                UiKit.Chip(chips, label, UiTheme.Hex("0A2640"), UiTheme.BorderStrong, UiTheme.TextPale, 13f, 14, 8);
            UiKit.Spacer(hero);
            UiKit.Text(hero, "Dikembangkan oleh Falah Inovasi Teknologi", 13f, UiTheme.TextDim, FontStyles.Normal, TextAlignmentOptions.TopLeft, 0f, false);
            var glyph = UiKit.Glyph(hero, 150f);
            UiKit.IgnoreLayout(glyph);
            UiKit.Anchor(glyph, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-110, -110), new Vector2(150, 96));

            // form panel
            var form = UiKit.Rect(screen, "Form");
            UiKit.Img(form, UiTheme.Panel, 0f, true);
            UiKit.Size(form, 560);
            UiKit.VGroup(form, 28, 64, 96, 64, 96);
            var edge = UiKit.Rect(form, "LeftBorder");
            edge.anchorMin = new Vector2(0, 0); edge.anchorMax = new Vector2(0, 1); edge.pivot = new Vector2(0, 0.5f); edge.sizeDelta = new Vector2(1, 0);
            UiKit.IgnoreLayout(edge);
            UiKit.Img(edge, UiTheme.Border);

            var title = UiKit.Rect(form, "Title");
            UiKit.VGroup(title, 8);
            UiKit.Text(title, "Masuk", 30f, UiTheme.Text, FontStyles.Bold);
            UiKit.Text(title, "Mode demo: nama pengguna dan kata sandi apa saja diterima.", 15f, UiTheme.TextMuted);

            var userField = LabeledInput(form, "Nama pengguna", "Nama pengguna", false);
            var passField = LabeledInput(form, "Kata sandi", "Kata sandi", true);

            var roleBlock = UiKit.Rect(form, "Role");
            UiKit.VGroup(roleBlock, 8);
            UiKit.Text(roleBlock, "Masuk sebagai", 13f, UiTheme.TextSoft, FontStyles.Bold);
            var roleRow = UiKit.Rect(roleBlock, "Choices");
            UiKit.HGroup(roleRow, 8, 0, 0, 0, 0, TextAnchor.MiddleLeft, true, true);
            var roles = new[]
            {
                (LoginScreen.TraineeId, "Trainee"), (LoginScreen.InstructorId, "Instruktur"), (LoginScreen.AdministratorId, "Administrator"),
            };
            var roleChoices = roles.Select(r =>
            {
                var box = UiKit.MakeBox(roleRow, r.Item1, UiTheme.Card, UiTheme.BorderStrong, 1f, 8f);
                UiKit.Size(box.Outer, 0, 52, 1);
                UiKit.VGroup(box.Outer, 0, 8, 0, 8, 0, TextAnchor.MiddleCenter);
                UiKit.Text(box.Outer, r.Item2, 15f, UiTheme.Text, FontStyles.Bold, TextAlignmentOptions.Center, 0f, false);
                return UiKit.MakeChoice(box, r.Item1, true, 1f, 1.5f);
            }).ToArray();
            var roleGroup = UiKit.MakeGroup(roleRow.gameObject, roleChoices);

            var error = UiKit.Text(form, string.Empty, 13f, UiTheme.Danger, FontStyles.Normal, TextAlignmentOptions.MidlineLeft, 0f, false);
            UiKit.Size(error.rectTransform, -1, 18);
            var login = UiKit.PrimaryButton(form, "Masuk", 56, 17);

            UiKit.Spacer(form);
            var status = UiKit.Rect(form, "Status");
            UiKit.VGroup(status, 10);
            var tileRow = StatusRow(status, "Memeriksa server tile lokal...", UiTheme.Warn, out var tileDot, out var tileLabel);
            StatusRow(status, "Mode offline aktif", UiTheme.Ok, out _, out _);
            UiKit.Text(status, "Versi draft 0.1", 12f, UiTheme.TextDim, FontStyles.Normal, TextAlignmentOptions.TopLeft, 0f, false);
            var tile = tileRow.gameObject.AddComponent<TileServerStatus>();
            UiKit.Bind(tile, "dot", tileDot);
            UiKit.Bind(tile, "label", tileLabel);

            var comp = screen.gameObject.AddComponent<LoginScreen>();
            UiKit.Bind(comp, "userField", userField);
            UiKit.Bind(comp, "passwordField", passField);
            UiKit.Bind(comp, "roleGroup", roleGroup);
            UiKit.Bind(comp, "errorText", error);
            SetScreenFields(comp, login, null, null);
            return comp;
        }

        static TMP_InputField LabeledInput(Transform parent, string label, string placeholder, bool password)
        {
            var block = UiKit.Rect(parent, label);
            UiKit.VGroup(block, 8);
            UiKit.Text(block, label, 13f, UiTheme.TextSoft, FontStyles.Bold);
            return UiKit.InputField(block, placeholder, password);
        }

        static RectTransform StatusRow(Transform parent, string text, Color dotColor, out Image dot, out TMP_Text label)
        {
            var row = UiKit.Rect(parent, "StatusRow");
            UiKit.HGroup(row, 8, 0, 0, 0, 0, TextAnchor.MiddleLeft);
            var d = UiKit.Rect(row, "Dot");
            UiKit.Size(d, 8, 8);
            dot = UiKit.Img(d, dotColor, 4f);
            label = UiKit.Text(row, text, 13f, UiTheme.TextDim, FontStyles.Normal, TextAlignmentOptions.MidlineLeft, 0f, false);
            return row;
        }

        // ---------- 2. Pilih ROV ----------

        static RovSelectScreen BuildRovSelect(RectTransform canvas)
        {
            var screen = NewScreenWithBody(canvas, "RovSelectScreen", 2, false, out var header, out var body);
            UiKit.VGroup(body, 24, 40, 36, 40, 0);

            var titleRow = UiKit.Rect(body, "TitleRow");
            UiKit.HGroup(titleRow, 0, 0, 0, 0, 0, TextAnchor.LowerLeft);
            var titles = UiKit.Rect(titleRow, "Titles");
            UiKit.VGroup(titles, 8);
            UiKit.Text(titles, "Pilih ROV", 34f, UiTheme.Text, FontStyles.Bold);
            UiKit.Text(titles, "Panel kontrol, fisika, dan alat mengikuti ROV yang dipilih.", 16f, UiTheme.TextMuted);
            UiKit.Spacer(titleRow);
            var note = UiKit.Rect(titleRow, "Note");
            UiKit.HGroup(note, 8, 0, 0, 0, 0, TextAnchor.MiddleLeft);
            var noteDot = UiKit.Rect(note, "Dot");
            UiKit.Size(noteDot, 8, 8);
            UiKit.Img(noteDot, UiTheme.Warn, 4f);
            UiKit.Text(note, "Spesifikasi dari data publik, belum diverifikasi dengan datasheet unit Pushidrosal", 13f, UiTheme.Warn, FontStyles.Normal, TextAlignmentOptions.MidlineLeft, 0f, false);

            var grid = UiKit.Rect(body, "Grid");
            UiKit.HGroup(grid, 20, 0, 0, 0, 0, TextAnchor.UpperLeft, true, false);
            var artSprite = UiKit.GradientSprite("card_gradient", UiTheme.Hex("0B3050"), UiTheme.Hex("0A2640"), 0.5f, UiTheme.Hex("07192D"));

            var choices = MenuCatalog.Rovs.Select(rov => BuildRovCard(grid, rov, artSprite)).ToArray();
            var group = UiKit.MakeGroup(grid.gameObject, choices);

            BuildFooter(screen, out var left, out var right);
            UiKit.Text(left, "Dipilih", 14f, UiTheme.TextMuted, FontStyles.Normal, TextAlignmentOptions.MidlineLeft, 0f, false);
            var selected = UiKit.Text(left, "Tortuga", 18f, UiTheme.Text, FontStyles.Bold, TextAlignmentOptions.MidlineLeft, 0f, false);
            UiKit.Text(left, "Kontrol: gamepad XInput", 14f, UiTheme.TextMuted, FontStyles.Normal, TextAlignmentOptions.MidlineLeft, 0f, false);
            var back = UiKit.SecondaryButton(right, "Kembali", 52, 16);
            var next = UiKit.PrimaryButton(right, "Lanjut: pilih skenario", 52, 16);

            var comp = screen.gameObject.AddComponent<RovSelectScreen>();
            UiKit.Bind(comp, "rovGroup", group);
            UiKit.Bind(comp, "selectedLabel", selected);
            SetScreenFields(comp, next, back, header);
            return comp;
        }

        static Choice BuildRovCard(RectTransform grid, RovOption rov, Sprite art)
        {
            var box = UiKit.MakeBox(grid, rov.Id, UiTheme.Card, UiTheme.BorderStrong, 1f, 12f);
            UiKit.Size(box.Outer, 0, -1, 1);
            UiKit.VGroup(box.Outer, 0, 2, 2, 2, 2);

            var image = UiKit.Rect(box.Outer, "Art");
            UiKit.Size(image, -1, 190);
            var artImg = UiKit.Img(image, Color.white);
            artImg.sprite = art;
            var glyph = UiKit.Glyph(image, rov.GlyphWidth);
            UiKit.Anchor(glyph, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(rov.GlyphWidth, 96));
            var badge = UiKit.Chip(image, "Dipilih", UiTheme.Accent, Color.clear, UiTheme.OnAccent, 12f, 10, 4, FontStyles.Bold);
            UiKit.IgnoreLayout(badge.Outer);
            UiKit.Anchor(badge.Outer, new Vector2(0, 1), new Vector2(0, 1), new Vector2(14, -14), new Vector2(76, 24));
            badge.Outer.gameObject.SetActive(false);

            var content = UiKit.Rect(box.Outer, "Body");
            UiKit.VGroup(content, 14, 22, 20, 22, 22);
            var names = UiKit.Rect(content, "Names");
            UiKit.VGroup(names, 2);
            UiKit.Text(names, rov.DisplayName, 24f, UiTheme.Text, FontStyles.Bold);
            UiKit.Text(names, rov.Subtitle, 14f, UiTheme.TextMuted);

            var tags = UiKit.Rect(content, "Tags");
            UiKit.VGroup(tags, 8, 0, 0, 0, 0, TextAnchor.UpperLeft, false, false);
            foreach (var tag in rov.Tags) UiKit.Chip(tags, tag, UiTheme.Tag, Color.clear, UiTheme.TextPale, 12f, 10, 4);
            foreach (var tag in rov.WarningTags) UiKit.Chip(tags, tag, UiTheme.WarnBg, Color.clear, UiTheme.Warn, 12f, 10, 4);

            var specs = UiKit.Rect(content, "Specs");
            UiKit.VGroup(specs, 8);
            foreach (var row in rov.Specs)
            {
                var r = UiKit.Rect(specs, "Spec");
                UiKit.HGroup(r, 8, 0, 0, 0, 0, TextAnchor.MiddleLeft);
                UiKit.Text(r, row.Label, 14f, UiTheme.TextMuted, FontStyles.Normal, TextAlignmentOptions.MidlineLeft, 0f, false);
                UiKit.Spacer(r);
                var value = UiKit.Text(r, row.Value, 14f, UiTheme.TextPale, FontStyles.Normal, TextAlignmentOptions.MidlineRight, 0f, false);
                if (row.Mono) UiKit.Mono(value);
            }
            return UiKit.MakeChoice(box, rov.Id, rov.Selectable, 1f, 2f, badge.Outer.gameObject);
        }

        // ---------- 3. Mode, skenario, lingkungan ----------

        static ScenarioScreen BuildScenario(RectTransform canvas)
        {
            var screen = NewScreenWithBody(canvas, "ScenarioScreen", 3, true, out var header, out var body);
            UiKit.HGroup(body, 24, 40, 28, 40, 0, TextAnchor.UpperLeft, false, false);

            // modes
            var modeCol = UiKit.Rect(body, "Modes");
            UiKit.Size(modeCol, 330);
            UiKit.VGroup(modeCol, 14);
            SectionLabel(modeCol, "MODE LATIHAN");
            var modeChoices = MenuCatalog.Modes.Select(m =>
            {
                var box = UiKit.MakeBox(modeCol, m.Id, UiTheme.Card, UiTheme.BorderStrong, 1f, 10f);
                UiKit.VGroup(box.Outer, 4, 18, 16, 18, 16);
                UiKit.Text(box.Outer, m.DisplayName, 17f, UiTheme.Text, FontStyles.Bold);
                UiKit.Text(box.Outer, m.Description, 13f, UiTheme.TextMuted);
                return UiKit.MakeChoice(box, m.Id, m.Selectable);
            }).ToArray();
            var modeGroup = UiKit.MakeGroup(modeCol.gameObject, modeChoices);

            // scenarios
            var scenCol = UiKit.Rect(body, "Scenarios");
            UiKit.Size(scenCol, 0, -1, 1);
            UiKit.VGroup(scenCol, 14);
            SectionLabel(scenCol, "SKENARIO");
            var scenChoices = MenuCatalog.Scenarios.Select(s =>
            {
                var box = UiKit.MakeBox(scenCol, s.Id, UiTheme.Card, UiTheme.BorderStrong, 1f, 10f);
                UiKit.HGroup(box.Outer, 16, 20, 16, 20, 16, TextAnchor.MiddleLeft, false, false);
                var texts = UiKit.Rect(box.Outer, "Texts");
                UiKit.Size(texts, 0, -1, 1);
                UiKit.VGroup(texts, 4);
                UiKit.Text(texts, s.DisplayName, 17f, UiTheme.Text, FontStyles.Bold);
                UiKit.Text(texts, s.Description, 13f, UiTheme.TextMuted);
                Color fg = s.Level == LevelKind.Basic ? UiTheme.Ok : s.Level == LevelKind.Medium ? UiTheme.Warn : UiTheme.Danger;
                Color bg = s.Level == LevelKind.Basic ? UiTheme.OkBg : s.Level == LevelKind.Medium ? UiTheme.WarnBg : UiTheme.DangerBg;
                UiKit.Chip(box.Outer, s.LevelLabel, bg, Color.clear, fg, 12f, 10, 4, FontStyles.Bold);
                return UiKit.MakeChoice(box, s.Id, s.Selectable);
            }).ToArray();
            var scenGroup = UiKit.MakeGroup(scenCol.gameObject, scenChoices);
            var custom = UiKit.MakeBox(scenCol, "Custom", UiTheme.Bg, UiTheme.BorderStrong, 1f, 10f);
            UiKit.VGroup(custom.Outer, 4, 20, 16, 20, 16);
            UiKit.Text(custom.Outer, "Skenario khusus Pushidrosal", 17f, UiTheme.TextMuted, FontStyles.Bold);
            UiKit.Text(custom.Outer, "[___]", 13f, UiTheme.TextDim);

            // environment
            var envCol = UiKit.Rect(body, "Environment");
            UiKit.Size(envCol, 420);
            UiKit.VGroup(envCol, 16);
            SectionLabel(envCol, "LINGKUNGAN");

            var locCard = UiKit.MakeBox(envCol, "Location", UiTheme.Card, UiTheme.Border, 1f, 10f);
            UiKit.VGroup(locCard.Outer, 8, 18, 16, 18, 16);
            UiKit.Text(locCard.Outer, "Lokasi latihan (data batimetri)", 13f, UiTheme.TextMuted);
            var drop = UiKit.MakeBox(locCard.Outer, "Location", UiTheme.Panel, UiTheme.BorderStrong, 1f, 8f);
            UiKit.Size(drop.Outer, -1, 44);
            UiKit.HGroup(drop.Outer, 0, 14, 0, 14, 0, TextAnchor.MiddleLeft);
            var locText = UiKit.Text(drop.Outer, SessionSetup.DefaultLocation, 15f, UiTheme.Text, FontStyles.Normal, TextAlignmentOptions.MidlineLeft, 0f, false);
            var map = UiKit.MakeBox(locCard.Outer, "Map", UiTheme.Hex("0A1B2E"), Color.clear, 0f, 6f);
            UiKit.Size(map.Outer, -1, 96);
            UiKit.VGroup(map.Outer, 0, 0, 0, 0, 0, TextAnchor.MiddleCenter);
            UiKit.Text(map.Outer, "Peta kontur (data lokal)", 12f, UiTheme.TextDim, FontStyles.Normal, TextAlignmentOptions.Center, 0f, false);

            var sliders = UiKit.MakeBox(envCol, "Sliders", UiTheme.Card, UiTheme.Border, 1f, 10f);
            UiKit.VGroup(sliders.Outer, 18, 18, 16, 18, 16);
            var current = UiKit.SliderRow(sliders.Outer, "Arus", 0f, 4f, 1.5f, " kn", "0.#", false);
            var visibility = UiKit.SliderRow(sliders.Outer, "Visibilitas", 0f, 20f, 4f, " m", "0.#", false);
            var depth = UiKit.SliderRow(sliders.Outer, "Kedalaman target", 0f, 250f, 32f, " m", "0", true);
            var timeRow = UiKit.Rect(sliders.Outer, "TimeOfDay");
            UiKit.HGroup(timeRow, 8, 0, 0, 0, 0, TextAnchor.MiddleLeft, true, true);
            var timeChoices = new[] { (ScenarioScreen.DayId, "Siang"), (ScenarioScreen.DuskId, "Senja"), (ScenarioScreen.NightId, "Malam") }.Select(t =>
            {
                var box = UiKit.MakeBox(timeRow, t.Item1, UiTheme.Card, UiTheme.BorderStrong, 1f, 8f);
                UiKit.Size(box.Outer, 0, 40, 1);
                UiKit.VGroup(box.Outer, 0, 8, 0, 8, 0, TextAnchor.MiddleCenter);
                UiKit.Text(box.Outer, t.Item2, 14f, UiTheme.Text, FontStyles.Bold, TextAlignmentOptions.Center, 0f, false);
                return UiKit.MakeChoice(box, t.Item1, true, 1f, 1.5f);
            }).ToArray();
            var timeGroup = UiKit.MakeGroup(timeRow.gameObject, timeChoices);

            var dist = UiKit.MakeBox(envCol, "Disturbances", UiTheme.Card, UiTheme.Border, 1f, 10f);
            UiKit.VGroup(dist.Outer, 12, 18, 16, 18, 16);
            UiKit.Text(dist.Outer, "Gangguan terjadwal", 14f, UiTheme.TextSoft);
            var leak = UiKit.SwitchRow(dist.Outer, "Kebocoran thruster", true);
            var lights = UiKit.SwitchRow(dist.Outer, "Lampu padam", false);
            var tether = UiKit.SwitchRow(dist.Outer, "Tether tersangkut", false);

            BuildFooter(screen, out var left, out var right);
            UiKit.Text(left, "Mode", 14f, UiTheme.TextMuted, FontStyles.Normal, TextAlignmentOptions.MidlineLeft, 0f, false);
            var summaryMode = UiKit.Text(left, "Misi penuh", 14f, UiTheme.Text, FontStyles.Bold, TextAlignmentOptions.MidlineLeft, 0f, false);
            UiKit.Spacer(left, 0, 8);
            UiKit.Text(left, "Skenario", 14f, UiTheme.TextMuted, FontStyles.Normal, TextAlignmentOptions.MidlineLeft, 0f, false);
            var summaryScenario = UiKit.Text(left, "Investigasi target sonar", 14f, UiTheme.Text, FontStyles.Bold, TextAlignmentOptions.MidlineLeft, 0f, false);
            var back = UiKit.SecondaryButton(right, "Kembali", 52, 16);
            var next = UiKit.PrimaryButton(right, "Lanjut: briefing", 52, 16);

            var comp = screen.gameObject.AddComponent<ScenarioScreen>();
            UiKit.Bind(comp, "modeGroup", modeGroup);
            UiKit.Bind(comp, "scenarioGroup", scenGroup);
            UiKit.Bind(comp, "timeGroup", timeGroup);
            UiKit.Bind(comp, "current", current);
            UiKit.Bind(comp, "visibility", visibility);
            UiKit.Bind(comp, "targetDepth", depth);
            UiKit.Bind(comp, "thrusterLeak", leak);
            UiKit.Bind(comp, "lightsOut", lights);
            UiKit.Bind(comp, "tetherSnag", tether);
            UiKit.Bind(comp, "summaryMode", summaryMode);
            UiKit.Bind(comp, "summaryScenario", summaryScenario);
            UiKit.Bind(comp, "locationText", locText);
            SetScreenFields(comp, next, back, header);
            return comp;
        }

        // ---------- 4. Briefing dan pre-dive check ----------

        static BriefingScreen BuildBriefing(RectTransform canvas)
        {
            var screen = NewScreenWithBody(canvas, "BriefingScreen", 4, true, out var header, out var body);
            UiKit.HGroup(body, 24, 40, 28, 40, 0, TextAnchor.UpperLeft, false, true);

            // mission card
            var mission = UiKit.MakeBox(body, "Mission", UiTheme.Card, UiTheme.Border, 1f, 12f);
            UiKit.Size(mission.Outer, 520);
            UiKit.VGroup(mission.Outer, 18, 28, 28, 28, 28);
            SectionLabel(mission.Outer, "BRIEFING MISI");
            var title = UiKit.Text(mission.Outer, "Investigasi target sonar", 28f, UiTheme.Text, FontStyles.Bold);

            var grid = UiKit.Rect(mission.Outer, "Info");
            UiKit.VGroup(grid, 16);
            var rovValue = InfoCell(grid, "ROV", "Tortuga", out var depthValue, "Kedalaman target", "32 m");
            var envValue = InfoCell(grid, "Arus / visibilitas", "1.5 kn / 4 m", out var timeValue, "Batas waktu", "[___] menit");

            SectionLabel(mission.Outer, "OBJEKTIF");
            var objectives = new TMP_Text[4];
            for (int i = 0; i < objectives.Length; i++)
            {
                var row = UiKit.Rect(mission.Outer, "Objective" + (i + 1));
                UiKit.HGroup(row, 14, 0, 0, 0, 0, TextAnchor.UpperLeft);
                var num = UiKit.MakeBox(row, "Number", UiTheme.Card, UiTheme.Accent, 1f, 999f);
                UiKit.Size(num.Outer, 24, 24);
                UiKit.VGroup(num.Outer, 0, 0, 0, 0, 0, TextAnchor.MiddleCenter);
                UiKit.Text(num.Outer, (i + 1).ToString(), 13f, UiTheme.Accent, FontStyles.Bold, TextAlignmentOptions.Center, 0f, false);
                var text = UiKit.Text(row, "-", 15f, UiTheme.TextSoft);
                UiKit.Size(text.rectTransform, 0, -1, 1);
                objectives[i] = text;
            }
            UiKit.Spacer(mission.Outer);
            UiKit.Text(mission.Outer, "Gangguan dapat terjadi selama misi. Instruktur mengendalikan waktunya.", 13f, UiTheme.TextDim);

            // map placeholder
            var map = UiKit.MakeBox(body, "Map", UiTheme.Hex("0A1B2E"), UiTheme.Border, 1f, 12f);
            UiKit.Size(map.Outer, 0, -1, 1);
            UiKit.VGroup(map.Outer, 0, 0, 0, 0, 0, TextAnchor.MiddleCenter);
            UiKit.Text(map.Outer, "Peta area operasi (data lokal)", 16f, UiTheme.TextDim, FontStyles.Normal, TextAlignmentOptions.Center, 0f, false);
            var cap = UiKit.Chip(map.Outer, "Area operasi kontur 5 m", UiTheme.Panel, UiTheme.BorderStrong, UiTheme.TextSoft, 13f, 12, 6);
            UiKit.IgnoreLayout(cap.Outer);
            UiKit.Anchor(cap.Outer, new Vector2(0, 0), new Vector2(0, 0), new Vector2(18, 18), new Vector2(210, 30));

            // pre-dive check
            var check = UiKit.MakeBox(body, "PreDive", UiTheme.Card, UiTheme.Border, 1f, 12f);
            UiKit.Size(check.Outer, 420);
            UiKit.VGroup(check.Outer, 14, 28, 28, 28, 28);
            SectionLabel(check.Outer, "PRE-DIVE CHECK");
            var items = new[]
            {
                ("Daya dan baterai", "100%"), ("Thruster", "4 / 4"), ("Kamera dan lampu", "OK"), ("Sensor kebocoran", "Kering"),
                ("Tether dan komunikasi", "OK"), ("Sonar imaging", "Terpasang"), ("Kontrol gamepad", "Belum dikalibrasi"),
            };
            var icons = new TMP_Text[items.Length];
            var values = new TMP_Text[items.Length];
            for (int i = 0; i < items.Length; i++)
            {
                var row = UiKit.Rect(check.Outer, "Check" + (i + 1));
                UiKit.HGroup(row, 10, 0, 0, 0, 0, TextAnchor.MiddleLeft);
                bool pending = i == items.Length - 1;
                icons[i] = UiKit.Text(row, pending ? "CEK" : "OK", 12f, pending ? UiTheme.Warn : UiTheme.Ok, FontStyles.Bold, TextAlignmentOptions.MidlineLeft, 0f, false);
                UiKit.Size(icons[i].rectTransform, 26);
                UiKit.Text(row, items[i].Item1, 15f, UiTheme.Text, FontStyles.Normal, TextAlignmentOptions.MidlineLeft, 0f, false);
                UiKit.Spacer(row);
                values[i] = UiKit.Text(row, items[i].Item2, 14f, pending ? UiTheme.Warn : UiTheme.Ok, FontStyles.Normal, TextAlignmentOptions.MidlineRight, 0f, false);
            }
            var calibrate = UiKit.SecondaryButton(check.Outer, "Kalibrasi kontrol", 44, 15, 20, UiTheme.Card);
            UiKit.Spacer(check.Outer);
            var summary = UiKit.Text(check.Outer, "Satu pemeriksaan tersisa sebelum simulasi dapat dimulai.", 14f, UiTheme.TextMuted);

            BuildFooter(screen, out var left, out var right);
            UiKit.Text(left, "Pastikan semua pemeriksaan lulus sebelum memulai.", 14f, UiTheme.TextMuted, FontStyles.Normal, TextAlignmentOptions.MidlineLeft, 0f, false);
            var back = UiKit.SecondaryButton(right, "Kembali", 52, 16);
            var start = UiKit.PrimaryButton(right, "Mulai simulasi", 52, 16);

            var comp = screen.gameObject.AddComponent<BriefingScreen>();
            UiKit.Bind(comp, "scenarioTitle", title);
            UiKit.Bind(comp, "rovValue", rovValue);
            UiKit.Bind(comp, "depthValue", depthValue);
            UiKit.Bind(comp, "environmentValue", envValue);
            UiKit.Bind(comp, "timeLimitValue", timeValue);
            UiKit.BindList(comp, "objectiveTexts", objectives);
            UiKit.BindList(comp, "checkIcons", icons);
            UiKit.BindList(comp, "checkValues", values);
            UiKit.Bind(comp, "calibrateButton", calibrate);
            UiKit.Bind(comp, "checkSummary", summary);
            SetScreenFields(comp, start, back, header);
            return comp;
        }

        /// <summary>Two label/value cells side by side; returns the first value, outputs the second.</summary>
        static TMP_Text InfoCell(Transform parent, string label1, string value1, out TMP_Text second, string label2, string value2)
        {
            var row = UiKit.Rect(parent, "Row");
            UiKit.HGroup(row, 16, 0, 0, 0, 0, TextAnchor.UpperLeft, true, false);
            var a = Cell(row, label1, value1);
            second = Cell(row, label2, value2);
            return a;
        }

        static TMP_Text Cell(Transform parent, string label, string value)
        {
            var cell = UiKit.Rect(parent, label);
            UiKit.Size(cell, 0, -1, 1);
            UiKit.VGroup(cell, 4);
            UiKit.Text(cell, label, 13f, UiTheme.TextMuted);
            return UiKit.Mono(UiKit.Text(cell, value, 18f, UiTheme.Text, FontStyles.Normal));
        }
    }
}
