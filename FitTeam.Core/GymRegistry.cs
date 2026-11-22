using System.Text.Json;
namespace FitTeam.Core;
public sealed class Gym
{
    public Guid Id {get;set;}=Guid.NewGuid();public Guid ManagerId {get;set;}
    public bool IsActive {get;set;}=true;public string Name {get;set;}="";public string City {get;set;}="";
    public string Address {get;set;}="";public string Hours {get;set;}="";public string Description {get;set;}="";
    public string CheckInCode {get;set;}="";
}
public static class GymMigration
{
    public static void Apply(AppData data)
    {
        var used=data.GymMemberships.Select(m=>m.GymId).Concat(data.GymVisits.Select(v=>v.GymId)).ToHashSet();
        foreach(var team in data.Teams.Where(t=>used.Contains(t.Id)&&!data.Gyms.Any(g=>g.Id==t.Id)))
            data.Gyms.Add(new(){Id=team.Id,ManagerId=team.CoachId,Name=team.Name,City=team.City,CheckInCode=team.CheckInCode,IsActive=team.IsActive});
    }
}
public sealed partial class FitService
{
    public Gym SaveGym(Guid? id,string name,string city,string address,string hours,string description,Guid? managerId=null)
    {
        var me=RequireCurrent();if(!HasAdministrationAccess&&me.Role!=Choices.Roles[2]&&me.Role!=Choices.Roles[3])throw new InvalidOperationException("Salon oluşturmak için antrenör veya yönetici hesabı gerekir.");
        var manager=HasAdministrationAccess?managerId??me.Id:me.Id;
        if(!Data.Users.Any(u=>u.Id==manager&&u.IsActive))throw new ArgumentException("Aktif bir salon yetkilisi seç.");
        if(name.Trim().Length is <2 or >100||city.Trim().Length is <2 or >80||address.Trim().Length>500||hours.Length>150||description.Length>1500)throw new ArgumentException("Salon adı, şehir ve açıklamalarını kontrol et.");
        var gym=id==null?new Gym():Data.Gyms.FirstOrDefault(g=>g.Id==id)??throw new ArgumentException("Salon bulunamadı.");
        if(id!=null&&!HasAdministrationAccess&&gym.ManagerId!=me.Id)throw new InvalidOperationException("Başka bir yetkilinin salonunu değiştiremezsin.");
        gym.Name=name.Trim();gym.City=city.Trim();gym.Address=address.Trim();gym.Hours=hours.Trim();gym.Description=description.Trim();gym.ManagerId=manager;
        if(id==null)Data.Gyms.Add(gym);Save();return gym;
    }
    public void RejectGym(Guid userId,Guid gymId)
    {
        var me=RequireCurrent();var gym=GymDirectory().FirstOrDefault(g=>g.Id==gymId)??throw new ArgumentException("Salon bulunamadı.");
        if(!HasAdministrationAccess&&gym.ManagerId!=me.Id)throw new InvalidOperationException("Bu salonun üyelik isteğini yönetemezsin.");
        var member=Data.GymMemberships.FirstOrDefault(m=>m.UserId==userId&&m.GymId==gymId&&!m.Approved)??throw new ArgumentException("Bekleyen başvuru bulunamadı.");
        member.Rejected=true;Notify(userId,"Salon başvurun sonuçlandı","Başvurun onaylanmadı. Başka bir salon seçebilirsin.","Spor Salonum");Save();
    }
    public void SaveMemberBasics(string name,DateOnly? birth,string gender,decimal height,decimal weight,decimal target,string goal,string activity,string city,string phone,string health)
    {
        var me=RequireCurrent();var today=DateOnly.FromDateTime(DateTime.Today);int age=me.Profile.Age;
        if(birth.HasValue){age=today.Year-birth.Value.Year;if(birth.Value.AddYears(age)>today)age--;}
        phone=LoginContact.NormalizeDigits(phone.Trim());phone=System.Text.RegularExpressions.Regex.Replace(phone,@"[\s()-]","");
        if(phone.Length>0&&!System.Text.RegularExpressions.Regex.IsMatch(phone,@"^\+[1-9][0-9]{8,14}$"))throw new ArgumentException("Telefonu ülke koduyla yaz: +90…");
        if(phone.Length>0&&Data.Users.Any(u=>u.Id!=me.Id&&u.IsActive&&u.Phone==phone))throw new ArgumentException("Bu telefon başka bir hesapta kayıtlı.");
        if(gender is not ("Kadın" or "Erkek" or "Belirtmek istemiyorum")||city.Length>100||health.Length>3000||activity.Length>0&&!Nutrition.Activities.Any(a=>a.Id==activity))throw new ArgumentException("Profil alanlarını kontrol et.");
        var p=JsonSerializer.Deserialize<Profile>(JsonSerializer.Serialize(me.Profile))!;p.Name=name.Trim();p.Age=age;p.Gender=gender;p.Height=height;p.Weight=weight;p.TargetWeight=target;p.Goal=goal;p.City=city.Trim();
        UpdateProfile(p);me.Profile.BirthDate=birth;me.Profile.ActivityLevel=activity;me.Profile.HealthNotes=health.Trim();
        if(me.Phone!=phone)me.PhoneVerified=false;me.Phone=phone;Save();
    }
    public void SaveAvatar(byte[] png)
    {
        var me=RequireCurrent();if(png.Length is <8 or >2_000_000||!png.Take(8).SequenceEqual(new byte[]{137,80,78,71,13,10,26,10}))throw new ArgumentException("Geçerli bir profil fotoğrafı seç.");
        me.Profile.AvatarBase64=Convert.ToBase64String(png);Save();
    }
}
