using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

namespace MuOnline.UI.Login
{
    /// <summary>
    /// Fondo cinemático tipo MMORPG: estático, parallax 3 capas (idle + ratón) o vídeo (RT + RawImage)
    /// con blur UI opcional, viñeta, color grading y bloom falso (sin post stack pesado).
    /// </summary>
    public class BackgroundController : MonoBehaviour
    {
        public enum BackgroundMode
        {
            Static,
            Parallax,
            Video
        }

        const string BlurShaderName = "Mu/UI/LoginBlur";

        [Header("Mode")]
        [SerializeField] private BackgroundMode mode = BackgroundMode.Static;

        [Header("Roots (enable one per mode)")]
        [SerializeField] private GameObject staticRoot;
        [SerializeField] private GameObject parallaxRoot;
        [SerializeField] private GameObject videoRoot;

        [Header("Screen overlay (readability)")]
        [SerializeField] private Image darkOverlay;
        [SerializeField, Range(0.35f, 0.65f)] private float overlayAlpha = 0.48f;

        [Header("Static — gradient")]
        [SerializeField] private RawImage staticGradientImage;
        [SerializeField] private Color staticColorTop = new Color(0.12f, 0.04f, 0.22f, 1f);
        [SerializeField] private Color staticColorMid = new Color(0.05f, 0.08f, 0.28f, 1f);
        [SerializeField] private Color staticColorBottom = new Color(0.02f, 0.03f, 0.12f, 1f);
        [Tooltip("Capa suave adicional (profundidad).")]
        [SerializeField] private RawImage staticSoftLayer;
        [SerializeField] private float staticFloatAmplitude = 5f;
        [SerializeField] private float staticFloatSpeed = 0.3f;

        [Header("Parallax — cinematic 3 layers")]
        [SerializeField] private bool useThreeLayerParallax = true;
        [SerializeField] private ParallaxLayerConfig parallaxFar;
        [SerializeField] private ParallaxLayerConfig parallaxMid;
        [SerializeField] private ParallaxLayerConfig parallaxNear;
        [Tooltip("Parallax según posición del ratón (normalizado).")]
        [SerializeField] private bool mouseParallaxEnabled = true;
        [SerializeField, Range(0.5f, 3f)] private float mouseParallaxSmoothing = 8f;

        [Header("Parallax — legacy list (if three-layer off)")]
        [SerializeField] private List<ParallaxLayer> parallaxLayers = new();

        [Header("Video")]
        [SerializeField] private VideoPlayer videoPlayer;
        [SerializeField] private RawImage videoRawImage;
        [SerializeField] private VideoClip videoClip;
        [SerializeField] private RenderTexture videoRenderTexture;
        [SerializeField] private bool createRenderTextureIfNull = true;
        [SerializeField] private Vector2Int videoTargetSize = new Vector2Int(1920, 1080);
        [Header("Video presentation")]
        [SerializeField] private bool autoArrangeVideoSiblings = true;
        [Tooltip("RawImage detrás del vídeo principal, mismo RT, material blur.")]
        [SerializeField] private bool useVideoBlurLayer = true;
        [SerializeField] private RawImage videoBlurRawImage;
        [SerializeField, Range(0.3f, 2.5f)] private float videoBlurStrength = 1.1f;
        [SerializeField] private bool useVideoVignette = true;
        [SerializeField] private RawImage videoVignetteRawImage;
        [SerializeField, Range(0.25f, 0.75f)] private float videoVignetteIntensity = 0.52f;

        [Header("Cinematic post (subtle, all modes)")]
        [SerializeField] private bool enableCinematicPost = true;
        [Tooltip("Si null, se crea bajo CinematicPost.")]
        [SerializeField] private Transform cinematicFxRoot;
        [SerializeField] private Image colorGradeOverlay;
        [SerializeField] private Color colorGradeTint = new Color(0.38f, 0.32f, 0.58f, 1f);
        [SerializeField, Range(0.02f, 0.22f)] private float colorGradeStrength = 0.1f;
        [SerializeField, Range(0f, 0.03f)] private float colorGradeBreath = 0.012f;
        [SerializeField] private RawImage bloomOrbTopLeft;
        [SerializeField] private RawImage bloomOrbBottomRight;
        [SerializeField] private Color bloomTint = new Color(1f, 0.82f, 0.45f, 0.11f);
        [SerializeField] private float bloomPulseSpeed = 0.35f;
        [SerializeField, Range(0f, 0.06f)] private float bloomPulseAmount = 0.025f;

