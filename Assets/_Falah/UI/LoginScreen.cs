using Falah.RovSim.Core;
using TMPro;
using UnityEngine;

namespace Falah.RovSim.UI
{
    /// <summary>Dummy login: any non-empty user name is accepted. No account store, no network.</summary>
    public sealed class LoginScreen : MenuScreen
    {
        [SerializeField] TMP_InputField userField;
        [SerializeField] TMP_InputField passwordField;
        [SerializeField] ChoiceGroup roleGroup;
        [SerializeField] TMP_Text errorText;

        public const string TraineeId = "trainee";
        public const string InstructorId = "instructor";
        public const string AdministratorId = "administrator";

        void Awake()
        {
            if (errorText != null) errorText.text = string.Empty;
            if (roleGroup != null) roleGroup.Select(TraineeId, false);
            if (userField != null) userField.onValueChanged.AddListener(_ => { if (errorText != null) errorText.text = string.Empty; });
        }

        public override bool TryAdvance()
        {
            string user = userField != null ? userField.text.Trim() : string.Empty;
            if (user.Length == 0)
            {
                if (errorText != null) errorText.text = "Isi nama pengguna.";
                return false;
            }
            SessionSetup.ResetCurrent();
            var s = SessionSetup.Current;
            s.UserName = user;
            s.Role = ToRole(roleGroup != null ? roleGroup.SelectedId : TraineeId);
            return true;
        }

        public static UserRole ToRole(string id)
        {
            switch (id)
            {
                case InstructorId: return UserRole.Instructor;
                case AdministratorId: return UserRole.Administrator;
                default: return UserRole.Trainee;
            }
        }
    }
}
