namespace FitTeam.Core;
public sealed record SuggestedPortion(FoodGuideItem Food,decimal Grams)
{
    public decimal Calories=>Food.Calories*Grams/100;
    public decimal Protein=>Food.Protein*Grams/100;
}
public sealed record SuggestedMeal(string Meal,string Title,List<SuggestedPortion> Portions)
{
    public MealRoutine? ProteinProduct {get;init;}
    public bool Edited {get;init;}
    public int Calories=>(int)Math.Round(Portions.Sum(p=>p.Calories))+(ProteinProduct?.ProductCalories??0);
    public decimal Protein=>Math.Round(Portions.Sum(p=>p.Protein),1)+(ProteinProduct?.ProductProtein??0);
}
public sealed record SuggestedDay(NutritionState State,string Goal,int Target,List<SuggestedMeal> Meals)
{
    public int TotalCalories=>Meals.Sum(m=>m.Calories);
}
public static class MealSuggestions
{
    private sealed record Recipe(string Title,(string Food,decimal Grams)[] Foods);
    private static Recipe R(string title,params (string,decimal)[] foods)=>new(title,foods);
    private static readonly Recipe[][] Loss=[
        [R("Yumurtalı ve lorlu kahvaltı",("Yumurta",50),("Lor peyniri",100),("Tam buğday ekmeği",60),("Domates",100),("Salatalık",100)),R("Tavuk, bulgur ve sebze",("Tavuk göğsü",120),("Bulgur (pişmiş)",200),("Brokoli",200),("Zeytinyağı",7)),R("Mercimek, yoğurt ve salata",("Mercimek (pişmiş)",200),("Yoğurt",150),("Domates",100),("Salatalık",100),("Tam buğday ekmeği",40),("Zeytinyağı",7)),R("Elma ve badem",("Elma",150),("Badem",10))],
        [R("Yoğurtlu yulaf ve çilek",("Yoğurt",200),("Yulaf",45),("Çilek",150),("Ceviz",10)),R("Hindi ve kinoalı sebze",("Hindi göğsü",130),("Kinoa (pişmiş)",180),("Brokoli",200),("Zeytinyağı",7)),R("Fasulye, bulgur ve salata",("Fasulye (pişmiş)",220),("Bulgur (pişmiş)",140),("Domates",150),("Salatalık",100),("Zeytinyağı",5)),R("Kefir ve portakal",("Kefir",150),("Portakal",150))],
        [R("Lorlu tam buğday kahvaltısı",("Lor peyniri",120),("Tam buğday ekmeği",70),("Domates",120),("Salatalık",100),("Yumurta",50)),R("Nohut, bulgur ve sebze",("Nohut (pişmiş)",180),("Bulgur (pişmiş)",150),("Ispanak",180),("Zeytinyağı",5)),R("Somon, patates ve brokoli",("Somon",120),("Patates (haşlanmış)",180),("Brokoli",200)),R("Yoğurt ve elma",("Yoğurt",150),("Elma",150))]
    ];
    private static readonly Recipe[][] Gain=[
        [R("Muzlu ve cevizli yulaf",("Yulaf",65),("Yoğurt",200),("Muz",120),("Ceviz",15)),R("Tavuk, pirinç ve sebze",("Tavuk göğsü",150),("Pirinç (pişmiş)",220),("Brokoli",150),("Zeytinyağı",10)),R("Somon ve patates tabağı",("Somon",150),("Patates (haşlanmış)",250),("Ispanak",150),("Zeytinyağı",10)),R("Süt, muz ve badem",("Süt",200),("Muz",100),("Badem",20))],
        [R("Yumurtalı ve avokadolu kahvaltı",("Yumurta",100),("Tam buğday ekmeği",90),("Avokado",70),("Domates",150)),R("Hindi, makarna ve sebze",("Hindi göğsü",180),("Makarna (pişmiş)",250),("Brokoli",150),("Zeytinyağı",10)),R("Nohut, pirinç ve yoğurt",("Nohut (pişmiş)",200),("Pirinç (pişmiş)",180),("Yoğurt",180),("Salatalık",100)),R("Kefir, hurma ve ceviz",("Kefir",200),("Hurma",35),("Ceviz",20))],
        [R("Lorlu ekmek ve süt",("Lor peyniri",150),("Tam buğday ekmeği",100),("Süt",200),("Domates",150)),R("Somon, kinoa ve sebze",("Somon",170),("Kinoa (pişmiş)",230),("Brokoli",180),("Zeytinyağı",10)),R("Tavuk, bulgur ve avokado",("Tavuk göğsü",170),("Bulgur (pişmiş)",250),("Avokado",70),("Domates",150)),R("Yoğurt, muz ve fındık",("Yoğurt",200),("Muz",120),("Fındık",20))]
    ];
    private static readonly Recipe[][] Vegetarian=[
        [R("Yoğurtlu yulaf ve meyve",("Yoğurt",180),("Yulaf",50),("Elma",120),("Badem",10)),R("Mercimek, bulgur ve sebze",("Mercimek (pişmiş)",220),("Bulgur (pişmiş)",150),("Brokoli",200),("Zeytinyağı",7)),R("Nohutlu salata ve yoğurt",("Nohut (pişmiş)",200),("Yoğurt",150),("Domates",150),("Salatalık",150),("Zeytinyağı",5)),R("Kefir ve portakal",("Kefir",180),("Portakal",150))],
        [R("Muzlu ve cevizli yulaf",("Yulaf",70),("Süt",230),("Muz",120),("Ceviz",15)),R("Nohut ve pirinç tabağı",("Nohut (pişmiş)",220),("Pirinç (pişmiş)",200),("Brokoli",180),("Zeytinyağı",10)),R("Fasulye, ekmek ve yoğurt",("Fasulye (pişmiş)",250),("Tam buğday ekmeği",80),("Yoğurt",180),("Domates",150)),R("Yoğurt, hurma ve badem",("Yoğurt",200),("Hurma",30),("Badem",15))],
        [R("Yumurtalı ve lorlu kahvaltı",("Yumurta",100),("Lor peyniri",100),("Tam buğday ekmeği",60),("Domates",150)),R("Fasulye, kinoa ve sebze",("Fasulye (pişmiş)",220),("Kinoa (pişmiş)",150),("Ispanak",200),("Zeytinyağı",7)),R("Mercimek, yoğurt ve salata",("Mercimek (pişmiş)",230),("Yoğurt",180),("Tam buğday ekmeği",60),("Domates",150),("Salatalık",150)),R("Muz ve fındık",("Muz",120),("Fındık",15))]
    ];
    public static SuggestedDay Create(Profile profile,DateOnly day,bool? vegetarian=null,int alternative=0,string? activity=null,bool? specialCare=null,MealRoutine? routine=null)
    {
        routine??=profile.MealRoutine;
        var estimate=Nutrition.Calculate(profile,activity,(specialCare??profile.NutritionNeedsSpecialCare)||!string.IsNullOrWhiteSpace(routine?.AllergyNotes));
        if(estimate.State!=NutritionState.Ready)return new(estimate.State,"",0,[]);
        if(estimate.Target is <1200 or >3500)return new(NutritionState.TargetNeedsReview,"",estimate.Target,[]);
        int productCalories=0;
        if(routine!=null&&!routine.FoodPreferencesSet)return new(NutritionState.MissingPreferences,"",estimate.Target,[]);
        if(routine!=null)routine.Validate();
        if(routine?.UsesProteinProduct==true){routine.Validate();productCalories=routine.ProductCalories;if(productCalories>=estimate.Target*0.35m)return new(NutritionState.TargetNeedsReview,"",estimate.Target,[]);}
        string goal=estimate.Adjustment<0?"Kilo verme":estimate.Adjustment>0?"Kilo alma":"Kiloyu koruma";
        if(profile.Goal.Contains("Kas",StringComparison.OrdinalIgnoreCase))goal="Kas hedefi • "+goal;
        int variant=((day.DayNumber+alternative)%3+3)%3;var recipes=((vegetarian??routine?.Vegetarian??false)?Vegetarian:estimate.Adjustment>0?Gain:Loss)[variant];
        var budgets=Nutrition.MealBudget(estimate.Target-productCalories);var meals=new List<SuggestedMeal>();
        for(int i=0;i<recipes.Length;i++)
        {
            var recipe=recipes[i];var ingredients=recipe.Foods.Select(f=>(Food:FoodGuide.All.First(g=>g.Name==f.Food),f.Grams)).ToList();
            string title=recipe.Title;
            if(routine?.FoodPreferencesSet==true)
            {
                var allowed=FoodPreferences.Options.Where(o=>routine.PreferredFoods.Contains(o.Id)&&o.Foods.Any(name=>!routine.ExcludedFoods.Contains(name))&&(!(vegetarian??routine.Vegetarian)||o.Id is "eggs" or "dairy" or "legumes")).ToList();
                if(allowed.Count==0)return new(NutritionState.MissingPreferences,"",estimate.Target,[]);
                bool replaced=false;
                for(int j=0;j<ingredients.Count;j++)
                {
                    var group=FoodPreferences.Options.FirstOrDefault(o=>o.Foods.Contains(ingredients[j].Food.Name));if(group==null)continue;
                    if(allowed.Any(o=>o.Id==group.Id))continue;
                    // Seçilmeyen grupların yerine kabul edilen bir kaynak kullanılır.
                    var selected=allowed[(day.DayNumber+alternative+i)%allowed.Count];
                    var accepted=selected.Foods.Where(name=>!routine.ExcludedFoods.Contains(name)).ToList();
                    var replacement=FoodGuide.All.First(f=>f.Name==accepted[(day.DayNumber+alternative+i+j)%accepted.Count]);
                    ingredients[j]=(replacement,ingredients[j].Grams);replaced=true;
                }
                for(int j=0;j<ingredients.Count;j++)if(routine.ExcludedFoods.Contains(ingredients[j].Food.Name))
                {
                    var substitutes=PersonalMealFoods.Available(routine).Where(food=>food.Category==ingredients[j].Food.Category).ToList();
                    if(substitutes.Count==0){ingredients.RemoveAt(j--);replaced=true;continue;}
                    ingredients[j]=(substitutes[(day.DayNumber+i+j)%substitutes.Count],ingredients[j].Grams);replaced=true;
                }
                if(ingredients.Count==0)return new(NutritionState.MissingPreferences,"",estimate.Target,[]);
                var combined=ingredients.GroupBy(f=>f.Food.Name).Select(g=>(Food:g.First().Food,Grams:g.Sum(f=>f.Grams))).ToList();ingredients=combined;
                if(replaced)title=string.Join(" ve ",ingredients.Take(2).Select(f=>f.Food.Name));
            }
            decimal baseline=ingredients.Sum(f=>f.Food.Calories*f.Grams/100);decimal factor=budgets[i].Calories/baseline;
            var portions=ingredients.Select(f=>new SuggestedPortion(f.Food,Math.Max(5,Math.Round(f.Grams*factor/5)*5))).ToList();
            meals.Add(new(budgets[i].Meal,title,portions){ProteinProduct=i==3&&routine?.UsesProteinProduct==true?routine:null});
        }
        return new(NutritionState.Ready,goal,estimate.Target,meals);
    }
    public static List<(DateOnly Date,SuggestedDay Plan)> Week(Profile profile,DateOnly start,MealRoutine routine)
        =>Enumerable.Range(0,7).Select(i=>(start.AddDays(i),Create(profile,start.AddDays(i),routine:routine))).ToList();
}
