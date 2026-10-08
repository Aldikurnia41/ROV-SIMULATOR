using UnityEngine;

namespace Falah.RovSim.Core
{
    /// <summary>Operator command for the vehicle, each axis -1..1. Sources: joystick/gamepad, keyboard, scripted tests.</summary>
    public interface IControlInput
    {
        /// <summary>x = sway (right +), y = heave (up +), z = surge (forward +).</summary>
        Vector3 Translation { get; }

        /// <summary>Yaw rate command, right (clockwise seen from above) +.</summary>
        float Yaw { get; }
    }

    /// <summary>Fixed command for tests and scripted scenarios.</summary>
    public sealed class ScriptedInput : IControlInput
    {
        public Vector3 Translation { get; set; }
        public float Yaw { get; set; }
    }

    /// <summary>Deadzone and response curve applied to raw stick values; pure functions.</summary>
    public static class ControlShaper
    {
        /// <summary>
        /// Rescaled deadzone: 0 inside <paramref name="deadzone"/>, then rises linearly from 0 to 1 at full deflection
        /// (no jump at the edge), followed by an expo blend (1-e)·x + e·x³ that softens small deflections.
        /// </summary>
        public static float Axis(float value, float deadzone, float expo)
        {
            float v = Mathf.Clamp(value, -1f, 1f);
            float a = Mathf.Abs(v);
            deadzone = Mathf.Clamp(deadzone, 0f, 0.95f);
            if (a <= deadzone) return 0f;
            float x = (a - deadzone) / (1f - deadzone);
            expo = Mathf.Clamp01(expo);
            return Mathf.Sign(v) * ((1f - expo) * x + expo * x * x * x);
        }

        /// <summary>Circular deadzone for a two-axis stick, so diagonal input is not cut by the square corners.</summary>
        public static Vector2 Stick(Vector2 value, float deadzone, float expo)
        {
            float m = value.magnitude;
            if (m < 1e-6f) return Vector2.zero;
            float shaped = Axis(Mathf.Min(m, 1f), deadzone, expo);
            return value / m * shaped;
        }
    }
}
