using System;
using UnityEngine;

namespace Falah.RovSim.Core
{
    /// <summary>One thruster in the vehicle frame (X right, Y up, Z forward). Force limits in newtons.</summary>
    [Serializable]
    public sealed class ThrusterDef
    {
        public string Name;
        /// <summary>Where the force acts, relative to the centre of mass.</summary>
        public Vector3 Position;
        /// <summary>Direction of positive thrust; normalised by <see cref="ThrusterModel"/>.</summary>
        public Vector3 Direction;
        public float MaxForward;
        public float MaxReverse;
        /// <summary>First-order time constant of the thrust response, seconds.</summary>
        public float ResponseTime = 0.2f;
    }

    /// <summary>Force (N) and torque (N·m) the vehicle should receive, in the vehicle frame.</summary>
    public struct Wrench
    {
        public Vector3 Force;
        public Vector3 Torque;

        public Wrench(Vector3 force, Vector3 torque)
        {
            Force = force;
            Torque = torque;
        }
    }

    /// <summary>
    /// Allocates a 6-DOF wrench to thruster forces and simulates the thruster response. Plain C#, no scene objects.
    /// Allocation is the damped pseudo-inverse T = Bᵀ(BBᵀ + λI)⁻¹w, which also works for layouts that cannot produce
    /// every DOF (the missing ones simply stay unactuated). A thruster whose efficiency is ~0 is dropped from the
    /// allocation so the others compensate; efficiency below 1 scales its force limits. When any thruster would exceed its
    /// limit all thrusts are scaled down together, so the direction of the resulting wrench is preserved.
    /// </summary>
    public sealed class ThrusterModel
    {
        /// <summary>Efficiency at or below this removes the thruster from the allocation.</summary>
        public const float DeadEfficiency = 0.01f;

        const double Damping = 1e-6;

        readonly ThrusterDef[] defs;
        readonly Vector3[] directions;
        readonly float[] thrust;
        readonly double[,] columns; // 6 x N, unit thrust -> wrench

        public ThrusterModel(ThrusterDef[] defs)
        {
            if (defs == null || defs.Length == 0) throw new ArgumentException("at least one thruster is required", nameof(defs));
            this.defs = defs;
            directions = new Vector3[defs.Length];
            thrust = new float[defs.Length];
            columns = new double[6, defs.Length];
            for (int i = 0; i < defs.Length; i++)
            {
                directions[i] = defs[i].Direction.normalized;
                Vector3 torque = Vector3.Cross(defs[i].Position, directions[i]);
                columns[0, i] = directions[i].x; columns[1, i] = directions[i].y; columns[2, i] = directions[i].z;
                columns[3, i] = torque.x; columns[4, i] = torque.y; columns[5, i] = torque.z;
            }
        }

        public int Count => defs.Length;

        /// <summary>Current (lagged) thrust of each thruster, newtons along its direction.</summary>
        public float[] Thrust => thrust;

        public ThrusterDef Def(int index) => defs[index];

        public Vector3 DirectionOf(int index) => directions[index];

        /// <summary>Thrust each thruster should produce for the wrench, after fault handling and saturation. No lag.</summary>
        public void Allocate(Wrench wrench, float[] efficiency, float[] commanded)
        {
            int n = defs.Length;
            var active = new bool[n];
            for (int i = 0; i < n; i++)
            {
                active[i] = Eff(efficiency, i) > DeadEfficiency;
                commanded[i] = 0f;
            }

            double[] w = { wrench.Force.x, wrench.Force.y, wrench.Force.z, wrench.Torque.x, wrench.Torque.y, wrench.Torque.z };

            // M = B Bᵀ + λI over the active thrusters
            var m = new double[6, 6];
            for (int r = 0; r < 6; r++)
                for (int c = 0; c < 6; c++)
                {
                    double sum = r == c ? Damping : 0.0;
                    for (int i = 0; i < n; i++) if (active[i]) sum += columns[r, i] * columns[c, i];
                    m[r, c] = sum;
                }

            double[] y = Solve(m, w);
            if (y == null) return;

            for (int i = 0; i < n; i++)
            {
                if (!active[i]) continue;
                double t = 0.0;
                for (int r = 0; r < 6; r++) t += columns[r, i] * y[r];
                commanded[i] = (float)t;
            }

            // proportional saturation
            float worst = 1f;
            for (int i = 0; i < n; i++)
            {
                if (!active[i]) continue;
                float limit = (commanded[i] >= 0f ? defs[i].MaxForward : defs[i].MaxReverse) * Mathf.Clamp01(Eff(efficiency, i));
                if (limit <= 0f) { if (Mathf.Abs(commanded[i]) > 0f) worst = float.PositiveInfinity; continue; }
                worst = Mathf.Max(worst, Mathf.Abs(commanded[i]) / limit);
            }
            if (worst > 1f)
            {
                float k = float.IsInfinity(worst) ? 0f : 1f / worst;
                for (int i = 0; i < n; i++) commanded[i] *= k;
            }
        }

