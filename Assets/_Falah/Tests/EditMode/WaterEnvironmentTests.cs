using Falah.RovSim.Core;
using NUnit.Framework;
using UnityEngine;

namespace Falah.RovSim.Tests.EditMode
{
    public class WaterEnvironmentTests
    {
        static WaterEnvironment Calm(float knots, float dir)
        {
            var w = new WaterEnvironment { GustFraction = 0f, DirectionWobbleDegrees = 0f };
            w.SetCurrent(knots, dir);
            return w;
        }

        [TestCase(0f, 0f, 0f, 1f)]    // flows north (+Z)
        [TestCase(90f, 1f, 0f, 0f)]   // east (+X)
        [TestCase(180f, 0f, 0f, -1f)] // south
        [TestCase(270f, -1f, 0f, 0f)] // west
        public void Direction_IsClockwiseFromNorth(float dir, float x, float y, float z)
        {
            var v = Calm(1f, dir).BaseCurrent / WaterEnvironment.KnotsToMetersPerSecond;
            Assert.AreEqual(x, v.x, 1e-4f);
            Assert.AreEqual(y, v.y, 1e-4f);
            Assert.AreEqual(z, v.z, 1e-4f);
        }

        [Test]
        public void Speed_ConvertsKnotsToMetersPerSecond()
        {
            Assert.AreEqual(2f * 0.514444f, Calm(2f, 45f).BaseCurrent.magnitude, 1e-4f);
        }

        [Test]
        public void ZeroCurrent_IsZeroAtAnyTime()
        {
            var w = Calm(0f, 90f);
            Assert.AreEqual(Vector3.zero, w.CurrentAt(Vector3.zero, 12.3f));
        }

        [Test]
        public void Disturbance_StaysWithinItsBounds_AndIsDeterministic()
        {
            var w = new WaterEnvironment();
            w.SetCurrent(2f, 45f);
            float baseSpeed = 2f * WaterEnvironment.KnotsToMetersPerSecond;
            float min = float.MaxValue, max = 0f;
            for (float t = 0f; t < 200f; t += 0.5f)
            {
                var a = w.CurrentAt(new Vector3(5, -10, 7), t);
                Assert.AreEqual(a, w.CurrentAt(new Vector3(-9, -50, 0), t), "uniform in space");
                min = Mathf.Min(min, a.magnitude); max = Mathf.Max(max, a.magnitude);
                Assert.AreEqual(0f, a.y);
            }
            Assert.AreEqual(baseSpeed * 0.85f, min, baseSpeed * 0.01f);
            Assert.AreEqual(baseSpeed * 1.15f, max, baseSpeed * 0.01f);
        }

        [Test]
        public void Setters_ClampAndWrap()
        {
            var w = new WaterEnvironment();
            w.SetCurrent(-3f, -90f);
            Assert.AreEqual(0f, w.CurrentKnots);
            Assert.AreEqual(270f, w.CurrentDirectionDegrees, 1e-4f);
            w.SetVisibility(-5f);
            Assert.Greater(w.SurfaceVisibilityMeters, 0f);
        }

        [Test]
        public void Visibility_FallsWithDepth_ToTheConfiguredFraction()
        {
            var w = new WaterEnvironment { VisibilityAtDepthFraction = 0.5f, VisibilityFullDepth = 200f };
            w.SetVisibility(10f);
            Assert.AreEqual(10f, w.VisibilityAt(0f), 1e-4f);
            Assert.AreEqual(10f, w.VisibilityAt(-5f), 1e-4f, "above the surface: surface value");
            Assert.AreEqual(7.5f, w.VisibilityAt(100f), 1e-4f);
            Assert.AreEqual(5f, w.VisibilityAt(200f), 1e-4f);
            Assert.AreEqual(5f, w.VisibilityAt(1000f), 1e-4f);
        }

        [Test]
        public void FogDensity_IsInverseToVisibility_AndGivesFivePercentAtThatDistance()
        {
            Assert.Greater(WaterEnvironment.FogDensityFor(2f), WaterEnvironment.FogDensityFor(8f));
            float d = WaterEnvironment.FogDensityFor(6f);
            float transmission = Mathf.Exp(-(d * 6f) * (d * 6f));
            Assert.AreEqual(0.05f, transmission, 0.002f);
            Assert.IsFalse(float.IsInfinity(WaterEnvironment.FogDensityFor(0f)));
        }
    }
}
