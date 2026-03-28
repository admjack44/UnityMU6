using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using MuOnline.Core;
using MuOnline.Network;
using MuOnline.Network.Packets;

namespace MuOnline.UI
{
    /// <summary>
    /// Pantalla selección de personaje — Layout fiel a MU Online Season 6.
    ///
    /// Composición:
    ///  ┌─────────────────────────────────────────────────┐
    ///  │  TOP BAR (50px)  Logo | Título | Servidor       │
    ///  ├──────────┬──────────────────────────────────────┤
    ///  │ SLOTS    │                                      │
    ///  │ (220px)  │   ARTE PERSONAJE  (resto pantalla)   │
    ///  │          │   DarkKnight Idle por defecto         │
    ///  ├──────────┴──────────────────────────────────────┤
    ///  │  BOTTOM BAR (90px)  [ENTRAR] [NUEVO] [ELIMINAR] │
    ///  └─────────────────────────────────────────────────┘
    /// </summary>
    [DefaultExecutionOrder(10)]
    public class CharacterSelectScreen : MonoBehaviour
    {
        // ── Datos ─────────────────────────────────────────────────────────────
        private MuCharacterInfo[] _characters;
        private int               _selectedIndex = -1;

        // ── UI refs ───────────────────────────────────────────────────────────
        private Image           _charArtImg;
        private TextMeshProUGUI _charNameTxt;
        private TextMeshProUGUI _charInfoTxt;
        private TextMeshProUGUI _slotCountTxt;
        private TextMeshProUGUI _statusTxt;
        private Button          _playBtn;
        private Button          _createBtn;
        private Button          _deleteBtn;
        private Transform       _slotsRoot;

        // Modales
        private GameObject      _createPanel;
        private GameObject      _deletePanel;
        private TMP_InputField  _createNameInput;
        private TextMeshProUGUI _createStatusTxt;
        private int             _selectedClass = 0;   // índice de clase seleccionada
        private TextMeshProUGUI _classLabelTxt;       // muestra la clase actual

        // Slot visual references
        private readonly List<Button> _slotBtns  = new();
        private readonly List<Image>  _slotImgs  = new();

        // ── Paleta MU Season 6 ────────────────────────────────────────────────
        static readonly Color Gold      = new(1.00f, 0.85f, 0.25f, 1f);
        static readonly Color GoldDim   = new(0.60f, 0.45f, 0.07f, 1f);
        static readonly Color GoldBrt   = new(1.00f, 0.96f, 0.68f, 1f);
        static readonly Color PanelDark = new(0.02f, 0.03f, 0.10f, 0.94f);
        static readonly Color SlotNorm  = new(0.04f, 0.05f, 0.16f, 0.92f);
        static readonly Color SlotSel   = new(0.08f, 0.14f, 0.38f, 1.00f);
        static readonly Color BlueLight = new(0.55f, 0.72f, 1.00f, 1f);

        const int TOP_H    = 50;
        const int BOTTOM_H = 90;
        const int PANEL_W  = 220;

        // ── Unity lifecycle ───────────────────────────────────────────────────
        void Awake() => BuildUI();

        void OnEnable()
        {
            EventBus.Subscribe<AuthEvents.CharacterListReceived>(OnCharListReceived);
            EventBus.Subscribe<AuthEvents.CharacterSelected>(OnCharSelected);
            EventBus.Subscribe<AuthEvents.CharacterCreateSuccess>(OnCharCreateSuccess);
            EventBus.Subscribe<AuthEvents.CharacterCreateFailed>(OnCharCreateFailed);
        }

        void OnDisable()
        {
            EventBus.Unsubscribe<AuthEvents.CharacterListReceived>(OnCharListReceived);
            EventBus.Unsubscribe<AuthEvents.CharacterSelected>(OnCharSelected);
            EventBus.Unsubscribe<AuthEvents.CharacterCreateSuccess>(OnCharCreateSuccess);
            EventBus.Unsubscribe<AuthEvents.CharacterCreateFailed>(OnCharCreateFailed);
        }

        void Start()
        {
            _playBtn.onClick.AddListener(OnPlayPressed);
            _createBtn.onClick.AddListener(() => ShowModal(_createPanel));
            _deleteBtn.onClick.AddListener(() => ShowModal(_deletePanel));

            if (NetworkClient.Instance != null && NetworkClient.Instance.IsConnected)
                NetworkClient.Instance.Send(ClientPackets.RequestCharacterList());
            else
                Status("Sin conexion al servidor.", MuUITheme.TextError);
        }

        // ═══════════════════════════════════════════════════════════════════════
        // BUILD UI
        // ═══════════════════════════════════════════════════════════════════════

        void BuildUI()
        {
            // EventSystem — imprescindible para que los botones reciban clics.
            // Se crea solo si no existe ya en la escena (evita duplicados).
            if (FindFirstObjectByType<EventSystem>() == null)
            {
                var esGo = new GameObject("EventSystem");
                esGo.AddComponent<EventSystem>();
                esGo.AddComponent<InputSystemUIInputModule>();
                DontDestroyOnLoad(esGo);
            }

            // Canvas raíz
            var cvGo = new GameObject("CharSelectCanvas");
            var cv   = cvGo.AddComponent<Canvas>();
            cv.renderMode = RenderMode.ScreenSpaceOverlay;
            cv.sortingOrder = 10;
            var sc = cvGo.AddComponent<CanvasScaler>();
            sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            sc.referenceResolution = new Vector2(1920, 1080);
            sc.matchWidthOrHeight = 0.5f;
            cvGo.AddComponent<GraphicRaycaster>();
            cvGo.transform.SetParent(transform, false);
            Transform root = cvGo.transform;

            // 1) Fondo
            BuildBG(root);

            // 2) Arte personaje (capa media)
            BuildCharArt(root);

            // 3) Panel de slots (encima del arte)
            BuildSlotsPanel(root);

            // 4) Barras superiores/inferiores — van DESPUÉS del contenido para
            //    quedar más arriba en la jerarquía y recibir raycasts primero
            BuildTopBar(root);
            BuildBottomBar(root);

            // 5) Modales (top layer)
            _createPanel = BuildCreateModal(root);
            _deletePanel = BuildDeleteModal(root);
            _createPanel.SetActive(false);
            _deletePanel.SetActive(false);
        }

