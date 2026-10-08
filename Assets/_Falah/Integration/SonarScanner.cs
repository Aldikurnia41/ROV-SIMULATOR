using Falah.RovSim.Core;
using UnityEngine;

namespace Falah.RovSim.Integration
{
    /// <summary>
    /// Forward-looking sonar on the scene ROV: a fan of horizontal raycasts at the profile's update rate (default 10 Hz).
    /// Everything is allocated once (the sweep, the hit buffer), and <see cref="Scan"/> allocates nothing. Hits on the
    /// vehicle's own colliders are ignored; the nearest other hit sets the beam's range.
    /// </summary>
    public sealed class SonarScanner : MonoBehaviour, ISonarSource
    {
        [SerializeField] LayerMask mask = ~0;
        [Tooltip("Yaw of the sonar axis relative to the transform; 180 when the model faces -Z (the scene ROV does).")]
        [SerializeField] float bodyYaw;
        [Tooltip("Origin of the fan relative to the vehicle centre, in the sonar frame (metres).")]
        [SerializeField] Vector3 mountOffset = new Vector3(0f, 0f, 0.4f);

        const int HitBuffer = 8;

        readonly RaycastHit[] hits = new RaycastHit[HitBuffer];
        SonarSettings settings;
        SonarSweep sweep;
        Vector3[] beamDirections; // in the sonar frame, precomputed
        float nextScan;

        public SonarSweep Sweep => sweep;

        public bool TryGetSweep(out SonarSweep s)
        {
            s = sweep;
            return sweep != null;
        }

        void Awake()
        {
            Configure(RovProfiles.Get(SessionSetup.Current.RovId).Sonar);
        }

        public void Configure(SonarSettings s)
        {
            settings = s;
            sweep = new SonarSweep(s.BeamCount, s.RangeMeters, s.SectorDegrees);
            beamDirections = new Vector3[s.BeamCount];
            for (int i = 0; i < s.BeamCount; i++)
            {
                float a = SonarGeometry.BeamAngle(i, s.BeamCount, s.SectorDegrees) * Mathf.Deg2Rad;
                beamDirections[i] = new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a));
            }
        }

        void Update()
        {
            if (Time.time < nextScan) return;
            nextScan = Time.time + 1f / Mathf.Max(settings.UpdateHz, 0.1f);
            Scan();
        }

        /// <summary>One full sweep; allocation-free.</summary>
        public void Scan()
        {
            Quaternion frame = transform.rotation * Quaternion.Euler(0f, bodyYaw, 0f);
            Vector3 origin = transform.position + frame * mountOffset;
            float range = settings.RangeMeters;
            for (int i = 0; i < beamDirections.Length; i++)
            {
                Vector3 direction = frame * beamDirections[i];
                int count = Physics.RaycastNonAlloc(origin, direction, hits, range, mask, QueryTriggerInteraction.Ignore);
                float nearest = SonarSweep.NoReturn;
                for (int h = 0; h < count; h++)
                {
                    if (hits[h].collider.transform.IsChildOf(transform)) continue;
                    if (nearest < 0f || hits[h].distance < nearest) nearest = hits[h].distance;
                }
                sweep.Ranges[i] = nearest;
            }
            sweep.Version++;
        }
    }
}
