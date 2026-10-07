using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Falah.RovSim.Core
{
    /// <summary>
    /// Saved summary of a session (JSON, versioned from the start). Scores stay empty until the weights are agreed with
    /// Pushidrosal (SPEC section 2); the raw facts (duration, events, note) are always recorded.
    /// </summary>
    [Serializable]
    public sealed class SessionReport
    {
        public const int FormatVersion = 1;

        public int version = FormatVersion;
        public string savedAtUtc;
        public string trainee;
        public string role;
        public string rovId;
        public string mode;
        public string scenarioId;
        public string location;
        public float durationSeconds;
        public float currentKnots;
        public float visibilityMeters;
        public string instructorNote;
        public int objectivesDone;
        public int objectivesTotal;
        public List<SimEvent> events = new List<SimEvent>();
        public int trackPoints;

        public static SessionReport Build(SessionSetup setup, SessionLog log, DateTime utcNow)
        {
            return new SessionReport
            {
                savedAtUtc = utcNow.ToString("o"),
                trainee = setup.UserName,
                role = setup.Role.ToString(),
                rovId = setup.RovId,
                mode = setup.Mode.ToString(),
                scenarioId = setup.ScenarioId,
                location = setup.LocationLabel,
                durationSeconds = log.Elapsed,
                currentKnots = setup.CurrentKnots,
                visibilityMeters = setup.VisibilityMeters,
                instructorNote = log.InstructorNote,
                objectivesDone = log.ObjectivesDone,
                objectivesTotal = log.ObjectivesTotal,
                events = new List<SimEvent>(log.Events),
                trackPoints = log.Track.Count,
            };
        }

        public string ToJson() => JsonUtility.ToJson(this, true);

        /// <summary>Writes the report as sessions/report_yyyyMMdd_HHmmss.json under <paramref name="rootFolder"/>.</summary>
        public string Save(string rootFolder)
        {
            string folder = Path.Combine(rootFolder, "sessions");
            Directory.CreateDirectory(folder);
            string name = "report_" + DateTime.UtcNow.ToString("yyyyMMdd_HHmmss") + ".json";
            string path = Path.Combine(folder, name);
            File.WriteAllText(path, ToJson());
            return path;
        }
    }
}
