using Falah.RovSim.Core;
using NUnit.Framework;

namespace Falah.RovSim.Tests.EditMode
{
    public class CameraTests
    {
        [Test]
        public void Tilt_MovesAtRateTimesInput()
        {
            Assert.AreEqual(-12f + 22.5f, CameraTilt.Step(-12f, 1f, 45f, 0.5f, -90f, 30f), 1e-4f);
            Assert.AreEqual(-12f - 11.25f, CameraTilt.Step(-12f, -0.5f, 45f, 0.5f, -90f, 30f), 1e-4f);
        }

        [Test]
        public void Tilt_IsClampedToLimits()
        {
            Assert.AreEqual(30f, CameraTilt.Step(29f, 1f, 45f, 10f, -90f, 30f));
            Assert.AreEqual(-90f, CameraTilt.Step(-89f, -1f, 45f, 10f, -90f, 30f));
        }

        [Test]
        public void Tilt_NoInputKeepsValue_AndInputIsClamped()
        {
            Assert.AreEqual(-12f, CameraTilt.Step(-12f, 0f, 45f, 1f, -90f, 30f));
            Assert.AreEqual(-12f + 45f, CameraTilt.Step(-12f, 9f, 45f, 1f, -90f, 90f), 1e-4f);
        }

        [Test]
        public void OrbitPitch_StopsBeforeThePoles()
        {
            Assert.AreEqual(80f, CameraTilt.OrbitPitch(120f));
            Assert.AreEqual(-80f, CameraTilt.OrbitPitch(-120f));
            Assert.AreEqual(10f, CameraTilt.OrbitPitch(10f));
        }

        [Test]
        public void OrbitDistance_ZoomsAndClamps()
        {
            Assert.Less(CameraTilt.OrbitDistance(10f, 0.5f, 2f, 40f), 10f);
            Assert.Greater(CameraTilt.OrbitDistance(10f, -0.5f, 2f, 40f), 10f);
            Assert.AreEqual(2f, CameraTilt.OrbitDistance(10f, 9f, 2f, 40f));
            Assert.AreEqual(40f, CameraTilt.OrbitDistance(10f, -9f, 2f, 40f));
        }

        [Test]
        public void Profiles_DifferPerRov_AndDefaultWithinLimits()
        {
            var a = RovCameraSettings.For("tortuga");
            var b = RovCameraSettings.For("teledyne");
            Assert.AreNotEqual(a.FieldOfView, b.FieldOfView);
            foreach (var s in new[] { a, b })
            {
                Assert.GreaterOrEqual(s.DefaultTilt, s.MinTilt);
                Assert.LessOrEqual(s.DefaultTilt, s.MaxTilt);
            }
        }
    }
}
