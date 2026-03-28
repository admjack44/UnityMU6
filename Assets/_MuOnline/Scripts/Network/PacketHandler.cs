using System;
using System.Collections.Generic;
using UnityEngine;
using MuOnline.Core;
using MuOnline.Network.Packets;
using MuOnline.Network.Protocol;

namespace MuOnline.Network
{
    /// <summary>
    /// Despachador central de paquetes.
    /// Registra handlers por (headCode, subCode) y los invoca en el hilo de Unity.
    /// </summary>
    public class PacketHandler : MonoBehaviour
    {
        public static PacketHandler Instance { get; private set; }

        private readonly Dictionary<(byte, byte), Action<PacketReader>> _handlers = new();

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            RegisterAllHandlers();
        }

        private void RegisterAllHandlers()
        {
            // Auth
            Register(PacketIds.S_LOGIN_RESULT,         0x00, OnLoginResult);
            Register(PacketIds.S_REGISTER_RESULT,      0x00, OnRegisterResult);
            Register(PacketIds.S_CHARACTER_LIST,       PacketIds.CharSubCmd.LIST,   OnCharacterList);
            Register(PacketIds.S_CHARACTER_SELECT,     PacketIds.CharSubCmd.SELECT, OnCharacterSelect);
            Register(PacketIds.S_CHARACTER_CREATE,     PacketIds.CharSubCmd.CREATE, OnCharacterCreate);

            // Mundo
            Register(PacketIds.S_MAP_ENTER,            0x00, OnMapEnter);
            Register(PacketIds.S_ENTITY_MOVE,          0x00, OnEntityMove);
            Register(PacketIds.S_ENTITY_SPAWN,         0x00, OnEntitySpawn);
            Register(PacketIds.S_ENTITY_DESPAWN,       0x00, OnEntityDespawn);

            // Combate
            Register(PacketIds.S_ATTACK_RESULT,        0x00, OnAttackResult);
            Register(PacketIds.S_ENTITY_DIED,          0x00, OnEntityDied);

            // Sistema
            Register(PacketIds.S_SERVER_HELLO,         0x01, OnServerHello);
            Register(PacketIds.S_PONG,                 0x00, OnPong);
            Register(PacketIds.S_CHARACTER_STATS,      0x00, OnCharacterStats);
        }

        public void Register(byte headCode, byte subCode, Action<PacketReader> handler)
        {
            var key = (headCode, subCode);
            _handlers[key] = handler;
        }

        public void ProcessPacket(byte[] data)
        {
            if (data == null || data.Length < 3) return;

            var reader = new PacketReader(data);
            byte headCode = reader.HeadCode;
            byte subCode  = reader.SubCode;

            var key = (headCode, subCode);

            if (_handlers.TryGetValue(key, out var handler))
            {
                try { handler.Invoke(reader); }
                catch (Exception ex) { Debug.LogError($"[PacketHandler] Error en 0x{headCode:X2}/0x{subCode:X2}: {ex}"); }
            }
            else
            {
                Debug.LogWarning($"[PacketHandler] Sin handler para 0x{headCode:X2} / sub 0x{subCode:X2} (len={data.Length})");
            }
        }

        // ── Handlers ─────────────────────────────────────────────────────────

        private void OnLoginResult(PacketReader r)
        {
            byte result = r.ReadByte();
            if (result == 1)
            {
                string account = r.ReadString(10);
                EventBus.Publish(new AuthEvents.LoginSuccess { AccountName = account });
            }
            else
            {
                EventBus.Publish(new AuthEvents.LoginFailed
                {
                    ErrorCode = result,
                    Message = GetLoginErrorMessage(result)
                });
            }
        }

        private void OnRegisterResult(PacketReader r)
        {
            byte result  = r.ReadByte();
            string name  = r.ReadString(10);
            if (result == 0x01)
                EventBus.Publish(new AuthEvents.RegisterSuccess { AccountName = name });
            else
                EventBus.Publish(new AuthEvents.RegisterFailed
                {
                    Message = result switch
                    {
                        0x00 => "El nombre de usuario ya existe.",
                        0x02 => "El nombre de usuario no es válido.",
                        _    => "Error al registrar la cuenta."
                    }
                });
        }

        private void OnCharacterList(PacketReader r)
        {
            byte count = r.ReadByte();
            var chars = new MuCharacterInfo[count];
            for (int i = 0; i < count; i++)
            {
                chars[i] = new MuCharacterInfo
                {
                    Name  = r.ReadString(10),
                    Class = (Core.CharacterClass)r.ReadByte(),
                    Level = r.ReadUShort(),
                    MapId = r.ReadByte()
                };
                r.Skip(3); // reservado
            }
            EventBus.Publish(new AuthEvents.CharacterListReceived { Characters = chars });
        }