        // ── Fondo ─────────────────────────────────────────────────────────────

        void BuildBG(Transform root)
        {
            // Negro base
            var bg = Img("BG", root, stretch: true);
            bg.color = Color.black;
            bg.raycastTarget = false;
            bg.raycastTarget = false;

            // Fondo atmosférico MU S6 — Logo/UI/back1 (29KB, imagen real del juego)
            TryBgSprite(root,
                MuAssetLoader.LogoScreen.Back1,
                MuAssetLoader.LogoScreen.Back2,
                MuAssetLoader.LogoScreen.LoginBack1,
                MuAssetLoader.Login.BgArt1);

            // Viñeta oscura en bordes para dar profundidad
            var ov = Img("Vignette", root, stretch: true);
            ov.color = new Color(0f, 0f, 0f, 0.30f);
            ov.raycastTarget = false;
        }

        void TryBgSprite(Transform root, params string[] paths)
        {
            foreach (var path in paths)
            {
                var spr = MuAssetLoader.Get(path);
                if (spr == null) continue;
                var img = Img("BGArt", root, stretch: true);
                img.sprite = spr;
                img.color  = Color.white;
                img.preserveAspect = false;
                img.raycastTarget  = false;
                return;
            }
        }

        // ── Top bar ───────────────────────────────────────────────────────────

        void BuildTopBar(Transform root)
        {
            var bar = Img("TopBar", root);
            bar.color = new Color(0f, 0f, 0.05f, 0.93f);
            bar.raycastTarget = false;
            AnchorTop(bar.GetComponent<RectTransform>(), TOP_H);

            // Línea dorada inferior
            var ln = Img("TopLine", root);
            ln.color = GoldDim;
            ln.raycastTarget = false;
            var lrt = ln.GetComponent<RectTransform>();
            lrt.anchorMin = new Vector2(0, 1); lrt.anchorMax = new Vector2(1, 1);
            lrt.pivot = new Vector2(0.5f, 1f);
            lrt.anchoredPosition = new Vector2(0, -TOP_H);
            lrt.sizeDelta = new Vector2(0, 1f);

            // Logo MU izquierda
            var logoSpr = MuAssetLoader.Get(MuAssetLoader.LogoScreen.MuLogoGold)
                       ?? MuAssetLoader.Get(MuAssetLoader.LogoScreen.MuLogoSmall);
            if (logoSpr != null)
            {
                var li = Img("Logo", bar.transform);
                li.sprite = logoSpr; li.color = Color.white;
                li.preserveAspect = true; li.raycastTarget = false;
                var lr = li.GetComponent<RectTransform>();
                lr.anchorMin = new Vector2(0, 0); lr.anchorMax = new Vector2(0, 1);
                lr.pivot = new Vector2(0, 0.5f);
                lr.anchoredPosition = new Vector2(8, 0);
                lr.sizeDelta = new Vector2(70, -8);
            }

            // Título
            var ttl = Lbl("Title", bar.transform, 0f, 1f, Vector2.zero, new Vector2(0, 0));
            ttl.text = "SELECCIÓN  DE  PERSONAJE";
            ttl.fontSize = 17f; ttl.fontStyle = FontStyles.Bold;
            ttl.color = Gold; ttl.alignment = TextAlignmentOptions.Center;
            ttl.raycastTarget = false;

            // Servidor derecha
            var srv = Lbl("Srv", bar.transform, 1f, 1f, new Vector2(-12, 0), new Vector2(260, 0));
            srv.text = "MU PEGASO  ·  Servidor 1";
            srv.fontSize = 11f; srv.color = BlueLight;
            srv.alignment = TextAlignmentOptions.Right;
            srv.raycastTarget = false;
        }

        // ── Arte del personaje (centro, detrás del panel de slots) ────────────

