using System.Globalization;
using Falah.RovSim.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Falah.RovSim.UI
{
    /// <summary>Mission briefing and pre-dive check. "Mulai simulasi" is enabled once the gamepad is calibrated.</summary>
    public sealed class BriefingScreen : MenuScreen
    {
        const int GamepadCheckIndex = 6;

        [SerializeField] TMP_Text scenarioTitle;
        [SerializeField] TMP_Text rovValue;
        [SerializeField] TMP_Text depthValue;
        [SerializeField] TMP_Text environmentValue;
        [SerializeField] TMP_Text timeLimitValue;
        [SerializeField] TMP_Text[] objectiveTexts;
        [SerializeField] TMP_Text[] checkIcons;
        [SerializeField] TMP_Text[] checkValues;
        [SerializeField] Button calibrateButton;
        [SerializeField] TMP_Text checkSummary;

        void Awake()
        {
            if (calibrateButton != null) calibrateButton.onClick.AddListener(OnCalibrate);
        }

        public override void OnShow()
        {
            base.OnShow();
            var s = SessionSetup.Current;
            var scenario = MenuCatalog.FindScenario(s.ScenarioId);
            scenarioTitle.text = scenario.DisplayName;
            rovValue.text = MenuCatalog.FindRov(s.RovId).DisplayName;
            depthValue.text = Format(s.TargetDepthMeters) + " m";
            environmentValue.text = Format(s.CurrentKnots) + " kn / " + Format(s.VisibilityMeters) + " m";
            timeLimitValue.text = scenario.TimeLimit;
            for (int i = 0; i < objectiveTexts.Length; i++)
            {
                bool has = i < scenario.Objectives.Length;
                objectiveTexts[i].transform.parent.gameObject.SetActive(has);
                if (has) objectiveTexts[i].text = scenario.Objectives[i];
            }
            RefreshChecks();
        }

        void OnCalibrate()
        {
            SessionSetup.Current.GamepadCalibrated = true;
            RefreshChecks();
        }

        void RefreshChecks()
        {
            bool ok = SessionSetup.Current.GamepadCalibrated;
            checkIcons[GamepadCheckIndex].text = ok ? "OK" : "CEK";
            checkIcons[GamepadCheckIndex].color = ok ? UiTheme.Ok : UiTheme.Warn;
            checkValues[GamepadCheckIndex].text = ok ? "Terkalibrasi" : "Belum dikalibrasi";
            checkValues[GamepadCheckIndex].color = ok ? UiTheme.Ok : UiTheme.Warn;
            if (calibrateButton != null) calibrateButton.gameObject.SetActive(!ok);
            checkSummary.text = ok
                ? "Semua pemeriksaan lulus. Simulasi siap dimulai."
                : "Satu pemeriksaan tersisa sebelum simulasi dapat dimulai.";
            nextButton.interactable = ok;
        }

        static string Format(float v) => v.ToString("0.#", CultureInfo.InvariantCulture);
    }
}
