using MuServer.Database;
using MuServer.Network.Packets;
using DbLoginResult = MuServer.Database.LoginResult;

namespace MuServer.Network
{
    /// <summary>
    /// Despacha paquetes recibidos al handler correspondiente.
    /// </summary>
    public class PacketProcessor
    {
        private readonly ClientManager _clients;
        private readonly DatabaseManager _db;

        // Handlers: headCode -> handler async
        private readonly Dictionary<byte, Func<ClientSession, ServerPacketReader, Task>> _handlers = new();

        public PacketProcessor(ClientManager clients, DatabaseManager db)
        {
            _clients = clients;
            _db = db;
            RegisterHandlers();
        }

        private void RegisterHandlers()
        {
            _handlers[0xF1] = HandleAuthPacket;   // Login
            _handlers[0xF2] = HandleRegister;     // Register
            _handlers[0xF3] = HandleCharPacket;   // Character
            _handlers[0xD4] = HandleWalk;
            _handlers[0x11] = HandleAttack;
            _handlers[0x00] = HandleChat;
            _handlers[0x0E] = HandlePing;
        }

        public async Task ProcessAsync(ClientSession session, byte[] data)
        {
            var reader = new ServerPacketReader(data);

            if (_handlers.TryGetValue(reader.HeadCode, out var handler))
            {
                try   { await handler(session, reader); }
                catch (Exception ex) { Console.WriteLine($"[Processor] Error 0x{reader.HeadCode:X2}: {ex.Message}"); }
            }
            else
            {
                Console.WriteLine($"[Processor] Sin handler para 0x{reader.HeadCode:X2} (session={session.SessionId})");
            }
        }

        // ── Register ─────────────────────────────────────────────────────────

        private async Task HandleRegister(ClientSession session, ServerPacketReader r)
        {
            string username = r.ReadString(10);
            string password = r.ReadString(10);

            Console.WriteLine($"[Auth] Registro: '{username}'");

            var (ok, code) = await _db.CreateAccountAsync(username, password);

            var response = new ServerPacketWriter(PacketType.C1, 0xF2, 16)
                .WriteByte(0x00)                        // sub-code (consistente con F1/F3)
                .WriteByte(ok ? (byte)0x01 : code)      // resultado
                .WriteString(username, 10)
                .Build();
            session.Send(response);
        }

        // ── Auth ─────────────────────────────────────────────────────────────

        private async Task HandleAuthPacket(ClientSession session, ServerPacketReader r)
        {
            byte sub = r.ReadByte(); // 0x00 = login
            if (sub != 0x00) return;

            string account  = r.ReadString(10);
            string password = r.ReadString(10);

            Console.WriteLine($"[Auth] Login: account='{account}'");

            var result = await _db.ValidateLoginAsync(account, password);

            // Verificar si la cuenta ya está online (solo si la sesión está realmente activa)
            var existing = _clients.GetByAccount(account);
            if (result == DbLoginResult.Success && existing != null && existing.IsConnected)
                result = DbLoginResult.AlreadyConnected;

            byte code = result switch
            {
                DbLoginResult.Success          => 0x01,
                DbLoginResult.InvalidAccount   => 0x00,
                DbLoginResult.WrongPassword    => 0x02,
                DbLoginResult.AlreadyConnected => 0x03,
                DbLoginResult.Banned           => 0x05,
                _                              => 0x00
            };

            if (result == DbLoginResult.Success)
                session.AccountName = account;

            var response = new ServerPacketWriter(PacketType.C1, 0xF1, 16)
                .WriteByte(0x00)
                .WriteByte(code)
                .WriteString(account, 10)
                .Build();

            session.Send(response);
        }

        private async Task HandleCharPacket(ClientSession session, ServerPacketReader r)
        {
            if (!session.IsLoggedIn) return;
            byte sub = r.ReadByte();

            switch (sub)
            {
                case 0x00: await SendCharacterList(session); break;
                case 0x01: await HandleCharacterSelect(session, r); break;
                case 0x02: await HandleCharacterCreate(session, r); break;
                case 0x03: await HandleCharacterDelete(session, r); break;
            }
        }

        private async Task SendCharacterList(ClientSession session)
        {
            var characters = await _db.GetCharactersAsync(session.AccountName!);
            int count = characters.Count;

            var w = new ServerPacketWriter(PacketType.C1, 0xF3, 4 + count * 20);
            w.WriteByte(0x00);   // sub LIST
            w.WriteByte((byte)count);

            foreach (var ch in characters)
            {
                w.WriteString(ch.Name, 10);
                w.WriteByte((byte)ch.Class);
                w.WriteUShort((ushort)ch.Level);
                w.WriteByte((byte)ch.MapId);
                w.WriteByte(0); w.WriteByte(0); w.WriteByte(0); // reservado
            }

            session.Send(w.Build());
            await Task.CompletedTask;
        }