        void BuildCharArt(Transform root)
        {
            // Contenedor que ocupa todo excepto las barras
            var area = new GameObject("ArtArea").AddComponent<RectTransform>();
            area.transform.SetParent(root, false);
            area.anchorMin = Vector2.zero;
            area.anchorMax = Vector2.one;
            area.offsetMin = new Vector2(0, BOTTOM_H);
            area.offsetMax = new Vector2(0, -TOP_H);

            // Imagen principal del personaje — cubre desde el panel de slots hasta el borde derecho
            var artGo = new GameObject("CharArt");
            artGo.transform.SetParent(area, false);
            _charArtImg = artGo.AddComponent<Image>();
            _charArtImg.preserveAspect = true;
            _charArtImg.raycastTarget  = false;
            var artRt = artGo.GetComponent<RectTransform>();
            // Ocupar 75% derecho de la pantalla, centrado verticalmente
            artRt.anchorMin = new Vector2(0f, 0f);
            artRt.anchorMax = new Vector2(1f, 1f);
            artRt.offsetMin = new Vector2(PANEL_W + 6, 0);
            artRt.offsetMax = new Vector2(0, 0);

            // Mostrar DarkKnight de forma predeterminada (hace la pantalla viva desde el inicio)
            SetDefaultArt();

            // Panel nombre + info del personaje — fondo oscuro en la parte inferior del área de arte
            var infoPanel = Img("InfoPanel", area);
            infoPanel.color = new Color(0f, 0f, 0f, 0.68f);
            infoPanel.raycastTarget = false;
            var ipRt = infoPanel.GetComponent<RectTransform>();
            // Barra de info: fija al fondo del área de arte, altura 60px
            // offsetMin.x = PANEL_W+6 para que no cubra el panel de slots
            ipRt.anchorMin = new Vector2(0f, 0f); ipRt.anchorMax = new Vector2(1f, 0f);
            ipRt.pivot = new Vector2(0.5f, 0f);
            ipRt.anchoredPosition = Vector2.zero;
            ipRt.sizeDelta = new Vector2(-(PANEL_W + 6), 60);
            // Shift right by offset: reajustamos anchorMin para no cubrir slots
            ipRt.anchorMin = new Vector2(0f, 0f);
            ipRt.offsetMin = new Vector2(PANEL_W + 6, 0);
            ipRt.offsetMax = new Vector2(0, 60);

            // Línea dorada superior del info panel
            var ipLine = Img("IPLine", infoPanel.transform);
            ipLine.color = GoldDim; ipLine.raycastTarget = false;
            var ilRt = ipLine.GetComponent<RectTransform>();
            ilRt.anchorMin = new Vector2(0,1); ilRt.anchorMax = new Vector2(1,1);
            ilRt.pivot = new Vector2(0.5f, 1f);
            ilRt.anchoredPosition = Vector2.zero; ilRt.sizeDelta = new Vector2(0, 1f);

            // Nombre del personaje — mitad superior del info panel
            {
                var go = new GameObject("CharName");
                var rt = go.AddComponent<RectTransform>();
                _charNameTxt = go.AddComponent<TextMeshProUGUI>();
                go.transform.SetParent(infoPanel.transform, false);
                rt.anchorMin = new Vector2(0, 0.48f); rt.anchorMax = new Vector2(1, 1f);
                rt.offsetMin = new Vector2(8, 0); rt.offsetMax = new Vector2(-8, 0);
                _charNameTxt.text = ""; _charNameTxt.fontSize = 22f;
                _charNameTxt.fontStyle = FontStyles.Bold;
                _charNameTxt.color = GoldBrt;
                _charNameTxt.alignment = TextAlignmentOptions.Center;
                _charNameTxt.raycastTarget = false;
            }

            // Info (clase/nivel/mapa) — mitad inferior del info panel
            {
                var go = new GameObject("CharInfo");
                var rt = go.AddComponent<RectTransform>();
                _charInfoTxt = go.AddComponent<TextMeshProUGUI>();
                go.transform.SetParent(infoPanel.transform, false);
                rt.anchorMin = new Vector2(0, 0); rt.anchorMax = new Vector2(1, 0.50f);
                rt.offsetMin = new Vector2(8, 0); rt.offsetMax = new Vector2(-8, 0);
                _charInfoTxt.text = ""; _charInfoTxt.fontSize = 13f;
                _charInfoTxt.color = BlueLight;
                _charInfoTxt.alignment = TextAlignmentOptions.Center;
                _charInfoTxt.raycastTarget = false;
            }
        }

        void SetDefaultArt()
        {
            var spr = MuAssetLoader.Get(MuAssetLoader.CharSelect.DarkKnightIdle)
                   ?? MuAssetLoader.Get(MuAssetLoader.CharSelect.WizardIdle)
                   ?? MuAssetLoader.Get(MuAssetLoader.CharSelect.FairyElfIdle);
            if (spr != null)
            {
                _charArtImg.sprite = spr;
                _charArtImg.color  = new Color(1f, 1f, 1f, 0.85f);  // ligeramente transparente = "idle"
            }
        }

        // ── Panel de slots (izquierda) ─────────────────────────────────────────

        void BuildSlotsPanel(Transform root)
        {
            // Fondo del panel — anclado al lado izquierdo completo entre barras
            var panel = Img("SlotsPanel", root);
            panel.color = PanelDark;
            panel.raycastTarget = false;  // no bloquear clics de los botones de la barra inferior
            var rt = panel.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 0);
            rt.anchorMax = new Vector2(0, 1);
            rt.pivot     = new Vector2(0, 0.5f);
            // anchoredPosition.y = (BOTTOM_H - TOP_H)/2 centra el gap asimétrico
            rt.anchoredPosition = new Vector2(0, (BOTTOM_H - TOP_H) * 0.5f);
            rt.sizeDelta = new Vector2(PANEL_W, -(TOP_H + BOTTOM_H));

            // Marco del panel: usamos Interface02.tga como decoración de borde si existe
            var frameSpr = MuAssetLoader.Get(MuAssetLoader.LogoScreen.Interface2)
                        ?? MuAssetLoader.Get(MuAssetLoader.LogoScreen.Interface1);
            if (frameSpr != null)
            {
                var fi = Img("PanelFrame", panel.transform, stretch: true);
                fi.sprite = frameSpr; fi.color = Color.white;
                fi.type = Image.Type.Sliced; fi.raycastTarget = false;
            }
            else
            {
                // Sin sprite: borde dorado simple
                var ol = panel.gameObject.AddComponent<Outline>();
                ol.effectColor = GoldDim;
                ol.effectDistance = new Vector2(1, -1);
            }

