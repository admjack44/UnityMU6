using UnityEngine;
using MuOnline.Core;
using MuOnline.Gameplay.Data;

namespace MuOnline.Gameplay.Player
{
    /// <summary>Estadísticas runtime del personaje local. El servidor podrá empujar valores vía EventBus.</summary>
    public class CharacterStats : MonoBehaviour
    {
        [SerializeField] private StatBlock baseStats = StatBlock.CreateHeroDefault(1);
        [SerializeField] private int currentHp;
        [SerializeField] private int currentMp;
        [SerializeField] private long experience;
        [SerializeField] private long experienceToNext = 1000;
        [SerializeField] private long zen;

        public StatBlock BaseStats => baseStats;
        public int CurrentHp => currentHp;
        public int CurrentMp => currentMp;
        public long Experience => experience;
        public long ExperienceToNext => experienceToNext;
        public long Zen => zen;

        public int MaxHp => baseStats.MaxHp;
        public int MaxMp => baseStats.MaxMp;

        void Awake()
        {
            currentHp = baseStats.MaxHp;
            currentMp = baseStats.MaxMp;
            PublishVitals();
            PublishExp();
        }

        void OnEnable()
        {
            EventBus.Subscribe<WorldEvents.PlayerStatsReceived>(OnServerStats);
        }

        void OnDisable()
        {
            EventBus.Unsubscribe<WorldEvents.PlayerStatsReceived>(OnServerStats);
        }

        void OnServerStats(WorldEvents.PlayerStatsReceived e)
        {
            baseStats.MaxHp = Mathf.Max(1, e.MaxHp);
            baseStats.MaxMp = Mathf.Max(1, e.MaxMp);
            currentHp = Mathf.Clamp(e.Hp, 0, baseStats.MaxHp);
            currentMp = Mathf.Clamp(e.Mp, 0, baseStats.MaxMp);
            zen = e.Zen;
            experience = e.Exp;
            experienceToNext = e.ExpMax > 0 ? e.ExpMax : 1;
            baseStats.Level = e.Level;
            PublishVitals();
            PublishExp();
        }

        public void ApplyLocalDamage(int amount)
        {
            if (amount <= 0) return;
            currentHp = Mathf.Max(0, currentHp - amount);
            PublishVitals();
        }

        public bool TryConsumeMp(int mp)
        {
            if (currentMp < mp) return false;
            currentMp -= mp;
            PublishVitals();
            return true;
        }

        public void RestoreVitalsFull()
        {
            currentHp = baseStats.MaxHp;
            currentMp = baseStats.MaxMp;
            PublishVitals();
        }

        public void AddExperience(long amount)
        {
            if (amount <= 0) return;
            experience += amount;
            while (experienceToNext > 0 && experience >= experienceToNext)
            {
                experience -= experienceToNext;
                baseStats.Level++;
                RecalculateDerivedStats();
                experienceToNext = NextLevelExp(baseStats.Level);
                currentHp = baseStats.MaxHp;
                currentMp = baseStats.MaxMp;
            }

            PublishExp();
            PublishVitals();
        }

        public void AddZen(long amount)
        {
            zen += amount;
            EventBus.Publish(new LocalGameplayEvents.ZenChanged { Zen = zen });
        }

        void RecalculateDerivedStats()
        {
            // Regla simple offline; servidor reemplazará con datos reales.
            int lv = baseStats.Level;
            baseStats.MaxHp = 110 + baseStats.Vitality * 2 + lv * 8;
            baseStats.MaxMp = 40 + baseStats.Energy * 2 + lv * 4;
            baseStats.AttackMin = 15 + baseStats.Strength / 4 + lv;
            baseStats.AttackMax = 22 + baseStats.Strength / 3 + lv + 2;
            baseStats.Defense = 8 + baseStats.Agility / 5 + lv / 2;
        }

        static long NextLevelExp(int level) => 1000 + (level - 1) * 350;

        void PublishVitals()
        {
            EventBus.Publish(new LocalGameplayEvents.VitalsChanged
            {
                Hp = currentHp, MaxHp = baseStats.MaxHp, Mp = currentMp, MaxMp = baseStats.MaxMp
            });
        }

        void PublishExp()
        {
            EventBus.Publish(new LocalGameplayEvents.ExpChanged
            {
                Exp = experience, ExpMax = experienceToNext, Level = baseStats.Level
            });
        }

        public int RollAttackDamage()
        {
            return Random.Range(baseStats.AttackMin, baseStats.AttackMax + 1);
        }
    }
}
