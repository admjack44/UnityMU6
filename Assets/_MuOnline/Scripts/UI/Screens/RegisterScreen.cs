using TMPro;
using UnityEngine;
using UnityEngine.UI;
using MuOnline.Core;
using MuOnline.Network;
using MuOnline.Network.Packets;

namespace MuOnline.UI
{
    public class RegisterScreen : MonoBehaviour
    {
        private TMP_InputField  _usernameInput;
        private TMP_InputField  _passwordInput;
        private TMP_InputField  _confirmInput;
        private Button          _confirmButton;
        private Button          _backButton;
        private TextMeshProUGUI _statusText;

        private RectTransform _myPanel;
        private GameObject    _loginPanel;

        public void Init(RectTransform myPanel, GameObject loginPanel)
        {
            _myPanel    = myPanel;
            _loginPanel = loginPanel;
            BuildFields(myPanel);
        }

        void OnEnable()
        {
            EventBus.Subscribe<AuthEvents.RegisterSuccess>(OnRegisterSuccess);
            EventBus.Subscribe<AuthEvents.RegisterFailed>(OnRegisterFailed);
        }

        void OnDisable()
        {
            EventBus.Unsubscribe<AuthEvents.RegisterSuccess>(OnRegisterSuccess);
            EventBus.Unsubscribe<AuthEvents.RegisterFailed>(OnRegisterFailed);
        }

        private void BuildFields(RectTransform parent)
        {
            // Usuario
            _usernameInput = UIBuilder.CreateInputField(parent, "UsernameInput", "Usuario (mín. 4 caracteres)");
            var uRt = _usernameInput.GetComponent<RectTransform>();
            uRt.anchorMin = uRt.anchorMax = new Vector2(0.5f, 0.5f);
            uRt.anchoredPosition = new Vector2(0, 90);
            uRt.sizeDelta        = new Vector2(360, 55);

            // Contraseña
            _passwordInput = UIBuilder.CreateInputField(parent, "PasswordInput", "Contraseña (mín. 6 caracteres)", true);
            var pRt = _passwordInput.GetComponent<RectTransform>();
            pRt.anchorMin = pRt.anchorMax = new Vector2(0.5f, 0.5f);
            pRt.anchoredPosition = new Vector2(0, 20);
            pRt.sizeDelta        = new Vector2(360, 55);

            // Confirmar
            _confirmInput = UIBuilder.CreateInputField(parent, "ConfirmInput", "Confirmar contraseña", true);
            var cRt = _confirmInput.GetComponent<RectTransform>();
            cRt.anchorMin = cRt.anchorMax = new Vector2(0.5f, 0.5f);
            cRt.anchoredPosition = new Vector2(0, -50);
            cRt.sizeDelta        = new Vector2(360, 55);

            // Botón registrar
            _confirmButton = UIBuilder.CreateButton(parent, "ConfirmBtn", "REGISTRAR", MuUITheme.ButtonNormal);
            var btnRt = _confirmButton.GetComponent<RectTransform>();
            btnRt.anchorMin = btnRt.anchorMax = new Vector2(0.5f, 0.5f);
            btnRt.anchoredPosition = new Vector2(0, -130);
            btnRt.sizeDelta        = new Vector2(200, 50);
            _confirmButton.onClick.AddListener(OnConfirmPressed);

            // Botón volver
            _backButton = UIBuilder.CreateButton(parent, "BackBtn", "← VOLVER", MuUITheme.PanelBackground);
            var backRt = _backButton.GetComponent<RectTransform>();
            backRt.anchorMin = backRt.anchorMax = new Vector2(0.5f, 0.5f);
            backRt.anchoredPosition = new Vector2(0, -185);
            backRt.sizeDelta        = new Vector2(200, 38);
            _backButton.GetComponentInChildren<TextMeshProUGUI>().fontSize = 13f;
            _backButton.onClick.AddListener(OnBackPressed);

            // Status
            var statusGo = new GameObject("Status");
            statusGo.transform.SetParent(parent, false);
            _statusText = statusGo.AddComponent<TextMeshProUGUI>();
            _statusText.fontSize  = MuUITheme.FontSizeSmall;
            _statusText.color     = MuUITheme.TextError;
            _statusText.alignment = TextAlignmentOptions.Center;
            var sRt = statusGo.GetComponent<RectTransform>();
            sRt.anchorMin = sRt.anchorMax = new Vector2(0.5f, 0.5f);
            sRt.anchoredPosition = new Vector2(0, -225);
            sRt.sizeDelta        = new Vector2(400, 40);
        }

        private void OnConfirmPressed()
        {
            string user    = _usernameInput.text.Trim();
            string pass    = _passwordInput.text;
            string confirm = _confirmInput.text;

            if (user.Length < 4)
            {
                ShowStatus("El usuario debe tener al menos 4 caracteres.", MuUITheme.TextError);
                return;
            }
            if (pass.Length < 6)
            {
                ShowStatus("La contraseña debe tener al menos 6 caracteres.", MuUITheme.TextError);
                return;
            }
            if (pass != confirm)
            {
                ShowStatus("Las contraseñas no coinciden.", MuUITheme.TextError);
                return;
            }

            SetInteractable(false);
            ShowStatus("Registrando cuenta...", MuUITheme.TextWarning);
            NetworkClient.Instance.Send(ClientPackets.Register(user, pass));
        }

        private void OnBackPressed()
        {
            gameObject.SetActive(false);
            _loginPanel?.SetActive(true);
        }

        private void OnRegisterSuccess(AuthEvents.RegisterSuccess evt)
        {
            ShowStatus($"¡Cuenta '{evt.AccountName}' creada! Puedes iniciar sesión.", MuUITheme.TextSuccess);
            SetInteractable(true);

            // Volver al login tras 2 segundos
            Invoke(nameof(GoBackToLogin), 2f);
        }

        private void OnRegisterFailed(AuthEvents.RegisterFailed evt)
        {
            ShowStatus(evt.Message, MuUITheme.TextError);
            SetInteractable(true);
        }

        private void GoBackToLogin() => OnBackPressed();

        private void ShowStatus(string msg, Color color)
        {
            if (_statusText == null) return;
            _statusText.text  = msg;
            _statusText.color = color;
        }

        private void SetInteractable(bool v)
        {
            if (_usernameInput  != null) _usernameInput.interactable  = v;
            if (_passwordInput  != null) _passwordInput.interactable  = v;
            if (_confirmInput   != null) _confirmInput.interactable   = v;
            if (_confirmButton  != null) _confirmButton.interactable  = v;
        }
    }
}
