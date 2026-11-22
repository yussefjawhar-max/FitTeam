namespace FitTeam.Core;
public sealed class MealHour
{
    public string Meal {get;set;}="";
    public TimeOnly Time {get;set;}
}
public sealed class MealRoutine
{
    public bool FoodPreferencesSet {get;set;}
    public List<string> PreferredFoods {get;set;}=[];
    public List<string> ExcludedFoods {get;set;}=[];
    public string AllergyNotes {get;set;}="";
    public bool Vegetarian {get;set;}
    public bool UsesProteinProduct {get;set;}
    public string ProductName {get;set;}="";
    public int ProductCalories {get;set;}
    public decimal ProductProtein {get;set;}
    public List<MealHour> Hours {get;set;}=[];
    public void Validate()
    {
        if(AllergyNotes.Length>1000||ExcludedFoods.Any(name=>!FoodGuide.All.Any(food=>food.Name==name)))throw new ArgumentException("Beslenme tercihlerini kontrol et; alerji notu en fazla 1000 karakter olabilir.");
        if(FoodPreferencesSet&&(PreferredFoods.Count==0||PreferredFoods.Any(id=>!FoodPreferences.Options.Any(o=>o.Id==id))||Vegetarian&&!PreferredFoods.Any(id=>id is "eggs" or "dairy" or "legumes")))throw new ArgumentException("Yemek programı için sevdiğin en az bir besin grubunu seç.");
        if(UsesProteinProduct&&(ProductName.Trim().Length is <2 or >80||ProductCalories is <1 or >1500||ProductProtein is <1 or >100||ProductCalories+10<ProductProtein*4))throw new ArgumentException("Protein ürününün etiketindeki bir porsiyonun adını, kalorisini ve protein gramını kontrol et.");
        if(Hours.Count!=4||Hours.Select(h=>h.Meal).Distinct().Count()!=4||Hours.Any(h=>!Nutrition.Meals.Contains(h.Meal)))throw new ArgumentException("Dört öğünün saatini HH:mm biçiminde gir.");
    }
    public static List<MealHour> DefaultHours(Profile profile)
    {
        var breakfast=new TimeOnly(8,0);var lunch=new TimeOnly(13,0);var dinner=new TimeOnly(19,0);var snack=new TimeOnly(16,0);
        if(profile.TrainingStart is { } start)
        {
            var end=start.ToTimeSpan().TotalMinutes+profile.TrainingHours*60;
            if(start.Hour is >=7 and <=9)breakfast=start.AddHours(-1);
            if(start.Hour is >=12 and <=14)lunch=start.AddHours(-1);
            if(end>=19*60&&end+30<24*60)dinner=TimeOnly.FromTimeSpan(TimeSpan.FromMinutes(end+30));
        }
        return [new(){Meal=Nutrition.Meals[0],Time=breakfast},new(){Meal=Nutrition.Meals[1],Time=lunch},new(){Meal=Nutrition.Meals[2],Time=dinner},new(){Meal=Nutrition.Meals[3],Time=snack}];
    }
}
public sealed record FoodPreference(string Id,string Label,string[] Foods);
public static class FoodPreferences
{
    public static readonly FoodPreference[] Options=
    [
        new("fish","Balık",["Somon","Ton balığı"]),new("meat","Kırmızı et",["Dana eti"]),
        new("poultry","Tavuk ve hindi",["Tavuk göğsü","Hindi göğsü"]),new("eggs","Yumurta",["Yumurta"]),
        new("dairy","Süt, yoğurt ve peynir",["Yoğurt","Süt","Lor peyniri","Beyaz peynir","Kefir"]),
        new("legumes","Mercimek, nohut ve fasulye",["Mercimek (pişmiş)","Nohut (pişmiş)","Fasulye (pişmiş)"])
    ];
}
public sealed partial class FitService
{
    public void SetMealRoutine(MealRoutine routine,string activity,bool specialCare)
    {
        routine.Validate();if(!Nutrition.Activities.Any(a=>a.Id==activity))throw new ArgumentException("Hareket düzeyini seç.");
        var me=RequireCurrent();me.Profile.MealRoutine=routine;me.Profile.ActivityLevel=activity;me.Profile.NutritionNeedsSpecialCare=specialCare||!string.IsNullOrWhiteSpace(routine.AllergyNotes);store.Save();
    }
}
