namespace FitTeam.Core;
public sealed record ParticipantStanding(User User,int Planned,int Completed,decimal Compliance,decimal WeightLoss);
public sealed record CountryParticipation(string Code,string Name,int Count,decimal Share,decimal AverageCompliance,decimal AverageWeightLoss);
public static class ParticipationAnalytics
{
    public static ParticipantStanding Standing(AppData data,User user,DateOnly today)
    {
        var plan=data.Plans.Where(p=>p.UserId==user.Id).OrderByDescending(p=>p.Start).FirstOrDefault();
        var days=plan?.Days.Where(d=>d.Date<=today&&d.Date>=today.AddDays(-29)&&d.Exercises.Count>0).ToList()??[];
        var completed=days.Count(d=>data.Workouts.Any(w=>w.UserId==user.Id&&w.PlanDayId==d.Id&&w.Completed));
        return new(user,days.Count,completed,days.Count==0?0:Math.Round(100m*completed/days.Count,1),user.Profile.InitialWeight-user.Profile.Weight);
    }
    public static List<CountryParticipation> Countries(List<ParticipantStanding> people)
    {
        return people.GroupBy(p=>CountryCatalog.ByCode(p.User.Profile.CountryCode)?.Code??CountryCatalog.Resolve(p.User.Profile.Country)?.Code??"??").Select(g=>new CountryParticipation(g.Key,CountryCatalog.ByCode(g.Key)?.Name??AppLanguage.Text("Belirtilmedi"),g.Count(),people.Count==0?0:Math.Round(100m*g.Count()/people.Count,1),Math.Round(g.Average(p=>p.Compliance),1),Math.Round(g.Average(p=>p.WeightLoss),1))).OrderByDescending(c=>c.Count).ToList();
    }
}
