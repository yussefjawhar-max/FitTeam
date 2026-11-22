using FitTeam.Core;
static class ClubAccessChecks
{
    public static async Task Run(Action<bool,string> check)
    {
        var path=Path.Combine(Path.GetTempPath(),"fitteam-club-access-"+Guid.NewGuid()+".json");
        try
        {
            var clock=new CheckClock();var service=new FitService(new DataStore(path),clock);
            void Reject(Action a,string label){bool rejected=false;try{a();}catch(Exception ex)when(ex is ArgumentException or InvalidOperationException){rejected=true;}check(rejected,label);}
            var auth=new CodeAuthClient(new Uri("http://127.0.0.1:12345/"),new HttpClient(new CodeFixture("jawharyusuf446@gmail.com",true)));
            var owner=await auth.PresidentLogin("fixture","fixture-password");service.LoginCode(owner);
            var team=service.AdminCreateClub("Yeni Test Kulübü","Futbol","Karma",new ClubChairperson{Name="Örnek Başkan",Age=35,Country="Türkiye",City="İstanbul"});
            var password=service.AdminRotateClubPassword(team.Id);
            check(password.Length==8&&password.All(char.IsDigit)&&team.JoinPasswordHash!=password,"Kulüp şifresi sekiz rakamdır ve yalnız tuzlu parola özeti saklanır");
            var ticket=service.VerifyClubAccess(team.Name,password);check(service.IsPresident,"Kulüp şifresini doğrulamak mevcut hesabı kulüp yöneticisine dönüştürmez");
            clock.Advance(TimeSpan.FromDays(2));check(service.VerifyClubAccess(team.Name,password).TeamId==team.Id,"Kulüp şifresi yetkili yenileyene kadar geçerlidir");
            var girl=service.AdminAddUser("clubgirl","Örnek Kız Öğrenci",Choices.Roles[1],"","","FixturePass-84!","Futbol");girl.ProfileComplete=true;girl.Profile.Gender="Kadın";girl.Profile.Country="Türkiye";girl.Profile.City="Ankara";service.Save();
            check(service.Login("clubgirl","FixturePass-84!"),"Öğrenci kulüp şifresinden ayrı kişisel hesabıyla girer");service.JoinClub(ticket);check(service.RequireCurrent().Id==girl.Id&&girl.TeamId==team.Id&&!service.IsPresident&&girl.Role==Choices.Roles[1],"Kulübe katılan öğrencinin kimliği ve öğrenci rolü korunur");
            Reject(()=>service.AdminRotateClubPassword(team.Id),"Öğrenci kulübün ortak şifresini yenileyemez");Reject(()=>service.ClubStudents(team.Id),"Öğrenci yetkili öğrenci listesini açamaz");
            service.LoginCode(owner);var boy=service.AdminAddUser("clubboy","Örnek Erkek Öğrenci",Choices.Roles[1],"","","FixturePass-84!","Futbol");boy.ProfileComplete=true;boy.Profile.Gender="Erkek";boy.TeamId=team.Id;
            var unspecified=service.AdminAddUser("clubunspecified","Örnek Öğrenci",Choices.Roles[1],"","","FixturePass-84!","Futbol");unspecified.ProfileComplete=true;unspecified.Profile.Gender="Belirtmek istemiyorum";unspecified.TeamId=team.Id;service.Save();
            var counts=service.ClubCounts(team.Id);check(counts.Total==3&&counts.Girls==1&&counts.Boys==1&&counts.Unspecified==1,"Kulübün toplam, kız, erkek ve belirtilmeyen öğrenci sayıları ayrı hesaplanır");
            service.AdminSetUserActive(boy.Id,false);check(service.ClubCounts(team.Id).Total==2,"Çıkarılan öğrenci aktif kulüp sayısına katılmaz");
            string replacement;do{replacement=service.AdminRotateClubPassword(team.Id);}while(replacement==password);
            Reject(()=>service.VerifyClubAccess(team.Name,password),"Yeni kulüp şifresi oluşturulunca eski şifre geçersiz olur");
            service.Login("clubgirl","FixturePass-84!");Reject(()=>service.JoinClub(ticket),"Şifre yenilenmeden önce alınan kulüp katılım bileti geçersiz olur");
            var newTicket=service.VerifyClubAccess(team.Name,replacement);service.JoinClub(newTicket);check(girl.TeamId==team.Id,"Yeni kulüp şifresi kişisel öğrenci hesabına katılım sağlar");
            service.LoginCode(owner);Reject(()=>service.JoinClub(newTicket),"Kulüp şifresi yetkili hesabını öğrenci hesabına dönüştüremez");
            var loaded=new FitService(new DataStore(path),clock);loaded.LoginCode(owner);check(loaded.Data.Teams.First(t=>t.Id==team.Id).Chairperson.Country=="Türkiye"&&loaded.ClubCounts(team.Id).Total==2,"Başkan bilgileri ve öğrenci sayıları yeniden açılışta korunur");
            service.AdminSetClubActive(team.Id,false);Reject(()=>service.VerifyClubAccess(team.Name,replacement),"Çıkarılan kulübün sekiz haneli şifresi kapanır");
            service.AdminSetClubActive(team.Id,true);Reject(()=>service.VerifyClubAccess(team.Name,replacement),"Yeniden etkinleştirilen kulübe yeni şifre oluşturulması gerekir");
            service.AdminRemoveClub(team.Id);check(!service.Data.Teams.Any(t=>t.Id==team.Id)&&girl.TeamId==null&&!service.Data.Users.Any(u=>u.Id==team.CoachId),"Kulübü sıfırlamak kulüp hesabını ve üyelik bağlarını kaldırır, öğrenci kişisel hesabını korur");
            var recreated=service.AdminCreateClub("Yeni Test Kulübü","Futbol","Karma",new ClubChairperson{Name="Yeni Başkan",Age=40,Country="Türkiye",City="Ankara"});
            check(recreated.Id!=team.Id&&recreated.Chairperson.Name=="Yeni Başkan"&&service.ClubCounts(recreated.Id).Total==0,"Sıfırlanan kulüp aynı gün aynı adla boş kayıt olarak yeniden oluşturulur");

        }
        finally{File.Delete(path);File.Delete(path+".bak");File.Delete(path+".tmp");}
    }
}