        Vector2[] _parallaxBases;
        Vector2 _staticGradBase;
        Vector2[] _parallax3Bases = new Vector2[3];
        bool _staticBasesCached;
        Vector2 _mouseParallaxVel;
        Color _colorGradeBaseAlpha;
        Material _blurMaterialInstance;

        [System.Serializable]
        public class ParallaxLayerConfig
        {
            public RectTransform Rect;
            [Tooltip("Oscilación idle (píxeles ref).")]
            public float IdleAmplitude = 12f;
            public Vector2 IdleSpeed = new Vector2(0.16f, 0.13f);
            [Tooltip("Desplazamiento máx. con ratón en borde de pantalla (píxeles).")]
            public float MouseParallaxPixels = 18f;
            public float PhaseOffset;
        }

        [System.Serializable]
        public class ParallaxLayer
        {
            public RectTransform Rect;
            public Vector2 Speed = new Vector2(14f, 10f);
            public float Amplitude = 18f;
        }

        void Awake()
        {
            ApplyOverlayAlpha();
            ApplyModeRoots();
            switch (mode)
            {
                case BackgroundMode.Static:
                    SetupStatic();
                    break;
                case BackgroundMode.Parallax:
                    SetupParallax();
                    break;
                case BackgroundMode.Video:
                    SetupVideo();
                    break;
            }

            if (enableCinematicPost)
                EnsureCinematicPostStack();
        }

        void OnValidate()
        {
            if (!isActiveAndEnabled) return;
            ApplyOverlayAlpha();
            ApplyModeRoots();
            if (_blurMaterialInstance != null)
                _blurMaterialInstance.SetFloat("_Blur", videoBlurStrength);
        }

        void OnDestroy()
        {
            if (_blurMaterialInstance != null)
                Destroy(_blurMaterialInstance);
        }

        public void SetMode(BackgroundMode newMode)
        {
            mode = newMode;
            ApplyModeRoots();
            switch (mode)
            {
                case BackgroundMode.Static:
                    SetupStatic();
                    break;
                case BackgroundMode.Parallax:
                    SetupParallax();
                    break;
                case BackgroundMode.Video:
                    SetupVideo();
                    break;
            }
        }

        void ApplyOverlayAlpha()
        {
            if (darkOverlay == null) return;
            var c = darkOverlay.color;
            c.a = overlayAlpha;
            darkOverlay.color = c;
            darkOverlay.raycastTarget = false;
        }

        void ApplyModeRoots()
        {
            if (staticRoot != null) staticRoot.SetActive(mode == BackgroundMode.Static);
            if (parallaxRoot != null) parallaxRoot.SetActive(mode == BackgroundMode.Parallax);
            if (videoRoot != null) videoRoot.SetActive(mode == BackgroundMode.Video);
        }

        void SetupStatic()
        {
            if (staticGradientImage != null)
            {
                var tex = BuildVerticalGradient(4, 512, staticColorTop, staticColorMid, staticColorBottom);
                staticGradientImage.texture = tex;
                staticGradientImage.uvRect = new Rect(0f, 0f, 1f, 1f);
            }

            if (staticSoftLayer != null)
            {
                var soft = BuildVignetteSoft(256, 256, new Color(0f, 0f, 0f, 0.2f));
                staticSoftLayer.texture = soft;
            }

            CacheStaticBases();
        }

        void CacheStaticBases()
        {
            if (staticGradientImage != null)
                _staticGradBase = staticGradientImage.rectTransform.anchoredPosition;
            _staticBasesCached = true;
        }

        void SetupParallax()
        {
            if (useThreeLayerParallax)
            {
                _parallax3Bases[0] = parallaxFar.Rect != null ? parallaxFar.Rect.anchoredPosition : Vector2.zero;
                _parallax3Bases[1] = parallaxMid.Rect != null ? parallaxMid.Rect.anchoredPosition : Vector2.zero;
                _parallax3Bases[2] = parallaxNear.Rect != null ? parallaxNear.Rect.anchoredPosition : Vector2.zero;
            }
            else
            {
                _parallaxBases = new Vector2[parallaxLayers.Count];
                for (int i = 0; i < parallaxLayers.Count; i++)
                {
                    var l = parallaxLayers[i];
                    if (l.Rect != null)
                        _parallaxBases[i] = l.Rect.anchoredPosition;
                }
            }
        }

