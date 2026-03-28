using System;
using System.Collections.Generic;

namespace MuOnline.Core
{
    /// <summary>
    /// Sistema de eventos desacoplado. Permite comunicación entre sistemas
    /// sin referencias directas entre ellos.
    /// </summary>
    public static class EventBus
    {
        private static readonly Dictionary<Type, List<Delegate>> _subscribers = new();

        public static void Subscribe<T>(Action<T> handler) where T : struct
        {
            var type = typeof(T);
            if (!_subscribers.ContainsKey(type))
                _subscribers[type] = new List<Delegate>();

            _subscribers[type].Add(handler);
        }

        public static void Unsubscribe<T>(Action<T> handler) where T : struct
        {
            var type = typeof(T);
            if (_subscribers.TryGetValue(type, out var list))
                list.Remove(handler);
        }

        public static void Publish<T>(T eventData) where T : struct
        {
            var type = typeof(T);
            if (!_subscribers.TryGetValue(type, out var list)) return;

            foreach (var del in list.ToArray())
                (del as Action<T>)?.Invoke(eventData);
        }

        public static void Clear()
        {
            _subscribers.Clear();
        }
    }

    // ── Definición de todos los eventos del juego ──────────────────────────

    public static class NetworkEvents
    {
        public struct Connected { }
        public struct Disconnected { public string Reason; }
        public struct PacketReceived { public byte[] Data; public int Length; }
    }

    public static class AuthEvents
    {
        public struct LoginSuccess    { public string AccountName; }
        public struct LoginFailed     { public byte ErrorCode; public string Message; }
        public struct RegisterSuccess { public string AccountName; }
        public struct RegisterFailed  { public string Message; }
        public struct CharacterListReceived  { public MuCharacterInfo[] Characters; }
        public struct CharacterSelected      { public string Name; }
        public struct CharacterCreateSuccess { }
        public struct CharacterCreateFailed  { public byte ErrorCode; public string Message; }
        public struct CharacterDeleteSuccess { }
    }

    public static class WorldEvents
    {
        public struct MapLoaded { public int MapId; }
        /// <summary>Snapshot autoritativo del servidor al entrar al mapa o al actualizar stats.</summary>
        public struct PlayerStatsReceived
        {
            public string Name;
            public int    Level;
            public int    Hp, MaxHp, Mp, MaxMp;
            public long   Zen;
            public long   Exp, ExpMax;
        }
        public struct PlayerSpawned { public uint EntityId; public float X; public float Z; }
        public struct EntityMoved { public uint EntityId; public float X; public float Z; public byte Direction; }
        public struct EntityDied { public uint EntityId; }
        public struct EntitySpawned { public uint EntityId; public EntityType Type; public float X; public float Z; }
    }

    public static class CombatEvents
    {
        public struct DamageDealt { public uint AttackerId; public uint TargetId; public int Damage; public bool IsCritical; }
        public struct SkillUsed { public uint CasterId; public ushort SkillId; public uint TargetId; }
        public struct PlayerDied { }
        public struct PlayerRevived { }
    }

    public static class InventoryEvents
    {
        public struct ItemPickedUp { public ItemInfo Item; }
        public struct ItemEquipped { public ItemInfo Item; public byte Slot; }
        public struct ItemDropped { public uint ItemEntityId; }
        public struct GoldChanged { public uint NewAmount; }
    }

    public struct MuCharacterInfo
    {
        public string Name;
        public Core.CharacterClass Class;
        public int Level;
        public int MapId;
    }

    public struct ItemInfo
    {
        public ushort ItemId;
        public byte Level;
        public byte Slot;
        public string Name;
    }

    public enum EntityType : byte
    {
        Player = 0,
        Monster = 1,
        NPC = 2,
        Item = 3
    }
}
