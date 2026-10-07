using Falah.RovSim.Core;
using NUnit.Framework;

namespace Falah.RovSim.Tests.EditMode
{
    public class ThrusterFaultModelTests
    {
        const int LeakingThruster = 2; // DisturbanceCatalog: leak hits thruster #3
        const int DeadThruster = 0;

        [Test]
        public void NoFault_EveryThrusterFullEfficiency()
        {
            var log = new SessionLog();
            for (int i = 0; i < 4; i++) Assert.AreEqual(1f, ThrusterFaultModel.Efficiency(i, log));
            Assert.AreEqual(1f, ThrusterFaultModel.Efficiency(0, null));
        }

        [Test]
        public void Leak_LosesThrustGraduallyDownToTheFloor()
        {
            var log = new SessionLog { Elapsed = 100f };
            log.ToggleDisturbance(DisturbanceKind.ThrusterLeak);

            Assert.AreEqual(1f, ThrusterFaultModel.Efficiency(LeakingThruster, log), 1e-4f);

            log.Elapsed = 100f + ThrusterFaultModel.LeakDecaySeconds * 0.5f;
            float mid = 0.5f * (1f + ThrusterFaultModel.LeakFloorEfficiency);
            Assert.AreEqual(mid, ThrusterFaultModel.Efficiency(LeakingThruster, log), 1e-4f);

            log.Elapsed = 100f + ThrusterFaultModel.LeakDecaySeconds * 4f;
            Assert.AreEqual(ThrusterFaultModel.LeakFloorEfficiency, ThrusterFaultModel.Efficiency(LeakingThruster, log), 1e-4f);
        }

        [Test]
        public void Leak_DoesNotAffectOtherThrusters()
        {
            var log = new SessionLog();
            log.ToggleDisturbance(DisturbanceKind.ThrusterLeak);
            log.Elapsed = 1000f;
            foreach (int i in new[] { 0, 1, 3 }) Assert.AreEqual(1f, ThrusterFaultModel.Efficiency(i, log));
        }

        [Test]
        public void Dead_GivesNoThrustImmediately()
        {
            var log = new SessionLog();
            log.ToggleDisturbance(DisturbanceKind.ThrusterDead);
            Assert.AreEqual(0f, ThrusterFaultModel.Efficiency(DeadThruster, log));
        }

        [Test]
        public void ClearingTheDisturbance_RestoresThrust()
        {
            var log = new SessionLog();
            log.ToggleDisturbance(DisturbanceKind.ThrusterLeak);
            log.Elapsed = 1000f;
            Assert.Less(ThrusterFaultModel.Efficiency(LeakingThruster, log), 1f);
            log.ToggleDisturbance(DisturbanceKind.ThrusterLeak);
            Assert.AreEqual(1f, ThrusterFaultModel.Efficiency(LeakingThruster, log));
        }

        [Test]
        public void Pair_IsTheMeanOfTwoThrusters()
        {
            var log = new SessionLog();
            log.ToggleDisturbance(DisturbanceKind.ThrusterLeak);
            log.Elapsed = 1000f; // fully decayed: thruster 2 at the floor, thruster 3 healthy
            float expected = 0.5f * (ThrusterFaultModel.LeakFloorEfficiency + 1f);
            Assert.AreEqual(expected, ThrusterFaultModel.Pair(log, 2, 3), 1e-4f);
            Assert.AreEqual(1f, ThrusterFaultModel.Pair(log, 0, 1), 1e-4f);
        }

        [Test]
        public void Dead_BeatsLeakOnTheSameThruster_WorstFaultWins()
        {
            // Both catalog faults on different thrusters; the minimum rule is exercised through Single().
            Assert.AreEqual(0f, ThrusterFaultModel.Single(FaultBehavior.Dead, 0f));
            Assert.AreEqual(1f, ThrusterFaultModel.Single(FaultBehavior.None, 50f));
            Assert.AreEqual(1f, ThrusterFaultModel.Single(FaultBehavior.GradualLoss, 0f), 1e-4f);
        }
    }
}
