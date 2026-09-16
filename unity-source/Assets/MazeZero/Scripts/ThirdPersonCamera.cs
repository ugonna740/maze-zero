using UnityEngine;
using UnityEngine.InputSystem;

namespace MazeZero
{
    public sealed class ThirdPersonCamera : MonoBehaviour
    {
        private Transform target;
        [Header("Editable Camera Settings")]
        [SerializeField] private float yaw = 42f;
        [SerializeField, Range(52f, 78f)] private float pitch = 62f;
        [SerializeField, Min(2f)] private float distance = 13.5f;
        [SerializeField, Min(0f)] private float lookHeight = 3.7f;
        [SerializeField, Min(.01f)] private float followSmoothTime = .18f;
        [SerializeField, Min(1f)] private float rotationResponsiveness = 12f;
        private Vector3 followVelocity;
        [Header("Trap Hit Shake")]
        [SerializeField, Min(0f)] private float hitShakeDuration = .28f;
        [SerializeField, Min(0f)] private float hitShakeDegrees = .9f;
        private float shakeRemaining, shakeStrength;
        private float activeShakeDuration;
        private Quaternion shakeRotation = Quaternion.identity;

        public void ShakeOnHit(bool fatal = false)
        {
            activeShakeDuration = hitShakeDuration * 1.4f;
            shakeRemaining = activeShakeDuration;
            shakeStrength = hitShakeDegrees * 2.5f * (fatal ? 1.35f : 1f);
        }

        public void Follow(Transform followTarget)
        {
            target = followTarget;
            shakeRemaining = 0f;
            shakeRotation = Quaternion.identity;
            SnapToTarget();
        }

        private void LateUpdate()
        {
            // Remove last frame's shake before smoothing, so it cannot accumulate.
            transform.rotation *= Quaternion.Inverse(shakeRotation);
            shakeRotation = Quaternion.identity;
            if (target == null) return;
            ReadOrbitInput();
            var focus = target.position + Vector3.up * lookHeight;
            var orbit = Quaternion.Euler(pitch, yaw, 0f);
            var desired = focus - orbit * Vector3.forward * distance;
            transform.position = Vector3.SmoothDamp(transform.position, desired, ref followVelocity, followSmoothTime);
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(focus - transform.position), rotationResponsiveness * Time.deltaTime);
            if (shakeRemaining > 0f && activeShakeDuration > 0f)
            {
                shakeRemaining = Mathf.Max(0f, shakeRemaining - Time.unscaledDeltaTime);
                var strength = shakeStrength * Mathf.Pow(shakeRemaining / activeShakeDuration, 2f);
                var t = Time.unscaledTime * 36f;
                shakeRotation = Quaternion.Euler(
                    (Mathf.PerlinNoise(t, 0f) * 2f - 1f) * strength,
                    (Mathf.PerlinNoise(0f, t) * 2f - 1f) * strength,
                    (Mathf.PerlinNoise(t, 17f) * 2f - 1f) * strength * .5f);
                transform.rotation *= shakeRotation;
            }
        }

        private void ReadOrbitInput()
        {
            if (Mouse.current != null && Mouse.current.rightButton.isPressed)
            {
                var delta = Mouse.current.delta.ReadValue();
                yaw += delta.x * .16f;
                pitch = Mathf.Clamp(pitch - delta.y * .12f, 38f, 72f);
            }
            var touchDelta = OnScreenControls.ConsumeCameraDelta();
            yaw += touchDelta.x * .16f;
            pitch = Mathf.Clamp(pitch - touchDelta.y * .12f, 52f, 78f);
        }

        private void SnapToTarget()
        {
            if (target == null) return;
            var focus = target.position + Vector3.up * lookHeight;
            var orbit = Quaternion.Euler(pitch, yaw, 0f);
            transform.position = focus - orbit * Vector3.forward * distance;
            transform.LookAt(focus);
        }
    }
}
