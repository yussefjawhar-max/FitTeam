using System.Net;
using System.Net.Mail;
using System.Text;
using System.Text.Json;
using FitTeam.Core;

namespace FitTeam.Persistence;

public sealed class SmtpSettings
{
    public string Host { get; set; } = "smtp.gmail.com";
    public int Port { get; set; } = 587;
    public string Sender { get; set; } = "";
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
    public bool EnableTls { get; set; } = true;
    public void Validate()
    {
        _ = EmailAddress.Validate(Sender);
        if (Host.Trim().Length == 0 || Host.IndexOfAny(['\r', '\n', '/', '\\']) >= 0 || Port is < 1 or > 65535)
            throw new ArgumentException("SMTP sunucu adresini ve portunu kontrol edin.");
        if (Port == 465) throw new ArgumentException("Bu bağlantı STARTTLS kullanır. E-posta hizmetinin STARTTLS portunu seçin; Gmail için 587.");
        bool local = Host is "localhost" or "127.0.0.1" or "::1";
        if (!EnableTls && !local) throw new ArgumentException("E-posta gönderimi için TLS açık olmalı.");
        if (!local && (Username.Length == 0 || Password.Length == 0)) throw new ArgumentException("SMTP kullanıcı adını ve uygulama parolasını doldurun.");
    }
}

/// <summary>Gerçek SMTP gönderimi. Sunucu kabulü ile alıcı kutusuna teslim ayrı durumlardır.</summary>
public sealed class SmtpEmailSender(Func<SmtpSettings?> loadSettings) : IEmailSender
{
    public bool IsConfigured => loadSettings() != null;
    public async Task<EmailSendReceipt> Send(OutgoingEmail message, CancellationToken cancellation = default)
    {
        var settings = loadSettings() ?? throw new EmailDeliveryException("E-posta hizmeti kurulmamış. Yetkili, E-posta ayarları sayfasından SMTP bağlantısını tamamlamalı.");
        settings.Validate();
        var recipient = EmailAddress.Validate(message.Recipient);
        if (message.Subject.Length is < 1 or > 500 || message.Subject.IndexOfAny(['\r', '\n']) >= 0)
            throw new ArgumentException("E-posta konusu geçersiz.");
        string messageId = "<" + Guid.NewGuid().ToString("N") + "@" + new MailAddress(settings.Sender).Host + ">";
        using var mail = new MailMessage(new MailAddress(settings.Sender, "FitTeam"), new MailAddress(recipient))
        {
            Subject = message.Subject, Body = message.Text, IsBodyHtml = false,
            SubjectEncoding = Encoding.UTF8, BodyEncoding = Encoding.UTF8
        };
        mail.Headers.Add("Message-ID", messageId);
        using var client = new SmtpClient(settings.Host.Trim(), settings.Port)
        {
            EnableSsl = settings.EnableTls, DeliveryMethod = SmtpDeliveryMethod.Network,
            UseDefaultCredentials = false, Timeout = 20000
        };
        if (settings.Username.Length > 0) client.Credentials = new NetworkCredential(settings.Username, settings.Password);
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellation);
        deadline.CancelAfter(TimeSpan.FromSeconds(25));
        try
        {
            await client.SendMailAsync(mail, deadline.Token);
            return new EmailSendReceipt(messageId, DateTimeOffset.UtcNow);
        }
        catch (SmtpFailedRecipientException ex) { throw new EmailDeliveryException("E-posta sunucusu alıcı adresini kabul etmedi. Adresi kontrol edin.", ex); }
        catch (SmtpException ex) { throw new EmailDeliveryException("E-posta gönderilemedi. SMTP hesabını, uygulama parolasını, TLS ayarını ve internet bağlantısını kontrol edin.", ex); }
        catch (OperationCanceledException ex) when (!cancellation.IsCancellationRequested) { throw new EmailDeliveryException("E-posta sunucusu zamanında yanıt vermedi. Tekrar deneyin.", ex); }
    }
}

/// <summary>SMTP parolası kaynak koduna yazılmaz; yerel dosyada korumalı kopya tutulur.</summary>
public sealed class ProtectedSmtpSettings(string path)
{
    private static readonly Guid ProtectionId = new("e8adb54a-64ca-4af2-93f7-cf8880c69263");
    private sealed class SavedSettings
    {
        public SmtpSettings Connection { get; set; } = new();
        public string ProtectedPassword { get; set; } = "";
    }
    private LocalAccountPasswordProtector Protector => new(Path.Combine(Path.GetDirectoryName(Path.GetFullPath(path))!, "smtp-settings.key"));
    public SmtpSettings? Load()
    {
        if (!File.Exists(path)) return null;
        var saved = JsonSerializer.Deserialize<SavedSettings>(File.ReadAllText(path)) ?? throw new InvalidDataException("E-posta ayarları okunamadı.");
        saved.Connection.Password = Protector.Unprotect(ProtectionId, saved.ProtectedPassword) ?? throw new InvalidDataException("E-posta parolası bu bilgisayarda açılamadı. Bağlantıyı yeniden kurun.");
        return saved.Connection;
    }
    public void Save(SmtpSettings settings)
    {
        settings.Validate();
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        string protectedPassword = Protector.Protect(ProtectionId, settings.Password);
        var copy = JsonSerializer.Deserialize<SmtpSettings>(JsonSerializer.Serialize(settings))!;
        copy.Password = "";
        string temporary = path + ".tmp";
        File.WriteAllText(temporary, JsonSerializer.Serialize(new SavedSettings { Connection = copy, ProtectedPassword = protectedPassword }));
        if (!OperatingSystem.IsWindows()) File.SetUnixFileMode(temporary, UnixFileMode.UserRead | UnixFileMode.UserWrite);
        File.Move(temporary, path, true);
    }
}
