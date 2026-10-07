using System.Linq;
using Falah.RovSim.Core;
using TMPro;
using UnityEngine;

namespace Falah.RovSim.UI
{
    public sealed class ScenarioScreen : MenuScreen
    {
        public const string DayId = "day";
        public const string DuskId = "dusk";
        public const string NightId = "night";

        [SerializeField] ChoiceGroup modeGroup;
        [SerializeField] ChoiceGroup scenarioGroup;
        [SerializeField] ChoiceGroup timeGroup;
        [SerializeField] SliderField current;
        [SerializeField] SliderField visibility;
        [SerializeField] SliderField targetDepth;
        [SerializeField] SwitchToggle thrusterLeak;
        [SerializeField] SwitchToggle lightsOut;
        [SerializeField] SwitchToggle tetherSnag;
        [SerializeField] TMP_Text summaryMode;
        [SerializeField] TMP_Text summaryScenario;
        [SerializeField] TMP_Text locationText;

        void Awake()
        {
            modeGroup.Changed += id => { SessionSetup.Current.Mode = MenuCatalog.Modes.First(m => m.Id == id).Mode; RefreshSummary(); };
            scenarioGroup.Changed += id => { SessionSetup.Current.ScenarioId = id; RefreshSummary(); };
            timeGroup.Changed += id => SessionSetup.Current.TimeOfDay = ToTimeOfDay(id);
            current.Changed += v => SessionSetup.Current.CurrentKnots = v;
            visibility.Changed += v => SessionSetup.Current.VisibilityMeters = v;
            targetDepth.Changed += v => SessionSetup.Current.TargetDepthMeters = v;
            thrusterLeak.Changed += on => SessionSetup.Current.Disturbances.ThrusterLeak = on;
            lightsOut.Changed += on => SessionSetup.Current.Disturbances.LightsOut = on;
            tetherSnag.Changed += on => SessionSetup.Current.Disturbances.TetherSnag = on;
        }

        public override void OnShow()
        {
            base.OnShow();
            var s = SessionSetup.Current;
            modeGroup.Select(MenuCatalog.FindMode(s.Mode).Id, false);
            scenarioGroup.Select(s.ScenarioId, false);
            timeGroup.Select(ToId(s.TimeOfDay), false);
            current.SetValue(s.CurrentKnots);
            visibility.SetValue(s.VisibilityMeters);
            targetDepth.SetValue(s.TargetDepthMeters);
            thrusterLeak.Set(s.Disturbances.ThrusterLeak, false);
            lightsOut.Set(s.Disturbances.LightsOut, false);
            tetherSnag.Set(s.Disturbances.TetherSnag, false);
            if (locationText != null) locationText.text = s.LocationLabel;
            RefreshSummary();
        }

        void RefreshSummary()
        {
            var s = SessionSetup.Current;
            if (summaryMode != null) summaryMode.text = MenuCatalog.FindMode(s.Mode).DisplayName;
            if (summaryScenario != null) summaryScenario.text = MenuCatalog.FindScenario(s.ScenarioId).DisplayName;
        }

        public static string ToId(TimeOfDay t)
        {
            switch (t)
            {
                case TimeOfDay.Dusk: return DuskId;
                case TimeOfDay.Night: return NightId;
                default: return DayId;
            }
        }

        public static TimeOfDay ToTimeOfDay(string id)
        {
            switch (id)
            {
                case DuskId: return TimeOfDay.Dusk;
                case NightId: return TimeOfDay.Night;
                default: return TimeOfDay.Day;
            }
        }
    }
}
