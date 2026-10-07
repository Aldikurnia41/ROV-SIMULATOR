using System.Collections.Generic;
using Falah.RovSim.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Falah.RovSim.UI
{
    /// <summary>
    /// Top-down map (X east, Z north). Live mode follows the vehicle and keeps a trail; track mode draws a finished
    /// session fitted to the area, with the stretches during injected disturbances in amber.
    /// </summary>
    public sealed class TrackMap : MonoBehaviour
    {
        [SerializeField] RectTransform area;
        [SerializeField] RectTransform vehicleMarker;
        [SerializeField] RectTransform headingTick;
        [SerializeField] RectTransform startMarker;
        [SerializeField] RectTransform endMarker;
        [SerializeField] Image dotTemplate;
        [SerializeField] int maxDots = 300;
        [SerializeField] float minRangeMeters = 60f;
        [SerializeField] float liveSpacingMeters = 2f;

        readonly List<Image> dots = new List<Image>();
        readonly List<Vector2> trail = new List<Vector2>();
        Vector2 origin;
        bool hasOrigin;

        void Awake()
        {
            if (dotTemplate != null) dotTemplate.gameObject.SetActive(false);
        }

        // ---------- live ----------

        public void Feed(Vector3 position, float headingDegrees)
        {
            var p = new Vector2(position.x, position.z);
            if (!hasOrigin) { origin = p; hasOrigin = true; trail.Add(p); }
            else if ((trail[trail.Count - 1] - p).magnitude >= liveSpacingMeters)
            {
                trail.Add(p);
                if (trail.Count > maxDots) trail.RemoveAt(0);
            }
            float range = Mathf.Max(minRangeMeters, Extent(trail, origin) * 1.25f);
            Place(vehicleMarker, ToMap(p, range), true);
            if (headingTick != null) headingTick.localEulerAngles = new Vector3(0f, 0f, -headingDegrees);
            DrawDots(trail, range, null);
            if (startMarker != null) Place(startMarker, ToMap(trail[0], range), true);
            if (endMarker != null) endMarker.gameObject.SetActive(false);
        }

        // ---------- recorded track ----------

        IReadOnlyList<TrackPoint> trackPoints;
        IReadOnlyList<SimEvent> trackEvents;
        Vector2 drawnSize;

        /// <summary>
        /// Shows a finished session. Drawing waits for the area to have a size (layout runs after OnEnable) and repeats
        /// whenever the area is resized, because positions are in pixels.
        /// </summary>
        public void ShowTrack(IReadOnlyList<TrackPoint> points, IReadOnlyList<SimEvent> events)
        {
            trackPoints = points;
            trackEvents = events;
            drawnSize = Vector2.zero;
        }

        void LateUpdate()
        {
            if (trackPoints == null) return;
            Vector2 size = area.rect.size;
            if (size.x <= 1f || size.y <= 1f || size == drawnSize) return;
            DrawTrack(trackPoints, trackEvents);
            drawnSize = size;
        }

        void DrawTrack(IReadOnlyList<TrackPoint> points, IReadOnlyList<SimEvent> events)
        {
            if (vehicleMarker != null) vehicleMarker.gameObject.SetActive(false);
            if (points.Count == 0)
            {
                DrawDots(new List<Vector2>(), minRangeMeters, null);
                if (startMarker != null) startMarker.gameObject.SetActive(false);
                if (endMarker != null) endMarker.gameObject.SetActive(false);
                return;
            }
            int stride = Mathf.Max(1, Mathf.CeilToInt(points.Count / (float)maxDots));
            var xy = new List<Vector2>();
            var times = new List<float>();
            for (int i = 0; i < points.Count; i += stride)
            {
                xy.Add(new Vector2(points[i].X, points[i].Z));
                times.Add(points[i].Time);
            }
            var bounds = Bounds(xy);
            origin = bounds.center;
            hasOrigin = true;
            float range = Mathf.Max(minRangeMeters, Mathf.Max(bounds.size.x, bounds.size.y) * 0.62f);
            DrawDots(xy, range, DisturbanceIntervals(events, points[points.Count - 1].Time, times));
            Place(startMarker, ToMap(xy[0], range), true);
            Place(endMarker, ToMap(xy[xy.Count - 1], range), true);
        }

        // ---------- shared ----------

        void DrawDots(List<Vector2> pts, float range, List<bool> amber)
        {
            while (dots.Count < pts.Count)
            {
                var d = Instantiate(dotTemplate, dotTemplate.transform.parent);
                d.gameObject.name = "Dot" + dots.Count;
                dots.Add(d);
            }
            for (int i = 0; i < dots.Count; i++)
            {
                bool used = i < pts.Count;
                dots[i].gameObject.SetActive(used);
                if (!used) continue;
                ((RectTransform)dots[i].transform).anchoredPosition = ToMap(pts[i], range);
                dots[i].color = amber != null && amber[i] ? UiTheme.Warn : UiTheme.Accent;
            }
        }

        Vector2 ToMap(Vector2 world, float range)
        {
            Vector2 half = area.rect.size * 0.5f;
            float scale = Mathf.Min(half.x, half.y) * 0.9f / range;
            return (world - origin) * scale;
        }

        static void Place(RectTransform marker, Vector2 position, bool visible)
        {
            if (marker == null) return;
            marker.gameObject.SetActive(visible);
            marker.anchoredPosition = position;
        }

        static float Extent(List<Vector2> pts, Vector2 center)
        {
            float e = 0f;
            foreach (var p in pts) e = Mathf.Max(e, Mathf.Abs(p.x - center.x), Mathf.Abs(p.y - center.y));
            return e;
        }

        static Rect Bounds(List<Vector2> pts)
        {
            Vector2 min = pts[0], max = pts[0];
            foreach (var p in pts) { min = Vector2.Min(min, p); max = Vector2.Max(max, p); }
            return new Rect(min, max - min);
        }

        /// <summary>For each sampled point, whether it falls inside an injected-disturbance interval.</summary>
        public static List<bool> DisturbanceIntervals(IReadOnlyList<SimEvent> events, float endTime, List<float> times)
        {
            var result = new List<bool>(new bool[times.Count]);
            float? start = null;
            var spans = new List<Vector2>();
            foreach (var e in events)
            {
                bool injected = e.Type == SimEventType.ThrusterFault || e.Type == SimEventType.DisturbanceInjected;
                if (injected && start == null) start = e.Time;
                else if (e.Type == SimEventType.DisturbanceCleared && start != null) { spans.Add(new Vector2(start.Value, e.Time)); start = null; }
            }
            if (start != null) spans.Add(new Vector2(start.Value, endTime));
            for (int i = 0; i < times.Count; i++)
                foreach (var s in spans)
                    if (times[i] >= s.x && times[i] <= s.y) result[i] = true;
            return result;
        }
    }
}
