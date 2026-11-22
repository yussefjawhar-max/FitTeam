using FitTeam.Core;

static class PersonalMealChecks
{
    public static void Run(Action<bool, string> check)
    {
        var directory = Path.Combine(Path.GetTempPath(), "FitTeam_personal_meals_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, "fitteam.json");
        try
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            var service = new FitService(new DataStore(path));
            service.Login("sporcu", "Fit123!");
            var routine = new MealRoutine { FoodPreferencesSet = true,
                PreferredFoods = FoodPreferences.Options.Select(option => option.Id).ToList(),
                Hours = MealRoutine.DefaultHours(service.RequireCurrent().Profile) };
            service.SetMealRoutine(routine, "low", false);
            var meal = Nutrition.Meals[0];
            var portions = new List<PersonalMealPortion> { new() { Food = "Yulaf", Grams = 70 }, new() { Food = "Yoğurt", Grams = 200 } };
            var consumedBefore = service.Data.Foods.Count;
            service.SavePersonalMeal(today, meal, "Kendi kahvaltım", portions, routine, "low", false);
            var restored = new FitService(new DataStore(path));
            restored.Login("sporcu", "Fit123!");
            var profile = restored.RequireCurrent().Profile;
            var restoredRoutine = profile.MealRoutine!;
            var menu = restored.ApplyPersonalMeals(today, MealSuggestions.Create(profile, today), restoredRoutine);
            var breakfast = menu.Meals.Single(item => item.Meal == meal);
            check(breakfast.Edited && breakfast.Title == "Kendi kahvaltım" && breakfast.Portions[0].Grams == 70,
                "Düzenlenen yemek ve gramlar yeniden açılışta korunur");
            check(breakfast.Calories == (int)Math.Round(FoodGuide.All.Single(food => food.Name == "Yulaf").Calories * 0.7m
                + FoodGuide.All.Single(food => food.Name == "Yoğurt").Calories * 2),
                "Değiştirilen yiyecek miktarları gösterilen kalori toplamına yansır");
            check(restored.Data.Foods.Count == consumedBefore, "Yemek programını kaydetmek öğünü yenmiş saymaz");
            var tomorrow = today.AddDays(1);
            restored.SavePersonalMeal(tomorrow, meal, "Yarınki kahvaltım", portions, restoredRoutine, "low", false);
            check(restored.ApplyPersonalMeals(today, MealSuggestions.Create(profile, today), restoredRoutine).Meals[0].Title == "Kendi kahvaltım"
                && restored.ApplyPersonalMeals(tomorrow, MealSuggestions.Create(profile, tomorrow), restoredRoutine).Meals[0].Title == "Yarınki kahvaltım",
                "Farklı günlerin aynı öğünü ayrı program olarak tutulur");
            restored.Login("oyuncu", "Fit123!");
            var other = restored.RequireCurrent().Profile;
            var otherMenu = restored.ApplyPersonalMeals(today, MealSuggestions.Create(other, today, activity: "low"), routine);
            check(otherMenu.Meals.Count == 4 && otherMenu.Meals.All(item => !item.Edited),
                "Başka üyenin yemek düzenlemesi kişisel programa karışmaz");
            restored.RestoreMealSuggestion(today, meal);
            restored.Login("sporcu", "Fit123!");
            check(restored.ApplyPersonalMeals(today, MealSuggestions.Create(profile, today), restoredRoutine).Meals[0].Edited,
                "Bir üye başka üyenin kayıtlı yemek düzenlemesini kaldıramaz");
            var meat = Nutrition.Meals[1];
            restored.SavePersonalMeal(today, meat, "Öğle tabağım", [new() { Food = "Tavuk göğsü", Grams = 120 }], restoredRoutine, "low", false);
            var vegetarian = new MealRoutine { FoodPreferencesSet = true, PreferredFoods = ["eggs", "dairy", "legumes"],
                Vegetarian = true, Hours = MealRoutine.DefaultHours(profile) };
            var vegetarianMenu = restored.ApplyPersonalMeals(today, MealSuggestions.Create(profile, today, routine: vegetarian), vegetarian);
            check(vegetarianMenu.Meals.SelectMany(item => item.Portions).All(item => item.Food.Category != "Et ve balık"),
                "Vejetaryen seçimine geçildiğinde eski etli düzenleme gösterilmez");
            void Reject(Action action, string message)
            {
                bool rejected = false;
                try { action(); } catch (ArgumentException) { rejected = true; }
                check(rejected, message);
            }
            Reject(() => restored.SavePersonalMeal(today, meal, "Geçersiz", [new() { Food = "Yulaf", Grams = 0 }], restoredRoutine, "low", false),
                "Sıfır miktarla yemek programı kaydedilmez");
            Reject(() => restored.SavePersonalMeal(today, meal, "Geçersiz", [new() { Food = "Bilinmeyen besin", Grams = 100 }], restoredRoutine, "low", false),
                "Bilinen gıda değerleri olmayan besin için kalori uydurulmaz");
            Reject(() => restored.SavePersonalMeal(today.AddDays(7), meal, "Geçersiz", portions, restoredRoutine, "low", false),
                "Yedi günlük seçimin dışındaki güne yemek düzenlemesi yazılmaz");
            Reject(() => restored.SavePersonalMeal(today, meal, "Geçersiz", portions, restoredRoutine, "low", true),
                "Özel beslenme işaretlendiğinde otomatik önerinin düzenlenmesi engellenir");
            Reject(() => restored.SavePersonalMeal(today, meal, "Geçersiz", [new() { Food = "Tavuk göğsü", Grams = 120 }], vegetarian, "low", false),
                "Vejetaryen tercihiyle etli yemek kaydedilemez");
            restored.RestoreMealSuggestion(today, meal);
            check(!restored.ApplyPersonalMeals(today, MealSuggestions.Create(profile, today), restoredRoutine).Meals[0].Edited
                && restored.Data.PersonalMeals.Any(item => item.Date == tomorrow),
                "Öneriye dönüş yalnız seçilen günün öğününü sıfırlar");
            foreach (var language in new[] { "en", "ar" })
            {
                AppLanguage.Use(language);
                check(AppLanguage.Text("Öğünü düzenle") != "Öğünü düzenle", language + " dilinde yemek düzenleme düğmesi çevrilir");
            }
        }
        finally { AppLanguage.Use("tr"); Directory.Delete(directory, true); }
    }
}
