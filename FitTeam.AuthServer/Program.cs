using System.Collections.Concurrent;
using System.Net;
using System.Security.Cryptography;
using System.Text.Json;
using FitTeam.Core;
using FitTeam.Persistence;

// Bu hizmet yalnız yerel başkan kullanıcı adı/parola girişini sağlar.
var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();
var app = builder.Build();
var gate = new object();
var sessions = new ConcurrentDictionary<string, Session>();
var setupToken = Environment.GetEnvironmentVariable("FITTEAM_SETUP_TOKEN") ?? Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
var path = Environment.GetEnvironmentVariable("FITTEAM_AUTH_DATA") ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FitTeam", "members.json");
Passwords.CopyProtector = new LocalAccountPasswordProtector(Path.Combine(Path.GetDirectoryName(Path.GetFullPath(path))!, "account-passwords.key"));
var databaseConfig = Environment.GetEnvironmentVariable("FITTEAM_MYSQL_CONFIG");
var database = databaseConfig is { Length: > 0 } ? new MySqlIdentityStore(MySqlSettings.Load(databaseConfig)) : null;
var savedIdentity = database?.Load();
var db = JsonSerializer.Deserialize<MemberDatabase>(savedIdentity ?? (File.Exists(path) ? File.ReadAllText(path) : "{}")) ?? new MemberDatabase();
if (database != null && savedIdentity == null) database.Save(JsonSerializer.Serialize(db));
// Önceden kurulmuş başkan kaydının yerel kimliği; bu adresle dış bağlantı yapılmaz.
const string ownerIdentity = "jawharyusuf446@gmail.com";
int adminFailures = 0;
DateTimeOffset adminBlockedUntil = DateTimeOffset.MinValue;
void Save()
{
    if (database != null) { database.Save(JsonSerializer.Serialize(db)); return; }
    Directory.CreateDirectory(Path.GetDirectoryName(path)!);
    var temporary = path + ".tmp";
    File.WriteAllText(temporary, JsonSerializer.Serialize(db));
    if (File.Exists(path)) File.Copy(path, path + ".bak", true);
    File.Move(temporary, path, true);
}
Session Require(HttpContext context, bool owner = false)
{
    var header = context.Request.Headers.Authorization.ToString();
    if (!header.StartsWith("Bearer ") || !sessions.TryGetValue(header[7..], out var session) || session.Expires <= DateTimeOffset.UtcNow)
        throw new AccessException("Oturumun süresi doldu. Tekrar giriş yapın.");
    if (owner && session.Contact != ownerIdentity) throw new AccessException("Bu sayfaya yalnız başkan erişebilir.");
    return session;
}
object IssuePresident()
{
    foreach (var item in sessions.Where(item => item.Value.Expires <= DateTimeOffset.UtcNow)) sessions.TryRemove(item.Key, out _);
    var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
    sessions[token] = new Session(ownerIdentity, "president-password", DateTimeOffset.UtcNow.AddHours(1));
    return new { token, contact = ownerIdentity, channel = "president-password", isOwner = true };
}
void ValidateCredentials(AdminCredentials credentials)
{
    if (credentials.Username.Trim().Length is < 3 or > 64 || credentials.Password.Length is < 8 or > 128)
        throw new ArgumentException("Başkan kullanıcı adı 3–64, parola 8–128 karakter olmalı.");
}
app.Use(async (context, next) =>
{
    try
    {
        if (context.Connection.RemoteIpAddress is null || !IPAddress.IsLoopback(context.Connection.RemoteIpAddress))
            throw new AccessException("Giriş hizmetine yalnız bu bilgisayardan erişilebilir.");
        await next();
    }
    catch (AccessException error) { context.Response.StatusCode = 403; await context.Response.WriteAsJsonAsync(new { error = error.Message }); }
    catch (Exception error) when (error is ArgumentException or InvalidOperationException or FormatException)
    { context.Response.StatusCode = 400; await context.Response.WriteAsJsonAsync(new { error = error.Message }); }
    catch { context.Response.StatusCode = 500; await context.Response.WriteAsJsonAsync(new { error = "Giriş işlemi tamamlanamadı. Tekrar deneyin." }); }
});
app.MapGet("/health", () => new { ready = true, email = false, phone = false });
app.MapGet("/admin/status", () => { lock (gate) return new { initialized = !string.IsNullOrEmpty(db.OwnerCredential.Salt) }; });
app.MapPost("/admin/initialize", (HttpContext context, AdminCredentials credentials) =>
{
    if (context.Request.Headers.Authorization != "Bearer " + setupToken) throw new AccessException("İlk kurulum yetkisi yok.");
    ValidateCredentials(credentials);
    lock (gate)
    {
        if (!string.IsNullOrEmpty(db.OwnerCredential.Salt)) throw new AccessException("Başkan hesabı zaten kurulmuş. Kullanıcı adı ve parolanla giriş yap.");
        db.OwnerCredential.Username = credentials.Username.Trim();
        Passwords.Set(db.OwnerCredential, credentials.Password);
        Save();
    }
    return new { ready = true };
});
app.MapPost("/admin/login", (AdminCredentials credentials) =>
{
    lock (gate)
    {
        if (adminBlockedUntil > DateTimeOffset.UtcNow) throw new AccessException("Çok fazla başkan giriş denemesi var. Beş dakika sonra tekrar deneyin.");
        if (string.IsNullOrEmpty(db.OwnerCredential.Salt)) throw new AccessException("Başkan hesabı henüz kurulmamış. İlk kurulumdan kendi kullanıcı adını ve parolanı belirle.");
        if (!credentials.Username.Trim().Equals(db.OwnerCredential.Username, StringComparison.OrdinalIgnoreCase) || !Passwords.Verify(db.OwnerCredential, credentials.Password))
        {
            if (++adminFailures >= 5) { adminBlockedUntil = DateTimeOffset.UtcNow.AddMinutes(5); adminFailures = 0; }
            throw new AccessException("Başkan kullanıcı adı veya parolası yanlış.");
        }
        adminFailures = 0;
        if (Passwords.RememberVerified(db.OwnerCredential, credentials.Password)) Save();
    }
    return IssuePresident();
});
app.MapGet("/session", (HttpContext context) => { var session = Require(context); return new { contact = session.Contact, channel = session.Channel, isOwner = session.Contact == ownerIdentity }; });
app.MapPost("/session/revoke", (HttpContext context) =>
{
    var session = Require(context);
    foreach (var key in sessions.Where(pair => pair.Value.Contact == session.Contact).Select(pair => pair.Key).ToArray()) sessions.TryRemove(key, out _);
    return new { ready = true };
});
app.MapPost("/admin/password", (HttpContext context, AdminCredentials credentials) =>
{
    _ = Require(context, true); ValidateCredentials(credentials);
    lock (gate) { db.OwnerCredential.Username = credentials.Username.Trim(); Passwords.Set(db.OwnerCredential, credentials.Password); Save(); }
    return new { ready = true };
});
app.MapPost("/members", (HttpContext context, MemberInput input) =>
{
    var session = Require(context);
    if (input.Name.Trim().Length < 2 || !Choices.Roles.Contains(input.Role)) throw new ArgumentException("Ad ve kullanım türünü kontrol edin.");
    lock (gate)
    {
        var member = db.Members.FirstOrDefault(member => member.Contact == session.Contact);
        if (member is null) { member = new Member { Id = Guid.NewGuid(), Contact = session.Contact, RegisteredAt = DateTimeOffset.UtcNow }; db.Members.Add(member); }
        member.Name = input.Name.Trim(); member.Role = input.Role; member.Channel = session.Channel; Save(); return member;
    }
});
app.MapGet("/admin/members", (HttpContext context) => { _ = Require(context, true); lock (gate) return db.Members.Select(member => new { member.Id, member.Name, member.Contact, member.Role, member.Channel, member.RegisteredAt }).ToArray(); });
app.Run();
record Session(string Contact, string Channel, DateTimeOffset Expires);
record AdminCredentials(string Username, string Password);
record MemberInput(string Name, string Role);
sealed class AccessException(string message) : Exception(message);
sealed class MemberDatabase { public List<Member> Members { get; set; } = []; public User OwnerCredential { get; set; } = new(); }
sealed class Member { public Guid Id { get; set; } public string Name { get; set; } = ""; public string Contact { get; set; } = ""; public string Role { get; set; } = ""; public string Channel { get; set; } = ""; public DateTimeOffset RegisteredAt { get; set; } }
