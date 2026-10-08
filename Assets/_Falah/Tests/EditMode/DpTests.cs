using Falah.RovSim.Core;
using NUnit.Framework;
using UnityEngine;

namespace Falah.RovSim.Tests.EditMode
{
    public class DpTests
    {
        static PidGains P(float kp, float ki = 0f, float kd = 0f, float il = 100f, float ol = 100f) => new PidGains(kp, ki, kd, il, ol);

        // ---------- PID ----------

        [Test]
        public void Pid_Proportional_IsKpTimesError_AndClampedToTheOutputLimit()
        {
            var pid = new PidController(P(2f, ol: 5f));
            Assert.AreEqual(4f, pid.Update(2f, 0f, 0.1f), 1e-5f);
            Assert.AreEqual(5f, pid.Update(100f, 0f, 0.1f), 1e-5f);
            Assert.AreEqual(-5f, pid.Update(-100f, 0f, 0.1f), 1e-5f);
        }

        [Test]
        public void Pid_Derivative_UsesTheSuppliedErrorRate()
        {
            var pid = new PidController(P(0f, 0f, 3f));
            Assert.AreEqual(-6f, pid.Update(0f, -2f, 0.1f), 1e-5f);
        }

        [Test]
        public void Pid_Integral_AccumulatesAndIsClamped()
        {
            var pid = new PidController(P(0f, 1f, 0f, il: 0.5f, ol: 10f));
            for (int i = 0; i < 100; i++) pid.Update(1f, 0f, 0.1f);
            Assert.AreEqual(0.5f, pid.Integral, 1e-5f);
        }

        [Test]
        public void Pid_AntiWindup_StopsIntegratingWhileSaturated_SoItRecoversQuickly()
        {
            // output saturates at 1 for a long time with a big error; a naive integral would be huge by now
            var pid = new PidController(P(1f, 5f, 0f, il: 1000f, ol: 1f));
            for (int i = 0; i < 1000; i++) pid.Update(10f, 0f, 0.05f);
            Assert.Less(Mathf.Abs(pid.Integral), 1.5f, "integral must not wind up while the output is pinned");

            // the error reverses: the output must leave saturation within a few updates, not after unwinding a huge integral
            float out1 = 0f;
            for (int i = 0; i < 10; i++) out1 = pid.Update(-10f, 0f, 0.05f);
            Assert.AreEqual(-1f, out1, 1e-5f);
        }

        [Test]
        public void Pid_ZeroOrNegativeDt_ReturnsZero()
        {
            var pid = new PidController(P(1f, 1f));
            Assert.AreEqual(0f, pid.Update(5f, 0f, 0f));
            Assert.AreEqual(0f, pid.Integral);
        }

        [Test]
        public void Pid_Reset_ClearsTheIntegral()
        {
            var pid = new PidController(P(0f, 1f));
            pid.Update(1f, 0f, 1f);
            pid.Reset();
            Assert.AreEqual(0f, pid.Integral);
        }

        // ---------- step response (offline plant) ----------

        [Test]
        public void StepResponse_DefaultPositionGains_SettleWithoutBigOvershoot()
        {
            var r = DpStepSimulator.Run(DpTuning.Defaults("tortuga").Position, DpPlant.SceneRov(), 5f, 30f);
            Assert.GreaterOrEqual(r.SettleTime, 0f, "must settle");
            Assert.Less(r.SettleTime, 12f, "limited by the 200 N output: a 5 m step takes ~9 s on this plant");
            Assert.Less(r.Overshoot, 0.1f);
            Assert.Less(Mathf.Abs(r.FinalError), 0.1f, "within 2% of the 5 m step after 30 s (the integral tail is slow)");
        }

        [Test]
        public void StepResponse_ConstantCurrent_IsRejectedByTheIntegralTerm()
        {
            var plant = DpPlant.SceneRov();
            plant.Disturbance = 60f; // steady push
            var withI = DpStepSimulator.Run(DpTuning.Defaults("tortuga").Position, plant, 0f, 60f);
            Assert.Less(Mathf.Abs(withI.Positions[withI.Positions.Length - 1]), 0.05f, "ends back on the target");

            var gains = DpTuning.Defaults("tortuga").Position;
            gains.Ki = 0f;
            var noI = DpStepSimulator.Run(gains, plant, 0f, 60f);
            Assert.Greater(Mathf.Abs(noI.Positions[noI.Positions.Length - 1]), 0.3f, "P-only keeps a steady-state offset (60 N / 150 N/m = 0.4 m)");
        }

        [Test]
        public void StepResponse_DoesNotProduceNaN_WithSaturatingGains()
        {
            var r = DpStepSimulator.Run(P(1000f, 100f, 0f, 50f, 20f), DpPlant.SceneRov(), 50f, 20f);
            foreach (float x in r.Positions) Assert.IsFalse(float.IsNaN(x) || float.IsInfinity(x));
        }

        // ---------- DpController ----------

