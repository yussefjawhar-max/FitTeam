using FitTeam.Core;
static class FitnessCountsChecks
{
    public static void Run(Action<bool,string> check)
    {
        User Person(string role,string code="",bool active=true,bool pending=false,bool club=false)=>new(){Role=role,IsActive=active,NeedsApproval=pending,IsClubAccount=club,Profile=new(){CountryCode=code}};
        var users=new List<User>{Person(Choices.Roles[0],"TR"),Person(Choices.Roles[0],"SY"),Person(Choices.Roles[0]),Person(Choices.Roles[2]),Person(Choices.Roles[0],"TR",active:false),Person(Choices.Roles[0],"SY",pending:true),Person(Choices.Roles[3],club:true),Person(Choices.DeputyRole)};
        var owner=Person(Choices.Roles[0],"TR");owner.Email="jawharyusuf446@gmail.com";users.Add(owner);
        var summary=FitnessStatistics.Calculate(users);
        check(summary.Coaches==1&&summary.Members==3,"Fitness sayıları kapalı, bekleyen, kulüp, yardımcı ve başkan hesaplarını dahil etmez");
        check(summary.Turkey==1&&summary.OtherCountries==1&&summary.UnknownCountry==1,"Eksik ülke yabancı ülke olarak sayılmaz; grupların toplamı üye sayısına eşittir");
        check(summary.TurkeyShare==33.3m&&summary.OtherShare==33.3m&&FitnessStatistics.Calculate([]).TurkeyShare==0,"Fitness ülke oranları toplam üyelere göre hesaplanır; boş grupta sıfıra bölme olmaz");
        var path=Path.Combine(Path.GetTempPath(),"FitTeam_fitness_counts_"+Guid.NewGuid().ToString("N")+".json");
        try
        {
            var service=new FitService(new DataStore(path));var coaches=Enumerable.Range(1,10).Select(i=>new User{Username="fitness_coach_"+i,Role=Choices.Roles[2],Profile=new(){Name="Koç "+i}}).ToList();service.Data.Users.AddRange(coaches);
            service.Login("sporcu","Fit123!");service.SelectCoaches(coaches.Select(c=>c.Id));var member=service.RequireCurrent();
            var restored=new FitService(new DataStore(path));restored.Login("sporcu","Fit123!");
            check(FitService.CoachLinks(member).Count==10&&restored.SelectedCoaches(restored.RequireCurrent()).Count==10,"Üye on aktif fitness hocası seçebilir; seçimler yeniden açılışta korunur");
        }
        finally{foreach(var suffix in new[]{"",".bak",".tmp"})if(File.Exists(path+suffix))File.Delete(path+suffix);}
    }
}