        void SetupVideo()
        {
            if (videoPlayer == null || videoRawImage == null)
            {
                Debug.LogWarning("[BackgroundController] Video mode needs VideoPlayer + RawImage.");
                return;
            }

            if (videoRenderTexture == null && createRenderTextureIfNull)
            {
                videoRenderTexture = new RenderTexture(
                    Mathf.Max(64, videoTargetSize.x),
                    Mathf.Max(64, videoTargetSize.y),
                    0,
                    RenderTextureFormat.ARGB32)
                {
                    filterMode = FilterMode.Bilinear
                };
                videoRenderTexture.Create();
            }

            if (videoRenderTexture != null)
            {
                videoPlayer.targetTexture = videoRenderTexture;
                videoRawImage.texture = videoRenderTexture;
            }

            videoPlayer.renderMode = VideoRenderMode.RenderTexture;
            videoPlayer.playOnAwake = true;
            videoPlayer.isLooping = true;
            videoPlayer.waitForFirstFrame = true;

            if (videoClip != null)
                videoPlayer.clip = videoClip;

            videoPlayer.Play();

            if (autoArrangeVideoSiblings || useVideoBlurLayer || useVideoVignette)
                EnsureVideoPresentationLayers();
        }

        void EnsureVideoPresentationLayers()
        {
            if (videoRoot == null || videoRawImage == null) return;
            var rt = videoRenderTexture != null ? videoRenderTexture : videoRawImage.texture as RenderTexture;
            if (rt == null) return;

            Transform parent = videoRawImage.transform.parent;
            if (parent == null) return;

            if (useVideoBlurLayer && videoBlurRawImage == null)
            {
                var go = new GameObject("VideoBlurLayer", typeof(RectTransform), typeof(RawImage));
                go.transform.SetParent(parent, false);
                Stretch(go.GetComponent<RectTransform>());
                var raw = go.GetComponent<RawImage>();
                raw.texture = rt;
                raw.raycastTarget = false;
                var sh = Shader.Find(BlurShaderName);
                if (sh != null)
                {
                    if (_blurMaterialInstance != null)
                        Destroy(_blurMaterialInstance);
                    _blurMaterialInstance = new Material(sh);
                    _blurMaterialInstance.SetFloat("_Blur", videoBlurStrength);
                    raw.material = _blurMaterialInstance;
                }

                videoBlurRawImage = raw;
            }
            else if (videoBlurRawImage != null && _blurMaterialInstance == null)
            {
                var sh = Shader.Find(BlurShaderName);
                if (sh != null && videoBlurRawImage.material == null)
                {
                    _blurMaterialInstance = new Material(sh);
                    _blurMaterialInstance.SetFloat("_Blur", videoBlurStrength);
                    videoBlurRawImage.material = _blurMaterialInstance;
                }

                videoBlurRawImage.texture = rt;
            }

            if (_blurMaterialInstance != null)
                _blurMaterialInstance.SetFloat("_Blur", videoBlurStrength);

            if (useVideoVignette && videoVignetteRawImage == null)
            {
                var go = new GameObject("VideoVignette", typeof(RectTransform), typeof(RawImage));
                go.transform.SetParent(parent, false);
                Stretch(go.GetComponent<RectTransform>());
                var raw = go.GetComponent<RawImage>();
                raw.texture = BuildCinematicVignette(512, 512, videoVignetteIntensity);
                raw.color = Color.white;
                raw.raycastTarget = false;
                videoVignetteRawImage = raw;
            }
            else if (videoVignetteRawImage != null && videoVignetteRawImage.texture == null)
            {
                videoVignetteRawImage.texture = BuildCinematicVignette(512, 512, videoVignetteIntensity);
            }

            if (autoArrangeVideoSiblings)
            {
                if (videoBlurRawImage != null)
                    videoBlurRawImage.transform.SetSiblingIndex(0);
                videoRawImage.transform.SetSiblingIndex(videoBlurRawImage != null ? 1 : 0);
                if (videoVignetteRawImage != null)
                    videoVignetteRawImage.transform.SetAsLastSibling();
            }
        }

