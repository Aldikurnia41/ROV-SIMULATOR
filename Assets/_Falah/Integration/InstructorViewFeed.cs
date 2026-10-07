using UnityEngine;

namespace Falah.RovSim.Integration
{
    /// <summary>
    /// Live "trainee view" for the instructor station: a hidden camera that copies the pose and lens of the active
    /// ROV camera and renders into a RenderTexture. Lives next to <see cref="RoVTelemetryAdapter"/> because it finds the
    /// vehicle through <see cref="RoVPhysics"/>. The camera only exists while the component is enabled.
    /// </summary>
    public sealed class InstructorViewFeed : MonoBehaviour
    {
        [SerializeField] RenderTexture target;
        [SerializeField] float searchInterval = 1f;

        Camera feed;
        Camera source;
        float nextSearch;

        void OnEnable()
        {
            var go = new GameObject("InstructorViewCamera") { hideFlags = HideFlags.HideAndDontSave };
            feed = go.AddComponent<Camera>();
            feed.targetTexture = target;
            feed.depth = -20;
            feed.enabled = false;
        }

        void OnDisable()
        {
            if (feed != null) Destroy(feed.gameObject);
            feed = null;
            source = null;
        }

        void LateUpdate()
        {
            if (feed == null) return;
            if ((source == null || !source.isActiveAndEnabled) && Time.unscaledTime >= nextSearch)
            {
                nextSearch = Time.unscaledTime + searchInterval;
                source = FindSourceCamera();
            }
            feed.enabled = source != null;
            if (source == null) return;
            feed.transform.SetPositionAndRotation(source.transform.position, source.transform.rotation);
            feed.fieldOfView = source.fieldOfView;
            feed.nearClipPlane = source.nearClipPlane;
            feed.farClipPlane = source.farClipPlane;
        }

        Camera FindSourceCamera()
        {
            var rov = FindFirstObjectByType<RoVPhysics>();
            if (rov == null) return null;
            foreach (var cam in rov.GetComponentsInChildren<Camera>())
                if (cam != feed && cam.isActiveAndEnabled) return cam;
            return null;
        }
    }
}
