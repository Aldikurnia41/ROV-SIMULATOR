using Falah.RovSim.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Falah.RovSim.Integration
{
    /// <summary>
    /// Reads the two control schemes at the same time (whichever is touched is used, values are summed and clamped) and
    /// feeds <see cref="RovThrusterDriver"/>. The bindings are built in code so the project's own Input Actions asset,
    /// which <see cref="RoVPhysics"/> still uses, is left untouched.
    ///
    ///  Keyboard:  W/S surge, A/D sway, E/Q heave up/down, Right/Left arrow yaw.
    ///  Gamepad:   left stick surge + sway, right stick X yaw, right stick Y heave.
    ///  Joystick:  stick surge + sway, twist yaw, hat up/down heave.
    /// </summary>
    public sealed class ControlInputReader : MonoBehaviour, IControlInput
    {
        [SerializeField] RovThrusterDriver driver;
        [Range(0f, 0.9f)] [SerializeField] float deadzone = 0.15f;
        [Range(0f, 1f)] [SerializeField] float expo = 0.4f;
        [Tooltip("Apply the ROV chosen in the menu (SessionSetup) to the driver's thruster layout at start.")]
        [SerializeField] bool useSessionRov = true;

        InputAction move;   // Vector2: x sway, y surge
        InputAction heave;  // float
        InputAction yaw;    // float

        public Vector3 Translation { get; private set; }
        public float Yaw { get; private set; }

        void Awake()
        {
            move = new InputAction("Move", InputActionType.Value, expectedControlType: "Vector2");
            move.AddCompositeBinding("2DVector").With("Up", "<Keyboard>/w").With("Down", "<Keyboard>/s").With("Left", "<Keyboard>/a").With("Right", "<Keyboard>/d");
            move.AddBinding("<Gamepad>/leftStick");
            move.AddBinding("<Joystick>/stick");

            heave = new InputAction("Heave", InputActionType.Value, expectedControlType: "Axis");
            heave.AddCompositeBinding("1DAxis").With("Positive", "<Keyboard>/e").With("Negative", "<Keyboard>/q");
            heave.AddBinding("<Gamepad>/rightStick/y");
            heave.AddCompositeBinding("1DAxis").With("Positive", "<Joystick>/hat/up").With("Negative", "<Joystick>/hat/down");

            yaw = new InputAction("Yaw", InputActionType.Value, expectedControlType: "Axis");
            yaw.AddCompositeBinding("1DAxis").With("Positive", "<Keyboard>/rightArrow").With("Negative", "<Keyboard>/leftArrow");
            yaw.AddBinding("<Gamepad>/rightStick/x");
            yaw.AddBinding("<Joystick>/twist");
        }

        void Start()
        {
            if (driver != null && useSessionRov) driver.SetLayout(SessionSetup.Current.RovId);
        }

        void OnEnable()
        {
            move.Enable();
            heave.Enable();
            yaw.Enable();
        }

        void OnDisable()
        {
            move.Disable();
            heave.Disable();
            yaw.Disable();
            Translation = Vector3.zero;
            Yaw = 0f;
            if (driver != null) driver.Command = Vector4.zero;
        }

        void OnDestroy()
        {
            move?.Dispose();
            heave?.Dispose();
            yaw?.Dispose();
        }

        void Update()
        {
            Vector2 stick = ControlShaper.Stick(move.ReadValue<Vector2>(), deadzone, expo);
            float h = ControlShaper.Axis(heave.ReadValue<float>(), deadzone, expo);
            float r = ControlShaper.Axis(yaw.ReadValue<float>(), deadzone, expo);
            Translation = new Vector3(stick.x, h, stick.y);
            Yaw = r;
            if (driver != null) driver.Command = new Vector4(Translation.x, Translation.y, Translation.z, Yaw);
        }
    }
}
