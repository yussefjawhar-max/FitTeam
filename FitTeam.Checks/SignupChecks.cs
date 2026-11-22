using System.Net;
using System.Text.Json;
using FitTeam.Core;
static class SignupChecks
{
    public static async Task Run(Action<bool,string> check)
    {
        var path=Path.Combine(Path.GetTempPath(),"fitteam-signup-"+Guid.NewGuid()+".json");
        try
        {
            var service=new FitService(new DataStore(path));
            var directFixture=new DirectGoogleFixture();var directAuth=new CodeAuthClient(new Uri("http://127.0.0.1:12345/"),new HttpClient(directFixture));
            var directIdentity=await directAuth.GoogleLogin("fixture-request","signed-fixture-token");
            check(directIdentity.Channel=="google"&&directFixture.Direct&&directFixture.Paths.SequenceEqual(new[]{"/google/verify","/session"}),"Google doğrudan giriş isteği SMS gönderme veya kod doğrulama çağrısı yapmaz");

            void Reject(Action action,string label){bool rejected=false;try{action();}catch(Exception ex)when(ex is ArgumentException or InvalidOperationException){rejected=true;}check(rejected,label);}
            var draft=new SignupApplication();
            for(int step=0;step<5;step++){int currentStep=step;Reject(()=>draft.ValidateStep(currentStep),"Boş zorunlu kayıt alanı ilerlemeyi engeller: "+step);}
            draft.ValidateStep(5);draft.ValidateStep(6);check(draft.Email==""&&draft.Phone=="","E-posta ve telefon adımları boş bırakılabilir");
            draft.Username="basvuran";draft.FirstName="Örnek";draft.LastName="Başvuran";draft.Age="25";draft.Sport="Basketbol";draft.Experience="3";draft.EmailAddress="signupfixture@gmail.com";draft.MobileDigits="321234567";draft.BloodGroup="AB Rh−";draft.Trainer=true;draft.Gender="Erkek";draft.Country="Türkiye";draft.CountryCode="TR";draft.City="İstanbul";draft.HomeAddress="Örnek Sokak No: 1";draft.Password=draft.PasswordConfirmation="FixtureOwn-84!";draft.Validate();
            check(draft.Email=="signupfixture@gmail.com"&&draft.Phone=="+905321234567","Kayıtta tam e-posta ve ülke koduyla telefon korunur");
            draft.Experience="26";Reject(draft.Validate,"Deneyim yılı yaştan fazla olamaz");draft.Experience="0";draft.Validate();
            draft.BloodGroup="Belirsiz değer";Reject(draft.Validate,"Geçersiz kan grubu reddedilir");draft.BloodGroup="Bilmiyorum";draft.Validate();
            async Task<CodeIdentity> Proof(string email,string phone,string channel="google",bool owner=false)
            {
                var auth=new CodeAuthClient(new Uri("http://127.0.0.1:12345/"),new HttpClient(new SignupFixture(email,channel is "google" or "google+email"?"":phone,channel,owner)));
                return await auth.Verify(phone,"phone","fixture","123456");
            }
            var wrongEmail=await Proof("otherfixture@gmail.com",draft.Phone);int count=service.Data.Users.Count;
            Reject(()=>service.LoginSignup(draft,wrongEmail),"Kayıt formundan farklı Google hesabı reddedilir");check(service.Data.Users.Count==count,"Yanlış kimlikle kayıt veri oluşturmaz");
            var phoneOnly=await Proof(draft.Email,draft.Phone,"phone");Reject(()=>service.LoginSignup(draft,phoneOnly),"Google hesabı doğrulanmadan kayıt tamamlanamaz");
            var emailOnly=await Proof(draft.Email,"","email");Reject(()=>service.LoginSignup(draft,emailOnly),"Yalnız e-posta kodu Google kimliği gereken kaydı tamamlayamaz");
            draft.PasswordConfirmation="Different-84!";Reject(draft.Validate,"Kayıt sonunda şifre tekrarı eşleşmek zorunda");draft.PasswordConfirmation=draft.Password;
            var proof=await Proof(draft.Email,draft.Phone);service.LoginSignup(draft,proof);var applicant=service.RequireCurrent();
            check(applicant.NeedsApproval&&applicant.RequestedRole==Choices.Roles[3]&&applicant.Role!=Choices.Roles[3],"Antrenör başvurusu yetkili onayına kadar antrenör rolü kazanmaz");
            check(applicant.Profile.Name=="Örnek Başvuran"&&applicant.Profile.Age==25&&applicant.Profile.BloodGroup=="Bilmiyorum"&&applicant.ExperienceYears==0,"Adım adım kayıt bilgileri kan grubu ve deneyimle kaydedilir");
            check(FitService.MatchesTrainerEntry(applicant,Choices.Roles[3],"Basketbol")&&!FitService.MatchesTrainerEntry(applicant,Choices.Roles[3],"Voleybol"),"Bekleyen antrenör kendi branşının girişinde onay durumunu görür");
            Reject(()=>service.AdminApproveUser(applicant.Id),"Antrenör kendi başvurusunu onaylayamaz");
            var owner=await Proof("jawharyusuf446@gmail.com","","president-password",true);service.LoginCode(owner);service.AdminApproveUser(applicant.Id);
            check(applicant.Role==Choices.Roles[3]&&applicant.RequestedRole==""&&!applicant.NeedsApproval,"Yetkili onayı başvurulan antrenör rolünü açar");
            service.LoginCode(proof);check(service.RequireCurrent().EmailVerified&&!service.RequireCurrent().PhoneVerified,"Google doğrudan giriş yapar; SMS doğrulaması yapılmış gibi gösterilmez");
            check(service.Login(draft.Email,"FixtureOwn-84!"),"Kayıt sonunda kişinin seçtiği şifreyle giriş yapılır");
            service.ChangeOwnPassword("FixtureOwn-84!","ChangedOwn-85!");service.ChangeOwnPassword("ChangedOwn-85!","ChangedAgain-86!");
            check(!service.Login(draft.Email,"FixtureOwn-84!")&&!service.Login(draft.Email,"ChangedOwn-85!")&&service.Login(draft.Email,"ChangedAgain-86!"),"Şifre aynı oturumda iki kez değiştirilebilir; yalnız son şifre geçerli kalır");
            service.LoginCode(owner);
            var oldPhone=applicant.Phone;draft.MobileDigits="339876543";var existingProof=await Proof(draft.Email,draft.Phone);
            Reject(()=>service.LoginSignup(draft,existingProof),"Mevcut hesabı yeni kayıtla yeniden değiştirmek reddedilir");check(applicant.Phone==oldPhone,"Reddedilen kayıt mevcut hesabın telefonunu değiştirmez");
            var club=service.AdminAddClub("Örnek Basket Kulübü","signupclub","signupclub@gmail.com","FixturePass-84!","Karma","Basketbol");var code=service.AdminIssueClubCode(club.Id);
            check(service.LoginClubCode("örnek basket kulübü",code),"Kulüp adı ve tek kullanımlık kodla giriş yapılır");check(!service.LoginClubCode(club.Name,code),"Kulüp adına verilen kod ikinci kez kullanılamaz");
            var memberDraft=new SignupApplication{Username="uyeFixture",FirstName="Örnek",LastName="Üye",Age="24",Sport="Fitness",Experience="0",EmailAddress="memberfixture@gmail.com",MobileDigits="359876543",Password="MemberOwn-84!",PasswordConfirmation="MemberOwn-84!",Gender="Erkek",Country="Türkiye",CountryCode="TR",HomeAddress="Örnek Sokak No: 2",City="İstanbul"};
            memberDraft.TrainingStart=new TimeOnly(18,0);memberDraft.CoachIds=[service.Data.Users.First(u=>u.Role==Choices.Roles[2]).Id];
            var wrongCombined=await Proof("other-combined@gmail.com","", "google+email");
            Reject(()=>service.LoginSignup(memberDraft,wrongCombined),"Google ve Gmail kodu başka kayıt adresine aktarılamaz");
            var memberProof=await Proof(memberDraft.Email,"", "google+email");service.LoginSignup(memberDraft,memberProof);
            check(service.RequireCurrent().ProfileComplete&&!service.RequireCurrent().NeedsApproval&&!service.IsPresident,"Google ve Gmail koduyla doğrulanan yeni üye profilini tamamlar");
            check(service.RequireCurrent().EmailVerified&&!service.RequireCurrent().PhoneVerified&&service.LoginMethod.Contains("Gmail kodu doğrulandı"),"Google ve Gmail kodu doğrulaması telefon doğrulaması iddiası oluşturmaz");
            check(service.Login(memberDraft.Phone,"MemberOwn-84!"),"Kayıtta verilen telefon ve FitTeam şifresiyle SMS olmadan giriş yapılır");
            var noPhoneDraft=new SignupApplication{Username="noPhoneGoogleFixture",FirstName="Telefonsuz",LastName="Üye",Age="24",Sport="Fitness",Experience="0",EmailAddress="nophonegooglefixture@gmail.com",Password="NoPhoneOwn-84!",PasswordConfirmation="NoPhoneOwn-84!",Gender="Erkek",Country="Türkiye",CountryCode="TR",HomeAddress="Örnek Sokak No: 4",City="İstanbul",TrainingStart=new TimeOnly(18,0),CoachIds=memberDraft.CoachIds.ToList()};
            int beforeNoPhone=service.Data.Users.Count;
            var emptyPhoneProof=await Proof(noPhoneDraft.Email,"","google+phone");
            Reject(()=>service.LoginSignup(noPhoneDraft,emptyPhoneProof),"Boş telefon SMS ile doğrulanmış kabul edilmez");
            check(service.Data.Users.Count==beforeNoPhone,"Boş telefonlu SMS kanıtı kayıt oluşturmaz");
            var noPhoneProof=await Proof(noPhoneDraft.Email,"","google+email");service.LoginSignup(noPhoneDraft,noPhoneProof);
            check(service.RequireCurrent().ProfileComplete&&service.RequireCurrent().EmailVerified&&!service.RequireCurrent().PhoneVerified&&service.RequireCurrent().Phone==""&&!service.IsPresident,"Google ve Gmail koduyla kayıt telefon olmadan tamamlanır; telefon veya başkan yetkisi uydurulmaz");
            var smsDraft=new SignupApplication{Username="smsUyeFixture",FirstName="SMS",LastName="Üyesi",Age="24",Sport="Fitness",Experience="0",EmailAddress="smsmemberfixture@gmail.com",MobileDigits="369876543",Password="SmsMemberOwn-84!",PasswordConfirmation="SmsMemberOwn-84!",Gender="Erkek",Country="Türkiye",CountryCode="TR",HomeAddress="Örnek Sokak No: 3",City="İstanbul",TrainingStart=new TimeOnly(18,0),CoachIds=memberDraft.CoachIds.ToList()};
            int beforeSmsSignup=service.Data.Users.Count;
            var mismatchedSms=await Proof(smsDraft.Email,"+905321234560","google+phone");
            Reject(()=>service.LoginSignup(smsDraft,mismatchedSms),"Google ve SMS kanıtı başka telefon numarasıyla kayıt oluşturamaz");
            check(service.Data.Users.Count==beforeSmsSignup,"Telefonu eşleşmeyen SMS kaydı veri oluşturmaz");
            var missingSmsPhone=await Proof(smsDraft.Email,"","google+phone");
            Reject(()=>service.LoginSignup(smsDraft,missingSmsPhone),"Telefon numarası bulunmayan Google ve SMS kanıtı reddedilir");
            var smsProof=await Proof(smsDraft.Email,smsDraft.Phone,"google+phone");service.LoginSignup(smsDraft,smsProof);
            check(service.RequireCurrent().ProfileComplete&&service.RequireCurrent().EmailVerified&&service.RequireCurrent().PhoneVerified&&service.RequireCurrent().Phone==smsDraft.Phone,"Google ve eşleşen SMS koduyla kayıt Gmail ve telefon doğrulamasını korur");
            check(!service.IsPresident&&service.LoginMethod.Contains("Kod doğrulandı"),"Google ve SMS kaydı yalnız üye yetkisiyle açılır");
            int beforeDuplicatePhone=service.Data.Users.Count;
            var duplicatePhone=await Proof("duplicated-smsmember@gmail.com",smsDraft.Phone,"google+phone");
            Reject(()=>service.LoginCode(duplicatePhone),"Google ve SMS girişinde başka üyeye bağlı telefon yeniden bağlanamaz");
            check(service.Data.Users.Count==beforeDuplicatePhone&&service.Current==null,"Yinelenen SMS telefonu ikinci hesap veya oturum oluşturmaz");
            var loaded=new DataStore(path).Data.Users.First(u=>u.Id==applicant.Id);check(loaded.Profile.BloodGroup=="Bilmiyorum"&&loaded.ExperienceYears==0&&FitService.SportOf(loaded)=="Basketbol","Kan grubu, deneyim ve branş uygulama yeniden açıldığında korunur");
        }
        finally{File.Delete(path);File.Delete(path+".bak");File.Delete(path+".tmp");}
    }
}
sealed class SignupFixture(string email,string phone,string channel,bool owner):HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken cancellationToken)
    {
        var json=request.RequestUri!.AbsolutePath=="/session"?JsonSerializer.Serialize(new{contact=email,phone,channel,isOwner=owner,googleSubject="signup-fixture-subject"}):JsonSerializer.Serialize(new{token="fixture-only"});
        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK){Content=new StringContent(json,System.Text.Encoding.UTF8,"application/json")});
    }
}

sealed class DirectGoogleFixture:HttpMessageHandler
{
    public bool Direct;public List<string> Paths=[];
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken cancellationToken)
    {
        var path=request.RequestUri!.AbsolutePath;Paths.Add(path);
        if(path=="/google/verify")Direct=JsonDocument.Parse(await request.Content!.ReadAsStringAsync(cancellationToken)).RootElement.GetProperty("direct").GetBoolean();
        var json=path=="/session"?JsonSerializer.Serialize(new{contact="directfixture@gmail.com",channel="google",isOwner=false,googleSubject="fixture-subject",phone=""}):JsonSerializer.Serialize(new{token="fixture-only"});
        return new HttpResponseMessage(HttpStatusCode.OK){Content=new StringContent(json,System.Text.Encoding.UTF8,"application/json")};
    }
}
