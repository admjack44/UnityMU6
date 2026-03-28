using UnityEngine;

namespace MuOnline.Gameplay
{
    /// <summary>
    /// Cámara isométrica estilo MU Online.
    /// Sigue al jugador desde arriba-atrás con ángulo fijo.
    /// </summary>
    public class CameraController : MonoBehaviour
    {
        [Header("Target")]
        public Transform Target;

        [Header("Isometric Settings")]
        [SerializeField] private float _distance = 18f;
        [SerializeField] private float _pitch    = 52f;   // inclinación vertical (grados)
        [SerializeField] private float _yaw      = 45f;   // rotación horizontal  (grados)

        [Header("Smoothing")]
        [SerializeField] private float _followSpeed = 8f;

        private Vector3 _desiredPos;

        void LateUpdate()
        {
            if (Target == null) return;

            // Calcular posición isométrica
            var rot = Quaternion.Euler(_pitch, _yaw, 0f);
            _desiredPos = Target.position - rot * Vector3.forward * _distance;

            transform.position = Vector3.Lerp(transform.position, _desiredPos, Time.deltaTime * _followSpeed);
            transform.rotation = rot;
        }

        /// <summary>Asigna el objetivo y posiciona la cámara inmediatamente.</summary>
        public void SetTarget(Transform target)
        {
            Target = target;
            if (target == null) return;

            var rot = Quaternion.Euler(_pitch, _yaw, 0f);
            transform.position = target.position - rot * Vector3.forward * _distance;
            transform.rotation = rot;
        }
    }
}
