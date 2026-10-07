using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace Falah.RovSim.Core
{
    public enum SimEventType
    {
        SessionStart,
        ObjectiveDone,
        ThrusterFault,
        DisturbanceInjected,
        DisturbanceCleared,
        CurrentChanged,
        VisibilityChanged,
        Collision,
        DpEngaged,
        SessionPaused,
        SessionResumed,
        SessionEnd,
    }

    public enum EventSeverity { Info, Success, Warning, Critical }

    [Serializable]
    public struct SimEvent
    {
        public float Time;
        public SimEventType Type;
        public EventSeverity Severity;
        public string Message;
        /// <summary>Text shown on the pilot HUD banner (only for events that warn the pilot).</summary>
        public string PilotAlert;
    }

    [Serializable]
    public struct TrackPoint
    {
        public float Time;
        public float X;
        public float Y;
        public float Z;
    }

    /// <summary>
    /// Events and the vehicle track of the running session, shared by the instructor station, the HUD and the debrief.
    /// Plain C# (no scene objects) so it survives scene loads and can be unit tested. The persistent, versioned recording
    /// of the architecture (20 Hz, replay) is a later task; this is the in-memory session record.
    /// </summary>
    public sealed class SessionLog
    {
        public static SessionLog Current { get; private set; } = new SessionLog();

        readonly List<SimEvent> events = new List<SimEvent>();
        readonly List<TrackPoint> track = new List<TrackPoint>();
        /// <summary>Active disturbances and the session time they started.</summary>
        readonly Dictionary<DisturbanceKind, float> active = new Dictionary<DisturbanceKind, float>();

        public IReadOnlyList<SimEvent> Events => events;
        public IReadOnlyList<TrackPoint> Track => track;
        public IReadOnlyCollection<DisturbanceKind> ActiveDisturbances => active.Keys;

        /// <summary>Simulation seconds since the session started (stops while paused).</summary>
        public float Elapsed;
        public bool Ended;
        public string InstructorNote = string.Empty;

        public event Action<SimEvent> EventAdded;

        /// <summary>Raised for every event of whichever log is current; screens subscribe once and survive log resets.</summary>
        public static event Action<SimEvent> EventRaised;

        public static void ResetCurrent() => Current = new SessionLog();

        public SimEvent Add(SimEventType type, string message, EventSeverity severity = EventSeverity.Info, string pilotAlert = null)
        {
            var e = new SimEvent { Time = Elapsed, Type = type, Severity = severity, Message = message, PilotAlert = pilotAlert };
            events.Add(e);
            EventAdded?.Invoke(e);
            if (ReferenceEquals(this, Current)) EventRaised?.Invoke(e);
            return e;
        }

        public void AddTrack(Vector3 position)
        {
            track.Add(new TrackPoint { Time = Elapsed, X = position.x, Y = position.y, Z = position.z });
        }

        public bool IsActive(DisturbanceKind kind) => active.ContainsKey(kind);

        /// <summary>Session time at which the disturbance was injected.</summary>
        public bool TryGetActiveSince(DisturbanceKind kind, out float since) => active.TryGetValue(kind, out since);

        /// <summary>Starts the disturbance, or stops it when it is already active. Returns true when it is now active.</summary>
        public bool ToggleDisturbance(DisturbanceKind kind)
        {
            var info = DisturbanceCatalog.Find(kind);
            if (active.Remove(kind))
            {
                Add(SimEventType.DisturbanceCleared, "Instruktur: " + info.Label.ToLowerInvariant() + " dihentikan", EventSeverity.Info);
                return false;
            }
            active[kind] = Elapsed;
            Add(info.EventType, "Instruktur: " + info.Injected, EventSeverity.Warning, info.PilotAlert);
            return true;
        }

        /// <summary>mm:ss for a duration in seconds (hours roll into minutes).</summary>
        public static string FormatTime(float seconds)
        {
            int total = Mathf.Max(0, Mathf.FloorToInt(seconds));
            return (total / 60).ToString("00", CultureInfo.InvariantCulture) + ":" + (total % 60).ToString("00", CultureInfo.InvariantCulture);
        }
    }

    public enum DisturbanceKind { ThrusterLeak, ThrusterDead, LightsOut, TetherSnag, CommsLoss }

    public enum FaultBehavior
    {
        /// <summary>Not a thruster fault.</summary>
        None,
        /// <summary>Thrust efficiency falls gradually to a floor (leak).</summary>
        GradualLoss,
        /// <summary>No thrust at all.</summary>
        Dead,
    }

    public sealed class DisturbanceInfo
    {
        public DisturbanceKind Kind;
        public SimEventType EventType;
        /// <summary>Zero-based thruster the fault applies to; -1 when it is not a thruster fault.</summary>
        public int ThrusterIndex = -1;
        public FaultBehavior Behavior = FaultBehavior.None;
        public string Label;
        /// <summary>Event text for the instructor timeline and debrief.</summary>
        public string Injected;
        /// <summary>Banner text for the pilot HUD.</summary>
        public string PilotAlert;
    }

    public static class DisturbanceCatalog
    {
        public static readonly DisturbanceInfo[] All =
        {
            new DisturbanceInfo { Kind = DisturbanceKind.ThrusterLeak, EventType = SimEventType.ThrusterFault, Label = "Kebocoran thruster",
                ThrusterIndex = 2, Behavior = FaultBehavior.GradualLoss,
                Injected = "kebocoran thruster #3 diinjeksi", PilotAlert = "Kebocoran kecil terdeteksi, thruster #3" },
            new DisturbanceInfo { Kind = DisturbanceKind.ThrusterDead, EventType = SimEventType.ThrusterFault, Label = "Thruster mati",
                ThrusterIndex = 0, Behavior = FaultBehavior.Dead,
                Injected = "thruster mati diinjeksi", PilotAlert = "Thruster tidak merespons" },
            new DisturbanceInfo { Kind = DisturbanceKind.LightsOut, EventType = SimEventType.DisturbanceInjected, Label = "Lampu padam",
                Injected = "lampu padam diinjeksi", PilotAlert = "Lampu ROV padam" },
            new DisturbanceInfo { Kind = DisturbanceKind.TetherSnag, EventType = SimEventType.DisturbanceInjected, Label = "Tether tersangkut",
                Injected = "tether tersangkut diinjeksi", PilotAlert = "Tether tersangkut" },
            new DisturbanceInfo { Kind = DisturbanceKind.CommsLoss, EventType = SimEventType.DisturbanceInjected, Label = "Komunikasi hilang",
                Injected = "komunikasi hilang diinjeksi", PilotAlert = "Komunikasi dengan ROV terputus" },
        };

        public static DisturbanceInfo Find(DisturbanceKind kind)
        {
            foreach (var d in All) if (d.Kind == kind) return d;
            return All[0];
        }
    }
}
