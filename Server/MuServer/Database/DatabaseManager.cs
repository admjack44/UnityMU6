using Microsoft.Data.Sqlite;

namespace MuServer.Database
{
    public class DatabaseManager
    {
        private readonly string _connectionString;

        public DatabaseManager(string dbPath)
        {
            _connectionString = $"Data Source={dbPath}";
        }

        public void Initialize()
        {
            using var conn = OpenConnection();
            ExecuteNonQuery(conn, @"
                CREATE TABLE IF NOT EXISTS accounts (
                    id       INTEGER PRIMARY KEY AUTOINCREMENT,
                    username TEXT NOT NULL UNIQUE COLLATE NOCASE,
                    password TEXT NOT NULL,
                    pin      TEXT NOT NULL DEFAULT '1234',
                    is_banned INTEGER NOT NULL DEFAULT 0,
                    created_at TEXT DEFAULT CURRENT_TIMESTAMP
                );

                CREATE TABLE IF NOT EXISTS characters (
                    id        INTEGER PRIMARY KEY AUTOINCREMENT,
                    account   TEXT NOT NULL REFERENCES accounts(username),
                    name      TEXT NOT NULL UNIQUE COLLATE NOCASE,
                    class     INTEGER NOT NULL DEFAULT 0,
                    level     INTEGER NOT NULL DEFAULT 1,
                    experience INTEGER NOT NULL DEFAULT 0,
                    strength  INTEGER NOT NULL DEFAULT 25,
                    agility   INTEGER NOT NULL DEFAULT 20,
                    vitality  INTEGER NOT NULL DEFAULT 25,
                    energy    INTEGER NOT NULL DEFAULT 10,
                    command   INTEGER NOT NULL DEFAULT 0,
                    hp        INTEGER NOT NULL DEFAULT 110,
                    max_hp    INTEGER NOT NULL DEFAULT 110,
                    mana      INTEGER NOT NULL DEFAULT 60,
                    max_mana  INTEGER NOT NULL DEFAULT 60,
                    map_id    INTEGER NOT NULL DEFAULT 0,
                    pos_x     REAL NOT NULL DEFAULT 135,
                    pos_z     REAL NOT NULL DEFAULT 130,
                    zen       INTEGER NOT NULL DEFAULT 0,
                    created_at TEXT DEFAULT CURRENT_TIMESTAMP
                );
            ");

            // Crear cuenta de administrador por defecto si no existe
            SeedDefaultAccount(conn);

            Console.WriteLine("[DB] Base de datos inicializada.");
        }

        private void SeedDefaultAccount(SqliteConnection conn)
        {
            var count = ExecuteScalar<long>(conn,
                "SELECT COUNT(*) FROM accounts WHERE username='admin'");
            if (count == 0)
            {
                ExecuteNonQuery(conn,
                    "INSERT INTO accounts (username, password) VALUES ('admin', 'admin123')");
                Console.WriteLine("[DB] Cuenta de administrador creada: admin / admin123");
            }
        }

        public async Task<LoginResult> ValidateLoginAsync(string account, string password)
        {
            await using var conn = OpenConnection();
            var reader = await ExecuteReaderAsync(conn,
                "SELECT password, is_banned FROM accounts WHERE username=@u",
                new SqliteParameter("@u", account));

            if (!await reader.ReadAsync())
                return LoginResult.InvalidAccount;

            string dbPass    = reader.GetString(0);
            bool   isBanned  = reader.GetInt32(1) == 1;

            if (isBanned) return LoginResult.Banned;
            if (dbPass != password) return LoginResult.WrongPassword;
            return LoginResult.Success;
        }

        public async Task<List<CharacterData>> GetCharactersAsync(string account)
        {
            var list = new List<CharacterData>();
            await using var conn = OpenConnection();
            var reader = await ExecuteReaderAsync(conn,
                "SELECT name, class, level, map_id FROM characters WHERE account=@a ORDER BY id",
                new SqliteParameter("@a", account));

            while (await reader.ReadAsync())
            {
                list.Add(new CharacterData
                {
                    Name  = reader.GetString(0),
                    Class = reader.GetInt32(1),
                    Level = reader.GetInt32(2),
                    MapId = reader.GetInt32(3)
                });
            }
            return list;
        }

        public async Task<CharacterData?> GetCharacterAsync(string account, string name)
        {
            await using var conn = OpenConnection();
            var reader = await ExecuteReaderAsync(conn,
                @"SELECT name, class, level, map_id, pos_x, pos_z,
                         strength, agility, vitality, energy, hp, max_hp, mana, max_mana, zen, experience
                  FROM characters WHERE account=@a AND name=@n",
                new SqliteParameter("@a", account),
                new SqliteParameter("@n", name));

            if (!await reader.ReadAsync()) return null;

            return new CharacterData
            {
                Name     = reader.GetString(0),
                Class    = reader.GetInt32(1),
                Level    = reader.GetInt32(2),
                MapId    = reader.GetInt32(3),
                PosX     = reader.GetDouble(4),
                PosZ     = reader.GetDouble(5),
                Strength = reader.GetInt32(6),
                Agility  = reader.GetInt32(7),
                Vitality = reader.GetInt32(8),
                Energy   = reader.GetInt32(9),
                Hp       = reader.GetInt32(10),
                MaxHp    = reader.GetInt32(11),
                Mana     = reader.GetInt32(12),
                MaxMana  = reader.GetInt32(13),
                Zen          = reader.GetInt32(14),
                Experience = reader.GetInt32(15)
            };
        }

        public async Task<(bool ok, byte code)> CreateAccountAsync(string username, string password)
        {
            await using var conn = OpenConnection();

            // Verificar si ya existe
            var exists = ExecuteScalar<long>(conn,
                "SELECT COUNT(*) FROM accounts WHERE username=@u",
                new SqliteParameter("@u", username));
            if (exists > 0) return (false, 0x00); // ya existe

            if (username.Length < 4 || password.Length < 6) return (false, 0x02); // inválido

            try
            {
                ExecuteNonQuery(conn,
                    "INSERT INTO accounts (username, password) VALUES (@u, @p)",
                    new SqliteParameter("@u", username),
                    new SqliteParameter("@p", password));
                Console.WriteLine($"[DB] Cuenta creada: {username}");
                return (true, 0x01);
            }
            catch { return (false, 0xFF); }
        }

        public async Task<bool> CreateCharacterAsync(string account, string name, int charClass)
        {
            await using var conn = OpenConnection();
            // Verificar límite de personajes (max 5)
            var count = ExecuteScalar<long>(conn,
                "SELECT COUNT(*) FROM characters WHERE account=@a",
                new SqliteParameter("@a", account));
            if (count >= 5) return false;

            // Verificar nombre único
            var exists = ExecuteScalar<long>(conn,
                "SELECT COUNT(*) FROM characters WHERE name=@n",
                new SqliteParameter("@n", name));
            if (exists > 0) return false;

            try
            {
                ExecuteNonQuery(conn,
                    "INSERT INTO characters (account, name, class) VALUES (@a, @n, @c)",
                    new SqliteParameter("@a", account),
                    new SqliteParameter("@n", name),
                    new SqliteParameter("@c", charClass));
                return true;
            }
            catch { return false; }
        }

        public async Task<bool> DeleteCharacterAsync(string account, string name, string pin)
        {
            await using var conn = OpenConnection();
            var dbPin = ExecuteScalar<string>(conn,
                "SELECT pin FROM accounts WHERE username=@a",
                new SqliteParameter("@a", account));

            if (dbPin != pin) return false;

            int rows = ExecuteNonQuery(conn,
                "DELETE FROM characters WHERE account=@a AND name=@n",
                new SqliteParameter("@a", account),
                new SqliteParameter("@n", name));
            await Task.CompletedTask;
            return rows > 0;
        }

        // ── Helpers ──────────────────────────────────────────────────────────

        private SqliteConnection OpenConnection()
        {
            var conn = new SqliteConnection(_connectionString);
            conn.Open();
            return conn;
        }

        private int ExecuteNonQuery(SqliteConnection conn, string sql, params SqliteParameter[] pars)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            foreach (var p in pars) cmd.Parameters.Add(p);
            return cmd.ExecuteNonQuery();
        }

