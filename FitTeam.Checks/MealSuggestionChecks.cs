using FitTeam.Core;
static class MealSuggestionChecks
{
    public static void Run(Action<bool,string> check)
    {
        var day=new DateOnly(2026,10,10);var p=new Profile{Name="Sporcu",Age=30,Gender="Erkek",Height=180,Weight=80,InitialWeight=80,TargetWeight=75,ActivityLevel="low"};
        var loss=MealSuggestions.Create(p,day);p.TargetWeight=85;var gain=MealSuggestions.Create(p,day);
        check(loss.Meals.Count==4&&gain.Meals.Count==4&&gain.Target>loss.Target&&gain.Meals[0].Title!=loss.Meals[0].Title,"Kilo alma ve verme farklı kahvaltı, öğle, akşam ve ara öğün önerileri üretir");
        check(Enumerable.Range(0,3).All(i=>{var menu=MealSuggestions.Create(p,day,alternative:i);return Math.Abs(menu.TotalCalories-menu.Target)<menu.Target*0.04m&&menu.Meals.All(m=>m.Portions.All(f=>f.Grams>0));}),"Üç öğün alternatifi ölçülü porsiyonlar kullanır ve toplam kalori hedefinin yüzde dört içinde kalır");
        var veg=MealSuggestions.Create(p,day,true);check(veg.Meals.SelectMany(m=>m.Portions).All(f=>f.Food.Category!="Et ve balık"),"Vejetaryen yemek tercihi et ve balık içermez");
        p.Age=17;check(MealSuggestions.Create(p,day).Meals.Count==0,"18 yaş altında otomatik yetişkin öğün programı oluşturulmaz");p.Age=30;
        check(MealSuggestions.Create(p,day,activity:"").State==NutritionState.MissingActivity&&MealSuggestions.Create(p,day,specialCare:true).Meals.Count==0,"Eksik hareket veya özel beslenme durumunda otomatik öğün reçetesi verilmez");
        var routine=new MealRoutine{FoodPreferencesSet=true,PreferredFoods=FoodPreferences.Options.Select(o=>o.Id).ToList(),UsesProteinProduct=true,ProductName="Etiketli ürün",ProductCalories=120,ProductProtein=24,Hours=MealRoutine.DefaultHours(p)};
        var supplemented=MealSuggestions.Create(p,day,routine:routine);
        check(supplemented.Meals.Count(m=>m.ProteinProduct!=null)==1&&supplemented.Target==gain.Target&&Math.Abs(supplemented.TotalCalories-supplemented.Target)<100,"Etiket porsiyonu bir kez eklenir ve kalorisi günlük hedefin içinde tutulur");
        routine.UsesProteinProduct=false;check(MealSuggestions.Create(p,day,routine:routine).Meals.All(m=>m.ProteinProduct==null),"Hayır seçimi protein ürününü yemeklerden çıkarır");
        var unselected=new MealRoutine{Hours=MealRoutine.DefaultHours(p)};check(MealSuggestions.Create(p,day,routine:unselected).State==NutritionState.MissingPreferences,"Sevilen besinler seçilmeden kişisel yemek programı oluşturulmaz");
        var selected=new MealRoutine{FoodPreferencesSet=true,PreferredFoods=["eggs","dairy","legumes"],Hours=MealRoutine.DefaultHours(p)};
        var week=MealSuggestions.Week(p,day,selected);check(week.Count==7&&week.Select(w=>w.Date).Distinct().Count()==7&&week.All(w=>w.Plan.Meals.Count==4&&w.Plan.Meals.SelectMany(m=>m.Portions).All(f=>f.Food.Category!="Et ve balık")&&Math.Abs(w.Plan.TotalCalories-w.Plan.Target)<w.Plan.Target*0.04m),"Yedi günlük dört öğün planı seçilmeyen et, tavuk ve balığı kullanmaz; porsiyonlar hedef kaloriyi karşılar");
        var male=MealSuggestions.Create(p,day,routine:selected);p.Gender="Kadın";var female=MealSuggestions.Create(p,day,routine:selected);p.Gender="Erkek";
        check(male.Target!=female.Target&&male.TotalCalories!=female.TotalCalories,"Kadın ve erkek profillerinin kişisel enerji ve porsiyon hesabı farklıdır");
        p.TrainingStart=new TimeOnly(18,0);p.TrainingHours=2;check(MealRoutine.DefaultHours(p).Single(h=>h.Meal==Nutrition.Meals[2]).Time==new TimeOnly(20,30),"Akşam dersinden sonra öğün saati önerilir ve kullanıcı saatleri değiştirebilir");
        var path=Path.Combine(Path.GetTempPath(),"FitTeam_meal_routine_"+Guid.NewGuid().ToString("N")+".json");
        try
        {
            var service=new FitService(new DataStore(path));service.Login("sporcu","Fit123!");service.SetMealRoutine(routine,"moderate",false);var restored=new FitService(new DataStore(path));restored.Login("sporcu","Fit123!");
            check(restored.RequireCurrent().Profile.MealRoutine?.Hours.Count==4&&restored.RequireCurrent().Profile.ActivityLevel=="moderate","Protein tercihi ve dört öğün saati yeniden açılışta korunur");
            var foodCount=service.Data.Foods.Count;MealSuggestions.Create(p,day);check(service.Data.Foods.Count==foodCount,"Öğün önerisi üretmek yemeği yenmiş olarak kaydetmez");
        }
        finally{foreach(var suffix in new[]{"",".bak",".tmp"})if(File.Exists(path+suffix))File.Delete(path+suffix);}
    }
}
