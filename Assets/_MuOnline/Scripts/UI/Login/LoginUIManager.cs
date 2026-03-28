using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MuOnline.UI.Login
{
    [System.Serializable]
    public class LoginCredentialsEvent : UnityEvent<string, string> { }

    /// <summary>
    /// Lógica de formulario: validación, feedback y hover en botones (escala + color ya vía <see cref="Button"/>).
    /// </summary>
    public class LoginUIManager : MonoBehaviour
    {
        [Header("Fields")]
        [SerializeField] private TMP_InputField usernameField;
        [SerializeField] private TMP_InputField passwordField;

        [Header("Buttons")]
        [SerializeField] private Button loginButton;
        [SerializeField] private Button registerButton;

        [Header("Feedback")]
        [SerializeField] private TextMeshProUGUI validationMessage;
        [SerializeField] private string emptyUserMessage = "Introduce tu usuario.";
        [SerializeField] private string emptyPassMessage = "Introduce tu contraseña.";

        [Header("Button hover (scale)")]
        [SerializeField] private float hoverScale = 1.04f;
        [SerializeField] private float hoverDuration = 0.12f;

        [Header("Events")]
        [SerializeField] private LoginCredentialsEvent onLoginValidated;
        [SerializeField] private UnityEvent onRegisterPressed;

        static readonly Color HoverTint = new Color(1.05f, 0.98f, 0.88f, 1f);

        readonly Dictionary<RectTransform, Coroutine> _scaleCoroutines = new();

        void Start()
        {
            if (loginButton != null)
            {
                loginButton.onClick.RemoveListener(OnLoginClicked);
                loginButton.onClick.AddListener(OnLoginClicked);
                AddHoverFeedback(loginButton);
            }

            if (registerButton != null)
            {
                registerButton.onClick.RemoveListener(OnRegisterPressedHandler);
                registerButton.onClick.AddListener(OnRegisterPressedHandler);
                AddHoverFeedback(registerButton);
            }

            ClearValidationMessage();
        }

        void OnDestroy()
        {
            if (loginButton != null) loginButton.onClick.RemoveListener(OnLoginClicked);
            if (registerButton != null) registerButton.onClick.RemoveListener(OnRegisterPressedHandler);
        }

        void OnLoginClicked()
        {
            ClearValidationMessage();

            string user = usernameField != null ? usernameField.text.Trim() : "";
            string pass = passwordField != null ? passwordField.text : "";

            if (string.IsNullOrEmpty(user))
            {
                ShowValidation(emptyUserMessage);
                Debug.LogWarning("[LoginUIManager] Login bloqueado: usuario vacío.");
                return;
            }

            if (string.IsNullOrEmpty(pass))
            {
                ShowValidation(emptyPassMessage);
                Debug.LogWarning("[LoginUIManager] Login bloqueado: contraseña vacía.");
                return;
            }

            Debug.Log($"[LoginUIManager] Intento de login — Usuario: \"{user}\" (longitud contraseña: {pass.Length})");
            onLoginValidated?.Invoke(user, pass);
        }

        void OnRegisterPressedHandler()
        {
            ClearValidationMessage();
            Debug.Log("[LoginUIManager] Crear cuenta (secundario).");
            onRegisterPressed?.Invoke();
        }

        void ShowValidation(string msg)
        {
            if (validationMessage != null)
            {
                validationMessage.text = msg;
                validationMessage.gameObject.SetActive(true);
            }
        }

        void ClearValidationMessage()
        {
            if (validationMessage == null) return;
            validationMessage.text = "";
            validationMessage.gameObject.SetActive(false);
        }

        void AddHoverFeedback(Button btn)
        {
            var trigger = btn.gameObject.GetComponent<EventTrigger>();
            if (trigger == null) trigger = btn.gameObject.AddComponent<EventTrigger>();

            var rt = btn.transform as RectTransform;
            if (rt == null) return;

            Vector3 baseScale = rt.localScale;

            AddEntry(trigger, EventTriggerType.PointerEnter, _ =>
            {
                RunScale(rt, baseScale * hoverScale, hoverDuration);
                var c = btn.colors;
                c.normalColor = Color.Lerp(c.normalColor, HoverTint, 0.25f);
                btn.colors = c;
            });

            AddEntry(trigger, EventTriggerType.PointerExit, _ =>
            {
                RunScale(rt, baseScale, hoverDuration);
            });

            AddEntry(trigger, EventTriggerType.PointerDown, _ =>
            {
                RunScale(rt, baseScale * 0.97f, 0.06f);
            });

            AddEntry(trigger, EventTriggerType.PointerUp, _ =>
            {
                RunScale(rt, baseScale * hoverScale, 0.08f);
            });
        }

        static void AddEntry(EventTrigger trigger, EventTriggerType type, UnityAction<BaseEventData> callback)
        {
            var e = new EventTrigger.Entry { eventID = type };
            e.callback.AddListener(callback);
            trigger.triggers.Add(e);
        }

        void RunScale(RectTransform rt, Vector3 target, float duration)
        {
            if (_scaleCoroutines.TryGetValue(rt, out var existing) && existing != null)
                StopCoroutine(existing);
            _scaleCoroutines[rt] = StartCoroutine(ScaleRoutine(rt, target, duration));
        }

        IEnumerator ScaleRoutine(RectTransform rt, Vector3 target, float duration)
        {
            Vector3 a = rt.localScale;
            float t = 0f;
            while (t < 1f)
            {
                t += Time.unscaledDeltaTime / Mathf.Max(0.01f, duration);
                rt.localScale = Vector3.Lerp(a, target, Mathf.SmoothStep(0f, 1f, t));
                yield return null;
            }

            rt.localScale = target;
            _scaleCoroutines[rt] = null;
        }
    }
}
