using System;
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using MuOnline.Core;

namespace MuOnline.Network
{
    /// <summary>
    /// Cliente TCP async. Lectura/escritura en segundo plano; paquetes a la cola principal de Unity.
    /// Importante: no usar Task.WhenAny sobre los bucles — al terminar uno se cerraba el socket por error.
    /// </summary>
    public class NetworkClient : MonoBehaviour
    {
        public static NetworkClient Instance { get; private set; }

        public bool IsConnected => _tcpClient?.Connected ?? false;

        private TcpClient _tcpClient;
        private NetworkStream _stream;
        private CancellationTokenSource _cts;

        private readonly ConcurrentQueue<byte[]> _incomingPackets = new();
        private readonly ConcurrentQueue<byte[]> _outgoingPackets = new();

        private int _connectionGeneration;
        private volatile bool _teardownPosted;
        private int _connectInProgress;

        private const int BUFFER_SIZE = 4096;
        private const int MAX_PACKET_SIZE = 65535;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        void Update()
        {
            while (_incomingPackets.TryDequeue(out var packet))
            {
                if (PacketHandler.Instance != null)
                    PacketHandler.Instance.ProcessPacket(packet);
                else
                    Debug.LogWarning("[Network] PacketHandler no listo; paquete descartado.");
            }
        }

        void OnDestroy()
        {
            DisconnectUser();
        }

        public void Connect(string host, int port)
        {
            if (IsConnected) return;
            if (Interlocked.CompareExchange(ref _connectInProgress, 1, 0) != 0)
            {
                Debug.Log("[Network] Ya hay un intento de conexión en curso.");
                return;
            }

            _teardownPosted = false;
            Interlocked.Increment(ref _connectionGeneration);
            _cts = new CancellationTokenSource();
            int gen = _connectionGeneration;
            _ = ConnectAsync(host, port, gen, _cts.Token);
        }

        private async Task ConnectAsync(string host, int port, int gen, CancellationToken ct)
        {
            try
            {
                Debug.Log($"[Network] Conectando a {host}:{port}...");
                var client = new TcpClient();
                client.NoDelay = true;
                client.ReceiveBufferSize = BUFFER_SIZE;
                client.SendBufferSize = BUFFER_SIZE;

                await client.ConnectAsync(host, port);
                if (gen != _connectionGeneration) { client.Close(); return; }

                _tcpClient = client;
                _stream    = _tcpClient.GetStream();

                Debug.Log("[Network] Conectado.");
                EventBus.Publish(new NetworkEvents.Connected());

                _ = ReadLoopAsync(gen, ct);
                _ = WriteLoopAsync(gen, ct);
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                Debug.LogWarning($"[Network] Error de conexión: {ex.Message}");
                TeardownFromError(gen, ex.Message);
            }
            finally
            {
                Interlocked.Exchange(ref _connectInProgress, 0);
            }
        }

        /// <summary>Cierre iniciado por el usuario (no publicar dos veces si ya hubo error).</summary>
        public void Disconnect()
        {
            DisconnectUser();
        }

        void DisconnectUser()
        {
            Interlocked.Increment(ref _connectionGeneration);
            _cts?.Cancel();
            TryPublishTeardown("Desconectado.");
            CleanupSocket();
        }

        void TeardownFromError(int gen, string reason)
        {
            if (gen != _connectionGeneration) return;
            _cts?.Cancel();
            TryPublishTeardown(reason);
            CleanupSocket();
        }

        void TryPublishTeardown(string reason)
        {
            if (_teardownPosted) return;
            _teardownPosted = true;
            EventBus.Publish(new NetworkEvents.Disconnected { Reason = reason });
        }

