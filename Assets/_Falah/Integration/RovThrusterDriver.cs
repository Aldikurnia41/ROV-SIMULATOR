using Falah.RovSim.Core;
using UnityEngine;

namespace Falah.RovSim.Integration
{
    /// <summary>
    /// Applies <see cref="ThrusterModel"/> thrust to a Rigidbody with AddForceAtPosition (T1.2). The command is a
    /// normalised 4-axis input (surge, sway, heave, yaw; -1..1) scaled to the layout's capability. Injected faults from
    /// the session log reduce each thruster's efficiency. Input mapping is T1.3; until then <see cref="Command"/> is set
    /// by tests or tools. Disabled by default on the existing ROV so <see cref="RoVPhysics"/> keeps driving it.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public sealed class RovThrusterDriver : MonoBehaviour
    {
        [SerializeField] string rovId = SessionSetup.DefaultRovId;
        [Tooltip("Surge/sway/heave force at full command, newtons (PLACEHOLDER).")]
        [SerializeField] Vector3 fullForce = new Vector3(60f, 60f, 100f);
        [Tooltip("Yaw torque at full command, N*m (PLACEHOLDER).")]
        [SerializeField] float fullYawTorque = 15f;

        Rigidbody body;
        ThrusterModel model;
        float[] efficiency;

        /// <summary>x = sway, y = heave, z = surge, w = yaw; each -1..1.</summary>
        public Vector4 Command;

        public ThrusterModel Model => model;

        public void SetLayout(string id)
        {
            rovId = id;
            model = new ThrusterModel(ThrusterLayouts.For(id));
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
            var wrench = new Wrench(
                new Vector3(cmd.x * fullForce.x, cmd.y * fullForce.y, cmd.z * fullForce.z),
                new Vector3(0f, cmd.w * fullYawTorque, 0f));
            model.Step(wrench, efficiency, Time.fixedDeltaTime);

            for (int i = 0; i < model.Count; i++)
            {
                float thrust = model.Thrust[i];
                if (thrust == 0f) continue;
                Vector3 worldForce = transform.TransformDirection(model.DirectionOf(i) * thrust);
                Vector3 worldPoint = transform.TransformPoint(model.Def(i).Position);
                body.AddForceAtPosition(worldForce, worldPoint, ForceMode.Force);
            }
        }
    }
}
