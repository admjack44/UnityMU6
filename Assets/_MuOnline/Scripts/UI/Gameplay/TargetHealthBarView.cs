using TMPro;
using UnityEngine;
using UnityEngine.UI;
using MuOnline.Core;
using MuOnline.Gameplay.Combat;
using MuOnline.UI;

namespace MuOnline.UI.Gameplay
{
    /// <summary>Barra de vida del objetivo seleccionado (estilo MU móvil).</summary>
    public class TargetHealthBarView : MonoBehaviour
    {
        [SerializeField] private CanvasGroup group;
        [SerializeField] private Image fill;
        [SerializeField] private TextMeshProUGUI label;
        private Damageable _tracked;

        public void Wire(CanvasGroup cg, Image fillImage, TextMeshProUGUI lbl)
        {
            group = cg;
            fill = fillImage;
            label = lbl;
        }

        void OnEnable()
        {
            EventBus.Subscribe<LocalGameplayEvents.TargetChanged>(OnTarget);
        }

        void OnDisable()
        {
            EventBus.Unsubscribe<LocalGameplayEvents.TargetChanged>(OnTarget);
        }

        void OnTarget(LocalGameplayEvents.TargetChanged e)
        {
            _tracked = e.TargetTransform != null
                ? e.TargetTransform.GetComponent<Damageable>()
                : null;
            if (group != null) group.alpha = _tracked != null ? 1f : 0f;
            Refresh();
        }

        void LateUpdate()
        {
            if (_tracked == null) return;
            Refresh();
        }

        void Refresh()
        {
            if (_tracked == null || fill == null)
            {
                if (group != null) group.alpha = 0f;
                return;
            }

            if (_tracked.IsDead)
            {
                if (group != null) group.alpha = 0f;
                return;
            }

            if (group != null) group.alpha = 1f;
            float t = _tracked.MaxHp > 0 ? (float)_tracked.CurrentHp / _tracked.MaxHp : 0f;
            fill.fillAmount = t;
            if (label != null)
                label.text = $"{_tracked.CurrentHp} / {_tracked.MaxHp}";
        }

        /// <summary>Crea layout por código si no hay prefab.</summary>
        public static TargetHealthBarView CreateUnder(Transform parent)
        {
            var root = new GameObject("TargetHealthBar");
            root.transform.SetParent(parent, false);
            var rt = root.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0, -12f);
            rt.sizeDelta = new Vector2(420f, 36f);

            var cg = root.AddComponent<CanvasGroup>();
            cg.alpha = 0f;

            var bg = new GameObject("BG");
            bg.transform.SetParent(root.transform, false);
            var bgr = bg.AddComponent<RectTransform>();
            UIBuilder.StretchFill(bgr);
            var img = bg.AddComponent<Image>();
            img.color = MuUITheme.PanelBackground;

            var barGo = new GameObject("Bar");
            barGo.transform.SetParent(root.transform, false);
            var srt = barGo.AddComponent<RectTransform>();
            UIBuilder.StretchFill(srt);
            srt.offsetMin = new Vector2(8, 8);
            srt.offsetMax = new Vector2(-8, -18);

            var fillGo = new GameObject("Fill");
            fillGo.transform.SetParent(barGo.transform, false);
            var fr = fillGo.AddComponent<RectTransform>();
            fr.anchorMin = Vector2.zero;
            fr.anchorMax = Vector2.one;
            fr.offsetMin = fr.offsetMax = Vector2.zero;
            var fi = fillGo.AddComponent<Image>();
            fi.color = MuUITheme.HpColor;
            fi.type = Image.Type.Filled;
            fi.fillMethod = Image.FillMethod.Horizontal;
            fi.fillAmount = 1f;

            var lblGo = new GameObject("Label");
            lblGo.transform.SetParent(root.transform, false);
            var lr = lblGo.AddComponent<RectTransform>();
            lr.anchorMin = new Vector2(0, 0);
            lr.anchorMax = new Vector2(1, 0);
            lr.pivot = new Vector2(0.5f, 0f);
            lr.anchoredPosition = Vector2.zero;
            lr.sizeDelta = new Vector2(0, 16f);
            var tmp = lblGo.AddComponent<TextMeshProUGUI>();
            tmp.fontSize = 12;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = MuUITheme.TextPrimary;

            var v = root.AddComponent<TargetHealthBarView>();
            v.Wire(cg, fi, tmp);
            return v;
        }
    }
}
