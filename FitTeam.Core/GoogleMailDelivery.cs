using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
namespace FitTeam.Core;

// Yalnız doğrulanmış Google hesabının kendi adresine giriş kodu gönderilir.
// Erişim anahtarı, kod ve mesaj içerikleri dosyaya veya günlüğe yazılmaz.
public sealed class GoogleMailDelivery(HttpClient? client=null)
{
    private readonly HttpClient http=client??new HttpClient {Timeout=TimeSpan.FromSeconds(25)};
    public async Task VerifyAccount(string token,GoogleProof proof)
    {
        using var request=new HttpRequestMessage(HttpMethod.Get,"https://openidconnect.googleapis.com/v1/userinfo");
        request.Headers.Authorization=new AuthenticationHeaderValue("Bearer",token);
        using var response=await http.SendAsync(request);
        if(!response.IsSuccessStatusCode)throw new InvalidOperationException("Google gönderim izni doğrulanamadı. Tekrar Google ile bağlan.");
        using var document=JsonDocument.Parse(await response.Content.ReadAsStringAsync());var p=document.RootElement;
        if(p.GetProperty("sub").GetString()!=proof.Subject || !p.GetProperty("email_verified").GetBoolean() || !string.Equals(p.GetProperty("email").GetString(),proof.Email,StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Gönderim izni giriş yaptığın Google hesabına ait olmalı.");
    }
    public async Task Send(string token,string email,string code)
    {
        // Adres ve kod MIME başlıklarına eklenmeden önce kesin olarak doğrulanır.
        if(!email.EndsWith("@gmail.com",StringComparison.Ordinal) || LoginContact.Gmail(email[..^10])!=email || code.Length!=6 || code.Any(c=>c<'0'||c>'9'))throw new ArgumentException("Kod gönderim bilgisi geçersiz.");
        string message=$"From: {email}\r\nTo: {email}\r\nSubject: FitTeam giris kodun\r\nMIME-Version: 1.0\r\nContent-Type: text/plain; charset=UTF-8\r\nContent-Transfer-Encoding: base64\r\n\r\n"+Convert.ToBase64String(Encoding.UTF8.GetBytes($"FitTeam giriş kodun: {code}\n\nKod 2 dakika geçerlidir. Bu girişi sen istemediysen kodu paylaşma."));
        string raw=Convert.ToBase64String(Encoding.UTF8.GetBytes(message)).TrimEnd('=').Replace('+','-').Replace('/','_');
        using var request=new HttpRequestMessage(HttpMethod.Post,"https://gmail.googleapis.com/gmail/v1/users/me/messages/send");
        request.Headers.Authorization=new AuthenticationHeaderValue("Bearer",token);request.Content=JsonContent.Create(new {raw});
        using var response=await http.SendAsync(request);
        if(!response.IsSuccessStatusCode)throw new InvalidOperationException("Gmail kodu gönderilemedi. Gmail gönderim bağlantısını ve Google gönderme iznini kontrol et.");
    }
}
