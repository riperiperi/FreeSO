using System.Security.Cryptography;
using Npgsql;

namespace FSO.WsGateway
{
    /// <summary>
    /// Accounts for the hosted demo: a handful of friends who sign themselves up.
    /// Postgres (Neon) in production; a JSON file locally so the whole auth flow is
    /// testable without a database. Passwords are PBKDF2-hashed — not because the
    /// URL is a target, but because people reuse passwords.
    /// </summary>
    public interface IUserStore
    {
        Task InitAsync();
        Task<bool> ExistsAsync(string username);
        Task CreateAsync(string username, string passwordHash);
        Task<string> GetHashAsync(string username);
        Task<int> CountAsync();
    }

    public static class Passwords
    {
        const int Iterations = 100_000, SaltBytes = 16, HashBytes = 32;

        public static string Hash(string password)
        {
            var salt = RandomNumberGenerator.GetBytes(SaltBytes);
            var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, HashBytes);
            return $"pbkdf2${Iterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
        }

        public static bool Verify(string password, string stored)
        {
            if (string.IsNullOrEmpty(stored)) return false;
            var parts = stored.Split('$');
            if (parts.Length != 4 || parts[0] != "pbkdf2") return false;
            if (!int.TryParse(parts[1], out var iterations)) return false;
            var salt = Convert.FromBase64String(parts[2]);
            var expected = Convert.FromBase64String(parts[3]);
            var actual = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, expected.Length);
            return CryptographicOperations.FixedTimeEquals(actual, expected);
        }
    }

    public class PostgresUserStore : IUserStore
    {
        readonly string _connectionString;

        /// <summary>Accepts a Neon/Heroku style URL (postgres://user:pass@host/db) as well
        /// as a plain ADO connection string — Neon's dashboard hands out the former.</summary>
        public PostgresUserStore(string connectionStringOrUrl)
        {
            _connectionString = Normalize(connectionStringOrUrl);
        }

        public static string Normalize(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) throw new ArgumentException("empty connection string");
            if (!s.StartsWith("postgres://") && !s.StartsWith("postgresql://")) return s;
            var uri = new Uri(s);
            var creds = uri.UserInfo.Split(':', 2);
            var b = new NpgsqlConnectionStringBuilder
            {
                Host = uri.Host,
                Port = uri.Port > 0 ? uri.Port : 5432,
                Database = uri.AbsolutePath.TrimStart('/'),
                Username = Uri.UnescapeDataString(creds[0]),
                Password = creds.Length > 1 ? Uri.UnescapeDataString(creds[1]) : "",
                SslMode = SslMode.Require,
            };
            // Neon requires SSL and passes options like ?sslmode=require&channel_binding=…
            return b.ConnectionString;
        }

        async Task<NpgsqlConnection> OpenAsync()
        {
            var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();
            return conn;
        }

        public async Task InitAsync()
        {
            using var conn = await OpenAsync();
            using var cmd = new NpgsqlCommand(
                "CREATE TABLE IF NOT EXISTS users (" +
                " username TEXT PRIMARY KEY," +
                " password_hash TEXT NOT NULL," +
                " created_at TIMESTAMPTZ NOT NULL DEFAULT now())", conn);
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task<bool> ExistsAsync(string username)
        {
            using var conn = await OpenAsync();
            using var cmd = new NpgsqlCommand("SELECT 1 FROM users WHERE username = @u", conn);
            cmd.Parameters.AddWithValue("u", username);
            return await cmd.ExecuteScalarAsync() != null;
        }

        public async Task CreateAsync(string username, string passwordHash)
        {
            using var conn = await OpenAsync();
            using var cmd = new NpgsqlCommand(
                "INSERT INTO users (username, password_hash) VALUES (@u, @h)", conn);
            cmd.Parameters.AddWithValue("u", username);
            cmd.Parameters.AddWithValue("h", passwordHash);
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task<string> GetHashAsync(string username)
        {
            using var conn = await OpenAsync();
            using var cmd = new NpgsqlCommand("SELECT password_hash FROM users WHERE username = @u", conn);
            cmd.Parameters.AddWithValue("u", username);
            return (string)await cmd.ExecuteScalarAsync();
        }

        public async Task<int> CountAsync()
        {
            using var conn = await OpenAsync();
            using var cmd = new NpgsqlCommand("SELECT count(*) FROM users", conn);
            return Convert.ToInt32(await cmd.ExecuteScalarAsync());
        }
    }

    /// <summary>Local-development store: same behaviour, a JSON file instead of Neon,
    /// so signup/login can be exercised end to end without a database.</summary>
    public class FileUserStore : IUserStore
    {
        readonly string _path;
        readonly Dictionary<string, string> _users = new(StringComparer.OrdinalIgnoreCase);
        readonly SemaphoreSlim _lock = new(1, 1);

        public FileUserStore(string path) { _path = path; }

        public Task InitAsync()
        {
            if (File.Exists(_path))
            {
                foreach (var line in File.ReadAllLines(_path))
                {
                    var i = line.IndexOf('\t');
                    if (i > 0) _users[line[..i]] = line[(i + 1)..];
                }
            }
            return Task.CompletedTask;
        }

        public Task<bool> ExistsAsync(string username) => Task.FromResult(_users.ContainsKey(username));

        public async Task CreateAsync(string username, string passwordHash)
        {
            await _lock.WaitAsync();
            try
            {
                _users[username] = passwordHash;
                Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(_path)));
                await File.WriteAllLinesAsync(_path, _users.Select(kv => kv.Key + "\t" + kv.Value));
            }
            finally { _lock.Release(); }
        }

        public Task<string> GetHashAsync(string username) =>
            Task.FromResult(_users.TryGetValue(username, out var h) ? h : null);

        public Task<int> CountAsync() => Task.FromResult(_users.Count);
    }
}
