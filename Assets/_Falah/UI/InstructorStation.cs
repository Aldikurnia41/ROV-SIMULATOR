using Falah.RovSim.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Falah.RovSim.UI
{
    /// <summary>
    /// Instructor station (design "6. Station instruktur"). Everything on it is live except the effect of injected
    /// disturbances and of the environment sliders on the physics, which belong to later tasks (T2.1, T3.3).
    /// </summary>
    public sealed class InstructorStation : MonoBehaviour
    {
        [SerializeField] SessionController session;
        [SerializeField] MonoBehaviour telemetrySource;

        [Header("Header")]
        [SerializeField] TMP_Text rovName;
        [SerializeField] TMP_Text scenarioName;
        [SerializeField] TMP_Text clockElapsed;
        [SerializeField] TMP_Text clockLimit;

        [Header("Session")]
        [SerializeField] TMP_Text traineeValue;
        [SerializeField] TMP_Text modeValue;
        [SerializeField] TMP_Text recordingValue;
        [SerializeField] Button pauseButton;
        [SerializeField] TMP_Text pauseLabel;
        [SerializeField] Button endButton;

        [Header("Disturbances (order of DisturbanceCatalog.All)")]
        [SerializeField] Button[] disturbanceButtons;
        [SerializeField] TMP_Text[] disturbanceLabels;
        [SerializeField] Graphic[] disturbanceBorders;
        [SerializeField] Graphic[] disturbanceFills;

        [Header("Environment")]
        [SerializeField] SliderField currentSpeed;
        [SerializeField] SliderField currentDirection;
        [SerializeField] SliderField visibility;
        [SerializeField] TMP_InputField noteField;

        [Header("Views")]
        [SerializeField] EventListView timeline;
        [SerializeField] TrackMap map;

        const float EnvironmentLogDelay = 1f;

        static readonly Color ActiveFill = UiTheme.Hex("2A2110");

        ITelemetrySource source;
        float environmentDirtyAt = -1f;
        float limitSeconds;

        void Awake()
        {
            source = telemetrySource as ITelemetrySource;
            pauseButton.onClick.AddListener(TogglePause);
            endButton.onClick.AddListener(() => session.EndSession());
            for (int i = 0; i < disturbanceButtons.Length; i++)
            {
                var kind = DisturbanceCatalog.All[i].Kind;
                disturbanceButtons[i].onClick.AddListener(() => SessionLog.Current.ToggleDisturbance(kind));
            }
            currentSpeed.Changed += v => { SessionSetup.Current.CurrentKnots = v; MarkEnvironmentDirty(); };
            currentDirection.Changed += v => { SessionSetup.Current.CurrentDirectionDegrees = v; MarkEnvironmentDirty(); };
            visibility.Changed += v => { SessionSetup.Current.VisibilityMeters = v; MarkEnvironmentDirty(); };
            noteField.onValueChanged.AddListener(t => SessionLog.Current.InstructorNote = t);
        }

        void OnEnable()
        {
            SessionLog.EventRaised += OnEvent;
            var s = SessionSetup.Current;
            rovName.text = MenuCatalog.FindRov(s.RovId).DisplayName.ToUpperInvariant();
            var scenario = MenuCatalog.FindScenario(s.ScenarioId);
            scenarioName.text = scenario.DisplayName;
            limitSeconds = PilotHud.ParseMinutes(scenario.TimeLimit) * 60f;
            traineeValue.text = string.IsNullOrEmpty(s.UserName) ? "[Nama trainee]" : s.UserName;
            modeValue.text = MenuCatalog.FindMode(s.Mode).DisplayName;
            recordingValue.text = "Aktif";
            currentSpeed.SetValue(s.CurrentKnots);
            currentDirection.SetValue(s.CurrentDirectionDegrees);
            visibility.SetValue(s.VisibilityMeters);
            noteField.SetTextWithoutNotify(SessionLog.Current.InstructorNote);
            Refresh();
        }

        void OnDisable()
        {
            SessionLog.EventRaised -= OnEvent;
        }

        void Update()
        {
            var log = SessionLog.Current;
            clockElapsed.text = SessionLog.FormatTime(log.Elapsed);
            clockLimit.text = "/ " + (limitSeconds > 0f ? SessionLog.FormatTime(limitSeconds) : "--:--");
            if (source != null && source.TryGetSample(out var sample)) map.Feed(sample.Position, sample.HeadingDegrees);

            if (environmentDirtyAt >= 0f && Time.unscaledTime - environmentDirtyAt >= EnvironmentLogDelay)
            {
                environmentDirtyAt = -1f;
                var s = SessionSetup.Current;
                log.Add(SimEventType.CurrentChanged,
                    "Instruktur: arus " + s.CurrentKnots.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) + " kn arah " +
                    Mathf.RoundToInt(s.CurrentDirectionDegrees).ToString("000") + "°, visibilitas " +
                    s.VisibilityMeters.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture) + " m", EventSeverity.Info);
                s.NotifyEnvironmentChanged();
            }
        }

        void MarkEnvironmentDirty() => environmentDirtyAt = Time.unscaledTime;

        void TogglePause()
        {
            session.SetPaused(!session.Paused);
            Refresh();
        }

        void OnEvent(SimEvent e) => Refresh();

        void Refresh()
        {
            var log = SessionLog.Current;
            timeline.Show(log.Events);
            for (int i = 0; i < disturbanceButtons.Length; i++)
            {
                var info = DisturbanceCatalog.All[i];
                bool on = log.IsActive(info.Kind);
                disturbanceLabels[i].text = on ? info.Label + " · aktif" : info.Label;
                disturbanceLabels[i].color = on ? UiTheme.Warn : UiTheme.Text;
                disturbanceBorders[i].color = on ? UiTheme.Warn : UiTheme.BorderStrong;
                disturbanceFills[i].color = on ? ActiveFill : UiTheme.Panel;
            }
            pauseLabel.text = session.Paused ? "Lanjut" : "Jeda";
        }
    }
}
