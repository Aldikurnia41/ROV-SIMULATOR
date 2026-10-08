using System.Collections.Generic;
using Falah.RovSim.Core;
using Falah.RovSim.Scenario;
using NUnit.Framework;
using UnityEngine;

namespace Falah.RovSim.Tests.EditMode
{
    public class BlackBoxScenarioTests
    {
        sealed class Targets : ITargetLocator
        {
            public Vector3 Box = new Vector3(0, -60, 0);
            public bool TryGetPosition(string name, out Vector3 p) { p = Box; return name == ScenarioLibrary.BlackBoxTarget; }
        }

        static VehicleState State(Vector3 pos, float depth, float lamp, bool mark = false) =>
            new VehicleState { Position = pos, Rotation = Quaternion.LookRotation(Vector3.down), Depth = depth, LampLevel = lamp, MarkPressed = mark };

        static ScenarioRunner Runner(out SessionLog log)
        {
            log = new SessionLog();
            return new ScenarioRunner(ScenarioLibrary.BlackBox(), new Targets());
        }

        [Test]
        public void Catalog_BlackBoxIsSelectable_AndHasLibraryData()
        {
            Assert.IsTrue(MenuCatalog.FindScenario("black-box").Selectable);
            var def = ScenarioLibrary.Find("black-box");
            Assert.AreEqual(4, def.Objectives.Length);
            Assert.AreEqual(1, def.Targets.Length);
        }

        [Test]
        public void Identify_NeedsLamp_WhenDeeperThanLimit()
        {
            var runner = Runner(out var log);
            runner.Tick(0.1f, State(new Vector3(0, -55, 0), 55f, 0f), log);   // reach-area done
            Assert.AreEqual(1, runner.ActiveIndex);
            for (int i = 0; i < 20; i++) runner.Tick(0.5f, State(new Vector3(0, -55, 0), 55f, 0f), log);
            Assert.AreEqual(1, runner.ActiveIndex, "dark: no identification");
            for (int i = 0; i < 10; i++) runner.Tick(0.5f, State(new Vector3(0, -55, 0), 55f, 0.7f), log);
            Assert.AreEqual(2, runner.ActiveIndex, "lamp on: identified");
        }

        [Test]
        public void Identify_WithoutLamp_WorksInSunlitWater()
        {
            var targets = new Targets { Box = new Vector3(0, -20, 0) };
            var log = new SessionLog();
            var runner = new ScenarioRunner(ScenarioLibrary.BlackBox(), targets);
            runner.Tick(0.1f, State(new Vector3(0, -15, 0), 15f, 0f), log);
            for (int i = 0; i < 10; i++) runner.Tick(0.5f, State(new Vector3(0, -15, 0), 15f, 0f), log);
            Assert.AreEqual(2, runner.ActiveIndex);
        }

        [Test]
        public void Mark_RequiresKeyPressNearTarget_ThenSurfaceFinishesScenario()
        {
            var runner = Runner(out var log);
            runner.Tick(0.1f, State(new Vector3(0, -55, 0), 55f, 0.7f), log);
            for (int i = 0; i < 10; i++) runner.Tick(0.5f, State(new Vector3(0, -55, 0), 55f, 0.7f), log);
            Assert.AreEqual(2, runner.ActiveIndex);

            runner.Tick(0.1f, State(new Vector3(0, -55, 0), 55f, 0.7f, mark: false), log);
            Assert.AreEqual(2, runner.ActiveIndex, "no key, no mark");
            runner.Tick(0.1f, State(new Vector3(50, -55, 0), 55f, 0.7f, mark: true), log);
            Assert.AreEqual(2, runner.ActiveIndex, "too far");
            runner.Tick(0.1f, State(new Vector3(0, -55, 0), 55f, 0.7f, mark: true), log);
            Assert.AreEqual(3, runner.ActiveIndex);

            runner.Tick(0.1f, State(new Vector3(0, -30, 0), 30f, 0.7f), log);
            Assert.IsFalse(runner.Finished, "still deep");
            runner.Tick(0.1f, State(new Vector3(0, -3, 0), 3f, 0.7f), log);
            Assert.IsTrue(runner.Finished);
            Assert.AreEqual(4, runner.DoneCount);
        }

        [Test]
        public void Lamp_LevelFollowsToggleBrightnessAndFailure()
        {
            var lamp = new LampState();
            Assert.AreEqual(0.7f, lamp.Level, 1e-5f);
            lamp.Toggle();
            Assert.AreEqual(0f, lamp.Level);
            lamp.Toggle();
            lamp.Step(1); lamp.Step(1); lamp.Step(1); lamp.Step(1);
            Assert.AreEqual(1f, lamp.Brightness, 1e-5f, "clamped at full");
            for (int i = 0; i < 20; i++) lamp.Step(-1);
            Assert.AreEqual(LampState.MinBrightness, lamp.Brightness, 1e-5f);
            lamp.Failed = true;
            Assert.AreEqual(0f, lamp.Level, "failed lamp gives no light even when on");
        }

        [Test]
        public void SeabedContact_FiresOncePerGrounding_AndRearmsAfterLiftOff()
        {
            var c = new SeabedContact();
            Assert.IsFalse(c.Update(float.NaN, 2f));
            Assert.IsFalse(c.Update(5f, 2f));
            Assert.IsTrue(c.Update(0.3f, 1f));
            Assert.IsFalse(c.Update(0.2f, 1f), "same grounding");
            Assert.IsFalse(c.Update(1.0f, 1f), "not cleared yet");
            Assert.IsFalse(c.Update(3f, 1f));
            Assert.IsTrue(c.Update(0.4f, 1f), "re-armed");
            Assert.IsFalse(new SeabedContact().Update(0.2f, 0.05f), "resting contact is not an impact");
        }
    }
}
