using UnityEngine;

namespace Falah.RovSim.Core
{
    /// <summary>
    /// Thrust efficiency (0..1) of each thruster under the faults the instructor injected. Computed on demand from the
    /// session log, so it has no state of its own. The values below are PLACEHOLDERS until the RovProfile exists and the
    /// real leak behaviour is confirmed with Pushidrosal.
    /// </summary>
    public static class ThrusterFaultModel
    {
        /// <summary>PLACEHOLDER: efficiency a leaking thruster settles at.</summary>
        public const float LeakFloorEfficiency = 0.4f;

        /// <summary>PLACEHOLDER: seconds a leak takes to reach the floor.</summary>
        public const float LeakDecaySeconds = 30f;

        /// <summary>Efficiency of one fault <paramref name="secondsSinceStart"/> after it began.</summary>
        public static float Single(FaultBehavior behavior, float secondsSinceStart)
        {
            switch (behavior)
            {
                case FaultBehavior.Dead:
                    return 0f;
                case FaultBehavior.GradualLoss:
                    return Mathf.Lerp(1f, LeakFloorEfficiency, Mathf.Clamp01(secondsSinceStart / LeakDecaySeconds));
                default:
                    return 1f;
            }
        }

        /// <summary>Efficiency of the zero-based thruster; the worst active fault wins. 1 when none applies.</summary>
        public static float Efficiency(int thrusterIndex, SessionLog log)
        {
            if (log == null) return 1f;
            float result = 1f;
            var all = DisturbanceCatalog.All;
            for (int i = 0; i < all.Length; i++)
            {
                var info = all[i];
                if (info.ThrusterIndex != thrusterIndex || info.Behavior == FaultBehavior.None) continue;
                if (!log.TryGetActiveSince(info.Kind, out float since)) continue;
                result = Mathf.Min(result, Single(info.Behavior, log.Elapsed - since));
            }
            return result;
        }

        /// <summary>Mean efficiency of two thrusters that share one axis (their forces add up equally).</summary>
        public static float Pair(SessionLog log, int a, int b) => 0.5f * (Efficiency(a, log) + Efficiency(b, log));
    }
}
