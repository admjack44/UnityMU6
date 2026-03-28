using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using MuOnline.Gameplay;
using MuOnline.Network;

namespace MuOnline.Core
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Configuration")]
        [SerializeField] private string serverHost = "127.0.0.1";
        [SerializeField] private int serverPort = 44405;

        public string ServerHost => serverHost;
        public int    ServerPort => serverPort;

        public GameState CurrentState { get; private set; } = GameState.Boot;
        public PlayerSession LocalPlayer { get; private set; }
        /// <summary>Cuenta usada en el último login (para LocalPlayer al elegir personaje).</summary>
        public string CurrentAccountName { get; private set; } = "";

        /// <summary>Último snapshot de stats del servidor (por si el HUD aún no estaba en escena).</summary>
        public WorldEvents.PlayerStatsReceived LastPlayerStats { get; private set; }
        public bool HasPlayerStats { get; private set; }

        public static event Action<GameState, GameState> OnStateChanged;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            Application.targetFrameRate = 60;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
        }

        void Start()
        {
            EventBus.Subscribe<NetworkEvents.Connected>(OnConnected);
            EventBus.Subscribe<NetworkEvents.Disconnected>(OnDisconnected);
            EventBus.Subscribe<AuthEvents.LoginSuccess>(OnLoginSuccessAccount);
            EventBus.Subscribe<WorldEvents.PlayerStatsReceived>(OnPlayerStatsReceived);
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        void OnDestroy()
        {
            EventBus.Unsubscribe<NetworkEvents.Connected>(OnConnected);
            EventBus.Unsubscribe<NetworkEvents.Disconnected>(OnDisconnected);
            EventBus.Unsubscribe<AuthEvents.LoginSuccess>(OnLoginSuccessAccount);
            EventBus.Unsubscribe<WorldEvents.PlayerStatsReceived>(OnPlayerStatsReceived);
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        void OnLoginSuccessAccount(AuthEvents.LoginSuccess e)
            => CurrentAccountName = e.AccountName ?? "";

        void OnPlayerStatsReceived(WorldEvents.PlayerStatsReceived e)
        {
            LastPlayerStats = e;
            HasPlayerStats  = true;
        }

        /// <summary>Evita que el HUD muestre stats de una sesión anterior al pulsar Entrar.</summary>
        public void ClearCachedStats() => HasPlayerStats = false;

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // Al entrar a la escena Game, instanciar el setup automáticamente
            // si no existe ya en la escena
            if (scene.name == "Game" &&
                FindFirstObjectByType<GameSceneSetup>() == null)
            {
                var go = new GameObject("GameSceneSetup");
                go.AddComponent<GameSceneSetup>();
            }
        }

        public void ConnectToServer()
        {
            NetworkClient.Instance.Connect(serverHost, serverPort);
        }

        public void SetLocalPlayer(PlayerSession session)
        {
            LocalPlayer = session;
        }

        public void TransitionTo(GameState newState)
        {
            var previous = CurrentState;
            CurrentState = newState;
            OnStateChanged?.Invoke(previous, newState);

            switch (newState)
            {
                case GameState.Login:
                    if (SceneTransitionManager.Instance != null)
                        SceneTransitionManager.Instance.LoadScene("Login");
                    else
                        SceneManager.LoadScene("Login");
                    break;
                case GameState.CharacterSelect:
                    if (SceneTransitionManager.Instance != null)
                        SceneTransitionManager.Instance.LoadScene("CharacterSelect");
                    else
                        SceneManager.LoadScene("CharacterSelect");
                    break;
                case GameState.InGame:
                    if (SceneTransitionManager.Instance != null)
                        SceneTransitionManager.Instance.LoadScene("Game");
                    else
                        SceneManager.LoadScene("Game");
                    break;
            }
        }

        private void OnConnected(NetworkEvents.Connected evt)
        {
            Debug.Log("[GameManager] Conectado al servidor.");
            TransitionTo(GameState.Login);
        }

        private void OnDisconnected(NetworkEvents.Disconnected evt)
        {
            Debug.LogWarning("[GameManager] Desconectado del servidor.");
            LocalPlayer   = null;
            HasPlayerStats = false;
            TransitionTo(GameState.Disconnected);
        }
    }

    public enum GameState
    {
        Boot,
        Connecting,
        Login,
        CharacterSelect,
        InGame,
        Disconnected
    }

    [Serializable]
    public class PlayerSession
    {
        public string AccountName;
        public string CharacterName;
        public int CharacterLevel;
        public CharacterClass CharacterClass;
        public int MapId;
        public float PosX;
        public float PosZ;
    }

    public enum CharacterClass : byte
    {
        DarkKnight = 0,
        DarkWizard = 1,
        FairyElf = 2,
        MagicGladiator = 3,
        DarkLord = 4,
        Summoner = 5,
        RageFighter = 6
    }
}
