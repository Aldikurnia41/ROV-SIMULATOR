using Falah.RovSim.Core;
using UnityEngine;

namespace Falah.RovSim.Integration
{
    /// <summary>
    /// Bridges the existing <see cref="RoVPhysics"/> vehicle (global namespace, Assembly-CSharp) to the
    /// <see cref="ITelemetrySource"/> contract used by the HUD. It lives outside the UI assembly so the UI never
    /// depends on gameplay scripts. The active vehicle is looked up lazily because SwitchRoV instantiates it at runtime.
    /// </summary>
    public sealed class RoVTelemetryAdapter : MonoBehaviour, ITelemetrySource
    {
        [SerializeField] float seaLevelY;
        [SerializeField] LayerMask seabedMask = ~0;
        [SerializeField] float maxAltitude = 500f;
        [SerializeField] float searchInterval = 1f;

        static readonly RaycastHit[] Hits = new RaycastHit[8];

        RoVPhysics rov;
        float nextSearch;

        public bool TryGetSample(out TelemetrySample sample)
        {
            if (rov == null || !rov.isActiveAndEnabled)
            {
                rov = null;
                if (Time.unscaledTime >= nextSearch)
                {
                    nextSearch = Time.unscaledTime + searchInterval;
                    rov = FindFirstObjectByType<RoVPhysics>();
                }
            }
            if (rov == null)
            {
                sample = default;
                return false;
            }
            var t = rov.transform;
            sample = TelemetryMath.FromPose(Time.time, t.position, t.rotation, seaLevelY, Altitude(t));
            return true;
        }

        float Altitude(Transform vehicle)
        {
            int count = Physics.RaycastNonAlloc(vehicle.position, Vector3.down, Hits, maxAltitude, seabedMask, QueryTriggerInteraction.Ignore);
            float best = float.PositiveInfinity;
            for (int i = 0; i < count; i++)
            {
                if (Hits[i].collider.transform.IsChildOf(vehicle)) continue; // ignore the vehicle's own colliders
                if (Hits[i].distance < best) best = Hits[i].distance;
            }
            return float.IsPositiveInfinity(best) ? float.NaN : best;
        }
    }
}