            // Header del panel
            var hdr = Img("Header", panel.transform);
            hdr.color = new Color(0.05f, 0.04f, 0.18f, 1f);
            hdr.raycastTarget = false;
            var hRt = hdr.GetComponent<RectTransform>();
            hRt.anchorMin = new Vector2(0, 1); hRt.anchorMax = new Vector2(1, 1);
            hRt.pivot = new Vector2(0.5f, 1f);
            hRt.anchoredPosition = Vector2.zero; hRt.sizeDelta = new Vector2(0, 34);

            var hLine = Img("HLine", hdr.transform);
            hLine.color = GoldDim; hLine.raycastTarget = false;
            var hlRt = hLine.GetComponent<RectTransform>();
            hlRt.anchorMin = new Vector2(0, 0); hlRt.anchorMax = new Vector2(1, 0);
            hlRt.pivot = new Vector2(0.5f, 0f);
            hlRt.anchoredPosition = Vector2.zero; hlRt.sizeDelta = new Vector2(-4, 1f);

            _slotCountTxt = StrTxt("HdrTxt", hdr.transform, "PERSONAJES   0 / 5", 12f, Gold, bold: true);

            // Línea vertical dorada derecha del panel
            var vLine = Img("VLine", root);
            vLine.color = GoldDim; vLine.raycastTarget = false;
            var vlRt = vLine.GetComponent<RectTransform>();
            vlRt.anchorMin = new Vector2(0, 0); vlRt.anchorMax = new Vector2(0, 1);
            vlRt.pivot = new Vector2(0, 0.5f);
            vlRt.anchoredPosition = new Vector2(PANEL_W, (BOTTOM_H - TOP_H) * 0.5f);
            vlRt.sizeDelta = new Vector2(2, -(TOP_H + BOTTOM_H + 4));

            // Contenedor de slots
            var slotsGo = new GameObject("Slots").AddComponent<RectTransform>();
            slotsGo.transform.SetParent(panel.transform, false);
            slotsGo.anchorMin = new Vector2(0, 0);
            slotsGo.anchorMax = new Vector2(1, 1);
            slotsGo.offsetMin = new Vector2(3, 4);
            slotsGo.offsetMax = new Vector2(-3, -36);
            _slotsRoot = slotsGo.transform;

            var vlg = slotsGo.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(2, 2, 2, 2);
            vlg.spacing = 4;
            vlg.childForceExpandWidth  = true;
            vlg.childForceExpandHeight = false;
            vlg.childControlWidth  = true;
            vlg.childControlHeight = false;
        }

        // ── Bottom bar ────────────────────────────────────────────────────────

        void BuildBottomBar(Transform root)
        {
            var bar = Img("BottomBar", root);
            bar.color = new Color(0f, 0f, 0.04f, 0.95f);
            bar.raycastTarget = false;  // solo visual; los Buttons hijos manejan sus propios raycasts
            AnchorBottom(bar.GetComponent<RectTransform>(), BOTTOM_H);

            // Línea dorada superior del bar
            var ln = Img("BotLine", root);
            ln.color = GoldDim; ln.raycastTarget = false;
            var lrt = ln.GetComponent<RectTransform>();
            lrt.anchorMin = new Vector2(0, 0); lrt.anchorMax = new Vector2(1, 0);
            lrt.pivot = new Vector2(0.5f, 0f);
            lrt.anchoredPosition = new Vector2(0, BOTTOM_H);
            lrt.sizeDelta = new Vector2(0, 1f);

            // Status text — stretch horizontal, anclado al tope del bar
            {
                var go = new GameObject("Status");
                var rt = go.AddComponent<RectTransform>();
                _statusTxt = go.AddComponent<TextMeshProUGUI>();
                go.transform.SetParent(bar.transform, false);
                rt.anchorMin = new Vector2(0f, 1f); rt.anchorMax = new Vector2(1f, 1f);
                rt.pivot     = new Vector2(0.5f, 1f);
                rt.anchoredPosition = new Vector2(0, -4);
                rt.sizeDelta = new Vector2(0, 18);
                _statusTxt.fontSize = 10f; _statusTxt.color = BlueLight;
                _statusTxt.alignment = TextAlignmentOptions.Center;
                _statusTxt.raycastTarget = false;
            }

            // ── Botones con sprites MU originales ─────────────────────────────
            // Bar height = BOTTOM_H = 90px
            // anchor=(0.5,0.5) en bar → child y=0 está en el centro del bar (45px desde abajo)
            // Queremos botones un poco por debajo del centro: y = -5

            // [ENTRAR AL MUNDO] — botón principal, más grande
            _playBtn = SprBtn(bar.transform, "BtnPlay",
                MuAssetLoader.Login.BtnConnect,
                new Vector2(-290f, -4f), new Vector2(195f, 66f), "ENTRAR");
            _playBtn.interactable = false;

            // [NUEVO PERSONAJE]
            _createBtn = SprBtn(bar.transform, "BtnCreate",
                MuAssetLoader.Buttons.Create,
                new Vector2(-50f, -4f), new Vector2(170f, 58f), "NUEVO");

            // [ELIMINAR]
            _deleteBtn = SprBtn(bar.transform, "BtnDelete",
                MuAssetLoader.Buttons.Delete,
                new Vector2(170f, -4f), new Vector2(170f, 58f), "ELIMINAR");
            _deleteBtn.interactable = false;
        }

        // ── Slots ─────────────────────────────────────────────────────────────

        void RefreshSlots()
        {
            _slotBtns.Clear(); _slotImgs.Clear();
            foreach (Transform c in _slotsRoot) Destroy(c.gameObject);
            if (_characters == null) return;

            if (_slotCountTxt != null)
                _slotCountTxt.text = $"PERSONAJES   {_characters.Length} / 5";

            for (int i = 0; i < _characters.Length; i++) BuildSlot(_characters[i], i);
            for (int i = _characters.Length; i < 5; i++) BuildEmptySlot(i + 1);
        }

