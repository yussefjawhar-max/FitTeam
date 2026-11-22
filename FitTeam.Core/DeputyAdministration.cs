namespace FitTeam.Core;
public sealed partial class FitService
{
    public bool HasAdministrationAccess => IsPresident || Current is {IsActive:true,Role:Choices.DeputyRole};
    public bool HasFullAdministrationView => IsPresident || Current is {IsActive:true,Role:Choices.DeputyRole,FullAdministrationView:true};
    public void CompleteDeputyProfile(string name,int age,string gender,string email,string phone,string city)
    {
        var user=RequireCurrent();if(user.Role!=Choices.DeputyRole)throw new InvalidOperationException("Yardımcı hesabıyla giriş yap.");
        email=email.Trim().ToLowerInvariant();phone=System.Text.RegularExpressions.Regex.Replace(LoginContact.NormalizeDigits(phone.Trim()),@"[\s()-]","");
        if(name.Trim().Length<2||age is <13 or >90||gender is not ("Kadın" or "Erkek" or "Belirtmek istemiyorum")||city.Length>100||
            email.Length>0&&(!System.Net.Mail.MailAddress.TryCreate(email,out var parsed)||parsed.Address!=email||Data.Users.Any(other=>other.Id!=user.Id&&other.Email.Equals(email,StringComparison.OrdinalIgnoreCase)))||
            phone.Length>0&&(!System.Text.RegularExpressions.Regex.IsMatch(phone,@"^\+[1-9][0-9]{9,14}$")||Data.Users.Any(other=>other.Id!=user.Id&&other.Phone==phone)))
            throw new ArgumentException("Adını, yaşını ve iletişim bilgilerini kontrol et.");
        user.Profile.Name=name.Trim();user.Profile.Age=age;user.Profile.Gender=gender;user.Profile.City=city.Trim();
        if(user.Email!=email){user.Email=email;user.EmailVerified=false;}if(user.Phone!=phone){user.Phone=phone;user.PhoneVerified=false;}
        user.ProfileComplete=true;Save();
    }
    private void RequireAdministrator(){if(!HasAdministrationAccess)throw new InvalidOperationException("Yetkili hesabıyla giriş yap.");}
    public User AdminCreateDeputy(string name,string username,string password)
    {
        RequirePresident();username=Usernames.Validate(username);
        if(username.Equals(Data.OwnerLoginUsername,StringComparison.OrdinalIgnoreCase))throw new ArgumentException("Bu kullanıcı adı asıl yetkiliye ait. Başka bir ad seç.");
        var user=AdminAddUser(username,name,Choices.DeputyRole,"","",password);
        user.Profile=new Profile{Name=name.Trim(),Age=0,Gender="Belirtmek istemiyorum"};user.RegisteredAt=DateTimeOffset.UtcNow;
        user.SportBranch="";store.Save();return user;
    }
}
