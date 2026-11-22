namespace FitTeam.Core;

public sealed class SignupApplication
{
    public string Username { get; set; } = "";
    public PostalAddress? Address {get;set;}
    public Guid? RequestedClubId {get;set;}
    public TimeOnly? TrainingStart {get;set;}
    public int TrainingHours {get;set;}=2;
    public static readonly string[] BloodGroups=["Bilmiyorum","A Rh+","A Rh−","B Rh+","B Rh−","AB Rh+","AB Rh−","0 Rh+","0 Rh−"];
    public string FirstName {get;set;}="";
    public string LastName {get;set;}="";
    public string Age {get;set;}="";
    public string Sport {get;set;}="";
    public string Experience {get;set;}="";
    public string EmailAddress {get;set;}="";
    public string MobileDigits {get;set;}="";
    public string BloodGroup {get;set;}="Bilmiyorum";
    public bool Trainer {get;set;}
    public string Password {get;set;}="";
    public string PasswordConfirmation {get;set;}="";
    public decimal Height {get;set;}=175;
    public decimal Weight {get;set;}=75;
    public decimal TargetWeight {get;set;}=70;
    public string Goal {get;set;}=Choices.Goals[0];
    public List<DayOfWeek> Days {get;set;}=[DayOfWeek.Monday,DayOfWeek.Wednesday,DayOfWeek.Friday];
    public List<Guid> CoachIds {get;set;}=[];
    public Guid? TeamId {get;set;}
    public ClubAccessTicket? ClubTicket {get;set;}
    public string Gender {get;set;}="";
    public string Country {get;set;}="";
    public string CountryCode {get;set;}="";
    public string HomeAddress {get;set;}="";
    public string City {get;set;}="";
    public void ValidatePassword()
    {
        Username = Usernames.Validate(Username);
        if(Password.Length<8)throw new ArgumentException("FitTeam şifren en az 8 karakter olmalı.");
        if(Password!=PasswordConfirmation)throw new ArgumentException("İki şifre eşleşmiyor. Aynı şifreyi tekrar yaz.");
    }
    public void ValidateTraining()
    {
        if(!new[]{"Kadın","Erkek","Belirtmek istemiyorum"}.Contains(Gender)||Country.Trim().Length<2||City.Trim().Length<2)throw new ArgumentException("Cinsiyet seçimini, ülke ve şehir bilgilerini doldur.");
        var selectedCountry=CountryCatalog.ByCode(CountryCode)??CountryCatalog.Resolve(Country);
        if(selectedCountry==null)throw new ArgumentException("Listeden bir ülke seç.");CountryCode=selectedCountry.Code;Country=selectedCountry.Turkish;
        if(Address!=null){Address.Validate(CountryCode);HomeAddress=Address.Display;City=Address.Province;}
        if(HomeAddress.Trim().Length<8)throw new ArgumentException("Ev adresini en az 8 karakterle yaz.");
        if(Trainer)return;
        if(Sport=="Fitness"&&CoachIds.Count==0)throw new ArgumentException("Fitness kaydı için en az bir hoca seç.");
        if(TrainingStart==null||TrainingHours is not (2 or 3)||TrainingStart.Value.Hour+TrainingHours>24||TrainingStart.Value.Hour+TrainingHours==24&&TrainingStart.Value.Minute>0)throw new ArgumentException("Ders başlangıç saatini ve 2 veya 3 saatlik aralığı seç.");
        if(Height is <100 or >230 || Weight is <30 or >300 || TargetWeight is <30 or >300 || !Choices.Goals.Contains(Goal)||Days.Count==0||Days.Any(d=>!Choices.Days.Contains(d)))throw new ArgumentException("Boy, kilo, hedef ve en az bir uygun gün seçimini kontrol et.");
    }
    public string Email
    {
        get
        {
            var value=EmailAddress.Trim().ToLowerInvariant();
            if(value.Length==0)return "";
            if(value.Length>254||!System.Net.Mail.MailAddress.TryCreate(value,out var address)||address.Address!=value||!address.Host.Contains('.'))
                throw new ArgumentException("Geçerli bir e-posta adresi yaz veya alanı boş bırak.");
            return value;
        }
    }
    public string Phone=>string.IsNullOrWhiteSpace(MobileDigits)?"":LoginContact.TurkishMobile(MobileDigits);
    public string DesiredRole=>Trainer?(Sport=="Fitness"?Choices.Roles[2]:Choices.Roles[3]):Sport=="Fitness"?Choices.Roles[0]:Choices.Roles[1];
    public void ValidateStep(int step)
    {
        Age=LoginContact.NormalizeDigits(Age);Experience=LoginContact.NormalizeDigits(Experience);
        switch(step)
        {
            case 0: if(FirstName.Trim().Length<2)throw new ArgumentException("Adını yaz; bu alan zorunlu.");break;
            case 1: if(LastName.Trim().Length<2)throw new ArgumentException("Soyadını yaz; bu alan zorunlu.");break;
            case 2: if(!int.TryParse(Age,out var age)||age is <13 or >90)throw new ArgumentException("Yaşını 13 ile 90 arasında yaz.");break;
            case 3: if(!FitService.Branches.Contains(Sport))throw new ArgumentException("Spor branşını seç.");break;
            case 4: if(!int.TryParse(Experience,out var years)||years<0||!int.TryParse(Age,out var a)||years>a)throw new ArgumentException("Deneyim yılını yaz. Yeni başlıyorsan 0 yazabilirsin; yaşından büyük olamaz.");break;
            case 5: _=Email;break;
            case 6: _=Phone;break;
            case 7: if(!BloodGroups.Contains(BloodGroup))throw new ArgumentException("Kan grubunu seç; bilmiyorsan Bilmiyorum seç.");break;
            default: throw new ArgumentOutOfRangeException(nameof(step));
        }
    }
    public void Validate(){for(int i=0;i<8;i++)ValidateStep(i);ValidateTraining();ValidatePassword();}
}

