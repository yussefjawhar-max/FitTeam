using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using FitTeam.Core;
namespace FitTeam.Desktop;
internal static class IssuedCredentialVault
{
    private static readonly string folder=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"FitTeam");
    private static readonly string file=Path.Combine(folder,"issued-credentials.json"),keyFile=Path.Combine(folder,"issued-credentials.key");
    private static Dictionary<string,string> Read()=>File.Exists(file)?JsonSerializer.Deserialize<Dictionary<string,string>>(File.ReadAllText(file))??[]:[];
    private static void Private(string path){if(!OperatingSystem.IsWindows())File.SetUnixFileMode(path,UnixFileMode.UserRead|UnixFileMode.UserWrite);}
    public static void Save(FitService service,string id,string password)
    {
        if(service.IsPreview)return;
        if(!service.HasAdministrationAccess)throw new InvalidOperationException("Yetkili hesabıyla giriş yap.");
        Directory.CreateDirectory(folder);if(!File.Exists(keyFile)){File.WriteAllBytes(keyFile,RandomNumberGenerator.GetBytes(32));Private(keyFile);}
        var key=File.ReadAllBytes(keyFile);var nonce=RandomNumberGenerator.GetBytes(12);var plain=Encoding.UTF8.GetBytes(password);var cipher=new byte[plain.Length];var tag=new byte[16];
        using(var aes=new AesGcm(key,16))aes.Encrypt(nonce,plain,cipher,tag);
        CryptographicOperations.ZeroMemory(plain);var records=Read();records[id]=Convert.ToBase64String(nonce.Concat(tag).Concat(cipher).ToArray());
        File.WriteAllText(file+".tmp",JsonSerializer.Serialize(records));Private(file+".tmp");File.Move(file+".tmp",file,true);Private(file);
    }
    private static string? Decrypt(string id)
    {
        try{if(!File.Exists(keyFile)||!Read().TryGetValue(id,out var value))return null;var bytes=Convert.FromBase64String(value);var plain=new byte[bytes.Length-28];using(var aes=new AesGcm(File.ReadAllBytes(keyFile),16))aes.Decrypt(bytes.AsSpan(0,12),bytes.AsSpan(28),bytes.AsSpan(12,16),plain);string result=Encoding.UTF8.GetString(plain);CryptographicOperations.ZeroMemory(plain);return result;}catch{return null;}
    }
    private static IEnumerable<string> ReportLines()
    {
        var candidate=Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,"../../../FitTeam_Ornek_Hesap_Sifreleri.txt"));
        var generated=Path.Combine(folder,"fitteam.json.club-credentials.txt");
        return (File.Exists(candidate)?File.ReadAllLines(candidate):[]).Concat(File.Exists(generated)?File.ReadAllLines(generated):[]);
    }
    private static bool Matches(User user,string? value)=>value!=null&&user.Salt.Length>0&&user.PasswordHash.Length>0&&Passwords.Verify(user,value);
    public static string? UserPassword(FitService service,User user)
    {
        if(service.IsPreview)return user.Salt.Length>0&&Passwords.Verify(user,"Fit123!")?"Fit123!":null;
        if(!service.HasFullAdministrationView)throw new InvalidOperationException("Yetkili hesabıyla giriş yap.");
        var personal=service.AdminReadPasswordCopy(user.Id);if(personal!=null)return personal;
        string? saved=Decrypt("user:"+user.Id);if(Matches(user,saved))return saved;
        string? pending=null;
        foreach(var line in ReportLines())
        {
            var inline=Regex.Match(line,@"Kullanıcı adı:\s*(?<name>[^|]+)\s*\|\s*Şifre:\s*(?<pass>\S+)",RegexOptions.IgnoreCase|RegexOptions.CultureInvariant);
            if(inline.Success&&inline.Groups["name"].Value.Trim().Equals(user.Username,StringComparison.OrdinalIgnoreCase)&&Matches(user,inline.Groups["pass"].Value))return inline.Groups["pass"].Value;
            if(line.Trim().StartsWith("Koç kullanıcı adı:"))pending=line.Split(':',2)[1].Trim();
            if(line.Trim().StartsWith("Koç kişisel şifresi:")&&pending==user.Username){var password=line.Split(':',2)[1].Trim();if(Matches(user,password))return password;}
        }
        return null;
    }
    public static string? OwnerPassword(FitService service)
    {
        if(service.IsPreview)return null;
        if(!service.HasFullAdministrationView)throw new InvalidOperationException("Yetkili hesabıyla giriş yap.");
        try{using var doc=JsonDocument.Parse(File.ReadAllText(Path.Combine(folder,"members.json")));var owner=doc.RootElement.GetProperty("OwnerCredential").Deserialize<User>();return owner==null?null:Passwords.ReadCopy(owner);}catch{return null;}
    }
    public static string? ClubPassword(FitService service,Team club)
    {
        if(service.IsPreview)return null;
        if(!service.HasFullAdministrationView)throw new InvalidOperationException("Yetkili hesabıyla giriş yap.");
        var credential=new User{Salt=club.JoinPasswordSalt,PasswordHash=club.JoinPasswordHash};string? saved=Decrypt("club:"+club.Id);if(Matches(credential,saved))return saved;
        string? name=null;
        foreach(var line in ReportLines())
        {
            if(line==club.Name)name=line;
            else if(name==club.Name&&line.Trim().StartsWith("Kulüp katılım şifresi:")){var password=line.Split(':',2)[1].Trim();return Matches(credential,password)?password:null;}
        }
        return null;
    }
}
