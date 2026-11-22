using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
namespace FitTeam.Core;

public sealed partial class FitService
{
    private readonly TimeProvider administrationTime=clock??TimeProvider.System;
    public static bool IsExample(User u)=>u.IsExample || (string.IsNullOrEmpty(u.Email)&&string.IsNullOrEmpty(u.Phone)&&new[]{"sporcu","futbolcu","koc","koc2","antrenor","uye","oyuncu"}.Contains(u.Username));
    private void RequirePresident(){if(!IsPresident||Current==null)throw new InvalidOperationException("Yetkili hesabıyla giriş yapın.");}
    private User AdminTarget(Guid id)
    {
        RequireAdministrator();var u=Data.Users.FirstOrDefault(u=>u.Id==id)??throw new ArgumentException("Kişi bulunamadı.");
        if(!IsPresident&&u.Role==Choices.DeputyRole)throw new InvalidOperationException("Yardımcı hesaplarını yalnız asıl yetkili yönetebilir.");
        if(u.Id==Current!.Id||u.Email.Equals("jawharyusuf446@gmail.com",StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("Yetkili hesabını kendi parola ekranından yönet.");
        return u;
    }
    public User AdminAddUser(string username,string name,string role,string email,string phone,string temporaryPassword,string? sport=null)
    {
        RequireAdministrator();if(role==Choices.DeputyRole)RequirePresident();username=Usernames.Validate(username);email=email.Trim().ToLowerInvariant();name=name.Trim();
        if(username.Equals(Data.OwnerLoginUsername,StringComparison.OrdinalIgnoreCase))throw new ArgumentException("Bu kullanıcı adı asıl yetkiliye ait. Başka bir ad seç.");
        if(username.Length<3||name.Length<2||(!Choices.Roles.Contains(role)&&role!=Choices.DeputyRole))throw new ArgumentException("Kullanıcı adı, ad soyad ve kullanım türünü kontrol et.");
        if(Data.Users.Any(u=>u.Username.Equals(username,StringComparison.OrdinalIgnoreCase)))throw new ArgumentException("Bu kullanıcı adı zaten var.");
        if(email.Length>0&&(!System.Net.Mail.MailAddress.TryCreate(email,out var address)||address.Address!=email))throw new ArgumentException("Geçerli e-posta adresi gir.");
        if(email.Length>0&&Data.Users.Any(u=>u.Email.Equals(email,StringComparison.OrdinalIgnoreCase)))throw new ArgumentException("Bu e-posta zaten kayıtlı.");
        phone=Regex.Replace(phone.Trim(),@"[\s()-]","");
        if(phone.Length>0&&(!Regex.IsMatch(phone,@"^\+[1-9][0-9]{9,14}$")||Data.Users.Any(u=>u.Phone==phone)))throw new ArgumentException("Telefonu ülke koduyla yaz; aynı numara başka hesapta olamaz.");
        if(sport!=null&&((role==Choices.Roles[0]||role==Choices.Roles[2])!=(sport=="Fitness")))throw new ArgumentException("Fitness için bireysel kullanıcı veya fitness koçu; diğer branşlarda kulüp sporcusu/antrenörü seç.");
        if(sport!=null&&!Branches.Contains(sport))throw new ArgumentException("Geçerli spor branşı seç.");
        if(temporaryPassword.Length<8)throw new ArgumentException("Geçici FitTeam şifresi en az 8 karakter olmalı.");
        var user=new User {Username=username,Role=role,Email=email,Phone=phone,Profile=new Profile{Name=name},MustChangePassword=false,SportBranch=sport??(role==Choices.Roles[0]||role==Choices.Roles[2]?"Fitness":"Futbol")};
        Passwords.Set(user,temporaryPassword);Data.Users.Add(user);store.Save();return user;
    }
    public void AdminSetPassword(Guid id,string password)
    {
        var user=AdminTarget(id);if(password.Length<8)throw new ArgumentException("FitTeam şifresi en az 8 karakter olmalı.");
        Passwords.Set(user,password);user.MustChangePassword=false;store.Save();
    }
    public void ChangeOwnPassword(string currentPassword,string newPassword)
    {
        var user=RequireCurrent();if(IsPresident)throw new InvalidOperationException("Yetkili parolasını başkan panelinden değiştir.");
        if(string.IsNullOrEmpty(user.Salt)||!Passwords.Verify(user,currentPassword))throw new ArgumentException("Mevcut FitTeam şifren yanlış.");
        if(newPassword.Length<8||newPassword==currentPassword)throw new ArgumentException("Yeni şifre farklı ve en az 8 karakter olmalı.");
        Passwords.Set(user,newPassword);user.MustChangePassword=false;store.Save();
    }
    public void CreateOwnPassword(string password)
    {
        var user=RequireCurrent();
        if(IsPresident||!string.IsNullOrEmpty(user.PasswordHash)||(!user.EmailVerified&&!user.PhoneVerified))throw new InvalidOperationException("Önce hesabını doğrulayarak giriş yap veya mevcut FitTeam şifreni değiştir.");
        if(password.Length<8)throw new ArgumentException("FitTeam şifresi en az 8 karakter olmalı.");
        Passwords.Set(user,password);user.MustChangePassword=false;store.Save();
    }
    public void AdminSetUserActive(Guid id,bool active)
    {
        var u=AdminTarget(id);u.IsActive=active;store.Save();
    }
    public Team AdminAddClub(string name,string username,string contactEmail,string password,string gender,string sport="Futbol")
    {
        RequireAdministrator();if(!Branches.Contains(sport)||sport=="Fitness")throw new ArgumentException("Kulüp branşını seç.");if(name.Trim().Length<2||!new[]{"Kadın","Erkek","Karma"}.Contains(gender)||Data.Teams.Any(t=>t.Name.Equals(name.Trim(),StringComparison.OrdinalIgnoreCase)))throw new ArgumentException("Kulüp adı veya takım türü geçersiz; aynı kulüp tekrar eklenemez.");
        var user=AdminAddUser(username,name,Choices.Roles[3],contactEmail,"",password,sport);user.IsClubAccount=true;
        var team=new Team{Name=name.Trim(),CoachId=user.Id,Gender=gender,SportBranch=sport};Data.Teams.Add(team);store.Save();return team;
    }
    public void AdminSetClubActive(Guid id,bool active)
    {
        RequireAdministrator();var team=Data.Teams.FirstOrDefault(t=>t.Id==id)??throw new ArgumentException("Kulüp bulunamadı.");
        team.IsActive=active;if(!active){team.JoinPasswordHash="";team.JoinPasswordSalt="";}team.AccessCodeHash="";team.AccessCodeExpires=DateTimeOffset.MinValue;
        var user=Data.Users.FirstOrDefault(u=>u.Id==team.CoachId);if(user?.IsClubAccount==true)user.IsActive=active;store.Save();
    }
    public string AdminIssueClubCode(Guid id)
    {
        RequireAdministrator();var team=Data.Teams.FirstOrDefault(t=>t.Id==id&&t.IsActive)??throw new ArgumentException("Aktif kulüp seç.");
        var user=Data.Users.FirstOrDefault(u=>u.Id==team.CoachId&&u.IsActive&&u.IsClubAccount);
        if(user==null)throw new ArgumentException("Kodla giriş için kulübü + Kulüp ekle düğmesinden ayrı bir kulüp hesabıyla oluştur.");
        string code=RandomNumberGenerator.GetInt32(0,1000000).ToString("D6");
        team.AccessCodeHash=Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(team.Id+":"+code)));
        team.AccessCodeExpires=administrationTime.GetUtcNow().AddMinutes(2);team.AccessCodeAttempts=0;store.Save();return code;
    }
    public bool LoginClubCode(string username,string code)
    {
        Logout();var user=Data.Users.FirstOrDefault(u=>u.Username.Equals(username.Trim(),StringComparison.OrdinalIgnoreCase)&&u.IsActive&&u.IsClubAccount);
        var team=Data.Teams.FirstOrDefault(t=>t.IsActive&&t.Name.Equals(username.Trim(),StringComparison.OrdinalIgnoreCase))??(user==null?null:Data.Teams.FirstOrDefault(t=>t.CoachId==user.Id&&t.IsActive));
        if(team!=null)user=Data.Users.FirstOrDefault(u=>u.Id==team.CoachId&&u.IsActive&&u.IsClubAccount);
        if(user==null)return false;
        if(team==null||string.IsNullOrEmpty(team.AccessCodeHash)||team.AccessCodeExpires<=administrationTime.GetUtcNow()||team.AccessCodeAttempts>=5)return false;
        team.AccessCodeAttempts++;
        bool valid=Regex.IsMatch(code,@"^[0-9]{6}$")&&CryptographicOperations.FixedTimeEquals(Convert.FromHexString(team.AccessCodeHash),SHA256.HashData(Encoding.UTF8.GetBytes(team.Id+":"+code)));
        if(valid){team.AccessCodeHash="";Current=user;LoginMethod="Kulübün tek kullanımlık giriş kodu doğrulandı";}
        store.Save();return valid;
    }
}

