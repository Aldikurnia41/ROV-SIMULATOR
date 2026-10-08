using System;
using UnityEngine;

namespace Falah.RovSim.Core
{
    [Flags]
    public enum DpMode
    {
        None = 0,
        Heading = 1,
        Depth = 2,
        Position = 4,
    }

    /// <summary>Vehicle state the DP controller needs. Heading follows the vehicle's front (see the telemetry adapter).</summary>
    public struct DpMeasurement
    {
        public Vector3 Position;
        public Vector3 Velocity;
        /// <summary>0..360, clockwise from +Z (north in the scene).</summary>
        public float HeadingDegrees;
        /// <summary>Degrees per second, clockwise positive.</summary>
        public float YawRateDegrees;
        /// <summary>Metres below the surface, positive down.</summary>
        public float Depth;
        /// <summary>Metres per second, positive down.</summary>
        public float DepthRate;
    }

    /// <summary>
    /// Station keeping: three independent loops (heading, depth, horizontal position) that return a wrench in the vehicle
    /// frame (X sway, Y heave up, Z surge forward, torque about Y clockwise). Targets are captured when a mode is engaged.
    /// Plain C#; the scene adapter feeds the measurement and hands the wrench to the thruster allocation.
    /// </summary>
    public sealed class DpController
    {
        readonly PidController heading;
        readonly PidController depth;
        readonly PidController surge;
        readonly PidController sway;

        float targetHeading;
        float targetDepth;
        Vector3 targetPosition;

        public DpController(PidGains headingGains, PidGains depthGains, PidGains positionGains)
        {
            heading = new PidController(headingGains);
            depth = new PidController(depthGains);
            surge = new PidController(positionGains);
            sway = new PidController(positionGains);
        }

        public DpMode Modes { get; private set; }

        public float TargetHeading => targetHeading;
        public float TargetDepth => targetDepth;
        public Vector3 TargetPosition => targetPosition;

        public void SetGains(PidGains headingGains, PidGains depthGains, PidGains positionGains)
        {
            heading.Gains = headingGains;
            depth.Gains = depthGains;
            surge.Gains = positionGains;
            sway.Gains = positionGains;
        }

        /// <summary>Engages or releases one mode; engaging captures the vehicle's current value as the target.</summary>
        public void SetMode(DpMode modes, bool on, DpMeasurement now)
        {
            foreach (DpMode single in new[] { DpMode.Heading, DpMode.Depth, DpMode.Position })
                if ((modes & single) != 0) SetSingle(single, on, now);
        }

        void SetSingle(DpMode mode, bool on, DpMeasurement now)
        {
            bool was = (Modes & mode) != 0;
            if (on == was) return;
            if (on)
            {
                Modes |= mode;
                if (mode == DpMode.Heading) { targetHeading = now.HeadingDegrees; heading.Reset(); }
                if (mode == DpMode.Depth) { targetDepth = now.Depth; depth.Reset(); }
                if (mode == DpMode.Position) { targetPosition = now.Position; surge.Reset(); sway.Reset(); }
            }
            else
            {
                Modes &= ~mode;
                if (mode == DpMode.Heading) heading.Reset();
                if (mode == DpMode.Depth) depth.Reset();
                if (mode == DpMode.Position) { surge.Reset(); sway.Reset(); }
            }
        }

        public void SetTargetHeading(float degrees) => targetHeading = TelemetryMath.NormalizeHeading(degrees);
        public void SetTargetDepth(float meters) => targetDepth = meters;
        public void SetTargetPosition(Vector3 position) => targetPosition = position;

        /// <summary>Wrench the engaged loops ask for; zero for loops that are off.</summary>
        public Wrench Update(DpMeasurement m, float deltaTime)
        {
            Vector3 force = Vector3.zero;
            float yawTorque = 0f;

            if ((Modes & DpMode.Heading) != 0)
            {
                float error = TelemetryMath.NormalizeSigned(targetHeading - m.HeadingDegrees);
                yawTorque = heading.Update(error, -m.YawRateDegrees, deltaTime);
            }

            if ((Modes & DpMode.Depth) != 0)
            {
                // deeper than the target (negative error) needs upward force
                float error = targetDepth - m.Depth;
                force.y = -depth.Update(error, -m.DepthRate, deltaTime);
            }

            if ((Modes & DpMode.Position) != 0)
            {
                float psi = m.HeadingDegrees * Mathf.Deg2Rad;
                Vector3 forward = new Vector3(Mathf.Sin(psi), 0f, Mathf.Cos(psi));
                Vector3 right = new Vector3(Mathf.Cos(psi), 0f, -Mathf.Sin(psi));
                Vector3 error = targetPosition - m.Position;
                error.y = 0f;
                Vector3 velocity = m.Velocity;
                force.z = surge.Update(Vector3.Dot(error, forward), -Vector3.Dot(velocity, forward), deltaTime);
                force.x = sway.Update(Vector3.Dot(error, right), -Vector3.Dot(velocity, right), deltaTime);
            }

            return new Wrench(force, new Vector3(0f, yawTorque, 0f));
        }

        /// <summary>Horizontal distance to the position target, metres.</summary>
        public float PositionError(Vector3 position)
        {
            Vector3 d = targetPosition - position;
            d.y = 0f;
            return d.magnitude;
        }
    }
}
