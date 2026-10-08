using System;
using UnityEngine;

namespace Falah.RovSim.Scenario
{
    public enum ObjectiveType
    {
        /// <summary>Come within <see cref="ObjectiveDef.Radius"/> of the target.</summary>
        ReachZone,
        /// <summary>Stay within the radius for <see cref="ObjectiveDef.HoldSeconds"/> without leaving.</summary>
        HoldPosition,
        /// <summary>Keep the target in the camera view, within the radius, for the hold time.</summary>
        Identify,
        /// <summary>Within the radius of the target while the operator presses the mark key (records the position).</summary>
        MarkPosition,
        /// <summary>Come up to the surface: depth at or below <see cref="ObjectiveDef.Radius"/> metres.</summary>
        Surface,
    }

    public enum ObjectiveStatus { Pending, Active, Done, Failed }

    [Serializable]
    public sealed class ObjectiveDef
    {
        public string Id;
        public ObjectiveType Type;
        /// <summary>Indonesian text for the timeline and debrief.</summary>
        public string Description;
        /// <summary>Name the <see cref="ITargetLocator"/> resolves to a world position.</summary>
        public string TargetName;
        public float Radius = 10f;
        public float HoldSeconds;
        /// <summary>Identify only: the target must be within this angle of the camera axis.</summary>
        public float ViewHalfAngleDegrees = 25f;
        /// <summary>Identify only: deeper than this the target can only be seen with the lamp on (no sunlight). -1 = never needed.</summary>
        public float LampRequiredBelowDepth = -1f;
        public float MinLampLevel = 0.3f;
        /// <summary>0 = no limit. When exceeded the objective fails and the scenario moves on.</summary>
        public float TimeLimitSeconds;
    }

    /// <summary>A scenario is an ordered list of objectives; they are evaluated one at a time, in order.</summary>
    [Serializable]
    public sealed class ScenarioDef
    {
        public string Id;
        public string Name;
        public ObjectiveDef[] Objectives = new ObjectiveDef[0];
        /// <summary>Objects the scene must contain for this scenario (spawned over the seabed at session start).</summary>
        public TargetSpec[] Targets = new TargetSpec[0];
    }

    /// <summary>A search target placed relative to the vehicle's start position, on the seabed.</summary>
    [Serializable]
    public sealed class TargetSpec
    {
        public string Name;
        public float OffsetEast;
        public float OffsetNorth;
        public Vector3 Size = new Vector3(0.6f, 0.3f, 0.4f);
    }

    public struct VehicleState
    {
        public Vector3 Position;
        public Quaternion Rotation;
        /// <summary>Metres below the surface, positive down.</summary>
        public float Depth;
        /// <summary>0..1 brightness of the vehicle lamp.</summary>
        public float LampLevel;
        /// <summary>True for the frame in which the operator pressed the mark key.</summary>
        public bool MarkPressed;
    }

    /// <summary>Resolves a target name from scenario data to a world position (the scene decides where targets are).</summary>
    public interface ITargetLocator
    {
        bool TryGetPosition(string targetName, out Vector3 position);
    }

    /// <summary>Scenario data of the prototype. Numbers are PLACEHOLDERS until Pushidrosal confirms tolerances (SPEC 5).</summary>
    public static class ScenarioLibrary
    {
        public const string SonarContactId = "sonar-contact";

        /// <summary>Name of the scene object used as the sonar contact (the first search target in RoVGameplay).</summary>
        public const string SonarContactTarget = "suitcase";

        public const string BlackBoxId = "black-box";
        public const string BlackBoxTarget = "BlackBox";

        public static ScenarioDef Find(string id)
        {
            if (id == SonarContactId) return SonarContact();
            if (id == BlackBoxId) return BlackBox();
            return null;
        }

        /// <summary>
        /// Real search mission: an aircraft recorder lies on the seabed somewhere inside a search area. Dive to the area,
        /// find it on the sonar, identify it with the lamp, record its position, and return to the surface.
        /// The box is spawned over the seabed at the offset below; the offset, radii and times are PLACEHOLDERS to be set
        /// by the instructors (BACKLOG T2.3 "manual").
        /// </summary>
        public static ScenarioDef BlackBox() => new ScenarioDef
        {
            Id = BlackBoxId,
            Name = "Pencarian black box",
            Targets = new[] { new TargetSpec { Name = BlackBoxTarget, OffsetEast = 45f, OffsetNorth = 60f, Size = new Vector3(0.6f, 0.3f, 0.4f) } },
            Objectives = new[]
            {
                new ObjectiveDef
                {
                    Id = "reach-area", Type = ObjectiveType.ReachZone, TargetName = BlackBoxTarget,
                    Description = "Menuju area pencarian dan mendekati kontak sonar", Radius = 40f,
                },
                new ObjectiveDef
                {
                    Id = "identify", Type = ObjectiveType.Identify, TargetName = BlackBoxTarget,
                    Description = "Identifikasi black box secara visual (nyalakan lampu)", Radius = 8f, HoldSeconds = 3f,
                    ViewHalfAngleDegrees = 25f, LampRequiredBelowDepth = 40f, MinLampLevel = 0.3f,
                },
                new ObjectiveDef
                {
                    Id = "mark", Type = ObjectiveType.MarkPosition, TargetName = BlackBoxTarget,
                    Description = "Catat posisi black box (tekan Enter)", Radius = 8f,
                },
                new ObjectiveDef
                {
                    Id = "surface", Type = ObjectiveType.Surface, TargetName = string.Empty,
                    Description = "Kembali ke permukaan", Radius = 5f,
                },
            },
        };

        /// <summary>SPEC 5: dive and follow the sonar to the contact, hold station near it, identify the object.</summary>
        public static ScenarioDef SonarContact() => new ScenarioDef
        {
            Id = SonarContactId,
            Name = "Investigasi target sonar",
            Objectives = new[]
            {
                new ObjectiveDef
                {
                    Id = "reach-contact", Type = ObjectiveType.ReachZone, TargetName = SonarContactTarget,
                    Description = "Menyelam dan menuju kontak sonar", Radius = 40f, // PLACEHOLDER
                },
                new ObjectiveDef
                {
                    Id = "hold-station", Type = ObjectiveType.HoldPosition, TargetName = SonarContactTarget,
                    Description = "Menjaga posisi di dekat target", Radius = 12f, HoldSeconds = 10f, // PLACEHOLDER
                },
                new ObjectiveDef
                {
                    Id = "identify", Type = ObjectiveType.Identify, TargetName = SonarContactTarget,
                    Description = "Identifikasi objek secara visual", Radius = 10f, HoldSeconds = 3f, ViewHalfAngleDegrees = 25f, // PLACEHOLDER
                },
            },
        };
    }
}