public sealed partial class FitService
{
    public static readonly string[] Branches=["Fitness","Futbol","Basketbol","Voleybol"];
    public static string SportOf(User user)=>string.IsNullOrEmpty(user.SportBranch)?user.Role==Choices.Roles[0]||user.Role==Choices.Roles[2]?"Fitness":"Futbol":user.SportBranch;
    public static string RoleLabel(User user)=>user.Role==Choices.Roles[3]?SportOf(user)+" kulübü antrenörü":user.Role==Choices.Roles[1]?SportOf(user)+" sporcusu":user.Role;
    public void AdminApproveUser(Guid id)
    {
        var user=AdminTarget(id);
        if(!string.IsNullOrEmpty(user.RequestedRole))
        {
            if(!user.NeedsApproval || user.RequestedRole!=(SportOf(user)=="Fitness"?Choices.Roles[2]:Choices.Roles[3]))throw new InvalidOperationException("Antrenör başvurusu geçersiz.");
            user.Role=user.RequestedRole;user.RequestedRole="";
        }
        user.NeedsApproval=false;user.IsActive=true;store.Save();
    }
    public void SetCoachHours(User target,List<CoachHours> hours)
    {
        var current=RequireCurrent();
        if(!HasAdministrationAccess&&(target.Id!=current.Id||target.Role!=Choices.Roles[2]&&target.Role!=Choices.Roles[3]))throw new InvalidOperationException("Çalışma saatini yalnız yetkili veya hocanın kendisi düzenleyebilir.");
        if(!Data.Users.Any(u=>u.Id==target.Id)||target.Role!=Choices.Roles[2]&&target.Role!=Choices.Roles[3])throw new ArgumentException("Bir antrenör seç.");
        if(hours.Any(h=>!Choices.Days.Contains(h.Day)||h.Start>=h.End))throw new ArgumentException("Gün ve saatleri kontrol et; bitiş başlangıçtan sonra olmalı.");
        if(hours.GroupBy(h=>h.Day).Any(g=>g.OrderBy(h=>h.Start).Zip(g.OrderBy(h=>h.Start).Skip(1)).Any(p=>p.First.End>p.Second.Start)))throw new ArgumentException("Aynı günün çalışma saatleri çakışıyor.");
        target.WorkHours=hours.Select(h=>new CoachHours{Day=h.Day,Start=h.Start,End=h.End}).ToList();store.Save();
    }
}
