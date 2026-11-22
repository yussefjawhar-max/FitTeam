namespace FitTeam.Core;

public sealed record OutgoingEmail(string Recipient, string Subject, string Text, string Purpose, Guid? UserId = null);
public sealed record EmailSendReceipt(string MessageId, DateTimeOffset AcceptedAt);
public sealed class EmailDeliveryRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? UserId { get; set; }
    public string Recipient { get; set; } = "";
    public string Subject { get; set; } = "";
    public string Purpose { get; set; } = "";
    public string Status { get; set; } = "Bekliyor";
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? SentAt { get; set; }
    public string MessageId { get; set; } = "";
    public string ErrorMessage { get; set; } = "";
}
public interface IEmailSender
{
    bool IsConfigured { get; }
    Task<EmailSendReceipt> Send(OutgoingEmail message, CancellationToken cancellation = default);
}
public interface IEmailDeliveryStore
{
    void SaveDelivery(EmailDeliveryRecord record);
    IReadOnlyList<EmailDeliveryRecord> Deliveries(int limit = 100);
}
public sealed class EmailDeliveryException(string message, Exception? inner = null) : Exception(message, inner);

public static class EmailAddress
{
    public static string Validate(string input)
    {
        var value = input.Trim().ToLowerInvariant();
        if (value.Length > 254 || value.IndexOfAny(['\r', '\n']) >= 0
            || !System.Net.Mail.MailAddress.TryCreate(value, out var address)
            || address.Address != value || !address.Host.Contains('.'))
            throw new ArgumentException("Geçerli bir e-posta adresi yazın.");
        return value;
    }
}
