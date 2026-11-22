using FitTeam.Core;
using MySqlConnector;

namespace FitTeam.Persistence;

public sealed class MySqlMailStore(MySqlSettings settings) : IEmailDeliveryStore
{
    public void SaveDelivery(EmailDeliveryRecord record)
    {
        using var connection = new MySqlConnection(settings.ConnectionString); connection.Open();
        using var command = new MySqlCommand("INSERT INTO email_deliveries(id,user_id,recipient,subject,purpose,status,created_at,sent_at,message_id,error_message) VALUES(@id,@user,@to,@subject,@purpose,@status,@created,@sent,@message,@error)", connection);
        command.Parameters.AddWithValue("@id", record.Id.ToString()); command.Parameters.AddWithValue("@user", record.UserId?.ToString());
        command.Parameters.AddWithValue("@to", record.Recipient); command.Parameters.AddWithValue("@subject", record.Subject);
        command.Parameters.AddWithValue("@purpose", record.Purpose); command.Parameters.AddWithValue("@status", record.Status);
        command.Parameters.AddWithValue("@created", record.CreatedAt.UtcDateTime); command.Parameters.AddWithValue("@sent", record.SentAt?.UtcDateTime);
        command.Parameters.AddWithValue("@message", record.MessageId); command.Parameters.AddWithValue("@error", record.ErrorMessage);
        command.ExecuteNonQuery();
    }
    public IReadOnlyList<EmailDeliveryRecord> Deliveries(int limit = 100)
    {
        using var connection = new MySqlConnection(settings.ConnectionString); connection.Open();
        using var command = new MySqlCommand("SELECT id,user_id,recipient,subject,purpose,status,created_at,sent_at,message_id,error_message FROM email_deliveries ORDER BY created_at DESC LIMIT @limit", connection);
        command.Parameters.AddWithValue("@limit", Math.Clamp(limit, 1, 200));
        using var rows = command.ExecuteReader(); var result = new List<EmailDeliveryRecord>();
        while (rows.Read()) result.Add(new EmailDeliveryRecord
        {
            Id = rows.GetGuid(0), UserId = rows.IsDBNull(1) ? null : rows.GetGuid(1), Recipient = rows.GetString(2), Subject = rows.GetString(3),
            Purpose = rows.GetString(4), Status = rows.GetString(5), CreatedAt = new DateTimeOffset(DateTime.SpecifyKind(rows.GetDateTime(6), DateTimeKind.Utc)),
            SentAt = rows.IsDBNull(7) ? null : new DateTimeOffset(DateTime.SpecifyKind(rows.GetDateTime(7), DateTimeKind.Utc)),
            MessageId = rows.GetString(8), ErrorMessage = rows.GetString(9)
        });
        return result;
    }
}

public sealed class MySqlIdentityStore(MySqlSettings settings)
{
    public string? Load()
    {
        using var connection = new MySqlConnection(settings.ConnectionString); connection.Open();
        using var command = new MySqlCommand("SELECT payload FROM identity_state WHERE id=1", connection);
        return command.ExecuteScalar() as string;
    }
    public void Save(string json)
    {
        using var connection = new MySqlConnection(settings.ConnectionString); connection.Open();
        using var command = new MySqlCommand("INSERT INTO identity_state(id,payload) VALUES(1,@payload) ON DUPLICATE KEY UPDATE payload=@payload", connection);
        command.Parameters.AddWithValue("@payload", json); command.ExecuteNonQuery();
    }
}
