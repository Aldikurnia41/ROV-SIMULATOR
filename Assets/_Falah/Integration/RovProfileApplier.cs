using Falah.RovSim.Core;
using UnityEngine;

namespace Falah.RovSim.Integration
{
    /// <summary>
    /// Copies the mass, buoyancy and drag of the chosen ROV's profile onto the scene ROV (Rigidbody and
    /// <see cref="RoVPhysics"/>) when the simulation starts, so changing the profile changes the vehicle's behaviour.
    /// Thrusters, camera and DP gains read the profile themselves.
    /// </summary>
    [RequireComponent(typeof(Rigidbody), typeof(RoVPhysics))]
    public sealed class RovProfileApplier : MonoBehaviour
    {
        void Start() => Apply(RovProfiles.Get(SessionSetup.Current.RovId));

        public void Apply(RovProfile profile)
        {
            GetComponent<Rigidbody>().mass = profile.Mass;
            var physics = GetComponent<RoVPhysics>();
            physics.displacedVolume = profile.Volume;
            physics.centerOfBuoyancyOffset = profile.CenterOfBuoyancyOffset;
            physics.linearDragCoefficients = profile.LinearDrag;
            physics.angularDragCoefficients = profile.AngularDrag;
            physics.addedMassFactors = profile.AddedMassFactor;
        }
    }
}
