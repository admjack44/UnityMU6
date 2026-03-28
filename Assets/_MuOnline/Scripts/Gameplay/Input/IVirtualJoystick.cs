using UnityEngine;

namespace MuOnline.Gameplay.Input
{
    /// <summary>Abstracción para stick táctil o stick físico / teclado emulado.</summary>
    public interface IVirtualJoystick
    {
        /// <summary>Vector en plano XZ (-1..1), relativo a la cámara en <see cref="PlayerMotor"/>.</summary>
        Vector2 Value { get; }
        bool IsActive { get; }
    }
}
