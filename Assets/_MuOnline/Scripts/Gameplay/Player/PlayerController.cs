using UnityEngine;
using MuOnline.Gameplay.Input;
using MuOnline.Gameplay.Player;

namespace MuOnline.Gameplay
{
    /// <summary>
    /// Orquesta entrada (joystick + teclado) y <see cref="PlayerMotor"/>.
    /// Mantener delgado: sin lógica de combate ni red.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(PlayerMotor))]
    public class PlayerController : MonoBehaviour
    {
        [SerializeField] private PlayerMotor motor;
        [SerializeField] private PlayerInputAggregator input;

        void Awake()
        {
            if (motor == null) motor = GetComponent<PlayerMotor>();
            if (input == null) input = GetComponent<PlayerInputAggregator>();
        }

        void Update()
        {
            if (motor == null || input == null) return;
            motor.Move(input.MoveAxes, Time.deltaTime);
        }

        /// <summary>Enlace en runtime desde bootstrap de UI.</summary>
        public void BindJoystick(VirtualJoystick joystick) => input?.BindJoystick(joystick);
    }
}
