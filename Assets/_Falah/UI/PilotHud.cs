using System.Globalization;
using Falah.RovSim.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Falah.RovSim.UI
{
    /// <summary>
    /// Pilot camera HUD (design "5a. Simulasi: ROV kecil"). Depth, altitude, heading, pitch and roll come from an
    /// <see cref="ITelemetrySource"/>; current comes from the session setup. Sonar, thruster dials, tether, light and
    /// camera tilt have no simulation behind them yet and show sample data (the "Data contoh" tag stays visible).
    /// </summary>
    public sealed class PilotHud : MonoBehaviour
    {
        [Header("Source")]
        [SerializeField] MonoBehaviour telemetrySource;

        [Header("Top bar")]
        [SerializeField] TMP_Text rovName;
        [SerializeField] TMP_Text scenarioName;
        [SerializeField] TMP_Text timerElapsed;
        [SerializeField] TMP_Text timerLimit;
        [SerializeField] GameObject sampleDataTag;
        [SerializeField] Graphic recDot;

        [Header("Alert")]
        [SerializeField] GameObject alertRoot;
        [SerializeField] TMP_Text alertText;

        [Header("Telemetry")]
        [SerializeField] TMP_Text depth;
        [SerializeField] TMP_Text altitude;
        [SerializeField] TMP_Text heading;
        [SerializeField] TMP_Text pitch;
        [SerializeField] TMP_Text roll;
        [SerializeField] TMP_Text current;
        [SerializeField] TMP_Text tether;

        [Header("Thrusters (nominal values are sample data; efficiency under faults is live)")]
        [SerializeField] RectTransform[] needles;
        [SerializeField] Graphic[] rings;
        [SerializeField] TMP_Text[] thrustValues;
        [SerializeField] TMP_Text[] thrusterLabels;
        [SerializeField] float[] sampleThrustKgf = { 9.4f, 9.1f, 9.3f, 9.3f };
        [SerializeField] float[] sampleNeedleDegrees = { 30f, 62f, 30f, 28f };
        [Tooltip("Efficiency below this shows the thruster in the warning colour.")]
        [SerializeField] float faultThreshold = 0.95f;

        [Header("Light and camera tilt (sample data)")]
        [SerializeField] TMP_Text lightValue;
        [SerializeField] RectTransform lightFill;
        [SerializeField] TMP_Text tiltValue;
        [SerializeField] RectTransform tiltFill;

        const string MutedUnit = "<size=50%><color=#93A7C2> ";
        const string UnitEnd = "</color></size>";

        ITelemetrySource source;
        float limitSeconds = -1f;

        void Awake()
        {
            source = telemetrySource as ITelemetrySource;
        }

        void OnDisable()
        {
            SessionLog.EventRaised -= OnSessionEvent;
        }

        void OnSessionEvent(SimEvent e)
        {
            if (!string.IsNullOrEmpty(e.PilotAlert)) ShowAlert(e.PilotAlert);
            else if (e.Type == SimEventType.DisturbanceCleared) HideAlert();
        }

        void OnEnable()
        {
            SessionLog.EventRaised += OnSessionEvent;
            var s = SessionSetup.Current;
            var rov = MenuCatalog.FindRov(s.RovId);
            var scenario = MenuCatalog.FindScenario(s.ScenarioId);
            rovName.text = rov.DisplayName.ToUpperInvariant();
            scenarioName.text = scenario.DisplayName;
            limitSeconds = ParseMinutes(scenario.TimeLimit) * 60f;
            tether.text = "[___] m";
            if (alertRoot != null) alertRoot.SetActive(false);
            if (sampleDataTag != null) sampleDataTag.SetActive(true);
            ApplySampleThrusters();
            SetLight(0.7f);
            SetCameraTilt(-12f);
        }

        void Update()
        {
            timerElapsed.text = Clock(SessionLog.Current.Elapsed);
            timerLimit.text = "/ " + (limitSeconds > 0f ? Clock(limitSeconds) : "--:--");
            UpdateThrusters();
            var setup = SessionSetup.Current;
            current.text = Format(setup.CurrentKnots, "0.0") + " kn <size=65%><color=#93A7C2>" +
                           Format(setup.CurrentDirectionDegrees, "000") + "°" + UnitEnd;
            if (recDot != null) recDot.enabled = Mathf.FloorToInt(Time.unscaledTime * 1.5f) % 2 == 0;

            if (source != null && source.TryGetSample(out var t))
            {
                depth.text = Format(t.DepthMeters, "0.0") + MutedUnit + "m" + UnitEnd;
                altitude.text = float.IsNaN(t.AltitudeMeters)
                    ? "--" + MutedUnit + "m" + UnitEnd
                    : Format(t.AltitudeMeters, "0.0") + MutedUnit + "m" + UnitEnd;
                heading.text = Format(t.HeadingDegrees, "000") + MutedUnit + "°" + UnitEnd;
                pitch.text = Format(t.PitchDegrees, "+0.0;-0.0;0.0") + "°";
                roll.text = Format(t.RollDegrees, "+0.0;-0.0;0.0") + "°";
            }
            else
            {
                depth.text = "--" + MutedUnit + "m" + UnitEnd;
                altitude.text = "--" + MutedUnit + "m" + UnitEnd;
                heading.text = "---" + MutedUnit + "°" + UnitEnd;
                pitch.text = "--";
                roll.text = "--";
            }
        }

        /// <summary>Shows a warning banner under the top bar (instructor events, thruster faults).</summary>
        public void ShowAlert(string message)
        {
            alertText.text = message;
            alertRoot.SetActive(true);
        }

        public void HideAlert() => alertRoot.SetActive(false);

        /// <param name="level01">0..1</param>
        public void SetLight(float level01)
        {
            level01 = Mathf.Clamp01(level01);
            lightValue.text = Format(level01 * 100f, "0") + "%";
            SetFill(lightFill, level01);
        }

        /// <param name="degrees">-90..+90, shown as a bar centred on 0.</param>
        public void SetCameraTilt(float degrees)
        {
            degrees = Mathf.Clamp(degrees, -90f, 90f);
            tiltValue.text = Format(degrees, "+0;-0;0") + "°";
            SetFill(tiltFill, (degrees + 90f) / 180f * 0.84f);
        }

        void ApplySampleThrusters()
        {
            for (int i = 0; i < needles.Length; i++)
                needles[i].localEulerAngles = new Vector3(0f, 0f, -sampleNeedleDegrees[i]);
            UpdateThrusters();
        }

        /// <summary>Thrust shown = nominal sample x live efficiency from injected faults.</summary>
        void UpdateThrusters()
        {
            var log = SessionLog.Current;
            for (int i = 0; i < needles.Length; i++)
            {
                float efficiency = ThrusterFaultModel.Efficiency(i, log);
                bool fault = efficiency < faultThreshold;
                needles[i].GetComponent<Graphic>().color = fault ? UiTheme.Warn : UiTheme.Accent;
                rings[i].color = fault ? UiTheme.Warn : UiTheme.BorderStrong;
                thrustValues[i].text = Format(sampleThrustKgf[i] * efficiency, "0.0");
                thrustValues[i].color = fault ? UiTheme.Warn : UiTheme.Text;
                thrusterLabels[i].color = fault ? UiTheme.Warn : UiTheme.TextMuted;
            }
        }

        static void SetFill(RectTransform fill, float fraction)
        {
            fill.anchorMin = new Vector2(0f, 0f);
            fill.anchorMax = new Vector2(Mathf.Clamp01(fraction), 1f);
            fill.offsetMin = fill.offsetMax = Vector2.zero;
        }

        static string Format(float value, string format) => value.ToString(format, CultureInfo.InvariantCulture);

        /// <summary>mm:ss for a duration in seconds.</summary>
        public static string Clock(float seconds) => SessionLog.FormatTime(seconds);

        /// <summary>Leading integer of strings like "45 menit"; 0 when there is none (e.g. "[___] menit").</summary>
        public static int ParseMinutes(string text)
        {
            if (string.IsNullOrEmpty(text)) return 0;
            int value = 0, i = 0;
            while (i < text.Length && char.IsDigit(text[i])) value = value * 10 + (text[i++] - '0');
            return value;
        }
    }
}
