using System;
using Falah.RovSim.Core;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Falah.RovSim.UI
{
    /// <summary>
    /// Debrief (design "7. Debrief dan laporan"): duration, key events, the recorded track, the instructor note and a
    /// saved JSON report. Scores stay "[__]" until the weights are agreed (SPEC 2); replay (T4.2) and PDF export are
    /// out of the prototype and their buttons are locked.
    /// </summary>
    public sealed class DebriefScreen : MonoBehaviour
    {
        [SerializeField] string menuScene = "Menu";

        [Header("Header")]
        [SerializeField] TMP_Text subtitle;

        [Header("Score")]
        [SerializeField] TMP_Text durationText;
        [SerializeField] TMP_Text noteText;

        [Header("Views")]
        [SerializeField] EventListView keyEvents;
        [SerializeField] TrackMap map;

        [Header("Replay bar (display only)")]
        [SerializeField] TMP_Text replayElapsed;
        [SerializeField] TMP_Text replayTotal;
        [SerializeField] RectTransform markerParent;
        [SerializeField] Image markerTemplate;

        [Header("Actions")]
        [SerializeField] Button saveButton;
        [SerializeField] TMP_Text saveStatus;
        [SerializeField] Button menuButton;

        void Awake()
        {
            saveButton.onClick.AddListener(Save);
            menuButton.onClick.AddListener(BackToMenu);
            if (markerTemplate != null) markerTemplate.gameObject.SetActive(false);
        }

        void OnEnable()
        {
            var s = SessionSetup.Current;
            var log = SessionLog.Current;
            var scenario = MenuCatalog.FindScenario(s.ScenarioId);
            string trainee = string.IsNullOrEmpty(s.UserName) ? "[Nama trainee]" : s.UserName;
            subtitle.text = MenuCatalog.FindRov(s.RovId).DisplayName + " · " + scenario.DisplayName + " · " + trainee;

            int limit = PilotHud.ParseMinutes(scenario.TimeLimit) * 60;
            durationText.text = "Durasi " + SessionLog.FormatTime(log.Elapsed) + " dari " + (limit > 0 ? SessionLog.FormatTime(limit) : "--:--") +
                                (log.ObjectivesTotal > 0 ? "\nObjektif " + log.ObjectivesDone + " dari " + log.ObjectivesTotal + " selesai" : string.Empty);
            noteText.text = string.IsNullOrWhiteSpace(log.InstructorNote) ? "Catatan instruktur: (tidak ada)" : "Catatan instruktur: " + log.InstructorNote;

            keyEvents.Show(log.Events);
            map.ShowTrack(log.Track, log.Events);

            replayElapsed.text = SessionLog.FormatTime(0f);
            replayTotal.text = SessionLog.FormatTime(log.Elapsed);
            BuildMarkers(log);
            saveStatus.text = string.Empty;
        }

        void BuildMarkers(SessionLog log)
        {
            if (markerTemplate == null || log.Elapsed <= 0f) return;
            for (int i = markerParent.childCount - 1; i >= 0; i--)
                if (markerParent.GetChild(i).gameObject != markerTemplate.gameObject) Destroy(markerParent.GetChild(i).gameObject);
            foreach (var e in log.Events)
            {
                if (e.Severity != EventSeverity.Warning && e.Severity != EventSeverity.Critical) continue;
                var marker = Instantiate(markerTemplate, markerParent);
                marker.gameObject.SetActive(true);
                marker.color = EventListView.Tint(e.Severity);
                var rt = (RectTransform)marker.transform;
                float t = Mathf.Clamp01(e.Time / log.Elapsed);
                rt.anchorMin = rt.anchorMax = new Vector2(t, 0.5f);
                rt.anchoredPosition = Vector2.zero;
            }
        }

        void Save()
        {
            try
            {
                var report = SessionReport.Build(SessionSetup.Current, SessionLog.Current, DateTime.UtcNow);
                string path = report.Save(Application.persistentDataPath);
                saveStatus.text = "Tersimpan: " + path;
                saveStatus.color = UiTheme.Ok;
            }
            catch (Exception ex)
            {
                saveStatus.text = "Gagal menyimpan: " + ex.Message;
                saveStatus.color = UiTheme.Danger;
            }
        }

        void BackToMenu()
        {
            SessionLog.ResetCurrent();
            SceneManager.LoadScene(menuScene);
        }
    }
}
