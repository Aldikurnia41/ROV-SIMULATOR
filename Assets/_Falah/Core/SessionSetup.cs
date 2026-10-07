namespace Falah.RovSim.Core
{
    public enum UserRole { Trainee, Instructor, Administrator }

    public enum TrainingMode { Familiarization, Guided, FullMission, Exam }

    public enum TimeOfDay { Day, Dusk, Night }

    /// <summary>Disturbances the instructor may schedule for a session.</summary>
    public sealed class DisturbanceFlags
    {
        public bool ThrusterLeak = true;
        public bool LightsOut;
        public bool TetherSnag;
    }

    /// <summary>
    /// What the user chose in the menu flow (role, ROV, mode, scenario, environment).
    /// Plain C# so it can be unit tested; the simulation scene reads <see cref="Current"/>.
    /// </summary>
    public sealed class SessionSetup
    {
        public const string DefaultRovId = "tortuga";
        public const string DefaultScenarioId = "sonar-contact";
        public const string DefaultLocation = "Selat Makassar (lepas delta Mahakam)";

        public static SessionSetup Current { get; private set; } = new SessionSetup();

        public string UserName = string.Empty;
        public UserRole Role = UserRole.Trainee;
        public string RovId = DefaultRovId;
        public TrainingMode Mode = TrainingMode.FullMission;
        public string ScenarioId = DefaultScenarioId;
        public string LocationLabel = DefaultLocation;
        public float CurrentKnots = 1.5f;
        public float CurrentDirectionDegrees = 45f;
        public float VisibilityMeters = 4f;
        public float TargetDepthMeters = 32f;
        public TimeOfDay TimeOfDay = TimeOfDay.Day;
        public DisturbanceFlags Disturbances = new DisturbanceFlags();
        public bool GamepadCalibrated;

        /// <summary>Raised when the instructor changes current or visibility during the session.</summary>
        public static event System.Action EnvironmentChanged;

        public static void ResetCurrent() => Current = new SessionSetup();

        public void NotifyEnvironmentChanged() => EnvironmentChanged?.Invoke();
    }
}
