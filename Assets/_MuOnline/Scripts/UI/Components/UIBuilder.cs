using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MuOnline.UI
{
    /// <summary>
    /// Utilidades para construir UI por código con el estilo MU Online.
    /// Evita configurar manualmente cada elemento en el Inspector.
    /// </summary>
    public static class UIBuilder
    {
        // ── Canvas raíz ─────────────────────────────────────────────────────

        public static Canvas CreateRootCanvas(string name = "Canvas")
        {
            var go = new GameObject(name);
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920); // móvil vertical
            scaler.matchWidthOrHeight  = 0.5f;

            go.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        // ── Panel con fondo ─────────────────────────────────────────────────

        public static RectTransform CreatePanel(Transform parent, string name,
            Vector2 anchorMin, Vector2 anchorMax,
            Vector2 offsetMin = default, Vector2 offsetMax = default,
            Color? color = null)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);

            var img = go.AddComponent<Image>();
            img.color = color ?? MuUITheme.PanelBackground;

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;
            return rt;
        }

        // ── Texto ────────────────────────────────────────────────────────────

        public static TextMeshProUGUI CreateText(Transform parent, string name,
            string text, float fontSize = MuUITheme.FontSizeNormal,
            Color? color = null, TextAlignmentOptions alignment = TextAlignmentOptions.Center)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);

            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text      = text;
            tmp.fontSize  = fontSize;
            tmp.color     = color ?? MuUITheme.TextPrimary;
            tmp.alignment = alignment;

            StretchFill(go.GetComponent<RectTransform>());
            return tmp;
        }

        // ── Input Field ──────────────────────────────────────────────────────

        public static TMP_InputField CreateInputField(Transform parent, string name,
            string placeholder = "", bool isPassword = false)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);

            var bg = go.AddComponent<Image>();
            bg.color = MuUITheme.InputBackground;

            var field = go.AddComponent<TMP_InputField>();
            field.contentType = isPassword
                ? TMP_InputField.ContentType.Password
                : TMP_InputField.ContentType.Standard;

            // Text area
            var textAreaGo = new GameObject("Text Area");
            textAreaGo.transform.SetParent(go.transform, false);
            var textAreaMask = textAreaGo.AddComponent<RectMask2D>();
            var textAreaRt   = textAreaGo.GetComponent<RectTransform>();
            textAreaRt.anchorMin = Vector2.zero;
            textAreaRt.anchorMax = Vector2.one;
            textAreaRt.offsetMin = new Vector2(10, 4);
            textAreaRt.offsetMax = new Vector2(-10, -4);

            // Texto activo
            var textGo = new GameObject("Text");
            textGo.transform.SetParent(textAreaGo.transform, false);
            var textTmp = textGo.AddComponent<TextMeshProUGUI>();
            textTmp.fontSize  = MuUITheme.FontSizeNormal;
            textTmp.color     = MuUITheme.TextPrimary;
            textTmp.alignment = TextAlignmentOptions.MidlineLeft;
            StretchFill(textGo.GetComponent<RectTransform>());

            // Placeholder
            var phGo = new GameObject("Placeholder");
            phGo.transform.SetParent(textAreaGo.transform, false);
            var phTmp = phGo.AddComponent<TextMeshProUGUI>();
            phTmp.text      = placeholder;
            phTmp.fontSize  = MuUITheme.FontSizeNormal;
            phTmp.color     = MuUITheme.TextSecondary;
            phTmp.fontStyle = FontStyles.Italic;
            phTmp.alignment = TextAlignmentOptions.MidlineLeft;
            StretchFill(phGo.GetComponent<RectTransform>());

            field.textViewport   = textAreaRt;
            field.textComponent  = textTmp;
            field.placeholder    = phTmp;

            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(400, 55);
            return field;
        }

        // ── Botón ────────────────────────────────────────────────────────────

        public static Button CreateButton(Transform parent, string name,
            string label, Color? bgColor = null)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);

            var img = go.AddComponent<Image>();
            img.color = bgColor ?? MuUITheme.ButtonNormal;

            var btn = go.AddComponent<Button>();
            var colors = btn.colors;
            colors.normalColor      = bgColor ?? MuUITheme.ButtonNormal;
            colors.highlightedColor = MuUITheme.ButtonHover;
            colors.pressedColor     = MuUITheme.ButtonPressed;
            colors.disabledColor    = new Color(0.3f, 0.3f, 0.3f, 0.5f);
            btn.colors = colors;

            // Borde dorado
            var outline = go.AddComponent<Outline>();
            outline.effectColor    = MuUITheme.GoldDark;
            outline.effectDistance = new Vector2(1, -1);

            // Label
            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(go.transform, false);
            var tmp = labelGo.AddComponent<TextMeshProUGUI>();
            tmp.text      = label;
            tmp.fontSize  = MuUITheme.FontSizeNormal;
            tmp.color     = MuUITheme.GoldPrimary;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontStyle = FontStyles.Bold;
            StretchFill(labelGo.GetComponent<RectTransform>());

            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(300, 55);
            return btn;
        }

        // ── Separador horizontal ─────────────────────────────────────────────

        public static GameObject CreateSeparator(Transform parent, float width = 400f)
        {
            var go = new GameObject("Separator");
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.color = MuUITheme.GoldDark;
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(width, 1f);
            return go;
        }

        // ── Spinner de carga ─────────────────────────────────────────────────

        public static GameObject CreateSpinner(Transform parent)
        {
            var go = new GameObject("Spinner");
            go.transform.SetParent(parent, false);

            var img = go.AddComponent<Image>();
            img.color = MuUITheme.GoldPrimary;

            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(40, 40);

            go.AddComponent<SpinnerRotator>();
            return go;
        }

        // ── Helpers ──────────────────────────────────────────────────────────

        public static void StretchFill(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        public static void SetAnchored(RectTransform rt,
            Vector2 anchoredPos, Vector2 size)
        {
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta        = size;
        }
    }

    /// <summary>Rota el spinner de carga.</summary>
    public class SpinnerRotator : MonoBehaviour
    {
        [SerializeField] private float speed = 300f;
        void Update() => transform.Rotate(0, 0, -speed * Time.deltaTime);
    }
}
