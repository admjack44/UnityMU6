using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using MuOnline.Core;
using MuOnline.Network;
using MuOnline.Network.Packets;

namespace MuOnline.UI
{
    [DefaultExecutionOrder(10)]
    public class LoginScreen : MonoBehaviour
    {
        private TMP_InputField  _accountInput;
        private TMP_InputField  _passwordInput;
        private Button          _loginButton;
        private Button          _registerButton;
        private TextMeshProUGUI _statusText;
        private GameObject      _spinner;
        private GameObject      _loginPanel;
        private GameObject      _registerPanel;

        void Awake() => BuildUI();

        void OnEnable()
        {
            EventBus.Subscribe<NetworkEvents.Connected>(OnConnected);
            EventBus.Subscribe<NetworkEvents.Disconnected>(OnDisconnected);
            EventBus.Subscribe<AuthEvents.LoginSuccess>(OnLoginSuccess);
            EventBus.Subscribe<AuthEvents.LoginFailed>(OnLoginFailed);
            EventBus.Subscribe<AuthEvents.CharacterListReceived>(OnCharacterListReceived);
            RefreshConnectionStatus();
        }

        void OnDisable()
        {
            EventBus.Unsubscribe<NetworkEvents.Connected>(OnConnected);
            EventBus.Unsubscribe<NetworkEvents.Disconnected>(OnDisconnected);
            EventBus.Unsubscribe<AuthEvents.LoginSuccess>(OnLoginSuccess);
            EventBus.Unsubscribe<AuthEvents.LoginFailed>(OnLoginFailed);
            EventBus.Unsubscribe<AuthEvents.CharacterListReceived>(OnCharacterListReceived);
        }

        void Start()
        {
            _loginButton?.onClick.AddListener(OnLoginPressed);
            _registerButton?.onClick.AddListener(OnRegisterPanelPressed);

            SetInteractable(true);
            RefreshConnectionStatus();
            if (NetworkClient.Instance != null && !NetworkClient.Instance.IsConnected)
                GameManager.Instance?.ConnectToServer();
            StartCoroutine(WatchConnectionHint());
        }

        /// <summary>Si Connected ya ocurrio antes de suscribirse, el estado debe alinearse con IsConnected.</summary>
        void RefreshConnectionStatus()
        {
            if (NetworkClient.Instance == null)
            {
                SetStatus("Sin NetworkClient. Carga la escena Boot primero o coloca el prefab.", MuUITheme.TextError);
                return;
            }

            if (NetworkClient.Instance.IsConnected)
            {
                SetStatus("Servidor conectado. Introduce tus credenciales.", MuUITheme.TextSuccess);
                return;
            }

            SetStatus("Conectando al servidor...", MuUITheme.TextWarning);
        }

        IEnumerator WatchConnectionHint()
        {
            yield return new WaitForSeconds(6f);
            if (NetworkClient.Instance == null) yield break;
            if (NetworkClient.Instance.IsConnected) yield break;
            var gm  = GameManager.Instance;
            string ep = gm != null ? $"{gm.ServerHost}:{gm.ServerPort}" : "127.0.0.1:44405";
            SetStatus($"Sin respuesta ({ep}). Arranca MuServer: PowerShell → cd Server\\MuServer → dotnet run", MuUITheme.TextError);
        }

        // ── Construcción principal ────────────────────────────────────────────

        private void BuildUI()
        {
            // Destruir spinner huérfano del BootLoader si sobrevivió a la carga
            foreach (var sr in FindObjectsByType<SpinnerRotator>(FindObjectsSortMode.None))
                Destroy(sr.gameObject);

            // EventSystem — imprescindible para que los botones reciban clics.
            if (FindFirstObjectByType<EventSystem>() == null)
            {
                var esGo = new GameObject("EventSystem");
                esGo.AddComponent<EventSystem>();
                esGo.AddComponent<InputSystemUIInputModule>();
                DontDestroyOnLoad(esGo);
            }

            var canvasGo = new GameObject("LoginCanvas");
            var canvas   = canvasGo.AddComponent<Canvas>();
            canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;

            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);  // landscape PC
            scaler.matchWidthOrHeight  = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();
            canvasGo.transform.SetParent(transform, false);

