namespace FitTeam.Core;

public enum AdministrationGroup { People, Students, Coaches, FitnessCoaches, FitnessMembers }
public sealed record AdminPersonDay(User Person,DateOnly Date,List<PlanDay> Plans,List<WorkoutLog> Workouts,
    List<FoodEntry> Foods,List<MealAssignment> AssignedMeals,List<User> Coaches,decimal Water);

public sealed partial class FitService
{
    public List<User> AdministrationUsers(AdministrationGroup group)
    {
        RequireAdministrator();
        return Data.Users.Where(u=>u.IsActive&&!u.NeedsApproval&&ShowRecord(u)&&
            !u.Email.Equals("jawharyusuf446@gmail.com",StringComparison.OrdinalIgnoreCase)&&u.Role!=Choices.DeputyRole)
            .Where(u=>group switch
            {
                AdministrationGroup.Students=>u.Role==Choices.Roles[0]||u.Role==Choices.Roles[1],
                AdministrationGroup.Coaches=>u.Role==Choices.Roles[2]||u.Role==Choices.Roles[3],
                AdministrationGroup.FitnessCoaches=>u.Role==Choices.Roles[2],
                AdministrationGroup.FitnessMembers=>u.Role==Choices.Roles[0],
                _=>true
            }).OrderBy(u=>u.Profile.Name).ThenBy(u=>u.Username).ToList();
    }
    public AdminPersonDay AdministrationDay(Guid id,DateOnly date)
    {
        RequireAdministrator();
        var person=Data.Users.FirstOrDefault(u=>u.Id==id&&ShowRecord(u))??throw new InvalidOperationException("Bu kişinin kaydı bulunamadı.");
        var coachIds=CoachLinks(person).ToHashSet();
        var team=Data.Teams.FirstOrDefault(t=>t.Id==person.TeamId&&t.IsActive&&ShowClub(t));
        if(team!=null)coachIds.Add(team.CoachId);
        return new(person,date,Data.Plans.Where(p=>p.UserId==id).SelectMany(p=>p.Days).Where(d=>d.Date==date).ToList(),
            Data.Workouts.Where(w=>w.UserId==id&&w.Date==date).ToList(),Data.Foods.Where(f=>f.UserId==id&&f.Date==date).ToList(),
            Data.MealAssignments.Where(m=>m.UserId==id).ToList(),Data.Users.Where(u=>coachIds.Contains(u.Id)&&u.IsActive&&ShowRecord(u)).ToList(),
            Data.Water.Where(w=>w.UserId==id&&w.Date==date).Sum(w=>w.Litres));
    }
}
