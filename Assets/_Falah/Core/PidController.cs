using System;
using UnityEngine;

namespace Falah.RovSim.Core
{
    [Serializable]
    public struct PidGains
    {
        public float Kp;
        public float Ki;
        public float Kd;
        /// <summary>Largest magnitude of the integral term (output units).</summary>
        public float IntegralLimit;
        /// <summary>Largest magnitude of the controller output.</summary>
        public float OutputLimit;

        public PidGains(float kp, float ki, float kd, float integralLimit, float outputLimit)
        {
            Kp = kp;
            Ki = ki;
            Kd = kd;
            IntegralLimit = integralLimit;
            OutputLimit = outputLimit;
        }
    }

    /// <summary>
    /// PID with the derivative taken from the caller-supplied error rate (the measured velocity, so there is no noisy
    /// differencing and no kick when the target changes) and two anti-windup measures: the integral term is clamped, and
    /// it stops accumulating while the output is saturated and the error would push it further into saturation.
    /// </summary>
    public sealed class PidController
    {
        public PidGains Gains;

        float integral;

        public PidController(PidGains gains)
        {
            Gains = gains;
        }

        /// <summary>The integral term in output units.</summary>
        public float Integral => integral;

        public void Reset() => integral = 0f;

        /// <param name="error">target - measurement.</param>
        /// <param name="errorRate">d(error)/dt, i.e. minus the measured rate when the target is fixed.</param>
        public float Update(float error, float errorRate, float deltaTime)
        {
            if (deltaTime <= 0f) return 0f;
            var g = Gains;
            float limit = Mathf.Max(g.OutputLimit, 0f);
            float unsaturated = g.Kp * error + integral + g.Kd * errorRate;
            bool pushingFurther = Mathf.Abs(unsaturated) >= limit && Mathf.Sign(error) == Mathf.Sign(unsaturated);
            if (!pushingFurther)
                integral = Mathf.Clamp(integral + g.Ki * error * deltaTime, -Mathf.Max(g.IntegralLimit, 0f), Mathf.Max(g.IntegralLimit, 0f));
            return Mathf.Clamp(g.Kp * error + integral + g.Kd * errorRate, -limit, limit);
        }
    }
}
