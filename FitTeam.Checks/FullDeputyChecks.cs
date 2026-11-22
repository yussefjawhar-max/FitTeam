using FitTeam.Core;
static class FullDeputyChecks
{
    public static void Run(Action<bool,string> check)
    {
        var data=DataStore.Preview().Data;var records=new List<DeputySeedRecord>();
        for(int i=1;i<=4;i++){var user=new User();Passwords.Set(user,"HelperFixture-0"+i+"!");records.Add(new(){Id=user.Id,Username="helperfixture0"+i,Name="Yardımcı "+i,Salt=user.Salt,PasswordHash=user.PasswordHash});}
        check(PrivateDeputySetup.Apply(data,records)==4&&PrivateDeputySetup.Apply(data,records)==0,"Dört özel yardımcı hesabı eklenir ve tekrar içe aktarım çoğaltmaz");
        var service=new FitService(new DataStore(data));service.Login(records[0].Username,"HelperFixture-01!");
        check(service.HasFullAdministrationView&&service.HasAdministrationAccess&&!service.IsPresident&&!service.RequireCurrent().ProfileComplete,"Yeni yardımcının bütün yönetim görünümü açılır; başkan kimliği taklit edilmez");
        service.CompleteDeputyProfile("Ali Yardımcı",27,"Erkek","helperfixture@gmail.com","+905551234890","İstanbul");
        check(service.RequireCurrent().ProfileComplete&&service.RequireCurrent().Username==records[0].Username&&service.HasFullAdministrationView,"Yardımcı kendi bilgilerini doldururken kullanıcı adı ve yetkisi korunur");
        service.Login("sporcu","Fit123!");bool rejected=false;try{service.CompleteDeputyProfile("Sahte",25,"Erkek","","","");}catch(InvalidOperationException){rejected=true;}
        check(rejected&&!service.HasFullAdministrationView,"Normal öğrenci yardımcı profili veya tam yönetim erişimi açamaz");
        var path=Path.Combine(Path.GetTempPath(),"FitTeam_preview_guard_"+Guid.NewGuid()+".json");
        try
        {
            var actual=new FitService(new DataStore(path));bool blocked=false;
            try{actual.LoginPreviewAdministrator();}catch(InvalidOperationException){blocked=true;}
            check(blocked,"Örnek yetkili girişi gerçek kayıtlı veri dosyasında kullanılamaz");
        }
        finally{foreach(var suffix in new[]{"",".bak",".tmp"})File.Delete(path+suffix);}
    }
}
