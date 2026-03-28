using System.Net;
using System.Net.Sockets;
using MuServer.Database;
using MuServer.Network;

namespace MuServer.Core
{
    public class GameServer
    {
        private readonly ServerConfig _config;
        private readonly DatabaseManager _db;
        private TcpListener _listener = null!;
        private readonly CancellationTokenSource _cts = new();

        private readonly ClientManager _clientManager;
        private readonly PacketProcessor _packetProcessor;

        public GameServer(ServerConfig config, DatabaseManager db)
        {
            _config = config;
            _db = db;
            _clientManager = new ClientManager(_config.MaxConnections);
            _packetProcessor = new PacketProcessor(_clientManager, _db);
        }

        public async Task StartAsync()
        {
            _listener = new TcpListener(IPAddress.Parse(_config.Host), _config.Port);
            _listener.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            _listener.Start();

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"[Server] Escuchando en {_config.Host}:{_config.Port}");
            Console.ResetColor();

            // Loop de aceptación de clientes
            var acceptTask   = AcceptClientsAsync(_cts.Token);
            // Loop principal del juego (tick)
            var gameLoopTask = GameLoopAsync(_cts.Token);

            Console.CancelKeyPress += (_, e) =>
            {
                e.Cancel = true;
                _cts.Cancel();
                Console.WriteLine("\n[Server] Apagando servidor...");
            };

            await Task.WhenAll(acceptTask, gameLoopTask);
            _listener.Stop();
            Console.WriteLine("[Server] Servidor detenido.");
        }

        private async Task AcceptClientsAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    var tcpClient = await _listener.AcceptTcpClientAsync(ct);
                    var session   = _clientManager.CreateSession(tcpClient);
                    if (session != null)
                    {
                        _ = HandleSessionAsync(session, ct);
                        Console.WriteLine($"[Server] Cliente conectado: {session.RemoteEndPoint} (ID={session.SessionId})");
                    }
                    else
                    {
                        Console.WriteLine("[Server] Servidor lleno. Conexión rechazada.");
                        tcpClient.Close();
                    }
                }
                catch (OperationCanceledException) { break; }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Server] Error al aceptar cliente: {ex.Message}");
                }
            }
        }

        private async Task HandleSessionAsync(ClientSession session, CancellationToken ct)
        {
            await session.StartAsync(_packetProcessor, ct);
            _clientManager.RemoveSession(session.SessionId);
            Console.WriteLine($"[Server] Sesión {session.SessionId} eliminada del registro.");
        }

        private async Task GameLoopAsync(CancellationToken ct)
        {
            var delay = TimeSpan.FromMilliseconds(_config.TickRateMs);
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    // Tick del mundo (movimiento de monstruos, regeneración, etc.)
                    // Se implementará en fases posteriores
                    await Task.Delay(delay, ct);
                }
                catch (OperationCanceledException) { break; }
            }
        }
    }
}
