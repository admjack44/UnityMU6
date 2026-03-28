using System;

namespace MuOnline.Gameplay.Inventory
{
    [Serializable]
    public struct InventorySlot
    {
        public ushort ItemId;
        public int Count;
        public string DisplayName;
        public bool IsEmpty => ItemId == 0 || Count <= 0;
    }
}
