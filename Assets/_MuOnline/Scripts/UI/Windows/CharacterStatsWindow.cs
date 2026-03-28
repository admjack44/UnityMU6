using TMPro;
using UnityEngine;
using UnityEngine.UI;
using MuOnline.Core;
using MuOnline.Gameplay.Player;
using MuOnline.UI;

namespace MuOnline.UI.Windows
{
    public class CharacterStatsWindow : MonoBehaviour
    {
        [SerializeField] private CharacterStats stats;
        [SerializeField] private TextMeshProUGUI body;

        void OnEnable()
        {
            EventBus.Subscribe<LocalGameplayEvents.VitalsChanged>(OnVital);
            EventBus.Subscribe<LocalGameplayEvents.ExpChanged>(OnExp);
            Refresh();
        }

        void OnDisable()
        {
            EventBus.Unsubscribe<LocalGameplayEvents.VitalsChanged>(OnVital);
            EventBus.Unsubscribe<LocalGameplayEvents.ExpChanged>(OnExp);
        }

        void OnVital(LocalGameplayEvents.VitalsChanged e) => Refresh();
        void OnExp(LocalGameplayEvents.ExpChanged e) => Refresh();

        public void Bind(CharacterStats s) => stats = s;

        public void AssignBody(TextMeshProUGUI text) => body = text;

        public void Refresh()
        {
            if (stats == null) return;
            if (body == null) body = GetComponentInChildren<TextMeshProUGUI>();
            if (body == null) return;

            var b = stats.BaseStats;
            body.text =
                $"<b>Level</b> {b.Level}\n" +
                $"<b>HP</b> {stats.CurrentHp} / {stats.MaxHp}\n" +
                $"<b>MP</b> {stats.CurrentMp} / {stats.MaxMp}\n" +
                $"<b>STR</b> {b.Strength}   <b>AGI</b> {b.Agility}\n" +
                $"<b>VIT</b> {b.Vitality}   <b>ENE</b> {b.Energy}\n" +
                $"<b>ATK</b> {b.AttackMin}-{b.AttackMax}   <b>DEF</b> {b.Defense}\n" +
                $"<b>EXP</b> {stats.Experience} / {stats.ExperienceToNext}\n" +
                $"<b>Zen</b> {stats.Zen:N0}";
        }
    }
}
