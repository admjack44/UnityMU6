using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using MuOnline.Network;
using MuOnline.UI;

namespace MuOnline.Core
{
    [DefaultExecutionOrder(-100)]
    public class BootLoader : MonoBehaviour
    {
        [Header("Prefabs de Managers")]
        [SerializeField] private GameObject gameManagerPrefab;
        [SerializeField] private GameObject networkClientPrefab;
        [SerializeField] private GameObject packetHandlerPrefab;
        [SerializeField] private GameObject sceneTransitionPrefab;

        private TextMeshProUGUI _statusText;
        private RectTransform   _barFill;
        private Image           _barFillImg;

        void Awake()
        {
            Application.targetFrameRate = 60;
            QualitySettings.vSyncCount  = 0;
            BuildLoadingUI();
        }

        void Start() => StartCoroutine(InitSequence());

        private void BuildLoadingUI()
        {
            var canvasGo = new GameObject("BootCanvas");
            var canvas   = canvasGo.AddComponent<Canvas>();
            canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.screenMatchMode     = CanvasScaler.ScreenMatchMode.Expand;
            canvasGo.AddComponent<GraphicRaycaster>();

            var root = canvasGo.transform;

            // ── Fondo: negro base ─────────────────────────────────────────────
            var bgImg = MakeImg("BG", root, stretch: true);
            bgImg.color = new Color(0.02f, 0.01f, 0.05f, 1f);

            // Fondo original del intro screen de MU (Logo_out/Loading01-03.jpg)
            // Tiene prioridad sobre el LSBg de Interface_out
            var loadingBg = MuAssetLoader.Get(MuAssetLoader.LogoScreen.RandomLoadingBg());
            if (loadingBg == null) loadingBg = MuAssetLoader.Get(MuAssetLoader.Login.LoadBg2);
            if (loadingBg != null)
            {
                var lsImg = MakeImg("LSBg", root, stretch: true);
                lsImg.sprite         = loadingBg;
                lsImg.color          = Color.white;
                lsImg.preserveAspect = false;
            }

            // Overlay oscuro encima del fondo
            var overlay = MakeImg("Overlay", root, stretch: true);
            overlay.color = new Color(0f, 0f, 0f, 0.48f);

            // ── Logo MU oficial (MU-logo.tga de Logo_out) ────────────────────
            // Orden de prioridad: MU-logo_g.jpg (glow), MU-logo.tga, MU_TITLE.tga, texto
            var titleSprite =
                MuAssetLoader.Get(MuAssetLoader.LogoScreen.MuLogoGold)
                ?? MuAssetLoader.Get(MuAssetLoader.LogoScreen.MuLogoTga)
                ?? MuAssetLoader.Get(MuAssetLoader.Login.MuTitle);

            if (titleSprite != null)
            {
                var logoImg = MakeImg("MuLogo", root);
                logoImg.sprite         = titleSprite;
                logoImg.color          = Color.white;
                logoImg.preserveAspect = true;
                var lr = logoImg.GetComponent<RectTransform>();
                lr.anchorMin        = lr.anchorMax = new Vector2(0.5f, 0.5f);
                lr.pivot            = new Vector2(0.5f, 0.5f);
                lr.anchoredPosition = new Vector2(0, 110);
                lr.sizeDelta        = new Vector2(420, 140);
            }
            else
            {
                var logoTxt = MakeTxt("LogoFallback", root,
                    new Vector2(0, 110), new Vector2(600, 90), 68f,
                    new Color(1f, 0.82f, 0.18f, 1f), TextAlignmentOptions.Center);
                logoTxt.text      = "MU PEGASO";
                logoTxt.fontStyle = FontStyles.Bold;
                logoTxt.gameObject.AddComponent<Outline>().effectColor =
                    new Color(0.55f, 0.25f, 0.95f, 0.8f);
            }

            // Subtítulo PEGASO
            var subTxt = MakeTxt("ServerName", root,
                new Vector2(0, 35), new Vector2(400, 30), 16f,
                new Color(0.72f, 0.50f, 1.00f, 1f), TextAlignmentOptions.Center);
            subTxt.text      = "PEGASO  -  PRIVATE SERVER";
            subTxt.fontStyle = FontStyles.Italic;

            // ── Barra de progreso con assets originales ───────────────────────
            BuildProgressBar(root);

            // ── Texto de estado ───────────────────────────────────────────────
            _statusText = MakeTxt("Status", root,
                new Vector2(0, -125), new Vector2(540, 28), 13f,
                new Color(0.75f, 0.73f, 0.68f, 1f), TextAlignmentOptions.Center);
            _statusText.text = "Iniciando...";

            // ── Versión ───────────────────────────────────────────────────────
            var vTxt = MakeTxt("Version", root,
                new Vector2(0, 18), new Vector2(0, 24), 11f,
                new Color(0.25f, 0.20f, 0.35f, 1f), TextAlignmentOptions.Center,
                anchorMin: new Vector2(0f, 0f), anchorMax: new Vector2(1f, 0f),
                pivot: new Vector2(0.5f, 0f));
            vTxt.text = "v0.1.0 Alpha  |  Unity 6  |  MU Season 6 Assets";
        }

