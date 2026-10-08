using Falah.RovSim.Core;
using UnityEngine;

namespace Falah.RovSim.Integration
{
    /// <summary>
    /// Applies <see cref="ThrusterModel"/> thrust to a Rigidbody with AddForceAtPosition (T1.2). The command is a
    /// normalised 4-axis input (surge, sway, heave, yaw; -1..1) scaled by the ROV profile's full-command force and torque.
    /// Injected faults from the session log reduce each thruster's efficiency. Disabled-by-default on the legacy ROV
    /// input: <see cref="RoVPhysics.UseLegacyInput"/> must be off when this drives the same body.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public sealed class RovThrusterDriver : MonoBehaviour
    {
        [SerializeField] string rovId = SessionSetup.DefaultRovId;
        [Tooltip("Yaw of the thruster frame relative to the transform; 180 when the model faces -Z (the scene ROV does).")]
        [SerializeField] float bodyYaw;

        Rigidbody body;
        ThrusterModel model;
        RovProfile profile;
        float[] efficiency;

        /// <summary>x = sway, y = heave, z = surge, w = yaw; each -1..1.</summary>
        public Vector4 Command;

        /// <summary>Extra wrench in the thruster frame (newtons, N·m) added to the operator's, e.g. from station keeping.</summary>
        public Wrench AssistWrench;

        public ThrusterModel Model => model;

        public RovProfile Profile => profile;

        /// <summary>Loads the thruster layout and force scale of the ROV profile with this id.</summary>
        public void SetLayout(string id)
        {
            rovId = id;
            profile = RovProfiles.Get(id);
            model = new ThrusterModel(profile.Thrusters);
            efficiency = new float[model.Count];
        }

        void Awake()
        {
            body = GetComponent<Rigidbody>();
            SetLayout(rovId);
        }

        void FixedUpdate()
        {
            var log = SessionLog.Current;
            for (int i = 0; i < efficiency.Length; i++) efficiency[i] = ThrusterFaultModel.Efficiency(i, log);

            var cmd = new Vector4(Mathf.Clamp(Command.x, -1f, 1f), Mathf.Clamp(Command.y, -1f, 1f), Mathf.Clamp(Command.z, -1f, 1f), Mathf.Clamp(Command.w, -1f, 1f));
            Vector3 full = profile.FullForce;
            var wrench = new Wrench(
                new Vector3(cmd.x * full.x, cmd.y * full.y, cmd.z * full.z) + AssistWrench.Force,
                new Vector3(0f, cmd.w * profile.FullYawTorque, 0f) + AssistWrench.Torque);
            model.Step(wrench, efficiency, Time.fixedDeltaTime);

            Quaternion frame = Quaternion.Euler(0f, bodyYaw, 0f);
            for (int i = 0; i < model.Count; i++)
            {
                float thrust = model.Thrust[i];
                if (thrust == 0f) continue;
                Vector3 worldForce = transform.TransformDirection(frame * (model.DirectionOf(i) * thrust));
                Vector3 worldPoint = transform.TransformPoint(frame * model.Def(i).Position);
                body.AddForceAtPosition(worldForce, worldPoint, ForceMode.Force);
            }
        }
    }
}
