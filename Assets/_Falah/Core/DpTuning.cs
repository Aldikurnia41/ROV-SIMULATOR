using System;

namespace Falah.RovSim.Core
{
    /// <summary>
    /// Default DP gains used to build the placeholder <see cref="RovProfile"/>s. The live gains are the profile's own
    /// HeadingPid, DepthPid and PositionPid; tuning per ROV is a MANUAL job (BACKLOG T3.4) done in the DP Tuning window.
    /// </summary>
    public static class DpTuning
    {
        [Serializable]
        public struct Set
        {
            public string RovId;
            public PidGains Heading;
            public PidGains Depth;
            public PidGains Position;
        }

        public static Set Defaults(string rovId) => new Set
        {
            RovId = rovId,
            // PLACEHOLDER gains tuned on the scene ROV. Units: heading N·m per degree, depth and position N per metre.
            Heading = new PidGains(2.0f, 0.2f, 1.5f, 20f, 60f),
            Depth = new PidGains(120f, 40f, 160f, 200f, 300f),
            Position = new PidGains(150f, 10f, 40f, 60f, 200f),
        };
    }
}