        void BuildSlot(MuCharacterInfo ch, int idx)
        {
            // Slot MU: fondo oscuro + borde + nombre + clase/nivel
            var go  = new GameObject($"Slot_{ch.Name}");
            var rt  = go.AddComponent<RectTransform>();
            var img = go.AddComponent<Image>();
            var btn = go.AddComponent<Button>();
            go.transform.SetParent(_slotsRoot, false);
            rt.sizeDelta = new Vector2(0, 58);
            img.color = SlotNorm;

            var ol = go.AddComponent<Outline>();
            ol.effectColor    = GoldDim;
            ol.effectDistance = new Vector2(1, -1);

            int cap = idx;
            btn.onClick.AddListener(() => SelectSlot(cap));
            var bc = btn.colors;
            bc.highlightedColor = new Color(0.10f, 0.18f, 0.40f, 1f);
            bc.fadeDuration = 0.06f;
            btn.colors = bc;

            // Franja de color de clase — borde izquierdo
            var stripe = Img("Stripe", go.transform);
            stripe.color = MuUITheme.GetClassColor(ch.Class);
            stripe.raycastTarget = false;
            var srt = stripe.GetComponent<RectTransform>();
            srt.anchorMin = new Vector2(0, 0); srt.anchorMax = new Vector2(0, 1);
            srt.pivot = new Vector2(0, 0.5f);
            srt.anchoredPosition = new Vector2(0, 0); srt.sizeDelta = new Vector2(3, -4);

            // Mini-retrato
            var port = MuAssetLoader.Get(MuAssetLoader.CharPortraits.ByClass(ch.Class));
            float nameOffX = 8f;
            if (port != null)
            {
                var pi = Img("Port", go.transform);
                pi.sprite = port; pi.color = Color.white;
                pi.preserveAspect = true; pi.raycastTarget = false;
                var prt = pi.GetComponent<RectTransform>();
                prt.anchorMin = new Vector2(0, 0); prt.anchorMax = new Vector2(0, 1);
                prt.pivot = new Vector2(0, 0.5f);
                prt.anchoredPosition = new Vector2(5, 0); prt.sizeDelta = new Vector2(44, -6);
                nameOffX = 54f;
            }

            // Nombre
            var nm = new GameObject("Name").AddComponent<RectTransform>();
            nm.transform.SetParent(go.transform, false);
            var nmTx = nm.gameObject.AddComponent<TextMeshProUGUI>();
            nm.anchorMin = new Vector2(0, 0.48f); nm.anchorMax = new Vector2(1, 1f);
            nm.offsetMin = new Vector2(nameOffX, 0); nm.offsetMax = new Vector2(-4, 0);
            nmTx.text = ch.Name; nmTx.fontSize = 13f;
            nmTx.fontStyle = FontStyles.Bold;
            nmTx.color = GoldBrt;
            nmTx.alignment = TextAlignmentOptions.MidlineLeft;
            nmTx.raycastTarget = false;

            // Clase • Nivel
            var inf = new GameObject("Info").AddComponent<RectTransform>();
            inf.transform.SetParent(go.transform, false);
            var infTx = inf.gameObject.AddComponent<TextMeshProUGUI>();
            inf.anchorMin = new Vector2(0, 0); inf.anchorMax = new Vector2(1, 0.50f);
            inf.offsetMin = new Vector2(nameOffX, 0); inf.offsetMax = new Vector2(-4, 0);
            infTx.text = $"{GetClassName(ch.Class)}  Lv.{ch.Level}";
            infTx.fontSize = 10f; infTx.color = BlueLight;
            infTx.alignment = TextAlignmentOptions.MidlineLeft;
            infTx.raycastTarget = false;

            _slotBtns.Add(btn); _slotImgs.Add(img);
        }

        void BuildEmptySlot(int n)
        {
            var go  = new GameObject($"Empty_{n}");
            var rt  = go.AddComponent<RectTransform>();
            var img = go.AddComponent<Image>();
            go.transform.SetParent(_slotsRoot, false);
            rt.sizeDelta = new Vector2(0, 58);
            img.color = new Color(0.02f, 0.02f, 0.08f, 0.70f);

            var ol = go.AddComponent<Outline>();
            ol.effectColor = new Color(0.15f, 0.15f, 0.28f, 0.6f);
            ol.effectDistance = new Vector2(1, -1);

            var lbl = StrTxt("Lbl", go.transform,
                $"—  DISPONIBLE  —", 10f,
                new Color(0.20f, 0.22f, 0.35f, 1f));
            lbl.fontStyle = FontStyles.Italic;

            _slotBtns.Add(null); _slotImgs.Add(null);
        }

        void SelectSlot(int idx)
        {
            if (_characters == null || idx >= _characters.Length) return;
            _selectedIndex = idx;
            var ch = _characters[idx];

            var spr = MuAssetLoader.Get(MuAssetLoader.CharSelect.SelectedByClass(ch.Class))
                   ?? MuAssetLoader.Get(MuAssetLoader.CharSelect.IdleByClass(ch.Class));
            if (spr != null)
            {
                _charArtImg.sprite = spr;
                _charArtImg.color  = Color.white;
            }

            _charNameTxt.text  = ch.Name;
            _charInfoTxt.text  = $"{GetClassName(ch.Class)}   •   Nivel {ch.Level}   •   {MapName(ch.MapId)}";
            _charInfoTxt.color = MuUITheme.GetClassColor(ch.Class);

            UpdateBtns();

            for (int i = 0; i < _slotImgs.Count; i++)
                if (_slotImgs[i] != null)
                    _slotImgs[i].color = (i == idx) ? SlotSel : SlotNorm;
        }

