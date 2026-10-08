using System.Collections.Generic;
using Falah.RovSim.Core;
using UnityEngine;

namespace Falah.RovSim.Scenario
{
    /// <summary>Finds scenario targets by scene object name. Lookups are cached; a missing target is retried every second.</summary>
    public sealed class SceneTargetLocator : ITargetLocator
    {
        const float RetryInterval = 1f;

        readonly Dictionary<string, Transform> cache = new Dictionary<string, Transform>();
        readonly Dictionary<string, float> nextTry = new Dictionary<string, float>();

        public bool TryGetPosition(string targetName, out Vector3 position)
        {
            if (!cache.TryGetValue(targetName, out var t) || t == null)
            {
                nextTry.TryGetValue(targetName, out float due);
                if (Time.unscaledTime < due) { position = default; return false; }
                var go = GameObject.Find(targetName);
                if (go == null)
                {
                    nextTry[targetName] = Time.unscaledTime + RetryInterval;
                    position = default;
                    return false;
                }
                // Keep the transform even if the object is later deactivated (found targets are hidden by the old tracker).
                t = go.transform;
                cache[targetName] = t;
            }
            position = t.position;
            return true;
        }
    }

    /// <summary>Runs the session's scenario in the simulation scene: feeds the vehicle state to the <see cref="ScenarioRunner"/>.</summary>
    public sealed class ScenarioDriver : MonoBehaviour
    {
        [SerializeField] MonoBehaviour telemetrySource;

        const float PlacementTimeout = 8f;   // wait this long for the seabed collider before using the fallback depth
        const float SeabedProbeHeight = 300f;

        ITelemetrySource source;
        ScenarioRunner runner;
        readonly SeabedContact contact = new SeabedContact();
        readonly List<TargetSpec> unplaced = new List<TargetSpec>();
        Vector3 origin;
        bool hasOrigin;
        Vector3 lastPosition;
        float placeDeadline;

        public ScenarioRunner Runner => runner;

        void Start()
        {
            source = telemetrySource as ITelemetrySource;
            var scenario = ScenarioLibrary.Find(SessionSetup.Current.ScenarioId);
            if (scenario == null || source == null)
            {
                enabled = false;
                return;
            }
            runner = new ScenarioRunner(scenario, new SceneTargetLocator());
            unplaced.AddRange(scenario.Targets);
            placeDeadline = Time.unscaledTime + PlacementTimeout;
        }

        void Update()
        {
            var log = SessionLog.Current;
            if (runner == null || log.Ended) return;
            if (!source.TryGetSample(out var sample)) return;
            if (!hasOrigin) { origin = sample.Position; lastPosition = origin; hasOrigin = true; }
            if (unplaced.Count > 0) PlaceTargets(sample);

            float dt = Time.deltaTime;
            float speed = dt > 0f ? (sample.Position - lastPosition).magnitude / dt : 0f;
            lastPosition = sample.Position;
            if (contact.Update(sample.AltitudeMeters, speed))
                log.Add(SimEventType.Collision, "ROV menyentuh dasar laut (" + speed.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) + " m/s)", EventSeverity.Critical);

            runner.Tick(dt, new VehicleState
            {
                Position = sample.Position,
                Rotation = sample.Rotation,
                Depth = sample.DepthMeters,
                LampLevel = LampState.Current.Level,
                MarkPressed = OperatorMark.Pressed,
            }, log);
        }

        /// <summary>Puts each target on the seabed below its offset from the start point; falls back to the session's target depth when no collider answers.</summary>
        void PlaceTargets(TelemetrySample sample)
        {
            bool timedOut = Time.unscaledTime >= placeDeadline;
            for (int i = unplaced.Count - 1; i >= 0; i--)
            {
                var spec = unplaced[i];
                var xz = new Vector3(origin.x + spec.OffsetEast, 0f, origin.z + spec.OffsetNorth);
                float seaY = sample.Position.y + sample.DepthMeters;
                float y;
                if (Physics.Raycast(new Vector3(xz.x, seaY + SeabedProbeHeight, xz.z), Vector3.down, out var hit, 2f * SeabedProbeHeight + 2000f, ~0, QueryTriggerInteraction.Ignore))
                    y = hit.point.y + spec.Size.y * 0.5f;
                else if (timedOut)
                    y = seaY - SessionSetup.Current.TargetDepthMeters; // PLACEHOLDER until the Cesium collider is reliable offline
                else continue;
                var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                go.name = spec.Name;
                go.transform.position = new Vector3(xz.x, y, xz.z);
                go.transform.localScale = spec.Size;
                var r = go.GetComponent<Renderer>();
                if (r != null) r.material.color = new Color(0.95f, 0.45f, 0.1f); // recorder orange
                unplaced.RemoveAt(i);
            }
        }
    }
}
