using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
namespace FitTeam.Core;

public sealed record GoogleLoginStart(string RequestId,string Nonce,string ClientId);
public sealed record GoogleProof(string Subject,string Email,string Name);
public sealed record GoogleLoginTokens(string IdToken,string AccessToken);
public sealed class GoogleTokenVerifier(HttpClient? client=null)
{
    private readonly HttpClient http=client??new HttpClient {Timeout=TimeSpan.FromSeconds(20)};
    private static byte[] Decode(string value) {var s=value.Replace('-','+').Replace('_','/');return Convert.FromBase64String(s.PadRight(s.Length+(4-s.Length%4)%4,'='));}
    public async Task<GoogleProof> Verify(string token,string clientId,string nonce)
    {
        var parts=token.Split('.');if(parts.Length!=3)throw new InvalidOperationException("Google kimlik yanıtı geçersiz.");
        using var header=JsonDocument.Parse(Decode(parts[0]));
        if(header.RootElement.GetProperty("alg").GetString()!="RS256")throw new InvalidOperationException("Google imzası geçersiz.");
        string kid=header.RootElement.GetProperty("kid").GetString()!;
        using var keys=JsonDocument.Parse(await http.GetStringAsync("https://www.googleapis.com/oauth2/v3/certs"));
        var key=keys.RootElement.GetProperty("keys").EnumerateArray().FirstOrDefault(k=>k.GetProperty("kid").GetString()==kid);
        if(key.ValueKind==JsonValueKind.Undefined)throw new InvalidOperationException("Google imza anahtarı bulunamadı.");
        using var rsa=RSA.Create();rsa.ImportParameters(new RSAParameters {Modulus=Decode(key.GetProperty("n").GetString()!),Exponent=Decode(key.GetProperty("e").GetString()!)});
        if(!rsa.VerifyData(Encoding.ASCII.GetBytes(parts[0]+"."+parts[1]),Decode(parts[2]),HashAlgorithmName.SHA256,RSASignaturePadding.Pkcs1))throw new InvalidOperationException("Google kimlik imzası doğrulanamadı.");
        using var payload=JsonDocument.Parse(Decode(parts[1]));var p=payload.RootElement;var now=DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        if(p.GetProperty("aud").GetString()!=clientId || p.GetProperty("iss").GetString() is not ("https://accounts.google.com" or "accounts.google.com") || p.GetProperty("exp").GetInt64()<=now || p.GetProperty("iat").GetInt64()>now+60 || p.GetProperty("nonce").GetString()!=nonce || !p.GetProperty("email_verified").GetBoolean())throw new InvalidOperationException("Google hesabı veya oturum bağı doğrulanamadı.");
        var subject=p.GetProperty("sub").GetString();var email=p.GetProperty("email").GetString();if(string.IsNullOrEmpty(subject)||string.IsNullOrEmpty(email))throw new InvalidOperationException("Google hesap bilgisi eksik.");
        return new GoogleProof(subject,email.ToLowerInvariant(),p.TryGetProperty("name",out var name)?name.GetString()??email:email);
    }
}
public static class GoogleDesktopLogin
{
    private static string Encode(byte[] bytes)=>Convert.ToBase64String(bytes).TrimEnd('=').Replace('+','-').Replace('/','_');
    public static async Task<string> Open(GoogleLoginStart start,Func<Uri,Task<bool>> launch,string clientSecret="") => (await OpenTokens(start,launch,clientSecret)).IdToken;
    public static async Task<GoogleLoginTokens> OpenTokens(GoogleLoginStart start,Func<Uri,Task<bool>> launch,string clientSecret="",bool allowMail=false)
    {
        if(!start.ClientId.EndsWith(".apps.googleusercontent.com",StringComparison.Ordinal))throw new InvalidOperationException("Google masaüstü giriş kimliği ayarlanmamış.");
        string verifier=Encode(RandomNumberGenerator.GetBytes(32)),state=Encode(RandomNumberGenerator.GetBytes(32));
        using var probe=new System.Net.Sockets.TcpListener(IPAddress.Loopback,0);probe.Start();int port=((IPEndPoint)probe.LocalEndpoint).Port;probe.Stop();
        string redirect="http://127.0.0.1:"+port+"/";using var listener=new HttpListener();listener.Prefixes.Add(redirect);listener.Start();
        var parameters=new Dictionary<string,string> {["client_id"]=start.ClientId,["redirect_uri"]=redirect,["response_type"]="code",["scope"]=allowMail?"openid email profile https://www.googleapis.com/auth/gmail.send":"openid email profile",["code_challenge"]=Encode(SHA256.HashData(Encoding.ASCII.GetBytes(verifier))),["code_challenge_method"]="S256",["state"]=state,["nonce"]=start.Nonce,["prompt"]="select_account",["hl"]=AppLanguage.Code};
        var uri=new Uri("https://accounts.google.com/o/oauth2/v2/auth?"+string.Join("&",parameters.Select(p=>Uri.EscapeDataString(p.Key)+"="+Uri.EscapeDataString(p.Value))));
        if(!await launch(uri))throw new InvalidOperationException("Google giriş sayfası açılamadı.");
        using var deadline=new CancellationTokenSource(TimeSpan.FromMinutes(3));
        HttpListenerContext callback;
        while(true) {callback=await listener.GetContextAsync().WaitAsync(deadline.Token);if(callback.Request.HttpMethod=="GET" && callback.Request.Url?.AbsolutePath=="/" && callback.Request.QueryString["state"]==state)break;callback.Response.StatusCode=400;callback.Response.Close();}
        string? code=callback.Request.QueryString["code"];
        var html=Encoding.UTF8.GetBytes("<!doctype html><meta charset=utf-8><title>FitTeam</title><p>Google yanıtı alındı. FitTeam uygulamasına dönebilirsin.</p>");callback.Response.ContentType="text/html; charset=utf-8";callback.Response.Headers["Cache-Control"]="no-store";await callback.Response.OutputStream.WriteAsync(html);callback.Response.Close();listener.Stop();
        if(string.IsNullOrEmpty(code))throw new InvalidOperationException("Google girişi tamamlanmadı veya iptal edildi.");
        using var http=new HttpClient {Timeout=TimeSpan.FromSeconds(25)};
        var exchange=new Dictionary<string,string>{["client_id"]=start.ClientId,["code"]=code,["code_verifier"]=verifier,["redirect_uri"]=redirect,["grant_type"]="authorization_code"};
        if(!string.IsNullOrEmpty(clientSecret))exchange["client_secret"]=clientSecret;
        using var response=await http.PostAsync("https://oauth2.googleapis.com/token",new FormUrlEncodedContent(exchange));
        if(!response.IsSuccessStatusCode)throw new InvalidOperationException("Google girişi tamamlanamadı. Uygulamanın Google masaüstü bağlantı anahtarını ve istemci ayarını kontrol edin.");
        using var result=JsonDocument.Parse(await response.Content.ReadAsStringAsync());return new GoogleLoginTokens(result.RootElement.GetProperty("id_token").GetString()!,allowMail?result.RootElement.GetProperty("access_token").GetString()!:"");
    }
}