        private async Task ReadLoopAsync(int gen, CancellationToken ct)
        {
            var headerBuffer = new byte[3];

            try
            {
                while (!ct.IsCancellationRequested && gen == _connectionGeneration && IsConnected)
                {
                    await ReadExactAsync(headerBuffer, 0, 1, ct);
                    byte marker = headerBuffer[0];

                    byte[] packet;

                    if (marker == 0xC1 || marker == 0xC3)
                    {
                        await ReadExactAsync(headerBuffer, 1, 1, ct);
                        int size = headerBuffer[1];
                        if (size < 2) continue;

                        packet = new byte[size];
                        packet[0] = marker;
                        packet[1] = headerBuffer[1];
                        if (size > 2)
                            await ReadExactAsync(packet, 2, size - 2, ct);
                    }
                    else if (marker == 0xC2 || marker == 0xC4)
                    {
                        await ReadExactAsync(headerBuffer, 1, 2, ct);
                        int size = (headerBuffer[1] << 8) | headerBuffer[2];
                        if (size < 3) continue;

                        packet = new byte[size];
                        packet[0] = marker;
                        packet[1] = headerBuffer[1];
                        packet[2] = headerBuffer[2];
                        if (size > 3)
                            await ReadExactAsync(packet, 3, size - 3, ct);
                    }
                    else
                    {
                        Debug.LogWarning($"[Network] Byte de inicio desconocido: 0x{marker:X2}");
                        continue;
                    }

                    _incomingPackets.Enqueue(packet);
                }
            }
            catch (OperationCanceledException) { }
            catch (System.IO.IOException ex)
            {
                if (gen == _connectionGeneration && !ct.IsCancellationRequested)
                {
                    Debug.LogWarning($"[Network] Lectura: {ex.Message}");
                    TeardownFromError(gen, ex.Message);
                }
            }
            catch (System.Net.Sockets.SocketException ex)
            {
                if (gen == _connectionGeneration && !ct.IsCancellationRequested)
                {
                    Debug.LogWarning($"[Network] Socket: {ex.Message}");
                    TeardownFromError(gen, ex.Message);
                }
            }
            catch (Exception ex)
            {
                if (gen == _connectionGeneration && !ct.IsCancellationRequested)
                {
                    Debug.LogWarning($"[Network] Error en lectura: {ex.Message}");
                    TeardownFromError(gen, ex.Message);
                }
            }
        }

        private async Task WriteLoopAsync(int gen, CancellationToken ct)
        {
            try
            {
                while (!ct.IsCancellationRequested && gen == _connectionGeneration && IsConnected)
                {
                    if (_outgoingPackets.TryDequeue(out var packet))
                    {
                        await _stream.WriteAsync(packet, 0, packet.Length, ct);
                        await _stream.FlushAsync(ct);
                    }
                    else
                        await Task.Delay(1, ct);
                }
            }
            catch (OperationCanceledException) { }
            catch (System.IO.IOException ex)
            {
                if (gen == _connectionGeneration && !ct.IsCancellationRequested)
                    TeardownFromError(gen, ex.Message);
            }
            catch (System.Net.Sockets.SocketException ex)
            {
                if (gen == _connectionGeneration && !ct.IsCancellationRequested)
                    TeardownFromError(gen, ex.Message);
            }
            catch (Exception ex)
            {
                if (gen == _connectionGeneration && !ct.IsCancellationRequested)
                    Debug.LogWarning($"[Network] Error en escritura: {ex.Message}");
            }
        }

        private async Task ReadExactAsync(byte[] buffer, int offset, int count, CancellationToken ct)
        {
            int totalRead = 0;
            while (totalRead < count)
            {
                int read = await _stream.ReadAsync(buffer, offset + totalRead, count - totalRead, ct);
                if (read == 0)
                    throw new System.IO.IOException("El servidor cerró la conexión.");
                totalRead += read;
            }
        }

        public void Send(byte[] packet)
        {
            if (!IsConnected)
            {
                Debug.LogWarning("[Network] Intento de envío sin conexión activa.");
                return;
            }
            _outgoingPackets.Enqueue(packet);
        }

        private void CleanupSocket()
        {
            try { _stream?.Close(); } catch { /* ignore */ }
            try { _tcpClient?.Close(); } catch { /* ignore */ }
            _stream    = null;
            _tcpClient = null;
        }
    }
}
