using FitTeam.Core;
namespace FitTeam.Checks;
internal static class PortalCompletionChecks
{
 public static async Task Run(Action<bool,string> check)
 {
  var file=Path.Combine(Path.GetTempPath(),"FitTeam_completion_"+Guid.NewGuid()+".json");try{
   var s=new FitService(new DataStore(file));s.Login("sporcu","Fit123!");var me=s.RequireCurrent();var today=DateOnly.FromDateTime(DateTime.Today);
   void Reject(Action action,string label){bool rejected=false;try{action();}catch(ArgumentException){rejected=true;}catch(InvalidOperationException){rejected=true;}check(rejected,label);}
   var coach=s.Data.Users.First(u=>u.Username=="koc");var other=s.Data.Users.First(u=>u.Username=="koc2");var initial=me.Profile.InitialWeight;var days=me.Profile.AvailableDays.ToList();
   var birthday=today.AddYears(-24);s.SaveMemberBasics("Deniz يوسف",birthday,"Erkek",180,80,75,Choices.Goals[0],"light","İstanbul","+905321234567","Diz notu · الركبة");
   check(me.Profile.Age==24&&me.Profile.BirthDate==birthday&&me.Profile.ActivityLevel=="light"&&me.Profile.HealthNotes.Contains("الركبة"),"Tam profil doğum tarihinden yaşı hesaplar; hareket düzeyi ve Unicode not korunur");
   check(me.Profile.InitialWeight==initial&&me.Profile.AvailableDays.SequenceEqual(days)&&me.Phone=="+905321234567"&&!me.PhoneVerified,"Basit profil güncellemesi başlangıç kilosunu ve antrenman günlerini korur; telefon doğrulaması uydurmaz");
   Reject(()=>s.SaveMemberBasics("Yanlış",today,"Erkek",180,80,75,Choices.Goals[0],"light","","",""),"Geçersiz doğum tarihli profil reddedilir");check(me.Profile.Name=="Deniz يوسف"&&me.Profile.BirthDate==birthday,"Reddedilen profil mevcut kişisel bilgileri değiştirmez");
   Reject(()=>s.SaveMemberBasics("Deniz",birthday,"Erkek",180,80,75,Choices.Goals[0],"light","İstanbul","123",""),"Geçersiz telefon kayıt değişikliğinden önce reddedilir");
   s.UpdateProfile(me.Profile);check(me.Profile.BirthDate==birthday,"Gelişmiş profil güncellemesi doğum tarihini kaybetmez");
   Reject(()=>s.SaveGym(null,"Salon","İstanbul","","",""),"Üye salon oluşturamaz");
   s.Login("koc","Fit123!");var gym=s.SaveGym(null,"Yeni Fitness Salonu","İzmir","Örnek adres","09:00–21:00","Fitness ve grup dersleri");check(s.GymDirectory().Any(g=>g.Id==gym.Id)&&!s.Data.Teams.Any(t=>t.Id==gym.Id),"Spor salonları futbol kulüplerinden bağımsız kaydedilir");
   s.Login("koc2","Fit123!");Reject(()=>s.SaveGym(gym.Id,"Ele geçir","İzmir","","",""),"Antrenör başkasının salonunu düzenleyemez");Reject(()=>s.GymCode(gym.Id),"Başka salonun kodu yetkisiz görüntülenemez");
   s.Login("sporcu","Fit123!");s.RequestGym(gym.Id);s.Login("koc","Fit123!");s.RejectGym(me.Id,gym.Id);check(s.Data.GymMemberships.Single(m=>m.GymId==gym.Id).Rejected,"Reddedilen salon başvurusu geçmişi korunur");
   s.Login("sporcu","Fit123!");s.RequestGym(gym.Id);check(s.Data.GymMemberships.Count(m=>m.GymId==gym.Id)==1&&!s.Data.GymMemberships.Single(m=>m.GymId==gym.Id).Rejected,"Salon başvurusu tekrar yapılabilir ve yinelenmez");
   s.Login("koc","Fit123!");s.ApproveGym(me.Id,gym.Id);var code=s.GymCode(gym.Id);s.Login("sporcu","Fit123!");s.CheckIn(code);check(s.Visits(me).First().GymId==gym.Id,"Bağımsız salon üyeliği onayından sonra giriş yapılır");
   var r=new FitService(new DataStore(file));r.Login("sporcu","Fit123!");check(r.RequireCurrent().Profile.BirthDate==birthday&&r.GymDirectory().Any(g=>g.Name=="Yeni Fitness Salonu")&&r.RequireCurrent().Profile.HealthNotes.Contains("الركبة"),"Doğum tarihi, sağlık notu ve bağımsız salon yeniden açılınca korunur");
   var auth=new CodeAuthClient(new Uri("http://127.0.0.1:12345/"),new HttpClient(new global::CodeFixture("jawharyusuf446@gmail.com",true)));s.LoginCode(await auth.PresidentLogin("fixture","fixture-only"));var owner=s.RequireCurrent();s.AssignMeal(owner,"Kahvaltı","Kendi öğünüm","");check(s.MealPlan(owner).Count==1,"Başkan kendi kişisel beslenme listesini hazırlayabilir");s.SendMessage(me.Id,"Yetkili mesajı");s.Login("sporcu","Fit123!");check(s.MessageContacts().Any(u=>u.Id==owner.Id),"Üye başkanın mesajını açabilir ve yanıtlayabilir");s.SendMessage(owner.Id,"Yanıt");s.LoginCode(await auth.PresidentLogin("fixture","fixture-only"));check(s.Conversation(me.Id).Count==2,"Başkan ve üye konuşması iki yönde çalışır");owner.Profile.Name="Yussef يوسف";s.Save();s.LoginCode(await auth.PresidentLogin("fixture","fixture-only"));check(s.RequireCurrent().Profile.Name=="Yussef يوسف","Başkan yeniden giriş yaptığında düzenlediği adı korunur");
   var legacyTeam=s.Data.Teams.First();var data=new AppData{Teams=[legacyTeam],GymVisits=[new(){UserId=me.Id,GymId=legacyTeam.Id}]};GymMigration.Apply(data);GymMigration.Apply(data);check(data.Gyms.Count==1&&data.Gyms[0].Id==legacyTeam.Id&&data.Teams.Count==1,"Önceki salon ziyaretleri tekil olarak aktarılır; kulüp kaydı korunur");
  }finally{if(File.Exists(file))File.Delete(file);if(File.Exists(file+".bak"))File.Delete(file+".bak");}
 }
}
