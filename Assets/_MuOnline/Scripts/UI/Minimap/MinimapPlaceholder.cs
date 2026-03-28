using TMPro;
using UnityEngine;
using UnityEngine.UI;
using MuOnline.UI;

namespace MuOnline.UI.Minimap
{
    /// <summary>Marcador de posición para minimapa (textura / render texture más adelante).</summary>
    public class MinimapPlaceholder : MonoBehaviour
    {
        void Awake()
        {
            if (transform.childCount > 0) return;

            var rt = GetComponent<RectTransform>();
            if (rt == null) return;

            var bg = gameObject.AddComponent<Image>();
            bg.color = new Color(0.04f, 0.03f, 0.08f, 0.75f);
            gameObject.AddComponent<Outline>().effectColor = MuUITheme.PanelBorderGold;

            var tgo = new GameObject("Label");
            tgo.transform.SetParent(transform, false);
            var tr = tgo.AddComponent<RectTransform>();
            UIBuilder.StretchFill(tr);
            var tmp = tgo.AddComponent<TextMeshProUGUI>();
            tmp.text = "MINIMAP";
            tmp.fontSize = 11;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = MuUITheme.TextSecondary;
        }
    }
}
