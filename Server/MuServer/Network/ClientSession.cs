using System.Net;
using System.Net.Sockets;
using MuServer.Network.Packets;

namespace MuServer.Network
{
    public class ClientSession
    {
        private static int _nextId = 1;

        public int SessionId { get; } = Interlocked.Increment(ref _nextId);
        public string RemoteEndPoint { get; }
        public bool IsConnected => _tcpClient?.Connected ?? false;

        // Estado de autenticación del cliente
        public string? AccountName { get; set; }
        public string? CharacterName { get; set; }
        public bool IsLoggedIn => AccountName != null;
        public bool IsInGame => CharacterName != null;

        private readonly TcpClient _tcpClient;
        private readonly NetworkStream _stream;
        private readonly System.Collections.Concurrent.ConcurrentQueue<byte[]> _outgoing = new();

        public ClientSession(TcpClient client)
        {
            _tcpClient = client;
            _tcpClient.NoDelay = true;
            _stream = client.GetStream();
            RemoteEndPoint = (client.Client.RemoteEndPoint as IPEndPoint)?.ToString() ?? "unknown";
        }

        public async Task StartAsync(PacketProcessor processor, CancellationToken ct)
        {
            try
            {
                // Enviar paquete de bienvenida
                SendHello();

                // WhenAny hacía que al terminar un bucle (p. ej. lectura tras catch) se cortara la sesión de inmediato.
                await Task.WhenAll(ReadLoopAsync(processor, ct), WriteLoopAsync(ct));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Session {SessionId}] Error: {ex.Message}");
            }
            finally
            {
                Disconnect();
            }
        }

        private async Task ReadLoopAsync(PacketProcessor processor, CancellationToken ct)
        {
            var header = new byte[3];
            try
            {
                while (!ct.IsCancellationRequested && IsConnected)
                {
                    await ReadExactAsync(header, 0, 1, ct);
                    byte marker = header[0];

                    byte[] packet;
                    if (marker == 0xC1 || marker == 0xC3)
                    {
                        await ReadExactAsync(header, 1, 1, ct);
                        int size = header[1];
                        if (size < 2) continue;
                        packet = new byte[size];
                        packet[0] = marker;
                        packet[1] = header[1];
                        if (size > 2) await ReadExactAsync(packet, 2, size - 2, ct);
                    }
                    else if (marker == 0xC2 || marker == 0xC4)
                    {
                        await ReadExactAsync(header, 1, 2, ct);
                        int size = (header[1] << 8) | header[2];
                        if (size < 3) continue;
                        packet = new byte[size];
                        packet[0] = marker;
                        packet[1] = header[1];
                        packet[2] = header[2];
                        if (size > 3) await ReadExactAsync(packet, 3, size - 3, ct);
                    }
                    else continue;

                    await processor.ProcessAsync(this, packet);
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                Console.WriteLine($"[Session {SessionId}] Read error: {ex.Message}");
                throw;
            }
        }

        private async Task WriteLoopAsync(CancellationToken ct)
        {
            try
            {
                while (!ct.IsCancellationRequested && IsConnected)
                {
                    if (_outgoing.TryDequeue(out var packet))
                        await _stream.WriteAsync(packet, 0, packet.Length, ct);
                    else
                        await Task.Delay(1, ct);
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                Console.WriteLine($"[Session {SessionId}] Write error: {ex.Message}");
                throw;
            }
        }

        public void Send(byte[] packet)
        {
            if (IsConnected)
                _outgoing.Enqueue(packet);
        }

        public void Disconnect()
        {
            _stream?.Close();
            _tcpClient?.Close();
            Console.WriteLine($"[Session {SessionId}] Desconectado ({AccountName ?? "anon"}).");
        }

        private void SendHello()
        {
            var hello = new ServerPacketWriter(PacketType.C1, 0x00, 4)
                .WriteByte(0x01) // version protocol
                .WriteByte(0x00)
                .Build();
            Send(hello);
        }

        private async Task ReadExactAsync(byte[] buf, int offset, int count, CancellationToken ct)
        {
            int total = 0;
            while (total < count)
            {
                int read = await _stream.ReadAsync(buf, offset + total, count - total, ct);
                if (read == 0) throw new Exception("Conexión cerrada por el cliente.");
                total += read;
            }
        }
    }
}
