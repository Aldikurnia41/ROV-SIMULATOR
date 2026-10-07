using System.Collections.Generic;
using System.Globalization;
using Falah.RovSim.Core;
using UnityEngine;

namespace Falah.RovSim.Scenario
{
    /// <summary>
    /// Evaluates a scenario's objectives in order, one at a time, and records SimEvents in the session log.
    /// Plain C#: the caller feeds it the elapsed time and the vehicle state each frame.
    /// </summary>
    public sealed class ScenarioRunner
    {
        readonly ScenarioDef scenario;
        readonly ITargetLocator targets;
        readonly ObjectiveStatus[] status;

        float activeSeconds;
        float holdSeconds;
        bool missingTargetReported;

        public ScenarioRunner(ScenarioDef scenario, ITargetLocator targets)
        {
            this.scenario = scenario;
            this.targets = targets;
            status = new ObjectiveStatus[scenario.Objectives.Length];
            for (int i = 0; i < status.Length; i++) status[i] = ObjectiveStatus.Pending;
            if (status.Length > 0) status[0] = ObjectiveStatus.Active;
            else Finished = true;
        }

        public ScenarioDef Scenario => scenario;
        public IReadOnlyList<ObjectiveStatus> Status => status;

        /// <summary>Index of the objective being evaluated; equals the objective count once the scenario is finished.</summary>
        public int ActiveIndex { get; private set; }

        public bool Finished { get; private set; }

        /// <summary>Progress 0..1 of the active objective's hold timer (0 for objectives without one).</summary>
        public float Progress { get; private set; }

        public int DoneCount
        {
            get
            {
                int n = 0;
                foreach (var s in status) if (s == ObjectiveStatus.Done) n++;
                return n;
            }
        }

        public void Tick(float deltaTime, VehicleState vehicle, SessionLog log)
        {
            if (Finished || log == null) return;
            var objective = scenario.Objectives[ActiveIndex];
            activeSeconds += deltaTime;

            if (targets.TryGetPosition(objective.TargetName, out Vector3 target))
            {
                if (Satisfied(objective, vehicle, target, deltaTime)) { Resolve(ObjectiveStatus.Done, log); return; }
            }
            else if (!missingTargetReported)
            {
                missingTargetReported = true;
                log.Add(SimEventType.ObjectiveDone, "Target \"" + objective.TargetName + "\" tidak ditemukan di scene", EventSeverity.Warning);
            }

            if (objective.TimeLimitSeconds > 0f && activeSeconds >= objective.TimeLimitSeconds) Resolve(ObjectiveStatus.Failed, log);
            else Publish(log);
        }

        bool Satisfied(ObjectiveDef objective, VehicleState vehicle, Vector3 target, float deltaTime)
        {
            bool inside = Vector3.Distance(vehicle.Position, target) <= objective.Radius;
            switch (objective.Type)
            {
                case ObjectiveType.ReachZone:
                    Progress = 0f;
                    return inside;
                case ObjectiveType.HoldPosition:
                    return Hold(inside, objective, deltaTime);
                case ObjectiveType.Identify:
                    return Hold(inside && InView(vehicle, target, objective.ViewHalfAngleDegrees), objective, deltaTime);
                default:
                    return false;
            }
        }

        bool Hold(bool condition, ObjectiveDef objective, float deltaTime)
        {
            holdSeconds = condition ? holdSeconds + deltaTime : 0f;
            Progress = objective.HoldSeconds > 0f ? Mathf.Clamp01(holdSeconds / objective.HoldSeconds) : (condition ? 1f : 0f);
            return condition && holdSeconds >= objective.HoldSeconds;
        }

        static bool InView(VehicleState vehicle, Vector3 target, float halfAngleDegrees)
        {
            Vector3 toTarget = target - vehicle.Position;
            if (toTarget.sqrMagnitude < 1e-6f) return true;
            return Vector3.Angle(vehicle.Rotation * Vector3.forward, toTarget) <= halfAngleDegrees;
        }

        void Resolve(ObjectiveStatus result, SessionLog log)
        {
            var objective = scenario.Objectives[ActiveIndex];
            status[ActiveIndex] = result;
            string number = (ActiveIndex + 1).ToString(CultureInfo.InvariantCulture);
            if (result == ObjectiveStatus.Done)
                log.Add(SimEventType.ObjectiveDone, "Objektif " + number + " selesai: " + objective.Description, EventSeverity.Success);
            else
                log.Add(SimEventType.ObjectiveFailed, "Objektif " + number + " gagal (waktu habis): " + objective.Description, EventSeverity.Warning);

            ActiveIndex++;
            activeSeconds = 0f;
            holdSeconds = 0f;
            Progress = 0f;
            missingTargetReported = false;
            if (ActiveIndex >= status.Length)
            {
                Finished = true;
                log.Add(SimEventType.ScenarioComplete,
                    DoneCount == status.Length ? "Semua objektif selesai" : "Skenario selesai: " + DoneCount + " dari " + status.Length + " objektif",
                    DoneCount == status.Length ? EventSeverity.Success : EventSeverity.Warning);
            }
            else status[ActiveIndex] = ObjectiveStatus.Active;
            Publish(log);
        }

        /// <summary>Copies the progress into the session log so the instructor station and debrief can show it.</summary>
        void Publish(SessionLog log)
        {
            log.ObjectivesTotal = status.Length;
            log.ObjectivesDone = DoneCount;
            if (Finished) { log.ObjectiveSummary = DoneCount + "/" + status.Length + " selesai"; return; }
            string detail = scenario.Objectives[ActiveIndex].Description;
            if (Progress > 0f) detail += " (" + Mathf.RoundToInt(Progress * 100f).ToString(CultureInfo.InvariantCulture) + "%)";
            log.ObjectiveSummary = DoneCount + "/" + status.Length + " · " + detail;
        }
    }
}
