using Falah.RovSim.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Falah.RovSim.UI
{
    /// <summary>
    /// Runs in the simulation scene: starts a fresh <see cref="SessionLog"/>, advances session time, samples the
    /// vehicle track once a second, shows the instructor station for instructors, and ends the session into the debrief.
    /// Keys: F2 shows/hides the instructor station, F10 ends the session.
    /// </summary>
    public sealed class SessionController : MonoBehaviour
    {
        [SerializeField] MonoBehaviour telemetrySource;
        [SerializeField] GameObject instructorCanvas;
        [SerializeField] string debriefScene = "Debrief";
        [SerializeField] float trackInterval = 1f;

        ITelemetrySource source;
        float nextTrackAt;

        public bool Paused { get; private set; }

        void Awake()
        {
            source = telemetrySource as ITelemetrySource;
        }

        void Start()
        {
            Time.timeScale = 1f;
            SessionLog.ResetCurrent();
            SessionLog.Current.Add(SimEventType.SessionStart, "Sesi dimulai, ROV diturunkan", EventSeverity.Info);
            var role = SessionSetup.Current.Role;
            if (instructorCanvas != null) instructorCanvas.SetActive(role == UserRole.Instructor || role == UserRole.Administrator);
        }

        void Update()
        {
            var log = SessionLog.Current;
            if (log.Ended) return;
            log.Elapsed += Time.deltaTime;

            if (log.Elapsed >= nextTrackAt && source != null && source.TryGetSample(out var sample))
            {
                log.AddTrack(sample.Position);
                nextTrackAt = log.Elapsed + trackInterval;
            }

            var keyboard = Keyboard.current;
            if (keyboard == null) return;
            if (keyboard.f2Key.wasPressedThisFrame && instructorCanvas != null) instructorCanvas.SetActive(!instructorCanvas.activeSelf);
            if (keyboard.f10Key.wasPressedThisFrame) EndSession();
        }

        public void SetPaused(bool paused)
        {
            if (Paused == paused || SessionLog.Current.Ended) return;
            Paused = paused;
            Time.timeScale = paused ? 0f : 1f;
            SessionLog.Current.Add(paused ? SimEventType.SessionPaused : SimEventType.SessionResumed,
                paused ? "Sesi dijeda oleh instruktur" : "Sesi dilanjutkan", EventSeverity.Info);
        }

        public void EndSession()
        {
            var log = SessionLog.Current;
            if (log.Ended) return;
            Time.timeScale = 1f;
            Paused = false;
            log.Add(SimEventType.SessionEnd, "Sesi diakhiri", EventSeverity.Info);
            log.Ended = true;
            SceneManager.LoadScene(debriefScene);
        }

        void OnDestroy()
        {
            Time.timeScale = 1f;
        }
    }
}
