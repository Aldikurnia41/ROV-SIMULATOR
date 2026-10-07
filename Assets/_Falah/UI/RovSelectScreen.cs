using Falah.RovSim.Core;
using TMPro;
using UnityEngine;

namespace Falah.RovSim.UI
{
    public sealed class RovSelectScreen : MenuScreen
    {
        [SerializeField] ChoiceGroup rovGroup;
        [SerializeField] TMP_Text selectedLabel;

        void Awake()
        {
            rovGroup.Changed += OnChanged;
        }

        public override void OnShow()
        {
            base.OnShow();
            rovGroup.Select(SessionSetup.Current.RovId, false);
            if (string.IsNullOrEmpty(rovGroup.SelectedId)) rovGroup.Select(SessionSetup.DefaultRovId, false);
            Refresh();
        }

        void OnChanged(string id)
        {
            SessionSetup.Current.RovId = id;
            Refresh();
            if (header != null) header.Refresh();
        }

        void Refresh()
        {
            string id = string.IsNullOrEmpty(rovGroup.SelectedId) ? SessionSetup.Current.RovId : rovGroup.SelectedId;
            if (selectedLabel != null) selectedLabel.text = MenuCatalog.FindRov(id).DisplayName;
        }

        public override bool TryAdvance()
        {
            if (!string.IsNullOrEmpty(rovGroup.SelectedId)) SessionSetup.Current.RovId = rovGroup.SelectedId;
            return true;
        }
    }
}
