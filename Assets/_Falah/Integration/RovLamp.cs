using Falah.RovSim.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Falah.RovSim.Integration
{
    /// <summary>
    /// Operator controls that are not thrust: lamp (L on/off, PageUp/PageDown brightness; gamepad button west toggles)
    /// and the position mark (Enter; gamepad button north). Keeps a spot light on the active vehicle in step with
    /// <see cref="LampState"/> and mirrors the instructor's "Lampu padam" switch into it.
    /// </summary>
    public sealed class RovLamp : MonoBehaviour
    {
        [SerializeField] float maxIntensity = 6000f;
        [SerializeField] float range = 40f;
        [SerializeField] float spotAngle = 70f;
        [SerializeField] float searchInterval = 1f;

        RoVPhysics rov;
        Light lamp;
        float nextSearch;
        bool wasLightsOut;

        void Awake()
        {
            LampState.ResetCurrent();
        }

        void Update()
        {
            var state = LampState.Current;
            var keyboard = Keyboard.current;
            var pad = Gamepad.current;
            OperatorMark.Pressed = (keyboard != null && keyboard.enterKey.wasPressedThisFrame) || (pad != null && pad.buttonNorth.wasPressedThisFrame);
            if ((keyboard != null && keyboard.lKey.wasPressedThisFrame) || (pad != null && pad.buttonWest.wasPressedThisFrame)) state.Toggle();
            if (keyboard != null)
            {
                if (keyboard.pageUpKey.wasPressedThisFrame) state.Step(1);
                if (keyboard.pageDownKey.wasPressedThisFrame) state.Step(-1);
            }

            bool lightsOut = SessionSetup.Current.Disturbances.LightsOut;
            if (lightsOut != wasLightsOut)
            {
                wasLightsOut = lightsOut;
                state.Failed = lightsOut;
            }

            if (rov == null || !rov.isActiveAndEnabled)
            {
                rov = null;
                lamp = null;
                if (Time.unscaledTime >= nextSearch)
                {
                    nextSearch = Time.unscaledTime + searchInterval;
                    rov = FindFirstObjectByType<RoVPhysics>();
                    if (rov != null) lamp = CreateLamp(rov.transform);
                }
            }
            if (lamp != null)
            {
                lamp.intensity = state.Level * maxIntensity;
                lamp.enabled = state.Level > 0f;
            }
        }

        Light CreateLamp(Transform vehicle)
        {
            var existing = vehicle.Find("RovLamp");
            var go = existing != null ? existing.gameObject : new GameObject("RovLamp");
            go.transform.SetParent(vehicle, false);
            go.transform.localRotation = Quaternion.Euler(0f, 180f, 0f); // the scene ROV model faces -Z
            var l = go.GetComponent<Light>() ?? go.AddComponent<Light>();
            l.type = LightType.Spot;
            l.range = range;
            l.spotAngle = spotAngle;
            l.color = new Color(1f, 0.96f, 0.88f);
            return l;
        }
    }
}