            var root = canvasGo.transform;

            // Fondo base negro (sin raycast)
            var bgBase = MakeImg("BG", root, stretch: true);
            bgBase.color = new Color(0.02f, 0.01f, 0.05f, 1f);
            bgBase.raycastTarget = false;

            // Arte de fondo original MU (sin raycast)
            var art = MuAssetLoader.Get(MuAssetLoader.LogoScreen.RandomLoginBg())
                   ?? MuAssetLoader.Get(MuAssetLoader.Login.RandomBgArt());
            if (art != null)
            {
                var ai = MakeImg("BGArt", root, stretch: true);
                ai.sprite = art; ai.color = Color.white; ai.preserveAspect = false;
                ai.raycastTarget = false;
            }

            // Overlay semitransparente (sin raycast para no bloquear input)
            var ov = MakeImg("Overlay", root, stretch: true);
            ov.color = new Color(0f, 0f, 0f, 0.50f);
            ov.raycastTarget = false;

            // Logo + subtítulo
            BuildLogoArea(root);

            // Panel de login (retorna GameObject)
            _loginPanel = BuildLoginPanel(root);

            // Texto de estado — debajo del panel pero cerca, siempre visible
            _statusText = MakeLabel("Status", root,
                pos: new Vector2(0, -258), size: new Vector2(520, 28),
                fontSize: 13f, color: MuUITheme.TextSecondary,
                align: TextAlignmentOptions.Center);

            // Spinner
            var sg  = new GameObject("Spinner");
            var srt = sg.AddComponent<RectTransform>();
            sg.AddComponent<Image>().color = MuUITheme.GoldPrimary;
            sg.AddComponent<SpinnerRotator>();
            sg.transform.SetParent(root, false);
            srt.anchorMin = srt.anchorMax = new Vector2(0.5f, 0.5f);
            srt.anchoredPosition = new Vector2(40, -340);
            srt.sizeDelta        = new Vector2(20, 20);
            _spinner = sg;
            _spinner.SetActive(false);

            // Panel de registro (oculto)
            _registerPanel = BuildRegisterPanel(root);
            _registerPanel.SetActive(false);

            // Versión
            var vt = MakeLabel("Version", root,
                pos: new Vector2(-12, 12), size: new Vector2(260, 22),
                fontSize: 10f, color: new Color(0.3f, 0.25f, 0.4f, 1f),
                align: TextAlignmentOptions.Right,
                anchorMin: new Vector2(1f, 0f), anchorMax: new Vector2(1f, 0f),
                pivot: new Vector2(1f, 0f));
            vt.text = "MU PEGASO  v0.1.0 Alpha";
        }

        // ── Logo ──────────────────────────────────────────────────────────────

