using System.Collections.Generic;
using System.Linq;
using Falah.RovSim.Core;
using Falah.RovSim.Scenario;
using NUnit.Framework;
using UnityEngine;

namespace Falah.RovSim.Tests.EditMode
{
    public class ScenarioRunnerTests
    {
        sealed class FakeTargets : ITargetLocator
        {
            public readonly Dictionary<string, Vector3> Positions = new Dictionary<string, Vector3>();
            public bool TryGetPosition(string name, out Vector3 position) => Positions.TryGetValue(name, out position);
        }

        static ObjectiveDef Reach(string target, float radius = 5f) =>
            new ObjectiveDef { Id = "reach", Type = ObjectiveType.ReachZone, TargetName = target, Radius = radius, Description = "Menuju " + target };

        static ObjectiveDef Hold(string target, float radius, float seconds) =>
            new ObjectiveDef { Id = "hold", Type = ObjectiveType.HoldPosition, TargetName = target, Radius = radius, HoldSeconds = seconds, Description = "Tahan " + target };

        static ObjectiveDef Identify(string target, float radius, float seconds, float halfAngle = 25f) =>
            new ObjectiveDef { Id = "id", Type = ObjectiveType.Identify, TargetName = target, Radius = radius, HoldSeconds = seconds, ViewHalfAngleDegrees = halfAngle, Description = "Kenali " + target };

        static ScenarioDef Scenario(params ObjectiveDef[] objectives) => new ScenarioDef { Id = "t", Name = "t", Objectives = objectives };

        static VehicleState At(Vector3 position, Quaternion? rotation = null) =>
            new VehicleState { Position = position, Rotation = rotation ?? Quaternion.identity };

        static void TickFor(ScenarioRunner runner, SessionLog log, VehicleState v, float seconds, float step = 0.5f)
        {
            for (float t = 0f; t < seconds - 1e-4f; t += step) runner.Tick(step, v, log);
        }

        [Test]
        public void ReachZone_CompletesInsideRadius_AndLogsEvent()
        {
            var targets = new FakeTargets();
            targets.Positions["a"] = new Vector3(10, 0, 0);
            var log = new SessionLog();
            var runner = new ScenarioRunner(Scenario(Reach("a", 5f)), targets);

            runner.Tick(0.1f, At(Vector3.zero), log);
            Assert.AreEqual(ObjectiveStatus.Active, runner.Status[0]);

            runner.Tick(0.1f, At(new Vector3(7, 0, 0)), log);
            Assert.AreEqual(ObjectiveStatus.Done, runner.Status[0]);
            Assert.IsTrue(runner.Finished);
            Assert.AreEqual(SimEventType.ObjectiveDone, log.Events[0].Type);
            Assert.AreEqual(EventSeverity.Success, log.Events[0].Severity);
            Assert.AreEqual(SimEventType.ScenarioComplete, log.Events[1].Type);
        }

        [Test]
        public void Objectives_AreEvaluatedInOrder()
        {
            var targets = new FakeTargets();
            targets.Positions["far"] = new Vector3(100, 0, 0);
            targets.Positions["near"] = Vector3.zero; // the vehicle is already here
            var log = new SessionLog();
            var runner = new ScenarioRunner(Scenario(Reach("far"), Reach("near")), targets);

            TickFor(runner, log, At(Vector3.zero), 5f);

            Assert.AreEqual(0, runner.ActiveIndex);
            Assert.AreEqual(ObjectiveStatus.Active, runner.Status[0]);
            Assert.AreEqual(ObjectiveStatus.Pending, runner.Status[1]);

            TickFor(runner, log, At(new Vector3(100, 0, 0)), 0.5f);
            Assert.AreEqual(1, runner.ActiveIndex, "second objective becomes active only after the first");
            runner.Tick(0.1f, At(Vector3.zero), log);
            Assert.IsTrue(runner.Finished);
            Assert.AreEqual(2, runner.DoneCount);
        }

        [Test]
        public void HoldPosition_NeedsUninterruptedTime()
        {
            var targets = new FakeTargets();
            targets.Positions["a"] = Vector3.zero;
            var log = new SessionLog();
            var runner = new ScenarioRunner(Scenario(Hold("a", 5f, 10f)), targets);

            TickFor(runner, log, At(Vector3.zero), 6f);
            Assert.IsFalse(runner.Finished);
            Assert.AreEqual(0.6f, runner.Progress, 1e-3f);

            runner.Tick(0.5f, At(new Vector3(20, 0, 0)), log); // drifts out: timer restarts
            Assert.AreEqual(0f, runner.Progress, 1e-4f);

            TickFor(runner, log, At(Vector3.zero), 9.5f);
            Assert.IsFalse(runner.Finished, "9.5 s since returning is still short of 10 s");

            runner.Tick(0.5f, At(Vector3.zero), log);
            Assert.IsTrue(runner.Finished);
            Assert.AreEqual(ObjectiveStatus.Done, runner.Status[0]);
        }