public sealed partial class FitService
{
    public User RegisterLocalSignup(SignupApplication application) => RegisterLocalSignup(application, false);

    internal User RegisterLocalSignup(SignupApplication application, bool emailVerified)
    {
        application.Validate();
        if(application.Username.Equals(Data.OwnerLoginUsername,StringComparison.OrdinalIgnoreCase))throw new ArgumentException("Bu kullanıcı adı asıl yetkiliye ait. Başka bir ad seç.");
        if(Data.Users.Any(u=>u.Username.Equals(application.Username,StringComparison.OrdinalIgnoreCase)))throw new ArgumentException("Bu kullanıcı adı kullanılıyor. Başka bir ad seç.");
        if(application.Email.Length>0&&Data.Users.Any(u=>u.Username.Equals(application.Email,StringComparison.OrdinalIgnoreCase)||u.Email.Equals(application.Email,StringComparison.OrdinalIgnoreCase)))throw new ArgumentException("Bu e-posta adresi zaten kayıtlı. Normal giriş bölümünü kullan.");
        if(application.Phone.Length>0&&Data.Users.Any(u=>u.Phone==application.Phone))throw new ArgumentException("Bu telefon başka hesaba kayıtlı. Normal giriş bölümünü kullan.");
        if(application.CoachIds.Any(id=>!Data.Users.Any(u=>u.Id==id&&u.IsActive&&!u.NeedsApproval&&u.Role==Choices.Roles[2])))throw new ArgumentException("Seçilen hoca artık aktif değil.");
        if(application.RequestedClubId!=null&&(application.Trainer||!Data.Teams.Any(t=>t.Id==application.RequestedClubId&&t.IsActive&&t.SportBranch==application.Sport)))throw new ArgumentException("Katılmak istediğin kulübü branşına göre seç.");
        if(application.ClubTicket!=null){var team=RequireClubTicket(application.ClubTicket);if(application.Trainer||team.SportBranch!=application.Sport)throw new ArgumentException("Kulüp kaydında branş veya hesap türü geçersiz.");application.TeamId=team.Id;}
        else if(application.TeamId!=null)throw new ArgumentException("Kulübe katılmak için kulüp girişinde 8 haneli şifreyi yaz.");
        var user=new User{RegisteredAt=DateTimeOffset.UtcNow,RequestedClubId=application.ClubTicket==null?application.RequestedClubId:null,Username=application.Username,Email=application.Email,EmailVerified=emailVerified && application.Email.Length>0,Phone=application.Phone,SportBranch=application.Sport,Role=application.Sport=="Fitness"?Choices.Roles[0]:Choices.Roles[1],RequestedRole=application.Trainer?application.DesiredRole:"",ExperienceYears=int.Parse(application.Experience),NeedsApproval=application.Trainer,ProfileComplete=true,TeamId=application.TeamId,
            Profile=new Profile{Name=application.FirstName.Trim()+" "+application.LastName.Trim(),Age=int.Parse(application.Age),BloodGroup=application.BloodGroup,Gender=application.Gender,Country=application.Country.Trim(),CountryCode=application.CountryCode,Address=application.Address,HomeAddress=application.HomeAddress.Trim(),City=application.City.Trim(),Height=application.Height,Weight=application.Weight,InitialWeight=application.Weight,TargetWeight=application.TargetWeight,Goal=application.Goal,AvailableDays=application.Days.ToList()}};
        user.CoachIds=application.Sport=="Fitness"?application.CoachIds.Distinct().ToList():[];user.CoachId=user.CoachIds.Count==0?null:user.CoachIds[0];Passwords.Set(user,application.Password);
        user.Profile.TrainingStart=application.TrainingStart;user.Profile.TrainingHours=application.TrainingHours;
        Logout();Data.Users.Add(user);Current=user;LoginMethod="FitTeam hesabınla giriş yaptın";
        if(!application.Trainer)AddWeightInternal(user,DateOnly.FromDateTime(DateTime.Today),application.Weight);
        store.Save();return user;
    }
    public void LoginSignup(SignupApplication application,CodeIdentity proof)
    {
        application.Validate();
        if(proof.IsOwner||proof.Channel is not ("google" or "google+email" or "google+phone")||string.IsNullOrEmpty(proof.GoogleSubject)||!proof.Contact.Equals(application.Email,StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Kayıt formuna yazdığın Gmail hesabını Google ile doğrula.");
        if(proof.Channel=="google+phone"&&(application.Phone.Length==0||proof.Phone!=application.Phone))
            throw new InvalidOperationException("Doğrulanan iletişim bilgisi kayıt formuyla eşleşmiyor.");
        var existing=Data.Users.FirstOrDefault(u=>u.Email.Equals(application.Email,StringComparison.OrdinalIgnoreCase));
        if(existing!=null&&(!existing.IsActive||existing.ProfileComplete||!existing.NeedsApproval))throw new InvalidOperationException("Bu hesap zaten kayıtlı veya girişe kapalı. Normal giriş bölümünü kullan.");
        if(application.Phone.Length>0&&Data.Users.Any(u=>u.Id!=existing?.Id&&u.Phone==application.Phone))throw new InvalidOperationException("Bu telefon başka hesaba kayıtlı. Yetkiliyle iletişime geç.");
        if(application.CoachIds.Any(id=>!Data.Users.Any(u=>u.Id==id&&u.IsActive&&!u.NeedsApproval&&u.Role==Choices.Roles[2])))throw new ArgumentException("Seçilen hoca artık aktif değil.");
        if(application.ClubTicket!=null){var team=RequireClubTicket(application.ClubTicket);if(application.Trainer||team.SportBranch!=application.Sport)throw new ArgumentException("Kulüp kaydında branş veya hesap türü geçersiz.");application.TeamId=team.Id;}
        else if(application.TeamId!=null)throw new ArgumentException("Kulübe katılmak için kulüp girişinde 8 haneli şifreyi yaz.");
        LoginCode(proof);ApplySignup(application,proof);
    }
    public void ApplySignup(SignupApplication application,CodeIdentity proof)
    {
        application.Validate();var user=RequireCurrent();
        if(IsPresident||proof.IsOwner||user.ProfileComplete||!user.NeedsApproval)throw new InvalidOperationException("Bu hesap zaten kayıtlı. Normal giriş bölümünü kullan.");
        if(proof.Channel is not ("google" or "google+email" or "google+phone")||string.IsNullOrEmpty(proof.GoogleSubject)||!proof.Contact.Equals(application.Email,StringComparison.OrdinalIgnoreCase)||user.Email!=proof.Contact||user.AuthenticationUid!=proof.GoogleSubject)
            throw new InvalidOperationException("Kayıt formuna yazdığın Gmail hesabını Google ile doğrula.");
        if(proof.Channel=="google+phone"&&(application.Phone.Length==0||proof.Phone!=application.Phone))
            throw new InvalidOperationException("Doğrulanan iletişim bilgisi kayıt formuyla eşleşmiyor.");
        if(Data.Users.Any(u=>u.Id!=user.Id&&(application.Phone.Length>0&&u.Phone==application.Phone||application.Email.Length>0&&u.Email.Equals(application.Email,StringComparison.OrdinalIgnoreCase))))throw new InvalidOperationException("Bu Gmail veya telefon başka bir hesaba kayıtlı. Yetkiliyle iletişime geç.");
        user.Profile.Name=application.FirstName.Trim()+" "+application.LastName.Trim();
        user.Profile.Age=int.Parse(application.Age);user.Profile.BloodGroup=application.BloodGroup;
        user.ExperienceYears=int.Parse(application.Experience);user.SportBranch=application.Sport;
        user.RequestedRole=application.Trainer?application.DesiredRole:"";
        user.Role=application.Sport=="Fitness"?Choices.Roles[0]:Choices.Roles[1];
        user.Phone=application.Phone;user.PhoneVerified=application.Phone.Length>0&&proof.Channel=="google+phone"&&proof.Phone==application.Phone;
        user.Profile.Gender=application.Gender;user.Profile.Country=application.Country.Trim();user.Profile.CountryCode=application.CountryCode;user.Profile.HomeAddress=application.HomeAddress.Trim();user.Profile.City=application.City.Trim();
        user.Profile.Height=application.Height;user.Profile.Weight=application.Weight;user.Profile.InitialWeight=application.Weight;user.Profile.TargetWeight=application.TargetWeight;user.Profile.Goal=application.Goal;user.Profile.AvailableDays=application.Days.ToList();
        user.CoachIds=application.Sport=="Fitness"?application.CoachIds.Distinct().ToList():[];user.CoachId=user.CoachIds.FirstOrDefault() is var coach&&coach!=Guid.Empty?coach:null;user.TeamId=application.TeamId;
        Passwords.Set(user,application.Password);user.MustChangePassword=false;
        if(!application.Trainer)AddWeightInternal(user,DateOnly.FromDateTime(DateTime.Today),application.Weight);
        user.ProfileComplete=true;user.NeedsApproval=application.Trainer;store.Save();
    }
    public static bool MatchesTrainerEntry(User user,string role,string? sport)=>
        (user.Role==role || user.NeedsApproval&&user.RequestedRole==role) && (sport==null||SportOf(user)==sport);
}
