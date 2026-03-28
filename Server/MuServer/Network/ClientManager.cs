using System.Collections.Concurrent;

namespace MuServer.Network
{
    public class ClientManager
    {
        private readonly int _maxConnections;
        private readonly ConcurrentDictionary<int, ClientSession> _sessions = new();

        public int ConnectedCount => _sessions.Count;
        public IEnumerable<ClientSession> Sessions => _sessions.Values;

        public ClientManager(int maxConnections)
        {
            _maxConnections = maxConnections;
        }

        public ClientSession? CreateSession(System.Net.Sockets.TcpClient client)
        {
            if (_sessions.Count >= _maxConnections) return null;

            var session = new ClientSession(client);
            _sessions[session.SessionId] = session;
            return session;
        }

        public void RemoveSession(int sessionId)
        {
            _sessions.TryRemove(sessionId, out _);
        }

        public ClientSession? GetByAccount(string account)
        {
            foreach (var s in _sessions.Values)
                if (s.AccountName == account) return s;
            return null;
        }

        public ClientSession? GetByCharacter(string name)
        {
            foreach (var s in _sessions.Values)
                if (s.CharacterName == name) return s;
            return null;
        }

        public void Broadcast(byte[] packet, ClientSession? exclude = null)
        {
            foreach (var s in _sessions.Values)
                if (s != exclude && s.IsConnected)
                    s.Send(packet);
        }

        public void BroadcastToMap(int mapId, byte[] packet, ClientSession? exclude = null)
        {
            // Se implementará cuando tengamos el sistema de mapas
            Broadcast(packet, exclude);
        }
    }
}
