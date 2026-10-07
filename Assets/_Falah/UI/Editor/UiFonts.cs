using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace Falah.RovSim.UI.Editor
{
    /// <summary>IBM Plex (OFL) as TMP dynamic font assets, created from the TTFs in Assets/_Falah/UI/Fonts on first use.</summary>
    internal static class UiFonts
    {
        const string Folder = "Assets/_Falah/UI/Fonts";

        public static TMP_FontAsset Sans(FontStyles style) => Load(style.HasFlag(FontStyles.Bold) ? "IBMPlexSans-SemiBold" : "IBMPlexSans-Regular");

        public static TMP_FontAsset SansBold() => Load("IBMPlexSans-Bold");

        public static TMP_FontAsset Mono() => Load("IBMPlexMono-Medium");

        static TMP_FontAsset Load(string name)
        {
            string assetPath = Folder + "/" + name + " SDF.asset";
            var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(assetPath);
            if (existing != null) return existing;

            var font = AssetDatabase.LoadAssetAtPath<Font>(Folder + "/" + name + ".ttf");
            if (font == null)
            {
                Debug.LogWarning("UiFonts: missing " + name + ".ttf, falling back to the TMP default font.");
                return TMP_Settings.defaultFontAsset;
            }
            var fa = TMP_FontAsset.CreateFontAsset(font, 90, 9, GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic, true);
            fa.name = name + " SDF";
            AssetDatabase.CreateAsset(fa, assetPath);
            fa.material.name = name + " Material";
            AssetDatabase.AddObjectToAsset(fa.material, fa);
            foreach (var atlas in fa.atlasTextures)
            {
                atlas.name = name + " Atlas";
                AssetDatabase.AddObjectToAsset(atlas, fa);
            }
            EditorUtility.SetDirty(fa);
            AssetDatabase.SaveAssets();
            return fa;
        }
    }
}
