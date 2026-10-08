using Falah.RovSim.Core;
using UnityEngine;

namespace Falah.RovSim.Integration
{
    /// <summary>
    /// Keeps one <see cref="WaterEnvironment"/> in step with the session setup (menu values, then the instructor's
    /// sliders) and publishes it to the vehicle physics (<see cref="RoVPhysics.Water"/>) and the underwater fog.
    /// </summary>
    public sealed class WaterEnvironmentDriver : MonoBehaviour
    {
        readonly WaterEnvironment environment = new WaterEnvironment();

        public WaterEnvironment Environment => environment;

        void OnEnable()
        {
            Sync();
            RoVPhysics.Water = environment;
        }

        void OnDisable()
        {
            if (ReferenceEquals(RoVPhysics.Water, environment)) RoVPhysics.Water = null;
        }

        void Update() => Sync();

        void Sync()
        {
            var s = SessionSetup.Current;
            environment.SetCurrent(s.CurrentKnots, s.CurrentDirectionDegrees);
            environment.SetVisibility(s.VisibilityMeters);
        }
    }
}
