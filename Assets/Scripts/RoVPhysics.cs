using System.Collections.Generic;
using Falah.RovSim.Core;
using UnityEngine;
using UnityEngine.InputSystem;

public class RoVPhysics : MonoBehaviour
{
    public Rigidbody Rbd;
    public float PropForce;
    [Tooltip("Keyboard-driven forces of the original prototype. Turn off when a RovThrusterDriver drives this body.")]
    public bool UseLegacyInput = true;

    /// <summary>Current and visibility of the water; set by WaterEnvironmentDriver. Null means still water.</summary>
    public static IWaterEnvironment Water;
    public InputAction Forward, Backward, Left, Right, Up, Down;

    public InputAction TiltLeft, TiltRight, TiltUp, TiltDown;
    [Header("Fluid Properties")]
    public float waterDensity = 1000f; // kg/m^3

    [Header("Buoyancy Settings")]
    public Vector3 centerOfBuoyancyOffset = new Vector3(0, 0.2f, 0); 
    public float displacedVolume = 0.05f; // m^3 (Equal to mass / density for neutral buoyancy)

    [Header("Quadratic Drag Coefficients (Cd * Area)")]
    public Vector3 linearDragCoefficients = new Vector3(1.2f, 1.5f, 0.4f); // X (surge), Y (heave), Z (sway)
    public Vector3 angularDragCoefficients = new Vector3(0.8f, 0.8f, 0.8f);

    [Header("Added Mass Coefficients (Percentage of Rigid Body Mass)")]
    public Vector3 addedMassFactors = new Vector3(0.5f, 0.8f, 0.2f); // Extra fluid inertia factor per axis

    private Rigidbody rb;
    private float _buoyancyMgt;
    public Transform TetherEnd;
    public List<Propeller> Thrusters = new List<Propeller>(); //0 and 1 is rear one, 2-3 is sideways
    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        // Disable Unity's linear drag models
        rb.linearDamping = 0f;
        rb.angularDamping = 0f;
        //precalc
        _buoyancyMgt = waterDensity * displacedVolume * Mathf.Abs(Physics.gravity.y);
        
