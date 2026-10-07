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
    /// Builds the instructor station (a Display 0 canvas in the simulation scene) and the Debrief scene from the UI design
    /// (screens 6 and 7). Menu: Falah/UI/Build Instructor Station (open RoVGameplay first) and Falah/UI/Build Debrief Scene.
    /// </summary>
    public static class SessionScreensBuilder
    {
        const string InstructorCanvasName = "CanvasInstructorStation";
        const string RenderTexturePath = "Assets/_Falah/UI/Art/instructor_view.renderTexture";

        static readonly Color MapBg = UiTheme.Hex("0B1A2C");
        static readonly Color GridLine = UiTheme.Hex("1B2E4B");

        // =====================================================================================================
        // Instructor station
        // =====================================================================================================

        [MenuItem("Falah/UI/Build Instructor Station")]
        public static string BuildInstructor()
        {
            var old = GameObject.Find(InstructorCanvasName);
            if (old != null) Object.DestroyImmediate(old);
            var oldController = GameObject.Find("SessionController");
            if (oldController != null) Object.DestroyImmediate(oldController);

            var go = new GameObject(InstructorCanvasName, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.targetDisplay = 0;
            canvas.sortingOrder = 10;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600, 900);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            var root = (RectTransform)go.transform;

            var screen = MenuSceneBuilder.NewScreen(root, "Station");
            UiKit.VGroup(screen, 0, 0, 0, 0, 0, TextAnchor.UpperLeft, true, false);
            var station = go.AddComponent<InstructorStation>();

            // header
            var bar = UiKit.Rect(screen, "Header");
            UiKit.Img(bar, UiTheme.Panel);
            UiKit.Size(bar, -1, 64);
            UiKit.HGroup(bar, 16, 24, 0, 24, 0, TextAnchor.MiddleLeft);
            AddBottomLine(bar);
            UiKit.Text(bar, "STATION INSTRUKTUR", 15f, UiTheme.Text, FontStyles.Bold, TextAlignmentOptions.MidlineLeft, 12f, false);
            var chip = UiKit.MakeBox(bar, "RovChip", UiTheme.CardSelected, UiTheme.Accent, 1f, 6f);
            UiKit.VGroup(chip.Outer, 0, 10, 5, 10, 5, TextAnchor.MiddleCenter);
            var rovName = UiKit.Text(chip.Outer, "TORTUGA", 12f, UiTheme.Text, FontStyles.Bold, TextAlignmentOptions.Center, 0f, false);
            var scenario = UiKit.Text(bar, "Skenario", 14f, UiTheme.TextSoft, FontStyles.Normal, TextAlignmentOptions.MidlineLeft, 0f, false);
            UiKit.Spacer(bar);
            var lan = UiKit.Rect(bar, "Link");
            UiKit.HGroup(lan, 8, 0, 0, 0, 0, TextAnchor.MiddleLeft);
            var lanDot = UiKit.Circle(lan, "Dot", 8, 8, UiTheme.Ok);
            UiKit.Size(lanDot.rectTransform, 8, 8);
            UiKit.Text(lan, "Satu mesin (lokal)", 13f, UiTheme.Ok, FontStyles.Normal, TextAlignmentOptions.MidlineLeft, 0f, false);
            var clock = UiKit.Rect(bar, "Clock");
            UiKit.HGroup(clock, 8, 0, 0, 0, 0, TextAnchor.MiddleLeft);
            var elapsed = UiKit.Mono(UiKit.Text(clock, "00:00", 18f, UiTheme.Text, FontStyles.Normal, TextAlignmentOptions.MidlineLeft, 0f, false));
            var limit = UiKit.Mono(UiKit.Text(clock, "/ --:--", 13f, UiTheme.TextDim, FontStyles.Normal, TextAlignmentOptions.MidlineLeft, 0f, false));

            // body
            var body = UiKit.Rect(screen, "Body");
            UiKit.Size(body, -1, -1, -1, 1);
            UiKit.HGroup(body, 16, 24, 16, 24, 24, TextAnchor.UpperLeft, false, true);

            // left column
            var left = UiKit.Rect(body, "Left");
            UiKit.Size(left, 340);
            UiKit.VGroup(left, 16, 0, 0, 0, 0, TextAnchor.UpperLeft, true, false);

            var session = Panel(left, "Session", 18, 16, 0);
            Caption(session.Outer, "SESI", 12f, 12);
            var rows = UiKit.Rect(session.Outer, "Rows");
            UiKit.VGroup(rows, 8);
            var trainee = InfoRow(rows, "Trainee", "[Nama trainee]", UiTheme.Text, FontStyles.Bold);
            var mode = InfoRow(rows, "Mode", "Misi penuh", UiTheme.Text, FontStyles.Normal);
            var recording = InfoRow(rows, "Rekaman", "Aktif", UiTheme.Danger, FontStyles.Bold);
            UiKit.Spacer(session.Outer, 0, 14);
            var buttons = UiKit.Rect(session.Outer, "Buttons");
            UiKit.HGroup(buttons, 8, 0, 0, 0, 0, TextAnchor.MiddleCenter, true, false);
            var pause = UiKit.OutlineButton(buttons, "Jeda", 40, 14, UiTheme.BorderStrong, UiTheme.Text, UiTheme.Card);
            UiKit.Size((RectTransform)pause.transform, 0, 40, 1);
            var end = UiKit.OutlineButton(buttons, "Akhiri sesi", 40, 14, UiTheme.Danger, UiTheme.Danger, UiTheme.Card);
            UiKit.Size((RectTransform)end.transform, 0, 40, 1);

            var dist = Panel(left, "Disturbances", 18, 16, 8);
            UiKit.Size(dist.Outer, -1, -1, -1, 1);
            Caption(dist.Outer, "INJEKSI GANGGUAN", 12f, 4);
            var distButtons = new Button[DisturbanceCatalog.All.Length];
            var distLabels = new TMP_Text[distButtons.Length];
            var distBorders = new Graphic[distButtons.Length];
            var distFills = new Graphic[distButtons.Length];
            for (int i = 0; i < distButtons.Length; i++)
            {
                var box = UiKit.MakeBox(dist.Outer, DisturbanceCatalog.All[i].Kind.ToString(), UiTheme.Panel, UiTheme.BorderStrong, 1f, 8f);
                UiKit.Size(box.Outer, -1, 44);
                UiKit.HGroup(box.Outer, 0, 14, 0, 14, 0, TextAnchor.MiddleLeft);
                distLabels[i] = UiKit.Text(box.Outer, DisturbanceCatalog.All[i].Label, 14f, UiTheme.Text, FontStyles.Normal, TextAlignmentOptions.MidlineLeft, 0f, false);
                distButtons[i] = UiKit.MakeButton(box.Outer.gameObject, box.Fill);
                distBorders[i] = box.Border;
                distFills[i] = box.Fill;
            }

            // center column
            var center = UiKit.Rect(body, "Center");
            UiKit.Size(center, 0, -1, 1);
            UiKit.VGroup(center, 16, 0, 0, 0, 0, TextAnchor.UpperLeft, true, false);

            var mapCard = UiKit.MakeBox(center, "Map", MapBg, UiTheme.Border, 1f, 10f);
            UiKit.Size(mapCard.Outer, -1, -1, -1, 1);
            var map = BuildMap(mapCard.Outer, "PETA ATAS DAN JEJAK", 14f, new[] { ("ROV", UiTheme.Accent, true), ("Awal", UiTheme.TextMuted, true), ("Jejak", UiTheme.Accent, false) });

            var timelineCard = Panel(center, "Timeline", 18, 16, 10);
            UiKit.Size(timelineCard.Outer, -1, 220);
            Caption(timelineCard.Outer, "LINIMASA KEJADIAN", 12f, 0);
            var timeline = BuildEventList(timelineCard.Outer, 6);

            // right column
            var right = UiKit.Rect(body, "Right");
            UiKit.Size(right, 380);
            UiKit.VGroup(right, 16, 0, 0, 0, 0, TextAnchor.UpperLeft, true, false);

            var viewCard = UiKit.MakeBox(right, "TraineeView", UiTheme.Card, UiTheme.Border, 1f, 10f);
            UiKit.VGroup(viewCard.Outer, 0, 1, 1, 1, 1);
            var viewHeader = UiKit.Rect(viewCard.Outer, "Header");
            UiKit.HGroup(viewHeader, 0, 14, 10, 14, 10, TextAnchor.MiddleLeft);
            Caption(viewHeader, "TAMPILAN TRAINEE", 12f, 0);
            UiKit.Spacer(viewHeader);
            UiKit.Text(viewHeader, "Langsung", 11f, UiTheme.TextMuted, FontStyles.Normal, TextAlignmentOptions.MidlineRight, 0f, false);
            var view = UiKit.Rect(viewCard.Outer, "View");
            UiKit.Size(view, -1, 212);
            UiKit.Img(view, UiTheme.Hex("06182A"));
            var raw = UiKit.Rect(view, "Image");
            UiKit.Stretch(raw);
            var rawImage = raw.gameObject.AddComponent<RawImage>();
            rawImage.texture = EnsureRenderTexture();
            rawImage.raycastTarget = false;
            var feedType = System.Type.GetType("Falah.RovSim.Integration.InstructorViewFeed, Assembly-CSharp");
            if (feedType == null) throw new System.InvalidOperationException("InstructorViewFeed not found (Assets/_Falah/Integration).");
            var feed = raw.gameObject.AddComponent(feedType);
            UiKit.Bind(feed, "target", rawImage.texture);

            var env = Panel(right, "Environment", 18, 16, 18);
            UiKit.Size(env.Outer, -1, -1, -1, 1);
            Caption(env.Outer, "LINGKUNGAN (LANGSUNG)", 12f, -4);
            var speed = UiKit.SliderRow(env.Outer, "Kecepatan arus", 0f, 4f, 1.5f, " kn", "0.0", false);
            var direction = UiKit.SliderRow(env.Outer, "Arah arus", 0f, 359f, 45f, "°", "000", true);
            var visibility = UiKit.SliderRow(env.Outer, "Visibilitas", 0f, 20f, 4f, " m", "0.#", false);
            UiKit.Spacer(env.Outer, 0, 4);
            Caption(env.Outer, "CATATAN INSTRUKTUR", 12f, 0);
            var note = UiKit.InputField(env.Outer, "Tulis catatan untuk debrief...", false, 84f, true, 13f, UiTheme.Panel);

            // controller object (session clock, track sampling, keys)
            var controllerGo = new GameObject("SessionController");
            var controller = controllerGo.AddComponent<SessionController>();
            var hudGo = GameObject.Find("CanvasHUD_Pilot");
            Component adapter = null;
            var adapterType = System.Type.GetType("Falah.RovSim.Integration.RoVTelemetryAdapter, Assembly-CSharp");
            if (hudGo != null && adapterType != null) adapter = hudGo.GetComponent(adapterType);
            UiKit.Bind(controller, "telemetrySource", adapter);
            UiKit.Bind(controller, "instructorCanvas", go);

            UiKit.Bind(station, "session", controller);
            UiKit.Bind(station, "telemetrySource", adapter);
            UiKit.Bind(station, "rovName", rovName);
            UiKit.Bind(station, "scenarioName", scenario);
            UiKit.Bind(station, "clockElapsed", elapsed);
            UiKit.Bind(station, "clockLimit", limit);
            UiKit.Bind(station, "traineeValue", trainee);
            UiKit.Bind(station, "modeValue", mode);
            UiKit.Bind(station, "recordingValue", recording);
            UiKit.Bind(station, "pauseButton", pause);
            UiKit.Bind(station, "pauseLabel", pause.GetComponentInChildren<TMP_Text>());
            UiKit.Bind(station, "endButton", end);
            UiKit.BindList(station, "disturbanceButtons", distButtons);
            UiKit.BindList(station, "disturbanceLabels", distLabels);
            UiKit.BindList(station, "disturbanceBorders", distBorders);
            UiKit.BindList(station, "disturbanceFills", distFills);
            UiKit.Bind(station, "currentSpeed", speed);
            UiKit.Bind(station, "currentDirection", direction);
            UiKit.Bind(station, "visibility", visibility);
            UiKit.Bind(station, "noteField", note);
            UiKit.Bind(station, "timeline", timeline);
            UiKit.Bind(station, "map", map);

            EditorSceneManager.MarkSceneDirty(go.scene);
            return "built " + InstructorCanvasName + " (Display 0) + SessionController in scene " + go.scene.name;
        }

        // =====================================================================================================
        // Debrief scene
        // =====================================================================================================

        [MenuItem("Falah/UI/Build Debrief Scene")]
        public static void BuildDebrief()
        {
            Directory.CreateDirectory("Assets/_Falah/Scenes");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var camera = MenuSceneBuilder.CreateCamera();
            MenuSceneBuilder.CreateEventSystem();
            var canvas = MenuSceneBuilder.CreateCanvas(camera);

            var screen = MenuSceneBuilder.NewScreen(canvas, "DebriefScreen");
            UiKit.VGroup(screen, 0, 0, 0, 0, 0, TextAnchor.UpperLeft, true, false);
            var comp = screen.gameObject.AddComponent<DebriefScreen>();

            var bar = UiKit.Rect(screen, "Header");
            UiKit.Img(bar, UiTheme.Panel);
            UiKit.Size(bar, -1, 72);
            UiKit.HGroup(bar, 16, 32, 0, 32, 0, TextAnchor.MiddleLeft);
            AddBottomLine(bar);
            UiKit.Text(bar, "DEBRIEF", 15f, UiTheme.Text, FontStyles.Bold, TextAlignmentOptions.MidlineLeft, 12f, false);
            var subtitle = UiKit.Text(bar, "ROV · Skenario · [Nama trainee]", 14f, UiTheme.TextSoft, FontStyles.Normal, TextAlignmentOptions.MidlineLeft, 0f, false);
            UiKit.Spacer(bar);
            var tag = UiKit.MakeBox(bar, "Tag", UiTheme.Panel, UiTheme.Warn, 1f, 6f);
            UiKit.VGroup(tag.Outer, 0, 10, 4, 10, 4, TextAnchor.MiddleCenter);
            UiKit.Text(tag.Outer, "Skor dan putar ulang: tahap berikutnya", 12f, UiTheme.Warn, FontStyles.Normal, TextAlignmentOptions.Center, 0f, false);

            var body = UiKit.Rect(screen, "Body");
            UiKit.Size(body, -1, -1, -1, 1);
            UiKit.HGroup(body, 20, 32, 20, 32, 20, TextAnchor.UpperLeft, false, true);

            // left: score
            var left = UiKit.Rect(body, "Left");
            UiKit.Size(left, 460);
            UiKit.VGroup(left, 20, 0, 0, 0, 0, TextAnchor.UpperLeft, true, false);

            var score = UiKit.MakeBox(left, "Score", UiTheme.Card, UiTheme.Border, 1f, 12f);
            UiKit.HGroup(score.Outer, 28, 24, 24, 24, 24, TextAnchor.MiddleLeft);
            var ring = UiKit.Rect(score.Outer, "Ring");
            UiKit.Size(ring, 150, 150);
            var track = UiKit.Circle(ring, "Track", 150, 150, UiTheme.Hex("1A2B47"));
            UiKit.Stretch(track.rectTransform);
            var holeRing = UiKit.Circle(ring, "Hole", 122, 122, UiTheme.Card);
            UiKit.Anchor(holeRing.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(122, 122));
            var scoreValue = UiKit.Text(ring, "[__]", 38f, UiTheme.TextMuted, FontStyles.Bold, TextAlignmentOptions.Center, 0f, false);
            UiKit.Stretch(scoreValue.rectTransform);
            var scoreText = UiKit.Rect(score.Outer, "Text");
            UiKit.VGroup(scoreText, 6);
            Caption(scoreText, "SKOR AKHIR", 12f, 0);
            UiKit.Text(scoreText, "Belum dinilai", 22f, UiTheme.TextMuted, FontStyles.Bold);
            var duration = UiKit.Text(scoreText, "Durasi 00:00 dari --:--", 13f, UiTheme.TextMuted);

            var criteria = Panel(left, "Criteria", 24, 22, 20);
            UiKit.Size(criteria.Outer, -1, -1, -1, 1);
            Caption(criteria.Outer, "KRITERIA PENILAIAN", 12f, 0);
            foreach (var name in new[] { "Navigasi", "Station keeping", "Penanganan gangguan", "Penyelesaian misi", "Waktu" })
            {
                var row = UiKit.Rect(criteria.Outer, name);
                UiKit.VGroup(row, 8);
                var top = UiKit.Rect(row, "Top");
                UiKit.HGroup(top, 0, 0, 0, 0, 0, TextAnchor.MiddleLeft);
                UiKit.Text(top, name, 14f, UiTheme.Text, FontStyles.Normal, TextAlignmentOptions.MidlineLeft, 0f, false);
                UiKit.Spacer(top);
                UiKit.Mono(UiKit.Text(top, "[__]", 14f, UiTheme.TextMuted, FontStyles.Normal, TextAlignmentOptions.MidlineRight, 0f, false));
                var bg = UiKit.Rect(row, "Bar");
                UiKit.Size(bg, -1, 8);
                UiKit.Img(bg, UiTheme.Hex("1A2B47"), 4f);
            }

            // center: track and replay bar
            var center = UiKit.Rect(body, "Center");
            UiKit.Size(center, 0, -1, 1);
            UiKit.VGroup(center, 20, 0, 0, 0, 0, TextAnchor.UpperLeft, true, false);
            var mapCard = UiKit.MakeBox(center, "Map", MapBg, UiTheme.Border, 1f, 12f);
            UiKit.Size(mapCard.Outer, -1, -1, -1, 1);
            var map = BuildMap(mapCard.Outer, "JEJAK ROV", 18f, new[] { ("Biru: jalur", UiTheme.Accent, false), ("Kuning: saat gangguan", UiTheme.Warn, false), ("Hijau: akhir", UiTheme.Ok, false) });

            var replay = Panel(center, "Replay", 22, 18, 0);
            var replayRow = UiKit.Rect(replay.Outer, "Row");
            UiKit.HGroup(replayRow, 16, 0, 0, 0, 0, TextAnchor.MiddleLeft);
            var playBox = UiKit.MakeBox(replayRow, "Play", UiTheme.Accent, UiTheme.Accent, 0f, 22f);
            UiKit.Size(playBox.Outer, 44, 44);
            UiKit.VGroup(playBox.Outer, 0, 0, 0, 0, 0, TextAnchor.MiddleCenter);
            UiKit.Text(playBox.Outer, ">", 18f, UiTheme.OnAccent, FontStyles.Bold, TextAlignmentOptions.Center, 0f, false);
            var playButton = UiKit.MakeButton(playBox.Outer.gameObject, playBox.Fill);
            playButton.interactable = false;
            var elapsed = UiKit.Mono(UiKit.Text(replayRow, "00:00", 14f, UiTheme.Text, FontStyles.Normal, TextAlignmentOptions.MidlineLeft, 0f, false));
            UiKit.Size(elapsed.rectTransform, 50);
            var bar2 = UiKit.Rect(replayRow, "Timeline");
            UiKit.Size(bar2, 0, 8, 1);
            UiKit.Img(bar2, UiTheme.Hex("1A2B47"), 4f);
            var markers = UiKit.Rect(bar2, "Markers");
            UiKit.Stretch(markers);
            var markerTemplate = UiKit.Rect(markers, "MarkerTemplate");
            UiKit.Anchor(markerTemplate, new Vector2(0, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(4, 16));
            var markerImg = UiKit.Img(markerTemplate, UiTheme.Warn);
            markerTemplate.gameObject.SetActive(false);
            var total = UiKit.Mono(UiKit.Text(replayRow, "00:00", 14f, UiTheme.TextMuted, FontStyles.Normal, TextAlignmentOptions.MidlineRight, 0f, false));
            UiKit.Size(total.rectTransform, 50);
            var speed = UiKit.MakeBox(replayRow, "Speed", UiTheme.Card, UiTheme.BorderStrong, 1f, 6f);
            UiKit.VGroup(speed.Outer, 0, 10, 6, 10, 6, TextAnchor.MiddleCenter);
            UiKit.Text(speed.Outer, "1x", 13f, UiTheme.TextMuted, FontStyles.Normal, TextAlignmentOptions.Center, 0f, false);

            // right: key events and actions
            var right = UiKit.Rect(body, "Right");
            UiKit.Size(right, 420);
            UiKit.VGroup(right, 20, 0, 0, 0, 0, TextAnchor.UpperLeft, true, false);
            var events = Panel(right, "Events", 24, 22, 16);
            UiKit.Size(events.Outer, -1, -1, -1, 1);
            Caption(events.Outer, "KEJADIAN PENTING", 12f, 0);
            var keyEvents = BuildEventList(events.Outer, 9, 16f, 48f);
            UiKit.Spacer(events.Outer);
            var noteBox = UiKit.MakeBox(events.Outer, "Note", UiTheme.Panel, UiTheme.BorderStrong, 1f, 8f);
            UiKit.VGroup(noteBox.Outer, 0, 14, 12, 14, 12);
            var noteText = UiKit.Text(noteBox.Outer, "Catatan instruktur: (tidak ada)", 13f, UiTheme.TextSoft);

            var actions = UiKit.Rect(right, "Actions");
            UiKit.VGroup(actions, 10);
            var save = UiKit.PrimaryButton(actions, "Simpan laporan", 52, 16);
            var status = UiKit.Text(actions, string.Empty, 12f, UiTheme.Ok, FontStyles.Normal, TextAlignmentOptions.TopLeft, 0f, true);
            var row2 = UiKit.Rect(actions, "Row");
            UiKit.HGroup(row2, 10, 0, 0, 0, 0, TextAnchor.MiddleCenter, true, false);
            var pdf = UiKit.OutlineButton(row2, "Ekspor PDF", 48, 15, UiTheme.BorderStrong, UiTheme.Text, UiTheme.Card);
            UiKit.Size((RectTransform)pdf.transform, 0, 48, 1);
            pdf.interactable = false;
            var again = UiKit.OutlineButton(row2, "Putar ulang misi", 48, 15, UiTheme.BorderStrong, UiTheme.Text, UiTheme.Card);
            UiKit.Size((RectTransform)again.transform, 0, 48, 1);
            again.interactable = false;
            var menu = UiKit.OutlineButton(actions, "Kembali ke menu", 48, 15, UiTheme.BorderStrong, UiTheme.Text, UiTheme.Panel);

            UiKit.Bind(comp, "subtitle", subtitle);
            UiKit.Bind(comp, "durationText", duration);
            UiKit.Bind(comp, "noteText", noteText);
            UiKit.Bind(comp, "keyEvents", keyEvents);
            UiKit.Bind(comp, "map", map);
            UiKit.Bind(comp, "replayElapsed", elapsed);
            UiKit.Bind(comp, "replayTotal", total);
            UiKit.Bind(comp, "markerParent", markers);
            UiKit.Bind(comp, "markerTemplate", markerImg);
            UiKit.Bind(comp, "saveButton", save);
            UiKit.Bind(comp, "saveStatus", status);
            UiKit.Bind(comp, "menuButton", menu);

            EditorSceneManager.SaveScene(scene, MenuSceneBuilder.DebriefScenePath);
            MenuSceneBuilder.EnsureBuildSettings();
            Debug.Log("Debrief scene built: " + MenuSceneBuilder.DebriefScenePath);
        }

        // =====================================================================================================
        // shared pieces
        // =====================================================================================================

        static Box Panel(Transform parent, string name, int padH, int padV, float spacing)
        {
            var box = UiKit.MakeBox(parent, name, UiTheme.Card, UiTheme.Border, 1f, 10f);
            UiKit.VGroup(box.Outer, spacing, padH, padV, padH, padV);
            return box;
        }

        static TMP_Text Caption(Transform parent, string text, float spacing, float marginBottom)
        {
            var t = UiKit.Text(parent, text, 11f, UiTheme.TextMuted, FontStyles.Normal, TextAlignmentOptions.TopLeft, spacing, false);
            if (marginBottom > 0f) UiKit.Size(t.rectTransform, -1, 11f + marginBottom);
            return t;
        }

        static TMP_Text InfoRow(Transform parent, string label, string value, Color valueColor, FontStyles style)
        {
            var row = UiKit.Rect(parent, label);
            UiKit.HGroup(row, 0, 0, 0, 0, 0, TextAnchor.MiddleLeft);
            UiKit.Text(row, label, 14f, UiTheme.TextMuted, FontStyles.Normal, TextAlignmentOptions.MidlineLeft, 0f, false);
            UiKit.Spacer(row);
            return UiKit.Text(row, value, 14f, valueColor, style, TextAlignmentOptions.MidlineRight, 0f, false);
        }

        static void AddBottomLine(RectTransform bar)
        {
            var line = UiKit.Rect(bar, "BottomBorder");
            line.anchorMin = new Vector2(0, 0); line.anchorMax = new Vector2(1, 0); line.pivot = new Vector2(0.5f, 0); line.sizeDelta = new Vector2(0, 1);
            UiKit.IgnoreLayout(line);
            UiKit.Img(line, UiTheme.Border);
        }

        static RenderTexture EnsureRenderTexture()
        {
            var rt = AssetDatabase.LoadAssetAtPath<RenderTexture>(RenderTexturePath);
            if (rt != null) return rt;
            Directory.CreateDirectory(Path.GetDirectoryName(RenderTexturePath));
            rt = new RenderTexture(760, 424, 24, RenderTextureFormat.ARGB32) { name = "instructor_view" };
            AssetDatabase.CreateAsset(rt, RenderTexturePath);
            return rt;
        }

        /// <summary>Event list with an inactive template row (time in mono + message).</summary>
        static EventListView BuildEventList(Transform parent, int maxRows, float spacing = 8f, float timeWidth = 56f)
        {
            var container = UiKit.Rect(parent, "Events");
            UiKit.VGroup(container, spacing);
            var template = UiKit.Rect(container, "RowTemplate");
            UiKit.HGroup(template, 16, 0, 0, 0, 0, TextAnchor.UpperLeft);
            var time = UiKit.Mono(UiKit.Text(template, "00:00", 14f, UiTheme.TextMuted, FontStyles.Normal, TextAlignmentOptions.TopLeft, 0f, false));
            UiKit.Size(time.rectTransform, timeWidth);
            var message = UiKit.Text(template, "Kejadian", 14f, UiTheme.Text);
            UiKit.Size(message.rectTransform, 0, -1, 1);
            template.gameObject.SetActive(false);
            var list = container.gameObject.AddComponent<EventListView>();
            UiKit.Bind(list, "container", container);
            UiKit.Bind(list, "rowTemplate", template.gameObject);
            UiKit.Bind(list, "maxRows", maxRows);
            return list;
        }

        /// <summary>Map area with grid, markers, dot template and legend, driven by a <see cref="TrackMap"/>.</summary>
        static TrackMap BuildMap(RectTransform card, string title, float titleMargin, (string label, Color color, bool round)[] legend)
        {
            var area = UiKit.Rect(card, "Area");
            UiKit.Stretch(area, 1, 1, 1, 1);
            UiKit.IgnoreLayout(area);
            area.gameObject.AddComponent<RectMask2D>();
            foreach (float f in new[] { 0.25f, 0.5f, 0.75f })
            {
                var h = UiKit.Rect(area, "GridH");
                h.anchorMin = new Vector2(0, f); h.anchorMax = new Vector2(1, f); h.sizeDelta = new Vector2(0, 1);
                UiKit.Img(h, GridLine);
                var v = UiKit.Rect(area, "GridV");
                v.anchorMin = new Vector2(f, 0); v.anchorMax = new Vector2(f, 1); v.sizeDelta = new Vector2(1, 0);
                UiKit.Img(v, GridLine);
            }
            var dot = UiKit.Circle(area, "DotTemplate", 5, 5, UiTheme.Accent);
            UiKit.Anchor(dot.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(5, 5));
            dot.gameObject.SetActive(false);

            var start = UiKit.Circle(area, "Start", 10, 10, UiTheme.TextMuted);
            UiKit.Anchor(start.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(10, 10));
            var endRt = UiKit.Rect(area, "End");
            UiKit.Anchor(endRt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(12, 12));
            UiKit.Img(endRt, UiTheme.Ok);
            var vehicle = UiKit.Circle(area, "Vehicle", 14, 14, UiTheme.Accent);
            UiKit.Anchor(vehicle.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(14, 14));
            var halo = UiKit.Circle(vehicle.rectTransform, "Halo", 30, 30, new Color(UiTheme.Accent.r, UiTheme.Accent.g, UiTheme.Accent.b, 0.25f));
            UiKit.Anchor(halo.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(30, 30));
            halo.transform.SetAsFirstSibling();
            var tick = UiKit.Rect(vehicle.rectTransform, "Heading");
            UiKit.Anchor(tick, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0f), Vector2.zero, new Vector2(3, 22));
            UiKit.Img(tick, UiTheme.Accent);

            var label = Caption(card, title, 12f, 0);
            UiKit.IgnoreLayout(label.rectTransform);
            UiKit.Anchor(label.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(titleMargin, -(titleMargin - 2f)), new Vector2(320, 16));

            var key = UiKit.Rect(card, "Legend");
            UiKit.IgnoreLayout(key);
            UiKit.Anchor(key, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-titleMargin, titleMargin - 2f), Vector2.zero);
            UiKit.HGroup(key, 16, 0, 0, 0, 0, TextAnchor.MiddleLeft);
            var fit = key.gameObject.AddComponent<ContentSizeFitter>();
            fit.horizontalFit = fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            foreach (var (text, color, round) in legend)
                UiKit.Text(key, text, 12f, round ? UiTheme.TextSoft : color, FontStyles.Normal, TextAlignmentOptions.MidlineLeft, 0f, false);

            var map = card.gameObject.AddComponent<TrackMap>();
            UiKit.Bind(map, "area", area);
            UiKit.Bind(map, "vehicleMarker", vehicle.rectTransform);
            UiKit.Bind(map, "headingTick", tick);
            UiKit.Bind(map, "startMarker", start.rectTransform);
            UiKit.Bind(map, "endMarker", endRt);
            UiKit.Bind(map, "dotTemplate", dot);
            return map;
        }
    }
}
