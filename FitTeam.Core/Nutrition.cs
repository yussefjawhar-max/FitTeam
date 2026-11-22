namespace FitTeam.Core;

public sealed record ActivityOption(string Id, string Label, decimal Factor);
public enum NutritionState { Ready, MissingActivity, MissingProfile, NeedsSpecialCare, TargetNeedsReview, MissingPreferences }
public sealed record CalorieEstimate(NutritionState State, int Resting = 0, int Maintenance = 0, int Target = 0, int Adjustment = 0);
public static class Nutrition
{
    public static readonly ActivityOption[] Activities =
    [
        new("low", "Az hareketli • çoğunlukla oturarak", 1.2m),
        new("light", "Hafif hareketli • haftada 1–3 gün", 1.375m),
        new("moderate", "Orta hareketli • haftada 3–5 gün", 1.55m),
        new("high", "Çok hareketli • haftada 6–7 gün", 1.725m)
    ];
    public static readonly string[] Meals = ["Kahvaltı", "Öğle yemeği", "Akşam yemeği", "Ara öğün"];
    public static CalorieEstimate Calculate(Profile profile, string? activityId = null, bool? specialCare = null)
    {
        if (profile.Age < 18 || (specialCare ?? profile.NutritionNeedsSpecialCare)) return new(NutritionState.NeedsSpecialCare);
        if (profile.Age > 90 || profile.Height is < 100 or > 230 || profile.Weight is < 30 or > 300
            || profile.TargetWeight is < 30 or > 300 || profile.Gender is not ("Kadın" or "Kız" or "Erkek"))
            return new(NutritionState.MissingProfile);
        var activity = Activities.FirstOrDefault(a => a.Id == (activityId ?? profile.ActivityLevel));
        if (activity == null) return new(NutritionState.MissingActivity);
        bool female = profile.Gender is "Kadın" or "Kız";
        decimal resting = 10 * profile.Weight + 6.25m * profile.Height - 5 * profile.Age + (female ? -161 : 5);
        int maintenance = (int)(Math.Round(resting * activity.Factor / 10) * 10);
        int minimum = female ? 1200 : 1500;
        if (maintenance < minimum) return new(NutritionState.TargetNeedsReview);
        decimal targetBmi = profile.TargetWeight / ((profile.Height / 100m) * (profile.Height / 100m));
        if (profile.TargetWeight < profile.Weight && targetBmi < 18.5m) return new(NutritionState.TargetNeedsReview);
        decimal baseline = profile.GoalStartWeight ?? profile.InitialWeight;
        bool reached = profile.TargetWeight > baseline ? profile.Weight >= profile.TargetWeight
            : profile.TargetWeight < baseline ? profile.Weight <= profile.TargetWeight : profile.Weight == profile.TargetWeight;
        int target = reached ? maintenance : profile.TargetWeight > profile.Weight ? maintenance + 300
            : profile.TargetWeight < profile.Weight ? Math.Max(minimum, maintenance - 500) : maintenance;
        if (target > maintenance && profile.TargetWeight < profile.Weight) return new(NutritionState.TargetNeedsReview);
        return new(NutritionState.Ready, (int)Math.Round(resting), maintenance, target, target - maintenance);
    }
    public static decimal Consumed(IEnumerable<FoodEntry> foods) => foods.Sum(f => f.Calories);
    public static decimal Remaining(CalorieEstimate estimate, IEnumerable<FoodEntry> foods)
        => estimate.State == NutritionState.Ready ? estimate.Target - Consumed(foods) : 0;
    public static List<(string Meal, int Calories)> MealBudget(int target)
    {
        int breakfast = (int)Math.Round(target * 0.25m);
        int lunch = (int)Math.Round(target * 0.35m);
        int dinner = (int)Math.Round(target * 0.30m);
        return [(Meals[0], breakfast), (Meals[1], lunch), (Meals[2], dinner), (Meals[3], target - breakfast - lunch - dinner)];
    }
}

public sealed partial class FitService
{
    public void SetNutritionSettings(string activityId, bool specialCare)
    {
        var user = RequireCurrent();
        if (!Nutrition.Activities.Any(a => a.Id == activityId)) throw new ArgumentException("Hareket düzeyini seç.");
        user.Profile.ActivityLevel = activityId; user.Profile.NutritionNeedsSpecialCare = specialCare; store.Save();
    }
    public List<FoodEntry> Foods(User user, DateOnly date)
    {
        EnsureAccess(user); return Data.Foods.Where(f => f.UserId == user.Id && f.Date == date).ToList();
    }
    public void AddFood(DateOnly date, string meal, string name, decimal caloriesPerPortion, decimal portions)
    {
        var user = RequireCurrent();
        if (date > DateOnly.FromDateTime(DateTime.Today)) throw new ArgumentException("Gelecek güne yemek kaydedilemez.");
        if (!Nutrition.Meals.Contains(meal) || name.Trim().Length is < 2 or > 120 || caloriesPerPortion is <= 0 or > 5000
            || portions is <= 0 or > 20 || caloriesPerPortion * portions > 10000)
            throw new ArgumentException("Yiyecek adı, porsiyon ve kalori bilgilerini kontrol et.");
        Data.Foods.Add(new FoodEntry { UserId = user.Id, Date = date, Meal = meal, Name = name.Trim(), CaloriesPerPortion = caloriesPerPortion, Portions = portions });
        store.Save();
    }
    public void RemoveFood(Guid id)
    {
        var user = RequireCurrent(); var entry = Data.Foods.FirstOrDefault(f => f.Id == id);
        if (entry == null || entry.UserId != user.Id) throw new InvalidOperationException("Bu yemek kaydına erişimin yok.");
        Data.Foods.Remove(entry);Data.MealIntakes.RemoveAll(intake=>intake.UserId==user.Id&&intake.FoodEntryId==id); store.Save();
    }
}
