using UnityEngine;

namespace Falah.RovSim.Core
{
    /// <summary>One reading of the vehicle state, in the conventions shown on the pilot HUD.</summary>
    public struct TelemetrySample
    {
        public float Time;
        public Vector3 Position;
        public Quaternion Rotation;
        /// <summary>Metres below the water surface (negative when above it).</summary>
        public float DepthMeters;
        /// <summary>Compass heading, 0..360.</summary>
        public float HeadingDegrees;
        /// <summary>Positive = nose up, -180..180.</summary>
        public float PitchDegrees;
        /// <summary>Positive = starboard (right) side down, -180..180.</summary>
        public float RollDegrees;
        /// <summary>Metres above the seabed; NaN when the seabed is out of range.</summary>
        public float AltitudeMeters;
    }

    /// <summary>Anything that can report the vehicle state (the HUD, recorder and instructor station read this).</summary>
    public interface ITelemetrySource
    {
        /// <summary>False while no vehicle is active.</summary>
        bool TryGetSample(out TelemetrySample sample);
    }

    public static class TelemetryMath
    {
        /// <summary>Wraps an angle into 0..360.</summary>
        public static float NormalizeHeading(float degrees)
        {
            float d = degrees % 360f;
            return d < 0f ? d + 360f : d;
        }

        /// <summary>Wraps an angle into -180..180.</summary>
        public static float NormalizeSigned(float degrees)
        {
            float d = NormalizeHeading(degrees);
            return d > 180f ? d - 360f : d;
        }

        /// <param name="seaLevelY">World Y of the water surface (0 in the simulator).</param>
        /// <param name="altitude">Metres above the seabed, or NaN when unknown.</param>
        public static TelemetrySample FromPose(float time, Vector3 position, Quaternion rotation, float seaLevelY, float altitude)
        {
            Vector3 euler = rotation.eulerAngles;
            return new TelemetrySample
            {
                Time = time,
                Position = position,
                Rotation = rotation,
                DepthMeters = seaLevelY - position.y,
                HeadingDegrees = NormalizeHeading(euler.y),
                // Unity: positive X rotation pitches the nose down, positive Z rolls the right side up.
                PitchDegrees = -NormalizeSigned(euler.x),
                RollDegrees = -NormalizeSigned(euler.z),
                AltitudeMeters = altitude,
            };
        }
    }
}