        void EnsureCinematicPostStack()
        {
            Transform root = cinematicFxRoot;
            if (root == null)
            {
                var existing = transform.Find("CinematicPost");
                if (existing == null)
                {
                    var go = new GameObject("CinematicPost", typeof(RectTransform));
                    go.transform.SetParent(transform, false);
                    Stretch(go.GetComponent<RectTransform>());
                    existing = go.transform;
                }

                root = existing;
                cinematicFxRoot = root;
            }

            var rootRt = root.GetComponent<RectTransform>();
            if (rootRt == null) rootRt = root.gameObject.AddComponent<RectTransform>();
            Stretch(rootRt);

            if (colorGradeOverlay == null)
            {
                var go = new GameObject("ColorGrade", typeof(RectTransform), typeof(Image));
                go.transform.SetParent(root, false);
                Stretch(go.GetComponent<RectTransform>());
                colorGradeOverlay = go.GetComponent<Image>();
                colorGradeOverlay.raycastTarget = false;
                colorGradeOverlay.color = new Color(colorGradeTint.r, colorGradeTint.g, colorGradeTint.b, colorGradeStrength);
            }

            _colorGradeBaseAlpha = colorGradeOverlay.color;
            var cg = colorGradeOverlay.color;
            cg.r = colorGradeTint.r;
            cg.g = colorGradeTint.g;
            cg.b = colorGradeTint.b;
            cg.a = colorGradeStrength;
            colorGradeOverlay.color = cg;
            _colorGradeBaseAlpha = cg;

            if (bloomOrbTopLeft == null)
            {
                bloomOrbTopLeft = CreateBloomOrb(root, "Bloom_TL", new Vector2(0f, 1f), new Vector2(1f, 1f),
                    new Vector2(100f, -100f), new Vector2(520f, 520f));
            }

            if (bloomOrbBottomRight == null)
            {
                bloomOrbBottomRight = CreateBloomOrb(root, "Bloom_BR", new Vector2(1f, 0f), new Vector2(1f, 0f),
                    new Vector2(-120f, 140f), new Vector2(560f, 560f));
            }

            ApplyBloomVisuals();
        }

        RawImage CreateBloomOrb(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 anchoredPos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(RawImage));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = anchorMin;
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;
            var raw = go.GetComponent<RawImage>();
            raw.texture = BuildBloomOrbTexture(256);
            raw.raycastTarget = false;
            return raw;
        }

        void ApplyBloomVisuals()
        {
            if (bloomOrbTopLeft != null)
            {
                var c = bloomTint;
                bloomOrbTopLeft.color = c;
            }

            if (bloomOrbBottomRight != null)
            {
                var c = bloomTint;
                c.a *= 0.85f;
                c.b = Mathf.Min(1f, c.b + 0.08f);
                bloomOrbBottomRight.color = c;
            }
        }

        void LateUpdate()
        {
            float uTime = Time.unscaledTime;
            if (mode == BackgroundMode.Parallax)
                TickParallax();
            else if (mode == BackgroundMode.Static && staticFloatAmplitude > 0.01f)
                TickStaticFloat();

            if (enableCinematicPost)
                TickCinematicPost(uTime);
        }

        void TickCinematicPost(float uTime)
        {
            if (colorGradeOverlay != null && colorGradeBreath > 0f)
            {
                var c = _colorGradeBaseAlpha;
                c.a = Mathf.Clamp01(_colorGradeBaseAlpha.a + Mathf.Sin(uTime * 0.7f) * colorGradeBreath);
                colorGradeOverlay.color = c;
            }

            if (bloomOrbTopLeft != null && bloomPulseAmount > 0f)
            {
                float p = 1f + Mathf.Sin(uTime * bloomPulseSpeed) * bloomPulseAmount;
                bloomOrbTopLeft.rectTransform.localScale = Vector3.one * p;
            }

            if (bloomOrbBottomRight != null && bloomPulseAmount > 0f)
            {
                float p = 1f + Mathf.Sin(uTime * bloomPulseSpeed * 0.85f + 1.7f) * bloomPulseAmount * 0.9f;
                bloomOrbBottomRight.rectTransform.localScale = Vector3.one * p;
            }
        }

        Vector2 GetSmoothedMouseParallaxNorm()
        {
            if (!mouseParallaxEnabled || Screen.width < 2 || Screen.height < 2)
                return Vector2.zero;

            var m = (Vector2)Input.mousePosition;
            var target = new Vector2(
                (m.x / Screen.width - 0.5f) * 2f,
                (m.y / Screen.height - 0.5f) * 2f);
            target = Vector2.ClampMagnitude(target, 1f);
            _mouseParallaxVel = Vector2.Lerp(_mouseParallaxVel, target,
                Time.unscaledDeltaTime * mouseParallaxSmoothing);
            return _mouseParallaxVel;
        }

