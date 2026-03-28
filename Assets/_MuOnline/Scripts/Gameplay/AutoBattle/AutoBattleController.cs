using UnityEngine;
using MuOnline.Core;
using MuOnline.Gameplay.Combat;
using MuOnline.Gameplay.Player;
using MuOnline.Gameplay.Targeting;

namespace MuOnline.Gameplay.AutoBattle
{
    /// <summary>Base de auto-play: busca enemigo, acerca al rango y ataca. Ampliable con skills.</summary>
    public class AutoBattleController : MonoBehaviour
    {
        [SerializeField] private bool enabledAuto;
        [SerializeField] private float scanRadius = 14f;
        [SerializeField] private LayerMask enemyMask = ~0;
        [SerializeField] private PlayerMotor motor;
        [SerializeField] private CombatController combat;
        [SerializeField] private TargetSelector targeting;
        [SerializeField] private CharacterStats stats;

        void Awake()
        {
            if (motor == null) motor = GetComponent<PlayerMotor>();
            if (combat == null) combat = GetComponent<CombatController>();
            if (targeting == null) targeting = GetComponent<TargetSelector>();
            if (stats == null) stats = GetComponent<CharacterStats>();
        }

        public bool IsAutoEnabled => enabledAuto;

        public void SetAuto(bool on)
        {
            enabledAuto = on;
            EventBus.Publish(new LocalGameplayEvents.AutoBattleToggled { Enabled = on });
        }

        public void ToggleAuto() => SetAuto(!enabledAuto);

        void Update()
        {
            if (!enabledAuto || motor == null || combat == null || targeting == null) return;
            if (stats != null && stats.CurrentHp <= 0) return;

            var target = targeting.CurrentTarget;
            if (target == null || !IsAliveEnemy(target))
            {
                var t = targeting.FindNearestEnemy(scanRadius, enemyMask);
                targeting.SetTarget(t);
                target = t;
            }

            if (target == null) return;

            float dist = combat.DistanceToCurrentTarget();
            if (dist > combat.AttackRange * 0.95f)
            {
                Vector3 dir = target.position - transform.position;
                dir.y = 0f;
                if (dir.sqrMagnitude > 0.0001f)
                    motor.MoveWorldDirection(dir.normalized, Time.deltaTime);
            }
            else
            {
                motor.Move(Vector2.zero, Time.deltaTime);
                combat.TryBasicAttack();
            }
        }

        static bool IsAliveEnemy(Transform t)
        {
            var d = t.GetComponent<Damageable>();
            return d != null && !d.IsDead;
        }
    }
}
