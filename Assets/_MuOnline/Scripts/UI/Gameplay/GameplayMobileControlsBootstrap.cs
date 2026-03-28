using UnityEngine;
using UnityEngine.UI;
using MuOnline.Gameplay.Input;

namespace MuOnline.UI.Gameplay
{
    /// <summary>Crea canvas móvil (1920×1080 ref) con joystick virtual.</summary>
    public static class GameplayMobileControlsBootstrap
    {
        public static VirtualJoystick Build(Transform parent = null)
        {
            var go = new GameObject("MobileControlsCanvas");
            if (parent != null) go.transform.SetParent(parent, false);
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 30;
            var sc = go.AddComponent<CanvasScaler>();
            sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            sc.referenceResolution = new Vector2(1920, 1080);
            sc.matchWidthOrHeight = 0.5f;
            go.AddComponent<GraphicRaycaster>();

            var joyGo = new GameObject("VirtualJoystick");
            joyGo.transform.SetParent(go.transform, false);
            var jrt = joyGo.AddComponent<RectTransform>();
            jrt.anchorMin = Vector2.zero;
            jrt.anchorMax = Vector2.zero;
            jrt.pivot = Vector2.zero;
            jrt.anchoredPosition = new Vector2(140f, 140f);
            jrt.sizeDelta = new Vector2(220f, 220f);

            var jBg = joyGo.AddComponent<Image>();
            jBg.color = new Color(0, 0, 0, 0.35f);
            jBg.raycastTarget = true;

            var stick = new GameObject("Handle");
            stick.transform.SetParent(joyGo.transform, false);
            var hrt = stick.AddComponent<RectTransform>();
            hrt.sizeDelta = new Vector2(96f, 96f);
            var hImg = stick.AddComponent<Image>();
            hImg.color = new Color(0.85f, 0.68f, 0.2f, 0.55f);
            hImg.raycastTarget = false;

            var vj = joyGo.AddComponent<VirtualJoystick>();
            vj.AssignHandle(hrt, 72f);
            return vj;
        }
    }
}