        void TickParallax()
        {
            float t = Time.unscaledTime;
            Vector2 mouse = GetSmoothedMouseParallaxNorm();

            if (useThreeLayerParallax)
            {
                ApplyParallaxLayer(parallaxFar, 0, t, mouse);
                ApplyParallaxLayer(parallaxMid, 1, t, mouse);
                ApplyParallaxLayer(parallaxNear, 2, t, mouse);
            }
            else
            {
                for (int i = 0; i < parallaxLayers.Count; i++)
                {
                    var l = parallaxLayers[i];
                    if (l.Rect == null) continue;
                    var b = i < _parallaxBases.Length ? _parallaxBases[i] : l.Rect.anchoredPosition;
                    float ox = Mathf.Sin(t * 0.4f + i * 0.7f) * l.Amplitude * (l.Speed.x * 0.05f);
                    float oy = Mathf.Cos(t * 0.32f + i * 0.5f) * l.Amplitude * (l.Speed.y * 0.05f);
                    Vector2 mOff = mouse * (8f + i * 10f);
                    l.Rect.anchoredPosition = b + new Vector2(ox, oy) + mOff;
                }
            }
        }

        void ApplyParallaxLayer(ParallaxLayerConfig cfg, int index, float time, Vector2 mouseNorm)
        {
            if (cfg.Rect == null) return;
            Vector2 b = _parallax3Bases[index];
            float ph = cfg.PhaseOffset + index * 0.9f;
            float idleX = Mathf.Sin(time * cfg.IdleSpeed.x + ph) * cfg.IdleAmplitude;
            float idleY = Mathf.Cos(time * cfg.IdleSpeed.y + ph * 1.1f) * cfg.IdleAmplitude * 0.82f;
            Vector2 mouseOff = mouseNorm * cfg.MouseParallaxPixels;
            cfg.Rect.anchoredPosition = b + new Vector2(idleX, idleY) + mouseOff;
        }

        void TickStaticFloat()
        {
            if (!_staticBasesCached) CacheStaticBases();
            if (staticGradientImage == null) return;
            float t = Time.unscaledTime * staticFloatSpeed;
            var off = new Vector2(Mathf.Sin(t) * staticFloatAmplitude, Mathf.Cos(t * 0.87f) * staticFloatAmplitude * 0.55f);
            staticGradientImage.rectTransform.anchoredPosition = _staticGradBase + off;
        }

        static void Stretch(RectTransform rt)
        {
            if (rt == null) return;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        static Texture2D BuildVerticalGradient(int width, int height, Color top, Color mid, Color bottom)
        {
            var tex = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            var px = new Color[width * height];
            for (int y = 0; y < height; y++)
            {
                float v = y / (float)(height - 1);
                Color row = v > 0.5f
                    ? Color.Lerp(mid, top, (v - 0.5f) * 2f)
                    : Color.Lerp(bottom, mid, v * 2f);
                for (int x = 0; x < width; x++)
                    px[y * width + x] = row;
            }

            tex.SetPixels(px);
            tex.Apply(false, true);
            return tex;
        }

        static Texture2D BuildVignetteSoft(int w, int h, Color edge)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            float cx = w * 0.5f, cy = h * 0.5f;
            float maxR = Mathf.Sqrt(cx * cx + cy * cy);
            var px = new Color[w * h];
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), new Vector2(cx, cy)) / maxR;
                float a = Mathf.Clamp01((d - 0.35f) / 0.65f) * edge.a;
                px[y * w + x] = new Color(edge.r, edge.g, edge.b, a);
            }

            tex.SetPixels(px);
            tex.Apply(false, true);
            return tex;
        }

        /// <summary>Viñeta más marcada para vídeo (esquinas oscuras).</summary>
        static Texture2D BuildCinematicVignette(int w, int h, float intensity)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            float cx = w * 0.5f, cy = h * 0.5f;
            float maxR = Mathf.Sqrt(cx * cx + cy * cy);
            var px = new Color[w * h];
            float aMul = Mathf.Clamp01(intensity);
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), new Vector2(cx, cy)) / maxR;
                float a = Mathf.Pow(Mathf.Clamp01((d - 0.25f) / 0.75f), 1.35f) * aMul * 0.92f;
                px[y * w + x] = new Color(0f, 0f, 0f, a);
            }

            tex.SetPixels(px);
            tex.Apply(false, true);
            return tex;
        }

        static Texture2D BuildBloomOrbTexture(int sz)
        {
            var tex = new Texture2D(sz, sz, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            float h = sz * 0.5f;
            var px = new Color[sz * sz];
            for (int y = 0; y < sz; y++)
            for (int x = 0; x < sz; x++)
            {
                float dx = (x - h) / h, dy = (y - h) / h;
                float r = Mathf.Sqrt(dx * dx + dy * dy);
                float a = Mathf.Pow(Mathf.Clamp01(1f - r), 2.4f);
                px[y * sz + x] = new Color(1f, 1f, 1f, a);
            }

            tex.SetPixels(px);
            tex.Apply(false, true);
            return tex;
        }
    }
}
