using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using FitTeam.Controllers;
using FitTeam.Core;
using FitTeam.Persistence;

internal static class EmailControllerChecks
{
    public static async Task Run(FitService model, Action<bool, string> check)
    {
        await using var smtp = new LocalSmtpFixture();
        var settings = new SmtpSettings { Host = "127.0.0.1", Port = smtp.Port, EnableTls = false, Sender = "fitteam@example.test" };
        var sender = new SmtpEmailSender(() => settings);
        var audit = new MemoryMailAudit();
        var controller = new FitTeamController(model, sender, audit);
        bool rejected = false;
        try { controller.Login("", ""); } catch (ArgumentException) { rejected = true; }
        check(rejected, "Controller boş giriş alanlarını reddediyor");
        rejected = false;
        try { await controller.Email.SendCode("yanlis-adres"); } catch (ArgumentException) { rejected = true; }
        check(rejected && smtp.Messages.Count == 0, "Hatalı e-posta için SMTP isteği yapılmıyor");
        var signup = new SignupApplication
        {
            Username = "mail_" + Guid.NewGuid().ToString("N")[..8], FirstName = "Eposta", LastName = "Test",
            Age = "22", Sport = "Fitness", Experience = "0", EmailAddress = "member@example.test",
            Gender = "Erkek", Country = "Türkiye", CountryCode = "TR", City = "İstanbul",
            HomeAddress = "Örnek test adresi", Password = "MailFixture123!", PasswordConfirmation = "MailFixture123!",
            TrainingStart = new TimeOnly(18, 0), CoachIds = [model.Data.Users.First(u => u.Role == Choices.Roles[2]).Id]
        };
        int userCount = model.Data.Users.Count;
        rejected = false;
        try { controller.Accounts.Register(signup); } catch (ArgumentException) { rejected = true; }
        check(rejected && model.Data.Users.Count == userCount, "Doğrulanmamış e-posta ile hesap oluşturulmuyor");
        string request = await controller.Email.SendCode("member@example.test");
        check(smtp.Messages.Count == 1 && audit.Records.Single().Status == "SMTP kabul etti", "C# SMTP gönderimi ve başarılı gönderim kaydı çalışıyor (yerel test sunucusu)");
        string body = smtp.Messages.Single();
        if (body.Contains("Content-Transfer-Encoding: base64", StringComparison.OrdinalIgnoreCase))
            body = Encoding.UTF8.GetString(Convert.FromBase64String(body.Split("\r\n\r\n", 2)[1].Trim()));
        string code = Regex.Match(body, @"\b[0-9]{6}\b").Value;
        check(code.Length == 6, "Gerçek SMTP mesajının gövdesinde altı haneli doğrulama kodu var");
        rejected = false;
        try { controller.Email.VerifyCode("member@example.test", request, code == "000000" ? "000001" : "000000"); }
        catch (InvalidOperationException) { rejected = true; }
        check(rejected && !controller.Email.IsVerified("member@example.test"), "Yanlış kod adresi doğrulamıyor");
        controller.Email.VerifyCode("member@example.test", request, code);
        check(controller.Email.IsVerified("member@example.test"), "Doğru kod yalnız ilgili e-posta adresini doğruluyor");
        rejected = false;
        try { controller.Email.VerifyCode("member@example.test", request, code); } catch (InvalidOperationException) { rejected = true; }
        check(rejected, "Tüketilmiş e-posta kodu tekrar kullanılamıyor");
        var registered = controller.Accounts.Register(signup);
        var mysqlSettings = MySqlSettings.Load(Environment.GetEnvironmentVariable("FITTEAM_MYSQL_CONFIG")!);
        var persisted = new DataStore(new MySqlAppRepository(mysqlSettings));
        check(persisted.Data.Users.Single(u => u.Id == registered.Id).EmailVerified,
            "Kodla doğrulanan hesap doğrulama bilgisiyle birlikte MySQL'e kaydediliyor");
        check(!controller.Email.IsVerified(signup.Email), "Kayıt sonrası doğrulama yetkisi tüketiliyor");
        var loginModel = new FitService(persisted);
        check(loginModel.Login(signup.Username, signup.Password), "Doğrulanmış yeni üye yeniden açılan uygulamada giriş yapabiliyor");
        model.Data.Users.RemoveAll(u => u.Id == registered.Id);
        model.Data.Weights.RemoveAll(w => w.UserId == registered.Id);
        model.Save();
        rejected = false;
        try { await controller.Email.SendTest("member@example.test"); } catch (InvalidOperationException) { rejected = true; }
        check(rejected, "Üye, yönetici test e-postası işlemine erişemiyor");
        rejected = false;
        try { new SmtpSettings { Host = "smtp.example.com", Sender = "sender@example.com", EnableTls = false }.Validate(); }
        catch (ArgumentException) { rejected = true; }
        check(rejected, "Uzak SMTP bağlantısında TLS kapatılamıyor");
        var unavailable = new FitTeamController(model);
        rejected = false;
        try { await unavailable.Email.SendCode("member@example.test"); } catch (EmailDeliveryException) { rejected = true; }
        check(rejected, "Kurulmamış e-posta hizmeti başarı gibi gösterilmiyor");
    }

