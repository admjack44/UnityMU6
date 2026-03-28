using MuOnline.Network.Protocol;

namespace MuOnline.Network.Packets
{
    /// <summary>
    /// Fábrica de paquetes que el cliente envía al servidor.
    /// Uso: NetworkClient.Instance.Send(ClientPackets.Login("user","pass"));
    /// </summary>
    public static class ClientPackets
    {
        // ── Autenticación ────────────────────────────────────────────────────

        public static byte[] Login(string account, string password)
        {
            return new PacketWriter(PacketType.C1, PacketIds.C_LOGIN_REQUEST, 32)
                .WriteByte(0x00)         // sub-command login
                .WriteString(account, 10)
                .WriteString(password, 10)
                .WriteByte(0)            // cliente version[0]
                .WriteByte(0)            // cliente version[1]
                .WriteByte(0)            // cliente version[2]
                .WriteByte(0)            // cliente version[3]
                .WriteByte(0)            // cliente version[4]
                .Build();
        }

        public static byte[] RequestCharacterList()
        {
            return new PacketWriter(PacketType.C1, PacketIds.C_CHARACTER_LIST, 4)
                .WriteByte(PacketIds.CharSubCmd.LIST)
                .Build();
        }

        public static byte[] SelectCharacter(string name)
        {
            return new PacketWriter(PacketType.C1, PacketIds.C_CHARACTER_SELECT, 16)
                .WriteByte(PacketIds.CharSubCmd.SELECT)
                .WriteString(name, 10)
                .Build();
        }

        public static byte[] CreateCharacter(string name, Core.CharacterClass charClass)
        {
            return new PacketWriter(PacketType.C1, PacketIds.C_CHARACTER_CREATE, 16)
                .WriteByte(PacketIds.CharSubCmd.CREATE)
                .WriteString(name, 10)
                .WriteByte((byte)charClass)
                .Build();
        }

        public static byte[] DeleteCharacter(string name, string pin)
        {
            return new PacketWriter(PacketType.C1, PacketIds.C_CHARACTER_DELETE, 20)
                .WriteByte(PacketIds.CharSubCmd.DELETE)
                .WriteString(name, 10)
                .WriteString(pin, 7)
                .Build();
        }

        // ── Movimiento ───────────────────────────────────────────────────────

        public static byte[] Walk(byte x, byte z, byte direction, byte[] path = null)
        {
            int pathLen = path?.Length ?? 0;
            var w = new PacketWriter(PacketType.C1, PacketIds.C_WALK, 8 + pathLen)
                .WriteByte(x)
                .WriteByte(z)
                .WriteByte(direction)
                .WriteByte((byte)pathLen);

            if (path != null)
                w.WriteBytes(path);

            return w.Build();
        }

        // ── Combate ──────────────────────────────────────────────────────────

        public static byte[] Attack(ushort targetId, byte attackType = 0)
        {
            return new PacketWriter(PacketType.C1, PacketIds.C_ATTACK, 8)
                .WriteUShort(targetId)
                .WriteByte(attackType)
                .Build();
        }

        public static byte[] UseSkill(ushort skillId, ushort targetId)
        {
            return new PacketWriter(PacketType.C1, PacketIds.C_SKILL_USE, 8)
                .WriteUShort(skillId)
                .WriteUShort(targetId)
                .Build();
        }

        // ── Items ────────────────────────────────────────────────────────────

        public static byte[] PickUpItem(ushort itemEntityId)
        {
            return new PacketWriter(PacketType.C1, PacketIds.C_ITEM_PICKUP, 6)
                .WriteUShort(itemEntityId)
                .Build();
        }

        public static byte[] DropItem(byte inventorySlot, byte x, byte z)
        {
            return new PacketWriter(PacketType.C1, PacketIds.C_ITEM_DROP, 6)
                .WriteByte(inventorySlot)
                .WriteByte(x)
                .WriteByte(z)
                .Build();
        }

        public static byte[] EquipItem(byte sourceSlot, byte targetSlot)
        {
            return new PacketWriter(PacketType.C1, PacketIds.C_ITEM_EQUIP, 6)
                .WriteByte(sourceSlot)
                .WriteByte(targetSlot)
                .Build();
        }

        // ── Registro ────────────────────────────────────────────────────────

        public static byte[] Register(string account, string password)
        {
            return new PacketWriter(PacketType.C1, PacketIds.C_REGISTER_ACCOUNT, 28)
                .WriteString(account, 10)
                .WriteString(password, 10)
                .Build();
        }

        // ── Chat ─────────────────────────────────────────────────────────────

        public static byte[] ChatMessage(string message)
        {
            int maxLen = 60;
            return new PacketWriter(PacketType.C1, PacketIds.C_CHAT_MESSAGE, maxLen + 4)
                .WriteString(message, maxLen)
                .Build();
        }

        // ── Sistema ──────────────────────────────────────────────────────────

        public static byte[] Ping()
        {
            return new PacketWriter(PacketType.C1, PacketIds.C_PING, 2)
                .Build();
        }

        public static byte[] AddStat(byte statType)
        {
            return new PacketWriter(PacketType.C1, PacketIds.C_ADD_STAT, 4)
                .WriteByte(statType)
                .Build();
        }
    }
}
