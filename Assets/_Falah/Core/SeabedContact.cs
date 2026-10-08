namespace Falah.RovSim.Core
{
    /// <summary>
    /// Decides when the vehicle has hit the seabed: altitude below the contact height while moving faster than the
    /// impact speed. Re-arms only after the vehicle has lifted clear, so one grounding gives one event.
    /// </summary>
    public sealed class SeabedContact
    {
        public float ContactAltitude = 0.5f;   // PLACEHOLDER: depends on the real hull height
        public float ClearAltitude = 1.5f;
        public float MinImpactSpeed = 0.3f;    // PLACEHOLDER

        bool armed = true;

        /// <returns>True on the tick that counts as a collision.</returns>
        public bool Update(float altitudeMeters, float speedMetersPerSecond)
        {
            if (float.IsNaN(altitudeMeters)) { armed = true; return false; }
            if (altitudeMeters >= ClearAltitude) { armed = true; return false; }
            if (altitudeMeters <= ContactAltitude && armed && speedMetersPerSecond >= MinImpactSpeed)
            {
                armed = false;
                return true;
            }
            return false;
        }
    }
}
