using System.Security.Cryptography;
namespace FitTeam.Core;
public static class RequestedClubRoster
{
    public const int Revision=1;
    public static readonly int[] Sizes=[25,35,22,28,32,24,30,26,21,29];
    public static List<ExampleClubStudent> Upgrade(AppData data,DateOnly today)
    {
        if(data.ClubRosterRevision>=Revision||!data.Users.Any(u=>u.ExampleBatch==RequestedClubStudents.Batch))return [];
        var clubs=data.Teams.Where(t=>t.IsActive&&t.ExampleBatch==RequestedExamples.Batch).OrderBy(t=>t.SportBranch).ThenBy(t=>t.Name).ToList();var result=new List<ExampleClubStudent>();
        string[] men=["Ahmet","Mehmet","Mustafa","Ali","Emre","Burak","Mert","Can","Onur","Hakan","Eren","Kerem","Berk","Oğuz","Serkan","Yasin","Enes","Arda","Umut","Kaan"];
        string[] women=["Ayşe","Zeynep","Elif","Selin","Ece","İrem","Ceren","İpek","Büşra","Seda","Aslı","Esra","Deniz","Derya","Melis","Nazlı","Sıla","Merve","Defne","Yağmur"];
        string[] surnames=["Yılmaz","Kaya","Demir","Çelik","Şahin","Yıldız","Arslan","Aydın","Öztürk","Aksoy","Karaca","Yıldırım","Koç","Kurt","Polat","Güneş","Eren","Tekin","Acar","Keskin"];
        for(int clubNumber=1;clubNumber<=clubs.Count;clubNumber++)
        {
            var club=clubs[clubNumber-1];int count=data.Users.Count(u=>u.TeamId==club.Id&&u.Role==Choices.Roles[1]&&u.IsActive&&!u.NeedsApproval);int slot=11;
            int target=Sizes[(clubNumber-1)%Sizes.Length];
            while(count<target)
            {
                string username="ogrenci"+clubNumber.ToString("D2")+slot.ToString("D2");if(data.Users.Any(u=>u.Username.Equals(username,StringComparison.OrdinalIgnoreCase))){slot++;continue;}
                int number=clubNumber*20+slot;bool female=club.Gender=="Kadın",syria=slot%10==0;
                var name=syria?(female?"لينا ":"عمر ")+new[]{"خالد","حسن","سليمان","مصطفى","أحمد"}[(clubNumber+slot)%5]:(female?women[(clubNumber+slot)%women.Length]:men[(clubNumber+slot)%men.Length])+" "+surnames[(clubNumber*3+slot)%surnames.Length];
                int collision=0;while(data.Users.Any(u=>u.TeamId==club.Id&&u.Profile.Name==name)){collision++;name=syria?(female?"لينا ":"عمر ")+new[]{"خالد","حسن","سليمان","مصطفى","أحمد"}[(clubNumber+slot/10+collision)%5]:(female?women[(clubNumber+slot)%women.Length]:men[(clubNumber+slot)%men.Length])+" "+surnames[(clubNumber*3+slot+collision)%surnames.Length];}
                var profile=new Profile{Name=name,Age=18+number%17,Gender=female?"Kadın":"Erkek",Country=syria?"Suriye":"Türkiye",CountryCode=syria?"SY":"TR",City=syria?"Şam":club.Chairperson.City,HomeAddress=syria?"شارع الرياضة رقم "+slot:"Yenişehir Mahallesi, Spor Sokak No: "+slot+", Daire 2",Height=female?160+number%18:170+number%20,Weight=female?58+number%15:68+number%20,Goal="Kondisyon geliştirmek",Level="Orta",AvailableDays=[DayOfWeek.Tuesday,DayOfWeek.Thursday,DayOfWeek.Saturday],Position=Choices.Positions[slot%Choices.Positions.Length],Jersey=slot};profile.InitialWeight=profile.Weight+0.8m;profile.TargetWeight=profile.Weight;
                string password="Ogr-"+Convert.ToHexString(RandomNumberGenerator.GetBytes(4));var user=new User{Username=username,Role=Choices.Roles[1],SportBranch=club.SportBranch,TeamId=club.Id,IsExample=true,ExampleBatch=RequestedClubStudents.Batch,ProfileComplete=true,ExperienceYears=1+number%5,RegisteredAt=DateTimeOffset.UtcNow,Profile=profile};Passwords.Set(user,password);data.Users.Add(user);result.Add(new(club.Name,username,name,password));
                AddCalendar(data,user,club,today,number);count++;slot++;
            }
            var coach=data.Users.FirstOrDefault(u=>u.Id==club.CoachId);if(coach!=null&&coach.IsExample&&coach.WorkHours.Count==0)coach.WorkHours=new[]{DayOfWeek.Tuesday,DayOfWeek.Thursday,DayOfWeek.Saturday}.Select(d=>new CoachHours{Day=d,Start=new TimeOnly(16+clubNumber%3,0),End=new TimeOnly(18+clubNumber%3,0)}).ToList();
        }
        var phones=data.Users.Where(u=>u.Phone.Length>0).Select(u=>u.Phone).ToHashSet();int phoneNumber=1;
        foreach(var user in data.Users.Where(u=>u.IsExample&&(u.ExampleBatch==RequestedExamples.Batch||u.ExampleBatch==RequestedClubStudents.Batch)))
        {
            if(user.Phone.Length==0){string phone;do{phone="+905000"+(phoneNumber++).ToString("D6");}while(phones.Contains(phone));user.Phone=phone;phones.Add(phone);}
            var p=user.Profile;int clubIndex=clubs.FindIndex(t=>t.Id==user.TeamId);p.TrainingStart??=new TimeOnly(clubIndex<0?18:16+(clubIndex+1)%3,0);p.TrainingHours=p.TrainingHours is 2 or 3?p.TrainingHours:2;p.ActivityLevel=p.ActivityLevel.Length==0?"moderate":p.ActivityLevel;p.BirthDate??=today.AddYears(-p.Age);
            if(p.Address==null&&p.CountryCode=="TR"&&p.HomeAddress.StartsWith("Yenişehir Mahallesi",StringComparison.Ordinal))
            {
                var province=AddressCatalog.Province(p.City)??AddressCatalog.Province("İstanbul")!;var district=province.Name=="İstanbul"?AddressCatalog.District(province.Id,"Bağcılar")!:AddressCatalog.Districts.First(d=>d.ProvinceId==province.Id);
                var neighborhood=AddressCatalog.Neighborhoods.First(n=>n.DistrictId==district.Id);p.Address=new PostalAddress{Province=province.Name,District=district.Name,Neighborhood=neighborhood.Name,Street="Spor Sokak",Building=(1+phoneNumber%40).ToString(),Apartment=(1+phoneNumber%12).ToString()};p.Address.Validate("TR");p.City=province.Name;p.HomeAddress=p.Address.Display;
            }
            if(user.Role==Choices.Roles[2]&&user.WorkHours.Count==0)user.WorkHours=Choices.Days.Select(d=>new CoachHours{Day=d,Start=new TimeOnly(9,0),End=new TimeOnly(21,0)}).ToList();
        }
        data.ClubRosterRevision=Revision;return result;
    }
    private static void AddCalendar(AppData data,User user,Team club,DateOnly today,int number)
    {
        var p=user.Profile;var start=today.AddDays(-29);var plan=new TrainingPlan{UserId=user.Id,Start=start};int index=0;
        for(int i=0;i<30;i++)
        {
            var date=start.AddDays(i);bool train=p.AvailableDays.Contains(date.DayOfWeek);var exercises=train?club.SportBranch=="Futbol"?FitService.BuildExercises(p,true,index++):new List<Exercise>{new(){Name="Dinamik ısınma",Muscle="Tüm vücut",Sets=1,Reps=10,Minutes=8},new(){Name=club.SportBranch=="Basketbol"?"Top sürme ve pas çalışması":"Pas ve servis çalışması",Muscle="Teknik",Sets=3,Reps=12,Minutes=20,RestSeconds=90},new(){Name="Çeviklik parkuru",Muscle="Kondisyon",Sets=3,Reps=8,Minutes=12,RestSeconds=90}}:[];
            bool done=train&&date<today&&(i+number)%7!=0;var day=new PlanDay{Date=date,Title=train?club.SportBranch+" takım antrenmanı":"Aktif dinlenme",Status=!train?"Dinlenme":date==today?"Planlandı":done?"Tamamlandı":"Kaçırıldı",Exercises=exercises,Baseline=exercises.Select(e=>e.Copy()).ToList()};plan.Days.Add(day);
            if(done)data.Workouts.Add(new WorkoutLog{UserId=user.Id,PlanDayId=day.Id,Date=date,Completed=true,Minutes=45,Calories=300,AverageHeart=125,MaxHeart=160,Fatigue=4,Note="Kulüp antrenmanı tamamlandı",Exercises=exercises.Select(e=>new ExerciseResult{Name=e.Name,Done=true,Sets=e.Sets,Reps=e.Reps,Minutes=e.Minutes}).ToList()});
        }
        data.Plans.Add(plan);for(int i=0;i<5;i++)data.Weights.Add(new(){UserId=user.Id,Date=start.AddDays(i*7),Weight=p.InitialWeight-i*0.2m});
    }
}
