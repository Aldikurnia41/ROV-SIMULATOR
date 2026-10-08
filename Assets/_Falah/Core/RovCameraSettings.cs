using UnityEngine;

namespace Falah.RovSim.Core
{
    /// <summary>Pilot camera of a ROV. PLACEHOLDER values until RovProfile (T0.4) carries the real camera data.</summary>
    [System.Serializable]
    public sealed class RovCameraSettings
    {
        public float FieldOfView = 60f;
        /// <summary>Degrees, positive = looking up, relative to the vehicle's forward axis.</summary>
        public float DefaultTilt = -12f;
        public float MinTilt = -90f;
        public float MaxTilt = 30f;
        /// <summary>Tilt speed at full input, degrees per second.</summary>
        public float TiltRate = 45f;

        public static RovCameraSettings For(string rovId)
        {
            if (rovId == "teledyne") return new RovCameraSettings { FieldOfView = 70f, DefaultTilt = 0f, MinTilt = -45f, MaxTilt = 45f, TiltRate = 35f };
            return new RovCameraSettings();
        }
    }

    public static class CameraTilt
    {
        /// <summary>Moves the tilt by input (-1..1) at <paramref name="rate"/> deg/s, clamped to the limits.</summary>
        public static float Step(float current, float input, float rate, float deltaTime, float min, float max)
        {
            return Mathf.Clamp(current + Mathf.Clamp(input, -1f, 1f) * rate * deltaTime, min, max);
        }

        /// <summary>Orbit pitch is limited so the camera never flips over the poles.</summary>
        public static float OrbitPitch(float pitch) => Mathf.Clamp(pitch, -80f, 80f);

        public static float OrbitDistance(float distance, float scrollDelta, float min, float max) =>
            Mathf.Clamp(distance * Mathf.Exp(-scrollDelta), min, max);
    }
}
