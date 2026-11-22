using FitTeam.Core;
using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
static class LanguageChecks
{
 public static void Run(Action<bool,string> check)
 {
  var roles=Choices.Roles.ToArray();var path=Path.Combine(Path.GetTempPath(),"fitteam-language-"+Guid.NewGuid()+".json");
  try
  {
   foreach(var code in new[]{"en","ar"})
   {
    using var stream=typeof(AppLanguage).Assembly.GetManifestResourceStream("FitTeam.Core.Localization."+code+".json")!;
    var dictionary=JsonSerializer.Deserialize<Dictionary<string,string>>(stream)!;
    check(dictionary.Count>=1000&&dictionary.Values.All(v=>!string.IsNullOrWhiteSpace(v)),code+" dilinde bütün metin kaynakları dolu");
    check(dictionary.All(e=>Regex.Matches(e.Key,@"\{\d+\}").Select(m=>m.Value).Distinct().Order().SequenceEqual(Regex.Matches(e.Value,@"\{\d+\}").Select(m=>m.Value).Distinct().Order())),code+" çevirilerinde sayı ve kullanıcı verisi yer tutucuları korunur");
   }
   AppLanguage.Use("en");check(AppLanguage.Text("Yeni üye kaydı")=="New member registration","İngilizce kayıt başlığı seçilen dille gösterilir");
   check(AppLanguage.Text("Merhaba محمد علي, bugün de bir adım ileri.")=="Hello محمد علي, one more step forward today.","İngilizce ekranda Arapça kullanıcı adı aynen korunur");
   AppLanguage.Use("ar");check(AppLanguage.RightToLeft&&AppLanguage.Text("Yeni üye kaydı")=="تسجيل عضو جديد","Arapça ekran dili ve sağdan sola yön seçilir");
   check(CultureInfo.CurrentCulture.DateTimeFormat.Calendar is GregorianCalendar,"Arapça arayüzde mevcut antrenman tarihleri Miladi takvimde kalır");
   check(AppLanguage.Text("Merhaba Çağrı Smith, bugün de bir adım ileri.")=="مرحباً Çağrı Smith، خطوة أخرى إلى الأمام اليوم.","Arapça ekranda Türkçe ve İngilizce kullanıcı adı aynen korunur");
   var data=new DataStore(path);var service=new FitService(data);
   var coach=data.Data.Users.First(u=>u.Username=="koc");
   var member=service.Register("عضو_عربي","Language-84!","محمد علي",Choices.Roles[0],coach.Id);
   check(service.Login("عضو_عربي","Language-84!")&&member.Profile.Name=="محمد علي","Arapça kullanıcı adı ve ad soyad ile yerel hesap çalışır");
   AppLanguage.Use("en");check(service.Current!.Username=="عضو_عربي"&&service.Current.Profile.Name=="محمد علي"&&Choices.Roles.SequenceEqual(roles),"Dil değişimi kullanıcı verilerini ve rol yetkilerini değiştirmez");
   var reopened=new DataStore(path);check(reopened.Data.Users.Any(u=>u.Username=="عضو_عربي"&&u.Profile.Name=="محمد علي"),"Arapça isimler yeniden açılışta Unicode olarak korunur");
   AppLanguage.Use("tr");check(!AppLanguage.RightToLeft&&AppLanguage.Text("Yeni üye kaydı")=="Yeni üye kaydı","Türkçe seçilince asıl dil ve soldan sağa düzen geri gelir");
  }
  finally{AppLanguage.Use("tr");File.Delete(path);File.Delete(path+".bak");File.Delete(path+".tmp");}
 }
}
