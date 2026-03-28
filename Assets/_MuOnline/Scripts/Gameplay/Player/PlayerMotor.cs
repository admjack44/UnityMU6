using UnityEngine;

namespace MuOnline.Gameplay.Player
{
    /// <summary>Movimiento con CharacterController; dirección relativa a la cámara.</summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerMotor : MonoBehaviour
    {
        [SerializeField] private float moveSpeed = 6f;
        [SerializeField] private float gravity = -24f;
        [SerializeField] private float rotationLerp = 12f;

        private CharacterController _cc;
        private Camera _cam;
        private Vector3 _velocity;

        public float MoveSpeed
        {
            get => moveSpeed;
            set => moveSpeed = value;
        }

        void Awake()
        {
            _cc = GetComponent<CharacterController>();
            _cam = Camera.main;
        }

        /// <param name="inputAxes">x = strafe, y = forward en espacio de cámara.</param>
        public void Move(Vector2 inputAxes, float deltaTime)
        {
            if (_cam == null) _cam = Camera.main;
            if (_cam == null) return;

            Vector3 move = Vector3.zero;
            if (inputAxes.sqrMagnitude > 0.0001f)
            {
                var camFwd = _cam.transform.forward; camFwd.y = 0f; camFwd.Normalize();
                var camRight = _cam.transform.right; camRight.y = 0f; camRight.Normalize();
                move = (camFwd * inputAxes.y + camRight * inputAxes.x).normalized;
                _cc.Move(move * (moveSpeed * deltaTime));

                if (move.sqrMagnitude > 0.0001f)
                {
                    var look = Quaternion.LookRotation(move);
                    transform.rotation = Quaternion.Slerp(transform.rotation, look, deltaTime * rotationLerp);
                }
            }

            ApplyGravityStep(deltaTime);
        }

        /// <summary>Dirección en plano mundo (XZ); útil para auto-batalla sin depender del stick.</summary>
        public void MoveWorldDirection(Vector3 worldDirXZ, float deltaTime)
        {
            worldDirXZ.y = 0f;
            if (worldDirXZ.sqrMagnitude < 0.0001f)
            {
                Move(Vector2.zero, deltaTime);
                return;
            }

            worldDirXZ.Normalize();
            if (_cam == null) _cam = Camera.main;
            if (_cam == null) return;

            var camFwd = _cam.transform.forward; camFwd.y = 0f; camFwd.Normalize();
            var camRight = _cam.transform.right; camRight.y = 0f; camRight.Normalize();
            float forward = Vector3.Dot(worldDirXZ, camFwd);
            float right = Vector3.Dot(worldDirXZ, camRight);
            Move(new Vector2(right, forward), deltaTime);
        }

        void ApplyGravityStep(float deltaTime)
        {
            if (_cc.isGrounded)
                _velocity.y = -2f;
            else
                _velocity.y += gravity * deltaTime;

            _cc.Move(_velocity * deltaTime);
        }
    }
}
