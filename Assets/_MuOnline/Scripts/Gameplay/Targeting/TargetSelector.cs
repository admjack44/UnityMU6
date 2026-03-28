using UnityEngine;
using MuOnline.Core;
using MuOnline.Gameplay.Combat;

namespace MuOnline.Gameplay.Targeting
{
    /// <summary>Mantiene el objetivo seleccionado y notifica por EventBus.</summary>
    public class TargetSelector : MonoBehaviour
    {
        [SerializeField] private Transform currentTarget;

        public Transform CurrentTarget => currentTarget;

        public void SetTarget(Transform t)
        {
            if (currentTarget == t) return;
            currentTarget = t;
            EventBus.Publish(new LocalGameplayEvents.TargetChanged { TargetTransform = currentTarget });
        }

        public void ClearTarget() => SetTarget(null);

        void LateUpdate()
        {
            if (currentTarget == null) return;
            var d = currentTarget.GetComponent<Damageable>();
            if (d != null && d.IsDead) ClearTarget();
        }

        /// <summary>El más cercano con tag Enemy dentro del radio.</summary>
        public Transform FindNearestEnemy(float radius, LayerMask enemyMask)
        {
            var cols = Physics.OverlapSphere(transform.position, radius, enemyMask, QueryTriggerInteraction.Ignore);
            Transform best = null;
            float bestD = float.MaxValue;
            foreach (var c in cols)
            {
                if (!c.CompareTag(GameplayLayers.EnemyTag)) continue;
                var d = (c.transform.position - transform.position).sqrMagnitude;
                if (d < bestD)
                {
                    bestD = d;
                    best = c.transform;
                }
            }

            return best;
        }
    }
}
