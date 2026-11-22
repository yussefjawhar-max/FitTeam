using FitTeam.Core;
static class AdministrationChecks
{
    public static async Task Run(Action<bool,string> check)
    {
        var path=Path.Combine(Path.GetTempPath(),"fitteam-administration-"+Guid.NewGuid()+".json");
        try
        {
            var clock=new CheckClock();var store=new DataStore(path);var s=new FitService(store,clock);
            void Reject(Action action,string label){bool fail=false;try{action();}catch(Exception e)when(e is ArgumentException or InvalidOperationException){fail=true;}check(fail,label);}
            s.Login("sporcu","Fit123!");Reject(()=>s.AdminAddUser("staff","Yeni Hoca",Choices.Roles[2],"staff@gmail.com","","Temporary-84!"),"Normal kullanıcı yetkili hesabı oluşturamaz");
            var auth=new CodeAuthClient(new Uri("http://127.0.0.1:12345/"),new HttpClient(new CodeFixture("jawharyusuf446@gmail.com",true)));
            async Task President()=>s.LoginCode(await auth.PresidentLogin("fixture","fixture-password"));
            await President();var staff=s.AdminAddUser("staff","Yeni Hoca",Choices.Roles[2],"staff@gmail.com","+905321234567","Temporary-84!");
            check(!staff.MustChangePassword&&!staff.EmailVerified&&!staff.PhoneVerified,"Yetkili uygulama şifresi verir; ilk girişte değişim zorunlu değildir ve doğrulama uydurulmaz");
            var club=s.AdminAddClub("Yıldız FK","yildizfk","club@gmail.com","Temporary-84!","Karma");
            var second=s.AdminAddClub("Kent FK","kentfk","otherclub@gmail.com","Temporary-84!","Erkek");
            var clubUser=s.Data.Users.First(u=>u.Id==club.CoachId);
            var player=s.AdminAddUser("player","Yeni Oyuncu",Choices.Roles[1],"","","Temporary-84!");player.TeamId=club.Id;
            var other=s.AdminAddUser("otherplayer","Diğer Oyuncu",Choices.Roles[1],"","","Temporary-84!");other.TeamId=second.Id;s.Save();
            check(s.Login("staff@gmail.com","Temporary-84!"),"Yetkilinin tanımladığı hoca uygulama şifresiyle girer");
            Reject(()=>s.ChangeOwnPassword("wrong","NewOwn-82!"),"Kendi şifre değişiminde mevcut şifre doğrulanır");
            s.ChangeOwnPassword("Temporary-84!","NewOwn-82!");check(!staff.MustChangePassword&&!s.Login("staff","Temporary-84!")&&s.Login("staff","NewOwn-82!"),"Kullanıcı geçici şifresini değiştirir; eski şifre reddedilir");
            var memberAuth=new CodeAuthClient(new Uri("http://127.0.0.1:12345/"),new HttpClient(new CodeFixture("verifiedmember@gmail.com",false)));
            s.LoginCode(await memberAuth.Verify("verifiedmember@gmail.com","email","fixture","123456"));
            s.CreateOwnPassword("MemberOwn-84!");
            check(s.Login("verifiedmember@gmail.com","MemberOwn-84!"),"Kodla doğrulanan üye kendine ait FitTeam şifresi belirleyebilir");
            Reject(()=>s.CreateOwnPassword("ReplaceWithoutOld-84!"),"Mevcut şifrenin yerine eski şifreyi doğrulamadan yeni şifre yazılamaz");
            await President();s.AdminSetPassword(staff.Id,"ResetAgain-82!");check(s.Login("staff","ResetAgain-82!")&&!staff.MustChangePassword,"Yetkili şifreyi sıfırlar; kişi ilk sayfaya kendi şifresiyle girebilir");
            await President();s.AdminSetUserActive(staff.Id,false);check(!s.Login("staff","ResetAgain-82!"),"Çıkarılan kişinin şifreli girişi kapanır");
            var staffAuth=new CodeAuthClient(new Uri("http://127.0.0.1:12345/"),new HttpClient(new CodeFixture("staff@gmail.com",false)));
            var staffIdentity=await staffAuth.Verify("staff@gmail.com","email","fixture","123456");Reject(()=>s.LoginCode(staffIdentity),"Çıkarılan kişi doğru Gmail koduyla da panele giremez");
            await President();s.AdminSetUserActive(staff.Id,true);check(s.Login("staff","ResetAgain-82!"),"Yetkili kişiyi verileriyle yeniden etkinleştirir");
            await President();var code=s.AdminIssueClubCode(club.Id);check(code.Length==6&&code.All(char.IsDigit),"Kulüp giriş kodu altı rakamdan oluşur");
            check(s.LoginClubCode("yildizfk",code)&&s.Current!.Id==clubUser.Id,"Kulüp kendi altı haneli koduyla girer");
            check(s.ManagedUsers().Any(u=>u.Id==player.Id)&&!s.ManagedUsers().Any(u=>u.Id==other.Id),"Kulüp kodu diğer kulübün oyuncularını açmaz");
            check(!s.LoginClubCode("yildizfk",code),"Kulüp kodu ikinci kez kullanılamaz");
            await President();code=s.AdminIssueClubCode(club.Id);clock.Advance(TimeSpan.FromMinutes(2));check(!s.LoginClubCode("yildizfk",code),"Kulüp kodu tam iki dakikada geçersiz olur");
            await President();code=s.AdminIssueClubCode(club.Id);for(int i=0;i<5;i++)s.LoginClubCode("yildizfk",code=="000000"?"111111":"000000");check(!s.LoginClubCode("yildizfk",code),"Beş yanlış kulüp kodundan sonra doğru kod da reddedilir");
            await President();code=s.AdminIssueClubCode(club.Id);s.AdminSetClubActive(club.Id,false);
            check(!s.LoginClubCode("yildizfk",code)&&!s.Login("yildizfk","Temporary-84!")&&s.Data.Users.Any(u=>u.Id==player.Id),"Kulübü çıkarmak girişlerini kapatır ve oyuncu kayıtlarını korur");
            var restored=new FitService(new DataStore(path),clock);check(!restored.Data.Teams.First(t=>t.Id==club.Id).IsActive&&!restored.Login("yildizfk","Temporary-84!"),"Çıkarılan kulüp yeniden açılışta kapalı kalır");
            await President();s.AdminSetClubActive(club.Id,true);check(s.Login("yildizfk","Temporary-84!"),"Çıkarılan kulüp kayıtları korunarak yeniden eklenir");
            await President();var basketball=s.AdminAddClub("Basket Kulübü","basketclub","basketclub@gmail.com","Temporary-84!","Karma","Basketbol");
            var basketballCoach=s.Data.Users.First(u=>u.Id==basketball.CoachId);check(basketball.SportBranch=="Basketbol"&&FitService.SportOf(basketballCoach)=="Basketbol","Basketbol kulübü ve antrenör hesabı kendi branşıyla oluşturulur");
            var volleyball=s.AdminAddClub("Voleybol Kulübü","volleyclub","volleyclub@gmail.com","Temporary-84!","Karma","Voleybol");check(volleyball.SportBranch=="Voleybol","Voleybol kulübü ayrı branşla oluşturulur");
            s.SetCoachHours(staff,[new CoachHours{Day=DayOfWeek.Monday,Start=new TimeOnly(9,0),End=new TimeOnly(12,0)}]);check(staff.WorkHours.Count==1,"Yetkili hocanın çalışma saatlerini tanımlar");
            Reject(()=>s.SetCoachHours(staff,[new CoachHours{Day=DayOfWeek.Monday,Start=new TimeOnly(12,0),End=new TimeOnly(9,0)}]),"Çalışma saati bitişi başlangıçtan önce olamaz");
            Reject(()=>s.SetCoachHours(staff,[new CoachHours{Day=DayOfWeek.Monday,Start=new TimeOnly(9,0),End=new TimeOnly(12,0)},new CoachHours{Day=DayOfWeek.Monday,Start=new TimeOnly(11,0),End=new TimeOnly(13,0)}]),"Hocanın aynı gündeki saatleri çakışamaz");
            s.Login("player","Temporary-84!");Reject(()=>s.SetCoachHours(staff,[]),"Sporcu başka hocanın çalışma saatlerini değiştiremez");
            var applicantAuth=new CodeAuthClient(new Uri("http://127.0.0.1:12345/"),new HttpClient(new CodeFixture("newapplicant@gmail.com",false)));
            s.LoginCode(await applicantAuth.Verify("newapplicant@gmail.com","email","fixture","123456"));var applicant=s.RequireCurrent();check(applicant.NeedsApproval&&!applicant.ProfileComplete,"Yeni üyenin doğrulanmış hesabı profil ve yetkili onayı bekler");
            s.UpdateProfile(new Profile{Name="Yeni Başvuran",AvailableDays=[DayOfWeek.Monday],Goal=Choices.Goals[0],Level=Choices.Levels[0]});check(applicant.ProfileComplete&&applicant.NeedsApproval,"Yeni üye bilgilerini doldurur; kendini yetkili onaylı gösteremez");
            Reject(()=>s.AdminApproveUser(applicant.Id),"Üye kendi başvurusunu onaylayamaz");
            await President();s.AdminApproveUser(applicant.Id);check(!applicant.NeedsApproval&&applicant.IsActive,"Yetkili yeni üyenin başvurusunu onaylar");
            var loaded=new DataStore(path);check(loaded.Data.Users.First(u=>u.Id==staff.Id).WorkHours.Single().Start==new TimeOnly(9,0),"Antrenör çalışma saatleri yeniden açılışta korunur");

        }
        finally{File.Delete(path);File.Delete(path+".bak");File.Delete(path+".tmp");}
    }
}
