using UnityEngine;
using MuOnline.Core;

namespace MuOnline.Gameplay.Inventory
{
    /// <summary>Inventario local (cliente). El servidor validará en builds online.</summary>
    public class InventoryController : MonoBehaviour
    {
        [SerializeField] private int slotCount = 32;
        [SerializeField] private InventorySlot[] slots;

        public int SlotCount => slotCount;
        public InventorySlot[] Slots => slots;

        void Awake()
        {
            if (slots == null || slots.Length != slotCount)
                slots = new InventorySlot[slotCount];
        }

        public bool AddItem(ushort itemId, int count, string displayName)
        {
            if (itemId == 0 || count <= 0) return false;

            for (int i = 0; i < slots.Length; i++)
            {
                if (!slots[i].IsEmpty && slots[i].ItemId == itemId)
                {
                    var s = slots[i];
                    s.Count += count;
                    slots[i] = s;
                    Publish();
                    return true;
                }
            }

            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i].IsEmpty)
                {
                    slots[i] = new InventorySlot
                    {
                        ItemId = itemId,
                        Count = count,
                        DisplayName = displayName
                    };
                    Publish();
                    return true;
                }
            }

            return false;
        }

        void Publish()
        {
            EventBus.Publish(new LocalGameplayEvents.InventoryUpdated { SlotCount = slotCount });
        }
    }
}
