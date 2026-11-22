using FitTeam.Core;
static class WeightGoalChecks
{
    public static void Run(Action<bool,string> check)
    {
        var today = new DateOnly(2026,10,9);
        WeightEntry Entry(int ago, decimal value) => new() { Date=today.AddDays(-ago),Weight=value };
        var loss = new Profile { Age=25,InitialWeight=90,Weight=88,TargetWeight=84 };
        var estimate = WeightGoalForecast.Calculate(loss,[Entry(14,90),Entry(7,89),Entry(0,88)],today);
        check(estimate.State==WeightGoalState.Estimated&&estimate.DaysRemaining==28&&estimate.TargetDate==today.AddDays(28)&&estimate.WeeklyChange==-1&&estimate.Progress>33&&estimate.Progress<34,"Kilo verme hedefi kaydedilmiş hızdan kalan gün ve tarihi hesaplar");
        var gain = new Profile { Age=25,InitialWeight=60,Weight=61,TargetWeight=63 };
        estimate = WeightGoalForecast.Calculate(gain,[Entry(14,60),Entry(7,60.5m),Entry(0,61)],today);
        check(estimate.State==WeightGoalState.Estimated&&estimate.DaysRemaining==28&&estimate.WeeklyChange==0.5m,"Kilo alma hedefi pozitif değişimden tarih hesaplar");
        check(WeightGoalForecast.Calculate(loss,[Entry(0,88)],today).State==WeightGoalState.MoreRecordsNeeded&&WeightGoalForecast.Calculate(loss,[Entry(1,89),Entry(0,88)],today).TargetDate==null,"Tek kayıt ve bir günlük tartı farkı tahmini tarih üretmez");
        check(WeightGoalForecast.Calculate(loss,[Entry(7,88),Entry(0,88)],today).State==WeightGoalState.Stable&&WeightGoalForecast.Calculate(loss,[Entry(7,87),Entry(0,88)],today).State==WeightGoalState.MovingAway,"Sabit veya hedefe ters değişim yanıltıcı tarih üretmez");
        check(WeightGoalForecast.Calculate(loss,[Entry(35,90),Entry(20,88)],today).State==WeightGoalState.OldRecords&&WeightGoalForecast.Calculate(loss,[Entry(7,88.1m),Entry(0,88)],today).State==WeightGoalState.Estimated,"Eski kayıtlar yenilenmeyi ister; güncel kayıtlar kullanılabilir");
        check(WeightGoalForecast.Calculate(loss,[Entry(7,85),Entry(0,83)],today).State==WeightGoalState.Reached&&WeightGoalForecast.Calculate(gain,[Entry(7,62),Entry(0,64)],today).State==WeightGoalState.Reached,"Hem kilo alma hem verme hedefinin geçilmesi hedefe ulaşıldı olarak gösterilir");
        estimate=WeightGoalForecast.Calculate(loss,[Entry(7,90),Entry(7,89),Entry(0,88),Entry(-7,80)],today);
        check(estimate.DaysRemaining==28,"Aynı günün son değeri alınır ve gelecek tarihli tartılar tahmine katılmaz");
        check(WeightGoalForecast.Calculate(new Profile{Age=17,InitialWeight=90,Weight=88,TargetWeight=84},[Entry(7,89),Entry(0,88)],today).State==WeightGoalState.Under18,"Reşit olmayan hesaplara kilo hedef tarihi üretilmez");
        check(WeightGoalForecast.Calculate(new Profile{Age=25,InitialWeight=90,Weight=89.94m,TargetWeight=70},[Entry(7,90),Entry(0,89.94m)],today).State==WeightGoalState.LongHorizon,"Kısa kayıtlardan yıllar sonraya kesin tarih üretilmez");
        estimate=WeightGoalForecast.Calculate(new Profile{Age=25,InitialWeight=90,GoalStartWeight=80,GoalStarted=today.AddDays(-14),Weight=81,TargetWeight=83},[Entry(21,90),Entry(14,80),Entry(7,80.5m),Entry(0,81)],today);
        check(estimate.State==WeightGoalState.Estimated&&estimate.DaysRemaining==28,"Kilo verme sonrası yeni kilo alma hedefi eski başlangıçla tamamlandı sanılmaz");
    }
}
