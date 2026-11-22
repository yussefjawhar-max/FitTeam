using System.Security.Cryptography;
using System.Text.Json;
using FitTeam.Core;
static class PasswordCopyChecks
{
    public static void Run(Action<bool,string> check)
    {
        var folder=Path.Combine(Path.GetTempPath(),"FitTeam_passwordcopy_"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(folder);var key=Path.Combine(folder,"account-passwords.key");var previous=Passwords.CopyProtector;
        try
        {
            var protector=new LocalAccountPasswordProtector(key);Passwords.CopyProtector=protector;
            var user=new User{Username="selfchosen"};const string first="PersonalPass-84!";const string second="ChangedPass-28!";
            Passwords.Set(user,first);string encrypted=user.ProtectedPassword;
            check(Passwords.Verify(user,first)&&Passwords.ReadCopy(user)==first&&!JsonSerializer.Serialize(user).Contains(first)&&File.ReadAllBytes(key).Length==32,"Kişinin belirlediği şifre girişte doğrulanır ve korumalı kopyası geri açılır; veri dosyasında açık şifre bulunmaz");
            Passwords.Set(user,second);check(Passwords.ReadCopy(user)==second&&!Passwords.Verify(user,first)&&encrypted!=user.ProtectedPassword,"Kişisel şifre değişikliği başkanda gösterilecek kopyayı da yeniler; eski şifreyle giriş yapılamaz");
            check(protector.Unprotect(Guid.NewGuid(),user.ProtectedPassword)==null,"Başka hesaba taşınan şifre kopyası çözülemez");
            var bytes=Convert.FromBase64String(user.ProtectedPassword);bytes[^1]^=1;check(protector.Unprotect(user.Id,Convert.ToBase64String(bytes))==null,"Değiştirilmiş şifre kopyası doğrulama etiketiyle reddedilir");
            var oldUser=new User();Passwords.CopyProtector=null;Passwords.Set(oldUser,first);Passwords.CopyProtector=protector;var originalHash=oldUser.PasswordHash;
            check(!Passwords.RememberVerified(oldUser,"wrong-password")&&Passwords.RememberVerified(oldUser,first)&&oldUser.PasswordHash==originalHash&&Passwords.ReadCopy(oldUser)==first,"Eski hesapta yalnız doğru giriş şifresinin kopyası alınır; kullanıcı parolası değiştirilmez");
            var service=new FitService(new DataStore(new AppData{Users=[user]}));service.Login(user.Username,second);bool rejected=false;try{service.AdminReadPasswordCopy(user.Id);}catch(InvalidOperationException){rejected=true;}
            check(rejected,"Normal üye korumalı şifre listesini başkan yöntemiyle okuyamaz");
            File.Delete(key);check(Passwords.ReadCopy(user)==null&&Passwords.Verify(user,second),"Kopya anahtarı yoksa şifre gösterilmez; mevcut giriş şifresi çalışmaya devam eder");
        }
        finally{Passwords.CopyProtector=previous;Directory.Delete(folder,true);}
    }
}
