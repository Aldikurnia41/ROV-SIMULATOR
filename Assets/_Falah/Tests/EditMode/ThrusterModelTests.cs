using Falah.RovSim.Core;
using NUnit.Framework;
using UnityEngine;

namespace Falah.RovSim.Tests.EditMode
{
    public class ThrusterModelTests
    {
        static readonly string[] Rovs = { "tortuga", "teledyne" };

        static float[] Healthy(ThrusterModel m)
        {
            var e = new float[m.Count];
            for (int i = 0; i < e.Length; i++) e[i] = 1f;
            return e;
        }

        static Wrench Allocated(ThrusterModel m, Wrench w, float[] eff = null)
        {
            var t = new float[m.Count];
            m.Allocate(w, eff ?? Healthy(m), t);
            return m.Achieved(t);
        }

        static float[] Allocate(ThrusterModel m, Wrench w, float[] eff = null)
        {
            var t = new float[m.Count];
            m.Allocate(w, eff ?? Healthy(m), t);
            return t;
        }

        [TestCase("tortuga")]
        [TestCase("teledyne")]
        public void Surge_ProducesForceOnZOnly(string rov)
        {
            var m = new ThrusterModel(ThrusterLayouts.For(rov));
            var a = Allocated(m, new Wrench(new Vector3(0, 0, 40f), Vector3.zero));
            Assert.AreEqual(40f, a.Force.z, 0.01f);
            Assert.AreEqual(0f, a.Force.x, 0.01f);
            Assert.AreEqual(0f, a.Force.y, 0.01f);
            Assert.AreEqual(0f, a.Torque.magnitude, 0.01f);
        }

        [TestCase("tortuga")]
        [TestCase("teledyne")]
        public void Sway_ProducesForceOnXOnly(string rov)
        {
            var m = new ThrusterModel(ThrusterLayouts.For(rov));
            var a = Allocated(m, new Wrench(new Vector3(30f, 0, 0), Vector3.zero));
            Assert.AreEqual(30f, a.Force.x, 0.01f);
            Assert.AreEqual(0f, a.Force.z, 0.01f);
            Assert.AreEqual(0f, a.Force.y, 0.01f);
            Assert.AreEqual(0f, a.Torque.magnitude, 0.01f);
        }

        [TestCase("tortuga")]
        [TestCase("teledyne")]
        public void Heave_ProducesForceOnYOnly(string rov)
        {
            var m = new ThrusterModel(ThrusterLayouts.For(rov));
            var a = Allocated(m, new Wrench(new Vector3(0, 40f, 0), Vector3.zero));
            Assert.AreEqual(40f, a.Force.y, 0.01f);
            Assert.AreEqual(0f, new Vector2(a.Force.x, a.Force.z).magnitude, 0.01f);
            Assert.AreEqual(0f, a.Torque.magnitude, 0.01f);
        }

        [TestCase("tortuga")]
        [TestCase("teledyne")]
        public void Yaw_ProducesTorqueAboutYOnly(string rov)
        {
            var m = new ThrusterModel(ThrusterLayouts.For(rov));
            var a = Allocated(m, new Wrench(Vector3.zero, new Vector3(0, 5f, 0)));
            Assert.AreEqual(5f, a.Torque.y, 0.01f);
            Assert.AreEqual(0f, a.Force.magnitude, 0.01f);
            Assert.AreEqual(0f, new Vector2(a.Torque.x, a.Torque.z).magnitude, 0.01f);
        }

        [TestCase("tortuga")]
        [TestCase("teledyne")]
        public void Reverse_IsNegativeOfForward(string rov)
        {
            var m = new ThrusterModel(ThrusterLayouts.For(rov));
            var fwd = Allocate(m, new Wrench(new Vector3(0, 0, 20f), Vector3.zero));
            var back = Allocate(m, new Wrench(new Vector3(0, 0, -20f), Vector3.zero));
            for (int i = 0; i < fwd.Length; i++) Assert.AreEqual(-fwd[i], back[i], 1e-3f);
        }

        [TestCase("tortuga")]
        [TestCase("teledyne")]
        public void Saturation_StaysWithinLimits_AndKeepsDirection(string rov)
        {
            var m = new ThrusterModel(ThrusterLayouts.For(rov));
            // a combined demand far beyond the limits
            var demand = new Wrench(new Vector3(500f, 0, 1000f), new Vector3(0, 100f, 0));
            var t = Allocate(m, demand);
            for (int i = 0; i < t.Length; i++)
            {
                var d = m.Def(i);
                Assert.LessOrEqual(t[i], d.MaxForward + 1e-3f, d.Name);
                Assert.GreaterOrEqual(t[i], -d.MaxReverse - 1e-3f, d.Name);
            }
            var a = m.Achieved(t);
            // direction preserved: achieved wrench is parallel to the demand (cosine ~ 1)
            var got = new[] { a.Force.x, a.Force.y, a.Force.z, a.Torque.x, a.Torque.y, a.Torque.z };
            var want = new[] { 500f, 0f, 1000f, 0f, 100f, 0f };
            float dot = 0f, g2 = 0f, w2 = 0f;
            for (int i = 0; i < 6; i++) { dot += got[i] * want[i]; g2 += got[i] * got[i]; w2 += want[i] * want[i]; }
            Assert.Greater(dot / Mathf.Sqrt(g2 * w2), 0.9999f);
            Assert.Less(Mathf.Sqrt(g2), Mathf.Sqrt(w2), "the saturated wrench is smaller than the demand");
        }

