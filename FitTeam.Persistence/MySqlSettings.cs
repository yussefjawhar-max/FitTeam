using System.Text.Json;
using MySqlConnector;

namespace FitTeam.Persistence;

public sealed class MySqlSettings
{
    public string Server { get; set; } = "127.0.0.1";
    public uint Port { get; set; } = 33071;
    public string Database { get; set; } = "fitteam";
    public string Username { get; set; } = "fitteam_app";
    public string Password { get; set; } = "";
    public bool RequireTls { get; set; }

    public string ConnectionString
    {
        get
        {
            if (!System.Text.RegularExpressions.Regex.IsMatch(Database, "^[A-Za-z][A-Za-z0-9_]{0,63}$"))
                throw new ArgumentException("Veritabanı adında yalnız harf, rakam ve alt çizgi kullanılabilir.");
            if (Server.Length == 0 || Username.Length == 0 || Password.Length == 0)
                throw new ArgumentException("MySQL sunucu, kullanıcı adı ve parola alanlarını doldurun.");
            if (!RequireTls && Server is not ("127.0.0.1" or "localhost" or "::1"))
                throw new ArgumentException("Uzak MySQL bağlantıları için TLS doğrulaması açılmalı.");
            return new MySqlConnectionStringBuilder
            {
                Server = Server, Port = Port, Database = Database, UserID = Username, Password = Password,
                SslMode = RequireTls ? MySqlSslMode.VerifyFull : MySqlSslMode.Preferred,
                ConnectionTimeout = 5, DefaultCommandTimeout = 20, CharacterSet = "utf8mb4",
                AllowUserVariables = false, Pooling = true, MaximumPoolSize = 8
            }.ConnectionString;
        }
    }

    public static MySqlSettings Load(string path)
        => JsonSerializer.Deserialize<MySqlSettings>(File.ReadAllText(path), new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
            ?? throw new InvalidDataException("MySQL ayar dosyası okunamadı.");

    public void Save(string path)
    {
        _ = ConnectionString;
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        var temp = path + ".tmp";
        File.WriteAllText(temp, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
        if (!OperatingSystem.IsWindows()) File.SetUnixFileMode(temp, UnixFileMode.UserRead | UnixFileMode.UserWrite);
        File.Move(temp, path, true);
    }
}
