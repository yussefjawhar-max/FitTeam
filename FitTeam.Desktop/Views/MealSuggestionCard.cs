using Avalonia.Controls;
using Avalonia.Controls.Templates;
using FitTeam.Core;
namespace FitTeam.Desktop.Views;
public partial class MainWindow
{
    private Control MealSuggestionCard()
    {
        var today=DateOnly.FromDateTime(DateTime.Today);int alternative=0;var savedRoutine=Me.Profile.MealRoutine??new MealRoutine();
        var foodChoices=FoodPreferences.Options.Select(o=>new CheckBox{Content=o.Label,Tag=o.Id,IsChecked=savedRoutine.PreferredFoods.Contains(o.Id)}).ToList();
        var excluded=FoodGuide.All.Select(food=>new CheckBox{Content=food.Name,Tag=food.Name,IsChecked=savedRoutine.ExcludedFoods.Contains(food.Name)}).ToList();
        var allergies=Ui.Input(savedRoutine.AllergyNotes,"Alerji veya intoleransın varsa hangi besin olduğunu yaz");allergies.AcceptsReturn=true;allergies.MaxLength=1000;
        var dateChoices=Enumerable.Range(0,7).Select(i=>new MealDateChoice(today.AddDays(i))).ToList();
        var mealDate=new ComboBox{ItemsSource=dateChoices,SelectedIndex=0,ItemTemplate=new FuncDataTemplate<MealDateChoice>((d,_)=>Ui.UserText(d?.Date.ToString("dd MMMM dddd")??""))};
        var weekPanel=Ui.Stack();var shopping=Ui.Stack();
        var activity=new ComboBox{ItemsSource=Nutrition.Activities,SelectedItem=Nutrition.Activities.FirstOrDefault(a=>a.Id==Me.Profile.ActivityLevel),PlaceholderText=AppLanguage.Text("Hareket düzeyini seç"),ItemTemplate=new FuncDataTemplate<ActivityOption>((a,_)=>Ui.Text(a?.Label??""))};
        var preference=Ui.Combo(["Dengeli","Vejetaryen"],savedRoutine.Vegetarian?"Vejetaryen":"Dengeli");
        var protein=Ui.Combo(["Hayır","Evet"],savedRoutine.UsesProteinProduct?"Evet":"Hayır");var productName=Ui.Input(savedRoutine.ProductName,"Etiketteki ürün adı");var productCalories=Ui.Number(savedRoutine.ProductCalories,0,1500);var productProtein=Ui.Number(savedRoutine.ProductProtein,0,100,1);
        var productFields=Ui.Stack(Ui.Field("Protein ürünü",productName,450),Ui.Row(Ui.Field("Bir etiket porsiyonu: kcal",productCalories),Ui.Field("Bir etiket porsiyonu: protein (g)",productProtein)),Ui.Text("Etiketteki bir porsiyonun bilgilerini gir. Uygulama marka veya kullanım dozu önermez; miktarı ürün yönergesi veya diyetisyeninle belirle.",12,Ui.Muted));
        var special=new CheckBox{Content="Besin alerjisi, hamilelik, emzirme veya özel beslenme takibi durumum var",IsChecked=Me.Profile.NutritionNeedsSpecialCare};
        var defaults=MealRoutine.DefaultHours(Me.Profile);var hours=Nutrition.Meals.Select(m=>(Meal:m,Input:Ui.Input((savedRoutine.Hours.FirstOrDefault(h=>h.Meal==m)??defaults.First(h=>h.Meal==m)).Time.ToString("HH:mm")))).ToList();
        var hourFields=Ui.Row(hours.Select(h=>Ui.Field(h.Meal,h.Input,155)).ToArray());
        var meals=new StackPanel{Spacing=12};var status=Ui.Text("",14,Ui.Muted);var error=Ui.Text("",13,Ui.Muted);var alternativeButton=Ui.Button("Başka öğün önerileri göster",()=>{});
        MealRoutine ReadRoutine()
        {
            var routine=new MealRoutine{FoodPreferencesSet=foodChoices.Any(c=>c.IsChecked==true),PreferredFoods=foodChoices.Where(c=>c.IsChecked==true).Select(c=>(string)c.Tag!).ToList(),ExcludedFoods=excluded.Where(c=>c.IsChecked==true).Select(c=>(string)c.Tag!).ToList(),AllergyNotes=allergies.Text??"",Vegetarian=Ui.Selected(preference)=="Vejetaryen",UsesProteinProduct=Ui.Selected(protein)=="Evet",ProductName=productName.Text??"",ProductCalories=Ui.Int(productCalories),ProductProtein=Ui.Value(productProtein)};
            foreach(var hour in hours){if(!TimeOnly.TryParseExact(LoginContact.NormalizeDigits(hour.Input.Text??""),"HH:mm",out var time))throw new ArgumentException("Dört öğünün saatini HH:mm biçiminde gir.");routine.Hours.Add(new(){Meal=hour.Meal,Time=time});}
            routine.Validate();return routine;
        }
        void Render()
        {
            meals.Children.Clear();weekPanel.Children.Clear();shopping.Children.Clear();productFields.IsVisible=Ui.Selected(protein)=="Evet";MealRoutine routine;SuggestedDay plan;var date=(mealDate.SelectedItem as MealDateChoice)?.Date??today;
            try{routine=ReadRoutine();plan=service.ApplyPersonalMeals(date,MealSuggestions.Create(Me.Profile,date,routine.Vegetarian,alternative,(activity.SelectedItem as ActivityOption)?.Id??"",special.IsChecked==true,routine),routine);}
            catch(ArgumentException ex){status.Text=AppLanguage.Text(ex.Message);alternativeButton.IsEnabled=false;return;}
            alternativeButton.IsEnabled=plan.State==NutritionState.Ready;
            if(plan.State!=NutritionState.Ready)
            {
                status.Text=AppLanguage.Text(plan.State switch{NutritionState.MissingPreferences=>"Yemek programı için sevdiğin en az bir besin grubunu seç.",NutritionState.MissingActivity=>"Öğün önerilerini görmek için hareket düzeyini seç.",NutritionState.MissingProfile=>"Öğün önerileri için yaş, boy, kilo ve cinsiyet bilgilerini tamamla.",NutritionState.NeedsSpecialCare=>"18 yaş altı veya özel beslenme durumunda otomatik öğün listesi hazırlanmaz; antrenörün veya diyetisyenin listesini kullan.",_=>"Bu hedef için otomatik öğün listesi hazırlanmaz. Hedefini diyetisyeninle değerlendir."});return;
            }
            status.Text=AppLanguage.Text(plan.Goal)+" • "+AppLanguage.Text("Günlük hedef:")+" "+plan.Target+" kcal • "+AppLanguage.Text("Öğünlerin toplamı:")+" "+plan.TotalCalories+" kcal";
            foreach(var meal in plan.Meals.OrderBy(m=>routine.Hours.First(h=>h.Meal==m.Meal).Time))
            {
                var intake=service.MealIntakes(Me,date).FirstOrDefault(item=>item.Meal==meal.Meal);
                var intakeChoice=Ui.Combo(FitService.MealIntakeChoices,intake?.Status);var savedText=Ui.Text(intake?.Status??"",13,Ui.Green);
                var add=Ui.Button("Yediğim miktarı kaydet",()=>{try{service.RecordMealIntake(date,meal,Ui.Selected(intakeChoice));Render();}catch(Exception ex){savedText.Text=Ui.ErrorMessage(ex);}});add.IsEnabled=date<=today;
                var ingredients=Ui.Stack(meal.Portions.Select(p=>Ui.UserText(AppLanguage.Text(p.Food.Name)+" • "+p.Grams.ToString("0")+" g",14)).ToArray());
                if(meal.ProteinProduct is {} product)ingredients.Children.Add(Ui.UserText(product.ProductName+" • "+AppLanguage.Text("1 etiket porsiyonu")+" • "+product.ProductCalories+" kcal",14));
                var time=routine.Hours.First(h=>h.Meal==meal.Meal).Time.ToString("HH:mm");
                var actions=Ui.Row(Ui.Field("Ne kadar yedin?",intakeChoice,220),add,Ui.AsyncButton("Öğünü düzenle",async()=>{if(await EditPersonalMeal(date,meal,routine,(activity.SelectedItem as ActivityOption)?.Id??"",special.IsChecked==true)){error.Text=AppLanguage.Text("Yemek programın kaydedildi.");Render();}}));
                if(intake!=null)actions.Children.Add(Ui.Button("Yemek kaydını geri al",()=>{try{service.UndoMealIntake(date,meal.Meal);Render();}catch(Exception ex){savedText.Text=Ui.ErrorMessage(ex);}}));
                if(meal.Edited)actions.Children.Add(Ui.Button("Öneriye dön",()=>{service.RestoreMealSuggestion(date,meal.Meal);Render();}));
                meals.Children.Add(Ui.Card(Ui.Stack(Ui.Row(Ui.UserText(time+" • "+AppLanguage.Text(meal.Meal),20,Ui.Ink,true),Ui.Text(meal.Calories+" kcal",16,Ui.Green,true)),meal.Edited?Ui.UserText(meal.Title,17,Ui.Ink,true):Ui.Text(meal.Title,17,Ui.Ink,true),meal.Edited?Ui.Text("Senin düzenlediğin öğün",12,Ui.Green):Ui.Text("Öğün önerisi",12,Ui.Muted),ingredients,Ui.Text(AppLanguage.Text("Protein")+": "+meal.Protein.ToString("0.#")+" g",13,Ui.Muted),actions,savedText)));
            }
            var weekPlans=new List<SuggestedDay>();
            for(int i=0;i<7;i++)
            {
                var weekDate=today.AddDays(i);var dayPlan=service.ApplyPersonalMeals(weekDate,MealSuggestions.Create(Me.Profile,weekDate,routine.Vegetarian,alternative,(activity.SelectedItem as ActivityOption)?.Id??"",special.IsChecked==true,routine),routine);
                weekPlans.Add(dayPlan);
                weekPanel.Children.Add(Ui.Card(Ui.Stack(Ui.UserText(weekDate.ToString("dd MMMM dddd"),18,Ui.Ink,true),Ui.Text(dayPlan.TotalCalories+" kcal",14,Ui.Green),Ui.Stack(dayPlan.Meals.OrderBy(m=>routine.Hours.First(h=>h.Meal==m.Meal).Time).Select(m=>Ui.UserText(routine.Hours.First(h=>h.Meal==m.Meal).Time.ToString("HH:mm")+" • "+AppLanguage.Text(m.Meal)+" • "+(m.Edited?m.Title:AppLanguage.Text(m.Title)),14)).ToArray()))));
            }
            shopping.Children.Add(Ui.Text("Liste, ölçü bilgisi olan yedi günlük öğünlerden hazırlanır. Hocanın serbest metin listesi bu toplama dahil değildir.",12,Ui.Muted));
            foreach(var line in MealShopping.Create(weekPlans))shopping.Children.Add(new CheckBox{Content=line.Name+" • "+line.Quantity.ToString("0.#")+" "+AppLanguage.Text(line.Unit)});
            LanguageView.Apply(shopping);
            LanguageView.Apply(weekPanel);
            LanguageView.Apply(meals);
        }
        alternativeButton.Click+=(_,_)=>{alternative++;Render();};activity.SelectionChanged+=(_,_)=>Render();preference.SelectionChanged+=(_,_)=>Render();protein.SelectionChanged+=(_,_)=>Render();special.IsCheckedChanged+=(_,_)=>Render();
        productName.TextChanged+=(_,_)=>Render();productCalories.ValueChanged+=(_,_)=>Render();productProtein.ValueChanged+=(_,_)=>Render();foreach(var hour in hours)hour.Input.TextChanged+=(_,_)=>Render();foreach(var food in foodChoices)food.IsCheckedChanged+=(_,_)=>Render();foreach(var food in excluded)food.IsCheckedChanged+=(_,_)=>Render();allergies.TextChanged+=(_,_)=>Render();mealDate.SelectionChanged+=(_,_)=>Render();Render();
        return Ui.Card(Ui.Stack(Ui.Text("Hedefime göre ne yemeliyim?",24,Ui.Ink,true),Ui.Text("Kahvaltı, öğle, akşam ve ara öğünler hedefin ve günlük enerji hesabına göre değişir. Miktarlar gramdır; pişmiş yazan besinleri piştikten sonra tart.",14,Ui.Muted),Ui.Text("Önce sevdiğin besinleri seç",20,Ui.Ink,true),Ui.Row(foodChoices.ToArray()),Ui.Text("Seçmediğin besin grupları öğünlere eklenmez. Alerjini ayrıca aşağıda belirt.",12,Ui.Muted),new Expander{Header="Sevmediğim yiyecekler",Content=Ui.Row(excluded.ToArray())},Ui.Field("Alerji / intolerans notum",allergies,500),Ui.Text("Alerji notu girildiğinde otomatik yemek önerisi durur; diyetisyenin hazırladığı listeyi kullan.",12,Ui.Muted),Ui.Field("Günlük hareket düzeyi",activity,500),Ui.Field("Yemek tercihim",preference,300),Ui.Field("Protein ürünü kullanmak istiyor musun?",protein,450),productFields,special,
            Ui.Text("Öğün saatlerim",19,Ui.Ink,true),Ui.Text(TrainingWindow.Label(Me.Profile),13,Ui.Muted),hourFields,Ui.Text("Başlangıç saatleri ders saatine göre önerilir; günlük düzenine göre değiştirebilirsin.",12,Ui.Muted),
            Ui.Button("Beslenme tercihlerimi ve saatlerimi kaydet",()=>{try{var routine=ReadRoutine();if(!routine.FoodPreferencesSet)throw new ArgumentException("Yemek programı için sevdiğin en az bir besin grubunu seç.");service.SetMealRoutine(routine,(activity.SelectedItem as ActivityOption)?.Id??"",special.IsChecked==true);error.Text=AppLanguage.Text("Beslenme tercihlerin kaydedildi.");Render();}catch(Exception ex){error.Text=Ui.ErrorMessage(ex);}}),error,Ui.Field("Yemek programı günü",mealDate,350),status,alternativeButton,meals,new Expander{Header="7 günlük yemek programı",Content=weekPanel},new Expander{Header="Haftalık alışveriş listem",Content=shopping},
            Ui.Text("Yaklaşık yetişkin öğün önerileridir. Kaloriler ürün etiketi ve hazırlama şekline göre değişir. Programı düzenlemek yemeği yenmiş saymaz; tüketimini ayrıca kaydet.",12,Ui.Muted)));
    }
    private sealed record MealDateChoice(DateOnly Date);
}