    private sealed class MemoryMailAudit : IEmailDeliveryStore
    {
        public List<EmailDeliveryRecord> Records { get; } = [];
        public IReadOnlyList<EmailDeliveryRecord> Deliveries(int limit = 100) => Records.Take(limit).ToList();
        public void SaveDelivery(EmailDeliveryRecord record) => Records.Add(record);
    }

    private sealed class LocalSmtpFixture : IAsyncDisposable
    {
        private readonly TcpListener listener = new(IPAddress.Loopback, 0);
        private readonly CancellationTokenSource stop = new();
        private readonly Task worker;
        public int Port { get; }
        public List<string> Messages { get; } = [];
        public LocalSmtpFixture()
        {
            listener.Start(); Port = ((IPEndPoint)listener.LocalEndpoint).Port;
            worker = Task.Run(Run);
        }
        private async Task Run()
        {
            try
            {
                while (!stop.IsCancellationRequested)
                {
                    using var client = await listener.AcceptTcpClientAsync(stop.Token);
                    await using var stream = client.GetStream();
                    using var reader = new StreamReader(stream, Encoding.ASCII, leaveOpen: true);
                    await using var writer = new StreamWriter(stream, Encoding.ASCII, leaveOpen: true) { NewLine = "\r\n", AutoFlush = true };
                    await writer.WriteLineAsync("220 localhost FitTeam TEST SMTP");
                    while (await reader.ReadLineAsync(stop.Token) is { } line)
                    {
                        if (line.StartsWith("EHLO", StringComparison.OrdinalIgnoreCase)) await writer.WriteLineAsync("250-localhost\r\n250 SIZE 1000000");
                        else if (line.StartsWith("HELO", StringComparison.OrdinalIgnoreCase)) await writer.WriteLineAsync("250 localhost");
                        else if (line.Equals("DATA", StringComparison.OrdinalIgnoreCase))
                        {
                            await writer.WriteLineAsync("354 End data with <CRLF>.<CRLF>");
                            var content = new StringBuilder();
                            while (await reader.ReadLineAsync(stop.Token) is { } row && row != ".") content.Append(row.StartsWith("..") ? row[1..] : row).Append("\r\n");
                            Messages.Add(content.ToString()); await writer.WriteLineAsync("250 2.0.0 Test message accepted");
                        }
                        else if (line.Equals("QUIT", StringComparison.OrdinalIgnoreCase)) { await writer.WriteLineAsync("221 Bye"); break; }
                        else await writer.WriteLineAsync("250 2.1.0 OK");
                    }
                }
            }
            catch (OperationCanceledException) { }
            catch (SocketException) when (stop.IsCancellationRequested) { }
        }
        public async ValueTask DisposeAsync() { stop.Cancel(); listener.Stop(); await worker; stop.Dispose(); }
    }
}
