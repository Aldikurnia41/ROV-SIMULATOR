using UnityEngine;

namespace Falah.RovSim.UI
{
    /// <summary>Colour tokens from the UI design (artifact "ROV Trainer Pushidrosal: Desain UI").</summary>
    public static class UiTheme
    {
        public static readonly Color Bg = Hex("08101C");
        public static readonly Color Panel = Hex("0E1829");
        public static readonly Color Card = Hex("131F35");
        public static readonly Color CardSelected = Hex("12304B");
        public static readonly Color Border = Hex("26395B");
        public static readonly Color BorderStrong = Hex("2F4A73");
        public static readonly Color Accent = Hex("38B6F0");
        public static readonly Color OnAccent = Hex("06121F");
        public static readonly Color Text = Hex("E8F0FA");
        public static readonly Color TextSoft = Hex("B7C7DB");
        public static readonly Color TextPale = Hex("CFE3F6");
        public static readonly Color TextMuted = Hex("93A7C2");
        public static readonly Color TextDim = Hex("7F94B0");
        public static readonly Color Ok = Hex("43C08F");
        public static readonly Color OkBg = Hex("12362B");
        public static readonly Color Warn = Hex("F4B13E");
        public static readonly Color WarnBg = Hex("3A2E12");
        public static readonly Color Danger = Hex("EE5D5D");
        public static readonly Color DangerBg = Hex("3A1A1E");
        public static readonly Color Tag = Hex("1A2A47");
        public static readonly Color Input = Hex("131F35");
        public static readonly Color Glyph = Hex("7FD0F5");

        public static Color Hex(string rgb)
        {
            ColorUtility.TryParseHtmlString("#" + rgb, out var c);
            return c;
        }
    }
}
