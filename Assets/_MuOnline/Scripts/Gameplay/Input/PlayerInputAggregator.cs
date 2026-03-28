using UnityEngine;
using UnityEngine.InputSystem;

namespace MuOnline.Gameplay.Input
{
    /// <summary>Combina joystick virtual + WASD/flechas para editor y pruebas.</summary>
    public class PlayerInputAggregator : MonoBehaviour
    {
        [SerializeField] private VirtualJoystick joystick;
        [SerializeField] private bool enableKeyboardFallback = true;

        /// <summary>Dirección en espacio de pantalla/cámara: x horizontal, y vertical (como Input axes).</summary>
        public Vector2 MoveAxes { get; private set; }

        void Update()
        {
            Vector2 v = joystick != null && joystick.IsActive ? joystick.Value : Vector2.zero;

            if (enableKeyboardFallback && v.sqrMagnitude < 0.01f)
            {
                var kb = Keyboard.current;
                if (kb != null)
                {
                    float h = (kb.dKey.isPressed || kb.rightArrowKey.isPressed ? 1f : 0f) -
                              (kb.aKey.isPressed || kb.leftArrowKey.isPressed ? 1f : 0f);
                    float y = (kb.wKey.isPressed || kb.upArrowKey.isPressed ? 1f : 0f) -
                              (kb.sKey.isPressed || kb.downArrowKey.isPressed ? 1f : 0f);
                    v = new Vector2(h, y);
                    if (v.sqrMagnitude > 1f) v.Normalize();
                }
            }

            MoveAxes = v;
        }

        public void BindJoystick(VirtualJoystick j) => joystick = j;
    }
}
