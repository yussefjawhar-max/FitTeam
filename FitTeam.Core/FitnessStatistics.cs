namespace FitTeam.Core;
public sealed record FitnessPopulation(int Coaches,int Members,int Turkey,int OtherCountries,int UnknownCountry)
{
    public decimal TurkeyShare=>Members==0?0:Math.Round(100m*Turkey/Members,1);
    public decimal OtherShare=>Members==0?0:Math.Round(100m*OtherCountries/Members,1);
}
public static class FitnessStatistics
{
    public static FitnessPopulation Calculate(IEnumerable<User> users)
    {
        var active=users.Where(u=>u.IsActive&&!u.NeedsApproval&&!u.IsClubAccount&&!u.Email.Equals("jawharyusuf446@gmail.com",StringComparison.OrdinalIgnoreCase)).ToList();
        var members=active.Where(u=>u.Role==Choices.Roles[0]).ToList();
        var countries=members.Select(u=>CountryCatalog.ByCode(u.Profile.CountryCode)?.Code??CountryCatalog.Resolve(u.Profile.Country)?.Code).ToList();
        return new(active.Count(u=>u.Role==Choices.Roles[2]),members.Count,countries.Count(c=>c=="TR"),countries.Count(c=>c!=null&&c!="TR"),countries.Count(c=>c==null));
    }
}
