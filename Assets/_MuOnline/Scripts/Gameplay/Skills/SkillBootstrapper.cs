using UnityEngine;

namespace MuOnline.Gameplay.Skills
{
    /// <summary>Rellena slots de <see cref="SkillController"/> con skills temporales offline.</summary>
    public static class SkillBootstrapper
    {
        public static SkillDefinition[] CreateDefaultFive()
        {
            var a = new SkillDefinition[5];
            a[0] = ScriptableObject.CreateInstance<SkillDefinition>();
            a[0].Bootstrap(10, "Cyclone", 2.5f, 12, 5f, 24, 38);
            a[1] = ScriptableObject.CreateInstance<SkillDefinition>();
            a[1].Bootstrap(11, "Lunge", 3f, 18, 6f, 30, 48);
            a[2] = ScriptableObject.CreateInstance<SkillDefinition>();
            a[2].Bootstrap(12, "Burst", 4f, 22, 7f, 35, 55);
            a[3] = ScriptableObject.CreateInstance<SkillDefinition>();
            a[3].Bootstrap(13, "Shock", 5f, 28, 8f, 40, 62);
            a[4] = ScriptableObject.CreateInstance<SkillDefinition>();
            a[4].Bootstrap(14, "Rage", 8f, 40, 9f, 55, 80);
            return a;
        }
    }
}
