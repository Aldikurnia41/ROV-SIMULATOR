using System;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Falah.RovSim.UI
{
    /// <summary>Slider with a live value label ("1.5 kn").</summary>
    public sealed class SliderField : MonoBehaviour
    {
        [SerializeField] Slider slider;
        [SerializeField] TMP_Text valueLabel;
        [SerializeField] string unit = "";
        [SerializeField] string numberFormat = "0.#";

        public event Action<float> Changed;

        public float Value => slider.value;

        void Awake()
        {
            slider.onValueChanged.AddListener(OnValue);
        }

        public void SetValue(float value)
        {
            slider.SetValueWithoutNotify(value);
            Refresh(value);
        }

        void OnValue(float value)
        {
            Refresh(value);
            Changed?.Invoke(value);
        }

        void Refresh(float value)
        {
            valueLabel.text = value.ToString(numberFormat, CultureInfo.InvariantCulture) + unit;
        }
    }
}