        [Test]
        public void DeadThruster_ForceIsZero_AndOthersCompensate()
        {
            var m = new ThrusterModel(ThrusterLayouts.Teledyne());
            var eff = Healthy(m);
            eff[0] = 0f; // H1 dead
            var w = new Wrench(new Vector3(0, 0, 40f), Vector3.zero);
            var t = Allocate(m, w, eff);
            Assert.AreEqual(0f, t[0]);
            var a = m.Achieved(t);
            Assert.AreEqual(40f, a.Force.z, 0.05f, "the remaining horizontal thrusters still deliver the surge");
            Assert.AreEqual(0f, a.Force.x, 0.05f);
            Assert.AreEqual(0f, a.Torque.y, 0.05f);
        }

        [Test]
        public void DeadThruster_OnTortuga_LosesTheAxisOnlyThatThrusterServed()
        {
            var m = new ThrusterModel(ThrusterLayouts.Tortuga());
            var eff = Healthy(m);
            eff[3] = 0f; // the only vertical thruster
            var heave = Allocated(m, new Wrench(new Vector3(0, 40f, 0), Vector3.zero), eff);
            Assert.AreEqual(0f, heave.Force.y, 1e-3f);
            var surge = Allocated(m, new Wrench(new Vector3(0, 0, 40f), Vector3.zero), eff);
            Assert.AreEqual(40f, surge.Force.z, 0.05f);
        }

        [Test]
        public void PartialEfficiency_ScalesTheForceLimits()
        {
            var m = new ThrusterModel(ThrusterLayouts.Tortuga());
            var eff = Healthy(m);
            eff[3] = 0.5f;
            var t = Allocate(m, new Wrench(new Vector3(0, 1000f, 0), Vector3.zero), eff);
            Assert.AreEqual(0.5f * m.Def(3).MaxForward, t[3], 1e-3f);
        }

        [Test]
        public void Response_IsFirstOrder()
        {
            var defs = ThrusterLayouts.Tortuga();
            foreach (var d in defs) d.ResponseTime = 0.5f;
            var m = new ThrusterModel(defs);
            var w = new Wrench(new Vector3(0, 40f, 0), Vector3.zero);
            var eff = Healthy(m);

            m.Step(w, eff, 0.5f); // one time constant
            Assert.AreEqual(40f * (1f - Mathf.Exp(-1f)), m.Thrust[3], 0.01f);

            for (int i = 0; i < 40; i++) m.Step(w, eff, 0.5f);
            Assert.AreEqual(40f, m.Thrust[3], 0.01f, "converges to the commanded thrust");
        }

        [Test]
        public void Response_DoesNotOvershoot_WithLargeSteps()
        {
            var m = new ThrusterModel(ThrusterLayouts.Tortuga());
            var w = new Wrench(new Vector3(0, 40f, 0), Vector3.zero);
            m.Step(w, Healthy(m), 100f);
            Assert.LessOrEqual(m.Thrust[3], 40f + 1e-3f);
        }

        [Test]
        public void ZeroWrench_GivesZeroThrust_AndNoNaN()
        {
            foreach (var rov in Rovs)
            {
                var m = new ThrusterModel(ThrusterLayouts.For(rov));
                var t = Allocate(m, new Wrench(Vector3.zero, Vector3.zero));
                foreach (float v in t) { Assert.IsFalse(float.IsNaN(v)); Assert.AreEqual(0f, v, 1e-6f); }
            }
        }

        [Test]
        public void AllThrustersDead_GivesNoThrust_AndNoNaN()
        {
            var m = new ThrusterModel(ThrusterLayouts.Teledyne());
            var eff = new float[m.Count];
            var t = Allocate(m, new Wrench(new Vector3(0, 0, 40f), Vector3.zero), eff);
            foreach (float v in t) { Assert.IsFalse(float.IsNaN(v)); Assert.AreEqual(0f, v); }
        }

        [Test]
        public void Constructor_RejectsEmptyLayout()
        {
            Assert.Throws<System.ArgumentException>(() => new ThrusterModel(new ThrusterDef[0]));
        }

        [Test]
        public void FaultModel_Efficiency_FeedsTheAllocation()
        {
            var log = new SessionLog();
            log.ToggleDisturbance(DisturbanceKind.ThrusterDead); // catalog: thruster index 0 dead
            var m = new ThrusterModel(ThrusterLayouts.Teledyne());
            var eff = new float[m.Count];
            for (int i = 0; i < eff.Length; i++) eff[i] = ThrusterFaultModel.Efficiency(i, log);
            var t = Allocate(m, new Wrench(new Vector3(0, 0, 40f), Vector3.zero), eff);
            Assert.AreEqual(0f, t[0]);
            Assert.AreEqual(40f, m.Achieved(t).Force.z, 0.05f);
        }
    }
}
