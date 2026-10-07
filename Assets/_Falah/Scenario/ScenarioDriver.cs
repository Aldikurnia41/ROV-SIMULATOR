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

        ITelemetrySource source;
        ScenarioRunner runner;

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
        }

        void Update()
        {
            var log = SessionLog.Current;
            if (runner == null || log.Ended) return;
            if (!source.TryGetSample(out var sample)) return;
            runner.Tick(Time.deltaTime, new VehicleState { Position = sample.Position, Rotation = sample.Rotation }, log);
        }
    }
}
