using FitTeam.Core;
static class ClubRosterUpgradeChecks
{
    public static void Run(Action<bool,string> check)
    {
        var today=new DateOnly(2026,10,10);var data=new AppData();RequestedExamples.Create(data,today);var original=RequestedClubStudents.Create(data,today);var protectedUser=data.Users.First(u=>u.Username==original[0].Username);var hash=protectedUser.PasswordHash;protectedUser.Phone="+905551111111";protectedUser.Profile.TrainingStart=new TimeOnly(14,0);
        var added=RequestedClubRoster.Upgrade(data,today);var counts=data.Teams.Select(t=>data.Users.Count(u=>u.TeamId==t.Id&&u.IsActive&&u.Role==Choices.Roles[1])).ToList();
        check(counts.Sum()==816&&counts.Min()>=20&&counts.Distinct().Count()==10&&counts.Contains(25)&&counts.Contains(35)&&counts.Contains(22),"Kulüp kadroları en az yirmi kişidir; 25, 35 ve 22 dahil farklı büyüklüklerde toplam 816 öğrenci bulunur");
        check(added.Count==516&&protectedUser.PasswordHash==hash&&protectedUser.Phone=="+905551111111"&&protectedUser.Profile.TrainingStart==new TimeOnly(14,0),"Mevcut hesap şifreleri, girilmiş telefon ve ders saatleri korunur; yalnız eksik öğrenciler eklenir");
        var fixtures=data.Users.Where(u=>u.ExampleBatch==RequestedClubStudents.Batch).ToList();check(fixtures.All(u=>u.Phone.Length==13&&u.Profile.TrainingStart!=null&&u.Profile.BirthDate!=null)&&fixtures.Select(u=>u.Phone).Distinct().Count()==fixtures.Count,"Her kulüp öğrencisinin ayrı telefon, doğum tarihi ve ders saati kayıtlıdır");
        check(added.All(a=>Passwords.Verify(data.Users.Single(u=>u.Username==a.Username),a.Password))&&data.Users.Select(u=>u.Username.ToUpperInvariant()).Distinct().Count()==data.Users.Count,"Yeni öğrencilerin benzersiz kullanıcı adı ve geçerli kişisel şifresi vardır");
        check(data.Teams.All(t=>data.Users.Where(u=>u.TeamId==t.Id).All(u=>u.Profile.Gender==t.Gender)),"Farklı büyüklükteki kız ve erkek kulüp kadroları kendi grubuyla tutarlıdır");
        int before=data.Users.Count;check(RequestedClubRoster.Upgrade(data,today).Count==0&&data.Users.Count==before,"Kadrolar yeniden açılışta çoğaltılmaz");
    }
}
