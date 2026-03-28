using System;
using UnityEngine;

namespace MuOnline.Gameplay.Data
{
    /// <summary>Snapshot de atributos RPG. Ampliable con DEF, velocidad de ataque, etc.</summary>
    [Serializable]
    public struct StatBlock
    {
        public int Level;
        public int Strength;
        public int Agility;
        public int Vitality;
        public int Energy;

        public int MaxHp;
        public int MaxMp;
        public int AttackMin;
        public int AttackMax;
        public int Defense;

        public static StatBlock CreateHeroDefault(int level = 1)
        {
            int vit = 28, ene = 20, str = 32, agi = 25;
            int maxHp = 110 + vit * 2 + level * 8;
            int maxMp = 40 + ene * 2 + level * 4;
            return new StatBlock
            {
                Level      = level,
                Strength   = str,
                Agility    = agi,
                Vitality   = vit,
                Energy     = ene,
                MaxHp      = maxHp,
                MaxMp      = maxMp,
                AttackMin  = 15 + str / 4,
                AttackMax  = 22 + str / 3,
                Defense    = 8 + agi / 5
            };
        }
    }
}
