using System.Security.Cryptography;
namespace FitTeam.Core;
public class DiaryEntry
{
    public Guid Id {get;set;}=Guid.NewGuid(); public Guid UserId {get;set;}
    public DateOnly Date {get;set;} public string Kind {get;set;}=""; public decimal Value {get;set;}
    public string Title {get;set;}=""; public string Note {get;set;}="";
}
public class BodyMeasurement
{
    public Guid UserId {get;set;} public DateOnly Date {get;set;}
    public decimal? Weight {get;set;} public decimal? FatPercent {get;set;} public decimal? MuscleKg {get;set;}
    public decimal? Chest {get;set;} public decimal? Waist {get;set;} public decimal? Hip {get;set;}
    public decimal? Arm {get;set;} public decimal? Leg {get;set;} public string Note {get;set;}="";
}
public class MemberReminder
{
    public Guid Id {get;set;}=Guid.NewGuid();public Guid UserId {get;set;} public string Kind {get;set;}="";
    public TimeOnly Time {get;set;} public bool Enabled {get;set;} public DateOnly? LastSent {get;set;}
}
public class PanelNotification
{
    public Guid Id {get;set;}=Guid.NewGuid();public Guid UserId {get;set;} public string Title {get;set;}="";
    public string Body {get;set;}="";public string Page {get;set;}="Bildirimler";
    public DateTimeOffset CreatedAt {get;set;}=DateTimeOffset.UtcNow;public DateTimeOffset? ReadAt {get;set;}
    public bool Hidden {get;set;}
}
public class MemberMessage
{
    public Guid Id {get;set;}=Guid.NewGuid();public Guid FromId {get;set;}public Guid ToId {get;set;}
    public string Body {get;set;}="";public DateTimeOffset SentAt {get;set;}=DateTimeOffset.UtcNow;public bool Read {get;set;}
}
public class MealAssignment
{
    public Guid Id {get;set;}=Guid.NewGuid();public Guid UserId {get;set;}public Guid CoachId {get;set;}
    public string Meal {get;set;}="";public string Contents {get;set;}="";public string Note {get;set;}="";
}
public class GymMembership
{
    public Guid UserId {get;set;}public Guid GymId {get;set;}public bool Approved {get;set;}public bool Rejected {get;set;}
    public DateTimeOffset RequestedAt {get;set;}=DateTimeOffset.UtcNow;
}
public class GymVisit
{
    public Guid Id {get;set;}=Guid.NewGuid();public Guid UserId {get;set;}public Guid GymId {get;set;}
    public DateTimeOffset EnteredAt {get;set;}public DateTimeOffset? LeftAt {get;set;}
}
public sealed record FoodGuideItem(string Name,string Category,int Calories,decimal Protein,decimal Carbs,decimal Fat);
public static class FoodGuide
{
    // Genel, yaklaşık 100 g değerleri; marka/porsiyon ambalajı esas alınır.
    public static readonly FoodGuideItem[] All=[
        new("Tavuk göğsü","Et ve balık",165,31,0,3.6m),new("Hindi göğsü","Et ve balık",135,29,0,1.6m),
        new("Somon","Et ve balık",208,20,0,13),new("Ton balığı","Et ve balık",116,26,0,1),new("Dana eti","Et ve balık",250,26,0,15),
        new("Yumurta","Süt ve yumurta",155,13,1.1m,11),new("Yoğurt","Süt ve yumurta",61,3.5m,4.7m,3.3m),
        new("Süt","Süt ve yumurta",61,3.2m,4.8m,3.3m),new("Lor peyniri","Süt ve yumurta",90,17,3,1),
        new("Beyaz peynir","Süt ve yumurta",264,14,4,21),new("Kefir","Süt ve yumurta",60,3,5,3),
        new("Yulaf","Tahıllar",389,17,66,7),new("Bulgur (pişmiş)","Tahıllar",83,3.1m,18.6m,0.2m),
        new("Pirinç (pişmiş)","Tahıllar",130,2.7m,28,0.3m),new("Tam buğday ekmeği","Tahıllar",247,13,41,4.2m),
        new("Makarna (pişmiş)","Tahıllar",158,5.8m,31,0.9m),new("Kinoa (pişmiş)","Tahıllar",120,4.4m,21,1.9m),
        new("Mercimek (pişmiş)","Baklagiller",116,9,20,0.4m),new("Nohut (pişmiş)","Baklagiller",164,8.9m,27,2.6m),
        new("Fasulye (pişmiş)","Baklagiller",127,8.7m,23,0.5m),new("Muz","Meyveler",89,1.1m,23,0.3m),
        new("Elma","Meyveler",52,0.3m,14,0.2m),new("Portakal","Meyveler",47,0.9m,12,0.1m),
        new("Çilek","Meyveler",32,0.7m,8,0.3m),new("Avokado","Meyveler",160,2,9,15),new("Hurma","Meyveler",277,1.8m,75,0.2m),
        new("Brokoli","Sebzeler",34,2.8m,7,0.4m),new("Ispanak","Sebzeler",23,2.9m,3.6m,0.4m),
        new("Patates (haşlanmış)","Sebzeler",87,1.9m,20,0.1m),new("Domates","Sebzeler",18,0.9m,3.9m,0.2m),
        new("Salatalık","Sebzeler",15,0.7m,3.6m,0.1m),new("Havuç","Sebzeler",41,0.9m,10,0.2m),
        new("Badem","Kuruyemiş ve yağ",579,21,22,50),new("Ceviz","Kuruyemiş ve yağ",654,15,14,65),
        new("Fındık","Kuruyemiş ve yağ",628,15,17,61),new("Zeytinyağı","Kuruyemiş ve yağ",884,0,0,100)];
}
public sealed partial class FitService
{
    public static readonly string[] DiaryKinds=["Antrenman","Su","Uyku","Adım","Öğün","Not"];
    public List<DiaryEntry> Diary(User target){EnsureAccess(target);return Data.Diary.Where(d=>d.UserId==target.Id).OrderByDescending(d=>d.Date).ToList();}
    public void AddDiary(DateOnly date,string kind,decimal value,string title,string note)
    {
        var me=RequireCurrent();var today=DateOnly.FromDateTime(DateTime.Today);
        decimal max=kind switch{"Su"=>15,"Uyku"=>24,"Adım"=>100000,"Antrenman"=>1440,"Öğün"=>10000,_=>0};
        if(date>today||!DiaryKinds.Contains(kind)||value<0||value>max||kind!="Not"&&value==0||title.Length>120||note.Length>2000||kind=="Not"&&string.IsNullOrWhiteSpace(title+note))throw new ArgumentException("Günlük türünü, tarihini ve değerini kontrol et.");
        if(kind=="Su"){
            var water=Data.Water.FirstOrDefault(w=>w.UserId==me.Id&&w.Date==date);if((water?.Litres??0)+value>15)throw new ArgumentException("Günlük su toplamı 15 litreyi aşamaz.");
            if(water==null)Data.Water.Add(new(){UserId=me.Id,Date=date,Litres=value});else water.Litres+=value;
        }
        Data.Diary.Add(new(){UserId=me.Id,Date=date,Kind=kind,Value=value,Title=title.Trim(),Note=note.Trim()});Save();
    }
    public List<BodyMeasurement> Measurements(User target){EnsureAccess(target);return Data.Measurements.Where(m=>m.UserId==target.Id).OrderByDescending(m=>m.Date).ToList();}
    public void SaveMeasurement(BodyMeasurement entry)
    {
        var me=RequireCurrent();
        if(entry.Date>DateOnly.FromDateTime(DateTime.Today)||entry.Note.Length>2000||new[]{entry.Weight,entry.FatPercent,entry.MuscleKg,entry.Chest,entry.Waist,entry.Hip,entry.Arm,entry.Leg}.All(v=>v==null))throw new ArgumentException("En az bir ölçüm gir ve tarihini kontrol et.");
        if(entry.Weight is <30 or >300||entry.FatPercent is <=0 or >75||entry.MuscleKg is <=0 or >200||new[]{entry.Chest,entry.Waist,entry.Hip,entry.Arm,entry.Leg}.Any(v=>v is <=0 or >300))throw new ArgumentException("Ölçüm değerlerini kontrol et.");
        entry.UserId=me.Id;Data.Measurements.RemoveAll(m=>m.UserId==me.Id&&m.Date==entry.Date);Data.Measurements.Add(entry);
        if(entry.Weight.HasValue)SetWeight(me,entry.Date,entry.Weight.Value);else Save();
    }
    public void SetHealthNotes(string notes){var me=RequireCurrent();if(notes.Length>3000)throw new ArgumentException("Sağlık notu en fazla 3000 karakter olabilir.");me.Profile.HealthNotes=notes.Trim();Save();}
    public List<PanelNotification> Notifications()=>Data.Notifications.Where(n=>n.UserId==RequireCurrent().Id&&!n.Hidden&&(n.ReadAt==null||n.ReadAt>DateTimeOffset.UtcNow.AddDays(-7))).OrderByDescending(n=>n.CreatedAt).ToList();
    public void MarkNotification(Guid? id,bool hide=false){var me=RequireCurrent();foreach(var n in Data.Notifications.Where(n=>n.UserId==me.Id&&(id==null||n.Id==id))){n.ReadAt=DateTimeOffset.UtcNow;if(hide)n.Hidden=true;}Save();}
    private void Notify(Guid userId,string title,string body,string page,DateTimeOffset? when=null)=>Data.Notifications.Add(new(){UserId=userId,Title=title,Body=body,Page=page,CreatedAt=when??DateTimeOffset.UtcNow});
    public void TestNotification(){Notify(RequireCurrent().Id,"Deneme bildirimi","Bildirimlerin bu uygulamada çalışıyor.","Hatırlatmalar");Save();}
    public List<MemberReminder> Reminders()=>Data.Reminders.Where(r=>r.UserId==RequireCurrent().Id).ToList();
    public void SaveReminders(IEnumerable<MemberReminder> reminders)
    {
        var me=RequireCurrent();var items=reminders.ToList();
        if(items.Count>4||items.Any(r=>r.Kind is not ("Su" or "Antrenman")))throw new ArgumentException("Hatırlatma ayarlarını kontrol et.");
        var old=Reminders();foreach(var r in items){r.UserId=me.Id;r.LastSent=old.FirstOrDefault(x=>x.Kind==r.Kind&&x.Time==r.Time)?.LastSent;}
        Data.Reminders.RemoveAll(r=>r.UserId==me.Id);Data.Reminders.AddRange(items);Save();
    }
    public List<PanelNotification> TickReminders(DateTimeOffset localNow)
    {
        var me=RequireCurrent();int before=Data.Notifications.Count;var date=DateOnly.FromDateTime(localNow.Date);var due=Reminders().Where(r=>r.Enabled&&r.LastSent!=date&&localNow.DateTime>=date.ToDateTime(r.Time)&&localNow.DateTime<=date.ToDateTime(r.Time).AddMinutes(15)).ToList();
        foreach(var r in due){Notify(me.Id,r.Kind=="Su"?"Su içme vakti":"Antrenman vakti",r.Kind=="Su"?"Su hedefini ve bugünkü kaydını kontrol et.":"Bugünkü programını açıp antrenmanına başlayabilirsin.",r.Kind=="Su"?"Günlüğüm":"Programlarım",localNow);r.LastSent=date;}
        if(due.Count>0)Save();return Data.Notifications.Skip(before).Where(n=>n.UserId==me.Id).ToList();
    }
    public List<User> MessageContacts()
    {
        var me=RequireCurrent();var ids=CoachLinks(me).Concat(ManagedUsers().Select(u=>u.Id)).ToHashSet();
        if(me.TeamId is Guid id){var coach=Data.Teams.FirstOrDefault(t=>t.Id==id&&t.IsActive)?.CoachId;if(coach.HasValue)ids.Add(coach.Value);}
        return Data.Users.Where(u=>u.Id!=me.Id&&u.IsActive&&!u.NeedsApproval&&(HasAdministrationAccess||ids.Contains(u.Id)||u.Role==Choices.DeputyRole||u.Email.Equals("jawharyusuf446@gmail.com",StringComparison.OrdinalIgnoreCase))).ToList();
    }
    public List<MemberMessage> Conversation(Guid contactId)
    {
        var me=RequireCurrent();if(!MessageContacts().Any(u=>u.Id==contactId))throw new InvalidOperationException("Bu kişiyle mesajlaşma yetkin yok.");
        return Data.Messages.Where(m=>m.FromId==me.Id&&m.ToId==contactId||m.ToId==me.Id&&m.FromId==contactId).OrderBy(m=>m.SentAt).ToList();
    }
    public void SendMessage(Guid to,string body)
    {
        var me=RequireCurrent();if(!MessageContacts().Any(u=>u.Id==to))throw new InvalidOperationException("Yalnız bağlı olduğun kişilerle mesajlaşabilirsin.");
        if(string.IsNullOrWhiteSpace(body)||body.Length>2000)throw new ArgumentException("Mesaj 1–2000 karakter olmalı.");
        Data.Messages.Add(new(){FromId=me.Id,ToId=to,Body=body.Trim()});Notify(to,"Yeni mesaj",me.Profile.Name,"Mesajlar");Save();
    }
    public void MarkConversationRead(Guid contactId){var _=Conversation(contactId);foreach(var m in Data.Messages.Where(m=>m.ToId==RequireCurrent().Id&&m.FromId==contactId))m.Read=true;Save();}
    public List<MealAssignment> MealPlan(User target){EnsureAccess(target);return Data.MealAssignments.Where(m=>m.UserId==target.Id).ToList();}
    public void AssignMeal(User target,string meal,string contents,string note)
    {
        EnsureAccess(target);if(target.Id==RequireCurrent().Id&&!HasAdministrationAccess||!Nutrition.Meals.Contains(meal)||string.IsNullOrWhiteSpace(contents)||contents.Length>1500||note.Length>1000)throw new ArgumentException("Üye, öğün ve beslenme açıklamasını kontrol et.");
        Data.MealAssignments.RemoveAll(m=>m.UserId==target.Id&&m.Meal==meal);Data.MealAssignments.Add(new(){UserId=target.Id,CoachId=RequireCurrent().Id,Meal=meal,Contents=contents.Trim(),Note=note.Trim()});Notify(target.Id,"Beslenme listen güncellendi",meal,"Beslenme Listem");Save();
    }
    public List<Gym> GymDirectory()=>Data.Gyms.Where(t=>t.IsActive).ToList();
    public void RequestGym(Guid gymId)
    {
        var me=RequireCurrent();if(!GymDirectory().Any(t=>t.Id==gymId))throw new ArgumentException("Aktif bir salon seç.");
        var membership=Data.GymMemberships.FirstOrDefault(m=>m.UserId==me.Id&&m.GymId==gymId);if(membership==null)Data.GymMemberships.Add(new(){UserId=me.Id,GymId=gymId});else if(membership.Rejected){membership.Rejected=false;membership.RequestedAt=DateTimeOffset.UtcNow;}Save();
    }
    public void ApproveGym(Guid userId,Guid gymId)
    {
        var me=RequireCurrent();var gym=GymDirectory().FirstOrDefault(t=>t.Id==gymId)??throw new ArgumentException("Salon bulunamadı.");
        if(!HasAdministrationAccess&&gym.ManagerId!=me.Id)throw new InvalidOperationException("Salon üyeliğini yalnız salon yetkilisi onaylar.");
        var membership=Data.GymMemberships.FirstOrDefault(m=>m.GymId==gymId&&m.UserId==userId)??throw new ArgumentException("Üyelik isteği bulunamadı.");membership.Approved=true;membership.Rejected=false;Notify(userId,"Salon üyeliğin onaylandı",gym.Name,"Spor Salonum");Save();
    }
    public string GymCode(Guid gymId)
    {
        var me=RequireCurrent();var gym=GymDirectory().FirstOrDefault(t=>t.Id==gymId)??throw new ArgumentException("Salon bulunamadı.");
        if(!HasAdministrationAccess&&gym.ManagerId!=me.Id)throw new InvalidOperationException("Salon kodunu yalnız salon yetkilisi görüntüler.");
        if(gym.CheckInCode.Length==0){const string alphabet="ABCDEFGHJKLMNPQRSTUVWXYZ23456789";gym.CheckInCode=new string(Enumerable.Range(0,6).Select(_=>alphabet[RandomNumberGenerator.GetInt32(alphabet.Length)]).ToArray());Save();}return gym.CheckInCode;
    }
    public GymVisit CheckIn(string code,DateTimeOffset? time=null)
    {
        var me=RequireCurrent();code=code.Trim().ToUpperInvariant().Replace("FITTEAM:","");
        var gym=GymDirectory().FirstOrDefault(t=>t.CheckInCode.Length==6&&t.CheckInCode==code)??throw new ArgumentException("Salon kodu geçersiz.");
        if(!Data.GymMemberships.Any(m=>m.UserId==me.Id&&m.GymId==gym.Id&&m.Approved))throw new InvalidOperationException("Bu salonda onaylanmış üyeliğin bulunmuyor.");
        var now=time??DateTimeOffset.Now;var open=Data.GymVisits.LastOrDefault(v=>v.UserId==me.Id&&v.GymId==gym.Id&&v.LeftAt==null&&v.EnteredAt.Date==now.Date);
        var last=Data.GymVisits.LastOrDefault(v=>v.UserId==me.Id&&v.GymId==gym.Id);if(last!=null&&now-(last.LeftAt??last.EnteredAt)<TimeSpan.FromSeconds(30))throw new ArgumentException("Tekrar okutmak için 30 saniye bekle.");
        if(open==null){open=new(){UserId=me.Id,GymId=gym.Id,EnteredAt=now};Data.GymVisits.Add(open);}else open.LeftAt=now;Save();return open;
    }
    public List<GymVisit> Visits(User user){EnsureAccess(user);return Data.GymVisits.Where(v=>v.UserId==user.Id).OrderByDescending(v=>v.EnteredAt).ToList();}
}
