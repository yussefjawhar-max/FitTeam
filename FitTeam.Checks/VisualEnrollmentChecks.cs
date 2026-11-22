using FitTeam.Core;
static class VisualEnrollmentChecks
{
    public static void Run(Action<bool,string> check)
    {
        var clock=new VisualClock();var session=new SessionClock(clock);session.Start();clock.Advance(10);
        check(session.Read().Elapsed==10&&session.Read().Active==10,"Antrenman başlangıcı süre ve görsel hareket zamanını birlikte ilerletir");
        session.BeginRest(20);clock.Advance(5);session.Pause();var stopped=session.Read();clock.Advance(3600);
        check(session.Read()==stopped&&stopped.Rest==15&&stopped.Active==10,"Duraklatınca dinlenme ve hareket zamanları duvar saatinden bağımsız donar");
        session.Start();clock.Advance(15);check(session.Read().Rest==0&&session.Read().Active==10,"Devam et dinlenmeyi kaldığı yerden tamamlar");clock.Advance(8);
        check(session.Read().Active==18&&session.Read().Elapsed==38,"Dinlenme bitince görsel hareket zamanı yeniden ilerler");
        check(ExerciseLibrary.All.All(e=>ExerciseGuides.For(e.Name)!=null),"Kütüphanedeki bütün standart hareketlerin görsel ve açıklama eşlemesi vardır");
        check(ExerciseGuides.PerformedName("Sabit bisiklet",true)=="Tempolu yürüyüş"&&ExerciseGuides.PerformedName("Kalça köprüsü",true)=="Kalça köprüsü","Evde makine gerektiren hareket için açıkça alternatif gösterilir; ekipmansız hareket korunur");
        check(AddressCatalog.Provinces.Count==81&&AddressCatalog.Districts.Count==973,"İl ve ilçe listeleri çevrimdışı veri dosyasından yüklenir");
        var province=AddressCatalog.Province("istanbul")!;var district=AddressCatalog.District(province.Id,"bagcilar")!;
        check(AddressCatalog.Neighborhoods.Any(n=>n.DistrictId==district.Id&&AddressCatalog.Key(n.Name)=="mahmutbey")&&AddressCatalog.Neighborhoods.Any(n=>n.DistrictId==district.Id&&AddressCatalog.Key(n.Name)=="15 temmuz"),"Mahmutbey ve 15 Temmuz Bağcılar ilçesinin mahalle seçenekleridir");
        var address=new PostalAddress{Province="İstanbul",District="Bağcılar",Neighborhood="Mahmutbey",Street="Gazi Mustafa Kemal Caddesi",Building="١٢",Apartment="٣"};address.Validate("TR");
        check(address.Building=="12"&&address.Apartment=="3"&&address.Display.Contains("Mahmutbey"),"Adres parçaları ve Arapça numaralar ayrı alanlarda korunur");
        bool rejected=false;address.District="Mahmutbey";try{address.Validate("TR");}catch(ArgumentException){rejected=true;}check(rejected,"Mahalle adı yanlışlıkla ilçe olarak kaydedilemez");
        check(Usernames.Validate("محمد_أحمد")=="محمد_أحمد","Kullanıcı adında Arapça harfler ve alt çizgi korunur");
        rejected=false;try{Usernames.Validate("bu kullanıcı adı uzun ve boşluklu");}catch(ArgumentException){rejected=true;}check(rejected,"Kullanıcı adı 3–20 karakter ve boşluksuz olmalıdır");
        TrainingWindow.Validate(new TimeOnly(18,0),3);rejected=false;try{TrainingWindow.Validate(new TimeOnly(23,0),2);}catch(ArgumentException){rejected=true;}check(rejected,"Ders aralığı 2 veya 3 saattir; gün sınırını aşmaz");
        var application=new SignupApplication{Sport="Fitness",Gender="Erkek",Country="Türkiye",CountryCode="TR",City="İstanbul",HomeAddress="Test sokak no 1",TrainingStart=new TimeOnly(18,0)};
        rejected=false;try{application.ValidateTraining();}catch(ArgumentException){rejected=true;}check(rejected,"Yeni fitness kaydında hoca seçimi zorunludur");
    }
    private sealed class VisualClock:TimeProvider
    {
        private long ticks;
        public override long TimestampFrequency=>TimeSpan.TicksPerSecond;
        public override long GetTimestamp()=>ticks;
        public void Advance(int seconds)=>ticks+=TimeSpan.FromSeconds(seconds).Ticks;
    }
}
