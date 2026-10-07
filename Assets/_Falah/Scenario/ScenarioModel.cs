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
    }

    public struct VehicleState
    {
        public Vector3 Position;
        public Quaternion Rotation;
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

        public static ScenarioDef Find(string id)
        {
            if (id == SonarContactId) return SonarContact();
            return null;
        }

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