        [Test]
        public void Identify_NeedsTargetInViewWithinRange()
        {
            var targets = new FakeTargets();
            targets.Positions["box"] = new Vector3(0, 0, 8); // straight ahead (+Z)
            var log = new SessionLog();
            var runner = new ScenarioRunner(Scenario(Identify("box", 10f, 2f, 25f)), targets);

            TickFor(runner, log, At(Vector3.zero, Quaternion.Euler(0, 180f, 0)), 5f); // facing away
            Assert.IsFalse(runner.Finished);

            TickFor(runner, log, At(Vector3.zero, Quaternion.Euler(0, 40f, 0)), 5f); // 40 deg off the axis
            Assert.IsFalse(runner.Finished);

            TickFor(runner, log, At(new Vector3(0, 0, -20), Quaternion.identity), 5f); // looking at it but 28 m away
            Assert.IsFalse(runner.Finished);

            TickFor(runner, log, At(Vector3.zero, Quaternion.Euler(0, 10f, 0)), 2.5f);
            Assert.IsTrue(runner.Finished);
        }

        [Test]
        public void TimeLimit_FailsTheObjective_AndMovesOn()
        {
            var targets = new FakeTargets();
            targets.Positions["a"] = new Vector3(100, 0, 0);
            targets.Positions["b"] = Vector3.zero;
            var limited = Reach("a");
            limited.TimeLimitSeconds = 5f;
            var log = new SessionLog();
            var runner = new ScenarioRunner(Scenario(limited, Reach("b")), targets);

            TickFor(runner, log, At(Vector3.zero), 5.5f);

            Assert.AreEqual(ObjectiveStatus.Failed, runner.Status[0]);
            Assert.IsTrue(runner.Finished, "the second objective is satisfied where the vehicle stands");
            Assert.AreEqual(SimEventType.ObjectiveFailed, log.Events.First(e => e.Type == SimEventType.ObjectiveFailed).Type);
            Assert.AreEqual(1, runner.DoneCount);
            Assert.AreEqual(1, log.ObjectivesDone);
            Assert.AreEqual(2, log.ObjectivesTotal);
            Assert.AreEqual(EventSeverity.Warning, log.Events.Last().Severity, "partial completion is reported as a warning");
        }

        [Test]
        public void MissingTarget_IsReportedOnce_AndNothingCompletes()
        {
            var log = new SessionLog();
            var runner = new ScenarioRunner(Scenario(Reach("ghost")), new FakeTargets());

            TickFor(runner, log, At(Vector3.zero), 10f);

            Assert.IsFalse(runner.Finished);
            Assert.AreEqual(1, log.Events.Count(e => e.Severity == EventSeverity.Warning));
        }

        [Test]
        public void FullSonarContactScenario_RunsInOrder()
        {
            var targets = new FakeTargets();
            targets.Positions[ScenarioLibrary.SonarContactTarget] = new Vector3(0, -200, 300);
            var log = new SessionLog();
            var runner = new ScenarioRunner(ScenarioLibrary.SonarContact(), targets);
            var contact = targets.Positions[ScenarioLibrary.SonarContactTarget];

            // 1. dive and approach
            TickFor(runner, log, At(Vector3.zero), 5f);
            Assert.AreEqual(0, runner.ActiveIndex);
            TickFor(runner, log, At(contact + new Vector3(0, 0, -30)), 0.5f);
            Assert.AreEqual(1, runner.ActiveIndex);

            // 2. hold station
            TickFor(runner, log, At(contact + new Vector3(0, 0, -8)), 10f);
            Assert.AreEqual(2, runner.ActiveIndex);

            // 3. identify: face the contact from 6 m
            var pose = At(contact + new Vector3(0, 0, -6), Quaternion.identity);
            TickFor(runner, log, pose, 3.5f);

            Assert.IsTrue(runner.Finished);
            Assert.AreEqual(3, runner.DoneCount);
            var types = log.Events.Select(e => e.Type).ToArray();
            CollectionAssert.AreEqual(
                new[] { SimEventType.ObjectiveDone, SimEventType.ObjectiveDone, SimEventType.ObjectiveDone, SimEventType.ScenarioComplete }, types);
            Assert.AreEqual("Semua objektif selesai", log.Events.Last().Message);
            Assert.AreEqual("3/3 selesai", log.ObjectiveSummary);
        }

        [Test]
        public void Summary_ShowsActiveObjectiveAndHoldProgress()
        {
            var targets = new FakeTargets();
            targets.Positions["a"] = Vector3.zero;
            var log = new SessionLog();
            var runner = new ScenarioRunner(Scenario(Hold("a", 5f, 10f)), targets);

            TickFor(runner, log, At(Vector3.zero), 5f);
            StringAssert.StartsWith("0/1", log.ObjectiveSummary);
            StringAssert.Contains("Tahan a", log.ObjectiveSummary);
            StringAssert.Contains("50%", log.ObjectiveSummary);
        }

        [Test]
        public void Library_HasTheSonarContactScenario()
        {
            var def = ScenarioLibrary.Find(ScenarioLibrary.SonarContactId);
            Assert.IsNotNull(def);
            CollectionAssert.AreEqual(
                new[] { ObjectiveType.ReachZone, ObjectiveType.HoldPosition, ObjectiveType.Identify }, def.Objectives.Select(o => o.Type).ToArray());
            Assert.IsNull(ScenarioLibrary.Find("tidak-ada"));
            Assert.AreEqual(ScenarioLibrary.SonarContactId, SessionSetup.DefaultScenarioId);
        }

        [Test]
        public void EmptyScenario_IsImmediatelyFinished()
        {
            var runner = new ScenarioRunner(Scenario(), new FakeTargets());
            Assert.IsTrue(runner.Finished);
        }
    }
}
