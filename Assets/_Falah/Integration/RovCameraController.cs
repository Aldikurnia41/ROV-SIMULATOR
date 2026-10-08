using Falah.RovSim.Core;
using Falah.RovSim.UI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Falah.RovSim.Integration
{
    /// <summary>
    /// Pilot camera (FOV and tilt from <see cref="RovCameraSettings"/> of the chosen ROV, tilt on R/F or d-pad up/down) and
    /// a debug orbit camera toggled with F3 (right mouse button drags, wheel zooms). Bindings are built in code so the
    /// project's Input Actions asset stays untouched.
    /// </summary>
    public sealed class RovCameraController : MonoBehaviour
    {
        [SerializeField] Camera pilotCamera;
        [Tooltip("Yaw of the pilot camera relative to the vehicle's +Z axis (180 when the model faces -Z).")]
        [SerializeField] float pilotYaw = 180f;
        [SerializeField] PilotHud hud;
        [SerializeField] float orbitDistance = 8f;
        [SerializeField] float orbitHeight = 1f;

        Camera orbitCamera;
        InputAction tilt;
        InputAction toggleOrbit;
        InputAction orbitDrag;
        InputAction orbitLook;
        InputAction orbitZoom;

        RovCameraSettings settings;
        float tiltDegrees;
        float orbitYaw = 200f;
        float orbitPitch = 20f;

        public bool OrbitActive => orbitCamera != null && orbitCamera.enabled;
        public float Tilt => tiltDegrees;

        void Awake()
        {
            tilt = new InputAction("CameraTilt", InputActionType.Value, expectedControlType: "Axis");
            tilt.AddCompositeBinding("1DAxis").With("Positive", "<Keyboard>/r").With("Negative", "<Keyboard>/f");
            tilt.AddCompositeBinding("1DAxis").With("Positive", "<Gamepad>/dpad/up").With("Negative", "<Gamepad>/dpad/down");

            toggleOrbit = new InputAction("OrbitToggle", InputActionType.Button, "<Keyboard>/f3");
            orbitDrag = new InputAction("OrbitDrag", InputActionType.Button, "<Mouse>/rightButton");
            orbitLook = new InputAction("OrbitLook", InputActionType.Value, "<Mouse>/delta", expectedControlType: "Vector2");
            orbitZoom = new InputAction("OrbitZoom", InputActionType.Value, "<Mouse>/scroll/y", expectedControlType: "Axis");

            // The orbit camera shares Display 1 with the pilot camera and draws above it while active.
            var go = new GameObject("DebugOrbitCamera") { hideFlags = HideFlags.HideAndDontSave };
            orbitCamera = go.AddComponent<Camera>();
            orbitCamera.targetDisplay = pilotCamera != null ? pilotCamera.targetDisplay : 0;
            orbitCamera.depth = 50f;
            orbitCamera.nearClipPlane = 0.2f;
            orbitCamera.farClipPlane = pilotCamera != null ? pilotCamera.farClipPlane : 1000f;
            orbitCamera.enabled = false;
        }

        void Start()
        {
            settings = RovCameraSettings.For(SessionSetup.Current.RovId);
            tiltDegrees = settings.DefaultTilt;
            if (pilotCamera != null) pilotCamera.fieldOfView = settings.FieldOfView;
            ApplyTilt();
        }

        void OnEnable()
        {
            tilt.Enable(); toggleOrbit.Enable(); orbitDrag.Enable(); orbitLook.Enable(); orbitZoom.Enable();
        }

        void OnDisable()
        {
            tilt.Disable(); toggleOrbit.Disable(); orbitDrag.Disable(); orbitLook.Disable(); orbitZoom.Disable();
            if (orbitCamera != null) orbitCamera.enabled = false;
        }

        void OnDestroy()
        {
            tilt?.Dispose(); toggleOrbit?.Dispose(); orbitDrag?.Dispose(); orbitLook?.Dispose(); orbitZoom?.Dispose();
            if (orbitCamera != null) Destroy(orbitCamera.gameObject);
        }

        void Update()
        {
            if (settings == null) return;
            tiltDegrees = CameraTilt.Step(tiltDegrees, tilt.ReadValue<float>(), settings.TiltRate, Time.unscaledDeltaTime, settings.MinTilt, settings.MaxTilt);
            ApplyTilt();

            if (toggleOrbit.WasPressedThisFrame()) orbitCamera.enabled = !orbitCamera.enabled;
        }

        void LateUpdate()
        {
            if (!OrbitActive) return;
            if (orbitDrag.IsPressed())
            {
                Vector2 look = orbitLook.ReadValue<Vector2>();
                orbitYaw += look.x * 0.2f;
                orbitPitch = CameraTilt.OrbitPitch(orbitPitch - look.y * 0.2f);
            }
            orbitDistance = CameraTilt.OrbitDistance(orbitDistance, orbitZoom.ReadValue<float>() * 0.002f, 2f, 60f);
            Vector3 focus = transform.position + Vector3.up * orbitHeight;
            Quaternion rotation = Quaternion.Euler(orbitPitch, orbitYaw, 0f);
            orbitCamera.transform.SetPositionAndRotation(focus - rotation * Vector3.forward * orbitDistance, rotation);
        }

        void ApplyTilt()
        {
            if (pilotCamera != null)
                pilotCamera.transform.localRotation = Quaternion.Euler(-tiltDegrees, pilotYaw, 0f);
            if (hud != null) hud.SetCameraTilt(tiltDegrees);
        }
    }
}
