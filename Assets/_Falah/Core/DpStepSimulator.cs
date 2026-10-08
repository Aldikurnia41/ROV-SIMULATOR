using System;
using UnityEngine;

namespace Falah.RovSim.Core
{
    /// <summary>One-degree-of-freedom plant (mass with linear + quadratic drag) used to preview and test PID gains offline.</summary>
    [Serializable]
    public struct DpPlant
    {
        public float Mass;
        public float LinearDrag;
        public float QuadraticDrag;
        /// <summary>Constant external force (current) in the controlled direction, newtons.</summary>
        public float Disturbance;

        /// <summary>Roughly the scene ROV in surge: 20 kg, linear damping 2.5, quadratic drag 0.5·ρ·Cd·A.</summary>
        public static DpPlant SceneRov() => new DpPlant { Mass = 20f, LinearDrag = 50f, QuadraticDrag = 600f, Disturbance = 0f };
    }

    public struct StepResult
    {
        public float[] Positions;
        public float TimeStep;
        /// <summary>Peak beyond the target as a fraction of the step (0 = none).</summary>
        public float Overshoot;
        /// <summary>Seconds after which the response stays within 5% of the step; -1 when it never settles.</summary>
        public float SettleTime;
        /// <summary>Remaining error at the end, in the units of the target.</summary>
        public float FinalError;
    }

    public static class DpStepSimulator
    {
        public const float Step = 0.02f;

        /// <summary>Starts at 0 with a target of <paramref name="target"/>; returns the response over <paramref name="seconds"/>.</summary>
        public static StepResult Run(PidGains gains, DpPlant plant, float target, float seconds)
        {
            int n = Mathf.Max(2, Mathf.CeilToInt(seconds / Step));
            var pid = new PidController(gains);
            var positions = new float[n];
            float x = 0f, v = 0f, peak = 0f;
            for (int i = 0; i < n; i++)
            {
                float u = pid.Update(target - x, -v, Step);
                float drag = plant.LinearDrag * v + plant.QuadraticDrag * Mathf.Abs(v) * v;
                v += (u + plant.Disturbance - drag) / Mathf.Max(plant.Mass, 1e-3f) * Step;
                x += v * Step;
                positions[i] = x;
                peak = target >= 0f ? Mathf.Max(peak, x) : Mathf.Min(peak, x);
            }

            float band = Mathf.Abs(target) * 0.05f;
            int lastOutside = -1;
            for (int i = 0; i < n; i++) if (Mathf.Abs(positions[i] - target) > band) lastOutside = i;
            var r = new StepResult
            {
                Positions = positions,
                TimeStep = Step,
                SettleTime = lastOutside >= n - 1 ? -1f : (lastOutside + 1) * Step,
                FinalError = target - positions[n - 1],
            };
            float size = Mathf.Abs(target);
            r.Overshoot = size > 1e-6f ? Mathf.Max(0f, target < 0f ? (target - peak) / size : (peak - target) / size) : 0f;
            return r;
        }
    }
}
