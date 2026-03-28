using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using MuOnline.Core;
using MuOnline.Gameplay.Combat;

namespace MuOnline.Gameplay.Targeting
{
    /// <summary>Toque / click en pantalla para seleccionar enemigo bajo el rayo.</summary>
    public class TouchTargetPicker : MonoBehaviour
    {
        [SerializeField] private Camera rayCamera;
        [SerializeField] private LayerMask selectableLayers = ~0;
        [SerializeField] private float maxDistance = 80f;
        [SerializeField] private TargetSelector selector;

        void Awake()
        {
            if (rayCamera == null) rayCamera = Camera.main;
            if (selector == null) selector = GetComponent<TargetSelector>();
        }

        public void SetSelectableLayers(LayerMask mask) => selectableLayers = mask;

        void Update()
        {
            var mouse = Mouse.current;
            var touch = Touchscreen.current;
            bool pressed = false;
            Vector2 screen = default;

            if (mouse != null && mouse.leftButton.wasPressedThisFrame)
            {
                pressed = true;
                screen = mouse.position.ReadValue();
            }
            else if (touch != null && touch.primaryTouch.press.wasPressedThisFrame)
            {
                pressed = true;
                screen = touch.primaryTouch.position.ReadValue();
            }

            if (!pressed || selector == null || rayCamera == null) return;

            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;

            var ray = rayCamera.ScreenPointToRay(screen);
            if (!Physics.Raycast(ray, out var hit, maxDistance, selectableLayers, QueryTriggerInteraction.Collide))
                return;

            if (hit.collider.CompareTag(GameplayLayers.EnemyTag))
                selector.SetTarget(hit.collider.transform);
        }
    }
}
