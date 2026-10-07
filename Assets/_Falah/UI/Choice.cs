using UnityEngine;
using UnityEngine.UI;

namespace Falah.RovSim.UI
{
    /// <summary>One selectable card (ROV, mode, scenario, role, time of day). Visual state only.</summary>
    public sealed class Choice : MonoBehaviour
    {
        [SerializeField] string id;
        [SerializeField] Button button;
        [SerializeField] Image border;
        [SerializeField] Image fill;
        [SerializeField] RectTransform inner;
        [SerializeField] GameObject selectedBadge;
        [SerializeField] GameObject lockedOverlay;
        [SerializeField] bool selectable = true;
        [SerializeField] float normalBorder = 1f;
        [SerializeField] float selectedBorder = 2f;

        public string Id => id;
        public Button Button => button;
        public bool Selectable => selectable;

        public void SetSelected(bool selected)
        {
            border.color = selected ? UiTheme.Accent : UiTheme.BorderStrong;
            fill.color = selected ? UiTheme.CardSelected : UiTheme.Card;
            float w = selected ? selectedBorder : normalBorder;
            inner.offsetMin = new Vector2(w, w);
            inner.offsetMax = new Vector2(-w, -w);
            if (selectedBadge != null) selectedBadge.SetActive(selected);
        }

        void Awake()
        {
            if (lockedOverlay != null) lockedOverlay.SetActive(!selectable);
            if (button != null) button.interactable = selectable;
        }
    }
}