        private async Task HandleCharacterSelect(ClientSession session, ServerPacketReader r)
        {
            string name = r.ReadString(10);
            var ch = await _db.GetCharacterAsync(session.AccountName!, name);

            if (ch == null)
            {
                var fail = new ServerPacketWriter(PacketType.C1, 0xF3, 4)
                    .WriteByte(0x01).WriteByte(0x00).Build();
                session.Send(fail);
                return;
            }

            session.CharacterName = name;

            var ok = new ServerPacketWriter(PacketType.C1, 0xF3, 20)
                .WriteByte(0x01)
                .WriteByte(0xFF)
                .WriteString(name, 10)
                .Build();
            session.Send(ok);

            // Enviar al mapa inicial + snapshot de stats (autoritativo desde BD)
            await SendMapEnter(session, ch.MapId, (byte)ch.PosX, (byte)ch.PosZ);
            await SendPlayerStats(session, ch);
        }

        private async Task HandleCharacterCreate(ClientSession session, ServerPacketReader r)
        {
            string name     = r.ReadString(10);
            byte charClass  = r.ReadByte();

            bool ok = await _db.CreateCharacterAsync(session.AccountName!, name, charClass);

            var response = new ServerPacketWriter(PacketType.C1, 0xF3, 4)
                .WriteByte(0x02)
                .WriteByte(ok ? (byte)0x00 : (byte)0x01)
                .Build();
            session.Send(response);
        }

        private async Task HandleCharacterDelete(ClientSession session, ServerPacketReader r)
        {
            string name = r.ReadString(10);
            string pin  = r.ReadString(7);
            bool ok     = await _db.DeleteCharacterAsync(session.AccountName!, name, pin);

            var response = new ServerPacketWriter(PacketType.C1, 0xF3, 4)
                .WriteByte(0x03)
                .WriteByte(ok ? (byte)0x01 : (byte)0x00)
                .Build();
            session.Send(response);
        }

        private async Task SendMapEnter(ClientSession session, int mapId, byte x, byte z)
        {
            var packet = new ServerPacketWriter(PacketType.C1, 0x1C, 8)
                .WriteByte(0x00)
                .WriteByte((byte)mapId)
                .WriteByte(0)
                .WriteByte(x)
                .WriteByte(z)
                .Build();
            session.Send(packet);
            await Task.CompletedTask;
        }

        /// <summary>0x26/0x00 — HP/MP, zen, exp para sincronizar HUD del cliente.</summary>
        private async Task SendPlayerStats(ClientSession session, CharacterData ch)
        {
            uint expMax = ExperienceCapForLevel(ch.Level);
            var w = new ServerPacketWriter(PacketType.C1, 0x26, 52)
                .WriteByte(0x00)
                .WriteString(ch.Name, 10)
                .WriteUShort((ushort)ch.Level)
                .WriteInt(ch.Hp)
                .WriteInt(ch.MaxHp)
                .WriteInt(ch.Mana)
                .WriteInt(ch.MaxMana)
                .WriteUInt((uint)Math.Max(0, ch.Zen))
                .WriteUInt((uint)Math.Max(0, ch.Experience))
                .WriteUInt(expMax);
            session.Send(w.Build());
            await Task.CompletedTask;
        }

        /// <summary>Exp necesaria para el nivel actual (simplificada; se puede sustituir por tabla S6).</summary>
        private static uint ExperienceCapForLevel(int level)
            => (uint)Math.Max(100, level * 120 + 180);

        // ── Mundo ────────────────────────────────────────────────────────────

        private async Task HandleWalk(ClientSession session, ServerPacketReader r)
        {
            if (!session.IsInGame) return;
            byte x   = r.ReadByte();
            byte z   = r.ReadByte();
            byte dir = r.ReadByte();

            // Broadcast del movimiento a otros jugadores del mismo mapa
            var broadcast = new ServerPacketWriter(PacketType.C1, 0xD4, 8)
                .WriteUShort((ushort)session.SessionId)
                .WriteByte(x).WriteByte(z).WriteByte(dir)
                .Build();

            _clients.Broadcast(broadcast, exclude: session);
            await Task.CompletedTask;
        }

        private async Task HandleAttack(ClientSession session, ServerPacketReader r)
        {
            if (!session.IsInGame) return;
            ushort targetId  = r.ReadUShort();
            byte   attackType = r.ReadByte();

            // Lógica de combate básica (se expandirá en Fase 4)
            int damage = new Random().Next(10, 50);

            var result = new ServerPacketWriter(PacketType.C1, 0x11, 10)
                .WriteUShort((ushort)session.SessionId)
                .WriteUShort(targetId)
                .WriteUShort((ushort)damage)
                .WriteByte(0) // no crítico
                .Build();

            session.Send(result);
            await Task.CompletedTask;
        }

        private async Task HandleChat(ClientSession session, ServerPacketReader r)
        {
            if (!session.IsInGame) return;
            string message = r.ReadString(60);
            Console.WriteLine($"[Chat] {session.CharacterName}: {message}");

            var broadcast = new ServerPacketWriter(PacketType.C1, 0x00, 80)
                .WriteString(session.CharacterName!, 10)
                .WriteString(message, 60)
                .Build();

            _clients.Broadcast(broadcast);
            await Task.CompletedTask;
        }

        private async Task HandlePing(ClientSession session, ServerPacketReader r)
        {
            var pong = new ServerPacketWriter(PacketType.C1, 0x0E, 2).Build();
            session.Send(pong);
            await Task.CompletedTask;
        }
    }

}
