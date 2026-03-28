using UnityEngine;
using MuOnline.Core;
using MuOnline.Gameplay.Combat;
using MuOnline.Gameplay.Player;
using MuOnline.Gameplay.Targeting;

namespace MuOnline.Gameplay.Skills
{
    /// <summary>Ejecución local de skills con cooldown y coste de maná.</summary>
    public class SkillController : MonoBehaviour
    {
        [SerializeField] private SkillDefinition[] skills = new SkillDefinition[5];
        [SerializeField] private CharacterStats stats;
        [SerializeField] private TargetSelector targeting;
        [SerializeField] private Transform castOrigin;

        private float[] _nextReady;

        void Awake()
        {
            if (stats == null) stats = GetComponent<CharacterStats>();
            if (targeting == null) targeting = GetComponent<TargetSelector>();
            if (castOrigin == null) castOrigin = transform;
            RebuildCooldownArray();
        }

        /// <summary>Asignación en runtime (bootstrap de escena).</summary>
        public void SetSkills(SkillDefinition[] defs)
        {
            skills = defs;
            RebuildCooldownArray();
        }

        void RebuildCooldownArray()
        {
            int n = skills != null ? skills.Length : 0;
            _nextReady = new float[n];
        }

        public int SlotCount => skills.Length;

        public bool TryUseSkill(int index)
        {
            if (skills == null || index < 0 || index >= skills.Length) return false;
            var def = skills[index];
            if (def == null) return false;
            if (Time.time < _nextReady[index]) return false;

            var target = targeting != null ? targeting.CurrentTarget : null;
            if (target == null) return false;

            var dmg = target.GetComponent<Damageable>();
            if (dmg == null || dmg.IsDead) return false;

            float dist = Vector3.Distance(castOrigin.position, target.position);
            if (dist > def.Range) return false;

            if (stats != null && !stats.TryConsumeMp(def.MpCost)) return false;

            int raw = Random.Range(def.PowerMin, def.PowerMax + 1);
            int mit = Mathf.Max(1, raw - GetDefense(target));
            dmg.ApplyDamage(mit, gameObject, out _);

            _nextReady[index] = Time.time + def.CooldownSeconds;
            EventBus.Publish(new LocalGameplayEvents.SkillUsedLocal
            {
                SkillIndex = index,
                SkillId = def.SkillId
            });
            return true;
        }

        public float CooldownRemaining(int index)
        {
            if (_nextReady == null || index < 0 || index >= _nextReady.Length) return 0f;
            return Mathf.Max(0f, _nextReady[index] - Time.time);
        }

        public SkillDefinition GetDefinition(int index)
        {
            if (skills == null || index < 0 || index >= skills.Length) return null;
            return skills[index];
        }

        static int GetDefense(Transform t)
        {
            var p = t.GetComponent<EnemyStatProfile>();
            return p != null ? p.Defense : 0;
        }
    }
}
