using UnityEngine;
using MuOnline.Core;
using MuOnline.Gameplay.Inventory;

namespace MuOnline.Gameplay.Pickup
{
    /// <summary>Puente inventario ↔ drops. <see cref="WorldPickup"/> llama a <see cref="GrantItem"/>.</summary>
    public class PickupCollector : MonoBehaviour
    {
        [SerializeField] private InventoryController inventory;

        void Awake()
        {
            if (inventory == null) inventory = GetComponent<InventoryController>();
        }

        public void GrantItem(ushort itemId, int count, string displayName)
        {
            inventory?.AddItem(itemId, count, displayName);
            EventBus.Publish(new InventoryEvents.ItemPickedUp
            {
                Item = new ItemInfo
                {
                    ItemId = itemId,
                    Level = 0,
                    Slot = 0,
                    Name = displayName
                }
            });
        }
    }
}
