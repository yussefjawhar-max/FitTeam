using FitTeam.Core;

namespace FitTeam.Controllers;

public sealed class EmailVerificationController : ControllerBase
{
    private readonly IEmailSender sender;
    private readonly IEmailDeliveryStore? audit;
    private readonly CodeChallenges challenges = new();
    private readonly Dictionary<string, DateTimeOffset> verified = new(StringComparer.OrdinalIgnoreCase);
    public bool IsConfigured => sender.IsConfigured;
    public EmailVerificationController(FitService model, IEmailSender sender, IEmailDeliveryStore? audit = null) : base(model)
    { this.sender = sender; this.audit = audit; }

    public async Task<string> SendCode(string address, CancellationToken cancellation = default)
    {
        if (Model.IsPreview) throw new InvalidOperationException("Örnek pencereden e-posta gönderilemez. Asıl uygulamayı kullanın.");
        address = EmailAddress.Validate(address);
        if (!sender.IsConfigured) throw new EmailDeliveryException("E-posta hizmeti kurulmamış. Yetkili SMTP bağlantısını tamamlamalı.");
        return await challenges.Send(address, async code =>
        {
            var record = new EmailDeliveryRecord { Recipient = address, Subject = "FitTeam e-posta doğrulama kodunuz", Purpose = "Adres doğrulama" };
            try
            {
                var result = await sender.Send(new OutgoingEmail(address, record.Subject,
                    "FitTeam e-posta doğrulama kodunuz: " + code + "\n\nBu kod 2 dakika geçerlidir.\nBu isteği siz yapmadıysanız mesajı dikkate almayın.\nFitTeam şifrenizi kimseyle paylaşmayın.", record.Purpose), cancellation);
                record.Status = "SMTP kabul etti"; record.MessageId = result.MessageId; record.SentAt = result.AcceptedAt;
                audit?.SaveDelivery(record);
            }
            catch (Exception error)
            {
                record.Status = "Başarısız";
                record.ErrorMessage = error is EmailDeliveryException ? error.Message : "Gönderim tamamlanamadı.";
                audit?.SaveDelivery(record);
                throw;
            }
        });
    }
    public void VerifyCode(string address, string requestId, string code)
    {
        address = EmailAddress.Validate(address);
        challenges.Verify(address, requestId, LoginContact.NormalizeDigits(code.Trim()));
        verified[address] = DateTimeOffset.UtcNow.AddMinutes(20);
    }
    public bool IsVerified(string address)
    {
        address = EmailAddress.Validate(address);
        return verified.TryGetValue(address, out var until) && until > DateTimeOffset.UtcNow;
    }
    internal void ConsumeVerification(string address) => verified.Remove(EmailAddress.Validate(address));

    public async Task<EmailDeliveryRecord> SendTest(string address, CancellationToken cancellation = default)
    {
        if (Model.IsPreview) throw new InvalidOperationException("Örnek panelden gerçek e-posta gönderilemez.");
        RequireAdministration();
        var record = new EmailDeliveryRecord { UserId = RequireUser().Id, Recipient = EmailAddress.Validate(address), Subject = "FitTeam gerçek e-posta bağlantı testi", Purpose = "Bağlantı testi" };
        try
        {
            var receipt = await sender.Send(new OutgoingEmail(record.Recipient, record.Subject,
                "FitTeam masaüstü uygulamasının gerçek e-posta gönderim testi tamamlandı.\nGönderim zamanı: " + DateTimeOffset.Now.ToString("dd.MM.yyyy HH:mm zzz")
                + "\nBu mesaj C# uygulamasından SMTP üzerinden gönderildi. Herhangi bir parola içermez.", record.Purpose, record.UserId), cancellation);
            record.Status = "SMTP kabul etti"; record.MessageId = receipt.MessageId; record.SentAt = receipt.AcceptedAt;
            audit?.SaveDelivery(record); return record;
        }
        catch (Exception error)
        {
            record.Status = "Başarısız"; record.ErrorMessage = error is EmailDeliveryException ? error.Message : "Gönderim tamamlanamadı.";
            audit?.SaveDelivery(record); throw;
        }
    }
    public IReadOnlyList<EmailDeliveryRecord> DeliveryHistory() { RequireAdministration(); return audit?.Deliveries() ?? []; }
}

internal sealed class UnconfiguredEmailSender : IEmailSender
{
    public bool IsConfigured => false;
    public Task<EmailSendReceipt> Send(OutgoingEmail message, CancellationToken cancellation = default)
        => throw new EmailDeliveryException("E-posta hizmeti kurulmamış. Yetkili SMTP bağlantısını tamamlamalı.");
}
