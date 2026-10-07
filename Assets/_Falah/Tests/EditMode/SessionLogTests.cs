using System;
using System.IO;
using Falah.RovSim.Core;
using NUnit.Framework;
using UnityEngine;

namespace Falah.RovSim.Tests.EditMode
{
    public class SessionLogTests
    {
        [Test]
        public void Add_StampsElapsedTimeAndRaisesEvent()
        {
            var log = new SessionLog { Elapsed = 12.5f };
            SimEvent seen = default;
            log.EventAdded += e => seen = e;
            log.Add(SimEventType.Collision, "tabrakan", EventSeverity.Critical);
            Assert.AreEqual(1, log.Events.Count);
            Assert.AreEqual(12.5f, seen.Time);
            Assert.AreEqual(SimEventType.Collision, seen.Type);
        }

        [Test]
        public void ToggleDisturbance_StartsThenClears()
        {
            var log = new SessionLog();
            Assert.IsTrue(log.ToggleDisturbance(DisturbanceKind.ThrusterLeak));
            Assert.IsTrue(log.IsActive(DisturbanceKind.ThrusterLeak));
            Assert.AreEqual(SimEventType.ThrusterFault, log.Events[0].Type);
            Assert.AreEqual("Kebocoran kecil terdeteksi, thruster #3", log.Events[0].PilotAlert);

            Assert.IsFalse(log.ToggleDisturbance(DisturbanceKind.ThrusterLeak));
            Assert.IsFalse(log.IsActive(DisturbanceKind.ThrusterLeak));
            Assert.AreEqual(SimEventType.DisturbanceCleared, log.Events[1].Type);
        }

        [Test]
        public void EveryDisturbanceHasCatalogEntry()
        {
            foreach (DisturbanceKind kind in Enum.GetValues(typeof(DisturbanceKind)))
                Assert.AreEqual(kind, DisturbanceCatalog.Find(kind).Kind);
        }

        [Test]
        public void ResetCurrent_ClearsEverything()
        {
            SessionLog.Current.Add(SimEventType.SessionStart, "x");
            SessionLog.Current.AddTrack(Vector3.one);
            SessionLog.ResetCurrent();
            Assert.AreEqual(0, SessionLog.Current.Events.Count);
            Assert.AreEqual(0, SessionLog.Current.Track.Count);
        }

        [TestCase(0f, "00:00")]
        [TestCase(754f, "12:34")]
        [TestCase(2292f, "38:12")]
        public void FormatTime_IsMinutesAndSeconds(float seconds, string expected)
        {
            Assert.AreEqual(expected, SessionLog.FormatTime(seconds));
        }

        [Test]
        public void Report_CarriesSessionFactsAndWritesJson()
        {
            var setup = new SessionSetup { UserName = "Pilot Uji", RovId = "tortuga" };
            var log = new SessionLog { Elapsed = 100f, InstructorNote = "catatan" };
            log.Add(SimEventType.SessionStart, "mulai");
            log.AddTrack(Vector3.zero);

            var report = SessionReport.Build(setup, log, new DateTime(2026, 10, 7, 8, 0, 0, DateTimeKind.Utc));
            Assert.AreEqual(SessionReport.FormatVersion, report.version);
            Assert.AreEqual("Pilot Uji", report.trainee);
            Assert.AreEqual(100f, report.durationSeconds);
            Assert.AreEqual(1, report.events.Count);
            Assert.AreEqual(1, report.trackPoints);

            string root = Path.Combine(Path.GetTempPath(), "falah_report_test_" + Guid.NewGuid().ToString("N"));
            try
            {
                string path = report.Save(root);
                Assert.IsTrue(File.Exists(path));
                var loaded = JsonUtility.FromJson<SessionReport>(File.ReadAllText(path));
                Assert.AreEqual("Pilot Uji", loaded.trainee);
                Assert.AreEqual("catatan", loaded.instructorNote);
            }
            finally
            {
                if (Directory.Exists(root)) Directory.Delete(root, true);
            }
        }
    }
}