        private T ExecuteScalar<T>(SqliteConnection conn, string sql, params SqliteParameter[] pars)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            foreach (var p in pars) cmd.Parameters.Add(p);
            var result = cmd.ExecuteScalar();
            if (result == null || result is DBNull) return default!;
            return (T)Convert.ChangeType(result, typeof(T));
        }

        private async Task<SqliteDataReader> ExecuteReaderAsync(SqliteConnection conn, string sql, params SqliteParameter[] pars)
        {
            var cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            foreach (var p in pars) cmd.Parameters.Add(p);
            return await cmd.ExecuteReaderAsync();
        }
    }

    public class CharacterData
    {
        public string Name     { get; set; } = "";
        public int    Class    { get; set; }
        public int    Level    { get; set; } = 1;
        public int    MapId    { get; set; }
        public double PosX     { get; set; } = 135;
        public double PosZ     { get; set; } = 130;
        public int    Strength { get; set; } = 25;
        public int    Agility  { get; set; } = 20;
        public int    Vitality { get; set; } = 25;
        public int    Energy   { get; set; } = 10;
        public int    Hp       { get; set; } = 110;
        public int    MaxHp    { get; set; } = 110;
        public int    Mana     { get; set; } = 60;
        public int    MaxMana  { get; set; } = 60;
        public int    Zen          { get; set; }
        public int    Experience { get; set; }
    }

    public enum LoginResult { Success, InvalidAccount, WrongPassword, AlreadyConnected, Banned }
}
