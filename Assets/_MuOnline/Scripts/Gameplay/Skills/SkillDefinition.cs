using UnityEngine;

namespace MuOnline.Gameplay.Skills
{
    [CreateAssetMenu(menuName = "MU/Skill Definition", fileName = "SkillDefinition")]
    public class SkillDefinition : ScriptableObject
    {
        [SerializeField] private ushort skillId;
        [SerializeField] private string displayName = "Skill";
        [SerializeField] private float cooldownSeconds = 3f;
        [SerializeField] private int mpCost = 15;
        [SerializeField] private float range = 6f;
        [SerializeField] private int powerMin = 30;
        [SerializeField] private int powerMax = 48;

        public ushort SkillId => skillId;
        public string DisplayName => displayName;
        public float CooldownSeconds => cooldownSeconds;
        public int MpCost => mpCost;
        public float Range => range;
        public int PowerMin => powerMin;
        public int PowerMax => powerMax;

        /// <summary>Permite rellenar datos en runtime (bootstrap sin assets en disco).</summary>
        public void Bootstrap(ushort id, string name, float cd, int mp, float rng, int pMin, int pMax)
        {
            skillId = id;
            displayName = name;
            cooldownSeconds = cd;
            mpCost = mp;
            range = rng;
            powerMin = pMin;
            powerMax = pMax;
        }
    }
}
