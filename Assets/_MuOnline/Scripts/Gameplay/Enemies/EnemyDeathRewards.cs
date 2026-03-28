using UnityEngine;
using MuOnline.Core;
using MuOnline.Gameplay.Combat;
using MuOnline.Gameplay.Player;
using MuOnline.Gameplay.Pickup;

namespace MuOnline.Gameplay.Enemies
{
    /// <summary>Otorga EXP/Zen al jugador y opcionalmente spawnea drop.</summary>
    public class EnemyDeathRewards : MonoBehaviour
    {
        [SerializeField] private WorldPickup pickupPrefab;
        [SerializeField] private bool spawnPickupOnDeath = true;

        Damageable _dmg;

        public void ConfigureDrop(WorldPickup prefab, bool spawn)
        {
            pickupPrefab = prefab;
            spawnPickupOnDeath = spawn;
        }

        void Awake() => _dmg = GetComponent<Damageable>();

        void OnEnable()
        {
            if (_dmg != null) _dmg.Died += OnDied;
        }

        void OnDisable()
        {
            if (_dmg != null) _dmg.Died -= OnDied;
        }

        void OnDied(Damageable d)
        {
            var prof = GetComponent<EnemyStatProfile>();
            var player = GameObject.FindGameObjectWithTag(GameplayLayers.PlayerTag);
            if (player != null)
            {
                var st = player.GetComponent<CharacterStats>();
                if (st != null && prof != null)
                {
                    st.AddExperience(prof.ExpReward);
                    st.AddZen(prof.ZenReward);
                }
            }

            if (!spawnPickupOnDeath) return;

            if (pickupPrefab != null)
            {
                var drop = Instantiate(pickupPrefab, transform.position + Vector3.up * 0.35f, Quaternion.identity);
                drop.ConfigureRandomLoot();
                return;
            }

            var go = new GameObject("WorldDrop");
            go.transform.position = transform.position + Vector3.up * 0.35f;
            var sc = go.AddComponent<SphereCollider>();
            sc.isTrigger = true;
            sc.radius = 0.55f;
            var wp = go.AddComponent<WorldPickup>();
            wp.ConfigureRandomLoot();
        }
    }
}
