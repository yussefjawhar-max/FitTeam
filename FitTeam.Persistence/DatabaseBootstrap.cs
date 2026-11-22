using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using MySqlConnector;

namespace FitTeam.Persistence;

public sealed record DatabaseConnection(MySqlSettings Settings, string SettingsPath);

/// <summary>Paketle gelen gerçek MySQL'i yalnız loopback üzerinde, kullanıcıya ait klasörde başlatır.</summary>
public static class DatabaseBootstrap
{
    public static DatabaseConnection Prepare(string userDirectory, string? suppliedSettings = null)
    {
        if (suppliedSettings is { Length: > 0 })
        {
            var provided = MySqlSettings.Load(suppliedSettings);
            using var connection = new MySqlConnection(provided.ConnectionString); connection.Open();
            return new DatabaseConnection(provided, suppliedSettings);
        }
        string directory = Path.Combine(userDirectory, "Database");
        Directory.CreateDirectory(directory);
        if (!OperatingSystem.IsWindows()) File.SetUnixFileMode(directory, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        string path = Path.Combine(directory, "mysql.json");
        string lockName = "FitTeamMySql_" + Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(directory)))[..20];
        using var mutex = new Mutex(false, lockName);
        bool held = false;
        try
        {
            try { held = mutex.WaitOne(TimeSpan.FromSeconds(90)); } catch (AbandonedMutexException) { held = true; }
            if (!held) throw new InvalidOperationException("Veritabanı başka bir FitTeam penceresinde hazırlanıyor. Biraz sonra tekrar açın.");
            var engine = FindEngine();
            string dataDirectory = Path.Combine(directory, "data");
            if (File.Exists(path))
            {
                var existing = MySqlSettings.Load(path);
                if (!CanConnect(existing))
                {
                    if (existing.Server is not ("127.0.0.1" or "localhost"))
                        throw new InvalidOperationException("MySQL sunucusuna ulaşılamadı. Uzak sunucuyu ve bağlantı ayarlarını kontrol edin.");
                    StartServer(engine, dataDirectory, directory, existing.Port);
                    WaitForConnection(existing, TimeSpan.FromSeconds(45));
                }
                return new DatabaseConnection(existing, path);
            }
            string recovery = Path.Combine(directory, "mysql-pending.json");
            if (File.Exists(recovery))
            {
                var candidate = MySqlSettings.Load(recovery);
                if (CanConnect(candidate)) { File.Move(recovery, path); return new DatabaseConnection(candidate, path); }
                throw new InvalidOperationException("Önceki MySQL kurulumu tamamlanmamış. Veriler korunuyor; kurulum kaydını kontrol edin.");
            }
            if (Directory.Exists(dataDirectory))
                throw new InvalidOperationException("MySQL veri klasörü var ancak bağlantı ayarı bulunamadı. Kayıtları korumak için otomatik sıfırlama yapılmadı.");
            string staging = Path.Combine(directory, "initializing-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(staging);
            RunServerCommand(engine, ["--no-defaults", "--initialize-insecure", "--basedir=" + engine,
                "--datadir=" + staging, "--log-error=" + Path.Combine(directory, "mysql-setup.log")], TimeSpan.FromSeconds(90));
            Directory.Move(staging, dataDirectory);
            uint port = AvailablePort();
            StartServer(engine, dataDirectory, directory, port);
            var root = new MySqlConnectionStringBuilder { Server = "127.0.0.1", Port = port, UserID = "root", ConnectionTimeout = 2, SslMode = MySqlSslMode.Preferred };
            WaitForRoot(root.ConnectionString, TimeSpan.FromSeconds(45));
            var settings = new MySqlSettings { Port = port, Password = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)) };
            settings.Save(recovery);
            string rootPassword = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
            string rootPath = Path.Combine(directory, "mysql-root.key");
            File.WriteAllText(rootPath, rootPassword);
            if (!OperatingSystem.IsWindows()) File.SetUnixFileMode(rootPath, UnixFileMode.UserRead | UnixFileMode.UserWrite);
            using (var connection = new MySqlConnection(root.ConnectionString))
            {
                connection.Open();
                using var command = new MySqlCommand("CREATE DATABASE fitteam CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_as_ci; CREATE USER 'fitteam_app'@'127.0.0.1' IDENTIFIED BY @password; GRANT SELECT,INSERT,UPDATE,DELETE,CREATE,ALTER,INDEX,REFERENCES ON fitteam.* TO 'fitteam_app'@'127.0.0.1'; ALTER USER 'root'@'localhost' IDENTIFIED BY @rootPassword;", connection);
                command.Parameters.AddWithValue("@password", settings.Password); command.Parameters.AddWithValue("@rootPassword", rootPassword); command.ExecuteNonQuery();
            }
            WaitForConnection(settings, TimeSpan.FromSeconds(10));
            File.Move(recovery, path);
            return new DatabaseConnection(settings, path);
        }
        finally { if (held) mutex.ReleaseMutex(); }
    }

    private static string FindEngine()
    {
        var supplied = Environment.GetEnvironmentVariable("FITTEAM_MYSQL_ENGINE");
        string[] candidates = [supplied ?? "", Path.Combine(AppContext.BaseDirectory, "mysql"), Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "Resources", "mysql"))];
        string binary = OperatingSystem.IsWindows() ? "mysqld.exe" : "mysqld";
        return candidates.FirstOrDefault(p => p.Length > 0 && File.Exists(Path.Combine(p, "bin", binary)))
            ?? throw new InvalidOperationException("Bu pakette MySQL sunucusu bulunamadı. Veritabanı içeren güncel FitTeam masaüstü paketini açın.");
    }
    private static uint AvailablePort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0); listener.Start();
        var port = (uint)((IPEndPoint)listener.LocalEndpoint).Port; listener.Stop(); return port;
    }
    private static ProcessStartInfo StartInfo(string engine, IEnumerable<string> arguments)
    {
        var start = new ProcessStartInfo(Path.Combine(engine, "bin", OperatingSystem.IsWindows() ? "mysqld.exe" : "mysqld"))
        { UseShellExecute = false, CreateNoWindow = true, WorkingDirectory = engine };
        foreach (var argument in arguments) start.ArgumentList.Add(argument); return start;
    }
    private static void RunServerCommand(string engine, string[] arguments, TimeSpan timeout)
    {
        using var process = Process.Start(StartInfo(engine, arguments)) ?? throw new InvalidOperationException("MySQL kurulum işlemi başlatılamadı.");
        if (!process.WaitForExit((int)timeout.TotalMilliseconds))
        { process.Kill(true); throw new TimeoutException("MySQL kurulumu zamanında tamamlanamadı. Kurulum günlüğünü kontrol edin."); }
        if (process.ExitCode != 0) throw new InvalidOperationException("MySQL kurulumu tamamlanamadı. Veriler korunuyor; mysql-setup.log dosyasını kontrol edin.");
    }
    private static void StartServer(string engine, string data, string directory, uint port)
    {
        var arguments = new List<string> { "--no-defaults", "--basedir=" + engine, "--datadir=" + data,
            "--port=" + port, "--bind-address=127.0.0.1", "--mysqlx=0", "--log-error=" + Path.Combine(directory, "mysql-server.log"),
            "--pid-file=" + Path.Combine(directory, "mysql.pid"), "--socket=" + Path.Combine(directory, "mysql.sock") };
        if (!OperatingSystem.IsWindows()) arguments.Add("--daemonize");
        using var process = Process.Start(StartInfo(engine, arguments)) ?? throw new InvalidOperationException("MySQL sunucusu başlatılamadı.");
        if (!OperatingSystem.IsWindows() && (!process.WaitForExit(15000) || process.ExitCode != 0))
            throw new InvalidOperationException("MySQL sunucusu açılamadı. mysql-server.log dosyasını kontrol edin.");
    }
    private static bool CanConnect(MySqlSettings settings)
    {
        try { using var connection = new MySqlConnection(settings.ConnectionString); connection.Open(); return true; }
        catch (MySqlException) { return false; }
    }
    private static void WaitForConnection(MySqlSettings settings, TimeSpan timeout)
    {
        var until = DateTime.UtcNow + timeout;
        do { if (CanConnect(settings)) return; Thread.Sleep(200); } while (DateTime.UtcNow < until);
        throw new InvalidOperationException("MySQL bağlantısı kurulamadı. Sunucuyu ve bağlantı ayarlarını kontrol edin.");
    }
    private static void WaitForRoot(string connectionString, TimeSpan timeout)
    {
        var until = DateTime.UtcNow + timeout;
        do
        {
            try { using var connection = new MySqlConnection(connectionString); connection.Open(); return; }
            catch (MySqlException) { Thread.Sleep(200); }
        } while (DateTime.UtcNow < until);
        throw new InvalidOperationException("Yeni MySQL sunucusuna bağlanılamadı. Kurulum günlüğünü kontrol edin.");
    }
}
