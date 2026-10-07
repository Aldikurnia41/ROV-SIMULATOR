using Falah.RovSim.UI;
using NUnit.Framework;

namespace Falah.RovSim.Tests.EditMode
{
    public class PilotHudTests
    {
        [TestCase(0f, "00:00")]
        [TestCase(59.9f, "00:59")]
        [TestCase(754f, "12:34")]
        [TestCase(2700f, "45:00")]
        [TestCase(-5f, "00:00")]
        public void Clock_FormatsMinutesAndSeconds(float seconds, string expected)
        {
            Assert.AreEqual(expected, PilotHud.Clock(seconds));
        }

        [TestCase("45 menit", 45)]
        [TestCase("[___] menit", 0)]
        [TestCase("", 0)]
        [TestCase(null, 0)]
        public void ParseMinutes_ReadsLeadingNumber(string text, int expected)
        {
            Assert.AreEqual(expected, PilotHud.ParseMinutes(text));
        }
    }
}
