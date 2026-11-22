using System.Net.Http.Json;
using System.Net.Http.Headers;
using System.Text.Json;
namespace FitTeam.Core;

public sealed record CodeDelivery(string RequestId, DateTimeOffset ExpiresAt);

public sealed class CodeIdentity
{
    public string Contact { get; }
    public string Channel { get; }
    public bool IsOwner { get; }
    public string GoogleSubject {get;}
    public string Phone {get;}
    internal CodeIdentity(string contact,string channel,bool owner,string subject="",string phone="") { Contact=contact;Channel=channel;IsOwner=owner;GoogleSubject=subject;Phone=phone; }
}
public sealed class CodeMember
{
    public Guid Id {get;set;} public string Name {get;set;}=""; public string Contact {get;set;}=""; public string Role {get;set;}=""; public string Channel {get;set;}=""; public DateTimeOffset RegisteredAt {get;set;}
}
public sealed class CodeAuthClient(Uri endpoint,HttpClient? client=null)
{
    private readonly HttpClient http=client??new HttpClient { Timeout=TimeSpan.FromSeconds(30) };
    private string token="";
    public CodeIdentity? Identity {get;private set;}
    public void Logout() { token=""; Identity=null; }
    public async Task RevokeOwnSessions(){try{_ = await Call("session/revoke",new {});}finally{Logout();}}
    private async Task<JsonElement> Call(string path,object? body=null,string? setupToken=null)
    {
        if (endpoint.Scheme!="https" && !(endpoint.Scheme=="http" && endpoint.Host=="127.0.0.1")) throw new InvalidOperationException("Giriş hizmeti güvenli bağlantı kullanmalı.");
        using var request=new HttpRequestMessage(body==null?HttpMethod.Get:HttpMethod.Post,new Uri(endpoint,path));
        string bearer=setupToken??token; if(!string.IsNullOrEmpty(bearer)) request.Headers.Authorization=new AuthenticationHeaderValue("Bearer",bearer);
        if(body!=null) request.Content=JsonContent.Create(body);
        using var response=await http.SendAsync(request); using var json=JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        if(!response.IsSuccessStatusCode) { if(response.StatusCode==System.Net.HttpStatusCode.Forbidden) Logout(); throw new InvalidOperationException(json.RootElement.TryGetProperty("error",out var error)?error.GetString():"Giriş hizmeti yanıt vermedi."); }
        return json.RootElement.Clone();
    }
    public async Task<(bool Email,bool Phone)> Health() {var r=await Call("health");return (r.GetProperty("email").GetBoolean(),r.GetProperty("phone").GetBoolean());}
    public async Task Setup(string setupToken,string sender,string appPassword,string sid,string smsToken,string from)=>_ = await Call("setup",new { sender,appPassword,twilioSid=sid,twilioToken=smsToken,twilioFrom=from },setupToken);
    public async Task SetupSms(string setupToken,string sid,string smsToken,string from,string verifyService="")=>_ = await Call("sms/setup",new {twilioSid=sid,twilioToken=smsToken,twilioFrom=from,twilioVerifyService=verifyService},setupToken);
    public async Task SetupGoogle(string setupToken,string clientId)=>_ = await Call("google/setup",new {clientId},setupToken);
    public async Task<GoogleLoginStart> StartGoogle() { var s=await Call("google/start",new {});return new GoogleLoginStart(s.GetProperty("requestId").GetString()!,s.GetProperty("nonce").GetString()!,s.GetProperty("clientId").GetString()!); }
    public async Task<(string Email,string Name)> GooglePending(string requestId,string idToken,string accessToken="",string expectedEmail="")
    {
        Logout();var result=await Call("google/verify",new {requestId,idToken,accessToken,expectedEmail});token=result.GetProperty("token").GetString()!;return (result.GetProperty("contact").GetString()!,result.GetProperty("name").GetString()!);
    }
    public async Task<CodeIdentity> GoogleLogin(string requestId,string idToken)
    {
        Logout();return await Accept(await Call("google/verify",new {requestId,idToken,direct=true}));
    }
    public async Task<string> Send(string contact,string channel,bool consent=false) => (await SendDetails(contact,channel,consent)).RequestId;
    public async Task<CodeDelivery> SendDetails(string contact,string channel,bool consent=false)
    {
        var r=await Call("code/send",new {contact,channel,consent});
        return new CodeDelivery(r.GetProperty("requestId").GetString()!,r.GetProperty("expiresAt").GetDateTimeOffset());
    }
    private async Task<CodeIdentity> Accept(JsonElement result)
    {
        token=result.GetProperty("token").GetString()!;
        try {var s=await Call("session");var identity=new CodeIdentity(s.GetProperty("contact").GetString()!,s.GetProperty("channel").GetString()!,s.GetProperty("isOwner").GetBoolean(),s.TryGetProperty("googleSubject",out var subject)?subject.GetString()??"":"",s.TryGetProperty("phone",out var phone)?phone.GetString()??"":"");
            if(identity.IsOwner && identity.Contact!="jawharyusuf446@gmail.com") throw new InvalidOperationException("Başkan yetkisi doğrulanamadı."); Identity=identity;return identity;}
        catch {Logout();throw;}
    }
    public async Task<CodeIdentity> Verify(string contact,string channel,string requestId,string code)=>await Accept(await Call("code/verify",new {contact,channel,requestId,code}));
    public async Task<CodeIdentity> PresidentLogin(string username,string password)=>await Accept(await Call("admin/login",new {username,password}));
    public async Task<bool> PresidentInitialized() => (await Call("admin/status")).GetProperty("initialized").GetBoolean();
    public async Task InitializePresident(string setupToken,string username,string password) => _ = await Call("admin/initialize",new {username,password},setupToken);
    public async Task SetPresidentPassword(string username,string password)=>_ = await Call("admin/password",new {username,password});
    public async Task RegisterProfile(string name,string role)=>_ = await Call("members",new {name,role});
    public async Task<List<CodeMember>> Members()=>JsonSerializer.Deserialize<List<CodeMember>>((await Call("admin/members")).GetRawText(),new JsonSerializerOptions {PropertyNameCaseInsensitive=true})!;
}
public sealed partial class FitService
{
    public bool IsPresident {get;private set;}
    public string LoginMethod {get;private set;}="Yerel hesap";
    public void LoginCode(CodeIdentity identity)
    {
        Logout();
        var u=Data.Users.FirstOrDefault(u=>identity.Channel=="phone" ? u.Phone==identity.Contact : u.Email.Equals(identity.Contact,StringComparison.OrdinalIgnoreCase));
        if(!string.IsNullOrEmpty(identity.Phone)&&Data.Users.Any(other=>other.Id!=u?.Id&&other.Phone==identity.Phone))
            throw new InvalidOperationException("Bu telefon başka hesaba kayıtlı. Yetkiliyle iletişime geç.");
        if(u==null) { u=new User { Username=identity.Contact,Email=identity.Channel=="phone"?"":identity.Contact,Phone=identity.Channel=="phone"?identity.Contact:"",NeedsApproval=!identity.IsOwner,Profile=new Profile {Name=identity.IsOwner?"Yussef Jawhar":identity.Contact.Split('@')[0]} };Data.Users.Add(u); }
        if(!u.IsActive)throw new InvalidOperationException("Bu hesap yetkili tarafından çıkarıldı. Yetkiliyle iletişime geç.");
        if(identity.Channel=="phone") u.PhoneVerified=true; else if(identity.Channel!="president-password") u.EmailVerified=true;
        if(!string.IsNullOrEmpty(identity.Phone)){u.Phone=identity.Phone;u.PhoneVerified=true;}
        if(!string.IsNullOrEmpty(identity.GoogleSubject)){u.AuthenticationUid=identity.GoogleSubject;u.AuthenticationProject="google";}
        if(identity.IsOwner&&string.IsNullOrWhiteSpace(u.Profile.Name))u.Profile.Name="Yussef Jawhar";
        Current=u;IsPresident=identity.IsOwner;LoginMethod=identity.Channel.StartsWith("google+")?"Google hesabınla giriş yaptın • "+(identity.Channel.EndsWith("phone")?"Kod doğrulandı":"Gmail kodu doğrulandı"):identity.Channel=="google"?"Google hesabınla giriş yaptın":identity.Channel=="phone"?"İletişim bilgisi kodla doğrulandı":identity.Channel=="president-password"?"Başkan hesabıyla giriş yaptın":"Gmail adresin kodla doğrulandı; bu hesapla giriş yaptın";store.Save();
    }
}
public sealed partial class FitService
{
    public void CompleteCodeRegistration(string name,string role,Guid? coachId)
    {
        var user=RequireCurrent();
        if(name.Trim().Length<2 || !Choices.Roles.Contains(role))throw new ArgumentException("Ad ve kullanım türünü kontrol edin.");
        if(role==Choices.Roles[0]&&!IsPresident&&!Coaches().Any(c=>c.Id==coachId))throw new ArgumentException("Bir fitness koçu seçin.");
        user.Profile.Name=name.Trim();user.Role=role;user.CoachId=role==Choices.Roles[0]?coachId:null;store.Save();
    }
}

public sealed partial class FitService
{
    public static string LoginChannelLabel(string channel)=>channel switch
    {
        "president-password"=>"Yetkili şifresi","google+email"=>"Google ve e-posta kodu","google+phone"=>"Google ve telefon kodu","email"=>"E-posta doğrulama kodu","phone"=>"Telefon doğrulama kodu","google"=>"Google hesabı","Yetkili ekledi"=>"Yetkili ekledi",_=>"Yerel hesap"
    };
}
