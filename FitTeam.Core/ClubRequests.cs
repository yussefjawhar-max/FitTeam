namespace FitTeam.Core;
public sealed partial class FitService
{
    public void RequestClub(Guid clubId)
    {
        var user=RequireCurrent();
        if(user.Role!=Choices.Roles[1]||IsPresident||user.NeedsApproval||user.RequestedRole.Length>0)throw new InvalidOperationException("Kulüp üyelik isteği sporcu hesabından gönderilir.");
        var club=Data.Teams.FirstOrDefault(t=>t.Id==clubId&&t.IsActive&&t.SportBranch==SportOf(user))??throw new ArgumentException("Branşına uygun kulübü seç.");
        user.RequestedClubId=club.Id;store.Save();
    }
    public void AdminApproveClubRequest(Guid userId)
    {
        RequireAdministrator();var user=Data.Users.FirstOrDefault(u=>u.Id==userId&&u.IsActive&&!u.NeedsApproval&&u.RequestedRole.Length==0&&u.Role==Choices.Roles[1])??throw new ArgumentException("Aktif sporcu seç.");
        var club=Data.Teams.FirstOrDefault(t=>t.Id==user.RequestedClubId&&t.IsActive&&t.SportBranch==SportOf(user))??throw new ArgumentException("Bu kulüp isteği artık geçerli değil.");
        user.TeamId=club.Id;user.RequestedClubId=null;store.Save();
    }
    public void AdminRejectClubRequest(Guid userId)
    {
        RequireAdministrator();var user=Data.Users.FirstOrDefault(u=>u.Id==userId)??throw new ArgumentException("Kişi bulunamadı.");user.RequestedClubId=null;store.Save();
    }
}
