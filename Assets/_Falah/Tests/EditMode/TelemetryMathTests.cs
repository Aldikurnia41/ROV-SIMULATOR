using Falah.RovSim.Core;
using NUnit.Framework;
using UnityEngine;

namespace Falah.RovSim.Tests.EditMode
{
    public class TelemetryMathTests
    {
        [TestCase(0f, 0f)]
        [TestCase(370f, 10f)]
        [TestCase(-10f, 350f)]
        [TestCase(720f, 0f)]
        public void NormalizeHeading_WrapsInto0To360(float input, float expected)
        {
            Assert.AreEqual(expected, TelemetryMath.NormalizeHeading(input), 1e-4f);
        }

        [TestCase(190f, -170f)]
        [TestCase(-190f, 170f)]
        [TestCase(45f, 45f)]
        public void NormalizeSigned_WrapsInto180(float input, float expected)
        {
            Assert.AreEqual(expected, TelemetryMath.NormalizeSigned(input), 1e-4f);
        }

        [Test]
        public void FromPose_DepthIsBelowSeaLevel()
        {
            var s = TelemetryMath.FromPose(0f, new Vector3(0f, -31.8f, 0f), Quaternion.identity, 0f, float.NaN);
            Assert.AreEqual(31.8f, s.DepthMeters, 1e-4f);
            Assert.IsTrue(float.IsNaN(s.AltitudeMeters));
        }

        [Test]
        public void FromPose_AboveWaterDepthIsNegative()
        {
            var s = TelemetryMath.FromPose(0f, new Vector3(0f, 2f, 0f), Quaternion.identity, 0f, 1f);
            Assert.AreEqual(-2f, s.DepthMeters, 1e-4f);
        }

        [Test]
        public void FromPose_HeadingFollowsYaw()
        {
            var s = TelemetryMath.FromPose(0f, Vector3.zero, Quaternion.Euler(0f, 138f, 0f), 0f, 0f);
            Assert.AreEqual(138f, s.HeadingDegrees, 1e-3f);
        }

        [Test]
        public void FromPose_NoseDownIsNegativePitch()
        {
            var s = TelemetryMath.FromPose(0f, Vector3.zero, Quaternion.Euler(10f, 0f, 0f), 0f, 0f);
            Assert.AreEqual(-10f, s.PitchDegrees, 1e-3f);
        }

        [Test]
        public void FromPose_RightSideDownIsPositiveRoll()
        {
            // Positive Z rotation raises the right side in Unity, so right-side-down is a negative Z angle.
            var s = TelemetryMath.FromPose(0f, Vector3.zero, Quaternion.Euler(0f, 0f, -8f), 0f, 0f);
            Assert.AreEqual(8f, s.RollDegrees, 1e-3f);
        }
    }
}
