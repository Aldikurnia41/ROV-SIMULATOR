using Falah.RovSim.Core;
using NUnit.Framework;
using UnityEngine;

namespace Falah.RovSim.Tests.EditMode
{
    public class ControlShaperTests
    {
        [TestCase(0.1f)]
        [TestCase(-0.1f)]
        [TestCase(0.15f)]
        public void InsideDeadzone_IsZero(float v)
        {
            Assert.AreEqual(0f, ControlShaper.Axis(v, 0.15f, 0.4f));
        }

        [Test]
        public void FullDeflection_IsFullOutput_BothSigns()
        {
            Assert.AreEqual(1f, ControlShaper.Axis(1f, 0.15f, 0.4f), 1e-5f);
            Assert.AreEqual(-1f, ControlShaper.Axis(-1f, 0.15f, 0.4f), 1e-5f);
        }

        [Test]
        public void JustOutsideDeadzone_StartsNearZero_NoJump()
        {
            Assert.Less(ControlShaper.Axis(0.1501f, 0.15f, 0f), 0.001f);
        }

        [Test]
        public void Linear_WhenExpoIsZero()
        {
            // midpoint of the live range: (0.575 - 0.15) / 0.85 = 0.5
            Assert.AreEqual(0.5f, ControlShaper.Axis(0.575f, 0.15f, 0f), 1e-4f);
        }

        [Test]
        public void Expo_SoftensSmallDeflections_AndKeepsEndpoints()
        {
            float soft = ControlShaper.Axis(0.575f, 0.15f, 1f); // x = 0.5 -> 0.125
            Assert.AreEqual(0.125f, soft, 1e-4f);
            Assert.Less(soft, ControlShaper.Axis(0.575f, 0.15f, 0f));
            Assert.AreEqual(1f, ControlShaper.Axis(1f, 0.15f, 1f), 1e-5f);
        }

        [Test]
        public void Axis_IsMonotonic()
        {
            float prev = -2f;
            for (float v = -1f; v <= 1.0001f; v += 0.05f)
            {
                float o = ControlShaper.Axis(v, 0.2f, 0.5f);
                Assert.GreaterOrEqual(o, prev - 1e-6f);
                prev = o;
            }
        }

        [Test]
        public void OutOfRangeInput_IsClamped()
        {
            Assert.AreEqual(1f, ControlShaper.Axis(5f, 0.1f, 0f), 1e-5f);
            Assert.AreEqual(-1f, ControlShaper.Axis(-5f, 0.1f, 0f), 1e-5f);
        }

        [Test]
        public void Stick_UsesACircularDeadzone()
        {
            // 0.12 on both axes has magnitude 0.17 > 0.15, so it passes (a square deadzone would cut it)
            var o = ControlShaper.Stick(new Vector2(0.12f, 0.12f), 0.15f, 0f);
            Assert.Greater(o.magnitude, 0f);
            Assert.AreEqual(0f, ControlShaper.Stick(new Vector2(0.1f, 0.05f), 0.15f, 0f).magnitude);
        }

        [Test]
        public void Stick_KeepsDirection_AndCapsAtOne()
        {
            var o = ControlShaper.Stick(new Vector2(3f, 4f), 0.1f, 0.3f);
            Assert.AreEqual(1f, o.magnitude, 1e-4f);
            Assert.AreEqual(0.6f, o.x, 1e-4f);
            Assert.AreEqual(0.8f, o.y, 1e-4f);
            Assert.AreEqual(Vector2.zero, ControlShaper.Stick(Vector2.zero, 0.1f, 0.3f));
        }

        [Test]
        public void ScriptedInput_HoldsItsValues()
        {
            IControlInput input = new ScriptedInput { Translation = new Vector3(0.1f, 0.2f, 0.3f), Yaw = -0.4f };
            Assert.AreEqual(0.3f, input.Translation.z);
            Assert.AreEqual(-0.4f, input.Yaw);
        }
    }
}
