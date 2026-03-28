namespace MuOnline.Network.Protocol
{
    /// <summary>
    /// IDs de paquetes del protocolo MU Online.
    /// Convención: C_ = Client→Server  |  S_ = Server→Client
    /// </summary>
    public static class PacketIds
    {
        // ── Autenticación ────────────────────────────────────────────────────
        public const byte C_LOGIN_REQUEST        = 0xF1;
        public const byte S_LOGIN_RESULT         = 0xF1;

        public const byte C_CHARACTER_LIST       = 0xF3;
        public const byte S_CHARACTER_LIST       = 0xF3;

        public const byte C_CHARACTER_SELECT     = 0xF3;
        public const byte S_CHARACTER_SELECT     = 0xF3;

        public const byte C_CHARACTER_CREATE     = 0xF3;
        public const byte S_CHARACTER_CREATE     = 0xF3;

        public const byte C_CHARACTER_DELETE     = 0xF3;
        public const byte S_CHARACTER_DELETE     = 0xF3;

        // ── Mundo / Movimiento ───────────────────────────────────────────────
        public const byte C_WALK                 = 0xD4;
        public const byte S_ENTITY_MOVE          = 0xD4;

        public const byte S_TELEPORT             = 0x1C;
        public const byte S_MAP_ENTER            = 0x1C;

        public const byte S_ENTITY_LIST          = 0x12; // lista de entidades del mapa
        public const byte S_ENTITY_SPAWN         = 0x13;
        public const byte S_ENTITY_DESPAWN       = 0x14;

        // ── Combate ──────────────────────────────────────────────────────────
        public const byte C_ATTACK               = 0x11;
        public const byte S_ATTACK_RESULT        = 0x11;

        public const byte C_SKILL_USE            = 0x19;
        public const byte S_SKILL_RESULT         = 0x19;

        public const byte S_PLAYER_DIED          = 0x15;
        public const byte S_ENTITY_DIED          = 0x15;

        // ── Inventario / Items ───────────────────────────────────────────────
        public const byte S_INVENTORY_LIST       = 0x20;
        public const byte C_ITEM_PICKUP          = 0x22;
        public const byte S_ITEM_PICKUP          = 0x22;
        public const byte C_ITEM_DROP            = 0x23;
        public const byte S_ITEM_DROP            = 0x23;
        public const byte C_ITEM_EQUIP           = 0x25;
        public const byte S_ITEM_EQUIP           = 0x25;

        // ── Stats / Nivel ────────────────────────────────────────────────────
        public const byte S_CHARACTER_STATS      = 0x26;
        public const byte S_LEVEL_UP             = 0x26;
        public const byte C_ADD_STAT             = 0xFE;

        // ── Chat ─────────────────────────────────────────────────────────────
        public const byte C_CHAT_MESSAGE         = 0x00;
        public const byte S_CHAT_MESSAGE         = 0x00;

        // ── Party ────────────────────────────────────────────────────────────
        public const byte C_PARTY_REQUEST        = 0x40;
        public const byte S_PARTY_REQUEST        = 0x40;
        public const byte C_PARTY_RESPONSE       = 0x41;
        public const byte S_PARTY_INFO           = 0x42;
        public const byte C_PARTY_LEAVE          = 0x43;

        // ── Registro ─────────────────────────────────────────────────────────
        public const byte C_REGISTER_ACCOUNT     = 0xF2;
        public const byte S_REGISTER_RESULT      = 0xF2;

        // ── Sistema ──────────────────────────────────────────────────────────
        public const byte S_SERVER_HELLO         = 0x00; // primer paquete al conectar
        public const byte C_PING                 = 0x0E;
        public const byte S_PONG                 = 0x0E;

        // Sub-comandos de F3 (personajes)
        public static class CharSubCmd
        {
            public const byte LIST   = 0x00;
            public const byte SELECT = 0x01;
            public const byte CREATE = 0x02;
            public const byte DELETE = 0x03;
        }
    }
}
