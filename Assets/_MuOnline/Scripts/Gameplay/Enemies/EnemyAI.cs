using UnityEngine;
using MuOnline.Core;
using MuOnline.Gameplay.Combat;
using MuOnline.Gameplay.Player;

namespace MuOnline.Gameplay.Enemies
{
    /// <summary>IA simple: persigue al jugador y ataca en rango (sin NavMesh).</summary>
    public class EnemyAI : MonoBehaviour
    {
        [SerializeField] private float moveSpeed = 3.2f;
        [SerializeField] private float stopDistance = 1.35f;
        [SerializeField] private float attackRange = 1.8f;
        [SerializeField] private float attackCooldown = 1.4f;
        [SerializeField] private int attackDamageMin = 6;
        [SerializeField] private int attackDamageMax = 14;

        private Damageable _self;
        private Transform _player;
        private float _nextAttack;

        void Awake() => _self = GetComponent<Damageable>();

        void Start()
        {
            var go = GameObject.FindGameObjectWithTag(GameplayLayers.PlayerTag);
            if (go != null) _player = go.transform;
        }

        void Update()
        {
            if (_self != null && _self.IsDead) return;
            if (_player == null)
            {
                var g = GameObject.FindGameObjectWithTag(GameplayLayers.PlayerTag);
                if (g != null) _player = g.transform;
            }

            if (_player == null) return;

            Vector3 to = _player.position - transform.position;
            to.y = 0f;
            float dist = to.magnitude;

            if (dist > stopDistance && to.sqrMagnitude > 0.0001f)
            {
                var dir = to.normalized;
                transform.position += dir * (moveSpeed * Time.deltaTime);
                transform.rotation = Quaternion.Slerp(transform.rotation,
                    Quaternion.LookRotation(dir), Time.deltaTime * 8f);
            }

            if (dist <= attackRange && Time.time >= _nextAttack)
                TryHitPlayer();
        }

        void TryHitPlayer()
        {
            var stats = _player.GetComponent<CharacterStats>();
            if (stats == null) return;

            int dmg = Random.Range(attackDamageMin, attackDamageMax + 1);
            stats.ApplyLocalDamage(dmg);
            _nextAttack = Time.time + attackCooldown;
        }
    }
}
