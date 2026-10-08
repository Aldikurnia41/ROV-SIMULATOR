using System.Linq;
using Falah.RovSim.Core;
using NUnit.Framework;
using UnityEngine;

namespace Falah.RovSim.Tests.EditMode
{
    public class RovProfileTests
    {
        static RovProfile Make(string id = "tortuga") => RovProfile.CreateDefault(id);

        static bool HasError(RovProfile p, string fragment) =>
            RovProfileValidator.Validate(p).Any(i => i.Level == RovProfileValidator.Level.Error && i.Message.Contains(fragment));

        [TestCase("tortuga")]
        [TestCase("teledyne")]
        public void DefaultProfiles_HaveNoErrors_AndAreMarkedPlaceholder(string id)
        {
            var p = Make(id);
            Assert.AreEqual(DataStatus.Placeholder, p.Status);
            var issues = RovProfileValidator.Validate(p);
            Assert.IsFalse(issues.Any(i => i.Level == RovProfileValidator.Level.Error), string.Join("; ", issues.Select(i => i.ToString())));
            Assert.IsTrue(issues.Any(i => i.Level == RovProfileValidator.Level.Warning && i.Message.Contains("Placeholder")), "unconfirmed data must be flagged");
        }

        [Test]
        public void ConfirmedProfile_HasNoUnconfirmedWarning()
        {
            var p = Make();
            p.Status = DataStatus.Confirmed;
            Assert.IsEmpty(RovProfileValidator.Validate(p));
        }

        [Test]
        public void Profiles_Differ_BetweenTheTwoRovs()
        {
            var a = Make("tortuga");
            var b = Make("teledyne");
            Assert.AreNotEqual(a.Thrusters.Length, b.Thrusters.Length);
            Assert.AreNotEqual(a.Mass, b.Mass);
            Assert.AreNotEqual(a.Camera.FieldOfView, b.Camera.FieldOfView);
            Assert.AreNotEqual(a.MaxDepth, b.MaxDepth);
        }

        [Test]
        public void ThrusterWithoutDirection_IsAnError()
        {
            var p = Make();
            p.Thrusters[1].Direction = Vector3.zero;
            Assert.IsTrue(HasError(p, "Thruster 2"));
            Assert.IsTrue(HasError(p, "no direction"));
        }

        [Test]
        public void ZeroMass_ZeroVolume_AreErrors()
        {
            var p = Make();
            p.Mass = 0f;
            p.Volume = 0f;
            Assert.IsTrue(HasError(p, "Mass"));
            Assert.IsTrue(HasError(p, "Volume"));
        }

        [Test]
        public void NegativePidGain_IsAnError_PerLoop()
        {
            var p = Make();
            p.HeadingPid.Kp = -1f;
            p.DepthPid.Kd = -0.1f;
            p.PositionPid.Ki = -2f;
            Assert.IsTrue(HasError(p, "Heading PID has a negative gain"));
            Assert.IsTrue(HasError(p, "Depth PID has a negative gain"));
            Assert.IsTrue(HasError(p, "Position PID has a negative gain"));
        }

        [Test]
        public void IntegralWithoutLimit_AndMissingOutputLimit_AreErrors()
        {
            var p = Make();
            p.PositionPid.IntegralLimit = 0f;
            p.DepthPid.OutputLimit = 0f;
            Assert.IsTrue(HasError(p, "integral limit is zero"));
            Assert.IsTrue(HasError(p, "Depth PID needs an output limit"));
        }

        [Test]
        public void NoThrusters_BadThrusterLimits_BadResponseTime_AreErrors()
        {
            var p = Make();
            p.Thrusters = new ThrusterDef[0];
            Assert.IsTrue(HasError(p, "No thrusters"));

            p = Make();
            p.Thrusters[0].MaxForward = 0f;
            p.Thrusters[0].MaxReverse = 0f;
            p.Thrusters[1].ResponseTime = 0f;
            Assert.IsTrue(HasError(p, "no thrust in either direction"));
            Assert.IsTrue(HasError(p, "response time"));
        }

        [Test]
        public void CameraRanges_AreChecked()
        {
            var p = Make();
            p.Camera.FieldOfView = 400f;
            Assert.IsTrue(HasError(p, "field of view"));
            p = Make();
            p.Camera.MinTilt = 40f; p.Camera.MaxTilt = -40f;
            Assert.IsTrue(HasError(p, "MinTilt"));
            p = Make();
            p.Camera.DefaultTilt = 80f;
            Assert.IsTrue(HasError(p, "DefaultTilt"));
        }

        [Test]
        public void NegativeDrag_AndEmptyId_AreErrors()
        {
            var p = Make();
            p.LinearDrag = new Vector3(1f, -1f, 1f);
            p.Id = " ";
            Assert.IsTrue(HasError(p, "Drag"));
            Assert.IsTrue(HasError(p, "Id is empty"));
        }

        [Test]
        public void Registry_FallsBackToTheBuiltInPlaceholder_WhenNoAssetExists()
        {
            UnityEngine.TestTools.LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("No ROV profile asset for 'sample-rov-without-asset'"));
            var p = RovProfiles.Get("sample-rov-without-asset");
            Assert.IsNotNull(p);
            Assert.AreEqual("sample-rov-without-asset", p.Id);
            Assert.AreSame(p, RovProfiles.Get("sample-rov-without-asset"), "cached after the first lookup (no second warning)");
        }
    }
}
