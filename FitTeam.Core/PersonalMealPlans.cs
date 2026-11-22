namespace FitTeam.Core;

public sealed class PersonalMealPortion
{
    public string Food { get; set; } = "";
    public decimal Grams { get; set; }
}

public sealed class PersonalMealPlan
{
    public Guid UserId { get; set; }
    public DateOnly Date { get; set; }
    public string Meal { get; set; } = "";
    public string Title { get; set; } = "";
    public List<PersonalMealPortion> Portions { get; set; } = [];
}

public static class PersonalMealFoods
{
    public static List<FoodGuideItem> Available(MealRoutine routine) => FoodGuide.All.Where(food =>
    {
        if (routine.ExcludedFoods.Contains(food.Name) || routine.Vegetarian && food.Category == "Et ve balık") return false;
        var group = FoodPreferences.Options.FirstOrDefault(option => option.Foods.Contains(food.Name));
        return group == null || routine.PreferredFoods.Contains(group.Id);
    }).ToList();
}

public sealed partial class FitService
{
    public SuggestedDay ApplyPersonalMeals(DateOnly date, SuggestedDay suggested, MealRoutine routine)
        =>ApplyPersonalMeals(RequireCurrent(),date,suggested,routine);
    public SuggestedDay ApplyPersonalMeals(User target,DateOnly date,SuggestedDay suggested,MealRoutine routine)
    {
        EnsureAccess(target);var me=target;
        if (suggested.State != NutritionState.Ready) return suggested;
        var available = PersonalMealFoods.Available(routine).Select(food => food.Name).ToHashSet();
        var meals = suggested.Meals.Select(meal =>
        {
            var stored = Data.PersonalMeals.LastOrDefault(item => item.UserId == me.Id && item.Date == date && item.Meal == meal.Meal);
            if (stored == null || stored.Portions.Count is < 1 or > 12 || stored.Portions.Any(portion =>
                !available.Contains(portion.Food) || portion.Grams is < 1 or > 2000)) return meal;
            return new SuggestedMeal(meal.Meal, stored.Title, stored.Portions.Select(portion =>
                new SuggestedPortion(FoodGuide.All.First(food => food.Name == portion.Food), portion.Grams)).ToList())
                { ProteinProduct = meal.ProteinProduct, Edited = true };
        }).ToList();
        return suggested with { Meals = meals };
    }

    public void SavePersonalMeal(DateOnly date, string meal, string title, IEnumerable<PersonalMealPortion> portions,
        MealRoutine routine, string activity, bool specialCare)
    {
        var me = RequireCurrent();
        var today = DateOnly.FromDateTime((clock ?? TimeProvider.System).GetLocalNow().DateTime);
        if (date < today || date > today.AddDays(6) || !Nutrition.Meals.Contains(meal))
            throw new ArgumentException("Yemek programı için bugün veya önümüzdeki altı günden birini seç.");
        if (string.IsNullOrWhiteSpace(title) || title.Trim().Length is <2 or >120)
            throw new ArgumentException("Öğün adını 2–120 karakter olarak gir.");
        routine.Validate();
        if (MealSuggestions.Create(me.Profile, date, activity: activity, specialCare: specialCare, routine: routine).State != NutritionState.Ready)
            throw new ArgumentException("Öğünü düzenlemek için profilini ve beslenme tercihlerini tamamla.");
        var items = portions.Select(portion => new PersonalMealPortion { Food = portion.Food, Grams = portion.Grams }).ToList();
        var available = PersonalMealFoods.Available(routine).Select(food => food.Name).ToHashSet();
        if (items.Count is < 1 or > 12 || items.Any(portion => !available.Contains(portion.Food) || portion.Grams is < 1 or > 2000))
            throw new ArgumentException("Öğüne 1–12 yiyecek ekle; miktarları 1–2000 gram arasında gir ve beslenme tercihlerine uygun besinleri seç.");
        var calories = items.Sum(portion => FoodGuide.All.First(food => food.Name == portion.Food).Calories * portion.Grams / 100)
            + (meal == Nutrition.Meals[3] && routine.UsesProteinProduct ? routine.ProductCalories : 0);
        if (calories > 5000) throw new ArgumentException("Bu öğünün toplamı kayıt sınırını aşıyor. Yiyecek miktarlarını kontrol et.");
        me.Profile.MealRoutine = routine;
        me.Profile.ActivityLevel = activity;
        me.Profile.NutritionNeedsSpecialCare = specialCare||!string.IsNullOrWhiteSpace(routine.AllergyNotes);
        Data.PersonalMeals.RemoveAll(item => item.UserId == me.Id && item.Date == date && item.Meal == meal);
        Data.PersonalMeals.Add(new PersonalMealPlan { UserId = me.Id, Date = date, Meal = meal, Title = title.Trim(), Portions = items });
        Save();
    }

    public void RestoreMealSuggestion(DateOnly date, string meal)
    {
        var me = RequireCurrent();
        if (Data.PersonalMeals.RemoveAll(item => item.UserId == me.Id && item.Date == date && item.Meal == meal) > 0) Save();
    }
}
