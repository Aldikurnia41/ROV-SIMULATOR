using Falah.RovSim.Core;
using NUnit.Framework;
using UnityEngine;

namespace Falah.RovSim.Tests.EditMode
{
    public class SonarTests
    {
        [Test]
        public void BeamAngles_SpanTheSectorSymmetrically()
        {
            Assert.AreEqual(-60f, SonarGeometry.BeamAngle(0, 64, 120f), 1e-4f);
            Assert.AreEqual(60f, SonarGeometry.BeamAngle(63, 64, 120f), 1e-4f);
            Assert.AreEqual(0f, SonarGeometry.BeamAngle(1, 3, 120f), 1e-4f);
            Assert.AreEqual(0f, SonarGeometry.BeamAngle(0, 1, 120f));
        }

        [Test]
        public void MiddleBeam_OfAnOddCount_PointsStraightAhead()
        {
            Assert.AreEqual(0f, SonarGeometry.BeamAngle(32, 65, 120f), 1e-4f);
        }

        [Test]
        public void NearestBeam_RoundTripsEveryBeam()
        {
            for (int i = 0; i < 64; i++)
                Assert.AreEqual(i, SonarGeometry.NearestBeam(SonarGeometry.BeamAngle(i, 64, 120f), 64, 120f));
        }

        [Test]
        public void NearestBeam_OutsideTheFan_IsMinusOne()
        {
            Assert.AreEqual(-1, SonarGeometry.NearestBeam(75f, 64, 120f));
            Assert.AreEqual(-1, SonarGeometry.NearestBeam(-75f, 64, 120f));
            Assert.AreEqual(63, SonarGeometry.NearestBeam(60.5f, 64, 120f), "within half a beam of the edge still belongs to the edge beam");
        }

        [Test]
        public void ToPolar_UsesRightAsPositiveBearing()
        {
            SonarGeometry.ToPolar(new Vector3(0, 0, 10), out float b, out float r);
            Assert.AreEqual(0f, b, 1e-4f); Assert.AreEqual(10f, r, 1e-4f);
            SonarGeometry.ToPolar(new Vector3(5, 3, 5), out b, out r);
            Assert.AreEqual(45f, b, 1e-3f); Assert.AreEqual(Mathf.Sqrt(50f), r, 1e-4f);
            SonarGeometry.ToPolar(new Vector3(-4, 0, 0), out b, out r);
            Assert.AreEqual(-90f, b, 1e-3f);
        }

        [Test]
        public void Echo_PeaksAtTheHitRange_AndFadesWithinTheThickness()
        {
            Assert.AreEqual(1f, SonarGeometry.Echo(12f, 12f, 0.6f), 1e-5f);
            Assert.AreEqual(0.5f, SonarGeometry.Echo(12.3f, 12f, 0.6f), 1e-4f);
            Assert.AreEqual(0f, SonarGeometry.Echo(13f, 12f, 0.6f));
            Assert.AreEqual(0f, SonarGeometry.Echo(12f, SonarSweep.NoReturn, 0.6f));
        }

        [Test]
        public void Sweep_StartsEmpty_AndClears()
        {
            var s = new SonarSweep(8, 30f, 120f);
            foreach (float r in s.Ranges) Assert.AreEqual(SonarSweep.NoReturn, r);
            s.Ranges[3] = 5f;
            s.Clear();
            Assert.AreEqual(SonarSweep.NoReturn, s.Ranges[3]);
            Assert.AreEqual(8, s.BeamCount);
            Assert.AreEqual(1, new SonarSweep(0, 1f, 10f).BeamCount, "never an empty array");
        }

        [Test]
        public void Profile_SonarSettings_AreValidated()
        {
            var p = RovProfile.CreateDefault("tortuga");
            Assert.AreEqual(10f, p.Sonar.UpdateHz);
            p.Sonar.RangeMeters = 0f;
            p.Sonar.BeamCount = 1;
            p.Sonar.SectorDegrees = 400f;
            p.Sonar.UpdateHz = 0f;
            var messages = string.Join("|", System.Linq.Enumerable.Select(RovProfileValidator.Validate(p), i => i.Message));
            StringAssert.Contains("Sonar range", messages);
            StringAssert.Contains("at least 2 beams", messages);
            StringAssert.Contains("Sonar sector", messages);
            StringAssert.Contains("update rate", messages);
        }
    }
}
