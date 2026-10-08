using UnityEngine;

namespace Falah.RovSim.Core
{
    /// <summary>
    /// The vehicle lamp: on/off, a brightness the operator sets, and a failure (instructor's "Lampu padam").
    /// Plain C# so the rules are testable; <c>RovLamp</c> drives the scene light from it and the HUD reads <see cref="Current"/>.
    /// </summary>
    public sealed class LampState
    {
        public const float MinBrightness = 0.1f;
        public const float StepSize = 0.1f;

        public static LampState Current { get; private set; } = new LampState();

        public static void ResetCurrent() => Current = new LampState();

        public bool On = true;
        public bool Failed;
        float brightness = 0.7f;

        /// <summary>Brightness the operator has dialled in, 0.1..1.</summary>
        public float Brightness
        {
            get => brightness;
            set => brightness = Mathf.Clamp(value, MinBrightness, 1f);
        }

        /// <summary>What actually shines: 0 when off or failed.</summary>
        public float Level => On && !Failed ? brightness : 0f;

        public void Toggle() => On = !On;

        public void Step(int direction) => Brightness = Mathf.Round((brightness + direction * StepSize) * 10f) / 10f;
    }
}