        /// <summary>Allocates the wrench and advances the thrust response by <paramref name="deltaTime"/>.</summary>
        public void Step(Wrench wrench, float[] efficiency, float deltaTime)
        {
            var target = new float[defs.Length];
            Allocate(wrench, efficiency, target);
            for (int i = 0; i < defs.Length; i++)
            {
                float tau = Mathf.Max(defs[i].ResponseTime, 1e-4f);
                float alpha = 1f - Mathf.Exp(-Mathf.Max(deltaTime, 0f) / tau);
                thrust[i] += (target[i] - thrust[i]) * alpha;
            }
        }

        /// <summary>Wrench produced by the given thrusts (defaults to the current thrust).</summary>
        public Wrench Achieved(float[] thrusts = null)
        {
            thrusts = thrusts ?? thrust;
            Vector3 force = Vector3.zero, torque = Vector3.zero;
            for (int i = 0; i < defs.Length; i++)
            {
                force += directions[i] * thrusts[i];
                torque += Vector3.Cross(defs[i].Position, directions[i]) * thrusts[i];
            }
            return new Wrench(force, torque);
        }

        public void Reset()
        {
            Array.Clear(thrust, 0, thrust.Length);
        }

        static float Eff(float[] efficiency, int i) => efficiency == null || i >= efficiency.Length ? 1f : efficiency[i];

        /// <summary>Solves A x = b by Gaussian elimination with partial pivoting; null when singular.</summary>
        static double[] Solve(double[,] a, double[] b)
        {
            int n = b.Length;
            var m = (double[,])a.Clone();
            var x = (double[])b.Clone();
            for (int col = 0; col < n; col++)
            {
                int pivot = col;
                for (int r = col + 1; r < n; r++) if (Math.Abs(m[r, col]) > Math.Abs(m[pivot, col])) pivot = r;
                if (Math.Abs(m[pivot, col]) < 1e-12) return null;
                if (pivot != col)
                {
                    for (int c = 0; c < n; c++) { double tmp = m[col, c]; m[col, c] = m[pivot, c]; m[pivot, c] = tmp; }
                    double t = x[col]; x[col] = x[pivot]; x[pivot] = t;
                }
                for (int r = col + 1; r < n; r++)
                {
                    double f = m[r, col] / m[col, col];
                    for (int c = col; c < n; c++) m[r, c] -= f * m[col, c];
                    x[r] -= f * x[col];
                }
            }
            for (int r = n - 1; r >= 0; r--)
            {
                double sum = x[r];
                for (int c = r + 1; c < n; c++) sum -= m[r, c] * x[c];
                x[r] = sum / m[r, r];
            }
            return x;
        }
    }

    /// <summary>
    /// Thruster layouts of the prototype ROVs. PLACEHOLDER geometry and limits until RovProfile (T0.4) exists and
    /// Pushidrosal confirms the real vehicles; they only need to be physically sensible, not accurate.
    /// </summary>
    public static class ThrusterLayouts
    {
        const float Force = 60f;   // PLACEHOLDER newtons
        const float Reverse = 50f; // PLACEHOLDER newtons

        static ThrusterDef T(string name, Vector3 position, Vector3 direction) =>
            new ThrusterDef { Name = name, Position = position, Direction = direction, MaxForward = Force, MaxReverse = Reverse, ResponseTime = 0.25f };

        /// <summary>3 horizontal (two rear vectored, one lateral) + 1 vertical. Surge, sway, yaw and heave are independent.</summary>
        public static ThrusterDef[] Tortuga() => new[]
        {
            T("T1 rear left", new Vector3(-0.2f, 0f, -0.3f), new Vector3(0.7071f, 0f, 0.7071f)),
            T("T2 rear right", new Vector3(0.2f, 0f, -0.3f), new Vector3(-0.7071f, 0f, 0.7071f)),
            T("T3 front lateral", new Vector3(0f, 0f, 0.3f), new Vector3(1f, 0f, 0f)),
            T("T4 vertical", new Vector3(0f, 0f, 0f), new Vector3(0f, 1f, 0f)),
        };

        /// <summary>4 vectored horizontal (X layout) + 2 vertical, as in the SPEC "4 vektor + 2".</summary>
        public static ThrusterDef[] Teledyne() => new[]
        {
            T("H1 front left", new Vector3(-0.2f, 0f, 0.25f), new Vector3(0.7071f, 0f, 0.7071f)),
            T("H2 front right", new Vector3(0.2f, 0f, 0.25f), new Vector3(-0.7071f, 0f, 0.7071f)),
            T("H3 rear left", new Vector3(-0.2f, 0f, -0.25f), new Vector3(-0.7071f, 0f, 0.7071f)),
            T("H4 rear right", new Vector3(0.2f, 0f, -0.25f), new Vector3(0.7071f, 0f, 0.7071f)),
            T("V1 left", new Vector3(-0.15f, 0f, 0f), new Vector3(0f, 1f, 0f)),
            T("V2 right", new Vector3(0.15f, 0f, 0f), new Vector3(0f, 1f, 0f)),
        };

        public static ThrusterDef[] For(string rovId) => rovId == "teledyne" ? Teledyne() : Tortuga();
    }
}
