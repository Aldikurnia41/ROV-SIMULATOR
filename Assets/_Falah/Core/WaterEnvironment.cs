using UnityEngine;

namespace Falah.RovSim.Core
{
    /// <summary>What the vehicle physics and the camera effects ask the water (ARCHITECTURE section 3).</summary>
    public interface IWaterEnvironment
    {
        /// <summary>Velocity of the water in world space, m/s.</summary>
        Vector3 CurrentAt(Vector3 worldPosition, float time);

        /// <summary>Horizontal sight distance in metres at the given depth (positive down).</summary>
        float VisibilityAt(float depth);

        float WaterSurfaceY { get; }

        /// <summary>kg/m³.</summary>
        float Density { get; }
    }

    /// <summary>
    /// Current and visibility of the training area. The current is a constant vector plus a slow, deterministic
    /// disturbance (two sines of different periods on speed and on direction); it is uniform in space for now, and the
    /// instructor can change every value while the session runs. Plain C#.
    /// </summary>
    public sealed class WaterEnvironment : IWaterEnvironment
    {
        public const float KnotsToMetersPerSecond = 0.514444f;

        /// <summary>exp(-(d·V)²) = 5% at the visibility distance V, for exponential-squared fog.</summary>
        public const float FogConstant = 1.7308f;

        // PLACEHOLDER disturbance: ±15% speed on a 37 s period, ±6° heading on a 61 s period.
        public float GustFraction = 0.15f;
        public float GustPeriodSeconds = 37f;
        public float DirectionWobbleDegrees = 6f;
        public float DirectionWobblePeriodSeconds = 61f;

        /// <summary>PLACEHOLDER: visibility falls to this fraction of the surface value at <see cref="VisibilityFullDepth"/>.</summary>
        public float VisibilityAtDepthFraction = 0.5f;
        public float VisibilityFullDepth = 200f;

        public float CurrentKnots { get; private set; }
        /// <summary>Direction the water flows towards, degrees clockwise from +Z (north).</summary>
        public float CurrentDirectionDegrees { get; private set; }
        public float SurfaceVisibilityMeters { get; private set; } = 4f;

        public float WaterSurfaceY { get; set; }
        public float Density { get; set; } = 1025f;

        public void SetCurrent(float knots, float directionDegrees)
        {
            CurrentKnots = Mathf.Max(0f, knots);
            CurrentDirectionDegrees = TelemetryMath.NormalizeHeading(directionDegrees);
        }

        public void SetVisibility(float meters) => SurfaceVisibilityMeters = Mathf.Max(0.1f, meters);

        /// <summary>The steady part of the current (no disturbance).</summary>
        public Vector3 BaseCurrent => FromPolar(CurrentKnots * KnotsToMetersPerSecond, CurrentDirectionDegrees);

        public Vector3 CurrentAt(Vector3 worldPosition, float time)
        {
            float speed = CurrentKnots * KnotsToMetersPerSecond;
            if (speed <= 0f) return Vector3.zero;
            float gust = 1f + GustFraction * Mathf.Sin(2f * Mathf.PI * time / Mathf.Max(GustPeriodSeconds, 0.1f));
            float wobble = DirectionWobbleDegrees * Mathf.Sin(2f * Mathf.PI * time / Mathf.Max(DirectionWobblePeriodSeconds, 0.1f) + 1.3f);
            return FromPolar(speed * gust, CurrentDirectionDegrees + wobble);
        }

        public float VisibilityAt(float depth)
        {
            float t = Mathf.Clamp01(Mathf.Max(depth, 0f) / Mathf.Max(VisibilityFullDepth, 1f));
            return SurfaceVisibilityMeters * Mathf.Lerp(1f, VisibilityAtDepthFraction, t);
        }

        /// <summary>Exponential-squared fog density that gives the visibility distance.</summary>
        public static float FogDensityFor(float visibilityMeters) => FogConstant / Mathf.Max(visibilityMeters, 0.1f);

        static Vector3 FromPolar(float speed, float degrees)
        {
            float r = degrees * Mathf.Deg2Rad;
            return new Vector3(Mathf.Sin(r), 0f, Mathf.Cos(r)) * speed;
        }
    }
}
