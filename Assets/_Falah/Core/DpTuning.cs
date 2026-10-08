using System;
using UnityEngine;

namespace Falah.RovSim.Core
{
    /// <summary>
    /// PID gains of the three DP loops per ROV, editable in the Inspector and in the DP Tuning window. Defaults are
    /// PLACEHOLDERS tuned on the scene ROV; the real values are a MANUAL tuning job per profile (BACKLOG T3.4).
    /// </summary>
    [CreateAssetMenu(menuName = "Falah/DP Tuning", fileName = "DpTuning")]
    public sealed class DpTuning : ScriptableObject
    {
        [Serializable]
        public struct Set
        {
            public string RovId;
            public PidGains Heading;
            public PidGains Depth;
            public PidGains Position;
        }

        public Set[] Sets = { Defaults("tortuga"), Defaults("teledyne") };

        public static Set Defaults(string rovId) => new Set
        {
            RovId = rovId,
            // PLACEHOLDER gains. Units: heading N·m per degree, depth and position N per metre.
            Heading = new PidGains(2.0f, 0.2f, 1.5f, 20f, 60f),
            Depth = new PidGains(120f, 40f, 160f, 200f, 300f),
            Position = new PidGains(150f, 10f, 40f, 60f, 200f),
        };

        public Set For(string rovId)
        {
            if (Sets != null)
                foreach (var s in Sets) if (s.RovId == rovId) return s;
            return Defaults(rovId);
        }
    }
}
