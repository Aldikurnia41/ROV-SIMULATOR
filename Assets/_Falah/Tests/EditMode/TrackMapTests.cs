using System.Collections.Generic;
using Falah.RovSim.Core;
using Falah.RovSim.UI;
using NUnit.Framework;

namespace Falah.RovSim.Tests.EditMode
{
    public class TrackMapTests
    {
        static SimEvent Event(float time, SimEventType type) => new SimEvent { Time = time, Type = type };

        [Test]
        public void DisturbanceIntervals_MarksPointsBetweenInjectAndClear()
        {
            var events = new List<SimEvent> { Event(10f, SimEventType.ThrusterFault), Event(20f, SimEventType.DisturbanceCleared) };
            var times = new List<float> { 0f, 10f, 15f, 20f, 25f };
            var marked = TrackMap.DisturbanceIntervals(events, 30f, times);
            CollectionAssert.AreEqual(new[] { false, true, true, true, false }, marked);
        }

        [Test]
        public void DisturbanceIntervals_OpenIntervalRunsToSessionEnd()
        {
            var events = new List<SimEvent> { Event(10f, SimEventType.DisturbanceInjected) };
            var times = new List<float> { 5f, 12f, 30f };
            var marked = TrackMap.DisturbanceIntervals(events, 30f, times);
            CollectionAssert.AreEqual(new[] { false, true, true }, marked);
        }

        [Test]
        public void DisturbanceIntervals_NoEventsMarksNothing()
        {
            var marked = TrackMap.DisturbanceIntervals(new List<SimEvent>(), 30f, new List<float> { 1f, 2f });
            CollectionAssert.AreEqual(new[] { false, false }, marked);
        }
    }
}
