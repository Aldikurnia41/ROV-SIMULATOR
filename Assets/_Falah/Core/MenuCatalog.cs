using System.Linq;

namespace Falah.RovSim.Core
{
    public readonly struct SpecRow
    {
        public readonly string Label;
        public readonly string Value;
        public readonly bool Mono;

        public SpecRow(string label, string value, bool mono = true)
        {
            Label = label;
            Value = value;
            Mono = mono;
        }
    }

    public sealed class RovOption
    {
        public string Id;
        public string DisplayName;
        public string Subtitle;
        public string[] Tags;
        /// <summary>Tags drawn in the "needs confirmation" style (amber).</summary>
        public string[] WarningTags = new string[0];
        public SpecRow[] Specs;
        /// <summary>False = shown but not selectable in the prototype (SPEC: only Tortuga and Teledyne).</summary>
        public bool Selectable;
        public float GlyphWidth = 150f;
    }

    public sealed class ModeOption
    {
        public string Id;
        public TrainingMode Mode;
        public string DisplayName;
        public string Description;
        public bool Selectable;
    }

    public enum LevelKind { Basic, Medium, Advanced }

    public sealed class ScenarioOption
    {
        public string Id;
        public string DisplayName;
        public string Description;
        public string LevelLabel;
        public LevelKind Level;
        public bool Selectable;
        public string[] Objectives;
        public string TimeLimit;
    }

    /// <summary>
    /// Static content of the menu screens. Values come from the UI design; anything not confirmed
    /// with Pushidrosal stays as the "[___]" placeholder (see CLAUDE.md).
    /// </summary>
    public static class MenuCatalog
    {
        public static readonly RovOption[] Rovs =
        {
            new RovOption
            {
                Id = "tortuga", DisplayName = "Tortuga", Subtitle = "Subsea Tech",
                Tags = new[] { "ROV inspeksi" }, Selectable = true, GlyphWidth = 150f,
                Specs = new[]
                {
                    new SpecRow("Kedalaman", "500 m"), new SpecRow("Thruster", "4 azimuth"),
                    new SpecRow("Tahan arus", "4 knot"), new SpecRow("Alat", "Sonar imaging", false),
                },
            },
            new RovOption
            {
                Id = "teledyne", DisplayName = "Teledyne", Subtitle = "Tipe: [___]",
                Tags = new[] { "Mini ROV (dugaan)" }, WarningTags = new[] { "Perlu konfirmasi" },
                Selectable = true, GlyphWidth = 120f,
                Specs = new[]
                {
                    new SpecRow("Kedalaman", "300 m"), new SpecRow("Thruster", "4 vektor + 2"),
                    new SpecRow("Kecepatan", "3 knot"), new SpecRow("Alat", "[___]", false),
                },
            },
            new RovOption
            {
                Id = "eca-hytec", DisplayName = "ECA Hytec", Subtitle = "Model: [___]",
                Tags = new[] { "Observasi sampai work class" }, WarningTags = new[] { "Perlu konfirmasi" },
                Selectable = false, GlyphWidth = 164f,
                Specs = new[]
                {
                    new SpecRow("Kedalaman", "300 sampai 1000 m"), new SpecRow("Kandidat", "H300 / H800 / H1000"),
                    new SpecRow("Manipulator", "[___]", false), new SpecRow("TMS", "[___]", false),
                },
            },
            new RovOption
            {
                Id = "mariner-xl", DisplayName = "Mariner XL", Subtitle = "Argus Remote Systems",
                Tags = new[] { "Work class" }, Selectable = false, GlyphWidth = 170f,
                Specs = new[]
                {
                    new SpecRow("Kedalaman", "1000 m"), new SpecRow("Thruster", "7 listrik"),
                    new SpecRow("Manipulator", "2 lengan", false), new SpecRow("Peluncuran", "LARS", false),
                },
            },
        };

        public static readonly ModeOption[] Modes =
        {
            new ModeOption { Id = "familiarization", Mode = TrainingMode.Familiarization, DisplayName = "Familiarisasi",
                Description = "Terbang bebas untuk mengenal kendali. Tanpa penilaian.", Selectable = false },
            new ModeOption { Id = "guided", Mode = TrainingMode.Guided, DisplayName = "Terbimbing",
                Description = "Tugas bertahap dengan petunjuk di layar.", Selectable = false },
            new ModeOption { Id = "full", Mode = TrainingMode.FullMission, DisplayName = "Misi penuh",
                Description = "Skenario lengkap dengan gangguan dan penilaian.", Selectable = true },
            new ModeOption { Id = "exam", Mode = TrainingMode.Exam, DisplayName = "Ujian",
                Description = "Skenario tetap tanpa petunjuk. Hasil tersimpan resmi.", Selectable = true },
        };

        public static readonly ScenarioOption[] Scenarios =
        {
            new ScenarioOption
            {
                Id = SessionSetup.DefaultScenarioId, DisplayName = "Investigasi target sonar",
                Description = "Mendekati kontak hasil side scan, identifikasi visual, catat posisi.",
                LevelLabel = "Dasar", Level = LevelKind.Basic, Selectable = true, TimeLimit = "[___] menit",
                Objectives = new[]
                {
                    "Menyelam dan menuju kontak sonar.",
                    "Aktifkan station keeping di dekat target.",
                    "Identifikasi objek secara visual dan catat posisi.",
                    "Akhiri sesi dan lanjut ke debrief.",
                },
            },
            new ScenarioOption
            {
                Id = "wreck-inspection", DisplayName = "Inspeksi bangkai kapal",
                Description = "Menyusuri objek, dokumentasi kondisi, hindari tether tersangkut.",
                LevelLabel = "Menengah", Level = LevelKind.Medium, Selectable = false, TimeLimit = "[___] menit",
                Objectives = new string[0],
            },
            new ScenarioOption
            {
                Id = "black-box", DisplayName = "Pencarian black box",
                Description = "Menemukan perangkat kecil di dasar laut dengan sonar dan lampu, mencatat posisinya, lalu kembali ke permukaan.",
                LevelLabel = "Menengah", Level = LevelKind.Medium, Selectable = true, TimeLimit = "45 menit",
                Objectives = new[]
                {
                    "Menuju area pencarian dan dekati kontak sonar.",
                    "Identifikasi black box secara visual (nyalakan lampu: L).",
                    "Catat posisi black box (M).",
                    "Kembali ke permukaan.",
                },
            },
            new ScenarioOption
            {
                Id = "channel-inspection", DisplayName = "Inspeksi alur dan dermaga",
                Description = "Menyusuri tiang, dinding, atau kabel dengan arus.",
                LevelLabel = "Menengah", Level = LevelKind.Medium, Selectable = false, TimeLimit = "[___] menit",
                Objectives = new string[0],
            },
            new ScenarioOption
            {
                Id = "fault-handling", DisplayName = "Penanganan gangguan",
                Description = "Kebocoran, thruster mati, lampu padam, tether tersangkut.",
                LevelLabel = "Lanjut", Level = LevelKind.Advanced, Selectable = false, TimeLimit = "[___] menit",
                Objectives = new string[0],
            },
        };

        public static RovOption FindRov(string id) => Rovs.FirstOrDefault(r => r.Id == id) ?? Rovs[0];

        public static ModeOption FindMode(TrainingMode mode) => Modes.FirstOrDefault(m => m.Mode == mode) ?? Modes[2];

        public static ScenarioOption FindScenario(string id) => Scenarios.FirstOrDefault(s => s.Id == id) ?? Scenarios[0];
    }
}
