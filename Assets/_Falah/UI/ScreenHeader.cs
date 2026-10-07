using Falah.RovSim.Core;
using TMPro;
using UnityEngine;

namespace Falah.RovSim.UI
{
    /// <summary>Top bar chips (selected ROV, role, user name), refreshed from <see cref="SessionSetup.Current"/>.</summary>
    public sealed class ScreenHeader : MonoBehaviour
    {
        [SerializeField] GameObject rovChip;
        [SerializeField] TMP_Text rovText;
        [SerializeField] TMP_Text roleText;
        [SerializeField] TMP_Text userText;

        public void Refresh()
        {
            var s = SessionSetup.Current;
            if (rovText != null) rovText.text = MenuCatalog.FindRov(s.RovId).DisplayName;
            if (roleText != null) roleText.text = RoleLabel(s.Role);
            if (userText != null) userText.text = string.IsNullOrEmpty(s.UserName) ? "[Nama pengguna]" : s.UserName;
        }

        public static string RoleLabel(UserRole role)
        {
            switch (role)
            {
                case UserRole.Instructor: return "Instruktur";
                case UserRole.Administrator: return "Administrator";
                default: return "Trainee";
            }
        }
    }
}