        static DpMeasurement At(Vector3 pos, float heading = 0f, float depth = 0f) =>
            new DpMeasurement { Position = pos, HeadingDegrees = heading, Depth = depth };

        static DpController NewController()
        {
            var d = DpTuning.Defaults("tortuga");
            return new DpController(d.Heading, d.Depth, d.Position);
        }

        [Test]
        public void NoModes_ReturnsZeroWrench()
        {
            var w = NewController().Update(At(Vector3.one, 90f, 5f), 0.02f);
            Assert.AreEqual(0f, w.Force.magnitude);
            Assert.AreEqual(0f, w.Torque.magnitude);
        }

        [Test]
        public void Engaging_CapturesTheCurrentValueAsTarget()
        {
            var c = NewController();
            var now = At(new Vector3(10, -30, 20), 123f, 30f);
            c.SetMode(DpMode.Heading | DpMode.Depth | DpMode.Position, true, now);
            Assert.AreEqual(123f, c.TargetHeading);
            Assert.AreEqual(30f, c.TargetDepth);
            Assert.AreEqual(new Vector3(10, -30, 20), c.TargetPosition);
            var w = c.Update(now, 0.02f);
            Assert.AreEqual(0f, w.Force.magnitude, 1e-4f, "on target: no demand");
            Assert.AreEqual(0f, w.Torque.magnitude, 1e-4f);
        }

        [Test]
        public void Heading_TakesTheShortWayAround_Zero360()
        {
            var c = NewController();
            c.SetMode(DpMode.Heading, true, At(Vector3.zero, 350f));
            c.SetTargetHeading(10f);
            var w = c.Update(At(Vector3.zero, 350f), 0.02f);
            Assert.Greater(w.Torque.y, 0f, "20 degrees clockwise is shorter than 340 anticlockwise");
            c.SetTargetHeading(340f);
            w = c.Update(At(Vector3.zero, 10f), 0.02f);
            Assert.Less(w.Torque.y, 0f);
        }

        [Test]
        public void Depth_TooDeep_PushesUp_TooShallow_PushesDown()
        {
            var c = NewController();
            c.SetMode(DpMode.Depth, true, At(Vector3.zero, 0f, 30f));
            Assert.Greater(c.Update(At(Vector3.zero, 0f, 35f), 0.02f).Force.y, 0f);
            c.SetMode(DpMode.Depth, false, At(Vector3.zero, 0f, 30f));
            c.SetMode(DpMode.Depth, true, At(Vector3.zero, 0f, 30f));
            Assert.Less(c.Update(At(Vector3.zero, 0f, 25f), 0.02f).Force.y, 0f);
        }

        [Test]
        public void Position_ErrorIsRotatedIntoTheVehicleFrame()
        {
            var c = NewController();
            c.SetMode(DpMode.Position, true, At(Vector3.zero, 0f));
            // heading 0: target 5 m ahead (+Z) and 3 m to the right (+X)
            c.SetTargetPosition(new Vector3(3, 0, 5));
            var w = c.Update(At(Vector3.zero, 0f), 0.02f);
            Assert.Greater(w.Force.z, 0f);
            Assert.Greater(w.Force.x, 0f);

            // heading 90 (facing +X): the same world target is 3 m ahead and 5 m to the LEFT
            var c2 = NewController();
            c2.SetMode(DpMode.Position, true, At(Vector3.zero, 90f));
            c2.SetTargetPosition(new Vector3(3, 0, 5));
            var w2 = c2.Update(At(Vector3.zero, 90f), 0.02f);
            Assert.Greater(w2.Force.z, 0f);
            Assert.Less(w2.Force.x, 0f);
        }

        [Test]
        public void Position_IgnoresVerticalError()
        {
            var c = NewController();
            c.SetMode(DpMode.Position, true, At(Vector3.zero));
            c.SetTargetPosition(new Vector3(0, 50, 0));
            var w = c.Update(At(Vector3.zero), 0.02f);
            Assert.AreEqual(0f, w.Force.magnitude, 1e-4f);
        }

        [Test]
        public void ReleasingAMode_StopsItsOutput()
        {
            var c = NewController();
            c.SetMode(DpMode.Position, true, At(Vector3.zero));
            c.SetTargetPosition(new Vector3(0, 0, 5));
            Assert.Greater(c.Update(At(Vector3.zero), 0.02f).Force.z, 0f);
            c.SetMode(DpMode.Position, false, At(Vector3.zero));
            Assert.AreEqual(0f, c.Update(At(Vector3.zero), 0.02f).Force.magnitude);
            Assert.AreEqual(DpMode.None, c.Modes);
        }

        [Test]
        public void DefaultProfileGains_AreTheDpDefaults()
        {
            var p = RovProfile.CreateDefault("tortuga");
            var d = DpTuning.Defaults("tortuga");
            Assert.AreEqual(d.Position.Kp, p.PositionPid.Kp);
            Assert.AreEqual(d.Heading.Kd, p.HeadingPid.Kd);
            Assert.AreEqual(d.Depth.OutputLimit, p.DepthPid.OutputLimit);
        }
    }
}
