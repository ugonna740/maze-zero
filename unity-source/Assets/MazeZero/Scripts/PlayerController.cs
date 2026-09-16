using UnityEngine;
using UnityEngine.InputSystem;

namespace MazeZero
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class PlayerController : MonoBehaviour
    {
        private CharacterController controller;
        private float verticalSpeed;
        private Camera movementCamera;
        private Animator animator;
        private Vector3 lastFacing = Vector3.forward;
        private Vector3 spawnPosition;
        public bool CanMove { get; set; } = true;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            movementCamera = Camera.main;
            animator = GetComponentInChildren<Animator>();
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            spawnPosition = transform.position;
        }

        public void Respawn()
        {
            controller.enabled = false;
            transform.position = spawnPosition;
            transform.rotation = Quaternion.identity;
            verticalSpeed = 0f;
            controller.enabled = true;
        }

        public void SetSpawnPosition(Vector3 position, bool warpNow = true)
        {
            spawnPosition = position;
            if (!warpNow) return;
            controller.enabled = false;
            transform.position = spawnPosition;
            transform.rotation = Quaternion.identity;
            verticalSpeed = 0f;
            controller.enabled = true;
            CanMove = true;
        }

        private void OnTriggerEnter(Collider other) => TryTrap(other, other.ClosestPoint(controller.bounds.center));

        private void OnControllerColliderHit(ControllerColliderHit hit) => TryTrap(hit.collider, hit.point);

        private void TryTrap(Collider hitCollider, Vector3 hitPosition)
        {
            var damage = hitCollider.GetComponentInParent<TrapDamage>();
            if (damage != null) damage.TryApply(this, hitPosition);
        }

        private void Update()
        {
            if (!CanMove)
            {
                if (animator != null) animator.SetFloat("Speed", 0f, .12f, Time.deltaTime);
                return;
            }
            var move = Vector2.zero;
            if (Keyboard.current != null)
            {
                if (Keyboard.current.wKey.isPressed) move.y += 1;
                if (Keyboard.current.sKey.isPressed) move.y -= 1;
                if (Keyboard.current.dKey.isPressed) move.x += 1;
                if (Keyboard.current.aKey.isPressed) move.x -= 1;
            }
            move += OnScreenControls.Move;
            move = Vector2.ClampMagnitude(move, 1f);
            var cameraForward = movementCamera != null ? movementCamera.transform.forward : Vector3.forward;
            var cameraRight = movementCamera != null ? movementCamera.transform.right : Vector3.right;
            cameraForward.y = 0f;
            cameraRight.y = 0f;
            cameraForward.Normalize();
            cameraRight.Normalize();
            var planarMotion = (cameraForward * move.y + cameraRight * move.x).normalized;
            if (animator != null) animator.SetFloat("Speed", planarMotion.magnitude, .12f, Time.deltaTime);
            if (planarMotion.sqrMagnitude > .01f)
            {
                lastFacing = planarMotion;
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lastFacing), 14f * Time.deltaTime);
            }
            var motion = planarMotion * 6f;
            verticalSpeed = controller.isGrounded ? -1f : verticalSpeed - 18f * Time.deltaTime;
            motion.y = verticalSpeed;
            controller.Move(motion * Time.deltaTime);

        }
    }
}
