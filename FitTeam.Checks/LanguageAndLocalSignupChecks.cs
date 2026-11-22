using System.Globalization;
using FitTeam.Core;
static class LanguageAndLocalSignupChecks
{
    public static void Run(Action<bool,string> check)
    {
        string path=Path.Combine(Path.GetTempPath(),"fitteam-local-language-"+Guid.NewGuid()+".json");
        try
        {
            AppLanguage.Use("en");check(AppLanguage.Text("Kaydı tamamla ve giriş yap")=="Complete registration and sign in","İngilizce kayıt düğmesi dil dosyasından yüklenir");
            check(AppLanguage.Text("Telefon numaran (isteğe bağlı)")=="Your phone number (optional)","İngilizce kayıt telefon alanının isteğe bağlı olduğunu gösterir");
            check(AppLanguage.Text("E-posta adresin (isteğe bağlı)")=="Your email address (optional)","İngilizce kayıt genel e-posta alanının isteğe bağlı olduğunu gösterir");
            check(AppLanguage.Text("Adım 3 / 11")=="Step 3 / 11","İngilizce adım sayacı sayıları koruyarak çevrilir");
            AppLanguage.Use("ar");check(AppLanguage.RightToLeft&&AppLanguage.Text("Kaydı tamamla ve giriş yap")=="إكمال التسجيل والدخول","Arapça kayıt düğmesi yüklenir ve sağdan sola yön seçilir");
            check(AppLanguage.Text("Telefon numaran (isteğe bağlı)")=="رقم هاتفك (اختياري)","Arapça kayıt telefon alanının isteğe bağlı olduğunu gösterir");
            check(AppLanguage.Text("E-posta adresin (isteğe bağlı)")=="عنوان بريدك الإلكتروني (اختياري)","Arapça kayıt genel e-posta alanının isteğe bağlı olduğunu gösterir");
            check(CultureInfo.CurrentCulture.DateTimeFormat.Calendar is GregorianCalendar,"Arapça arayüz uygulamanın takvim tarihlerini değiştirmez");
            AppLanguage.Use("tr");check(!AppLanguage.RightToLeft&&AppLanguage.Text("Kaydı tamamla ve giriş yap")=="Kaydı tamamla ve giriş yap","Türkçe sürüm özgün metni korur");
            var service=new FitService(new DataStore(path));var draft=new SignupApplication{Username="محمد_أحمد",FirstName="محمد",LastName="أحمد",Age="22",Sport="Fitness",Experience="1",EmailAddress="localfixture@gmail.com",MobileDigits="359876543",Gender="Erkek",Country="مصر",CountryCode="EG",HomeAddress="عنوان تجريبي رقم 1",City="القاهرة",Password="FixtureOwn-84!",PasswordConfirmation="FixtureOwn-84!"};
            draft.TrainingStart=new TimeOnly(18,0);draft.CoachIds=[service.Data.Users.First(u=>u.Role==Choices.Roles[2]).Id];
            draft.Age="٢٢";draft.Experience="١";draft.MobileDigits="٣٥٩٨٧٦٥٤٣";
            var user=service.RegisterLocalSignup(draft);check(user.ProfileComplete&&!user.NeedsApproval&&service.Current!.Id==user.Id,"Yeni üye Google veya SMS çağrısı olmadan kişisel şifresiyle kayıt olur");
            check(!user.EmailVerified&&!user.PhoneVerified&&!service.IsPresident,"Yerel kayıt doğrulanmış Gmail veya yönetici yetkisi uydurmaz");
            check(user.Profile.Age==22&&user.ExperienceYears==1&&user.Phone=="+905359876543","Arapça rakamlarla yaş, deneyim ve telefon kaydı yapılır");
            check(user.Profile.Name=="محمد أحمد"&&user.Profile.CountryCode=="EG","Arapça kişisel bilgiler değiştirilmeden kaydedilir");
            check(service.Login(draft.Email,"FixtureOwn-84!"),"Yeni kişinin seçtiği uygulama şifresiyle giriş yapılır");
            bool duplicate=false;try{service.RegisterLocalSignup(draft);}catch(ArgumentException){duplicate=true;}check(duplicate,"Aynı yerel kayıt mevcut hesabın şifresini değiştiremez");
            int count=service.Data.Users.Count;draft.EmailAddress="mismatchfixture@gmail.com";draft.MobileDigits="339876543";draft.PasswordConfirmation="WrongRepeat-84!";
            bool mismatch=false;try{service.RegisterLocalSignup(draft);}catch(ArgumentException){mismatch=true;}check(mismatch&&service.Data.Users.Count==count,"Şifre tekrarı yanlışsa yerel hesap oluşturulmaz");
            service.Login("localfixture@gmail.com","FixtureOwn-84!");service.ChangeOwnPassword("FixtureOwn-84!","FixtureNew-85!");check(!service.Login(draft.Email,"FixtureOwn-84!")&&service.Login("localfixture@gmail.com","FixtureNew-85!"),"Yerel kayıt sonrasında kişi şifresini değiştirebilir");
            var loaded=new FitService(new DataStore(path));check(loaded.Login("localfixture@gmail.com","FixtureNew-85!")&&loaded.RequireCurrent().Profile.Name=="محمد أحمد","Yerel şifre ve Arapça profil yeniden açıldığında korunur");
            var noPhone=new SignupApplication{Username="nophonelocalfixture",FirstName="Telefonsuz",LastName="Üye",Age="22",Sport="Fitness",Experience="0",Gender="Erkek",Country="Türkiye",CountryCode="TR",HomeAddress="Örnek Sokak No: 5",City="İstanbul",Password="NoPhoneOwn-84!",PasswordConfirmation="NoPhoneOwn-84!",TrainingStart=new TimeOnly(18,0),CoachIds=draft.CoachIds.ToList()};
            var noPhoneUser=service.RegisterLocalSignup(noPhone);check(noPhoneUser.Email==""&&noPhoneUser.Phone==""&&!noPhoneUser.PhoneVerified&&!noPhoneUser.EmailVerified&&!service.IsPresident&&noPhoneUser.ProfileComplete,"Yerel üye e-posta veya telefon olmadan kayıt olur; doğrulama veya yetkili yetkisi kazanmaz");
            noPhone.Username="secondnophonefixture";noPhone.EmailAddress="   ";noPhone.MobileDigits="   ";
            var secondNoPhone=service.RegisterLocalSignup(noPhone);check(secondNoPhone.Email==""&&secondNoPhone.Phone==""&&secondNoPhone.Id!=noPhoneUser.Id,"Birden fazla kişi e-posta ve telefonu boş bırakabilir; boş değerler çakışmaz");
            int beforeInvalid=service.Data.Users.Count;noPhone.Username="invalidphonefixture";noPhone.EmailAddress="invalidphonefixture@gmail.com";noPhone.MobileDigits="123";
            bool invalidPhone=false;try{service.RegisterLocalSignup(noPhone);}catch(ArgumentException){invalidPhone=true;}check(invalidPhone&&service.Data.Users.Count==beforeInvalid,"Telefon yazılırsa eksik numara reddedilir ve hesap oluşturulmaz");
            noPhone.MobileDigits="359876543";
            bool duplicatePhone=false;try{service.RegisterLocalSignup(noPhone);}catch(ArgumentException){duplicatePhone=true;}check(duplicatePhone&&service.Data.Users.Count==beforeInvalid,"İsteğe bağlı yazılan telefon başka hesaba aitse kayıt reddedilir");
            noPhone.Username="generalemailfixture";noPhone.EmailAddress="  Optional@Example.org  ";noPhone.MobileDigits="";
            var generalEmail=service.RegisterLocalSignup(noPhone);check(generalEmail.Email=="optional@example.org"&&!generalEmail.EmailVerified&&!generalEmail.PhoneVerified&&!service.IsPresident,"Gmail dışındaki tam e-posta adresi iletişim bilgisi olarak kaydedilir ve doğrulanmış sayılmaz");
            int beforeInvalidEmail=service.Data.Users.Count;noPhone.Username="invalidemailfixture";noPhone.EmailAddress="gecersizadres";
            bool invalidEmail=false;try{service.RegisterLocalSignup(noPhone);}catch(ArgumentException){invalidEmail=true;}check(invalidEmail&&service.Data.Users.Count==beforeInvalidEmail,"Yazılan geçersiz e-posta reddedilir; boş adres izni geçersiz adresleri kabul etmez");
            noPhone.EmailAddress="OPTIONAL@EXAMPLE.ORG";
            bool duplicateEmail=false;try{service.RegisterLocalSignup(noPhone);}catch(ArgumentException){duplicateEmail=true;}check(duplicateEmail&&service.Data.Users.Count==beforeInvalidEmail,"İsteğe bağlı e-posta büyük-küçük harf farkıyla başka hesapta tekrar kullanılamaz");
            var phoneFreeReloaded=new FitService(new DataStore(path));check(phoneFreeReloaded.Login("nophonelocalfixture","NoPhoneOwn-84!")&&phoneFreeReloaded.RequireCurrent().Phone=="","Telefonsuz hesap yeniden açıldıktan sonra kullanıcı adı ve şifresiyle giriş yapar");
        }
        finally{AppLanguage.Use("tr");File.Delete(path);File.Delete(path+".bak");File.Delete(path+".tmp");}
    }
}
