using System.Collections;
using UnityEngine;

namespace MuOnline.UI.Login
{
    /// <summary>
    /// Intro del login: fade del fondo / contenido y escala tipo "pop" del panel central.
    /// </summary>
    public class LoginAnimator : MonoBehaviour
    {
        [Header("Fade")]
        [SerializeField] private CanvasGroup[] fadeGroups;
        [SerializeField] private float fadeDuration = 0.85f;
        [SerializeField] private AnimationCurve fadeCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Header("Panel pop")]
        [SerializeField] private RectTransform loginPanel;
        [SerializeField] private CanvasGroup loginPanelGroup;
        [SerializeField] private float panelDuration = 0.55f;
        [SerializeField] private float startScale = 0.9f;
        [SerializeField] private AnimationCurve panelScaleCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Header("Play")]
        [SerializeField] private bool playOnAwake = true;
        [SerializeField] private float startDelay;

        Coroutine _running;

        void Awake()
        {
            PrimeStartState();
            if (playOnAwake)
                PlayIntro();
        }

        /// <summary>Estado inicial antes de animar (invisible / pequeño).</summary>
        public void PrimeStartState()
        {
            if (fadeGroups != null)
            {
                foreach (var g in fadeGroups)
                {
                    if (g == null) continue;
                    g.alpha = 0f;
                }
            }

            if (loginPanel != null)
                loginPanel.localScale = Vector3.one * startScale;

            if (loginPanelGroup != null)
                loginPanelGroup.alpha = 0f;
        }

        public void PlayIntro()
        {
            if (_running != null)
                StopCoroutine(_running);
            _running = StartCoroutine(IntroRoutine());
        }

        IEnumerator IntroRoutine()
        {
            if (startDelay > 0f)
                yield return new WaitForSecondsRealtime(startDelay);

            float t = 0f;
            while (t < 1f)
            {
                t += Time.unscaledDeltaTime / Mathf.Max(0.01f, fadeDuration);
                float e = fadeCurve.Evaluate(Mathf.Clamp01(t));
                if (fadeGroups != null)
                {
                    foreach (var g in fadeGroups)
                    {
                        if (g != null) g.alpha = e;
                    }
                }

                yield return null;
            }

            if (fadeGroups != null)
            {
                foreach (var g in fadeGroups)
                {
                    if (g != null) g.alpha = 1f;
                }
            }

            t = 0f;
            while (t < 1f)
            {
                t += Time.unscaledDeltaTime / Mathf.Max(0.01f, panelDuration);
                float e = panelScaleCurve.Evaluate(Mathf.Clamp01(t));
                float s = Mathf.Lerp(startScale, 1f, e);
                if (loginPanel != null)
                    loginPanel.localScale = Vector3.one * s;
                if (loginPanelGroup != null)
                    loginPanelGroup.alpha = e;

                yield return null;
            }

            if (loginPanel != null)
                loginPanel.localScale = Vector3.one;
            if (loginPanelGroup != null)
                loginPanelGroup.alpha = 1f;

            _running = null;
        }
    }
}
