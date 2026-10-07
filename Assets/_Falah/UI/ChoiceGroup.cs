using System;
using System.Collections.Generic;
using UnityEngine;

namespace Falah.RovSim.UI
{
    /// <summary>Single-selection group of <see cref="Choice"/> cards.</summary>
    public sealed class ChoiceGroup : MonoBehaviour
    {
        [SerializeField] List<Choice> choices = new List<Choice>();

        string selectedId;

        public event Action<string> Changed;

        public string SelectedId => selectedId;

        void Awake()
        {
            foreach (var choice in choices)
            {
                var captured = choice;
                if (captured.Button != null)
                    captured.Button.onClick.AddListener(() => Select(captured.Id, true));
            }
        }

        public void Select(string id, bool notify)
        {
            var target = choices.Find(c => c.Id == id);
            if (target == null || !target.Selectable) return;
            selectedId = id;
            foreach (var c in choices) c.SetSelected(c == target);
            if (notify) Changed?.Invoke(id);
        }
    }
}