        private void OnCharacterSelect(PacketReader r)
        {
            byte result = r.ReadByte();
            if (result == 0xFF)
            {
                string name  = r.ReadString(10);
                EventBus.Publish(new AuthEvents.CharacterSelected { Name = name });
            }
        }

        private void OnCharacterCreate(PacketReader r)
        {
            byte result = r.ReadByte();
            Debug.Log($"[Auth] Creación de personaje: resultado={result}");

            if (result == 0x00)
                // Éxito — pedir lista actualizada de personajes
                EventBus.Publish(new AuthEvents.CharacterCreateSuccess());
            else
                EventBus.Publish(new AuthEvents.CharacterCreateFailed
                    { ErrorCode = result, Message = "No se pudo crear el personaje." });
        }

        private void OnMapEnter(PacketReader r)
        {
            byte mapId = r.ReadByte();
            r.Skip(1);
            float x = r.ReadByte();
            float z = r.ReadByte();
            EventBus.Publish(new WorldEvents.MapLoaded { MapId = mapId });
            EventBus.Publish(new WorldEvents.PlayerSpawned { EntityId = 0, X = x, Z = z });
        }

        private void OnEntityMove(PacketReader r)
        {
            uint entityId = r.ReadUShort();
            float x = r.ReadByte();
            float z = r.ReadByte();
            byte dir = r.ReadByte();
            EventBus.Publish(new WorldEvents.EntityMoved { EntityId = entityId, X = x, Z = z, Direction = dir });
        }

        private void OnEntitySpawn(PacketReader r)
        {
            uint id   = r.ReadUShort();
            var type  = (EntityType)r.ReadByte();
            float x   = r.ReadByte();
            float z   = r.ReadByte();
            EventBus.Publish(new WorldEvents.EntitySpawned { EntityId = id, Type = type, X = x, Z = z });
        }

        private void OnEntityDespawn(PacketReader r)
        {
            uint id = r.ReadUShort();
            EventBus.Publish(new WorldEvents.EntityDied { EntityId = id });
        }

        private void OnAttackResult(PacketReader r)
        {
            uint attacker = r.ReadUShort();
            uint target   = r.ReadUShort();
            int  damage   = r.ReadUShort();
            bool isCrit   = r.ReadByte() == 1;
            EventBus.Publish(new CombatEvents.DamageDealt
            {
                AttackerId = attacker,
                TargetId   = target,
                Damage     = damage,
                IsCritical = isCrit
            });
        }

        private void OnEntityDied(PacketReader r)
        {
            uint id = r.ReadUShort();
            EventBus.Publish(new WorldEvents.EntityDied { EntityId = id });
        }

        private void OnServerHello(PacketReader r)
        {
            byte version = r.ReadByte();
            Debug.Log($"[Network] Servidor conectado. Protocolo v{version}.");
        }

        private void OnPong(PacketReader r)
        {
            // Latency tracking se puede implementar aquí
        }

        private void OnCharacterStats(PacketReader r)
        {
            string name = r.ReadString(10);
            int level   = r.ReadUShort();
            int hp      = r.ReadInt();
            int maxHp   = r.ReadInt();
            int mp      = r.ReadInt();
            int maxMp   = r.ReadInt();
            long zen    = r.ReadUInt();
            long exp    = r.ReadUInt();
            long expMax = r.ReadUInt();

            var gm = Core.GameManager.Instance;
            if (gm?.LocalPlayer != null)
            {
                gm.LocalPlayer.CharacterName  = name.Trim();
                gm.LocalPlayer.CharacterLevel = level;
            }

            EventBus.Publish(new WorldEvents.PlayerStatsReceived
            {
                Name   = name.Trim(),
                Level  = level,
                Hp     = hp,
                MaxHp  = maxHp,
                Mp     = mp,
                MaxMp  = maxMp,
                Zen    = zen,
                Exp    = exp,
                ExpMax = expMax
            });
        }

        private string GetLoginErrorMessage(byte code) => code switch
        {
            0x00 => "Cuenta incorrecta.",
            0x02 => "Contraseña incorrecta.",
            0x03 => "Cuenta ya conectada.",
            0x04 => "Servidor lleno.",
            0x05 => "Cuenta bloqueada.",
            _    => $"Error desconocido ({code})."
        };
    }
}
