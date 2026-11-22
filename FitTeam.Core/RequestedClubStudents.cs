using System.Security.Cryptography;
namespace FitTeam.Core;
public sealed record ExampleClubStudent(string Club,string Username,string Name,string Password);
public static class RequestedClubStudents
{
    public const string Batch="requested-club-students-2026-10-09";
    public static List<ExampleClubStudent> Create(AppData data,DateOnly today)
    {
        if(data.Users.Any(u=>u.ExampleBatch==Batch))throw new InvalidOperationException("Kulüp öğrencileri zaten oluşturuldu.");
        var clubs=data.Teams.Where(t=>t.IsActive&&t.ExampleBatch==RequestedExamples.Batch).OrderBy(t=>t.SportBranch).ThenBy(t=>t.Name).ToList();if(clubs.Count!=30)throw new InvalidOperationException("Önce istenen otuz örnek kulübü oluştur.");
        string[] men=["Ahmet","Mehmet","Mustafa","Ali","Emre","Burak","Mert","Can","Onur","Hakan","Eren","Kerem","Berk","Oğuz","Serkan","Yasin","Enes","Arda","Umut","Kaan"];
        string[] women=["Ayşe","Zeynep","Elif","Selin","Ece","İrem","Ceren","İpek","Büşra","Seda","Aslı","Esra","Deniz","Derya","Melis","Nazlı","Sıla","Merve","Defne","Yağmur"];
        string[] surnames=["Yılmaz","Kaya","Demir","Çelik","Şahin","Yıldız","Arslan","Aydın","Öztürk","Aksoy","Karaca","Yıldırım","Koç","Kurt","Polat","Güneş","Eren","Tekin","Acar","Keskin"];
        string[] cities=["İstanbul","Ankara","İzmir","Bursa","Antalya","Eskişehir","Konya","Samsun"];
        var result=new List<ExampleClubStudent>();int clubNumber=0;
        foreach(var team in clubs)
        {
            clubNumber++;bool female=team.Gender=="Kadın";
            for(int i=0;i<10;i++)
            {
                int number=(clubNumber-1)*10+i;bool syria=i==9;string username="ogrenci"+clubNumber.ToString("D2")+(i+1).ToString("D2");if(data.Users.Any(u=>u.Username==username))throw new InvalidOperationException("Bu örnek öğrenci adı zaten var.");
                string name=syria?(female?"لينا "+new[]{"خالد","حسن","سليمان","مصطفى","أحمد"}[clubNumber%5]:"عمر "+new[]{"خالد","حسن","سليمان","مصطفى","أحمد"}[clubNumber%5]):(female?women[(clubNumber+i)%women.Length]:men[(clubNumber+i)%men.Length])+" "+surnames[(clubNumber*3+i)%surnames.Length];string password="Ogr-"+Convert.ToHexString(RandomNumberGenerator.GetBytes(4));
                var profile=new Profile{Name=name,Age=18+number%17,Gender=female?"Kadın":"Erkek",Country=syria?"Suriye":"Türkiye",CountryCode=syria?"SY":"TR",City=syria?(clubNumber%2==0?"Şam":"Halep"):cities[clubNumber%cities.Length],HomeAddress=syria?"شارع الرياضة رقم "+(i+1):"Yenişehir Mahallesi, Spor Sokak No: "+(i+1)+", Daire "+clubNumber+"",BloodGroup="Bilmiyorum",Height=female?158+number%20:170+number%22,Weight=female?58+number%15:68+number%20,Goal="Kondisyon geliştirmek",Level="Orta",AvailableDays=[DayOfWeek.Tuesday,DayOfWeek.Thursday,DayOfWeek.Saturday],Position=Choices.Positions[i%Choices.Positions.Length],Jersey=i+1};profile.InitialWeight=profile.Weight+0.8m;profile.TargetWeight=profile.Weight;
                var user=new User{Username=username,Role=Choices.Roles[1],SportBranch=team.SportBranch,TeamId=team.Id,IsExample=true,ExampleBatch=Batch,ProfileComplete=true,ExperienceYears=1+number%5,Profile=profile};Passwords.Set(user,password);data.Users.Add(user);result.Add(new(team.Name,username,name,password));
                var start=today.AddDays(-29);var plan=new TrainingPlan{UserId=user.Id,Start=start};int n=0;
                for(int d=0;d<30;d++)
                {
                    var date=start.AddDays(d);bool train=profile.AvailableDays.Contains(date.DayOfWeek);var exercises=train?team.SportBranch=="Futbol"?FitService.BuildExercises(profile,true,n++):new List<Exercise>{new(){Name="Dinamik ısınma",Muscle="Tüm vücut",Sets=1,Reps=10,Minutes=8},new(){Name=team.SportBranch=="Basketbol"?"Top sürme ve pas":"Voleybol pas ve servis",Muscle="Teknik",Sets=3,Reps=12,Minutes=20},new(){Name="Çeviklik parkuru",Muscle="Kondisyon",Sets=3,Reps=8,Minutes=12}}:[];
                    bool done=train&&(d+number)%7!=0;var day=new PlanDay{Date=date,Title=train?team.SportBranch+" takım antrenmanı":"Aktif dinlenme",Status=!train?"Dinlenme":done?"Tamamlandı":"Kaçırıldı",Exercises=exercises,Baseline=exercises.Select(e=>e.Copy()).ToList()};plan.Days.Add(day);
                    if(done)data.Workouts.Add(new WorkoutLog{UserId=user.Id,PlanDayId=day.Id,Date=date,Completed=true,Minutes=40+number%15,Calories=260+number%20*6,AverageHeart=125+number%15,MaxHeart=160+number%15,Steps=4000+number%12*150,Distance=3+number%7*0.2m,Fatigue=3+number%4,Note="Kulüp antrenmanı tamamlandı",Exercises=exercises.Select(e=>new ExerciseResult{Name=e.Name,Done=true,Sets=e.Sets,Reps=e.Reps,Minutes=e.Minutes}).ToList()});
                }
                data.Plans.Add(plan);for(int w=0;w<5;w++)data.Weights.Add(new WeightEntry{UserId=user.Id,Date=start.AddDays(w*7),Weight=profile.InitialWeight-0.2m*w});
            }
        }
        data.ShowExampleData=true;return result;
    }
}
