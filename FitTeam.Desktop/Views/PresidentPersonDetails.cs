using Avalonia.Controls;
using FitTeam.Core;
namespace FitTeam.Desktop.Views;

public partial class MainWindow
{
    private Control PresidentPersonRecords(User person)
    {
        var selectedDate=Ui.Date(DateOnly.FromDateTime(DateTime.Today));var records=Ui.Stack();
        void Render()
        {
            if(selectedDate.SelectedDate==null)return;
            var day=service.AdministrationDay(person.Id,Ui.DateValue(selectedDate));records.Children.Clear();
            var p=person.Profile;var estimate=Nutrition.Calculate(p);var consumed=Nutrition.Consumed(day.Foods);
            records.Children.Add(Ui.Stats(Ui.Stat("Antrenman kaydı",day.Workouts.Count.ToString()),Ui.Stat("Tüketilen",consumed.ToString("0")+" kcal"),Ui.Stat("İçilen su",day.Water.ToString("0.#")+" L")));
            var training=Ui.Stack(Ui.Text("Planlanan hareketler",20,Ui.Ink,true),Ui.Text(TrainingWindow.Label(p),14,Ui.Green));
            foreach(var plan in day.Plans)
            {
                training.Children.Add(Ui.Text(plan.Title+" • "+plan.Status,17,Ui.Ink,true));
                foreach(var exercise in plan.Exercises)training.Children.Add(Ui.Text(exercise.Name+" • "+exercise.Sets+" set × "+exercise.Reps+" tekrar • "+exercise.Minutes+" dk • "+exercise.RestSeconds+" sn dinlenme",14));
            }
            if(day.Plans.Count==0)training.Children.Add(Ui.Text("Bu tarih için antrenman planı yok.",14,Ui.Muted));
            records.Children.Add(Ui.Card(training));
            var completed=Ui.Stack(Ui.Text("Yaptığı antrenmanlar",20,Ui.Ink,true));
            foreach(var workout in day.Workouts)
            {
                completed.Children.Add(Ui.Text((workout.Completed?"Tamamlandı":"Kısmen tamamlandı")+" • "+workout.Location+" • "+workout.Minutes+" dk • "+workout.Calories+" kcal",16,Ui.Green,true));
                foreach(var movement in workout.Exercises)completed.Children.Add(Ui.Text((movement.PerformedName.Length>0?movement.PerformedName:movement.Name)+" • "+movement.Sets+" set × "+movement.Reps+" tekrar • "+movement.LoadKg+" kg • "+(movement.Done?"Yapıldı":"Yapılmadı"),14));
                completed.Children.Add(Ui.Text("Yorgunluk: "+workout.Fatigue+" / 10 • Ağrı: "+workout.Pain+" • Zorluk: "+workout.Difficulty,14,Ui.Muted));
                if(workout.Note.Length>0)completed.Children.Add(Ui.UserText(workout.Note,14));
            }
            if(day.Workouts.Count==0)completed.Children.Add(Ui.Text("Bu tarihte yapılmış antrenman kaydı yok.",14,Ui.Muted));
            records.Children.Add(Ui.Card(completed));
            var eaten=Ui.Stack(Ui.Text("Yediklerinin kaydı",20,Ui.Ink,true));
            foreach(var food in day.Foods)eaten.Children.Add(Ui.UserText(AppLanguage.Text(food.Meal)+" • "+food.Name+" • "+food.Portions+" "+AppLanguage.Text("porsiyon")+" • "+food.Calories.ToString("0")+" kcal",14));
            if(day.Foods.Count==0)eaten.Children.Add(Ui.Text("Bu tarihte yemek kaydı yok.",14,Ui.Muted));
            if(estimate.State==NutritionState.Ready)eaten.Children.Add(Ui.Text("Günlük hedef: "+estimate.Target+" kcal • Kalan: "+Nutrition.Remaining(estimate,day.Foods).ToString("0")+" kcal",16,Ui.Green,true));
            records.Children.Add(Ui.Card(eaten));
            var menu=Ui.Stack(Ui.Text("Yemek programı",20,Ui.Ink,true));
            foreach(var meal in day.AssignedMeals)
            {
                menu.Children.Add(Ui.Text(meal.Meal,17,Ui.Ink,true));menu.Children.Add(Ui.UserText(meal.Contents,15));if(meal.Note.Length>0)menu.Children.Add(Ui.UserText(meal.Note,13,Ui.Muted));
            }
            if(day.AssignedMeals.Count==0)
            {
                var suggested=p.MealRoutine?.FoodPreferencesSet==true?MealSuggestions.Create(p,day.Date):new SuggestedDay(NutritionState.MissingPreferences,"",0,[]);
                if(suggested.State==NutritionState.Ready)
                {
                    menu.Children.Add(Ui.Text("Hedefe göre öğün önerileri",15,Ui.Green,true));
                    foreach(var meal in suggested.Meals)
                    {
                        string time=p.MealRoutine?.Hours.FirstOrDefault(h=>h.Meal==meal.Meal)?.Time.ToString("HH:mm")??MealRoutine.DefaultHours(p).First(h=>h.Meal==meal.Meal).Time.ToString("HH:mm");
                        menu.Children.Add(Ui.Text(time+" • "+meal.Meal+" • "+meal.Title+" • "+meal.Calories+" kcal",15,Ui.Ink,true));
                        menu.Children.Add(Ui.UserText(string.Join(" / ",meal.Portions.Select(f=>AppLanguage.Text(f.Food.Name)+" "+f.Grams+" g")),14,Ui.Muted));
                        if(meal.ProteinProduct is {} product)menu.Children.Add(Ui.UserText(product.ProductName+" • "+product.ProductCalories+" kcal",14));
                    }
                    menu.Children.Add(Ui.Text("Öneriler, tüketim kayıtlarından ayrı gösterilir.",12,Ui.Muted));
                }
                else menu.Children.Add(Ui.Text("Otomatik yemek önerisi için beslenme tercihlerini ve profilini tamamlaması gerekir.",14,Ui.Muted));
            }
            records.Children.Add(Ui.Card(menu));
            var diary=service.Data.Diary.Where(d=>d.UserId==person.Id&&d.Date==day.Date).ToList();
            if(diary.Count>0)records.Children.Add(Ui.Card(Ui.Stack(new[]{Ui.Text("Günlük kayıtları",20,Ui.Ink,true)}.Concat(diary.Select(d=>Ui.UserText(AppLanguage.Text(d.Kind)+" • "+d.Title+" • "+d.Value+"\n"+d.Note,14))).ToArray())));
            LanguageView.Apply(records);
        }
        selectedDate.SelectedDateChanged+=(_,_)=>Render();Render();
        var body=Ui.Stack(Ui.Text("Günlük plan ve kayıtlar",24,Ui.Ink,true),Ui.Field("İncelemek istediğin tarih",selectedDate,300),records);
        var hours=Ui.Stack(Ui.Text("Hocaları ve çalışma saatleri",22,Ui.Ink,true));
        var snapshot=service.AdministrationDay(person.Id,DateOnly.FromDateTime(DateTime.Today));
        foreach(var coach in snapshot.Coaches)hours.Children.Add(Ui.UserText(coach.Profile.Name+" • "+AppLanguage.Text(FitService.SportOf(coach))+"\n"+AppLanguage.Text(HoursLabel(coach)),15));
        if(person.Role==Choices.Roles[2]||person.Role==Choices.Roles[3])
        {
            hours.Children.Add(Ui.Text(HoursLabel(person),15,Ui.Green));
            var teams=service.Data.Teams.Where(t=>t.IsActive&&t.CoachId==person.Id&&service.ShowClub(t)).Select(t=>t.Id).ToHashSet();
            var members=service.Data.Users.Where(u=>u.IsActive&&!u.NeedsApproval&&service.ShowRecord(u)&&(FitService.CoachLinks(u).Contains(person.Id)||(u.TeamId!=null&&teams.Contains(u.TeamId.Value)))).OrderBy(u=>u.Profile.Name).ToList();
            var names=Ui.Stack();foreach(var member in members)names.Children.Add(Ui.UserText(member.Profile.Name,14));
            hours.Children.Add(new Expander{Header=AppLanguage.Text("Bağlı öğrenciler")+" • "+members.Count,Content=names});
        }
        body.Children.Add(Ui.Card(hours));
        var history=Ui.Stack();
        foreach(var weight in service.Data.Weights.Where(w=>w.UserId==person.Id).OrderByDescending(w=>w.Date))history.Children.Add(Ui.Text(weight.Date.ToString("dd.MM.yyyy")+" • "+weight.Weight+" kg",14));
        if(history.Children.Count==0)history.Children.Add(Ui.Text("Henüz kilo kaydı yok.",14,Ui.Muted));
        body.Children.Add(new Expander{Header="Kilo geçmişi",Content=history});
        var allPlans=Ui.Stack();foreach(var plan in service.Data.Plans.Where(p=>p.UserId==person.Id).SelectMany(p=>p.Days).OrderByDescending(d=>d.Date))allPlans.Children.Add(Ui.Text(plan.Date.ToString("dd.MM.yyyy")+" • "+plan.Title+" • "+plan.Status+"\n"+string.Join(" / ",plan.Exercises.Select(e=>e.Name)),14));
        body.Children.Add(new Expander{Header="Antrenman takvimi",Content=allPlans});
        return body;
    }
    private Control PresidentPersonCredentials(User person)
    {
        var value=Ui.UserText("••••••••",16,Ui.Green,true);bool visible=false;
        return Ui.Card(Ui.Stack(Ui.Text("Hesap bilgileri",21,Ui.Ink,true),Ui.UserText(AppLanguage.Text("Kullanıcı adı:")+" "+person.Username,15),value,
            Ui.Row(Ui.Button("Şifreyi göster / gizle",()=>{visible=!visible;value.Text=visible?IssuedCredentialVault.UserPassword(service,person)??AppLanguage.Text("Şifre kopyası sonraki başarılı girişte kaydedilir."):"••••••••";}),Ui.AsyncButton("Şifre ver / Sıfırla",()=>Guard(()=>AdminPasswordDialog(person)))),
            Ui.Text("Korumalı kopyası bulunan kişisel veya geçici FitTeam şifresi gösterilir.",12,Ui.Muted)));
    }
}