        void UpdateBtns()
        {
            bool sel = _selectedIndex >= 0 && _characters != null && _selectedIndex < _characters.Length;
            _playBtn.interactable   = sel;
            _deleteBtn.interactable = sel;
        }

        // ── Modales ───────────────────────────────────────────────────────────

        GameObject BuildCreateModal(Transform root)
        {
            var ov = Overlay(root, "CreateOv");
            var p  = PanelBox(ov.transform, "CreatePanel", new Vector2(480, 400),
                              new Color(0.03f, 0.02f, 0.12f, 0.98f), GoldDim);

            StrTxt("HDR", PanelHdr(p, 44), "NUEVO PERSONAJE", 14f, GoldBrt, bold: true);

            // ── Nombre ────────────────────────────────────────────────────────
            TxtAt("LblN", p, "NOMBRE DEL PERSONAJE:", new Vector2(0, 118), new Vector2(420, 20), 11f, Gold);
            _createNameInput = UIBuilder.CreateInputField(p, "NameIn", "Minimo 4 caracteres...");
            Pos(_createNameInput.GetComponent<RectTransform>(), new Vector2(0, 78), new Vector2(420, 46));
            _createNameInput.GetComponent<Image>().color = new Color(0.06f, 0.03f, 0.16f);

            // ── Selector de clase — sin TMP_Dropdown (evita crash por falta de Template) ──
            TxtAt("LblC", p, "CLASE:", new Vector2(0, 20), new Vector2(420, 20), 11f, Gold);

            // Fondo del selector
            var selBg = new GameObject("ClassSel");
            selBg.AddComponent<RectTransform>();
            selBg.AddComponent<Image>().color = new Color(0.06f, 0.03f, 0.16f);
            selBg.transform.SetParent(p, false);
            Pos(selBg.GetComponent<RectTransform>(), new Vector2(0, -14), new Vector2(420, 42));
            selBg.AddComponent<Outline>().effectColor = GoldDim;

            // Botón ◄ anterior
            var prevBtn = new GameObject("Prev").AddComponent<RectTransform>();
            prevBtn.transform.SetParent(selBg.transform, false);
            var prevImg = prevBtn.gameObject.AddComponent<Image>();
            prevImg.color = new Color(0.12f, 0.06f, 0.28f);
            var prevBtnC = prevBtn.gameObject.AddComponent<Button>();
            prevBtn.anchorMin = new Vector2(0, 0); prevBtn.anchorMax = new Vector2(0, 1);
            prevBtn.pivot = new Vector2(0, 0.5f);
            prevBtn.anchoredPosition = Vector2.zero; prevBtn.sizeDelta = new Vector2(42, 0);
            var prevTxt = new GameObject("T").AddComponent<RectTransform>();
            prevTxt.transform.SetParent(prevBtn.transform, false);
            UIBuilder.StretchFill(prevTxt);
            var pt = prevTxt.gameObject.AddComponent<TextMeshProUGUI>();
            pt.text = "◄"; pt.fontSize = 16f; pt.color = Gold;
            pt.alignment = TextAlignmentOptions.Center; pt.raycastTarget = false;
            prevBtnC.onClick.AddListener(() => CycleClass(-1));

            // Etiqueta de clase
            var clsLbl = new GameObject("ClassLbl").AddComponent<RectTransform>();
            clsLbl.transform.SetParent(selBg.transform, false);
            clsLbl.anchorMin = new Vector2(0, 0); clsLbl.anchorMax = new Vector2(1, 1);
            clsLbl.offsetMin = new Vector2(44, 0); clsLbl.offsetMax = new Vector2(-44, 0);
            _classLabelTxt = clsLbl.gameObject.AddComponent<TextMeshProUGUI>();
            _classLabelTxt.text = GetClassName((CharacterClass)0);
            _classLabelTxt.fontSize = 14f; _classLabelTxt.color = GoldBrt;
            _classLabelTxt.fontStyle = FontStyles.Bold;
            _classLabelTxt.alignment = TextAlignmentOptions.Center;
            _classLabelTxt.raycastTarget = false;

            // Botón ► siguiente
            var nextBtn = new GameObject("Next").AddComponent<RectTransform>();
            nextBtn.transform.SetParent(selBg.transform, false);
            var nextImg = nextBtn.gameObject.AddComponent<Image>();
            nextImg.color = new Color(0.12f, 0.06f, 0.28f);
            var nextBtnC = nextBtn.gameObject.AddComponent<Button>();
            nextBtn.anchorMin = new Vector2(1, 0); nextBtn.anchorMax = new Vector2(1, 1);
            nextBtn.pivot = new Vector2(1, 0.5f);
            nextBtn.anchoredPosition = Vector2.zero; nextBtn.sizeDelta = new Vector2(42, 0);
            var nextTxt = new GameObject("T").AddComponent<RectTransform>();
            nextTxt.transform.SetParent(nextBtn.transform, false);
            UIBuilder.StretchFill(nextTxt);
            var nt = nextTxt.gameObject.AddComponent<TextMeshProUGUI>();
            nt.text = "►"; nt.fontSize = 16f; nt.color = Gold;
            nt.alignment = TextAlignmentOptions.Center; nt.raycastTarget = false;
            nextBtnC.onClick.AddListener(() => CycleClass(+1));

            // ── Estado y botones ──────────────────────────────────────────────
            _createStatusTxt = TxtAt("Sts", p, "", new Vector2(0, -72), new Vector2(420, 24), 11f, MuUITheme.TextError);

            SprBtn(p, "BtnOK",  MuAssetLoader.Buttons.OK,    new Vector2(-110, -112), new Vector2(155, 46), "CREAR")
                .onClick.AddListener(OnConfirmCreate);
            SprBtn(p, "BtnCan", MuAssetLoader.Buttons.Cancel, new Vector2( 110, -112), new Vector2(155, 46), "CANCELAR")
                .onClick.AddListener(() => ov.SetActive(false));

            return ov;
        }

