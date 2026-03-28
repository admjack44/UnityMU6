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
    /// HUD estilo MU Origin: orbes HP/MP = círculos 3D lisos (alto relieve, no facetados),
    /// 4 slots cuadrados solo para pociones, barras verticales, cluster de acción a la derecha.
    /// </summary>
    public class GameScreen : MonoBehaviour
    {
        private RawImage         _hpFillRaw, _mpFillRaw;
        private TextMeshProUGUI  _hpVal, _mpVal;
        private TextMeshProUGUI  _goldVal;
        private RectTransform    _expFillRt;
        private TextMeshProUGUI  _expLblTxt;
        private TextMeshProUGUI  _chatLog;
        private TMP_InputField   _chatInput;
        private Button[]         _tabBtns;
        private TextMeshProUGUI[] _tabTxts;
        private int              _activeTab = 1;

        private int    _hp = 110, _maxHp = 110;
        private int    _mp = 60,  _maxMp = 60;
        private long   _exp = 0,  _maxExp = 1000;
        private int    _level = 1;
        private string _charName = "Héroe";
        private long   _gold = 0;

        private readonly System.Collections.Generic.Queue<string> _chatQ = new();
        private const int MAX_CHAT = 10;

        static Color Hex(float r, float g, float b, float a = 1f) => new Color(r, g, b, a);
        static readonly Color HpBright  = Hex(0.98f, 0.14f, 0.10f);
        static readonly Color HpDark    = Hex(0.22f, 0.02f, 0.02f);
        static readonly Color MpBright  = Hex(0.18f, 0.42f, 1.00f);
        static readonly Color MpDark    = Hex(0.02f, 0.05f, 0.28f);
        static readonly Color ExpYellow = Hex(0.95f, 0.78f, 0.06f);
        static readonly Color Gold      = Hex(0.98f, 0.86f, 0.42f);
        static readonly Color GoldDim   = Hex(0.55f, 0.42f, 0.14f, 0.9f);
        static readonly Color BarDark   = Hex(0.03f, 0.02f, 0.06f, 0.98f);
        static readonly Color MetalDark = Hex(0.10f, 0.07f, 0.06f, 1f);
        static readonly Color MetalMid  = Hex(0.22f, 0.16f, 0.12f, 1f);
        static readonly Color TabOn     = Hex(0.24f, 0.16f, 0.05f, 1f);
        static readonly Color TabOff    = Hex(0.05f, 0.04f, 0.08f, 1f);

        const float ORB_VIS   = 108f;
        const float ORB_BLOCK_H = ORB_VIS + 22f;
        const float BAR_PAD   = 10f;
        const float FULL_BAR_H = BAR_PAD + ORB_BLOCK_H + 6f;
        const float EXP_H     = 14f;
        const float SLOT_SZ   = 54f;
        const float SLOT_GAP  = 5f;

        static readonly string[] TABS = { "Peace", "Union", "Team", "All" };

        float BottomHudTop => EXP_H + FULL_BAR_H;

        void Awake()
        {
            if (FindFirstObjectByType<EventSystem>() == null)
            {
                var es = new GameObject("EventSystem");
                es.AddComponent<EventSystem>();
                es.AddComponent<InputSystemUIInputModule>();
            }
            BuildHUD();
        }

        void OnEnable()
        {
            EventBus.Subscribe<WorldEvents.MapLoaded>(OnMapLoaded);
            EventBus.Subscribe<WorldEvents.PlayerStatsReceived>(OnPlayerStats);
            ApplyCachedServerStats();
        }

        void OnDisable()
        {
            EventBus.Unsubscribe<WorldEvents.MapLoaded>(OnMapLoaded);
            EventBus.Unsubscribe<WorldEvents.PlayerStatsReceived>(OnPlayerStats);
        }

        void ApplyCachedServerStats()
        {
            var gm = GameManager.Instance;
            if (gm == null || !gm.HasPlayerStats) return;
            ApplyServerStats(gm.LastPlayerStats);
        }

        void OnPlayerStats(WorldEvents.PlayerStatsReceived e) => ApplyServerStats(e);

        void ApplyServerStats(WorldEvents.PlayerStatsReceived e)
        {
            if (string.IsNullOrEmpty(e.Name)) return;
            SetNameLevel(e.Name, e.Level);
            SetStats(e.Hp, e.MaxHp, e.Mp, e.MaxMp);
            SetGold(e.Zen);
            long maxExp = e.ExpMax > 0 ? e.ExpMax : 1;
            SetExp(e.Exp, maxExp);
        }

        void BuildHUD()
        {
            var cvGo = new GameObject("GameHUDCanvas");
            var cv   = cvGo.AddComponent<Canvas>();
            cv.renderMode = RenderMode.ScreenSpaceOverlay;
            cv.sortingOrder = 20;
            var sc = cvGo.AddComponent<CanvasScaler>();
            sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            sc.referenceResolution = xy(1920, 1080);
            sc.matchWidthOrHeight  = 0.5f;
            cvGo.AddComponent<GraphicRaycaster>();
            cvGo.transform.SetParent(transform, false);
            var root = cvGo.transform;

            BuildExpStrip(root);
            BuildMainBar(root);
            BuildActionCluster(root);
            BuildChatBox(root);
        }

        void BuildExpStrip(Transform root)
        {
            var bg = RI("ExpBG", root, Flat(4, BarDark));
            bg.raycastTarget = false;
            BotStrip(bg.GetComponent<RectTransform>(), EXP_H);

            var fGo = new GameObject("ExpFill");
            fGo.transform.SetParent(root, false);
            var fi = fGo.AddComponent<RawImage>();
            fi.texture = Flat(4, ExpYellow);
            fi.raycastTarget = false;
            _expFillRt = fGo.GetComponent<RectTransform>();
            _expFillRt.anchorMin = xy(0, 0);
            _expFillRt.anchorMax = xy(0, 0);
            _expFillRt.pivot = xy(0, 0);
            _expFillRt.anchoredPosition = Vector2.zero;
            _expFillRt.sizeDelta = xy(0, EXP_H);

            _expLblTxt = Txt("ExpLbl", root, 9f, Gold, true);
            _expLblTxt.alignment = TextAlignmentOptions.MidlineLeft;
            _expLblTxt.raycastTarget = false;
            var er = _expLblTxt.GetComponent<RectTransform>();
            er.anchorMin = xy(0, 0);
            er.anchorMax = xy(0.55f, 0);
            er.pivot = xy(0, 0);
            er.anchoredPosition = xy(8, 0);
            er.sizeDelta = xy(0, EXP_H);

            var bonus = Txt("ExpBonus", root, 8f, GoldDim);
            bonus.text = "  0% EXP Bonus ▲";
            bonus.alignment = TextAlignmentOptions.MidlineRight;
            bonus.raycastTarget = false;
            var br = bonus.GetComponent<RectTransform>();
            br.anchorMin = xy(0.45f, 0);
            br.anchorMax = xy(0.92f, 0);
            br.pivot = xy(1, 0);
            br.anchoredPosition = xy(-8, 0);
            br.sizeDelta = xy(0, EXP_H);

            RefreshExp();
        }

        void BuildMainBar(Transform root)
        {
            var barGo = new GameObject("BarBG");
            barGo.transform.SetParent(root, false);
            var barImg = barGo.AddComponent<RawImage>();
            barImg.texture = Flat(4, BarDark);
            barImg.raycastTarget = false;
            var barRt = barGo.GetComponent<RectTransform>();
            barRt.anchorMin = xy(0, 0);
            barRt.anchorMax = xy(1, 0);
            barRt.pivot = xy(0.5f, 0);
            barRt.anchoredPosition = xy(0, EXP_H);
            barRt.sizeDelta = xy(0, FULL_BAR_H);

            var ln = RI("BarLine", barGo.transform, Flat(2, GoldDim));
            ln.raycastTarget = false;
            var lnRt = ln.GetComponent<RectTransform>();
            lnRt.anchorMin = xy(0, 0);
            lnRt.anchorMax = xy(1, 0);
            lnRt.pivot = xy(0.5f, 1f);
            lnRt.anchoredPosition = xy(0, FULL_BAR_H);
            lnRt.sizeDelta = xy(0, 2f);

            float tabW = 76f;
            float baseY = BAR_PAD;
            // Centro: oro + 4 pociones (SD y combo son las barras verticales en los lados de los orbes).
            float centerW = 8f + 102f + (4f * SLOT_SZ + 3f * SLOT_GAP) + 24f;
            float halfC   = centerW * 0.5f;
            const float ORB_GAP = 14f;

            BuildOrbColumn(barGo.transform, true, baseY, xy(0.5f, 0f), xy(1f, 0f), xy(-halfC - ORB_GAP, baseY), out _hpFillRaw, out _hpVal);
            BuildOrbColumn(barGo.transform, false, baseY, xy(0.5f, 0f), xy(0f, 0f), xy(halfC + ORB_GAP, baseY), out _mpFillRaw, out _mpVal);
            BuildCenterRow(barGo.transform, baseY, centerW);
            BuildTabs(barGo.transform, tabW, FULL_BAR_H);

            RefreshBars();
        }

        /// <summary>Orbe + marco + barras + valor; posición respecto al centro del HUD (modelo móvil).</summary>
        void BuildOrbColumn(Transform parent, bool isHp, float baseY,
                            Vector2 anchor, Vector2 pivot, Vector2 anchoredPos,
                            out RawImage fillRaw, out TextMeshProUGUI valueTxt)
        {
            fillRaw = null;
            valueTxt = null;

            float colW = ORB_VIS + 52f;
            var col = new GameObject(isHp ? "ColHP" : "ColMP");
            col.transform.SetParent(parent, false);
            var cRt = col.AddComponent<RectTransform>();
            cRt.anchorMin = cRt.anchorMax = anchor;
            cRt.pivot = pivot;
            cRt.anchoredPosition = anchoredPos;
            cRt.sizeDelta = xy(colW, ORB_BLOCK_H);

            float barY = ORB_BLOCK_H * 0.5f + 4f;

            var orbArea = new GameObject("OrbArea");
            orbArea.transform.SetParent(col.transform, false);
            var oRt = orbArea.AddComponent<RectTransform>();
            if (isHp)
            {
                oRt.anchorMin = oRt.anchorMax = xy(0, 1f);
                oRt.pivot = xy(0, 1f);
                oRt.anchoredPosition = xy(6f, -4f);
            }
            else
            {
                oRt.anchorMin = oRt.anchorMax = xy(1, 1f);
                oRt.pivot = xy(1, 1f);
                oRt.anchoredPosition = xy(-6f, -4f);
            }
            oRt.sizeDelta = xy(ORB_VIS + 8f, ORB_VIS + 8f);

            // Referencia MU: [Orbe HP][1 barra SD amarilla][oro+slots][1 barra combo cyan][Orbe MP]
            if (isHp)
                BuildSingleVerticalBar(col.transform, xy(0, 0.5f), xy(0, 0.5f),
                    xy(ORB_VIS + 8f, barY), xy(8f, 52f), "SDBar",
                    Hex(0.95f, 0.88f, 0.18f), 0.82f, null);
            else
                BuildSingleVerticalBar(col.transform, xy(1, 0.5f), xy(1, 0.5f),
                    xy(-ORB_VIS - 8f, barY), xy(8f, 52f), "ComboBar",
                    Hex(0.35f, 0.82f, 0.98f), 0.08f, "0%");

            var gemGo = new GameObject("Gem");
            gemGo.transform.SetParent(orbArea.transform, false);
            var gRt = gemGo.AddComponent<RectTransform>();
            gRt.anchorMin = gRt.anchorMax = xy(0.5f, 0.5f);
            gRt.sizeDelta = xy(ORB_VIS, ORB_VIS);
            gRt.anchoredPosition = Vector2.zero;

            Color br = isHp ? HpBright : MpBright;
            Color dk = isHp ? HpDark : MpDark;

            var bgGo = new GameObject("GemBG");
            bgGo.transform.SetParent(gemGo.transform, false);
            var bgRi = bgGo.AddComponent<RawImage>();
            bgRi.texture = MakeSmoothSphere(144, dk, dk * 0.55f);
            bgRi.raycastTarget = false;
            StretchRt(bgGo.GetComponent<RectTransform>());

            var fillGo = new GameObject("GemFill");
            fillGo.transform.SetParent(gemGo.transform, false);
            fillRaw = fillGo.AddComponent<RawImage>();
            fillRaw.texture = MakeSmoothSphere(144, br, dk);
            fillRaw.raycastTarget = false;
            StretchRt(fillGo.GetComponent<RectTransform>());

            var hlGo = new GameObject("Hl");
            hlGo.transform.SetParent(gemGo.transform, false);
            var hlRi = hlGo.AddComponent<RawImage>();
            hlRi.texture = MakeHighlight(96, Color.white);
            hlRi.raycastTarget = false;
            var hlRt = hlGo.GetComponent<RectTransform>();
            hlRt.anchorMin = xy(0.05f, 0.35f);
            hlRt.anchorMax = xy(0.48f, 0.92f);
            hlRt.offsetMin = hlRt.offsetMax = Vector2.zero;

            var spGo = new GameObject("Spec");
            spGo.transform.SetParent(gemGo.transform, false);
            var spRi = spGo.AddComponent<RawImage>();
            spRi.texture = MakeSpecular(48);
            spRi.raycastTarget = false;
            var spRt = spGo.GetComponent<RectTransform>();
            spRt.anchorMin = xy(0.12f, 0.52f);
            spRt.anchorMax = xy(0.36f, 0.78f);
            spRt.offsetMin = spRt.offsetMax = Vector2.zero;

            var rimGo = new GameObject("Rim");
            rimGo.transform.SetParent(gemGo.transform, false);
            var rimRi = rimGo.AddComponent<RawImage>();
            rimRi.texture = MakeOuterRim(144, MetalDark);
            rimRi.raycastTarget = false;
            StretchRt(rimGo.GetComponent<RectTransform>());

            valueTxt = Txt(isHp ? "HpV" : "MpV", orbArea.transform, 11f, Color.white, true);
            valueTxt.alignment = TextAlignmentOptions.Center;
            valueTxt.raycastTarget = false;
            var vRt = valueTxt.GetComponent<RectTransform>();
            vRt.anchorMin = vRt.anchorMax = xy(0.5f, 0f);
            vRt.pivot = xy(0.5f, 1f);
            vRt.anchoredPosition = xy(0, -8f);
            vRt.sizeDelta = xy(120, 18f);
        }

        /// <summary>Una barra vertical; <paramref name="belowText"/> opcional (p. ej. 0% bajo combo).</summary>
        void BuildSingleVerticalBar(Transform parent, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size,
                                    string barId, Color fillCol, float fill01, string belowText)
        {
            var g = new GameObject(barId);
            g.transform.SetParent(parent, false);
            var rt = g.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            var bg = g.AddComponent<RawImage>();
            bg.texture = Flat(4, Hex(0.02f, 0.02f, 0.03f, 0.95f));
            bg.raycastTarget = false;
            var fgGo = new GameObject("Fill");
            fgGo.transform.SetParent(g.transform, false);
            var fg = fgGo.AddComponent<RawImage>();
            fg.texture = Flat(4, fillCol);
            fg.raycastTarget = false;
            var frt = fgGo.GetComponent<RectTransform>();
            frt.anchorMin = xy(0.12f, 0);
            frt.anchorMax = xy(0.88f, fill01);
            frt.offsetMin = frt.offsetMax = Vector2.zero;

            if (!string.IsNullOrEmpty(belowText))
            {
                var t = Txt("BarLbl", g.transform, 9f, Hex(0.55f, 0.78f, 0.95f), true);
                t.text = belowText;
                t.alignment = TextAlignmentOptions.Center;
                t.raycastTarget = false;
                var tr = t.GetComponent<RectTransform>();
                tr.anchorMin = tr.anchorMax = xy(0.5f, 0f);
                tr.pivot = xy(0.5f, 1f);
                tr.anchoredPosition = xy(0, -6f);
                tr.sizeDelta = xy(48f, 16f);
            }
        }

        void BuildCenterRow(Transform parent, float baseY, float centerWidth)
        {
            var ctr = new GameObject("Center");
            ctr.transform.SetParent(parent, false);
            var cRt = ctr.AddComponent<RectTransform>();
            cRt.anchorMin = cRt.anchorMax = xy(0.5f, 0f);
            cRt.pivot = xy(0.5f, 0f);
            cRt.anchoredPosition = xy(0, baseY);
            cRt.sizeDelta = xy(centerWidth, ORB_BLOCK_H);

            float cy = ORB_BLOCK_H * 0.5f - 6f;

            _goldVal = Txt("GoldV", ctr.transform, 12f, Gold, true);
            _goldVal.alignment = TextAlignmentOptions.MidlineLeft;
            _goldVal.raycastTarget = false;
            SetRt(_goldVal.GetComponent<RectTransform>(), xy(0, 0), xy(0, 0), xy(4, cy), xy(100, 22));
            _goldVal.text = "0";

            var goldLbl = Txt("GoldIcoLbl", ctr.transform, 9f, GoldDim);
            goldLbl.text = "●";
            goldLbl.alignment = TextAlignmentOptions.MidlineLeft;
            goldLbl.raycastTarget = false;
            SetRt(goldLbl.GetComponent<RectTransform>(), xy(0, 0), xy(0, 0), xy(0, cy + 14f), xy(20, 14f));

            float startX = 108f;
            Color potEmpty = Hex(0.055f, 0.05f, 0.075f, 1f);
            for (int i = 0; i < 4; i++)
                BuildPotionSlot(ctr.transform, xy(startX + i * (SLOT_SZ + SLOT_GAP), cy - SLOT_SZ * 0.5f), potEmpty);
        }

        /// <summary>Cuatro iguales: marco metálico + hueco oscuro; solo pociones (icono/cantidad vendrá del inventario).</summary>
        void BuildPotionSlot(Transform parent, Vector2 pos, Color emptyInner)
        {
            var g = new GameObject("PotionSlot");
            g.transform.SetParent(parent, false);
            var rt = g.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = xy(0, 0);
            rt.pivot = xy(0, 0);
            rt.anchoredPosition = pos;
            rt.sizeDelta = xy(SLOT_SZ, SLOT_SZ);

            var frame = g.AddComponent<RawImage>();
            frame.texture = MakeBeveledSlotTex(96);
            frame.raycastTarget = false;

            var inGo = new GameObject("Inner");
            inGo.transform.SetParent(g.transform, false);
            var inn = inGo.AddComponent<RawImage>();
            inn.texture = Flat(4, emptyInner);
            inn.raycastTarget = false;
            var ir = inGo.GetComponent<RectTransform>();
            ir.anchorMin = xy(0.12f, 0.12f);
            ir.anchorMax = xy(0.88f, 0.72f);
            ir.offsetMin = ir.offsetMax = Vector2.zero;
        }

        void BuildTabs(Transform parent, float tabW, float fullH)
        {
            var ct = new GameObject("Tabs");
            ct.transform.SetParent(parent, false);
            var ctRt = ct.AddComponent<RectTransform>();
            ctRt.anchorMin = ctRt.anchorMax = xy(1, 0);
            ctRt.pivot = xy(1, 0);
            ctRt.anchoredPosition = xy(-2, 0);
            ctRt.sizeDelta = xy(tabW, fullH);

            var ctBg = ct.AddComponent<RawImage>();
            ctBg.texture = Flat(4, Hex(0.04f, 0.03f, 0.06f, 0.98f));
            ctBg.raycastTarget = false;

            _tabBtns = new Button[TABS.Length];
            _tabTxts = new TextMeshProUGUI[TABS.Length];
            float btnH = fullH / TABS.Length;

            for (int i = 0; i < TABS.Length; i++)
            {
                int idx = i;
                var bg = new GameObject($"Tab{i}");
                bg.transform.SetParent(ct.transform, false);
                var bRt = bg.AddComponent<RectTransform>();
                bRt.anchorMin = xy(0, 0);
                bRt.anchorMax = xy(1, 0);
                bRt.pivot = xy(0.5f, 0);
                bRt.anchoredPosition = xy(0, (TABS.Length - 1 - i) * btnH);
                bRt.sizeDelta = xy(0, btnH - 1f);

                var bImg = bg.AddComponent<Image>();
                bImg.color = i == _activeTab ? TabOn : TabOff;

                var btn = bg.AddComponent<Button>();
                var bc = btn.colors;
                bc.highlightedColor = Hex(0.32f, 0.22f, 0.08f);
                bc.pressedColor = Hex(0.48f, 0.34f, 0.12f);
                bc.fadeDuration = 0.05f;
                btn.colors = bc;

                bool active = i == _activeTab;
                var lbl = Txt($"TL{i}", bg.transform, 9f, active ? Gold : Hex(0.55f, 0.50f, 0.38f), active);
                lbl.text = TABS[i];
                lbl.alignment = TextAlignmentOptions.Center;
                lbl.raycastTarget = false;
                StretchRt(lbl.GetComponent<RectTransform>());

                _tabBtns[i] = btn;
                _tabTxts[i] = lbl;
                btn.onClick.AddListener(() => SelectTab(idx));
            }

            var clock = Txt("Clock", ct.transform, 8f, Hex(0.7f, 0.65f, 0.55f));
            clock.alignment = TextAlignmentOptions.BottomRight;
            clock.raycastTarget = false;
            var cr = clock.GetComponent<RectTransform>();
            cr.anchorMin = xy(0, 0);
            cr.anchorMax = xy(1, 0);
            cr.pivot = xy(1, 0);
            cr.anchoredPosition = xy(-4, 4);
            cr.sizeDelta = xy(tabW - 4, 14f);
            clock.text = System.DateTime.Now.ToString("HH:mm");
        }

        /// <summary>Esquina inferior derecha: ataque grande + arco de skills a su izquierda/arriba (como captura MU).</summary>
        void BuildActionCluster(Transform root)
        {
            const float mainR = 52f;
            const float skR   = 28f;
            const float arcR  = 80f;

            var cluster = new GameObject("ActionCluster");
            cluster.transform.SetParent(root, false);
            var cr = cluster.AddComponent<RectTransform>();
            cr.anchorMin = cr.anchorMax = xy(1, 0);
            cr.pivot = xy(1, 0);
            cr.anchoredPosition = xy(-6f - 76f, BottomHudTop + 4f);
            cr.sizeDelta = xy(248f, 228f);

            // Utilidades arriba-izquierda del grupo (Manual referencia tiene silueta arriba; Inv/Man compactos)
            BuildClusterBtn(cluster.transform, "Inv", xy(-218f, 198f), 28f, "Inv", Hex(0.38f, 0.30f, 0.14f));
            BuildClusterBtn(cluster.transform, "Man", xy(-218f, 162f), 28f, "Man", Hex(0.24f, 0.24f, 0.34f));

            // Botón principal pegado a la esquina inferior derecha del cluster
            float cx = -64f, cy = 70f;
            BuildMainAttackBtn(cluster.transform, xy(cx, cy), mainR);

            // Arco hacia la izquierda y arriba del botón principal (semi-círculo como en la imagen)
            float[] degs = { 160f, 140f, 120f, 100f, 80f };
            for (int i = 0; i < degs.Length; i++)
            {
                float rad = degs[i] * Mathf.Deg2Rad;
                Vector2 o = xy(cx + Mathf.Cos(rad) * arcR, cy + Mathf.Sin(rad) * arcR);
                Color ac = (i & 1) == 0 ? Hex(0.42f, 0.30f, 0.14f) : Hex(0.26f, 0.22f, 0.36f);
                BuildClusterBtn(cluster.transform, $"Sk{i + 1}", o, skR, $"{i + 1}", ac);
            }
        }

        void BuildMainAttackBtn(Transform parent, Vector2 localPos, float radius)
        {
            var go = new GameObject("AttackMain");
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = xy(0, 0);
            rt.pivot = xy(0.5f, 0.5f);
            rt.anchoredPosition = localPos;
            rt.sizeDelta = xy(radius * 2f, radius * 2f);

            var bg = go.AddComponent<RawImage>();
            bg.texture = MakeSkillCircle(160, Hex(0.55f, 0.38f, 0.12f));
            bg.raycastTarget = false;

            var rimGo = new GameObject("Rim");
            rimGo.transform.SetParent(go.transform, false);
            var rim = rimGo.AddComponent<RawImage>();
            rim.texture = MakeOuterRim(160, MetalMid);
            rim.raycastTarget = false;
            StretchRt(rimGo.GetComponent<RectTransform>());

            // Sin carácter Unicode especial: LiberationSans SDF no incluye ⚔ y dispara error en consola.
            var icon = Txt("Sword", go.transform, 15f, Color.white, true);
            icon.text = "ATK";
            icon.alignment = TextAlignmentOptions.Center;
            icon.raycastTarget = false;
            StretchRt(icon.GetComponent<RectTransform>());

            var btn = go.AddComponent<Button>();
            var c = btn.colors;
            c.highlightedColor = Hex(1.15f, 1.05f, 0.9f);
            c.pressedColor = Hex(0.75f, 0.6f, 0.35f);
            c.fadeDuration = 0.06f;
            btn.colors = c;
        }

        void BuildClusterBtn(Transform parent, string id, Vector2 localPos, float radius, string label, Color accent)
        {
            var go = new GameObject(id);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = xy(0, 0);
            rt.pivot = xy(0.5f, 0.5f);
            rt.anchoredPosition = localPos;
            rt.sizeDelta = xy(radius * 2f, radius * 2f);

            var bg = go.AddComponent<RawImage>();
            bg.texture = MakeSkillCircle(128, accent);
            bg.raycastTarget = false;

            var rimGo = new GameObject("Rim");
            rimGo.transform.SetParent(go.transform, false);
            var rim = rimGo.AddComponent<RawImage>();
            rim.texture = MakeOuterRim(128, MetalDark);
            rim.raycastTarget = false;
            StretchRt(rimGo.GetComponent<RectTransform>());

            var t = Txt($"L{id}", go.transform, radius > 22f ? 13f : 8f, Hex(0.92f, 0.9f, 0.85f), true);
            t.text = label;
            t.alignment = TextAlignmentOptions.Center;
            t.raycastTarget = false;
            StretchRt(t.GetComponent<RectTransform>());

            go.AddComponent<Button>();
        }

        void BuildChatBox(Transform root)
        {
            float bottomH = BottomHudTop + 8f;
            var panel = new GameObject("ChatPanel");
            panel.transform.SetParent(root, false);
            var pRt = panel.AddComponent<RectTransform>();
            pRt.anchorMin = xy(0, 0);
            pRt.anchorMax = xy(0, 1);
            pRt.pivot = xy(0, 0);
            pRt.anchoredPosition = xy(6, bottomH);
            pRt.sizeDelta = xy(380f, -(bottomH + 220f));

            var bg = panel.AddComponent<RawImage>();
            bg.texture = Flat(4, Hex(0, 0, 0, 0.38f));
            bg.raycastTarget = false;

            _chatLog = Txt("Log", panel.transform, 10f, Color.white);
            _chatLog.alignment = TextAlignmentOptions.BottomLeft;
            _chatLog.raycastTarget = false;
            var lRt = _chatLog.GetComponent<RectTransform>();
            lRt.anchorMin = xy(0, 0);
            lRt.anchorMax = xy(1, 1);
            lRt.offsetMin = xy(4, 22);
            lRt.offsetMax = xy(-4, -2);

            _chatInput = UIBuilder.CreateInputField(panel.transform, "ChatIF", "Escribe aquí...");
            var cRt2 = _chatInput.GetComponent<RectTransform>();
            cRt2.anchorMin = xy(0, 0);
            cRt2.anchorMax = xy(1, 0);
            cRt2.pivot = xy(0, 0);
            cRt2.anchoredPosition = Vector2.zero;
            cRt2.sizeDelta = xy(0, 20);
            _chatInput.GetComponent<Image>().color = Hex(0.05f, 0.03f, 0.12f, 0.92f);
            _chatInput.onSubmit.AddListener(SendChat);

            AddChat("<color=#FFD966>[Sistema]</color> Bienvenido a MU PEGASO");
            AddChat("<color=#88BBFF>WASD o click derecho para moverte.</color>");
        }

        // ── Texturas procedurales ───────────────────────────────────────────

        /// <summary>Esfera 3D continua (círculo perfecto, sin facetas): difuso + especular + borde suave.</summary>
        static Texture2D MakeSmoothSphere(int sz, Color bright, Color dark)
        {
            var tex = new Texture2D(sz, sz, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            var px = new Color[sz * sz];
            float h = sz * 0.5f;
            Vector3 L = new Vector3(-0.5f, 0.68f, 0.52f).normalized;
            Vector3 Vdir = Vector3.forward;

            for (int y = 0; y < sz; y++)
            for (int x = 0; x < sz; x++)
            {
                float dx = (x - h) / h, dy = (y - h) / h;
                float d2 = dx * dx + dy * dy;
                if (d2 > 1f) { px[y * sz + x] = Color.clear; continue; }

                float nz = Mathf.Sqrt(Mathf.Max(0f, 1f - d2));
                Vector3 N = new Vector3(dx, dy, nz).normalized;
                float diff = Mathf.Max(0f, Vector3.Dot(N, L));
                Vector3 Hh = (L + Vdir).normalized;
                float spec = Mathf.Pow(Mathf.Max(0f, Vector3.Dot(N, Hh)), 42f);
                float rim = Mathf.Pow(1f - nz, 3.5f);

                Color c = Color.Lerp(dark, bright, Mathf.Clamp01(diff * 0.95f + 0.06f));
                c.r = Mathf.Clamp01(c.r + spec * 0.9f);
                c.g = Mathf.Clamp01(c.g + spec * 0.9f);
                c.b = Mathf.Clamp01(c.b + spec * 0.9f);
                c = Color.Lerp(c, bright * 0.45f, rim * 0.28f);
                c.a = 1f;
                px[y * sz + x] = c;
            }
            tex.SetPixels(px);
            tex.Apply();
            return tex;
        }

        static Texture2D MakeBeveledSlotTex(int sz)
        {
            var tex = new Texture2D(sz, sz, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            var px = new Color[sz * sz];
            float h = sz * 0.5f;
            for (int y = 0; y < sz; y++)
            for (int x = 0; x < sz; x++)
            {
                float u = (x - h) / h, v = (y - h) / h;
                float ax = Mathf.Abs(u), ay = Mathf.Abs(v);
                float m = Mathf.Max(ax, ay);
                if (m > 1f) { px[y * sz + x] = Color.clear; continue; }

                float edge = Mathf.Clamp01((m - 0.72f) / 0.28f);
                float hi = Mathf.Clamp01(1f - m) * (1f - v * 0.35f);
                Color c = Color.Lerp(MetalDark, MetalMid, hi * 0.5f);
                c = Color.Lerp(c, GoldDim, edge * 0.55f);
                c.a = 1f;
                px[y * sz + x] = c;
            }
            tex.SetPixels(px);
            tex.Apply();
            return tex;
        }

        static Texture2D MakeHighlight(int sz, Color light)
        {
            var tex = new Texture2D(sz, sz, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            var px = new Color[sz * sz];
            for (int y = 0; y < sz; y++)
            for (int x = 0; x < sz; x++)
            {
                float u = x / (float)sz, v = y / (float)sz;
                float dx = u - 0.22f, dy = v - 0.72f;
                float r = Mathf.Sqrt(dx * dx * 5f + dy * dy * 3.5f);
                float a = Mathf.Pow(Mathf.Clamp01(1f - r * 2.2f), 2f) * 0.55f;
                px[y * sz + x] = new Color(light.r, light.g, light.b, a);
            }
            tex.SetPixels(px);
            tex.Apply();
            return tex;
        }

        static Texture2D MakeSpecular(int sz)
        {
            var tex = new Texture2D(sz, sz, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            var px = new Color[sz * sz];
            for (int y = 0; y < sz; y++)
            for (int x = 0; x < sz; x++)
            {
                float u = x / (float)sz - 0.25f, v = y / (float)sz - 0.78f;
                float r = Mathf.Sqrt(u * u * 8f + v * v * 6f);
                float a = Mathf.Pow(Mathf.Clamp01(1f - r * 4f), 3f) * 0.88f;
                px[y * sz + x] = new Color(1, 1, 1, a);
            }
            tex.SetPixels(px);
            tex.Apply();
            return tex;
        }

        static Texture2D MakeOuterRim(int sz, Color rimCol)
        {
            var tex = new Texture2D(sz, sz, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            var px = new Color[sz * sz];
            float h = sz * 0.5f;
            for (int y = 0; y < sz; y++)
            for (int x = 0; x < sz; x++)
            {
                float d = Mathf.Sqrt((x - h) * (x - h) + (y - h) * (y - h)) / h;
                float ring = Mathf.Clamp01((d - 0.84f) / 0.16f);
                px[y * sz + x] = d > 1f ? Color.clear : new Color(rimCol.r, rimCol.g, rimCol.b, ring * 0.95f);
            }
            tex.SetPixels(px);
            tex.Apply();
            return tex;
        }

        static Texture2D MakeSkillCircle(int sz, Color accent)
        {
            var tex = new Texture2D(sz, sz, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            var px = new Color[sz * sz];
            float h = sz * 0.5f;
            Color mid = Hex(0.10f, 0.08f, 0.14f);
            for (int y = 0; y < sz; y++)
            for (int x = 0; x < sz; x++)
            {
                float dx = (x - h) / h, dy = (y - h) / h;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                if (dist > 1f) { px[y * sz + x] = Color.clear; continue; }
                float nz = Mathf.Sqrt(Mathf.Max(0f, 1f - dx * dx - dy * dy));
                float rim = Mathf.Pow(1f - nz, 3.2f);
                Color c = Color.Lerp(mid, accent * 0.5f, rim);
                float glow = Mathf.Pow(nz, 2.2f) * 0.22f;
                c.r = Mathf.Clamp01(c.r + glow);
                c.g = Mathf.Clamp01(c.g + glow);
                c.b = Mathf.Clamp01(c.b + glow);
                c.a = 1f;
                px[y * sz + x] = c;
            }
            tex.SetPixels(px);
            tex.Apply();
            return tex;
        }

        static Texture2D Flat(int sz, Color col)
        {
            var tex = new Texture2D(sz, sz);
            var px = new Color[sz * sz];
            for (int i = 0; i < px.Length; i++) px[i] = col;
            tex.SetPixels(px);
            tex.Apply();
            return tex;
        }

        public void SetStats(int hp, int maxHp, int mp, int maxMp)
        {
            _hp = hp;
            _maxHp = maxHp;
            _mp = mp;
            _maxMp = maxMp;
            RefreshBars();
        }

        public void SetGold(long gold)
        {
            _gold = gold;
            if (_goldVal) _goldVal.text = $"{gold:N0}";
        }

        public void SetExp(long exp, long maxExp)
        {
            _exp = exp;
            _maxExp = maxExp;
            RefreshExp();
        }

        public void SetNameLevel(string name, int level)
        {
            _charName = name;
            _level = level;
            RefreshExp();
        }

        public void AddChat(string line)
        {
            if (_chatQ.Count >= MAX_CHAT) _chatQ.Dequeue();
            _chatQ.Enqueue(line);
            if (_chatLog) _chatLog.text = string.Join("\n", _chatQ);
        }

        void RefreshBars()
        {
            OrbFill(_hpFillRaw, _hpVal, _hp, _maxHp);
            OrbFill(_mpFillRaw, _mpVal, _mp, _maxMp);
        }

        void OrbFill(RawImage raw, TextMeshProUGUI val, int cur, int max)
        {
            if (raw == null) return;
            float t = max > 0 ? Mathf.Clamp01((float)cur / max) : 0f;
            var rt = raw.GetComponent<RectTransform>();
            rt.anchorMin = xy(0, 0);
            rt.anchorMax = xy(1, t);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            if (val) val.text = $"{cur:N0}";
        }

        void RefreshExp()
        {
            if (_expFillRt == null || _maxExp <= 0) return;
            float pct = Mathf.Clamp01((float)_exp / _maxExp);
            _expFillRt.anchorMin = xy(0, 0);
            _expFillRt.anchorMax = xy(pct, 0);
            _expFillRt.offsetMin = Vector2.zero;
            _expFillRt.offsetMax = xy(0, EXP_H);
            if (_expLblTxt) _expLblTxt.text = $"  Nivel {_level}   {pct * 100:F1}%";
        }

        void SendChat(string msg)
        {
            if (string.IsNullOrWhiteSpace(msg)) return;
            AddChat($"<color=#FFD966>{_charName}:</color> {msg}");
            NetworkClient.Instance?.Send(ClientPackets.ChatMessage(msg));
            _chatInput.text = "";
            _chatInput.ActivateInputField();
        }

        void SelectTab(int idx)
        {
            _activeTab = idx;
            for (int i = 0; i < TABS.Length; i++)
            {
                bool a = i == idx;
                if (_tabBtns[i]) _tabBtns[i].GetComponent<Image>().color = a ? TabOn : TabOff;
                if (_tabTxts[i])
                {
                    _tabTxts[i].color = a ? Gold : Hex(0.55f, 0.50f, 0.38f);
                    _tabTxts[i].fontStyle = a ? FontStyles.Bold : FontStyles.Normal;
                }
            }
            AddChat($"<color=#AAAAAA>[{TABS[idx]}]</color>");
        }

        void OnMapLoaded(WorldEvents.MapLoaded e)
        {
            string[] M = { "Lorencia", "Dungeon", "Devias", "Noria", "Lost Tower", "Atlans", "Tarkan" };
            AddChat($"<color=#88BBFF>[Mapa] {(e.MapId < M.Length ? M[e.MapId] : $"Map{e.MapId}")} </color>");
        }

        RawImage RI(string n, Transform p, Texture2D tex)
        {
            var go = new GameObject(n);
            go.transform.SetParent(p, false);
            go.AddComponent<RectTransform>();
            var r = go.AddComponent<RawImage>();
            r.texture = tex;
            return r;
        }

        TextMeshProUGUI Txt(string n, Transform p, float fs, Color col, bool bold = false)
        {
            var go = new GameObject(n);
            go.transform.SetParent(p, false);
            go.AddComponent<RectTransform>();
            var t = go.AddComponent<TextMeshProUGUI>();
            t.fontSize = fs;
            t.color = col;
            if (bold) t.fontStyle = FontStyles.Bold;
            return t;
        }

        void StretchRt(RectTransform rt, Vector2 oMin = default, Vector2 oMax = default)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = oMin;
            rt.offsetMax = oMax;
        }

        void BotStrip(RectTransform rt, float h)
        {
            rt.anchorMin = xy(0, 0);
            rt.anchorMax = xy(1, 0);
            rt.pivot = xy(0.5f, 0f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = xy(0, h);
        }

        void SetRt(RectTransform rt, Vector2 aMin, Vector2 aMax, Vector2 pos, Vector2 sz)
        {
            rt.anchorMin = aMin;
            rt.anchorMax = aMax;
            rt.pivot = xy(0f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = sz;
        }

        static Vector2 xy(float x, float y) => new Vector2(x, y);
    }
}
