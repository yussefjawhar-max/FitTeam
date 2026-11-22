using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using FitTeam.Core;
using MySqlConnector;

namespace FitTeam.Persistence;

/// <summary>İlişkisel MySQL deposu. Bir kaydetme işlemi tek InnoDB transaction içinde tamamlanır.</summary>
public sealed class MySqlAppRepository : IAppRepository
{
    private readonly string connectionString;
    private readonly object gate = new();
    private long revision;
    private Dictionary<string, Dictionary<string, string>> committed = new();
    private static readonly JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = true };
    private static readonly Dictionary<string, string> Collections = new()
    {
        ["Diary"] = "diary_entries", ["Measurements"] = "measurements", ["Reminders"] = "reminders",
        ["Notifications"] = "notifications", ["Messages"] = "messages", ["MealAssignments"] = "meal_assignments",
        ["PersonalMeals"] = "meal_plans", ["MealIntakes"] = "meal_intakes", ["Gyms"] = "gyms",
        ["GymMemberships"] = "gym_memberships", ["GymVisits"] = "gym_visits", ["Workouts"] = "workouts",
        ["Weights"] = "weight_entries", ["Foods"] = "food_entries", ["Water"] = "water_entries",
        ["Matches"] = "matches", ["Attendance"] = "attendance", ["Templates"] = "workout_templates",
        ["Drafts"] = "workout_drafts", ["Changes"] = "program_changes"
    };

    public string StorageId { get; }
    public MySqlAppRepository(MySqlSettings settings)
    {
        connectionString = settings.ConnectionString;
        StorageId = "MySQL • " + settings.Database;
    }

    public void EnsureSchema()
    {
        Run(() =>
        {
            using var connection = Open();
            using var stream = typeof(MySqlAppRepository).Assembly.GetManifestResourceStream("FitTeam.Persistence.Sql.schema.sql")!;
            using var text = new StreamReader(stream);
            using var cmd = new MySqlCommand(text.ReadToEnd(), connection);
            cmd.ExecuteNonQuery();
            foreach (var table in Collections.Values)
            {
                using var create = new MySqlCommand($"CREATE TABLE IF NOT EXISTS `{table}` (record_key VARCHAR(160) PRIMARY KEY, user_id CHAR(36) NULL, record_date DATE NULL, amount DECIMAL(12,3) NULL, payload JSON NOT NULL, ordinal_no INT NOT NULL, INDEX ix_user_date (user_id,record_date)) ENGINE=InnoDB", connection);
                create.ExecuteNonQuery();
            }
            using var init = new MySqlCommand("INSERT IGNORE INTO app_state(id,revision,metadata) VALUES(1,0,JSON_OBJECT())", connection);
            init.ExecuteNonQuery();
        });
    }

    public AppData? Load() => Run(() =>
    {
        lock (gate)
        {
            using var connection = Open();
            using var transaction = connection.BeginTransaction(System.Data.IsolationLevel.RepeatableRead);
            using var state = new MySqlCommand("SELECT revision, metadata FROM app_state WHERE id=1", connection, transaction);
            using (var reader = state.ExecuteReader())
            {
                if (!reader.Read()) throw new PersistenceException("Veritabanı şeması eksik. Bağlantı kurulumunu çalıştırın.");
                revision = reader.GetInt64(0);
                if (revision == 0) { reader.Close(); transaction.Commit(); committed = new(); return null; }
                var data = JsonSerializer.Deserialize<AppData>(reader.GetString(1), Json) ?? new AppData();
                reader.Close();
                data.Users = ReadUsers(connection, transaction);
                data.Teams = ReadPayloads<Team>("clubs", connection, transaction);
                data.Plans = ReadPlans(connection, transaction);
                foreach (var mapping in Collections)
                {
                    var property = typeof(AppData).GetProperty(mapping.Key)!;
                    var list = (IList)Activator.CreateInstance(property.PropertyType)!;
                    using var command = new MySqlCommand($"SELECT payload FROM `{mapping.Value}` ORDER BY ordinal_no", connection, transaction);
                    using var rows = command.ExecuteReader();
                    while (rows.Read()) list.Add(JsonSerializer.Deserialize(rows.GetString(0), property.PropertyType.GenericTypeArguments[0], Json)!);
                    property.SetValue(data, list);
                }
                transaction.Commit();
                committed = BuildRows(data);
                return data;
            }
        }
    });

    public User? FindUser(string identifier) => Run(() =>
    {
        using var connection = Open();
        using var command = new MySqlCommand("SELECT u.payload,u.username,u.full_name,u.role,u.email,u.phone,u.active,u.needs_approval,u.password_hash,u.salt,p.payload FROM users u JOIN profiles p ON p.user_id=u.id WHERE u.username_key=@key OR u.email_key=@key OR (u.phone<>'' AND u.phone=@value) LIMIT 1", connection);
        command.Parameters.AddWithValue("@key", Encoding.UTF8.GetBytes(identifier.ToUpperInvariant()));
        command.Parameters.AddWithValue("@value", identifier);
        using var row = command.ExecuteReader();
        return row.Read() ? UserFromRow(row) : null;
    });

    public void Save(AppData data) => Run(() =>
    {
        lock (gate)
        {
            var next = BuildRows(data);
            using var connection = Open();
            using var transaction = connection.BeginTransaction();
            using (var check = new MySqlCommand("SELECT revision FROM app_state WHERE id=1 FOR UPDATE", connection, transaction))
                if (Convert.ToInt64(check.ExecuteScalar()) != revision)
                    throw new PersistenceException("Kayıtlar başka bir FitTeam penceresinde değiştirildi. Güncel kayıtları almak için uygulamayı yeniden açın.");

            // Önce ebeveynler, sonra bağlı kayıtlar eklenir.
            foreach (var group in next)
                foreach (var row in group.Value)
                    if (!committed.TryGetValue(group.Key, out var old) || !old.TryGetValue(row.Key, out var value) || value != row.Value)
                        Upsert(group.Key, row.Key, row.Value, old?.ContainsKey(row.Key) == true, connection, transaction);

            var knownUsers = data.Users.Select(u => u.Id).ToHashSet();
            foreach (var user in data.Users)
            {
                var key = user.Id.ToString();
                if (committed.TryGetValue("users", out var previous) && previous.TryGetValue(key, out var previousUser)
                    && next["users"][key] == previousUser) continue;
                foreach (var coach in user.CoachIds.Concat(user.CoachId.HasValue ? new[] { user.CoachId.Value } : Array.Empty<Guid>()).Distinct().Where(knownUsers.Contains))
                {
                    using var link = new MySqlCommand("INSERT IGNORE INTO member_coaches(member_id,coach_id) VALUES(@member,@coach)", connection, transaction);
                    link.Parameters.AddWithValue("@member", key); link.Parameters.AddWithValue("@coach", coach.ToString()); link.ExecuteNonQuery();
                }
            }

            // Silme işlemleri bağlı kayıtlardan ebeveyne doğru yapılır.
            foreach (var group in committed.Reverse())
                foreach (var row in group.Value)
                    if (!next.TryGetValue(group.Key, out var current) || !current.ContainsKey(row.Key))
                    {
                        var key = group.Key switch { "users" or "clubs" or "plan_days" => "id", "profiles" or "training_plans" => "user_id", _ => "record_key" };
                        using var remove = new MySqlCommand($"DELETE FROM `{group.Key}` WHERE `{key}`=@id", connection, transaction);
                        remove.Parameters.AddWithValue("@id", row.Key);
                        remove.ExecuteNonQuery();
                    }
            var metadata = JsonSerializer.SerializeToNode(data)!;
            foreach (var property in typeof(AppData).GetProperties().Where(p => typeof(IList).IsAssignableFrom(p.PropertyType)))
                metadata.AsObject().Remove(property.Name);
            using var update = new MySqlCommand("UPDATE app_state SET revision=revision+1,metadata=@metadata WHERE id=1", connection, transaction);
            update.Parameters.AddWithValue("@metadata", metadata.ToJsonString());
            update.ExecuteNonQuery();
            transaction.Commit();
            revision++;
            committed = next;
        }
    });

    private static Dictionary<string, Dictionary<string, string>> BuildRows(AppData data)
    {
        var result = new Dictionary<string, Dictionary<string, string>>();
        void Add(string table, string key, object value)
        {
            if (!result.TryGetValue(table, out var rows)) result[table] = rows = new();
            if (!rows.TryAdd(key, JsonSerializer.Serialize(value))) throw new PersistenceException("Yinelenen kayıt kimliği bulundu; veri kaydedilmedi.");
        }
        for (int i = 0; i < data.Users.Count; i++)
        {
            var user = data.Users[i];
            Add("users", user.Id.ToString(), new { user, ordinal = i });
            Add("profiles", user.Id.ToString(), user.Profile);
        }
        for (int i = 0; i < data.Teams.Count; i++) Add("clubs", data.Teams[i].Id.ToString(), new { item = data.Teams[i], ordinal = i });
        for (int i = 0; i < data.Plans.Count; i++)
        {
            var plan = data.Plans[i];
            Add("training_plans", plan.UserId.ToString(), new { plan.UserId, plan.Start, ordinal = i });
            for (int d = 0; d < plan.Days.Count; d++) Add("plan_days", plan.Days[d].Id.ToString(), new { plan.UserId, item = plan.Days[d], ordinal = d });
        }
        foreach (var mapping in Collections)
        {
            result[mapping.Value] = new();
            var list = (IList)typeof(AppData).GetProperty(mapping.Key)!.GetValue(data)!;
            for (int i = 0; i < list.Count; i++) Add(mapping.Value, i.ToString("D10", CultureInfo.InvariantCulture), new { item = list[i], ordinal = i });
        }
        return result;
    }

    private static void Upsert(string table, string key, string serialized, bool exists, MySqlConnection connection, MySqlTransaction transaction)
    {
        using var document = JsonDocument.Parse(serialized);
        var root = document.RootElement;
        var values = new Dictionary<string, object?>();
        string? Value(JsonElement item, string name) => item.TryGetProperty(name, out var property) && property.ValueKind != JsonValueKind.Null ? property.ToString() : null;
        void Put(string name, object? value) => values[name] = value;
        if (table == "users")
        {
            var user = root.GetProperty("user").Deserialize<User>(Json)!;
            Put("id", key); Put("username", user.Username); Put("username_key", Encoding.UTF8.GetBytes(user.Username.ToUpperInvariant()));
            Put("full_name", user.Profile.Name); Put("role", user.Role); Put("email", user.Email.Length == 0 ? null : user.Email);
            Put("email_key", user.Email.Length == 0 ? null : Encoding.UTF8.GetBytes(user.Email.ToUpperInvariant()));
            Put("phone", user.Phone); Put("active", user.IsActive); Put("needs_approval", user.NeedsApproval);
            Put("password_hash", user.PasswordHash); Put("salt", user.Salt);
            var payload = JsonSerializer.SerializeToNode(user)!; payload.AsObject().Remove("Profile");
            Put("payload", payload.ToJsonString()); Put("ordinal_no", root.GetProperty("ordinal").GetInt32());
        }
        else if (table == "profiles")
        {
            var profile = root.Deserialize<Profile>(Json)!;
            Put("user_id", key); Put("age", profile.Age); Put("height_cm", profile.Height); Put("weight_kg", profile.Weight);
            Put("target_weight_kg", profile.TargetWeight); Put("goal", profile.Goal); Put("payload", serialized);
        }
        else if (table == "clubs")
        {
            var item = root.GetProperty("item").Deserialize<Team>(Json)!;
            Put("id", key); Put("name", item.Name); Put("coach_id", item.CoachId.ToString()); Put("sport", item.SportBranch);
            Put("active", item.IsActive); Put("payload", JsonSerializer.Serialize(item)); Put("ordinal_no", root.GetProperty("ordinal").GetInt32());
        }
        else if (table == "training_plans")
        {
            Put("user_id", key); Put("start_date", DateOnly.Parse(root.GetProperty("Start").GetString()!, CultureInfo.InvariantCulture).ToDateTime(TimeOnly.MinValue));
            Put("ordinal_no", root.GetProperty("ordinal").GetInt32());
        }
        else if (table == "plan_days")
        {
            var item = root.GetProperty("item").Deserialize<PlanDay>(Json)!;
            Put("id", key); Put("user_id", root.GetProperty("UserId").GetString()); Put("plan_date", item.Date.ToDateTime(TimeOnly.MinValue));
            Put("title", item.Title); Put("status", item.Status); Put("payload", JsonSerializer.Serialize(item)); Put("ordinal_no", root.GetProperty("ordinal").GetInt32());
        }
        else
        {
            var item = root.GetProperty("item");
            Put("record_key", key); Put("user_id", Value(item, "UserId") ?? Value(item, "OwnerId"));
            var date = Value(item, "Date");
            Put("record_date", DateOnly.TryParse(date, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed) ? parsed.ToDateTime(TimeOnly.MinValue) : null);
            var amount = Value(item, "Weight") ?? Value(item, "Litres") ?? Value(item, "CaloriesPerPortion");
            Put("amount", decimal.TryParse(amount, NumberStyles.Any, CultureInfo.InvariantCulture, out var number) ? number : null);
            Put("payload", item.GetRawText()); Put("ordinal_no", root.GetProperty("ordinal").GetInt32());
        }
        var columns = string.Join(",", values.Keys.Select(c => "`" + c + "`"));
        var parameters = string.Join(",", values.Keys.Select(c => "@" + c));
        var keyColumn = table switch { "users" or "clubs" or "plan_days" => "id", "profiles" or "training_plans" => "user_id", _ => "record_key" };
        var updates = string.Join(",", values.Keys.Where(c => c != keyColumn).Select(c => $"`{c}`=@{c}"));
        using var command = new MySqlCommand(exists
            ? $"UPDATE `{table}` SET {updates} WHERE `{keyColumn}`=@{keyColumn}"
            : $"INSERT INTO `{table}` ({columns}) VALUES ({parameters})", connection, transaction);
        foreach (var value in values) command.Parameters.AddWithValue("@" + value.Key, value.Value ?? DBNull.Value);
        command.ExecuteNonQuery();
        if (table == "users")
        {
            var user = root.GetProperty("user").Deserialize<User>(Json)!;
            using var clear = new MySqlCommand("DELETE FROM member_coaches WHERE member_id=@id", connection, transaction);
            clear.Parameters.AddWithValue("@id", key); clear.ExecuteNonQuery();
            // Bağlar bütün kullanıcılar eklendikten sonra tamamlanır; yükleme sırasında payload da korunur.
        }
        if (table == "plan_days")
        {
            using var clear = new MySqlCommand("DELETE FROM exercises WHERE day_id=@id", connection, transaction);
            clear.Parameters.AddWithValue("@id", key); clear.ExecuteNonQuery();
            var exercises = root.GetProperty("item").Deserialize<PlanDay>(Json)!.Exercises;
            for (int i = 0; i < exercises.Count; i++)
            {
                var exercise = exercises[i];
                using var add = new MySqlCommand("INSERT INTO exercises(day_id,sequence_no,name,muscle,sets,reps,minutes,rest_seconds) VALUES(@day,@number,@name,@muscle,@sets,@reps,@minutes,@rest)", connection, transaction);
                add.Parameters.AddWithValue("@day", key); add.Parameters.AddWithValue("@number", i); add.Parameters.AddWithValue("@name", exercise.Name);
                add.Parameters.AddWithValue("@muscle", exercise.Muscle); add.Parameters.AddWithValue("@sets", exercise.Sets); add.Parameters.AddWithValue("@reps", exercise.Reps);
                add.Parameters.AddWithValue("@minutes", exercise.Minutes); add.Parameters.AddWithValue("@rest", exercise.RestSeconds); add.ExecuteNonQuery();
            }
        }
    }

    private static List<User> ReadUsers(MySqlConnection connection, MySqlTransaction transaction)
    {
        using var command = new MySqlCommand("SELECT u.payload,u.username,u.full_name,u.role,u.email,u.phone,u.active,u.needs_approval,u.password_hash,u.salt,p.payload FROM users u JOIN profiles p ON p.user_id=u.id ORDER BY u.ordinal_no", connection, transaction);
        using var rows = command.ExecuteReader();
        var users = new List<User>(); while (rows.Read()) users.Add(UserFromRow(rows)); return users;
    }
    private static User UserFromRow(MySqlDataReader row)
    {
        var user = JsonSerializer.Deserialize<User>(row.GetString(0), Json)!;
        user.Username = row.GetString(1); user.Role = row.GetString(3); user.Email = row.IsDBNull(4) ? "" : row.GetString(4);
        user.Phone = row.GetString(5); user.IsActive = row.GetBoolean(6); user.NeedsApproval = row.GetBoolean(7);
        user.PasswordHash = row.GetString(8); user.Salt = row.GetString(9);
        user.Profile = JsonSerializer.Deserialize<Profile>(row.GetString(10), Json)!; user.Profile.Name = row.GetString(2);
        return user;
    }
    private static List<T> ReadPayloads<T>(string table, MySqlConnection connection, MySqlTransaction transaction)
    {
        using var command = new MySqlCommand($"SELECT payload FROM `{table}` ORDER BY ordinal_no", connection, transaction);
        using var rows = command.ExecuteReader(); var result = new List<T>();
        while (rows.Read()) result.Add(JsonSerializer.Deserialize<T>(rows.GetString(0), Json)!); return result;
    }
    private static List<TrainingPlan> ReadPlans(MySqlConnection connection, MySqlTransaction transaction)
    {
        var result = new List<TrainingPlan>();
        using (var command = new MySqlCommand("SELECT user_id,start_date FROM training_plans ORDER BY ordinal_no", connection, transaction))
        using (var rows = command.ExecuteReader())
            while (rows.Read()) result.Add(new TrainingPlan { UserId = rows.GetGuid(0), Start = DateOnly.FromDateTime(rows.GetDateTime(1)) });
        foreach (var plan in result)
        {
            using var command = new MySqlCommand("SELECT payload FROM plan_days WHERE user_id=@id ORDER BY ordinal_no", connection, transaction);
            command.Parameters.AddWithValue("@id", plan.UserId.ToString()); using var rows = command.ExecuteReader();
            while (rows.Read()) plan.Days.Add(JsonSerializer.Deserialize<PlanDay>(rows.GetString(0), Json)!);
        }
        return result;
    }
    private MySqlConnection Open() { var connection = new MySqlConnection(connectionString); try { connection.Open(); return connection; } catch { connection.Dispose(); throw; } }
    private static void Run(Action action) => Run(() => { action(); return true; });
    private static T Run<T>(Func<T> action)
    {
        try { return action(); }
        catch (MySqlException ex) when (ex.Number == 1062) { throw new PersistenceException("Bu kullanıcı adı veya e-posta zaten kayıtlı. Başka bir değer girin.", ex); }
        catch (MySqlException ex) { throw new PersistenceException("MySQL işlemi tamamlanamadı. Sunucunun açık olduğunu ve bağlantı ayarlarını kontrol edin. Kayıtlar silinmedi.", ex); }
    }
}
