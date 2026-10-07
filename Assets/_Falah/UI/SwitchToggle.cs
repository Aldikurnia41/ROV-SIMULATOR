using System;
using UnityEngine;
using UnityEngine.UI;

namespace Falah.RovSim.UI
{
    /// <summary>On/off switch (track + knob).</summary>
    public sealed class SwitchToggle : MonoBehaviour
    {
        [SerializeField] Button button;
        [SerializeField] Image track;
        [SerializeField] RectTransform knob;
        [SerializeField] Image knobImage;

        bool isOn;

        public event Action<bool> Changed;

        public bool IsOn => isOn;

        void Awake()
        {
            button.onClick.AddListener(() => Set(!isOn, true));
        }

        public void Set(bool on, bool notify)
        {
            isOn = on;
            track.color = on ? UiTheme.Accent : UiTheme.Border;
            knobImage.color = on ? UiTheme.OnAccent : UiTheme.TextMuted;
            float x = on ? 1f : 0f;
            knob.anchorMin = knob.anchorMax = new Vector2(x, 0.5f);
            knob.pivot = new Vector2(x, 0.5f);
            knob.anchoredPosition = new Vector2(on ? -3f : 3f, 0f);
            if (notify) Changed?.Invoke(on);
        }
    }
}
