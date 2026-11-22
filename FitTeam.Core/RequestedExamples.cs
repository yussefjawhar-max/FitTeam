using System.Security.Cryptography;
namespace FitTeam.Core;
public sealed record ExampleLogin(string Username,string Name,string Password,string Sport,string Gender);
public sealed record ExampleClubLogin(string Club,string Sport,string Gender,string Password,string CoachUsername,string CoachName,string CoachPassword);
public sealed record RequestedExampleResult(List<ExampleClubLogin> Clubs,List<ExampleLogin> FitnessMembers,List<ExampleLogin> FitnessCoaches);
public static class RequestedExamples
{
    public const string Batch="requested-examples-2026-10-09";
    private static string Password(string prefix)=>prefix+Convert.ToHexString(RandomNumberGenerator.GetBytes(4));
    public static RequestedExampleResult Create(AppData data,DateOnly today)
    {
        if(data.Users.Any(u=>u.ExampleBatch==Batch))throw new InvalidOperationException("Bu örnek kayıtlar zaten oluşturulmuş. Şifre listesini kullanabilirsin.");
        var clubs=new List<ExampleClubLogin>();var fitness=new List<ExampleLogin>();var fitnessCoaches=new List<ExampleLogin>();
        string[] brands=["Beşiktaş","Fenerbahçe","Galatasaray","Sarıyer","Trabzonspor"];
        string[] surnames=["Yılmaz","Kaya","Demir","Çelik","Şahin","Yıldız","Arslan","Aydın","Öztürk","Koç"];
        string[] men=["Ahmet","Mehmet","Mustafa","Ali","Hasan","Emre","Murat","Burak","Mert","Hakan","Tolga","Can","Serkan","Onur","Oğuz"];
        string[] women=["Ayşe","Fatma","Zeynep","Elif","Selin","Ece","İrem","Derya","Esra","Deniz","Ceren","İpek","Büşra","Seda","Aslı"];
        string[] cities=["İstanbul","Ankara","İzmir","Bursa","Antalya","Eskişehir","Konya","Samsun","Kayseri","Gaziantep"];
        User Account(string username,string name,string role,string sport,string gender,int age,string password)
        {
            if(data.Users.Any(u=>u.Username.Equals(username,StringComparison.OrdinalIgnoreCase)))throw new InvalidOperationException("Örnek kullanıcı adı başka bir hesapta kullanılıyor.");
            var user=new User{Username=username,Role=role,SportBranch=sport,IsExample=true,ExampleBatch=Batch,ProfileComplete=true,ExperienceYears=role==Choices.Roles[2]||role==Choices.Roles[3]?5+age%12:1+age%4,Profile=new Profile{Name=name,Age=age,Gender=gender,Country="Türkiye",CountryCode="TR",City=cities[age%cities.Length],HomeAddress="Yenişehir Mahallesi, Spor Sokak No: "+(age%30+1)+", Daire 2",BloodGroup="Bilmiyorum"}};
            Passwords.Set(user,password);data.Users.Add(user);return user;
        }
        int index=0;
        foreach(var sport in new[]{"Futbol","Basketbol","Voleybol"})foreach(var gender in new[]{"Erkek","Kadın"})for(int b=0;b<brands.Length;b++)
        {
            index++;string coachName=(gender=="Erkek"?men[(index-1)%men.Length]:women[(index-1)%women.Length])+" "+surnames[(index-1)%surnames.Length];string coachPassword=Password("Koc-");
            var coach=Account("koc"+index.ToString("D2"),coachName,Choices.Roles[3],sport,gender,28+index%18,coachPassword);
            var title=brands[b]+" "+(gender=="Kadın"?"Kız":"Erkek")+" "+sport;
            if(data.Teams.Any(t=>t.Name.Equals(title,StringComparison.OrdinalIgnoreCase)))throw new InvalidOperationException("Bu kulüp adı zaten kullanılıyor: "+title);
            string shared=RandomNumberGenerator.GetInt32(0,100000000).ToString("D8");while(clubs.Any(t=>t.Password==shared))shared=RandomNumberGenerator.GetInt32(0,100000000).ToString("D8");var credential=new User();Passwords.Set(credential,shared);
            var team=new Team{Name=title,CoachId=coach.Id,Gender=gender,SportBranch=sport,ExampleBatch=Batch,JoinPasswordHash=credential.PasswordHash,JoinPasswordSalt=credential.Salt,Chairperson=new ClubChairperson{Name=(gender=="Erkek"?men[(index+3)%men.Length]:women[(index+3)%women.Length])+" "+surnames[(index+2)%surnames.Length],Age=36+index%18,Country="Türkiye",City=cities[b%cities.Length]}};data.Teams.Add(team);
            clubs.Add(new(title,sport,gender,shared,coach.Username,coachName,coachPassword));
        }
        var coachIds=new List<Guid>();
        for(int i=0;i<3;i++)
        {
            string pw=Password("FitKoc-");var coach=Account("fitnesskoc"+(i+1),new[]{"Selin Aksoy","Tolga Yıldırım","Mert Karaca"}[i],Choices.Roles[2],"Fitness",i==0?"Kadın":"Erkek",29+i*4,pw);coach.CoachSpecialty=new[]{"Kilo yönetimi","Kuvvet ve kas gelişimi","Kondisyon"}[i];coachIds.Add(coach.Id);fitnessCoaches.Add(new(coach.Username,coach.Profile.Name,pw,"Fitness",coach.Profile.Gender));
        }
        string[] syrians=["عمر خالد","أحمد حسن","يوسف الحداد","مريم سليمان","لينا مصطفى"];
        var start=today.AddDays(-29);
        for(int i=0;i<50;i++)
        {
            bool female=i>=45?i>=48:i%2==1;string name=i>=45?syrians[i-45]:(female?women[(i/2)%women.Length]:men[(i/2)%men.Length])+" "+surnames[(i/10)%surnames.Length];string password=Password("Ft-");
            var user=Account("fitness"+(i+1).ToString("D2"),name,Choices.Roles[0],"Fitness",female?"Kadın":"Erkek",19+i%27,password);user.CoachId=coachIds[i%3];user.CoachIds=[coachIds[i%3]];if(i%10==0)user.CoachIds.Add(coachIds[(i+1)%3]);
            var p=user.Profile;if(i>=45){p.Country="Suriye";p.CountryCode="SY";p.City=i%2==0?"Şam":"Halep";p.HomeAddress="شارع الرياضة رقم "+(i-44)+"";}
            p.Height=female?159+i%15:172+i%20;p.InitialWeight=(female?70:84)+i%12;p.Weight=p.InitialWeight-(2.0m+i%6*0.4m);p.TargetWeight=p.Weight-3;p.Goal=Choices.Goals[0];p.Level=Choices.Levels[i%2];p.AvailableDays=[DayOfWeek.Monday,DayOfWeek.Wednesday,DayOfWeek.Friday];
            var plan=new TrainingPlan{UserId=user.Id,Start=start};int n=0;
            for(int dayIndex=0;dayIndex<30;dayIndex++)
            {
                var date=start.AddDays(dayIndex);bool train=p.AvailableDays.Contains(date.DayOfWeek);var exercises=train?FitService.BuildExercises(p,false,n++):[];
                bool attended=train&&(dayIndex+i)%(5+i%6)!=0;var day=new PlanDay{Date=date,Title=train?"Kişisel antrenman":"Aktif dinlenme",Status=!train?"Dinlenme":attended?"Tamamlandı":"Kaçırıldı",Exercises=exercises,Baseline=exercises.Select(e=>e.Copy()).ToList()};plan.Days.Add(day);
                if(attended)data.Workouts.Add(new WorkoutLog{UserId=user.Id,PlanDayId=day.Id,Date=date,Completed=true,Minutes=35+i%20,Calories=220+i%25*8,AverageHeart=120+i%20,MaxHeart=155+i%20,Steps=3500+i%20*170,Distance=2.5m+i%10*0.2m,Fatigue=3+(dayIndex+i)%5,Note="Antrenman tamamlandı",Exercises=exercises.Select(e=>new ExerciseResult{Name=e.Name,Done=true,Sets=e.Sets,Reps=e.Reps,Minutes=e.Minutes}).ToList()});
            }
            data.Plans.Add(plan);
            for(int week=0;week<5;week++)data.Weights.Add(new WeightEntry{UserId=user.Id,Date=start.AddDays(week*7),Weight=p.InitialWeight-(p.InitialWeight-p.Weight)*week/4});
            for(int d=0;d<7;d++)data.Water.Add(new WaterEntry{UserId=user.Id,Date=today.AddDays(-d),Litres=1.5m+(i+d)%8*0.2m});
            fitness.Add(new(user.Username,p.Name,password,"Fitness",p.Gender));
        }
        data.ShowExampleData=true;return new(clubs,fitness,fitnessCoaches);
    }
}
public sealed partial class FitService
{
    public bool ShowRecord(User user)=>!IsExample(user)||!string.IsNullOrEmpty(user.ExampleBatch);
    public bool ShowClub(Team team)=>!Data.Users.Any(u=>u.Id==team.CoachId&&IsExample(u))||!string.IsNullOrEmpty(team.ExampleBatch);
}
