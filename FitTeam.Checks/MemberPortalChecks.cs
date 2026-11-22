using FitTeam.Core;
namespace FitTeam.Checks;
internal static class MemberPortalChecks
{
 public static void Run(Action<bool,string> check)
 {
  var file=Path.Combine(Path.GetTempPath(),"FitTeam_portal_"+Guid.NewGuid()+".json");
  try{
   var store=new DataStore(file);var s=new FitService(store);s.Login("sporcu","Fit123!");var me=s.RequireCurrent();var coach=s.Data.Users.First(u=>u.Username=="koc");var stranger=s.Data.Users.First(u=>u.Username=="oyuncu");var today=DateOnly.FromDateTime(DateTime.Today);
   void Reject(Action action,string label){bool failed=false;try{action();}catch(ArgumentException){failed=true;}catch(InvalidOperationException){failed=true;}check(failed,label);}
   var water=s.WaterToday(me);s.AddDiary(today,"Su",0.5m,"ماء","Bugünkü not");check(s.WaterToday(me)==water+0.5m&&s.Diary(me).Single().Title=="ماء","Günlük su kaydı su toplamına ve Arapça başlığa yansır");
   Reject(()=>s.AddDiary(today.AddDays(1),"Uyku",8,"",""),"Gelecek güne günlük kaydı eklenemez");Reject(()=>s.AddDiary(today,"Uyku",25,"",""),"Uyku süresi 24 saati aşamaz");Reject(()=>s.AddDiary(today,"Not",0,"",""),"Boş günlük notu reddedilir");
   s.SaveMeasurement(new(){Date=today,Weight=80,FatPercent=20,Waist=90,Note="قياس"});s.SaveMeasurement(new(){Date=today,Weight=79,Waist=89});check(s.Measurements(me).Count==1&&me.Profile.Weight==79&&s.Measurements(me)[0].FatPercent==null,"Aynı gün ölçümü tek kayıt olarak güncellenir ve kilo profiliyle eşitlenir");
   Reject(()=>s.SaveMeasurement(new(){Date=today}),"Boş ölçüm reddedilir");Reject(()=>s.SaveMeasurement(new(){Date=today,FatPercent=90}),"Geçersiz yağ oranı reddedilir");
   s.SetHealthNotes("Sağ diz · الركبة");s.UpdateProfile(me.Profile);check(me.Profile.HealthNotes.Contains("الركبة"),"Sağlık notunda Türkçe ve Arapça korunur");
   var now=DateTimeOffset.Now;s.SaveReminders([new(){Kind="Su",Time=TimeOnly.FromDateTime(now.DateTime.AddMinutes(-1)),Enabled=true}]);check(s.TickReminders(now).Count==1&&s.TickReminders(now.AddMinutes(1)).Count==0,"Hatırlatma aynı gün yalnız bir kez üretilir");
   var saved=new FitService(new DataStore(file));saved.Login("sporcu","Fit123!");check(saved.TickReminders(now.AddMinutes(2)).Count==0,"Uygulama yeniden açılınca hatırlatma yinelenmez");
   s.SaveReminders([new(){Kind="Antrenman",Time=TimeOnly.FromDateTime(now.DateTime.AddMinutes(-20)),Enabled=true}]);check(s.TickReminders(now).Count==0,"15 dakikadan eski hatırlatma atlanır");
   s.SendMessage(coach.Id,"Merhaba hocam · مرحباً");check(s.Conversation(coach.Id).Count==1,"Üye bağlı koçuna Unicode mesaj gönderebilir");Reject(()=>s.SendMessage(stranger.Id,"Test"),"Üye ilişkisiz kişiye mesaj gönderemez");
   s.Login("koc","Fit123!");check(s.Conversation(me.Id).Single().Body.Contains("مرحباً")&&s.Diary(me).Count==1,"Koç bağlı üyenin mesajını ve günlüğünü görür");s.MarkConversationRead(me.Id);s.SendMessage(me.Id,"Programını güncelledim.");s.AssignMeal(me,"Kahvaltı","Yulaf ve yoğurt","Porsiyonu birlikte ayarlayalım.");
   s.Login("sporcu","Fit123!");check(s.Conversation(coach.Id).Count==2&&s.MealPlan(me).Single().Contents.Contains("Yulaf"),"Koç yanıtı ve hazırladığı beslenme listesi üyeye görünür");
   Reject(()=>s.Diary(stranger),"Üye başka üyenin günlüğünü okuyamaz");Reject(()=>s.Measurements(stranger),"Üye başka üyenin ölçümlerini okuyamaz");
   var gym=s.Data.Gyms.First();s.RequestGym(gym.Id);Reject(()=>s.CheckIn("ABC123"),"Geçersiz salon kodu reddedilir");s.Login("koc","Fit123!");var code=s.GymCode(gym.Id);s.ApproveGym(me.Id,gym.Id);s.Login("sporcu","Fit123!");
   var visit=s.CheckIn("FITTEAM:"+code,now);check(visit.LeftAt==null,"Onaylanmış üye QR koduyla giriş kaydı oluşturur");Reject(()=>s.CheckIn(code,now.AddSeconds(10)),"Yinelenen kısa aralıklı QR okutma reddedilir");s.CheckIn(code,now.AddMinutes(1));check(s.Visits(me).Count==1&&s.Visits(me)[0].LeftAt!=null,"İkinci QR okutma aynı ziyaretin çıkışını kaydeder");
   var n=s.Notifications().First();s.MarkNotification(n.Id,true);check(!s.Notifications().Any(x=>x.Id==n.Id)&&s.Data.Notifications.Any(x=>x.Id==n.Id),"Bildirim listeden kaldırılırken veri korunur");
   var reload=new FitService(new DataStore(file));reload.Login("sporcu","Fit123!");check(reload.Measurements(reload.RequireCurrent()).Count==1&&reload.Visits(reload.RequireCurrent()).Count==1&&reload.Conversation(coach.Id).Count==2,"Ölçüm, mesaj ve salon ziyaretleri yeniden açılınca korunur");
   var preview=DataStore.Preview();new FitService(preview).Save();check(preview.FilePath=="","Önizleme hiçbir gerçek hesap dosyasına yazmaz");
   foreach(var lang in new[]{"en","ar"}){AppLanguage.Use(lang);check(AppLanguage.Text("Günlüğüm")!="Günlüğüm"&&AppLanguage.Text("Ölçümlerim")!="Ölçümlerim"&&AppLanguage.Text("Hatırlatmalar")!="Hatırlatmalar",lang+" yeni üye paneli çevirileri hazır");}AppLanguage.Use("tr");
  }finally{if(File.Exists(file))File.Delete(file);if(File.Exists(file+".bak"))File.Delete(file+".bak");}
 }
}