        void CycleClass(int dir)
        {
            int total = System.Enum.GetValues(typeof(CharacterClass)).Length;
            _selectedClass = (_selectedClass + dir + total) % total;
            if (_classLabelTxt != null)
                _classLabelTxt.text = GetClassName((CharacterClass)_selectedClass);
        }

        GameObject BuildDeleteModal(Transform root)
        {
            var ov = Overlay(root, "DeleteOv");
            var p  = PanelBox(ov.transform, "DelPanel", new Vector2(440, 220),
                              new Color(0.08f, 0.01f, 0.01f, 0.98f), MuUITheme.TextError);

            TxtAt("T", p, "ELIMINAR PERSONAJE",  new Vector2(0, 70), new Vector2(400, 32), 17f, MuUITheme.TextError);
            TxtAt("W", p, "Esta accion no se puede deshacer.\nEl personaje sera eliminado permanentemente.",
                new Vector2(0, 15), new Vector2(400, 46), 11f, MuUITheme.TextSecondary);

            SprBtn(p, "BtnDel", MuAssetLoader.Buttons.Delete, new Vector2(-100,-68), new Vector2(150,46), "ELIMINAR")
                .onClick.AddListener(OnConfirmDelete);
            SprBtn(p, "BtnCan", MuAssetLoader.Buttons.Cancel, new Vector2( 100,-68), new Vector2(150,46), "CANCELAR")
                .onClick.AddListener(() => ov.SetActive(false));

            return ov;
        }

        // ── Eventos de red ────────────────────────────────────────────────────

        void OnCharListReceived(AuthEvents.CharacterListReceived e)
        {
            _characters    = e.Characters;
            _selectedIndex = -1;

            // Si hay personajes, auto-seleccionar el primero
            RefreshSlots();
            UpdateBtns();

            if (_characters.Length > 0)
                SelectSlot(0);
            else
            {
                SetDefaultArt();
                _charNameTxt.text = "";
                _charInfoTxt.text = "";
            }

            Status($"{_characters.Length} personaje(s).", MuUITheme.TextSecondary);
        }

        void OnCharSelected(AuthEvents.CharacterSelected _)
            => SceneTransitionManager.Instance?.LoadScene("Game");

        void OnPlayPressed()
        {
            if (_selectedIndex < 0 || _characters == null) return;
            Status("Entrando al mundo...", MuUITheme.TextWarning);
            var ch = _characters[_selectedIndex];
            var gm = GameManager.Instance;
            if (gm != null)
            {
                gm.ClearCachedStats();
                gm.SetLocalPlayer(new PlayerSession
                {
                    AccountName     = gm.CurrentAccountName,
                    CharacterName   = ch.Name,
                    CharacterLevel  = ch.Level,
                    CharacterClass  = ch.Class,
                    MapId           = ch.MapId
                });
            }
            NetworkClient.Instance.Send(ClientPackets.SelectCharacter(ch.Name));
        }

        void OnConfirmCreate()
        {
            var name = _createNameInput?.text.Trim() ?? "";
            if (name.Length < 4)
            {
                if (_createStatusTxt != null)
                    _createStatusTxt.text = "El nombre necesita al menos 4 caracteres.";
                return;
            }
            var cls = (CharacterClass)_selectedClass;
            NetworkClient.Instance.Send(ClientPackets.CreateCharacter(name, cls));
            _createPanel.SetActive(false);
            Status($"Creando {name} ({GetClassName(cls)})...", MuUITheme.TextWarning);
        }

        void OnConfirmDelete()
        {
            if (_selectedIndex < 0 || _characters == null) return;
            NetworkClient.Instance.Send(
                ClientPackets.DeleteCharacter(_characters[_selectedIndex].Name, "1234"));
            _deletePanel.SetActive(false);
            _selectedIndex = -1; SetDefaultArt();
            _charNameTxt.text = ""; _charInfoTxt.text = "";
            Status("Eliminando personaje...", MuUITheme.TextWarning);
            Invoke(nameof(RefreshList), 0.6f);
        }

        void RefreshList() => NetworkClient.Instance?.Send(ClientPackets.RequestCharacterList());

        void OnCharCreateSuccess(AuthEvents.CharacterCreateSuccess _)
        {
            Status("Personaje creado.", MuUITheme.TextSuccess);
            RefreshList();  // pedir lista actualizada inmediatamente
        }

        void OnCharCreateFailed(AuthEvents.CharacterCreateFailed e)
        {
            Status($"Error: {e.Message}", MuUITheme.TextError);
        }

        void ShowModal(GameObject m)
        {
            _createPanel.SetActive(false); _deletePanel.SetActive(false);
            m.SetActive(true);
        }

        void Status(string msg, Color col)
        {
            if (_statusTxt == null) return;
            _statusTxt.text = msg; _statusTxt.color = col;
        }

        // ═══════════════════════════════════════════════════════════════════════
        // HELPERS UI
        // ═══════════════════════════════════════════════════════════════════════

        Image Img(string name, Transform parent, bool stretch = false)
        {
            var go  = new GameObject(name);
            var rt  = go.AddComponent<RectTransform>();
            var img = go.AddComponent<Image>();
            go.transform.SetParent(parent, false);
            if (stretch) { rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero; }
            return img;
        }

