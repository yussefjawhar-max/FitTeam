using MySqlConnector;

namespace FitTeam.Persistence;

public sealed record DatabaseTable(string Name, long Rows);
public sealed record DatabaseColumn(string Name, string Type, string Nullable, string Key);
public sealed record DatabaseSample(IReadOnlyList<string> Columns, IReadOnlyList<Dictionary<string, string>> Rows);

/// <summary>Yalnız izin verilen, parola içermeyen sütunları gösteren salt okunur yönetim görünümü.</summary>
public sealed class DatabaseInspector(MySqlSettings settings)
{
    private static readonly Dictionary<string, string[]> VisibleColumns = new()
    {
        ["users"] = ["id","username","full_name","role","email","active","needs_approval"],
        ["profiles"] = ["user_id","age","height_cm","weight_kg","target_weight_kg","goal"],
        ["member_coaches"] = ["member_id","coach_id"],
        ["clubs"] = ["id","name","coach_id","sport","active"],
        ["training_plans"] = ["user_id","start_date"],
        ["plan_days"] = ["id","user_id","plan_date","title","status"],
        ["exercises"] = ["day_id","sequence_no","name","muscle","sets","reps","minutes"],
        ["measurements"] = ["record_key","user_id","record_date","amount"],
        ["weight_entries"] = ["record_key","user_id","record_date","amount"],
        ["email_deliveries"] = ["id","recipient","subject","status","sent_at"]
    };
    public IReadOnlyList<DatabaseTable> Tables()
    {
        using var connection = new MySqlConnection(settings.ConnectionString); connection.Open();
        var result = new List<DatabaseTable>();
        foreach (var table in VisibleColumns.Keys)
        {
            using var command = new MySqlCommand($"SELECT COUNT(*) FROM `{table}`", connection);
            result.Add(new DatabaseTable(table, Convert.ToInt64(command.ExecuteScalar())));
        }
        return result;
    }
    public IReadOnlyList<DatabaseColumn> Columns(string table)
    {
        if (!VisibleColumns.ContainsKey(table)) throw new ArgumentException("Bu tablo görüntülenemez.");
        using var connection = new MySqlConnection(settings.ConnectionString); connection.Open();
        using var command = new MySqlCommand("SELECT COLUMN_NAME,COLUMN_TYPE,IS_NULLABLE,COLUMN_KEY FROM information_schema.COLUMNS WHERE TABLE_SCHEMA=@database AND TABLE_NAME=@table ORDER BY ORDINAL_POSITION", connection);
        command.Parameters.AddWithValue("@database", settings.Database); command.Parameters.AddWithValue("@table", table);
        using var rows = command.ExecuteReader(); var result = new List<DatabaseColumn>();
        while (rows.Read()) result.Add(new DatabaseColumn(rows.GetString(0), rows.GetString(1), rows.GetString(2), rows.GetString(3)));
        return result;
    }
    public DatabaseSample Sample(string table)
    {
        if (!VisibleColumns.TryGetValue(table, out var columns)) throw new ArgumentException("Bu tablo görüntülenemez.");
        using var connection = new MySqlConnection(settings.ConnectionString); connection.Open();
        using var command = new MySqlCommand($"SELECT {string.Join(",", columns.Select(c => "`" + c + "`"))} FROM `{table}` LIMIT 50", connection);
        using var rows = command.ExecuteReader(); var result = new List<Dictionary<string, string>>();
        while (rows.Read())
        {
            var row = new Dictionary<string, string>();
            for (int i = 0; i < columns.Length; i++) row[columns[i]] = rows.IsDBNull(i) ? "" : rows.GetValue(i) is DateTime date ? date.ToString("dd.MM.yyyy HH:mm") : Convert.ToString(rows.GetValue(i), System.Globalization.CultureInfo.InvariantCulture) ?? "";
            result.Add(row);
        }
        return new DatabaseSample(columns, result);
    }
}
