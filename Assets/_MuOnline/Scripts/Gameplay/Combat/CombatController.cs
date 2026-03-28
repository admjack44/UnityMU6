using UnityEngine;
using MuOnline.Gameplay;
using MuOnline.Gameplay.Player;
using MuOnline.Gameplay.Targeting;

namespace MuOnline.Gameplay.Combat
{
    /// <summary>Ataques básicos cuerpo a cuerpo / rango corto. Sustituible por validación servidor.</summary>
    public class CombatController : MonoBehaviour
    {
        [SerializeField] private float attackRange = 2.2f;
        [SerializeField] private float attackCooldown = 0.55f;
        [SerializeField] private Transform attackOrigin;

        private CharacterStats _stats;
        private TargetSelector _targeting;
        private float _nextAttackTime;

        void Awake()
        {
            _stats = GetComponent<CharacterStats>();
            _targeting = GetComponent<TargetSelector>();
            if (attackOrigin == null) attackOrigin = transform;
        }

        public bool CanBasicAttackNow() => Time.time >= _nextAttackTime;

        /// <summary>Intenta golpear al objetivo actual. Devuelve true si hubo animación de ataque (cooldown aplicado).</summary>
        public bool TryBasicAttack()
        {
            if (!CanBasicAttackNow()) return false;
            var target = _targeting != null ? _targeting.CurrentTarget : null;
            if (target == null) return false;

            var dmg = target.GetComponent<Damageable>();
            if (dmg == null || dmg.IsDead) return false;

            float dist = Vector3.Distance(attackOrigin.position, target.position);
            if (dist > attackRange) return false;

            int raw = _stats != null ? _stats.RollAttackDamage() : Random.Range(10, 18);
            int mitigated = Mathf.Max(1, raw - EstimateTargetDefense(target));
            dmg.ApplyDamage(mitigated, gameObject, out _);

            _nextAttackTime = Time.time + attackCooldown;
            return true;
        }

        static int EstimateTargetDefense(Transform target)
        {
            var st = target.GetComponent<EnemyStatProfile>();
            return st != null ? st.Defense : 0;
        }

        /// <summary>Utilidad para auto-batalla y depuración.</summary>
        public float DistanceToCurrentTarget()
        {
            var t = _targeting != null ? _targeting.CurrentTarget : null;
            if (t == null) return float.MaxValue;
            return Vector3.Distance(attackOrigin.position, t.position);
        }

        public float AttackRange => attackRange;
    }
}
