using UnityEngine;
using MuOnline.Core;

namespace MuOnline.Gameplay.Pickup
{
    /// <summary>Objeto en el mundo que el jugador recoge al acercarse.</summary>
    [RequireComponent(typeof(SphereCollider))]
    public class WorldPickup : MonoBehaviour
    {
        [SerializeField] private ushort itemId = 1;
        [SerializeField] private int count = 1;
        [SerializeField] private string displayName = "Jewel of Bless";
        [SerializeField] private float bobAmplitude = 0.12f;
        [SerializeField] private float bobSpeed = 2.2f;

        Vector3 _basePos;

        void Awake()
        {
            _basePos = transform.position;
            gameObject.tag = GameplayLayers.PickupTag;
            var s = GetComponent<SphereCollider>();
            s.isTrigger = true;
            if (s.radius < 0.4f) s.radius = 0.55f;
        }

        void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag(GameplayLayers.PlayerTag)) return;
            var col = other.GetComponent<PickupCollector>()
                      ?? other.GetComponentInParent<PickupCollector>();
            if (col != null)
                GrantToCollector(col);
        }

        public void Configure(ushort id, int amount, string name)
        {
            itemId = id;
            count = Mathf.Max(1, amount);
            displayName = name;
        }

        /// <summary>Datos placeholder para prototipo offline.</summary>
        public void ConfigureRandomLoot()
        {
            ushort[] ids = { 1, 2, 3, 14 };
            string[] names = { "Bless", "Soul", "Chaos", "Zen Pouch" };
            int i = Random.Range(0, ids.Length);
            Configure(ids[i], Random.Range(1, 3), names[i]);
        }

        void Update()
        {
            float y = Mathf.Sin(Time.time * bobSpeed) * bobAmplitude;
            transform.position = _basePos + new Vector3(0f, y, 0f);
        }

        public void GrantToCollector(PickupCollector collector)
        {
            collector?.GrantItem(itemId, count, displayName);
            Destroy(gameObject);
        }
    }
}
