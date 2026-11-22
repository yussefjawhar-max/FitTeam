using FitTeam.Core;
using FitTeam.Persistence;
using System.Text.Json;

namespace FitTeam.Desktop;

public static class CourseRuntime
{
    private sealed class LaunchSettings { public string? MySqlSettingsPath { get; set; } }
    public static MySqlSettings? DatabaseSettings { get; private set; }
    public static ProtectedSmtpSettings? EmailSettings { get; private set; }
    public static IEmailSender? EmailSender { get; private set; }
    public static IEmailDeliveryStore? MailStore { get; private set; }
    public static FitService Start()
    {
        string directory = Environment.GetEnvironmentVariable("FITTEAM_DATA_DIRECTORY")
            ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FitTeam");
        Directory.CreateDirectory(directory);
        string launchPath = Path.Combine(AppContext.BaseDirectory, "fitteam-launch.json");
        var launch = File.Exists(launchPath) ? JsonSerializer.Deserialize<LaunchSettings>(File.ReadAllText(launchPath)) : null;
        var connection = DatabaseBootstrap.Prepare(directory, Environment.GetEnvironmentVariable("FITTEAM_MYSQL_CONFIG") ?? launch?.MySqlSettingsPath);
        Environment.SetEnvironmentVariable("FITTEAM_MYSQL_CONFIG", connection.SettingsPath);
        Environment.SetEnvironmentVariable("FITTEAM_AUTH_DATA", Path.Combine(directory, "members.json"));
        DatabaseSettings = connection.Settings;
        var repository = new MySqlAppRepository(connection.Settings); repository.EnsureSchema();
        Passwords.CopyProtector = new LocalAccountPasswordProtector(Path.Combine(directory, "account-passwords.key"));
        var store = new DataStore(repository, Path.Combine(directory, "fitteam.json"));
        EmailSettings = new ProtectedSmtpSettings(Path.Combine(directory, "smtp-settings.json"));
        EmailSender = new SmtpEmailSender(EmailSettings.Load);
        MailStore = new MySqlMailStore(connection.Settings);
        return new FitService(store);
    }
}
