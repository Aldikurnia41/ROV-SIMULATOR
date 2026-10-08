using System;
using UnityEngine;

namespace Falah.RovSim.Core
{
    /// <summary>Forward-looking imaging sonar of a ROV. PLACEHOLDER values until the profile data is confirmed.</summary>
    [Serializable]
    public sealed class SonarSettings
    {
        public float RangeMeters = 30f;
        /// <summary>Total horizontal opening of the fan, degrees.</summary>
        public float SectorDegrees = 120f;
        public int BeamCount = 64;
        public float UpdateHz = 10f;
    }

    /// <summary>
    /// One sonar sweep: the range of each beam (metres, or <see cref="NoReturn"/>). The arrays are allocated once and
    /// reused; <see cref="Version"/> increases with every completed sweep so readers can tell when to redraw.
    /// </summary>
    public sealed class SonarSweep
    {
        public const float NoReturn = -1f;

        public readonly float[] Ranges;
        public readonly float MaxRange;
        public readonly float SectorDegrees;
        public int Version;

        public SonarSweep(int beamCount, float maxRange, float sectorDegrees)
        {
            Ranges = new float[Mathf.Max(beamCount, 1)];
            MaxRange = maxRange;
            SectorDegrees = sectorDegrees;
            Clear();
        }

        public int BeamCount => Ranges.Length;

        public void Clear()
        {
            for (int i = 0; i < Ranges.Length; i++) Ranges[i] = NoReturn;
        }
    }

    public interface ISonarSource
    {
        /// <summary>False while no vehicle (and so no sonar) is active.</summary>
        bool TryGetSweep(out SonarSweep sweep);
    }

    /// <summary>Geometry shared by the scanner and the display; pure functions.</summary>
    public static class SonarGeometry
    {
        /// <summary>Bearing of a beam relative to the vehicle's front, degrees, negative = left; beams span the sector evenly.</summary>
        public static float BeamAngle(int index, int count, float sectorDegrees)
        {
            if (count <= 1) return 0f;
            return -0.5f * sectorDegrees + sectorDegrees * index / (count - 1f);
        }

        /// <summary>Beam closest to a bearing, or -1 when the bearing lies outside the fan.</summary>
        public static int NearestBeam(float bearingDegrees, int count, float sectorDegrees)
        {
            if (count <= 1) return Mathf.Abs(bearingDegrees) <= sectorDegrees * 0.5f ? 0 : -1;
            float half = sectorDegrees * 0.5f;
            float step = sectorDegrees / (count - 1f);
            if (bearingDegrees < -half - step * 0.5f || bearingDegrees > half + step * 0.5f) return -1;
            return Mathf.Clamp(Mathf.RoundToInt((bearingDegrees + half) / step), 0, count - 1);
        }

        /// <summary>Bearing (degrees, right = positive) and range of a point in the vehicle frame (X right, Z forward), on the horizontal plane.</summary>
        public static void ToPolar(Vector3 localPoint, out float bearingDegrees, out float range)
        {
            range = new Vector2(localPoint.x, localPoint.z).magnitude;
            bearingDegrees = Mathf.Atan2(localPoint.x, localPoint.z) * Mathf.Rad2Deg;
        }

        /// <summary>Brightness 0..1 of a pixel at <paramref name="pixelRange"/> for a beam whose echo came from <paramref name="hitRange"/>.</summary>
        public static float Echo(float pixelRange, float hitRange, float thickness)
        {
            if (hitRange < 0f) return 0f;
            float d = Mathf.Abs(pixelRange - hitRange);
            return d >= thickness ? 0f : 1f - d / thickness;
        }
    }
}
