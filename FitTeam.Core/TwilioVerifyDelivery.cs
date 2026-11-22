using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace FitTeam.Core;

// Verify kodu Twilio tarafından oluşturulur. Kod veya sağlayıcı yanıtı günlüğe yazılmaz.
public sealed class TwilioVerifyDelivery(string accountSid, string authToken, string serviceSid, HttpClient? client = null) : IDisposable
{
    private readonly HttpClient http = client ?? new HttpClient(new HttpClientHandler { AllowAutoRedirect = false }) { Timeout = TimeSpan.FromSeconds(25) };
    private static bool Sid(string value, string prefix) => Regex.IsMatch(value, "^" + prefix + "[0-9a-fA-F]{32}$", RegexOptions.CultureInvariant);
    public static bool IsServiceSid(string value) => Sid(value, "VA");

    public async Task<string> Send(string phone)
    {
        Validate(phone);
        using var response = await Post("Verifications", new() { ["To"] = phone, ["Channel"] = "sms" });
        if (!response.IsSuccessStatusCode) throw new InvalidOperationException("SMS kodu gönderilemedi. Twilio hesabını, doğrulanmış numaranı ve hizmet ayarını kontrol et.");
        using var json = await Read(response);
        var root = json.RootElement;
        var verificationSid = Value(root, "sid");
        if (!Sid(verificationSid, "VE") || !Matches(root, phone) || Value(root, "status") != "pending")
            throw new InvalidOperationException("SMS hizmetinden geçerli gönderim onayı alınamadı. Yeni kod isteyin.");
        return verificationSid;
    }

    public async Task<bool> Check(string phone, string verificationSid, string code)
    {
        Validate(phone);
        if (!Sid(verificationSid, "VE") || code.Length != 6 || code.Any(c => c < '0' || c > '9')) return false;
        // Deneme hesabı To/Code alanlarını kabul eder; yanıt yine saklanan VE isteğine bağlıdır.
        using var response = await Post("VerificationCheck", new() { ["To"] = phone, ["Code"] = code });
        if (response.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.NotFound or HttpStatusCode.TooManyRequests) return false;
        if (!response.IsSuccessStatusCode) throw new InvalidOperationException("SMS kodu doğrulanamadı. Hizmet bağlantısını kontrol et.");
        using var json = await Read(response);
        var root = json.RootElement;
        return Value(root, "sid") == verificationSid && Matches(root, phone) && Value(root, "status") == "approved";
    }

    private bool Matches(JsonElement root, string phone) => Value(root, "service_sid") == serviceSid && Value(root, "account_sid") == accountSid && Value(root, "to") == phone && Value(root, "channel") == "sms";
    private static string Value(JsonElement root, string property) => root.ValueKind == JsonValueKind.Object && root.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() ?? "" : "";
    private void Validate(string phone)
    {
        if (!Sid(accountSid, "AC") || authToken.Trim().Length < 16 || !IsServiceSid(serviceSid))
            throw new InvalidOperationException("SMS hesabının kimliğini, gizli bağlantı anahtarını ve Verify hizmet kimliğini kontrol et.");
        if (!Regex.IsMatch(phone, @"^\+[1-9][0-9]{9,14}$", RegexOptions.CultureInvariant)) throw new ArgumentException("Telefon numarasını +905… biçiminde girin.");
    }
    private async Task<HttpResponseMessage> Post(string action, Dictionary<string, string> fields)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "https://verify.twilio.com/v2/Services/" + serviceSid + "/" + action);
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes(accountSid + ":" + authToken)));
        request.Content = new FormUrlEncodedContent(fields);
        try { return await http.SendAsync(request); }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException) { throw new InvalidOperationException("SMS hizmetine ulaşılamadı. Bağlantıyı kontrol et."); }
    }
    private static async Task<JsonDocument> Read(HttpResponseMessage response)
    {
        try { return JsonDocument.Parse(await response.Content.ReadAsStringAsync()); }
        catch (JsonException) { throw new InvalidOperationException("SMS hizmetinden geçerli yanıt alınamadı. Yeni kod isteyin."); }
    }
    public void Dispose() { if (client is null) http.Dispose(); }
}
