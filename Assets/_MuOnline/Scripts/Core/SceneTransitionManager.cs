using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MuOnline.Core
{
    public class SceneTransitionManager : MonoBehaviour
    {
        public static SceneTransitionManager Instance { get; private set; }

        [SerializeField] private float fadeDuration = 0.4f;

        private Canvas _canvas;
        private Image  _overlay;
        private bool   _transitioning;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            BuildOverlay();
        }

        private void BuildOverlay()
        {
            _canvas = gameObject.AddComponent<Canvas>();
            _canvas.renderMode  = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 9999;

            gameObject.AddComponent<CanvasScaler>();
            gameObject.AddComponent<GraphicRaycaster>();

            var go = new GameObject("FadeOverlay");
            go.transform.SetParent(transform, false);

            _overlay = go.AddComponent<Image>();
            _overlay.color = Color.black;
            _overlay.raycastTarget = true;

            var rt = _overlay.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;

            SetAlpha(0f);
        }

        public void LoadScene(string sceneName, Action onMidpoint = null)
        {
            if (_transitioning) return;
            StartCoroutine(TransitionRoutine(sceneName, onMidpoint));
        }

        private IEnumerator TransitionRoutine(string sceneName, Action onMidpoint)
        {
            _transitioning = true;
            _overlay.raycastTarget = true;

            yield return FadeTo(1f);

            onMidpoint?.Invoke();
            yield return SceneManager.LoadSceneAsync(sceneName);

            yield return FadeTo(0f);
            _overlay.raycastTarget = false;
            _transitioning = false;
        }

        private IEnumerator FadeTo(float target)
        {
            float start   = _overlay.color.a;
            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                SetAlpha(Mathf.Lerp(start, target, elapsed / fadeDuration));
                yield return null;
            }
            SetAlpha(target);
        }

        private void SetAlpha(float a)
        {
            var c = _overlay.color;
            c.a = a;
            _overlay.color = c;
        }
    }
}
