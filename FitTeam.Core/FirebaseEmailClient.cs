using System.Net.Http.Json;
using System.Text.Json;
namespace FitTeam.Core;

public sealed class FirebaseSettings
{
    public string ApiKey { get; set; } = "";
    public string ProjectId { get; set; } = "";
    public static string SettingsPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FitTeam", "firebase.json");
    public static FirebaseSettings? Read()
    {
        var path = File.Exists(Path.Combine(AppContext.BaseDirectory, "firebase.json")) ? Path.Combine(AppContext.BaseDirectory, "firebase.json") : SettingsPath;
        if (!File.Exists(path)) return null;
        var settings = JsonSerializer.Deserialize<FirebaseSettings>(File.ReadAllText(path), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        return string.IsNullOrWhiteSpace(settings?.ApiKey) || string.IsNullOrWhiteSpace(settings.ProjectId) ? null : settings;
    }
}
public sealed class VerifiedEmailIdentity
{
    public string Uid { get; }
    public string Email { get; }
    public string ProjectId { get; }
    internal VerifiedEmailIdentity(string uid, string email, string projectId) { Uid = uid; Email = email; ProjectId = projectId; }
}
public sealed class FirebaseEmailClient(FirebaseSettings settings, HttpClient? client = null)
{
    private readonly HttpClient http = client ?? new HttpClient { Timeout = TimeSpan.FromSeconds(25) };
    private async Task<JsonElement> Call(string operation, object body)
    {
        if (string.IsNullOrWhiteSpace(settings.ApiKey) || string.IsNullOrWhiteSpace(settings.ProjectId)) throw new InvalidOperationException("Firebase bağlantısı ayarlanmamış.");
        using var response = await http.PostAsJsonAsync("https://identitytoolkit.googleapis.com/v1/accounts:" + operation + "?key=" + Uri.EscapeDataString(settings.ApiKey), body);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        if (!response.IsSuccessStatusCode)
        {
            string code = document.RootElement.TryGetProperty("error", out var error) && error.TryGetProperty("message", out var message) ? message.GetString() ?? "" : "";
            throw new InvalidOperationException(code switch
            {
                "EMAIL_EXISTS" => "Bu Gmail adresi zaten kayıtlı. Giriş ekranını kullanın.",
                "INVALID_LOGIN_CREDENTIALS" or "INVALID_PASSWORD" or "EMAIL_NOT_FOUND" => "Gmail adresi veya FitTeam parolası yanlış.",
                "TOO_MANY_ATTEMPTS_TRY_LATER" => "Çok fazla deneme yapıldı. Bir süre sonra tekrar deneyin.",
                "OPERATION_NOT_ALLOWED" => "Firebase projesinde e-posta ile giriş henüz etkinleştirilmemiş.",
                _ => "Google doğrulama hizmeti işlemi tamamlayamadı. Bağlantıyı ve proje ayarlarını kontrol edin."
            });
        }
        return document.RootElement.Clone();
    }
    public async Task<string> RegisterAndSendVerification(string email, string password)
    {
        if (!email.Trim().EndsWith("@gmail.com", StringComparison.OrdinalIgnoreCase) || password.Length < 6) throw new ArgumentException("Geçerli Gmail adresi ve en az 6 karakterli FitTeam parolası girin.");
        var account = await Call("signUp", new { email = email.Trim(), password, returnSecureToken = true });
        var token = account.GetProperty("idToken").GetString()!;
        await Call("sendOobCode", new { requestType = "VERIFY_EMAIL", idToken = token });
        return account.GetProperty("localId").GetString()!;
    }
    public async Task<VerifiedEmailIdentity> LoginVerified(string email, string password)
    {
        var result = await Call("signInWithPassword", new { email = email.Trim(), password, returnSecureToken = true });
        string uid = result.GetProperty("localId").GetString()!;
        var account = await Call("lookup", new { idToken = result.GetProperty("idToken").GetString()! });
        var users = account.GetProperty("users");
        if (users.GetArrayLength() != 1 || users[0].GetProperty("localId").GetString() != uid || !users[0].TryGetProperty("emailVerified", out var verified) || !verified.GetBoolean())
            throw new InvalidOperationException("Önce Gmail gelen kutundaki FitTeam doğrulama bağlantısına tıkla, sonra tekrar giriş yap.");
        string token = result.GetProperty("idToken").GetString()!;
        var pieces = token.Split('.');
        if (pieces.Length != 3) throw new InvalidOperationException("Doğrulama yanıtı geçersiz.");
        var encoded = pieces[1].Replace('-', '+').Replace('_', '/'); encoded = encoded.PadRight(encoded.Length + (4 - encoded.Length % 4) % 4, '=');
        using var claimsDoc = JsonDocument.Parse(Convert.FromBase64String(encoded)); var claims = claimsDoc.RootElement;
        // İmza/geçerlilik Google'ın lookup uç noktasında doğrulanmıştır; proje bağı ayrıca kontrol edilir.
        if (claims.GetProperty("aud").GetString() != settings.ProjectId || claims.GetProperty("iss").GetString() != "https://securetoken.google.com/" + settings.ProjectId || claims.GetProperty("sub").GetString() != uid)
            throw new InvalidOperationException("Google hesabı farklı bir doğrulama projesine ait.");
        string verifiedEmail = users[0].GetProperty("email").GetString()!;
        if (!verifiedEmail.Equals(email.Trim(), StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Doğrulanan hesap giriş adresiyle eşleşmiyor.");
        return new VerifiedEmailIdentity(uid, verifiedEmail, settings.ProjectId);
    }
}
public sealed partial class FitService
{
    public void RememberFirebaseRegistration(string uid, string project, string email, string name, string role, Guid? coachId)
    {
        var user = new User { Username = email.Trim().ToLowerInvariant(), Email = email.Trim().ToLowerInvariant(), AuthenticationUid = uid, AuthenticationProject = project, Role = role, CoachId = role == Choices.Roles[0] ? coachId : null, Profile = new Profile { Name = name.Trim() } };
        if (role == Choices.Roles[0] && !Coaches().Any(c => c.Id == coachId)) throw new ArgumentException("Bir fitness koçu seçin.");
        if (!Data.Users.Any(u => u.AuthenticationUid == uid && u.AuthenticationProject == project)) Data.Users.Add(user);
        store.Save();
    }
    public void LoginVerifiedEmail(VerifiedEmailIdentity identity)
    {
        Logout();
        var user = Data.Users.FirstOrDefault(u => u.AuthenticationUid == identity.Uid && u.AuthenticationProject == identity.ProjectId || u.Email.Equals(identity.Email,StringComparison.OrdinalIgnoreCase));
        if (user == null)
        {
            user = new User { NeedsApproval=true, Username = identity.Email, Email = identity.Email, AuthenticationUid = identity.Uid, AuthenticationProject = identity.ProjectId, Profile = new Profile { Name = identity.Email.Split('@')[0] } }; Data.Users.Add(user);
        }
        if(!user.IsActive)throw new InvalidOperationException("Bu hesap yetkili tarafından çıkarıldı.");
        user.AuthenticationUid=identity.Uid;user.AuthenticationProject=identity.ProjectId;
        IsPresident = false; LoginMethod = "Gmail adresin bağlantıyla doğrulandı"; user.EmailVerified = true; user.Email = identity.Email; Current = user; store.Save();
    }
}
