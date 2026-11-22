namespace FitTeam.Core;

public sealed class MealIntake
{
    public Guid UserId {get;set;}
    public DateOnly Date {get;set;}
    public string Meal {get;set;}="";
    public string Title {get;set;}="";
    public string Status {get;set;}="";
    public decimal Fraction {get;set;}
    public Guid? FoodEntryId {get;set;}
}
public sealed record ShoppingLine(string Name,decimal Quantity,string Unit);
public static class MealShopping
{
    public static List<ShoppingLine> Create(IEnumerable<SuggestedDay> days)
    {
        var plans=days.ToList();if(plans.Any(day=>day.State!=NutritionState.Ready))return [];
        var lines=plans.SelectMany(day=>day.Meals).SelectMany(meal=>meal.Portions).GroupBy(portion=>portion.Food.Name)
            .Select(group=>new ShoppingLine(group.Key,group.Sum(portion=>portion.Grams),"g")).ToList();
        lines.AddRange(plans.SelectMany(day=>day.Meals).Where(meal=>meal.ProteinProduct!=null).GroupBy(meal=>meal.ProteinProduct!.ProductName)
            .Select(group=>new ShoppingLine(group.Key,group.Count(),"etiket porsiyonu")));
        return lines.OrderBy(line=>line.Name).ToList();
    }
}
public sealed partial class FitService
{
    public static readonly string[] MealIntakeChoices=["Tamamını yedim","Yarısını yedim","Çeyreğini yedim","Öğünü atladım"];
    public List<MealIntake> MealIntakes(User target,DateOnly date){EnsureAccess(target);return Data.MealIntakes.Where(intake=>intake.UserId==target.Id&&intake.Date==date).ToList();}
    public void RecordMealIntake(DateOnly date,SuggestedMeal meal,string status)
    {
        var me=RequireCurrent();var today=DateOnly.FromDateTime((clock??TimeProvider.System).GetLocalNow().DateTime);
        if(date>today||!Nutrition.Meals.Contains(meal.Meal)||!MealIntakeChoices.Contains(status)||meal.Title.Trim().Length is <2 or >120||
            meal.Calories is <1 or >5000||meal.Portions.Any(portion=>!FoodGuide.All.Contains(portion.Food)||portion.Grams is <1 or >2000))
            throw new ArgumentException("Öğünü, tarihini ve yediğin miktarı kontrol et.");
        var fraction=status switch{"Tamamını yedim"=>1m,"Yarısını yedim"=>0.5m,"Çeyreğini yedim"=>0.25m,_=>0m};
        var old=Data.MealIntakes.Where(intake=>intake.UserId==me.Id&&intake.Date==date&&intake.Meal==meal.Meal).ToList();
        foreach(var previous in old)if(previous.FoodEntryId is Guid id)Data.Foods.RemoveAll(food=>food.Id==id&&food.UserId==me.Id);
        Data.MealIntakes.RemoveAll(intake=>intake.UserId==me.Id&&intake.Date==date&&intake.Meal==meal.Meal);
        FoodEntry? entry=null;if(fraction>0){entry=new FoodEntry{UserId=me.Id,Date=date,Meal=meal.Meal,Name=meal.Title,CaloriesPerPortion=meal.Calories,Portions=fraction};Data.Foods.Add(entry);}
        Data.MealIntakes.Add(new(){UserId=me.Id,Date=date,Meal=meal.Meal,Title=meal.Title,Status=status,Fraction=fraction,FoodEntryId=entry?.Id});Save();
    }
    public void UndoMealIntake(DateOnly date,string meal)
    {
        var me=RequireCurrent();foreach(var intake in Data.MealIntakes.Where(intake=>intake.UserId==me.Id&&intake.Date==date&&intake.Meal==meal).ToList())
            if(intake.FoodEntryId is Guid id)Data.Foods.RemoveAll(food=>food.Id==id&&food.UserId==me.Id);
        Data.MealIntakes.RemoveAll(intake=>intake.UserId==me.Id&&intake.Date==date&&intake.Meal==meal);Save();
    }
}
