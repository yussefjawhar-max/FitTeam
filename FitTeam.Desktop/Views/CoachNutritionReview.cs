using Avalonia.Controls;
using Avalonia.Controls.Templates;
using FitTeam.Core;
namespace FitTeam.Desktop.Views;

public partial class MainWindow
{
    private Control CoachNutritionReview(List<User> members)
    {
        var person=new ComboBox{ItemsSource=members,SelectedIndex=members.Count>0?0:-1,ItemTemplate=new FuncDataTemplate<User>((user,_)=>Ui.UserText(user?.Profile.Name??""))};
        var date=Ui.Date(DateOnly.FromDateTime(DateTime.Today));var details=Ui.Stack();
        void Render()
        {
            details.Children.Clear();if(person.SelectedItem is not User member)return;
            try
            {
                service.EnsureAccess(member);var day=Ui.DateValue(date);var routine=member.Profile.MealRoutine;
                if(routine!=null&&!string.IsNullOrWhiteSpace(routine.AllergyNotes))details.Children.Add(Ui.Card(Ui.Stack(Ui.Text("Üyenin alerji / intolerans notu",18,Ui.Ink,true),Ui.UserText(routine.AllergyNotes))));
                if(routine!=null&&routine.ExcludedFoods.Count>0)details.Children.Add(Ui.UserText(AppLanguage.Text("Sevmediği yiyecekler:")+" "+string.Join(", ",routine.ExcludedFoods.Select(AppLanguage.Text))));
                var assigned=service.MealPlan(member);foreach(var meal in assigned)details.Children.Add(Ui.Card(Ui.Stack(Ui.Text(meal.Meal,18,Ui.Ink,true),Ui.UserText(meal.Contents),Ui.UserText(meal.Note))));
                var suggested=MealSuggestions.Create(member.Profile,day);
                if(routine!=null&&suggested.State==NutritionState.Ready)
                {
                    var plan=service.ApplyPersonalMeals(member,day,suggested,routine);
                    foreach(var meal in plan.Meals)
                    {
                        var intake=service.MealIntakes(member,day).FirstOrDefault(item=>item.Meal==meal.Meal);
                        details.Children.Add(Ui.Card(Ui.Stack(Ui.Text(meal.Meal,18,Ui.Ink,true),Ui.UserText(meal.Title),Ui.Text(meal.Edited?"Üyenin düzenlediği öğün":"Öğün önerisi",12,Ui.Green),
                            Ui.UserText(string.Join(" / ",meal.Portions.Select(portion=>AppLanguage.Text(portion.Food.Name)+" "+portion.Grams.ToString("0.#")+" g"))),
                            Ui.Text(intake?.Status??"Tüketim kaydı yok",14,Ui.Muted))));
                    }
                }
                var consumed=service.Data.Foods.Where(food=>food.UserId==member.Id&&food.Date==day).ToList();
                var foodPanel=Ui.Stack(Ui.Text("Gerçekte yediklerinin kaydı",19,Ui.Ink,true));
                foreach(var food in consumed)foodPanel.Children.Add(Ui.UserText(AppLanguage.Text(food.Meal)+" • "+food.Name+" • "+food.Portions.ToString("0.##")+" × "+food.CaloriesPerPortion.ToString("0.#")+" = "+food.Calories.ToString("0.#")+" kcal"));
                if(consumed.Count==0)foodPanel.Children.Add(Ui.Text("Bu gün için henüz yemek kaydı yok.",14,Ui.Muted));
                foodPanel.Children.Add(Ui.Text("Kaydedilen yemekler: "+Nutrition.Consumed(consumed).ToString("0.#")+" kcal",15,Ui.Green,true));details.Children.Add(Ui.Card(foodPanel));
                foreach(var skipped in service.MealIntakes(member,day).Where(intake=>intake.Fraction==0))details.Children.Add(Ui.Text(skipped.Meal+" • "+skipped.Status,14,Ui.Muted));
                LanguageView.Apply(details);
            }
            catch(Exception ex){details.Children.Add(Ui.Text(Ui.ErrorMessage(ex),14,Ui.Muted));}
        }
        person.SelectionChanged+=(_,_)=>Render();date.SelectedDateChanged+=(_,_)=>Render();Render();
        return Ui.Card(Ui.Stack(Ui.Text("Üyenin yemek programı ve tüketimi",23,Ui.Ink,true),Ui.Row(Ui.Field("Üye",person,350),Ui.Field("Gün",date,230)),details));
    }
}
