using Falah.RovSim.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Falah.RovSim.UI
{
    /// <summary>
    /// Draws the sonar fan (design "SONAR IMAGING") into a texture: echoes of the latest sweep with a fading trail, range
    /// rings, and the fan outline. The pixel-to-beam lookup is built once, so a redraw allocates nothing.
    /// </summary>
    [RequireComponent(typeof(RawImage))]
    public sealed class SonarDisplay : MonoBehaviour
    {
        [SerializeField] MonoBehaviour sonarSource;
        [SerializeField] TMP_Text rangeLabel;
        [SerializeField] int width = 296;
        [SerializeField] int height = 200;
        [SerializeField] float echoThicknessMeters = 1.0f;
        [Range(0f, 1f)] [SerializeField] float trailDecay = 0.8f;

        static readonly Color32 Background = new Color32(3, 16, 24, 255);
        static readonly Color32 FanTint = new Color32(11, 58, 47, 255);
        static readonly Color32 RingColor = new Color32(40, 120, 92, 255);
        static readonly Color32 EchoColor = new Color32(155, 232, 196, 255);

        ISonarSource source;
        Texture2D texture;
        Color32[] pixels;
        float[] intensity;      // trail
        int[] pixelBeam;        // beam index per pixel, -1 outside the fan
        float[] pixelRange;     // metres per pixel
        bool[] isRing;
        int lastVersion = -1;
        SonarSweep builtFor;

        void Awake()
        {
            source = sonarSource as ISonarSource;
            texture = new Texture2D(width, height, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp, name = "SonarDisplay" };
            pixels = new Color32[width * height];
            intensity = new float[width * height];
            pixelBeam = new int[width * height];
            pixelRange = new float[width * height];
            isRing = new bool[width * height];
            GetComponent<RawImage>().texture = texture;
            Redraw(null);
        }

        void OnDestroy()
        {
            if (texture != null) Destroy(texture);
        }

        void Update()
        {
            if (source == null || !source.TryGetSweep(out var sweep)) return;
            if (sweep != builtFor) BuildLookup(sweep);
            if (sweep.Version == lastVersion) return;
            lastVersion = sweep.Version;
            Redraw(sweep);
        }

        /// <summary>Maps every pixel to its beam and range for the given sweep geometry (done once per sensor setup).</summary>
        void BuildLookup(SonarSweep sweep)
        {
            builtFor = sweep;
            lastVersion = -1;
            if (rangeLabel != null) rangeLabel.text = Mathf.RoundToInt(sweep.MaxRange) + " m";
            float cx = width * 0.5f, originY = 2f;
            float scale = Mathf.Min((height - originY - 2f) / sweep.MaxRange, (width * 0.5f - 2f) / (sweep.MaxRange * Mathf.Sin(Mathf.Min(sweep.SectorDegrees * 0.5f, 90f) * Mathf.Deg2Rad)));
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                {
                    int i = y * width + x;
                    float dx = (x + 0.5f - cx) / scale, dz = (y + 0.5f - originY) / scale;
                    float r = Mathf.Sqrt(dx * dx + dz * dz);
                    float bearing = Mathf.Atan2(dx, dz) * Mathf.Rad2Deg;
                    pixelRange[i] = r;
                    pixelBeam[i] = r <= sweep.MaxRange ? SonarGeometry.NearestBeam(bearing, sweep.BeamCount, sweep.SectorDegrees) : -1;
                    float ringStep = sweep.MaxRange / 3f;
                    float nearRing = Mathf.Abs(r - Mathf.Round(r / ringStep) * ringStep);
                    isRing[i] = pixelBeam[i] >= 0 && r > 0.5f && nearRing < 0.5f / scale;
                }
        }

        void Redraw(SonarSweep sweep)
        {
            for (int i = 0; i < pixels.Length; i++)
            {
                int beam = sweep != null ? pixelBeam[i] : -1;
                if (beam < 0) { pixels[i] = Background; intensity[i] = 0f; continue; }
                float echo = SonarGeometry.Echo(pixelRange[i], sweep.Ranges[beam], echoThicknessMeters);
                intensity[i] = Mathf.Max(echo, intensity[i] * trailDecay);
                float v = intensity[i];
                Color32 baseColor = isRing[i] ? RingColor : FanTint;
                pixels[i] = new Color32(
                    (byte)(baseColor.r + (EchoColor.r - baseColor.r) * v),
                    (byte)(baseColor.g + (EchoColor.g - baseColor.g) * v),
                    (byte)(baseColor.b + (EchoColor.b - baseColor.b) * v), 255);
            }
            texture.SetPixels32(pixels);
            texture.Apply(false);
        }

        /// <summary>Forces a redraw from the current sweep (used by tests and tools).</summary>
        public void Refresh()
        {
            if (source != null && source.TryGetSweep(out var sweep))
            {
                if (sweep != builtFor) BuildLookup(sweep);
                Redraw(sweep);
            }
        }
    }
}
