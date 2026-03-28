using UnityEngine;
using UnityEngine.UI;
using TMPro;
using MuOnline.Core;
using MuOnline.Gameplay.AutoBattle;
using MuOnline.Gameplay.Combat;
using MuOnline.Gameplay.Enemies;
using MuOnline.Gameplay.Input;
using MuOnline.Gameplay.Inventory;
using MuOnline.Gameplay.Pickup;
using MuOnline.Gameplay.Player;
using MuOnline.Gameplay.Skills;
using MuOnline.Gameplay.Targeting;
using MuOnline.UI;
using MuOnline.UI.DamageText;
using MuOnline.UI.Gameplay;
using MuOnline.UI.Minimap;
using MuOnline.UI.Windows;

namespace MuOnline.Gameplay
{
    /// <summary>
    /// Bootstrap de la escena <c>Game</c>: mundo, jugador, HUD, ventanas, enemigos y FX.
    /// Sustituir gradualmente por Addressables / prefabs de producción.
    /// </summary>
    public class GameSceneSetup : MonoBehaviour
    {
        [Header("Spawns")]
        [SerializeField] private int enemyCount = 4;
        [SerializeField] private float enemySpawnRadius = 14f;
        [SerializeField] private LayerMask enemyLayerMask = ~0;

        GameScreen _hud;
        PlayerController _player;
        CameraController _cam;
        GameObject _playerGo;

        void Awake()
        {
            BuildGround();
            BuildFxRoot();
            var hudRoot = BuildHudHost();
            BuildMobileControls();
            BuildWorldUi(hudRoot);
            SpawnPlayer(new Vector3(0f, 1f, 0f));
            SpawnEnemies();
        }

        void BuildGround()
        {
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.position = Vector3.zero;
            ground.transform.localScale = new Vector3(25.6f, 1f, 25.6f);

            var rend = ground.GetComponent<Renderer>();
            if (rend != null)
            {
                var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                mat.color = new Color(0.18f, 0.16f, 0.14f);
                rend.material = mat;
            }
        }

        void BuildFxRoot()
        {
            var fx = new GameObject("DamageFloaters");
            fx.transform.position = Vector3.zero;
            fx.AddComponent<DamageFloaterService>();
        }

        Transform BuildHudHost()
        {
            var hudGo = new GameObject("GameplayHud");
            _hud = hudGo.AddComponent<GameScreen>();
            hudGo.AddComponent<GameplayHudRelay>();
            hudGo.AddComponent<GameplayActionBinder>();
            var canvasTf = hudGo.transform.Find("GameHUDCanvas");
            if (canvasTf != null)
            {
                var minimapGo = new GameObject("MinimapPlaceholder");
                minimapGo.transform.SetParent(canvasTf, false);
                var mrt = minimapGo.AddComponent<RectTransform>();
                mrt.anchorMin = new Vector2(1f, 1f);
                mrt.anchorMax = new Vector2(1f, 1f);
                mrt.pivot = new Vector2(1f, 1f);
                mrt.anchoredPosition = new Vector2(-16f, -16f);
                mrt.sizeDelta = new Vector2(200f, 200f);
                minimapGo.AddComponent<MinimapPlaceholder>();
            }

            return hudGo.transform;
        }

        void BuildMobileControls()
        {
            var vj = GameplayMobileControlsBootstrap.Build(null);
            if (_player != null) _player.BindJoystick(vj);
            else
                StartCoroutine(BindJoystickNextFrame(vj));
        }

        System.Collections.IEnumerator BindJoystickNextFrame(VirtualJoystick vj)
        {
            yield return null;
            if (_player != null) _player.BindJoystick(vj);
        }

