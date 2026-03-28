using System.Text.Json;

namespace MuServer.Core
{
    public class ServerConfig
    {
        public string Host           { get; set; } = "0.0.0.0";
        public int    Port           { get; set; } = 44405;
        public int    MaxConnections { get; set; } = 1000;
        public string DatabasePath   { get; set; } = "muonline.db";
        public int    TickRateMs     { get; set; } = 50;   // 20 ticks/sec
        public bool   VerboseLogging { get; set; } = false;

        private static readonly string ConfigFile = "server.json";

        public static ServerConfig Load()
        {
            if (File.Exists(ConfigFile))
            {
                var json = File.ReadAllText(ConfigFile);
                return JsonSerializer.Deserialize<ServerConfig>(json) ?? new ServerConfig();
            }

            var defaults = new ServerConfig();
            File.WriteAllText(ConfigFile, JsonSerializer.Serialize(defaults,
                new JsonSerializerOptions { WriteIndented = true }));
            return defaults;
        }
    }
}
