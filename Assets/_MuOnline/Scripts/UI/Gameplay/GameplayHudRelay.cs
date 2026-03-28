using UnityEngine;
using MuOnline.Core;

namespace MuOnline.UI.Gameplay
{
    /// <summary>Enlaza el bus de eventos locales con el HUD procedural <see cref="GameScreen"/>.</summary>
    public class GameplayHudRelay : MonoBehaviour
    {
        [SerializeField] private GameScreen hud;

        void Awake()
        {
            if (hud == null) hud = GetComponent<GameScreen>();
        }

        void Start()
        {
            var st = FindFirstObjectByType<MuOnline.Gameplay.Player.CharacterStats>();
            if (hud == null || st == null) return;
            hud.SetStats(st.CurrentHp, st.MaxHp, st.CurrentMp, st.MaxMp);
            hud.SetExp(st.Experience, st.ExperienceToNext);
            hud.SetGold(st.Zen);
        }

        void OnEnable()
        {
            EventBus.Subscribe<LocalGameplayEvents.VitalsChanged>(OnVitals);
            EventBus.Subscribe<LocalGameplayEvents.ExpChanged>(OnExp);
            EventBus.Subscribe<LocalGameplayEvents.ZenChanged>(OnZen);
            EventBus.Subscribe<WorldEvents.PlayerStatsReceived>(OnServerStats);
        }

        void OnDisable()
        {
            EventBus.Unsubscribe<LocalGameplayEvents.VitalsChanged>(OnVitals);
            EventBus.Unsubscribe<LocalGameplayEvents.ExpChanged>(OnExp);
            EventBus.Unsubscribe<LocalGameplayEvents.ZenChanged>(OnZen);
            EventBus.Unsubscribe<WorldEvents.PlayerStatsReceived>(OnServerStats);
        }

        void OnVitals(LocalGameplayEvents.VitalsChanged e)
        {
            if (hud == null) return;
            hud.SetStats(e.Hp, e.MaxHp, e.Mp, e.MaxMp);
        }

        void OnExp(LocalGameplayEvents.ExpChanged e)
        {
            if (hud == null) return;
            hud.SetExp(e.Exp, e.ExpMax);
        }

        void OnZen(LocalGameplayEvents.ZenChanged e)
        {
            if (hud == null) return;
            hud.SetGold(e.Zen);
        }

        void OnServerStats(WorldEvents.PlayerStatsReceived e)
        {
            if (hud == null) return;
            hud.SetNameLevel(e.Name, e.Level);
            hud.SetStats(e.Hp, e.MaxHp, e.Mp, e.MaxMp);
            hud.SetGold(e.Zen);
            long maxExp = e.ExpMax > 0 ? e.ExpMax : 1;
            hud.SetExp(e.Exp, maxExp);
        }
    }
}
