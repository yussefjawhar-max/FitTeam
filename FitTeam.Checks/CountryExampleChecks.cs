using FitTeam.Core;
static class CountryExampleChecks
{
    public static void Run(Action<bool,string> check)
    {
        var searchStudent=new User{Username="ogrenci0101",Profile=new Profile{Name="İrem Şahin"}};
        check(StudentSearch.Find([searchStudent],"IREM SAHIN").Single()==searchStudent&&StudentSearch.Find([searchStudent],"  şahin irem  ").Single()==searchStudent,"Öğrenci araması Türkçe harfler, büyük küçük harf ve ad soyad sırasını kabul eder");
        var arabicStudent=new User{Username="ogrenci0102",Profile=new Profile{Name="عُمَر خالد"}};
        check(StudentSearch.Find([searchStudent,arabicStudent],"عمر").Single()==arabicStudent&&StudentSearch.Find([searchStudent],"0101").Single()==searchStudent&&StudentSearch.Find([searchStudent],"bilinmeyen").Count==0&&StudentSearch.Find([searchStudent],"   ").Count==0,"Öğrenci araması Arapça ad ve kullanıcı adını bulur; boş veya eşleşmeyen sorguda sonuç üretmez");
        AppLanguage.Use("tr");var t=CountryCatalog.All.Where(c=>CountryCatalog.Matches(c,"T")).Select(c=>c.Code).ToList();var s=CountryCatalog.All.Where(c=>CountryCatalog.Matches(c,"S")).Select(c=>c.Code).ToList();check(t.Contains("TR")&&t.Contains("TM"),"Ülke aramasında T Türkiye ve Türkmenistan seçeneklerini gösterir");check(s.Contains("SY")&&s.Contains("SA"),"Ülke aramasında S Suriye ve Suudi Arabistan seçeneklerini gösterir");
        check(CountryCatalog.Resolve("Türkiye")?.Code=="TR"&&CountryCatalog.Resolve("Turkey")?.Code=="TR"&&CountryCatalog.Resolve("سوريا")?.Code=="SY","Ülke adları üç dilde aynı ülke koduna bağlanır");
        var data=new AppData();var result=RequestedExamples.Create(data,new DateOnly(2026,10,9));
        check(result.Clubs.Count==30&&new[]{"Futbol","Basketbol","Voleybol"}.All(sport=>result.Clubs.Count(c=>c.Sport==sport&&c.Gender=="Erkek")==5&&result.Clubs.Count(c=>c.Sport==sport&&c.Gender=="Kadın")==5),"Üç branşta beş erkek ve beş kız kulübü, toplam otuz kulüp oluşturulur");
        check(result.Clubs.All(c=>c.Password.Length==8&&c.Password.All(char.IsDigit))&&result.Clubs.Select(c=>c.Password).Distinct().Count()==30,"Otuz kulübün sekiz haneli şifreleri farklıdır");
        check(result.FitnessMembers.Count==50&&data.Users.Count(u=>u.Role==Choices.Roles[0]&&u.Profile.CountryCode=="TR")==45&&data.Users.Count(u=>u.Role==Choices.Roles[0]&&u.Profile.CountryCode=="SY")==5,"Elli fitness üyesi yüzde doksan Türkiye, yüzde on Suriye olarak oluşturulur");
        check(data.Users.All(u=>u.IsExample&&u.ExampleBatch==RequestedExamples.Batch)&&data.Teams.All(t=>t.ExampleBatch==RequestedExamples.Batch),"Oluşturulan bütün kulüp ve kişi kayıtları örnek veri olarak işaretlenir");
        check(data.Users.Where(u=>u.Role==Choices.Roles[0]).All(u=>u.Profile.Weight<u.Profile.InitialWeight&&data.Weights.Count(w=>w.UserId==u.Id)==5),"Örnek üyelerde beş haftalık kilo kaydı ve kilo değişimi bulunur");
        var standings=data.Users.Where(u=>u.Role==Choices.Roles[0]).Select(u=>ParticipationAnalytics.Standing(data,u,new DateOnly(2026,10,9))).ToList();var countries=ParticipationAnalytics.Countries(standings);check(countries.Single(c=>c.Code=="TR").Share==90&&countries.Single(c=>c.Code=="SY").Share==10,"Ülke yüzdeleri kayıt sayısından doğru hesaplanır");
        check(standings.All(s=>s.Planned>0&&s.Completed<=s.Planned&&s.Compliance>=0&&s.Compliance<=100)&&standings.Select(s=>s.Compliance).Distinct().Count()>1,"Takvim uyumu geçmiş antrenmanlardan farklı ve geçerli oranlarla hesaplanır");
        check(result.FitnessMembers.All(a=>Passwords.Verify(data.Users.Single(u=>u.Username==a.Username),a.Password))&&result.Clubs.All(c=>Passwords.Verify(data.Users.Single(u=>u.Username==c.CoachUsername),c.CoachPassword)),"Üye ve antrenör şifreleri kendi hesaplarının tuzlu özetleriyle eşleşir");
        var students=RequestedClubStudents.Create(data,new DateOnly(2026,10,9));check(students.Count==300&&data.Teams.All(t=>data.Users.Count(u=>u.TeamId==t.Id)==10),"Otuz kulübün her birine on ayrı öğrenci hesabı bağlanır");
        check(data.Teams.All(t=>data.Users.Where(u=>u.TeamId==t.Id).All(u=>u.Profile.Gender==t.Gender))&&data.Users.Count(u=>u.ExampleBatch==RequestedClubStudents.Batch&&u.Profile.CountryCode=="SY")==30,"Kulüp öğrencileri takım cinsiyetine uyar; her kulüpte dokuz Türkiye bir Suriye kaydı olur");
        check(students.All(s=>Passwords.Verify(data.Users.Single(u=>u.Username==s.Username),s.Password)),"Kulüp öğrencileri kendi ayrı kullanıcı adı ve şifresiyle giriş yapar");
        int original=data.Users.Count;bool duplicate=false;try{RequestedExamples.Create(data,new DateOnly(2026,10,9));}catch(InvalidOperationException){duplicate=true;}check(duplicate&&data.Users.Count==original,"Örnek veriler ikinci kez yanlışlıkla çoğaltılmaz");
    }
}
