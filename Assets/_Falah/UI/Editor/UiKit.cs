using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Falah.RovSim.UI.Editor
{
    /// <summary>A bordered, rounded panel: Outer (border colour) with a Fill child inset by the border width.</summary>
    internal sealed class Box
    {
        public RectTransform Outer;
        public RectTransform FillRect;
        public Image Border;
        public Image Fill;
    }

    /// <summary>Helpers that build uGUI hierarchies (layout groups, text, buttons, sliders) in the editor.</summary>
    internal static class UiKit
    {
        const string ArtFolder = "Assets/_Falah/UI/Art";

        static Sprite roundSprite;
        static Sprite knobSprite;

        static Sprite Round => roundSprite != null ? roundSprite : roundSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        static Sprite Knob => knobSprite != null ? knobSprite : knobSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");

        // ---------- basic rects and images ----------

        public static RectTransform Rect(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        public static void Stretch(RectTransform r, float l = 0, float t = 0, float rt = 0, float b = 0)
        {
            r.anchorMin = Vector2.zero;
            r.anchorMax = Vector2.one;
            r.offsetMin = new Vector2(l, b);
            r.offsetMax = new Vector2(-rt, -t);
        }

        public static void Anchor(RectTransform r, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            r.anchorMin = r.anchorMax = anchor;
            r.pivot = pivot;
            r.anchoredPosition = pos;
            r.sizeDelta = size;
        }

        public static Image Img(RectTransform r, Color color, float radius = 0f, bool raycast = false)
        {
            var img = r.gameObject.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = raycast;
            if (radius > 0f)
            {
                // Radii above 16 px are "pill" requests; the sliced sprite shrinks its corners to fit small rects.
                img.sprite = Round;
                img.type = Image.Type.Sliced;
                img.pixelsPerUnitMultiplier = 4f / Mathf.Min(radius, 16f);
            }
            return img;
        }

        static LayoutElement Element(RectTransform r) =>
            r.gameObject.GetComponent<LayoutElement>() ?? r.gameObject.AddComponent<LayoutElement>();

        public static void IgnoreLayout(RectTransform r) => Element(r).ignoreLayout = true;

        /// <summary>
        /// Layout groups report a flexible size to their parent when any child is flexible (or force-expanded),
        /// which makes fixed-size panels grow. Groups are therefore non-flexible unless Size() says otherwise.
        /// </summary>
        static void DefaultInflexible(RectTransform r)
        {
            var le = Element(r);
            if (le.flexibleWidth < 0f) le.flexibleWidth = 0f;
            if (le.flexibleHeight < 0f) le.flexibleHeight = 0f;
        }

        // ---------- layout ----------

        public static VerticalLayoutGroup VGroup(RectTransform r, float spacing = 0, int l = 0, int t = 0, int rt = 0, int b = 0,
            TextAnchor align = TextAnchor.UpperLeft, bool expandW = true, bool expandH = false)
        {
            var g = r.gameObject.AddComponent<VerticalLayoutGroup>();
            g.spacing = spacing;
            g.padding = new RectOffset(l, rt, t, b);
            g.childAlignment = align;
            g.childControlWidth = g.childControlHeight = true;
            g.childForceExpandWidth = expandW;
            g.childForceExpandHeight = expandH;
            DefaultInflexible(r);
            return g;
        }

        public static HorizontalLayoutGroup HGroup(RectTransform r, float spacing = 0, int l = 0, int t = 0, int rt = 0, int b = 0,
            TextAnchor align = TextAnchor.MiddleLeft, bool expandW = false, bool expandH = false)
        {
            var g = r.gameObject.AddComponent<HorizontalLayoutGroup>();
            g.spacing = spacing;
            g.padding = new RectOffset(l, rt, t, b);
            g.childAlignment = align;
            g.childControlWidth = g.childControlHeight = true;
            g.childForceExpandWidth = expandW;
            g.childForceExpandHeight = expandH;
            DefaultInflexible(r);
            return g;
        }

        public static LayoutElement Size(RectTransform r, float w = -1, float h = -1, float flexW = -1, float flexH = -1)
        {
            var le = Element(r);
            if (w >= 0) { le.minWidth = w; le.preferredWidth = w; if (flexW < 0) le.flexibleWidth = 0f; }
            if (h >= 0) { le.minHeight = h; le.preferredHeight = h; if (flexH < 0) le.flexibleHeight = 0f; }
            if (flexW >= 0) le.flexibleWidth = flexW;
            if (flexH >= 0) le.flexibleHeight = flexH;
            return le;
        }

        /// <summary>Flexible gap along the parent's main axis (or a fixed gap when flex is 0).</summary>
        public static RectTransform Spacer(Transform parent, float flex = 1f, float fixedSize = 0f)
        {
            var r = Rect(parent, "Spacer");
            var le = r.gameObject.AddComponent<LayoutElement>();
            bool horizontal = parent.GetComponent<HorizontalLayoutGroup>() != null;
            bool vertical = parent.GetComponent<VerticalLayoutGroup>() != null;
            le.flexibleWidth = vertical ? 0f : flex;
            le.flexibleHeight = horizontal ? 0f : flex;
            le.minWidth = le.minHeight = fixedSize;
            return r;
        }

        // ---------- panels ----------

        /// <remarks>
        /// The border is the full-size Outer image with the Fill drawn on top, so a transparent fill would show the
        /// border colour. Outlined elements must pass the colour of the surface behind them as <paramref name="fill"/>.
        /// </remarks>
        public static Box MakeBox(Transform parent, string name, Color fill, Color border, float borderWidth = 1f, float radius = 8f)
        {
            if (fill.a < 0.01f && border.a > 0.01f && borderWidth > 0f)
                Debug.LogWarning("UiKit.MakeBox '" + name + "': transparent fill with a visible border renders as a solid border colour.");
            var outer = Rect(parent, name);
            var box = new Box { Outer = outer, Border = Img(outer, border, radius, true) };
            var fillRect = Rect(outer, "Fill");
            Stretch(fillRect, borderWidth, borderWidth, borderWidth, borderWidth);
            IgnoreLayout(fillRect);
            fillRect.SetSiblingIndex(0);
            box.FillRect = fillRect;
            box.Fill = Img(fillRect, fill, Mathf.Max(radius - borderWidth, 0f));
            return box;
        }

        /// <summary>Pill / badge with a text inside.</summary>
        public static Box Chip(Transform parent, string text, Color fill, Color border, Color textColor, float size = 13f,
            int padH = 14, int padV = 8, FontStyles style = FontStyles.Normal)
        {
            var box = MakeBox(parent, "Chip", fill, border, border.a > 0f ? 1f : 0f, 999f);
            VGroup(box.Outer, 0, padH, padV, padH, padV, TextAnchor.MiddleCenter);
            Text(box.Outer, text, size, textColor, style, TextAlignmentOptions.Center, 0f, false);
            return box;
        }

        // ---------- text ----------

        public static TextMeshProUGUI Text(Transform parent, string text, float size, Color color,
            FontStyles style = FontStyles.Normal, TextAlignmentOptions align = TextAlignmentOptions.TopLeft,
            float spacing = 0f, bool wrap = true)
        {
            var r = Rect(parent, "Text");
            var t = r.gameObject.AddComponent<TextMeshProUGUI>();
            t.font = UiFonts.Sans(style);
            t.text = text;
            t.fontSize = size;
            t.color = color;
            // The semibold face replaces synthetic bold; keep other style flags (italic).
            t.fontStyle = style & ~FontStyles.Bold;
            t.alignment = align;
            t.characterSpacing = spacing;
            t.textWrappingMode = wrap ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;
            t.raycastTarget = false;
            return t;
        }

        /// <summary>Fixed-width numerals (telemetry, specs, timers).</summary>
        public static TMP_Text Mono(TMP_Text text)
        {
            text.font = UiFonts.Mono();
            return text;
        }

        // ---------- buttons ----------

        public static Button MakeButton(GameObject target, Graphic graphic)
        {
            var b = target.AddComponent<Button>();
            b.targetGraphic = graphic;
            b.transition = Selectable.Transition.ColorTint;
            var c = b.colors;
            c.normalColor = Color.white;
            c.highlightedColor = new Color(0.88f, 0.88f, 0.88f, 1f);
            c.pressedColor = new Color(0.72f, 0.72f, 0.72f, 1f);
            c.selectedColor = Color.white;
            c.disabledColor = new Color(0.55f, 0.55f, 0.55f, 0.65f);
            c.fadeDuration = 0.08f;
            b.colors = c;
            return b;
        }

        public static Button PrimaryButton(Transform parent, string label, float height, float size, int padH = 32)
        {
            var box = MakeBox(parent, "Button", UiTheme.Accent, UiTheme.Accent, 0f, 8f);
            VGroup(box.Outer, 0, padH, 0, padH, 0, TextAnchor.MiddleCenter);
            Size(box.Outer, -1, height);
            var text = Text(box.Outer, label, size, UiTheme.OnAccent, FontStyles.Bold, TextAlignmentOptions.Center, 0f, false);
            text.font = UiFonts.SansBold();
            return MakeButton(box.Outer.gameObject, box.Fill);
        }

        /// <param name="background">Colour of the surface the button sits on (the outline button is filled with it).</param>
        public static Button SecondaryButton(Transform parent, string label, float height, float size, int padH = 28, Color? background = null)
        {
            var box = MakeBox(parent, "Button", background ?? UiTheme.Panel, UiTheme.BorderStrong, 1f, 8f);
            VGroup(box.Outer, 0, padH, 0, padH, 0, TextAnchor.MiddleCenter);
            Size(box.Outer, -1, height);
            Text(box.Outer, label, size, UiTheme.TextPale, FontStyles.Normal, TextAlignmentOptions.Center, 0f, false);
            return MakeButton(box.Outer.gameObject, box.Border);
        }

        // ---------- choice cards ----------

        /// <summary>Makes <paramref name="box"/> a selectable card wired to a <see cref="Choice"/>.</summary>
        public static Choice MakeChoice(Box box, string id, bool selectable, float normalBorder = 1f, float selectedBorder = 2f, GameObject badge = null)
        {
            var button = MakeButton(box.Outer.gameObject, box.Fill);
            var choice = box.Outer.gameObject.AddComponent<Choice>();
            GameObject overlay = null;
            if (!selectable)
            {
                var o = Rect(box.Outer, "LockedOverlay");
                Stretch(o);
                IgnoreLayout(o);
                Img(o, new Color(0.03f, 0.06f, 0.11f, 0.5f), 8f);
                var label = Text(o, "Fase berikut", 12f, UiTheme.TextMuted, FontStyles.Bold, TextAlignmentOptions.TopRight, 0f, false);
                Anchor(label.rectTransform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-14, -12), new Vector2(160, 18));
                overlay = o.gameObject;
            }
            Bind(choice, "id", id);
            Bind(choice, "button", button);
            Bind(choice, "border", box.Border);
            Bind(choice, "fill", box.Fill);
            Bind(choice, "inner", box.FillRect);
            Bind(choice, "selectedBadge", badge);
            Bind(choice, "lockedOverlay", overlay);
            Bind(choice, "selectable", selectable);
            Bind(choice, "normalBorder", normalBorder);
            Bind(choice, "selectedBorder", selectedBorder);
            return choice;
        }

        public static ChoiceGroup MakeGroup(GameObject host, params Choice[] choices)
        {
            var g = host.AddComponent<ChoiceGroup>();
            BindList(g, "choices", choices);
            return g;
        }

        // ---------- input field ----------

        public static TMP_InputField InputField(Transform parent, string placeholder, bool password)
        {
            var box = MakeBox(parent, "InputField", UiTheme.Input, UiTheme.BorderStrong, 1f, 8f);
            Size(box.Outer, -1, 52);
            var area = Rect(box.Outer, "Text Area");
            Stretch(area, 16, 4, 16, 4);
            IgnoreLayout(area);
            area.gameObject.AddComponent<RectMask2D>();
            var ph = Text(area, placeholder, 16f, UiTheme.TextDim, FontStyles.Italic, TextAlignmentOptions.MidlineLeft, 0f, false);
            ph.name = "Placeholder";
            Stretch(ph.rectTransform);
            var txt = Text(area, string.Empty, 16f, UiTheme.Text, FontStyles.Normal, TextAlignmentOptions.MidlineLeft, 0f, false);
            txt.name = "Text";
            Stretch(txt.rectTransform);
            var field = box.Outer.gameObject.AddComponent<TMP_InputField>();
            field.targetGraphic = box.Border;
            field.textViewport = area;
            field.textComponent = txt;
            field.placeholder = ph;
            field.caretColor = UiTheme.Accent;
            field.selectionColor = new Color(UiTheme.Accent.r, UiTheme.Accent.g, UiTheme.Accent.b, 0.35f);
            field.lineType = TMP_InputField.LineType.SingleLine;
            if (password) field.contentType = TMP_InputField.ContentType.Password;
            return field;
        }

        // ---------- slider and switch ----------

        public static SliderField SliderRow(Transform parent, string label, float min, float max, float value, string unit, string format, bool whole)
        {
            var row = Rect(parent, "SliderField");
            VGroup(row, 8);
            var top = Rect(row, "Top");
            HGroup(top, 0, 0, 0, 0, 0, TextAnchor.MiddleLeft, false, false);
            Text(top, label, 14f, UiTheme.TextSoft, FontStyles.Normal, TextAlignmentOptions.MidlineLeft, 0f, false);
            Spacer(top);
            var valueText = Text(top, string.Empty, 14f, UiTheme.Text, FontStyles.Normal, TextAlignmentOptions.MidlineRight, 0f, false);
            Mono(valueText);

            var sr = Rect(row, "Slider");
            Size(sr, -1, 18);
            var bg = Rect(sr, "Background");
            bg.anchorMin = new Vector2(0, 0.5f); bg.anchorMax = new Vector2(1, 0.5f); bg.sizeDelta = new Vector2(0, 6);
            Img(bg, UiTheme.Border, 3f);
            var fillArea = Rect(sr, "Fill Area");
            fillArea.anchorMin = new Vector2(0, 0.5f); fillArea.anchorMax = new Vector2(1, 0.5f);
            fillArea.offsetMin = new Vector2(9, -3); fillArea.offsetMax = new Vector2(-9, 3);
            var fill = Rect(fillArea, "Fill");
            fill.anchorMin = Vector2.zero; fill.anchorMax = new Vector2(0, 1); fill.sizeDelta = new Vector2(10, 0);
            Img(fill, UiTheme.Accent, 3f);
            var handleArea = Rect(sr, "Handle Slide Area");
            Stretch(handleArea, 9, 0, 9, 0);
            var handle = Rect(handleArea, "Handle");
            handle.sizeDelta = new Vector2(18, 18);
            var knobImg = handle.gameObject.AddComponent<Image>();
            knobImg.sprite = Knob;
            knobImg.color = UiTheme.Text;
            var slider = sr.gameObject.AddComponent<Slider>();
            slider.fillRect = fill;
            slider.handleRect = handle;
            slider.targetGraphic = knobImg;
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = min;
            slider.maxValue = max;
            slider.wholeNumbers = whole;
            slider.value = value;

            var field = row.gameObject.AddComponent<SliderField>();
            Bind(field, "slider", slider);
            Bind(field, "valueLabel", valueText);
            Bind(field, "unit", unit);
            Bind(field, "numberFormat", format);
            return field;
        }

        public static SwitchToggle SwitchRow(Transform parent, string label, bool isOn)
        {
            var row = Rect(parent, "SwitchRow");
            HGroup(row, 0, 0, 0, 0, 0, TextAnchor.MiddleLeft, false, false);
            Text(row, label, 14f, UiTheme.Text, FontStyles.Normal, TextAlignmentOptions.MidlineLeft, 0f, false);
            Spacer(row);
            var track = Rect(row, "Track");
            Size(track, 40, 22);
            var trackImg = Img(track, isOn ? UiTheme.Accent : UiTheme.Border, 11f, true);
            var knob = Rect(track, "Knob");
            knob.sizeDelta = new Vector2(16, 16);
            var knobImg = Img(knob, UiTheme.TextMuted, 8f);
            var button = MakeButton(track.gameObject, trackImg);
            button.transition = Selectable.Transition.None;
            var sw = row.gameObject.AddComponent<SwitchToggle>();
            Bind(sw, "button", button);
            Bind(sw, "track", trackImg);
            Bind(sw, "knob", knob);
            Bind(sw, "knobImage", knobImg);
            return sw;
        }

        // ---------- decorative ----------

        /// <summary>Simple ROV glyph (hull, lens, thruster stubs) drawn from rounded boxes.</summary>
        public static RectTransform Glyph(Transform parent, float width)
        {
            var root = Rect(parent, "Glyph");
            Size(root, width, 96);
            var body = MakeBox(root, "Hull", new Color(0.06f, 0.17f, 0.28f, 1f), UiTheme.Glyph, 2f, 22f);
            Anchor(body.Outer, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(width, 62));
            var lens = MakeBox(root, "Lens", UiTheme.OnAccent, UiTheme.Glyph, 2f, 18f);
            Anchor(lens.Outer, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(width * 0.26f, 0), new Vector2(36, 36));
            var dot = Rect(lens.Outer, "Dot");
            Anchor(dot, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(14, 14));
            Img(dot, new Color(UiTheme.Accent.r, UiTheme.Accent.g, UiTheme.Accent.b, 0.6f), 7f);
            foreach (float x in new[] { -0.28f, -0.1f })
                foreach (float y in new[] { 1f, -1f })
                {
                    var stub = Rect(root, "Thruster");
                    Anchor(stub, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(width * x, y * 40), new Vector2(3, 20));
                    Img(stub, UiTheme.Glyph);
                }
            return root;
        }

        public static Sprite GradientSprite(string fileName, Color top, Color mid, float midAt, Color bottom)
        {
            string path = ArtFolder + "/" + fileName + ".png";
            if (!File.Exists(path))
            {
                Directory.CreateDirectory(ArtFolder);
                const int h = 256;
                var tex = new Texture2D(4, h, TextureFormat.RGBA32, false);
                for (int y = 0; y < h; y++)
                {
                    float t = 1f - y / (h - 1f); // 0 at the top
                    Color c = t < midAt ? Color.Lerp(top, mid, t / midAt) : Color.Lerp(mid, bottom, (t - midAt) / (1f - midAt));
                    for (int x = 0; x < 4; x++) tex.SetPixel(x, y, c);
                }
                File.WriteAllBytes(path, tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
                AssetDatabase.ImportAsset(path);
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.mipmapEnabled = false;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        // ---------- serialized binding ----------

        public static void Bind(Object target, string field, object value)
        {
            var so = new SerializedObject(target);
            var p = so.FindProperty(field);
            if (p == null) { Debug.LogError("UiKit.Bind: no field '" + field + "' on " + target.GetType().Name); return; }
            switch (value)
            {
                case string s: p.stringValue = s; break;
                case bool b: p.boolValue = b; break;
                case float f: p.floatValue = f; break;
                case int i: p.intValue = i; break;
                case Object o: p.objectReferenceValue = o; break;
                case null: p.objectReferenceValue = null; break;
                default: Debug.LogError("UiKit.Bind: unsupported value for " + field); break;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void BindList(Object target, string field, Object[] values)
        {
            var so = new SerializedObject(target);
            var p = so.FindProperty(field);
            p.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) p.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