        void BuildWorldUi(Transform hudHost)
        {
            var canvasTf = hudHost.Find("GameHUDCanvas");
            if (canvasTf != null)
                TargetHealthBarView.CreateUnder(canvasTf);

            var winGo = new GameObject("WindowsCanvas");
            var canvas = winGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 40;
            var sc = winGo.AddComponent<CanvasScaler>();
            sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            sc.referenceResolution = new Vector2(1920, 1080);
            sc.matchWidthOrHeight = 0.5f;
            winGo.AddComponent<GraphicRaycaster>();

            var mgr = winGo.AddComponent<UIWindowManager>();

            var inv = BuildPanel(winGo.transform, "InventoryWindow", "INVENTARIO",
                new Vector2(0.5f, 0.5f), new Vector2(520, 420));
            var invW = inv.gameObject.AddComponent<InventoryWindow>();
            inv.gameObject.SetActive(false);

            var st = BuildPanel(winGo.transform, "CharacterStatsWindow", "PERSONAJE",
                new Vector2(0.5f, 0.5f), new Vector2(440, 520));
            var stW = st.gameObject.AddComponent<CharacterStatsWindow>();
            var bgo = new GameObject("Body");
            bgo.transform.SetParent(st, false);
            var brt = bgo.AddComponent<RectTransform>();
            UIBuilder.StretchFill(brt);
            brt.offsetMin = new Vector2(16, 16);
            brt.offsetMax = new Vector2(-16, -48);
            var body = bgo.AddComponent<TextMeshProUGUI>();
            body.fontSize = 16;
            body.color = MuUITheme.TextPrimary;
            stW.AssignBody(body);

            st.gameObject.SetActive(false);

            mgr.AssignWindows(invW, stW);
        }

        static RectTransform BuildPanel(Transform parent, string name, string title, Vector2 anchorCenter, Vector2 size)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            var rt = root.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = anchorCenter;
            rt.pivot = anchorCenter;
            rt.sizeDelta = size;

            var bg = root.AddComponent<Image>();
            bg.color = MuUITheme.PanelBackground;
            root.AddComponent<Outline>().effectColor = MuUITheme.PanelBorderGold;

            var titleGo = new GameObject("Title");
            titleGo.transform.SetParent(root.transform, false);
            var trt = titleGo.AddComponent<RectTransform>();
            trt.anchorMin = new Vector2(0, 1);
            trt.anchorMax = new Vector2(1, 1);
            trt.pivot = new Vector2(0.5f, 1f);
            trt.anchoredPosition = new Vector2(0, -8);
            trt.sizeDelta = new Vector2(0, 32);
            var tmp = titleGo.AddComponent<TextMeshProUGUI>();
            tmp.text = title;
            tmp.fontSize = 20;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = MuUITheme.GoldPrimary;

            return rt;
        }

        void OnEnable()
        {
            EventBus.Subscribe<WorldEvents.PlayerSpawned>(OnPlayerSpawned);
            EventBus.Subscribe<AuthEvents.CharacterListReceived>(OnCharListReceived);
        }

        void OnDisable()
        {
            EventBus.Unsubscribe<WorldEvents.PlayerSpawned>(OnPlayerSpawned);
            EventBus.Unsubscribe<AuthEvents.CharacterListReceived>(OnCharListReceived);
        }

        void OnPlayerSpawned(WorldEvents.PlayerSpawned e)
        {
            if (_playerGo == null) return;
            var pos = new Vector3(e.X, 1f, e.Z);
            _playerGo.transform.position = pos;
            _cam?.SetTarget(_playerGo.transform);
        }

        void OnCharListReceived(AuthEvents.CharacterListReceived e)
        {
            var lp = GameManager.Instance?.LocalPlayer;
            if (lp != null && _hud != null)
                _hud.SetNameLevel(lp.CharacterName, lp.CharacterLevel);
        }

