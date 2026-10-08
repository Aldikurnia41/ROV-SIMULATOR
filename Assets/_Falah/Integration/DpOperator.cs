using Falah.RovSim.Core;
using Falah.RovSim.UI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Falah.RovSim.Integration
{
    /// <summary>
    /// Station keeping on the scene ROV: engages the DP loops from the HUD chips (DP HOLD, AUTO DEPTH, AUTO HDG) or from
    /// the keys C, X and Z, reads the vehicle state from telemetry and the Rigidbody, and gives the result to
    /// <see cref="RovThrusterDriver.AssistWrench"/>. Gains come from a <see cref="DpTuning"/> asset (defaults when unset).
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public sealed class DpOperator : MonoBehaviour
    {
        [SerializeField] RovThrusterDriver driver;
        [SerializeField] MonoBehaviour telemetrySource;
        [SerializeField] DpTuning tuning;
        [SerializeField] ModeChip holdChip;
        [SerializeField] ModeChip depthChip;
        [SerializeField] ModeChip headingChip;

        Rigidbody body;
        ITelemetrySource source;
        DpController dp;
        InputAction hold, depth, heading;

        public DpController Controller => dp;

        void Awake()
        {
            body = GetComponent<Rigidbody>();
            source = telemetrySource as ITelemetrySource;
            var set = (tuning != null ? tuning.For(SessionSetup.Current.RovId) : DpTuning.Defaults(SessionSetup.Current.RovId));
            dp = new DpController(set.Heading, set.Depth, set.Position);

            hold = new InputAction("DpHold", InputActionType.Button, "<Keyboard>/c");
            depth = new InputAction("DpDepth", InputActionType.Button, "<Keyboard>/x");
            heading = new InputAction("DpHeading", InputActionType.Button, "<Keyboard>/z");
        }

        void OnEnable()
        {
            hold.Enable(); depth.Enable(); heading.Enable();
            Bind(holdChip, DpMode.Position, "posisi");
            Bind(depthChip, DpMode.Depth, "kedalaman");
            Bind(headingChip, DpMode.Heading, "heading");
        }

        void OnDisable()
        {
            hold.Disable(); depth.Disable(); heading.Disable();
            if (driver != null) driver.AssistWrench = default;
        }

        void OnDestroy()
        {
            hold?.Dispose(); depth?.Dispose(); heading?.Dispose();
        }

        void Start()
        {
            // The design shows sample chips as on; station keeping starts released.
            foreach (var chip in new[] { holdChip, depthChip, headingChip }) if (chip != null) chip.Set(false, false);
        }

        void Bind(ModeChip chip, DpMode mode, string label)
        {
            if (chip == null) return;
            chip.Changed += on => Engage(mode, on, label);
        }

        void Engage(DpMode mode, bool on, string label)
        {
            if (!TryMeasure(out var m)) return;
            dp.SetMode(mode, on, m);
            var log = SessionLog.Current;
            if (on) log.Add(SimEventType.DpEngaged, "Station keeping aktif: " + label, EventSeverity.Info);
            else log.Add(SimEventType.DpReleased, "Station keeping dilepas: " + label, EventSeverity.Info);
        }

        void Update()
        {
            Toggle(hold, holdChip);
            Toggle(depth, depthChip);
            Toggle(heading, headingChip);
        }

        static void Toggle(InputAction action, ModeChip chip)
        {
            if (chip != null && action.WasPressedThisFrame()) chip.Set(!chip.IsOn, true);
        }

        void FixedUpdate()
        {
            if (driver == null) return;
            if (dp.Modes == DpMode.None || !TryMeasure(out var m)) { driver.AssistWrench = default; return; }
            driver.AssistWrench = dp.Update(m, Time.fixedDeltaTime);
        }

        bool TryMeasure(out DpMeasurement m)
        {
            m = default;
            if (source == null || !source.TryGetSample(out var s)) return false;
            m = new DpMeasurement
            {
                Position = s.Position,
                Velocity = body.linearVelocity,
                HeadingDegrees = s.HeadingDegrees,
                YawRateDegrees = body.angularVelocity.y * Mathf.Rad2Deg,
                Depth = s.DepthMeters,
                DepthRate = -body.linearVelocity.y,
            };
            return true;
        }
    }
}
