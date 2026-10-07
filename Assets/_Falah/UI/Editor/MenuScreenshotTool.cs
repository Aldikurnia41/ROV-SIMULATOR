using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Falah.RovSim.UI.Editor
{
    /// <summary>
    /// Renders one menu screen at 1600x900 to a PNG, in edit mode (no Play mode needed).
    /// Used for design review: Falah/UI/Capture screens, or call Capture(index, path) from tooling.
    /// </summary>
    public static class MenuScreenshotTool
    {
        const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

        static readonly System.Collections.Generic.HashSet<int> initialized = new System.Collections.Generic.HashSet<int>();

        [MenuItem("Falah/UI/Capture screens")]
        public static void CaptureAll()
        {
            Directory.CreateDirectory("Assets/Screenshots");
            for (int i = 0; i < 4; i++) Capture(i, "Assets/Screenshots/menu_screen_" + (i + 1) + ".png");
        }

        public static string Capture(int screenIndex, string path)
        {
            var flow = Object.FindFirstObjectByType<MenuFlow>(FindObjectsInactive.Include);
            var camera = Camera.main;
            if (flow == null || camera == null) return "no MenuFlow/camera in the open scene";
            var screens = flow.GetComponentsInChildren<MenuScreen>(true);

            // Awake does not run in edit mode; run it once so button/group listeners exist, then show the screen.
            foreach (var s in screens)
            {
                s.gameObject.SetActive(true);
                if (initialized.Add(s.GetInstanceID())) s.GetType().GetMethod("Awake", Private)?.Invoke(s, null);
                s.gameObject.SetActive(false);
            }
            var target = screens[screenIndex];
            target.gameObject.SetActive(true);
            target.OnShow();

            const int w = 1600, h = 900;
            var rt = RenderTexture.GetTemporary(w, h, 24, RenderTextureFormat.ARGB32);
            var previous = camera.targetTexture;
            camera.targetTexture = rt;
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)target.transform);
            Canvas.ForceUpdateCanvases();
            camera.Render();
            camera.targetTexture = previous;

            var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
            var active = RenderTexture.active;
            RenderTexture.active = rt;
            tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            tex.Apply();
            RenderTexture.active = active;
            RenderTexture.ReleaseTemporary(rt);
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);

            target.gameObject.SetActive(false);
            screens[0].gameObject.SetActive(true);
            return path;
        }
    }
}