        private void BuildLogoArea(Transform root)
        {
            var logoSpr =
                MuAssetLoader.Get(MuAssetLoader.LogoScreen.MuLogoGold)
                ?? MuAssetLoader.Get(MuAssetLoader.LogoScreen.MuLogoTga)
                ?? MuAssetLoader.Get(MuAssetLoader.Login.Logo);

            if (logoSpr != null)
            {
                var li = MakeImg("MuLogo", root);
                li.sprite = logoSpr; li.color = Color.white; li.preserveAspect = true;
                SetRt(li.GetComponent<RectTransform>(),
                    new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                    new Vector2(0, -38), new Vector2(320, 110),
                    new Vector2(0.5f, 1f));
            }
            else
            {
                var lt = MakeLabel("LogoTxt", root,
                    pos: new Vector2(0, -55), size: new Vector2(520, 82),
                    fontSize: 58f, color: MuUITheme.GoldPrimary,
                    align: TextAlignmentOptions.Center,
                    anchorMin: new Vector2(0.5f, 1f), anchorMax: new Vector2(0.5f, 1f),
                    pivot: new Vector2(0.5f, 1f));
                lt.text = "MU PEGASO"; lt.fontStyle = FontStyles.Bold;
                lt.gameObject.AddComponent<Outline>().effectColor =
                    new Color(0.55f, 0.25f, 0.95f, 0.7f);
            }

            // Subtítulo
            var st = MakeLabel("SubTitle", root,
                pos: new Vector2(0, -158), size: new Vector2(360, 28),
                fontSize: 13f, color: MuUITheme.PurpleLight,
                align: TextAlignmentOptions.Center,
                anchorMin: new Vector2(0.5f, 1f), anchorMax: new Vector2(0.5f, 1f),
                pivot: new Vector2(0.5f, 1f));
            st.text = "[ MU PEGASO ]   Private Server";
            st.fontStyle = FontStyles.Italic;

            // Línea
            var li2 = MakeImg("LogoLine", root);
            li2.color = MuUITheme.GoldDark;
            SetRt(li2.GetComponent<RectTransform>(),
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0, -192), new Vector2(360, 1f),
                new Vector2(0.5f, 1f));
        }

        // ── Panel de Login ────────────────────────────────────────────────────

        private GameObject BuildLoginPanel(Transform root)
        {
            // Tamaños ajustados para 1920x1080 — visibles y clicables
            const float PANEL_W = 480f;
            const float PANEL_H = 420f;
            const float PANEL_Y = 0f;

            // Sombra (hermano de bg, aparece detrás)
            var sh = MakeImg("PanelShadow", root);
            sh.color = new Color(0f, 0f, 0f, 0.70f);
            sh.raycastTarget = false;
            SetRt(sh.GetComponent<RectTransform>(),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(6, PANEL_Y - 4), new Vector2(PANEL_W + 12, PANEL_H + 12));

            // Fondo panel — completamente opaco para bloquear todo lo de atrás
            var bg = MakeImg("LoginPanel", root);
            bg.color = new Color(0.04f, 0.02f, 0.13f, 1f);   // alpha 1 = totalmente opaco
            SetRt(bg.GetComponent<RectTransform>(),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0, PANEL_Y), new Vector2(PANEL_W, PANEL_H));

            var p = bg.transform;

            // Borde dorado del panel (sin sprite para evitar artefactos del 0Account.tga)
            var outline = bg.gameObject.AddComponent<Outline>();
            outline.effectColor    = MuUITheme.GoldDark;
            outline.effectDistance = new Vector2(2, -2);

            // ── Header ────────────────────────────────────────────────────────
            var hdr = MakeImg("Header", p);
            hdr.color = new Color(0.15f, 0.06f, 0.30f, 1f);
            SetRt(hdr.GetComponent<RectTransform>(),
                new Vector2(0f, 1f), new Vector2(1f, 1f),
                Vector2.zero, new Vector2(0, 52), new Vector2(0.5f, 1f));

            var ht = new GameObject("HdrTxt");
            ht.AddComponent<RectTransform>();
            var htTx = ht.AddComponent<TextMeshProUGUI>();
            ht.transform.SetParent(hdr.transform, false);
            UIBuilder.StretchFill(ht.GetComponent<RectTransform>());
            htTx.text = "INICIAR SESION";
            htTx.fontSize = 22f; htTx.fontStyle = FontStyles.Bold;
            htTx.color = MuUITheme.GoldBright;
            htTx.alignment = TextAlignmentOptions.Center;

            // ── Formulario (posiciones desde centro del panel) ────────────────
            // Layout: USUARIO label → input → CONTRASENA label → input → botón → link
            // Panel H=420, center y=0, top y=210
            // Header ocupa top 52px → contenido desde y=210-52=158 hacia abajo
            // Con padding interno de 16px → primer elemento en y=130

            // Label USUARIO
            MakeFormLabel("LblUsuario", p, "USUARIO", new Vector2(-60f, 118f), new Vector2(140f, 24f));

            // Input USUARIO
            _accountInput = UIBuilder.CreateInputField(p, "AccountInput", "usuario...");
            Pos(_accountInput, new Vector2(0f, 82f), new Vector2(400f, 48f));
            _accountInput.GetComponent<Image>().color = new Color(0.08f, 0.04f, 0.18f, 1f);
            StyleInputText(_accountInput, 18f, Color.white, new Color(0.7f, 0.7f, 0.7f, 0.8f));

            // Label CONTRASENA
            MakeFormLabel("LblPass", p, "CONTRASENA", new Vector2(-55f, 20f), new Vector2(160f, 24f));

            // Input CONTRASENA
            _passwordInput = UIBuilder.CreateInputField(p, "PassInput", "contrasena...", true);
            Pos(_passwordInput, new Vector2(0f, -16f), new Vector2(400f, 48f));
            _passwordInput.GetComponent<Image>().color = new Color(0.08f, 0.04f, 0.18f, 1f);
            StyleInputText(_passwordInput, 18f, Color.white, new Color(0.7f, 0.7f, 0.7f, 0.8f));

            // Línea separadora
            var sep = MakeImg("Sep", p);
            sep.color = MuUITheme.GoldDark;
            SetRt(sep.GetComponent<RectTransform>(),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, -72f), new Vector2(380f, 1f));

            // Botón ENTRAR — usa b_connect.tga (sprite original MU)
            _loginButton = MakeSpriteButton(p, "BtnLogin",
                MuAssetLoader.Login.BtnConnect,
                new Vector2(0f, -112f), new Vector2(200f, 76f),
                "ENTRAR");

            // Botón REGISTRARSE — secundario pequeño
            _registerButton = UIBuilder.CreateButton(p, "BtnRegister", "CREAR CUENTA NUEVA", new Color(0.10f, 0.05f, 0.22f));
            Pos(_registerButton, new Vector2(0f, -196f), new Vector2(260f, 38f));
            _registerButton.GetComponent<Image>().color = new Color(0.10f, 0.05f, 0.22f);
            var rLbl = _registerButton.GetComponentInChildren<TextMeshProUGUI>();
            if (rLbl != null)
            {
                rLbl.text = "CREAR CUENTA NUEVA";
                rLbl.fontSize = 14f;
                rLbl.color = MuUITheme.PurpleLight;
                rLbl.fontStyle = FontStyles.Bold;
            }
            SetButtonColors(_registerButton,
                new Color(0.10f, 0.05f, 0.22f),
                new Color(0.22f, 0.12f, 0.40f),
                new Color(0.05f, 0.02f, 0.10f));

            return bg.gameObject;
        }

        private Button MakeSpriteButton(Transform parent, string name,
            string spritePath, Vector2 pos, Vector2 size, string fallbackLabel = "")
        {
            var go  = new GameObject(name);
            var rt  = go.AddComponent<RectTransform>();
            var img = go.AddComponent<Image>();
            var btn = go.AddComponent<Button>();
            go.transform.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;

            var spr = MuAssetLoader.Get(spritePath);
            if (spr != null)
            {
                img.sprite = spr;
                img.color  = Color.white;
                img.type   = Image.Type.Simple;
                img.preserveAspect = true;
            }
            else
            {
                img.color = new Color(0.55f, 0.35f, 0.02f);
                var lGo = new GameObject("Label");
                lGo.AddComponent<RectTransform>();
                var lTx = lGo.AddComponent<TextMeshProUGUI>();
                lGo.transform.SetParent(go.transform, false);
                UIBuilder.StretchFill(lGo.GetComponent<RectTransform>());
                lTx.text = fallbackLabel; lTx.fontSize = 18f;
                lTx.color = Color.white; lTx.fontStyle = FontStyles.Bold;
                lTx.alignment = TextAlignmentOptions.Center;
            }

            var colors = btn.colors;
            colors.highlightedColor = new Color(1.00f, 0.95f, 0.75f, 1.00f);
            colors.pressedColor     = new Color(0.65f, 0.55f, 0.35f, 1.00f);
            colors.fadeDuration     = 0.08f;
            btn.colors = colors;

            return btn;
        }

        private void MakeFormLabel(string name, Transform parent, string text, Vector2 pos, Vector2 size)
        {
            var go  = new GameObject(name);
            var rt  = go.AddComponent<RectTransform>();
            var tmp = go.AddComponent<TextMeshProUGUI>();
            go.transform.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            tmp.text      = text;
            tmp.fontSize  = 14f;
            tmp.fontStyle = FontStyles.Bold;
            tmp.color     = MuUITheme.GoldPrimary;
            tmp.alignment = TextAlignmentOptions.MidlineLeft;
            tmp.raycastTarget = false;
        }

        private static void Pos(Component c, Vector2 pos, Vector2 size)
        {
            var rt = c.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
        }

        // ── Panel de Registro ─────────────────────────────────────────────────

        private GameObject BuildRegisterPanel(Transform root)
        {
            var bg = MakeImg("RegisterPanel", root);
            bg.color = new Color(0.04f, 0.02f, 0.12f, 0.97f);
            SetRt(bg.GetComponent<RectTransform>(),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0, 10f), new Vector2(466f, 500f));
            bg.gameObject.AddComponent<Outline>().effectColor = MuUITheme.PurplePrimary;

            // Header
            var hdr = MakeImg("Header", bg.transform);
            hdr.color = new Color(0.18f, 0.08f, 0.38f, 1f);
            SetRt(hdr.GetComponent<RectTransform>(),
                new Vector2(0f, 1f), new Vector2(1f, 1f),
                Vector2.zero, new Vector2(0, 46), new Vector2(0.5f, 1f));

            var ht = new GameObject("HdrTxt");
            var htRt = ht.AddComponent<RectTransform>();
            var htTx = ht.AddComponent<TextMeshProUGUI>();
            ht.transform.SetParent(hdr.transform, false);
            UIBuilder.StretchFill(htRt);
            htTx.text = "CREAR  CUENTA"; htTx.fontSize = 15f;
            htTx.fontStyle = FontStyles.Bold;
            htTx.color = MuUITheme.GoldBright;
            htTx.alignment = TextAlignmentOptions.Center;

            var rs = bg.gameObject.AddComponent<RegisterScreen>();
            rs.Init(bg.GetComponent<RectTransform>(), _loginPanel);

            return bg.gameObject;
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private Image MakeImg(string name, Transform parent, bool stretch = false)
        {
            var go  = new GameObject(name);
            var rt  = go.AddComponent<RectTransform>();
            var img = go.AddComponent<Image>();
            go.transform.SetParent(parent, false);
            if (stretch)
            {
                rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
                rt.offsetMin = rt.offsetMax = Vector2.zero;
            }
            return img;
        }

        private TextMeshProUGUI MakeLabel(string name, Transform parent,
            Vector2 pos, Vector2 size, float fontSize, Color color,
            TextAlignmentOptions align,
            Vector2? anchorMin = null, Vector2? anchorMax = null, Vector2? pivot = null)
        {
            var go  = new GameObject(name);
            var rt  = go.AddComponent<RectTransform>();
            var tmp = go.AddComponent<TextMeshProUGUI>();
            go.transform.SetParent(parent, false);
            rt.anchorMin        = anchorMin ?? new Vector2(0.5f, 0.5f);
            rt.anchorMax        = anchorMax ?? new Vector2(0.5f, 0.5f);
            rt.pivot            = pivot     ?? new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta        = size;
            tmp.fontSize  = fontSize;
            tmp.color     = color;
            tmp.alignment = align;
            return tmp;
        }

        private void SetRt(RectTransform rt, Vector2 aMin, Vector2 aMax,
            Vector2 pos, Vector2 size, Vector2? pivot = null)
        {
            rt.anchorMin = aMin; rt.anchorMax = aMax;
            rt.pivot = pivot ?? new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos; rt.sizeDelta = size;
        }

        private void StyleInputText(TMP_InputField field,
            float fontSize = 16f,
            Color? textColor = null,
            Color? placeholderColor = null)
        {
            if (field.textComponent != null)
            {
                field.textComponent.color    = textColor ?? MuUITheme.TextPrimary;
                field.textComponent.fontSize = fontSize;
            }
            if (field.placeholder is TextMeshProUGUI ph)
            {
                ph.color    = placeholderColor ?? new Color(0.55f, 0.55f, 0.55f, 0.8f);
                ph.fontSize = fontSize;
            }
        }

        private void SetButtonColors(Button btn, Color normal, Color hover, Color pressed)
        {
            var c = btn.colors;
            c.normalColor      = normal;
            c.highlightedColor = hover;
            c.pressedColor     = pressed;
            c.fadeDuration     = 0.08f;
            btn.colors = c;
        }

        // ── Eventos ───────────────────────────────────────────────────────────

        private void OnConnected(NetworkEvents.Connected _)
        {
            SetStatus("Introduce tus credenciales.", MuUITheme.TextSuccess);
            SetInteractable(true);
        }

        private void OnDisconnected(NetworkEvents.Disconnected evt)
        {
            SetStatus($"Sin conexion: {evt.Reason}", MuUITheme.TextError);
            SetInteractable(false);
            _spinner?.SetActive(false);
        }

        private void OnLoginSuccess(AuthEvents.LoginSuccess _)
        {
            SetStatus("Cargando personajes...", MuUITheme.TextSuccess);
            // Solicitar lista de personajes al servidor
            NetworkClient.Instance?.Send(ClientPackets.RequestCharacterList());
        }

        private void OnLoginFailed(AuthEvents.LoginFailed evt)
        {
            SetStatus(evt.Message, MuUITheme.TextError);
            SetInteractable(true);
            _spinner?.SetActive(false);
        }

        private void OnCharacterListReceived(AuthEvents.CharacterListReceived _)
        {
            if (SceneTransitionManager.Instance != null)
                SceneTransitionManager.Instance.LoadScene("CharacterSelect");
            else
                UnityEngine.SceneManagement.SceneManager.LoadScene("CharacterSelect");
        }

        private void OnLoginPressed()
        {
            var acc = _accountInput?.text.Trim() ?? "";
            var pwd = _passwordInput?.text ?? "";
            if (acc.Length < 3) { SetStatus("Usuario: minimo 3 caracteres.", MuUITheme.TextError); return; }
            if (pwd.Length < 4) { SetStatus("Contrasena: minimo 4 caracteres.", MuUITheme.TextError); return; }

            if (NetworkClient.Instance == null || !NetworkClient.Instance.IsConnected)
            {
                SetStatus("Sin conexion con el servidor. Reintentando...", MuUITheme.TextError);
                GameManager.Instance?.ConnectToServer();
                return;
            }

            SetInteractable(false);
            _spinner?.SetActive(true);
            SetStatus("Autenticando...", MuUITheme.TextWarning);
            NetworkClient.Instance.Send(ClientPackets.Login(acc, pwd));
        }

        private void OnRegisterPanelPressed()
        {
            _loginPanel?.SetActive(false);
            _registerPanel?.SetActive(true);
        }

        private void SetInteractable(bool v)
        {
            if (_loginButton    != null) _loginButton.interactable    = v;
            if (_registerButton != null) _registerButton.interactable = v;
            if (_accountInput   != null) _accountInput.interactable   = v;
            if (_passwordInput  != null) _passwordInput.interactable  = v;
        }

        private void SetStatus(string msg, Color color)
        {
            if (_statusText == null) return;
            _statusText.text  = msg;
            _statusText.color = color;
        }
    }
}
