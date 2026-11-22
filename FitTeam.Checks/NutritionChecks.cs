using FitTeam.Core;
static class NutritionChecks
{
    public static void Run(Action<bool,string> check)
    {
        var profile = new Profile { Age=30,Gender="Erkek",Height=180,InitialWeight=80,Weight=80,TargetWeight=75,ActivityLevel="low" };
        var estimate = Nutrition.Calculate(profile);
        check(estimate.State==NutritionState.Ready&&estimate.Resting==1780&&estimate.Maintenance==2140&&estimate.Target==1640&&estimate.Adjustment==-500,"Kalori hesabı boy kilo yaş ve seçilmiş harekete göre koruma ve kilo verme hedefi üretir");
        profile.TargetWeight=85;estimate=Nutrition.Calculate(profile);
        check(estimate.Target==2440&&estimate.Adjustment==300&&Nutrition.MealBudget(estimate.Target).Sum(m=>m.Calories)==estimate.Target,"Kilo alma hedefi ve öğün bütçesi günlük kalori toplamını korur");
        profile.TargetWeight=80;check(Nutrition.Calculate(profile).Adjustment==0,"Kilo koruma hedefi açık veya fazlalık eklemez");
        check(Nutrition.Calculate(profile,"",false).State==NutritionState.MissingActivity&&Nutrition.Calculate(profile,"low",true).State==NutritionState.NeedsSpecialCare,"Hareket bilgisi olmadan veya özel beslenme durumunda otomatik hedef verilmez");
        profile.Age=17;check(Nutrition.Calculate(profile).State==NutritionState.NeedsSpecialCare,"18 yaş altı için otomatik kalori kısıtlaması yapılmaz");
        profile.Age=30;profile.TargetWeight=40;check(Nutrition.Calculate(profile).State==NutritionState.TargetNeedsReview,"Düşük kilo hedefine otomatik kalori kısıtlaması verilmez");
        profile.Gender="Belirtmek istemiyorum";check(Nutrition.Calculate(profile).State==NutritionState.MissingProfile,"Cinsiyet için formül katsayısı sessizce varsayılmaz");
        var path=Path.Combine(Path.GetTempPath(),"FitTeam_food_"+Guid.NewGuid().ToString("N")+".json");
        try
        {
            var store=new DataStore(path);var service=new FitService(store);service.Login("sporcu","Fit123!");var me=service.RequireCurrent();var today=DateOnly.FromDateTime(DateTime.Today);
            service.SetNutritionSettings("moderate",false);service.AddFood(today,Nutrition.Meals[0],"Kayıtlı kahvaltı",125,2);
            check(service.Foods(me,today).Single().Calories==250&&service.Foods(me,today.AddDays(-1)).Count==0,"Yemek porsiyonları çarpılır ve günlük kayıtlar karışmaz");
            check(Nutrition.Remaining(new CalorieEstimate(NutritionState.Ready,Target:200),service.Foods(me,today))==-50,"Kalori hedefinin aşılması negatif kalan miktarla saklanır");
            bool rejected=false;try{service.AddFood(today.AddDays(1),Nutrition.Meals[0],"Yemek",100,1);}catch(ArgumentException){rejected=true;}
            check(rejected,"Gelecek güne gerçek yemek kaydı eklenmez");
            var other=store.Data.Users.First(u=>u.Username=="oyuncu");rejected=false;try{service.Foods(other,today);}catch(InvalidOperationException){rejected=true;}
            check(rejected,"Üye başka kişinin beslenme kayıtlarını göremez");
            var id=service.Foods(me,today).Single().Id;service.Login("uye","Fit123!");rejected=false;try{service.RemoveFood(id);}catch(InvalidOperationException){rejected=true;}
            check(rejected&&store.Data.Foods.Count==1,"Üye başka kişinin yemek kaydını kaldıramaz");
            var restored=new DataStore(path);check(restored.Data.Foods.Single().Calories==250&&restored.Data.Users.First(u=>u.Id==me.Id).Profile.ActivityLevel=="moderate","Yemek ve hareket bilgileri yeniden açılışta korunur");
            service.Login("sporcu","Fit123!");
            var edited=new Profile{Name=me.Profile.Name,Age=me.Profile.Age,Height=me.Profile.Height,Weight=me.Profile.Weight,TargetWeight=me.Profile.Weight+3,AvailableDays=me.Profile.AvailableDays.ToList()};
            service.UpdateProfile(edited);check(me.Profile.ActivityLevel=="moderate"&&me.Profile.GoalStartWeight==edited.Weight&&me.Profile.GoalStarted==today,"Profil güncellenirken hareket tercihi korunur ve yeni hedefin başlangıcı ayrılır");
            service.RemoveFood(id);check(service.Foods(me,today).Count==0,"Kendi hatalı yemek kaydı kaldırılabilir");
        }
        finally{foreach(var suffix in new[]{"",".bak",".tmp"})if(File.Exists(path+suffix))File.Delete(path+suffix);}
    }
}