        void SpawnPlayer(Vector3 position)
        {
            _playerGo = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            _playerGo.name = "LocalPlayer";
            _playerGo.tag = GameplayLayers.PlayerTag;
            _playerGo.transform.position = position;
            _playerGo.transform.localScale = new Vector3(0.8f, 1f, 0.8f);

            Destroy(_playerGo.GetComponent<CapsuleCollider>());

            var cc = _playerGo.AddComponent<CharacterController>();
            cc.center = new Vector3(0, 1f, 0);
            cc.height = 2f;
            cc.radius = 0.4f;

            var rend = _playerGo.GetComponent<Renderer>();
            if (rend != null)
            {
                var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                mat.color = new Color(0.45f, 0.32f, 0.12f);
                rend.material = mat;
            }

            _playerGo.AddComponent<PlayerMotor>();
            _playerGo.AddComponent<PlayerInputAggregator>();
            _player = _playerGo.AddComponent<PlayerController>();
            _playerGo.AddComponent<CharacterStats>();
            _playerGo.AddComponent<TargetSelector>();
            _playerGo.AddComponent<TouchTargetPicker>();
            _playerGo.AddComponent<CombatController>();
            var sk = _playerGo.AddComponent<SkillController>();
            sk.SetSkills(SkillBootstrapper.CreateDefaultFive());
            _playerGo.AddComponent<AutoBattleController>();
            _playerGo.AddComponent<InventoryController>();
            _playerGo.AddComponent<PickupCollector>();

            var mainCam = Camera.main;
            if (mainCam != null)
            {
                _cam = mainCam.GetComponent<CameraController>();
                if (_cam == null) _cam = mainCam.gameObject.AddComponent<CameraController>();
                _cam.SetTarget(_playerGo.transform);
            }

            var lp = GameManager.Instance?.LocalPlayer;
            string name = lp?.CharacterName ?? "Héroe";
            int level = lp?.CharacterLevel ?? 1;
            _hud?.SetNameLevel(name, level);
            _hud?.AddChat($"<color=#88BBFF>{name} Lv.{level} — Joystick + AUTO, toca enemigos para objetivo.</color>");

            var joystick = FindFirstObjectByType<VirtualJoystick>();
            if (joystick != null) _player.BindJoystick(joystick);

            var binder = FindFirstObjectByType<GameplayActionBinder>();
            if (binder != null)
            {
                var canvas = _hud != null ? _hud.transform.Find("GameHUDCanvas") : null;
                binder.Wire(
                    _playerGo.GetComponent<CombatController>(),
                    sk,
                    _playerGo.GetComponent<AutoBattleController>(),
                    canvas);
            }

            var invW = FindFirstObjectByType<InventoryWindow>();
            invW?.Bind(_playerGo.GetComponent<InventoryController>());
            var stW = FindFirstObjectByType<CharacterStatsWindow>();
            stW?.Bind(_playerGo.GetComponent<CharacterStats>());
        }

        void SpawnEnemies()
        {
            int layer = LayerMask.NameToLayer(GameplayLayers.EnemyLayerName);
            if (layer < 0) layer = 0;

            for (int i = 0; i < enemyCount; i++)
            {
                float ang = i * (Mathf.PI * 2f / enemyCount);
                var pos = new Vector3(Mathf.Cos(ang) * enemySpawnRadius, 0.9f, Mathf.Sin(ang) * enemySpawnRadius);
                var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                go.name = $"Enemy_{i}";
                go.tag = GameplayLayers.EnemyTag;
                go.layer = layer;
                go.transform.position = pos;
                go.transform.localScale = new Vector3(0.85f, 0.95f, 0.85f);

                var r = go.GetComponent<Renderer>();
                if (r != null)
                {
                    var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                    mat.color = new Color(0.35f, 0.08f, 0.12f);
                    r.material = mat;
                }

                var dmg = go.AddComponent<Damageable>();
                dmg.Configure(120, 120, true);
                go.AddComponent<EnemyStatProfile>();
                go.AddComponent<EnemyAI>();
                var rewards = go.AddComponent<EnemyDeathRewards>();
                rewards.ConfigureDrop(null, true);
            }

            var picker = _playerGo != null ? _playerGo.GetComponent<TouchTargetPicker>() : null;
            if (picker != null)
            {
                int em = LayerMask.GetMask(GameplayLayers.EnemyLayerName);
                LayerMask mask = em != 0 ? (LayerMask)em : enemyLayerMask;
                if (mask.value == 0) mask = ~0;
                picker.SetSelectableLayers(mask);
            }
        }
    }
}