        TextMeshProUGUI Lbl(string name, Transform parent,
            float aX, float aY, Vector2 pos, Vector2 size)
        {
            var go  = new GameObject(name);
            var rt  = go.AddComponent<RectTransform>();
            var t   = go.AddComponent<TextMeshProUGUI>();
            go.transform.SetParent(parent, false);
            rt.anchorMin = new Vector2(aX, aY); rt.anchorMax = new Vector2(aX, aY);
            rt.pivot = new Vector2(aX, aY);
            rt.anchoredPosition = pos; rt.sizeDelta = size;
            return t;
        }

        TextMeshProUGUI StrTxt(string name, Transform parent, string text,
            float fs, Color color, bool bold = false)
        {
            var go  = new GameObject(name);
            var rt  = go.AddComponent<RectTransform>();
            var t   = go.AddComponent<TextMeshProUGUI>();
            go.transform.SetParent(parent, false);
            UIBuilder.StretchFill(rt);
            t.text = text; t.fontSize = fs; t.color = color;
            t.alignment = TextAlignmentOptions.Center;
            t.raycastTarget = false;
            if (bold) t.fontStyle = FontStyles.Bold;
            return t;
        }

        TextMeshProUGUI TxtAt(string name, Transform parent, string text,
            Vector2 pos, Vector2 size, float fs, Color color,
            TextAlignmentOptions align = TextAlignmentOptions.Center)
        {
            var go  = new GameObject(name);
            var rt  = go.AddComponent<RectTransform>();
            var t   = go.AddComponent<TextMeshProUGUI>();
            go.transform.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos; rt.sizeDelta = size;
            t.text = text; t.fontSize = fs; t.color = color;
            t.alignment = align; t.raycastTarget = false;
            return t;
        }

        Button SprBtn(Transform parent, string name, string spritePath,
            Vector2 pos, Vector2 size, string fallback = "")
        {
            var go  = new GameObject(name);
            var rt  = go.AddComponent<RectTransform>();
            var img = go.AddComponent<Image>();
            var btn = go.AddComponent<Button>();
            go.transform.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos; rt.sizeDelta = size;

            var spr = MuAssetLoader.Get(spritePath);
            if (spr != null)
            {
                img.sprite = spr; img.color = Color.white;
                img.type = Image.Type.Simple; img.preserveAspect = true;
            }
            else
            {
                img.color = new Color(0.25f, 0.14f, 0.02f);
                var lGo = new GameObject("L"); lGo.AddComponent<RectTransform>();
                var lT  = lGo.AddComponent<TextMeshProUGUI>();
                lGo.transform.SetParent(go.transform, false);
                UIBuilder.StretchFill(lGo.GetComponent<RectTransform>());
                lT.text = fallback; lT.fontSize = 13f;
                lT.color = GoldBrt; lT.fontStyle = FontStyles.Bold;
                lT.alignment = TextAlignmentOptions.Center; lT.raycastTarget = false;
            }

            var c = btn.colors;
            c.highlightedColor = new Color(1f, 0.96f, 0.72f);
            c.pressedColor     = new Color(0.65f, 0.55f, 0.35f);
            c.disabledColor    = new Color(0.35f, 0.35f, 0.35f, 0.55f);
            c.fadeDuration = 0.07f;
            btn.colors = c;
            return btn;
        }

        void Pos(RectTransform rt, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos; rt.sizeDelta = size;
        }

        void AnchorTop(RectTransform rt, float h)
        {
            rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = Vector2.zero; rt.sizeDelta = new Vector2(0, h);
        }

        void AnchorBottom(RectTransform rt, float h)
        {
            rt.anchorMin = new Vector2(0, 0); rt.anchorMax = new Vector2(1, 0);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.anchoredPosition = Vector2.zero; rt.sizeDelta = new Vector2(0, h);
        }

        GameObject Overlay(Transform parent, string name)
        {
            var go  = new GameObject(name);
            var img = go.AddComponent<Image>();
            img.color = new Color(0, 0, 0, 0.76f);
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            return go;
        }

        Transform PanelBox(Transform parent, string name, Vector2 size, Color bg, Color border)
        {
            var go  = new GameObject(name);
            var img = go.AddComponent<Image>();
            img.color = bg;
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero; rt.sizeDelta = size;
            var ol = go.AddComponent<Outline>();
            ol.effectColor = border; ol.effectDistance = new Vector2(1, -1);
            return go.transform;
        }

        Transform PanelHdr(Transform panel, float h)
        {
            var go  = new GameObject("Hdr");
            var img = go.AddComponent<Image>();
            img.color = new Color(0.08f, 0.05f, 0.22f, 1f);
            img.raycastTarget = false;
            go.transform.SetParent(panel, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = Vector2.zero; rt.sizeDelta = new Vector2(0, h);
            return go.transform;
        }

        // ── Data helpers ──────────────────────────────────────────────────────

        string GetClassName(CharacterClass c) => c switch
        {
            CharacterClass.DarkKnight     => "Dark Knight",
            CharacterClass.DarkWizard     => "Dark Wizard",
            CharacterClass.FairyElf       => "Fairy Elf",
            CharacterClass.MagicGladiator => "Magic Gladiator",
            CharacterClass.DarkLord       => "Dark Lord",
            CharacterClass.Summoner       => "Summoner",
            CharacterClass.RageFighter    => "Rage Fighter",
            _                             => "Unknown"
        };

        string MapName(int id) => id switch
        {
            0 => "Lorencia", 1 => "Dungeon", 2 => "Devias",
            3 => "Noria", 4 => "Lost Tower", 7 => "Atlans",
            8 => "Tarkan", 10 => "Icarus", _ => $"Mapa {id}"
        };
    }
}
