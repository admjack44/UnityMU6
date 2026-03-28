using UnityEngine;
using UnityEngine.EventSystems;
namespace MuOnline.Gameplay.Input
{
    /// <summary>Joystick UGUI clásico (arrastre desde un punto base).</summary>
    [RequireComponent(typeof(RectTransform))]
    public class VirtualJoystick : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler, IVirtualJoystick
    {
        [SerializeField] private RectTransform handle;
        [SerializeField] private float handleRange = 72f;

        private Vector2 _pointerDown;
        private bool _active;
        private RectTransform _rt;

        public Vector2 Value { get; private set; }
        public bool IsActive => _active;

        /// <summary>Asignación desde bootstrap de UI (sin prefab).</summary>
        public void AssignHandle(RectTransform handleTransform, float rangePixels = 72f)
        {
            handle = handleTransform;
            handleRange = rangePixels;
        }

        void Awake()
        {
            _rt = (RectTransform)transform;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _rt, eventData.position, eventData.pressEventCamera, out var local))
                return;

            _active      = true;
            _pointerDown = local;
            UpdateHandle(local);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!_active) return;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _rt, eventData.position, eventData.pressEventCamera, out var local))
                return;

            UpdateHandle(local);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            _active = false;
            Value   = Vector2.zero;
            if (handle) handle.anchoredPosition = Vector2.zero;
        }

        void UpdateHandle(Vector2 localPoint)
        {
            var delta = localPoint - _pointerDown;
            if (delta.magnitude > handleRange)
                delta = delta.normalized * handleRange;

            if (handle)
                handle.anchoredPosition = delta;

            Value = handleRange > 0.01f ? delta / handleRange : Vector2.zero;
        }
    }
}