        private void BuildProgressBar(Transform root)
        {
            // Contenedor centrado
            var containerGo = new GameObject("ProgressContainer");
            var containerRt = containerGo.AddComponent<RectTransform>();
            containerGo.transform.SetParent(root, false);
            containerRt.anchorMin        = containerRt.anchorMax = new Vector2(0.5f, 0.5f);
            containerRt.pivot            = new Vector2(0.5f, 0.5f);
            containerRt.anchoredPosition = new Vector2(0, -85);
            containerRt.sizeDelta        = new Vector2(580, 28);

            // Fondo barra (Progress_Back.jpg)
            var bgBarImg = containerGo.AddComponent<Image>();
            var progBack = MuAssetLoader.Get(MuAssetLoader.HUD.ProgBack);
            if (progBack != null)
            {
                bgBarImg.sprite = progBack;
                bgBarImg.type   = Image.Type.Sliced;
                bgBarImg.color  = Color.white;
            }
            else
            {
                bgBarImg.color = new Color(0.08f, 0.04f, 0.18f, 1f);
                containerGo.AddComponent<Outline>().effectColor =
                    new Color(0.35f, 0.20f, 0.60f, 1f);
            }

            // Relleno barra (Progress.jpg)
            var fillGo  = new GameObject("BarFill");
            _barFill    = fillGo.AddComponent<RectTransform>();
            _barFillImg = fillGo.AddComponent<Image>();
            fillGo.transform.SetParent(containerGo.transform, false);

            var progFill = MuAssetLoader.Get(MuAssetLoader.HUD.Progress);
            if (progFill != null)
            {
                _barFillImg.sprite = progFill;
                _barFillImg.type   = Image.Type.Filled;
                _barFillImg.fillMethod    = Image.FillMethod.Horizontal;
                _barFillImg.fillOrigin    = (int)Image.OriginHorizontal.Left;
                _barFillImg.fillAmount    = 0f;
                _barFillImg.color         = Color.white;
                UIBuilder.StretchFill(_barFill);
            }
            else
            {
                // Fallback morado
                _barFillImg.color = new Color(0.55f, 0.25f, 0.95f, 1f);
                _barFill.anchorMin = Vector2.zero;
                _barFill.anchorMax = new Vector2(0f, 1f);
                _barFill.offsetMin = _barFill.offsetMax = Vector2.zero;
                _barFill.pivot     = new Vector2(0f, 0.5f);
            }
        }

        // ── Helpers ──────────────────────────────────────────────────────────

        private Image MakeImg(string name, Transform parent, bool stretch = false)
        {
            var go  = new GameObject(name);
            var rt  = go.AddComponent<RectTransform>();
            var img = go.AddComponent<Image>();
            go.transform.SetParent(parent, false);
            if (stretch)
            {
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = rt.offsetMax = Vector2.zero;
            }
            return img;
        }

        private TextMeshProUGUI MakeTxt(string name, Transform parent,
            Vector2 anchoredPos, Vector2 size,
            float fontSize, Color color, TextAlignmentOptions align,
            Vector2? anchorMin = null, Vector2? anchorMax = null, Vector2? pivot = null)
        {
            var go  = new GameObject(name);
            var rt  = go.AddComponent<RectTransform>();
            var tmp = go.AddComponent<TextMeshProUGUI>();
            go.transform.SetParent(parent, false);
            rt.anchorMin        = anchorMin ?? new Vector2(0.5f, 0.5f);
            rt.anchorMax        = anchorMax ?? new Vector2(0.5f, 0.5f);
            rt.pivot            = pivot     ?? new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta        = size;
            tmp.fontSize  = fontSize;
            tmp.color     = color;
            tmp.alignment = align;
            return tmp;
        }

        // ── Secuencia de inicialización ──────────────────────────────────────

        private IEnumerator InitSequence()
        {
            SetProgress(0f, "Iniciando managers...");
            yield return new WaitForSeconds(0.25f);

            if (gameManagerPrefab   != null) Instantiate(gameManagerPrefab);
            SetProgress(0.2f, "Game Manager listo.");
            yield return new WaitForSeconds(0.15f);

            if (networkClientPrefab != null) Instantiate(networkClientPrefab);
            SetProgress(0.4f, "Network Client listo.");
            yield return new WaitForSeconds(0.15f);

            if (packetHandlerPrefab != null) Instantiate(packetHandlerPrefab);
            SetProgress(0.6f, "Packet Handler listo.");
            yield return new WaitForSeconds(0.15f);

            if (sceneTransitionPrefab != null)
                Instantiate(sceneTransitionPrefab);
            else
            {
                var stmGo = new GameObject("SceneTransitionManager");
                stmGo.AddComponent<SceneTransitionManager>();
                DontDestroyOnLoad(stmGo);
            }

            SetProgress(0.8f, "Conectando al servidor...");
            yield return new WaitForSeconds(0.25f);

            if (GameManager.Instance != null)
            {
                GameManager.Instance.ConnectToServer();
                float waited = 0f;
                const float maxWait = 8f;
                while (waited < maxWait &&
                       NetworkClient.Instance != null &&
                       !NetworkClient.Instance.IsConnected)
                {
                    waited += Time.deltaTime;
                    yield return null;
                }

                if (NetworkClient.Instance != null && NetworkClient.Instance.IsConnected)
                    SetProgress(1f, "Conectado al servidor.");
                else
                    SetProgress(1f, "Sin conexion. Podras reintentar en Login.");
            }
            else
                SetProgress(1f, "Sin GameManager.");

            yield return new WaitForSeconds(0.4f);
            if (SceneTransitionManager.Instance != null)
                SceneTransitionManager.Instance.LoadScene("Login");
            else
                UnityEngine.SceneManagement.SceneManager.LoadScene("Login");
        }

        private void SetProgress(float value, string status)
        {
            if (_barFillImg != null && _barFillImg.type == Image.Type.Filled)
            {
                _barFillImg.fillAmount = value;
            }
            else if (_barFill != null)
            {
                _barFill.anchorMax = new Vector2(value, 1f);
            }

            if (_statusText != null)
                _statusText.text = status;
        }
    }
}