        Forward = InputSystem.actions.FindAction("forward");
        Backward = InputSystem.actions.FindAction("backward");
        Left = InputSystem.actions.FindAction("left");
        Right = InputSystem.actions.FindAction("right");
        Up = InputSystem.actions.FindAction("up");
        Down = InputSystem.actions.FindAction("down");
        TiltLeft = InputSystem.actions.FindAction("tilt_left");
        TiltRight = InputSystem.actions.FindAction("tilt_right");
    }

    void FixedUpdate()
    {

        if (UseLegacyInput) DetectInput();
        ApplyBuoyancy();
        ApplyQuadraticDrag();
    }

    private void DetectInput()
    {
        if (Left.IsPressed()) Rbd.AddForce(transform.right * PropForce);
        if (Right.IsPressed()) Rbd.AddForce(transform.right * -PropForce);
        
        if (Up.IsPressed())
        {
            if(Rbd.transform.position.y < 0.0f)
            {
                Rbd.AddForce(transform.up * PropForce * HeaveEfficiency);
                Thrusters[2].Rpm = 600 * ThrusterEfficiency(2);
                Thrusters[3].Rpm = 600 * ThrusterEfficiency(3);
            }
        }
        else if (Down.IsPressed())
        {
            Rbd.AddForce(transform.up * -PropForce * HeaveEfficiency);
            Thrusters[2].Rpm = -600 * ThrusterEfficiency(2);
            Thrusters[3].Rpm = -600 * ThrusterEfficiency(3);
        }
        else
        {
            Thrusters[2].Rpm = 0;
            Thrusters[3].Rpm = 0;
        }

        if (TiltLeft.IsPressed())
        {
            Rbd.AddTorque(Vector3.up*-PropForce*0.1f);
        }
        else if (TiltRight.IsPressed())
        {
            Rbd.AddTorque(Vector3.up*PropForce*0.1f);
        }
        if (Forward.IsPressed()) {
            Rbd.AddForce(transform.forward * 4.0f * -PropForce * SurgeEfficiency); //It has 4 props}
            Thrusters[0].Rpm = 600 * ThrusterEfficiency(0);
            Thrusters[1].Rpm = -600 * ThrusterEfficiency(1);
        }
        else if (Backward.IsPressed())
        {
            Rbd.AddForce(transform.forward * 4.0f * PropForce * SurgeEfficiency);
            Thrusters[0].Rpm = -600 * ThrusterEfficiency(0);
            Thrusters[1].Rpm = 600 * ThrusterEfficiency(1);
        }
        else
        {
            Thrusters[0].Rpm = 0;
            Thrusters[1].Rpm = 0;
        }
    }

    private void ApplyBuoyancy()
    {
        if (Rbd.transform.position.y > 0.0f)
        {

        }
        else
        {
            Vector3 cbWorldPosition = transform.TransformPoint(centerOfBuoyancyOffset);
            rb.AddForceAtPosition(Vector3.up * _buoyancyMgt, cbWorldPosition, ForceMode.Force);
        }
    }

    private void ApplyQuadraticDrag()
    {
        // 1. Linear Quadratic Drag, on the velocity relative to the water (the current carries a free ROV along)
        Vector3 current = Water != null ? Water.CurrentAt(rb.position, Time.time) : Vector3.zero;
        Vector3 localVel = transform.InverseTransformDirection(rb.linearVelocity - current);
        Vector3 localDragForce = -0.5f * waterDensity * new Vector3(
            linearDragCoefficients.x * localVel.x * Mathf.Abs(localVel.x),
            linearDragCoefficients.y * localVel.y * Mathf.Abs(localVel.y),
            linearDragCoefficients.z * localVel.z * Mathf.Abs(localVel.z)
        );

        rb.AddForce(transform.TransformDirection(localDragForce), ForceMode.Force);

        // 2. Angular Quadratic Drag
        Vector3 localAngVel = transform.InverseTransformDirection(rb.angularVelocity);
        Vector3 localDragTorque = -0.5f * waterDensity * new Vector3(
            angularDragCoefficients.x * localAngVel.x * Mathf.Abs(localAngVel.x),
            angularDragCoefficients.y * localAngVel.y * Mathf.Abs(localAngVel.y),
            angularDragCoefficients.z * localAngVel.z * Mathf.Abs(localAngVel.z)
        );

        rb.AddTorque(transform.TransformDirection(localDragTorque), ForceMode.Force);
    }

    // Helper method to pass control thruster forces while accounting for Added Mass
    public void AddThrusterForce(Vector3 localForce, Vector3 localPoint)
    {
        // Scale force down per axis to simulate fluid inertia (Added Mass)
        Vector3 compensatedLocalForce = new Vector3(
            localForce.x / (1f + addedMassFactors.x),
            localForce.y / (1f + addedMassFactors.y),
            localForce.z / (1f + addedMassFactors.z)
        );

        Vector3 worldForce = transform.TransformDirection(compensatedLocalForce);
        Vector3 worldPoint = transform.TransformPoint(localPoint);

        rb.AddForceAtPosition(worldForce, worldPoint, ForceMode.Force);
    }

    // Thruster faults injected by the instructor (Falah.RovSim.Core.ThrusterFaultModel). Thrusters 0-1 drive surge,
    // 2-3 drive heave (see the Thrusters comment above); sway is not tied to a thruster yet.
    public float SurgeEfficiency => ThrusterFaultModel.Pair(SessionLog.Current, 0, 1);
    public float HeaveEfficiency => ThrusterFaultModel.Pair(SessionLog.Current, 2, 3);

    private static float ThrusterEfficiency(int index) => ThrusterFaultModel.Efficiency(index, SessionLog.Current);

    void EnableGravity(bool value)
    {
        Rbd.useGravity = value;
    }
}
