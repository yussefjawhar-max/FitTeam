using System.Security.Cryptography;
namespace FitTeam.Core;
public sealed class ClubChairperson
{
    public string Name {get;set;}="";
    public int Age {get;set;}
    public string Country {get;set;}="";
    public string City {get;set;}="";
    public void Validate(){if(Name.Trim().Length<2||Age is <18 or >100||Country.Trim().Length<2||City.Trim().Length<2)throw new ArgumentException("Kulüp başkanının ad soyad, yaş, ülke ve şehir bilgilerini doldur.");}
}
public sealed class ClubAccessTicket
{
    public Guid TeamId {get;}
    internal string Hash {get;}
    internal ClubAccessTicket(Guid id,string hash){TeamId=id;Hash=hash;}
}
public sealed record ClubStudentCounts(int Total,int Girls,int Boys,int Unspecified);
public sealed partial class FitService
{
    public Team AdminCreateClub(string name,string sport,string gender,ClubChairperson chairperson)
    {
        RequireAdministrator();chairperson.Validate();
        var team=AdminAddClub(name,"kulup-"+Guid.NewGuid().ToString("N")[..12],"",Convert.ToHexString(RandomNumberGenerator.GetBytes(24)),gender,sport);
        team.Chairperson=chairperson;store.Save();return team;
    }
    public void AdminSetChairperson(Guid id,ClubChairperson chairperson)
    {
        RequireAdministrator();chairperson.Validate();var team=Data.Teams.FirstOrDefault(t=>t.Id==id)??throw new ArgumentException("Kulüp bulunamadı.");team.Chairperson=chairperson;store.Save();
    }
    public void AdminRemoveClub(Guid id)
    {
        RequireAdministrator();var team=Data.Teams.FirstOrDefault(t=>t.Id==id)??throw new ArgumentException("Kulüp bulunamadı.");
        foreach(var member in Data.Users.Where(u=>u.TeamId==id))member.TeamId=null;
        var clubAccount=Data.Users.FirstOrDefault(u=>u.Id==team.CoachId&&u.IsClubAccount);
        if(clubAccount!=null)Data.Users.Remove(clubAccount);
        Data.Teams.Remove(team);store.Save();
    }
    public string AdminRotateClubPassword(Guid id)
    {
        RequireAdministrator();var team=Data.Teams.FirstOrDefault(t=>t.Id==id&&t.IsActive)??throw new ArgumentException("Aktif kulüp seç.");
        string password;
        do{password=RandomNumberGenerator.GetInt32(0,100000000).ToString("D8");}
        while(!string.IsNullOrEmpty(team.JoinPasswordHash)&&Passwords.Verify(new User{Salt=team.JoinPasswordSalt,PasswordHash=team.JoinPasswordHash},password));
        var credential=new User();Passwords.Set(credential,password);
        team.JoinPasswordHash=credential.PasswordHash;team.JoinPasswordSalt=credential.Salt;team.JoinFailures=0;team.JoinBlockedUntil=DateTimeOffset.MinValue;store.Save();return password;
    }
    public ClubAccessTicket VerifyClubAccess(string name,string password)
    {
        var team=Data.Teams.FirstOrDefault(t=>t.Name.Equals(name.Trim(),StringComparison.OrdinalIgnoreCase)&&t.IsActive);
        if(team==null||string.IsNullOrEmpty(team.JoinPasswordHash)||team.JoinBlockedUntil>administrationTime.GetUtcNow())throw new ArgumentException("Kulüp adı veya şifre yanlış ya da giriş geçici olarak kapalı. Kulüp yetkilisinden güncel şifreyi iste.");
        if(password.Length!=8||!password.All(c=>c>='0'&&c<='9')||!Passwords.Verify(new User{Salt=team.JoinPasswordSalt,PasswordHash=team.JoinPasswordHash},password))
        {
            if(++team.JoinFailures>=5){team.JoinBlockedUntil=administrationTime.GetUtcNow().AddMinutes(5);team.JoinFailures=0;}
            store.Save();throw new ArgumentException("Kulüp adı veya 8 haneli şifre yanlış.");
        }
        team.JoinFailures=0;store.Save();return new ClubAccessTicket(team.Id,team.JoinPasswordHash);
    }
    private Team RequireClubTicket(ClubAccessTicket ticket)
    {
        return Data.Teams.FirstOrDefault(t=>t.Id==ticket.TeamId&&t.IsActive&&t.JoinPasswordHash==ticket.Hash&&!string.IsNullOrEmpty(t.JoinPasswordHash))??throw new InvalidOperationException("Kulüp şifresi yenilendi veya kulüp çıkarıldı. Güncel şifreyle tekrar gir.");
    }
    public void JoinClub(ClubAccessTicket ticket)
    {
        var member=RequireCurrent();var team=RequireClubTicket(ticket);
        if(IsPresident||member.Role==Choices.Roles[2]||member.Role==Choices.Roles[3]||!member.ProfileComplete||member.NeedsApproval)throw new InvalidOperationException("Kulübe kendi tamamlanmış öğrenci hesabınla katıl.");
        member.TeamId=team.Id;member.SportBranch=team.SportBranch;member.Role=Choices.Roles[1];member.CoachIds=[];member.CoachId=null;store.Save();
    }
    public List<User> ClubStudents(Guid id)
    {
        RequireAdministrator();return Data.Users.Where(u=>u.IsActive&&!u.NeedsApproval&&ShowRecord(u)&&!u.IsClubAccount&&u.TeamId==id&&(u.Role==Choices.Roles[0]||u.Role==Choices.Roles[1])).OrderBy(u=>u.Profile.Name).ToList();
    }
    public ClubStudentCounts ClubCounts(Guid id)
    {
        var students=ClubStudents(id);int girls=students.Count(u=>u.Profile.Gender is "Kadın" or "Kız"),boys=students.Count(u=>u.Profile.Gender=="Erkek");return new(students.Count,girls,boys,students.Count-girls-boys);
    }
}
