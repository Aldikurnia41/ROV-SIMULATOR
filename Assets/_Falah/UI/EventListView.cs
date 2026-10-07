using System.Collections.Generic;
using Falah.RovSim.Core;
using TMPro;
using UnityEngine;

namespace Falah.RovSim.UI
{
    /// <summary>
    /// Vertical list of timeline rows (time + message) built by cloning an inactive template row.
    /// The template must contain two TMP_Text children: the time first, the message second.
    /// </summary>
    public sealed class EventListView : MonoBehaviour
    {
        [SerializeField] RectTransform container;
        [SerializeField] GameObject rowTemplate;
        [SerializeField] int maxRows = 8;

        readonly List<GameObject> rows = new List<GameObject>();

        void Awake()
        {
            if (rowTemplate != null) rowTemplate.SetActive(false);
        }

        /// <summary>Shows the most recent <see cref="maxRows"/> events, oldest at the top.</summary>
        public void Show(IReadOnlyList<SimEvent> events)
        {
            foreach (var r in rows) Destroy(r);
            rows.Clear();
            int start = Mathf.Max(0, events.Count - maxRows);
            for (int i = start; i < events.Count; i++)
            {
                var row = Instantiate(rowTemplate, container);
                row.name = "Row" + i;
                row.SetActive(true);
                var texts = row.GetComponentsInChildren<TMP_Text>(true);
                var e = events[i];
                texts[0].text = SessionLog.FormatTime(e.Time);
                texts[1].text = e.Message;
                Color c = Tint(e.Severity);
                texts[0].color = e.Severity == EventSeverity.Info ? UiTheme.TextMuted : c;
                texts[1].color = e.Severity == EventSeverity.Info ? UiTheme.Text : c;
                rows.Add(row);
            }
        }

        public static Color Tint(EventSeverity severity)
        {
            switch (severity)
            {
                case EventSeverity.Warning: return UiTheme.Warn;
                case EventSeverity.Critical: return UiTheme.Danger;
                case EventSeverity.Success: return UiTheme.Ok;
                default: return UiTheme.Text;
            }
        }
    }
}
