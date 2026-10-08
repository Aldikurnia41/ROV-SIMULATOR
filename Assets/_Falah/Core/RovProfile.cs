using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace Falah.RovSim.Core
{
    public enum RovClass { Inspection, WorkClass }

    /// <summary>How far the numbers of a profile can be trusted (ARCHITECTURE section 4).</summary>
    public enum DataStatus { Placeholder, Estimated, Confirmed }

    /// <summary>
    /// All per-ROV data in one asset: mass, drag, thrusters, camera, DP gains and operating limits. The simulation reads
    /// its numbers from here, so adding a ROV means adding an asset, not changing code. Every value below is PLACEHOLDER
    /// until Pushidrosal confirms it; <see cref="Status"/> says so and the validator warns while it is not Confirmed.
    /// </summary>
    [CreateAssetMenu(menuName = "Falah/ROV Profile", fileName = "RovProfile")]
    public sealed class RovProfile : ScriptableObject
    {
        [Header("Identity")]
        public string Id = "tortuga";
        public string DisplayName = "Tortuga";
        public RovClass Class = RovClass.Inspection;
        public DataStatus Status = DataStatus.Placeholder;

        [Header("Mass and buoyancy")]
        [Tooltip("kg")] public float Mass = 20f;
        [Tooltip("m³ of displaced water at full submersion")] public float Volume = 0.002f;
        [Tooltip("Centre of buoyancy relative to the centre of mass, m")] public Vector3 CenterOfBuoyancyOffset = new Vector3(0f, 0.2f, 0f);

        [Header("Drag (Cd·A, per local axis X sway, Y heave, Z surge)")]
        public Vector3 LinearDrag = new Vector3(1.2f, 1.5f, 0.4f);
        public Vector3 AngularDrag = new Vector3(0.8f, 0.8f, 0.8f);
        [Tooltip("Fraction of the mass added by the surrounding water, per axis")] public Vector3 AddedMassFactor = new Vector3(0.5f, 0.8f, 0.2f);

        [Header("Thrusters")]
        public ThrusterDef[] Thrusters = new ThrusterDef[0];
        [Tooltip("Surge/sway/heave force at full operator command, N")] public Vector3 FullForce = new Vector3(200f, 200f, 300f);
        [Tooltip("Yaw torque at full operator command, N·m")] public float FullYawTorque = 60f;

        [Header("Camera")]
        public RovCameraSettings Camera = new RovCameraSettings();

        [Header("Station keeping")]
        public PidGains HeadingPid = new PidGains(2f, 0.2f, 1.5f, 20f, 60f);
        public PidGains DepthPid = new PidGains(120f, 40f, 160f, 200f, 300f);
        public PidGains PositionPid = new PidGains(150f, 10f, 40f, 60f, 200f);

        [Header("Operation")]
        [Tooltip("Hard depth limit, m")] public float MaxDepth = 500f;
        [Tooltip("m/s")] public float MaxSpeed = 2f;

        /// <summary>A profile built from the placeholder data of the prototype; used when no asset exists for the id.</summary>
        public static RovProfile CreateDefault(string id)
        {
            var p = CreateInstance<RovProfile>();
            p.Id = id;
            p.Status = DataStatus.Placeholder;
            p.Thrusters = ThrusterLayouts.For(id);
            p.Camera = RovCameraSettings.For(id);
            var dp = DpTuning.Defaults(id);
            p.HeadingPid = dp.Heading;
            p.DepthPid = dp.Depth;
            p.PositionPid = dp.Position;
            if (id == "teledyne")
            {
                p.DisplayName = "Teledyne";
                p.Mass = 25f;
                p.Volume = 0.0025f;
                p.MaxDepth = 300f;   // PLACEHOLDER, SPEC: public data 300 m, unverified
                p.MaxSpeed = 1.5f;   // PLACEHOLDER (~3 knots)
            }
            else
            {
                p.DisplayName = "Tortuga";
                p.MaxDepth = 500f;   // PLACEHOLDER, public data ~500 m, unverified
                p.MaxSpeed = 2f;
            }
            p.name = id;
            return p;
        }
    }

    /// <summary>Finds the profile of a ROV: the asset under Resources/RovProfiles, else the built-in placeholder.</summary>
    public static class RovProfiles
    {
        public const string ResourceFolder = "RovProfiles/";

        static readonly Dictionary<string, RovProfile> Fallbacks = new Dictionary<string, RovProfile>();

        public static RovProfile Get(string id)
        {
            var asset = Resources.Load<RovProfile>(ResourceFolder + id);
            if (asset != null) return asset;
            if (!Fallbacks.TryGetValue(id, out var fallback) || fallback == null)
            {
                Debug.LogWarning("No ROV profile asset for '" + id + "' under Resources/" + ResourceFolder + "; using built-in placeholder data.");
                fallback = RovProfile.CreateDefault(id);
                fallback.hideFlags = HideFlags.HideAndDontSave;
                Fallbacks[id] = fallback;
            }
            return fallback;
        }
    }

    /// <summary>Checks a profile for values that cannot work and for data that is not confirmed.</summary>
    public static class RovProfileValidator
    {
        public enum Level { Warning, Error }

        public struct Issue
        {
            public Level Level;
            public string Message;

            public override string ToString() => (Level == Level.Error ? "ERROR: " : "warning: ") + Message;
        }

        public static List<Issue> Validate(RovProfile p)
        {
            var issues = new List<Issue>();
            void Error(string m) => issues.Add(new Issue { Level = Level.Error, Message = m });
            void Warn(string m) => issues.Add(new Issue { Level = Level.Warning, Message = m });

            if (string.IsNullOrWhiteSpace(p.Id)) Error("Id is empty");
            if (p.Mass <= 0f) Error("Mass must be greater than zero");
            if (p.Volume <= 0f) Error("Volume must be greater than zero");
            if (p.MaxDepth <= 0f) Error("MaxDepth must be greater than zero");
            if (p.MaxSpeed <= 0f) Error("MaxSpeed must be greater than zero");
            if (Negative(p.LinearDrag) || Negative(p.AngularDrag)) Error("Drag coefficients must not be negative");
            if (Negative(p.AddedMassFactor)) Error("Added mass factors must not be negative");
            if (p.FullForce.x <= 0f || p.FullForce.y <= 0f || p.FullForce.z <= 0f || p.FullYawTorque <= 0f) Error("Full-command force and torque must be greater than zero");

            if (p.Thrusters == null || p.Thrusters.Length == 0) Error("No thrusters defined");
            else
                for (int i = 0; i < p.Thrusters.Length; i++)
                {
                    var t = p.Thrusters[i];
                    string name = "Thruster " + (i + 1) + (string.IsNullOrEmpty(t.Name) ? "" : " (" + t.Name + ")");
                    if (t.Direction.sqrMagnitude < 1e-6f) Error(name + " has no direction");
                    if (t.MaxForward <= 0f && t.MaxReverse <= 0f) Error(name + " has no thrust in either direction");
                    else if (t.MaxForward < 0f || t.MaxReverse < 0f) Error(name + " has a negative thrust limit");
                    if (t.ResponseTime <= 0f) Error(name + " needs a response time above zero");
                }

            if (p.Camera == null) Error("Camera settings missing");
            else
            {
                if (p.Camera.FieldOfView < 10f || p.Camera.FieldOfView > 150f) Error("Camera field of view must be between 10 and 150 degrees");
                if (p.Camera.MinTilt > p.Camera.MaxTilt) Error("Camera MinTilt is above MaxTilt");
                else if (p.Camera.DefaultTilt < p.Camera.MinTilt || p.Camera.DefaultTilt > p.Camera.MaxTilt) Error("Camera DefaultTilt is outside the tilt range");
            }

            CheckPid("Heading PID", p.HeadingPid, Error);
            CheckPid("Depth PID", p.DepthPid, Error);
            CheckPid("Position PID", p.PositionPid, Error);

            if (p.Status != DataStatus.Confirmed)
                Warn("Data status is " + p.Status.ToString() + ": the numbers are not confirmed with Pushidrosal");
            return issues;
        }

        static bool Negative(Vector3 v) => v.x < 0f || v.y < 0f || v.z < 0f;

        static void CheckPid(string label, PidGains g, Action<string> error)
        {
            if (g.Kp < 0f || g.Ki < 0f || g.Kd < 0f) error(label + " has a negative gain");
            if (g.OutputLimit <= 0f) error(label + " needs an output limit above zero");
            if (g.IntegralLimit < 0f) error(label + " has a negative integral limit");
            if (g.Ki > 0f && g.IntegralLimit <= 0f) error(label + " integrates (Ki > 0) but its integral limit is zero");
        }

        /// <summary>One line per issue, for logs.</summary>
        public static string Describe(RovProfile p)
        {
            var sb = new System.Text.StringBuilder();
            foreach (var i in Validate(p)) sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "[{0}] {1}", p.Id, i));
            return sb.ToString();
        }
    }
}
