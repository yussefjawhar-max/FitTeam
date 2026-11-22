using FitTeam.Core;
static class RequestedDistributionChecks
{
    public static void Run(Action<bool,string> check)
    {
        var today=new DateOnly(2026,10,10);var data=new AppData();
        for(int i=0;i<100;i++)data.Users.Add(new User{Username="ogrenci"+i.ToString("D3"),IsExample=true,ExampleBatch=RequestedClubStudents.Batch,Profile=new(){Name="Öğrenci "+i,CountryCode="TR",City="İstanbul",HomeAddress="Yenişehir Mahallesi, Spor Sokak No: 1, Daire 2",Age=25,Height=175,Weight=69,InitialWeight=69,TargetWeight=69,Goal="Kondisyon geliştirmek"}});
        var real=new User{Profile=new(){CountryCode="TR",HomeAddress="Kişinin kendi adresi",Weight=69,TargetWeight=69}};data.Users.Add(real);
        var edited=data.Users[0];edited.Profile.HomeAddress="Kullanıcının düzelttiği ev adresi";edited.Profile.GoalStarted=today.AddDays(-1);edited.Profile.TargetWeight=60;
        int redistributed=RequestedAddressDistribution.Apply(data);var addresses=data.Users.Where(u=>u.Profile.Address!=null).Select(u=>u.Profile.Address!).ToList();
        check(redistributed==99&&addresses.Select(a=>a.District).Distinct().Count()>=9&&addresses.Any(a=>a.District=="Sarıyer")&&addresses.Any(a=>a.District=="Güngören")&&addresses.Any(a=>a.District=="Zeytinburnu")&&addresses.Any(a=>a.Neighborhood=="Mahmutbey"),"Kurgu öğrenci adresleri Bağcılar, Sarıyer, Merter çevresi, Zeytinburnu ve farklı şehirlerde dağılır");
        check(addresses.All(a=>{var p=AddressCatalog.Province(a.Province)!;var d=AddressCatalog.District(p.Id,a.District);return d!=null&&AddressCatalog.Neighborhoods.Any(n=>n.DistrictId==d.Id&&n.Name==a.Neighborhood);})&&addresses.Select(a=>a.Display).Distinct().Count()==addresses.Count,"İl, ilçe ve mahalle ilişkileri katalogla tutarlıdır; açık adresler birbirinin aynısı değildir");
        check(edited.Profile.HomeAddress=="Kullanıcının düzelttiği ev adresi"&&real.Profile.HomeAddress=="Kişinin kendi adresi"&&RequestedAddressDistribution.Apply(data)==0,"Kişinin düzenlediği ve gerçek kayıtlardaki adresler korunur; tekrar açılış adresleri değiştirmez");
        int goalChanges=RequestedGoalDistribution.Apply(data,today);var generated=data.Users.Where(u=>u.IsExample&&u.Id!=edited.Id).ToList();
        check(goalChanges==99&&generated.All(u=>u.Profile.TargetWeight!=u.Profile.Weight)&&generated.Any(u=>u.Profile.TargetWeight<u.Profile.Weight)&&generated.Any(u=>u.Profile.TargetWeight>u.Profile.Weight)&&generated.Select(u=>u.Profile.Goal).Distinct().Count()==4,"Kurgu kişilerde kilo verme, alma, kas ve kilo verip kas hedefleri çeşitlenir; hedef kilo mevcut kiloyla aynı değildir");
        check(edited.Profile.TargetWeight==60&&real.Profile.TargetWeight==69&&generated.All(u=>u.Profile.GoalStarted==today&&u.Profile.GoalStartWeight==69)&&RequestedGoalDistribution.Apply(data,today)==0,"Kişinin kendi hedefi korunur; yenilenen kurgu hedefleri yeni başlangıç tarihiyle bir defa uygulanır");
    }
}
