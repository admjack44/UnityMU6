using System;
using UnityEngine;
using MuOnline.Core;

namespace MuOnline.Gameplay.Combat
{
    /// <summary>Componente genérico HP. Enemigos, jugador y bosses comparten la misma API.</summary>
    public class Damageable : MonoBehaviour
    {
        [SerializeField] private int maxHp = 100;
        [SerializeField] private int currentHp = 100;
        [SerializeField] private bool destroyOnDeath = true;
        [SerializeField] private float deathDestroyDelay = 1.5f;

        /// <summary>Invocado al morir (antes de destruir el objeto si aplica).</summary>
        public event Action<Damageable> Died;

        public int MaxHp => maxHp;
        public int CurrentHp => currentHp;
        public bool IsDead => currentHp <= 0;
        public Transform CachedTransform => transform;

        void Awake()
        {
            currentHp = Mathf.Clamp(currentHp, 0, maxHp);
        }

        public void Configure(int hp, int maximum, bool canDestroy = true)
        {
            maxHp     = Mathf.Max(1, maximum);
            currentHp = Mathf.Clamp(hp, 0, maxHp);
            destroyOnDeath = canDestroy;
        }

        /// <summary>
        /// Aplica daño. <paramref name="source"/> puede ser null (DOT / ambiente).
        /// Devuelve true si el golpe fue aplicado (no muerto previo).
        /// </summary>
        public bool ApplyDamage(int amount, GameObject source, out bool killed)
        {
            killed = false;
            if (IsDead || amount <= 0) return false;

            currentHp = Mathf.Max(0, currentHp - amount);
            Vector3 pos = transform.position + Vector3.up * 1.6f;
            EventBus.Publish(new LocalGameplayEvents.DamageFloaterRequested
            {
                WorldPosition   = pos,
                Amount          = amount,
                IsCritical      = false,
                IsPlayerSource  = source != null && source.CompareTag(GameplayLayers.PlayerTag)
            });

            if (currentHp <= 0)
            {
                killed = true;
                Died?.Invoke(this);
                if (destroyOnDeath)
                    Destroy(gameObject, deathDestroyDelay);
            }

            return true;
        }

        public void Heal(int amount)
        {
            if (IsDead || amount <= 0) return;
            currentHp = Mathf.Min(maxHp, currentHp + amount);
        }

        /// <summary>Sincronización futura con servidor (autoritativo).</summary>
        public void SetAuthoritativeHp(int hp, int max)
        {
            maxHp = Mathf.Max(1, max);
            currentHp = Mathf.Clamp(hp, 0, maxHp);
        }
    }
}
