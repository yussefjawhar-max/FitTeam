using Avalonia.Controls;
using FitTeam.Core;
using FitTeam.Desktop.Controls;
namespace FitTeam.Desktop.Views;
public partial class MainWindow
{
    private sealed record HomeChoice(string Name,List<Exercise> Exercises,Guid? TodayId=null){public override string ToString()=>AppLanguage.Text(Name);}
    private Control HomeWorkoutPage()
    {
        var today=DateOnly.FromDateTime(DateTime.Today);var plan=service.Plan(Me);var choices=new List<HomeChoice>();
        if(plan!=null)foreach(var day in plan.Days.Where(d=>d.Exercises.Count>0))choices.Add(new(day.Date.ToString("dd.MM.yyyy")+" • "+day.Title,day.Exercises,day.Date==today&&day.Status is "Planlandı" or "Eksik"?day.Id:null));
        if(choices.Count==0){var ready=ExerciseLibrary.ReadyWorkouts().First();choices.Add(new(ready.Name,ready.Exercises));}
        var selector=new ComboBox{ItemsSource=choices,SelectedItem=choices.FirstOrDefault(c=>c.TodayId!=null)??choices.Last(),ItemTemplate=new Avalonia.Controls.Templates.FuncDataTemplate<HomeChoice>((c,_)=>Ui.Text(c?.ToString()??""))};
        var body=new StackPanel{Spacing=14};
        void Render()
        {
            body.Children.Clear();if(selector.SelectedItem is not HomeChoice selected)return;
            body.Children.Add(Ui.Text(selected.TodayId!=null?"Bugünkü antrenmanını evde tamamlayabilirsin.":"Seçtiğin hareketleri yeniden gösterebilir ve serbest çalışabilirsin.",15,Ui.Muted));
            foreach(var exercise in selected.Exercises)
            {
                var guide=ExerciseGuides.For(exercise.Name,true);string name=ExerciseGuides.PerformedName(exercise.Name,true);
                var card=Ui.Stack(Ui.Text(name,21,Ui.Ink,true),new ExerciseIllustration{Movement=guide?.Movement??"",Height=230},Ui.Text(guide?.Instructions??"Bu hareketin tekniğini antrenörünle kontrol et.",14,Ui.Muted));
                if(name!=exercise.Name)card.Children.Add(Ui.Text("Programdaki hareket: "+exercise.Name+" • evde karşılığı gösteriliyor.",13,Ui.Muted));
                body.Children.Add(Ui.Card(card));
            }
            body.Children.Add(Ui.Button("Hareketlerle birlikte başlat",()=>{if(selected.TodayId!=null)OpenSession(selected.TodayId.Value,true);else OpenPractice(selected.Exercises,selected.Name);},true));
            if(practiceDraft!=null)body.Children.Add(Ui.Button("Serbest çalışmama devam et",()=>ShowPage("Canlı antrenman")));
        }
        selector.SelectionChanged+=(_,_)=>Render();Render();return Ui.Page(Ui.Card(Ui.Stack(Ui.Text("Evde antrenman",26,Ui.Ink,true),Ui.Text("Salona gelemediğinde hareket görsellerini ve sayaçları evde kullan. Makine gerektiren hareketlerin evdeki karşılığı açıkça gösterilir.",14,Ui.Muted),Ui.Field("Çalışacağın program",selector,550))),body);
    }
}
