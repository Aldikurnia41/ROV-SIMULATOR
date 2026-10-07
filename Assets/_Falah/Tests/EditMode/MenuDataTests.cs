using System.Linq;
using Falah.RovSim.Core;
using Falah.RovSim.UI;
using NUnit.Framework;

namespace Falah.RovSim.Tests.EditMode
{
    public class MenuDataTests
    {
        [Test]
        public void CatalogIds_AreUnique()
        {
            Assert.AreEqual(MenuCatalog.Rovs.Length, MenuCatalog.Rovs.Select(r => r.Id).Distinct().Count());
            Assert.AreEqual(MenuCatalog.Modes.Length, MenuCatalog.Modes.Select(m => m.Id).Distinct().Count());
            Assert.AreEqual(MenuCatalog.Scenarios.Length, MenuCatalog.Scenarios.Select(s => s.Id).Distinct().Count());
        }

        [Test]
        public void Prototype_OnlyTortugaAndTeledyneAreSelectable()
        {
            var ids = MenuCatalog.Rovs.Where(r => r.Selectable).Select(r => r.Id).OrderBy(i => i).ToArray();
            CollectionAssert.AreEqual(new[] { "teledyne", "tortuga" }, ids);
        }

        [Test]
        public void Defaults_PointToSelectableCatalogEntries()
        {
            var s = new SessionSetup();
            Assert.IsTrue(MenuCatalog.FindRov(s.RovId).Selectable);
            Assert.IsTrue(MenuCatalog.FindMode(s.Mode).Selectable);
            Assert.IsTrue(MenuCatalog.FindScenario(s.ScenarioId).Selectable);
        }

        [Test]
        public void FindRov_UnknownId_FallsBackToFirst()
        {
            Assert.AreEqual(MenuCatalog.Rovs[0].Id, MenuCatalog.FindRov("nope").Id);
        }

        [Test]
        public void ResetCurrent_RestoresDefaults()
        {
            var s = SessionSetup.Current;
            s.UserName = "x";
            s.RovId = "teledyne";
            s.GamepadCalibrated = true;
            SessionSetup.ResetCurrent();
            Assert.AreEqual(string.Empty, SessionSetup.Current.UserName);
            Assert.AreEqual(SessionSetup.DefaultRovId, SessionSetup.Current.RovId);
            Assert.IsFalse(SessionSetup.Current.GamepadCalibrated);
        }

        [TestCase(TimeOfDay.Day)]
        [TestCase(TimeOfDay.Dusk)]
        [TestCase(TimeOfDay.Night)]
        public void TimeOfDay_RoundTripsThroughId(TimeOfDay t)
        {
            Assert.AreEqual(t, ScenarioScreen.ToTimeOfDay(ScenarioScreen.ToId(t)));
        }

        [Test]
        public void LoginRole_MapsIds()
        {
            Assert.AreEqual(UserRole.Trainee, LoginScreen.ToRole(LoginScreen.TraineeId));
            Assert.AreEqual(UserRole.Instructor, LoginScreen.ToRole(LoginScreen.InstructorId));
            Assert.AreEqual(UserRole.Administrator, LoginScreen.ToRole(LoginScreen.AdministratorId));
            Assert.AreEqual(UserRole.Trainee, LoginScreen.ToRole("unknown"));
        }
    }
}
