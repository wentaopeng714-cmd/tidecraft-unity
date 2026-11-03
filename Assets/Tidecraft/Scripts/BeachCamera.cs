using UnityEngine;
namespace Tidecraft
{
    public sealed class BeachCamera : MonoBehaviour
    {
        public Camera Camera { get; private set; }
        public Vector3 focus = new Vector3(0, 0, 0);
        public float yaw = -18, elevation = 52, distance = 34;
        public bool UsingCinemachine { get; private set; }
        Transform rig;
        Vector3 smoothedFocus;
        float smoothedYaw, smoothedDistance;
        public void Initialize()
        {
            Camera = gameObject.AddComponent<Camera>(); Camera.tag = "MainCamera";
            Camera.fieldOfView = 42; Camera.nearClipPlane = .15f; Camera.farClipPlane = 120;
            Camera.clearFlags = CameraClearFlags.SolidColor; Camera.backgroundColor = Palette.Sky;
            gameObject.AddComponent<AudioListener>();
            // Keep the first import compilable before Package Manager installs Cinemachine.
            var brain = System.Type.GetType("Unity.Cinemachine.CinemachineBrain, Unity.Cinemachine");
            var vcam = System.Type.GetType("Unity.Cinemachine.CinemachineCamera, Unity.Cinemachine");
            if (brain != null && vcam != null)
            {
                gameObject.AddComponent(brain); var go = new GameObject("Beach inspection rig");
                var component = go.AddComponent(vcam); rig = go.transform;
                var lensField = vcam.GetField("Lens");
                if (lensField != null)
                {
                    object lens = lensField.GetValue(component); var f = lens.GetType().GetField("FieldOfView");
                    f?.SetValue(lens, 42f); lensField.SetValue(component, lens);
                }
                UsingCinemachine = true;
            }
            else rig = transform;
            Snap();
        }
        public void Home() { focus = new Vector3(0, 0, 0); yaw = -18; distance = 34; }
        public void Snap() { smoothedFocus = focus; smoothedYaw = yaw; smoothedDistance = distance; PositionRig(); }
        public void Gesture(Vector2 pan, float scale, float rotation)
        {
            Quaternion q = Quaternion.Euler(0, yaw, 0);
            focus -= q * new Vector3(pan.x, 0, pan.y) * (distance / Mathf.Max(Screen.height, 1)) * .85f;
            focus.x = Mathf.Clamp(focus.x, -10, 10); focus.z = Mathf.Clamp(focus.z, -10, 10);
            distance = Mathf.Clamp(distance / Mathf.Max(.5f, scale), 13, 48); yaw -= rotation;
        }
        public void Zoom(float delta) { distance = Mathf.Clamp(distance * Mathf.Exp(-delta * .0018f), 13, 48); }
        public void Rotate(float delta) { yaw += delta; }
        void LateUpdate()
        {
            float t = 1 - Mathf.Exp(-Time.unscaledDeltaTime * 12);
            smoothedFocus = Vector3.Lerp(smoothedFocus, focus, t); smoothedYaw = Mathf.LerpAngle(smoothedYaw, yaw, t);
            smoothedDistance = Mathf.Lerp(smoothedDistance, distance, t); PositionRig();
        }
        void PositionRig()
        {
            if (!rig) return;
            var rotation = Quaternion.Euler(elevation, smoothedYaw, 0);
            // Focus sits slightly above the centre so the bottom toolbar does not cover the work area.
            rig.SetPositionAndRotation(smoothedFocus + Vector3.up * .3f + rotation * new Vector3(0, 0, -smoothedDistance), rotation);
        }
    }
}
