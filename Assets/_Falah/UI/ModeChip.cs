using System;
using UnityEngine;
using UnityEngine.UI;

namespace Falah.RovSim.UI
{
    /// <summary>Toggle chip used for the HUD control modes (DP HOLD, AUTO DEPTH, AUTO HDG).</summary>
    public sealed class ModeChip : MonoBehaviour
    {
        [SerializeField] Button button;
        [SerializeField] Image border;
        [SerializeField] Image fill;
        [SerializeField] Graphic label;
        [SerializeField] bool startsOn;

        bool isOn;

        public event Action<bool> Changed;

        public bool IsOn => isOn;

        void Awake()
        {
            button.onClick.AddListener(() => Set(!isOn, true));
            Set(startsOn, false);
        }

        public void Set(bool on, bool notify)
        {
            isOn = on;
            border.color = on ? UiTheme.Accent : UiTheme.BorderStrong;
            fill.color = on ? UiTheme.CardSelected : new Color(0.024f, 0.055f, 0.094f, 0.72f);
            label.color = on ? UiTheme.Text : UiTheme.TextSoft;
            if (notify) Changed?.Invoke(on);
        }
    }
}
