using Avalonia.Controls;
using FitTeam.Core;
namespace FitTeam.Desktop.Views;
public partial class MainWindow
{
    private sealed record DayChoice(PlanDay Day) { public override string ToString() => AppLanguage.Text($"{Day.Date:dd.MM.yyyy} • {Day.Title} • {Day.Status}"); }
    private sealed record WorkoutRow(string Date, string Done, int Minutes, int Calories, int Heart, decimal Distance, int Fatigue);
    private Control WorkoutPage()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var eligible = service.Plan(Me)?.Days.Where(d => d.Date <= today && d.Status != "Dinlenme" && d.Status != "Kaçırıldı").OrderByDescending(d => d.Date).Select(d => new DayChoice(d)).ToList() ?? [];
        if (eligible.Count == 0) return Ui.Page(Ui.Card(Ui.Stack(Ui.Text("Kayıt yapılabilecek gün yok", 22, Ui.Ink, true), Ui.Text("Önce program oluştur. Gelecek günlerin antrenmanı önceden kaydedilemez."), Ui.Button("Programı aç", () => ShowPage("30 günlük program"), true))));
        var choice = new ComboBox { ItemsSource = eligible, SelectedItem = eligible.FirstOrDefault(c => c.Day.Id == selectedLogDay) ?? eligible[0] }; var editor = new StackPanel { Spacing = 12 };
        void Render()
        {
            editor.Children.Clear(); if (choice.SelectedItem is not DayChoice selection) return;
            var day = selection.Day; var draft = service.Draft(day.Id); var old = service.Logs(Me).FirstOrDefault(l => l.PlanDayId == day.Id);
            var done = new CheckBox { Content = "Antrenmanı tamamladım", IsChecked = old?.Completed ?? (draft?.Exercises.All(e => e.Done) ?? false) };
            var minutes = Ui.Number(old?.Minutes ?? (draft == null ? day.Exercises.Sum(e => e.Minutes) : Math.Max(1, (int)Math.Ceiling(draft.ElapsedSeconds / 60d))), 1, 600);
            var calories = Ui.Number(old?.Calories ?? 0, 0, 10000); var avg = Ui.Number(old?.AverageHeart ?? 0, 0, 250); var max = Ui.Number(old?.MaxHeart ?? 0, 0, 250);
            var steps = Ui.Number(old?.Steps ?? 0, 0, 100000); var distance = Ui.Number(old?.Distance ?? 0, 0, 100, 2); var fatigue = Ui.Number(old?.Fatigue ?? 5, 1, 10);
            var difficulty = Ui.Combo(["Kolay", "Normal", "Zor"], old?.Difficulty ?? "Normal");
            var muscles = new[] { "Yok", "Bacak", "Göğüs", "Sırt", "Omuz", "Kol", "Karın", "Tüm vücut" }; var pain = Ui.Combo(muscles, old?.Pain ?? "Yok"); var tired = Ui.Combo(muscles, old?.TiredMuscle ?? "Yok");
            var note = Ui.Input(old?.Note ?? ""); note.AcceptsReturn = true; note.MinHeight = 80;
            var exerciseResults = new List<ExerciseResult>(); var allDone = new CheckBox { Content = "Bütün hareketleri yaptım", IsChecked = old?.Exercises.All(e => e.Done) ?? (draft?.Exercises.All(e => e.Done) ?? false) };
            var boxes = new List<CheckBox>();
            editor.Children.Add(Ui.Text("Hareket kayıtları", 18, Ui.Ink, true)); editor.Children.Add(allDone);
            foreach (var e in day.Exercises)
            {
                var previous = old?.Exercises.FirstOrDefault(x => x.Name == e.Name) ?? draft?.Exercises.FirstOrDefault(x => x.Name == e.Name);
                var result = new ExerciseResult { Name = e.Name, PerformedName=ExerciseGuides.PerformedName(e.Name,(draft?.Location??old?.Location)=="Ev"), Done = previous?.Done ?? false, Sets = previous?.Sets ?? e.Sets, Reps = previous?.Reps ?? e.Reps, Minutes = previous?.Minutes ?? e.Minutes, LoadKg = previous?.LoadKg ?? 0 }; exerciseResults.Add(result);
                var check = new CheckBox { Content = e.Name, IsChecked = result.Done, MinWidth = 220 }; boxes.Add(check); check.IsCheckedChanged += (_, _) => result.Done = check.IsChecked == true;
                var sets = Ui.Number(result.Sets, 0, 20); sets.ValueChanged += (_, _) => result.Sets = Ui.Int(sets);
                var reps = Ui.Number(result.Reps, 0, 100); reps.ValueChanged += (_, _) => result.Reps = Ui.Int(reps);
                var load = Ui.Number(result.LoadKg, 0, 500, 1); load.ValueChanged += (_, _) => result.LoadKg = Ui.Value(load);
                var duration = Ui.Number(result.Minutes, 0, 180); duration.ValueChanged += (_, _) => result.Minutes = Ui.Int(duration);
                editor.Children.Add(Ui.Row(check, Ui.Field("Yapılan set", sets, 130), Ui.Field("Tekrar", reps, 130), Ui.Field("Süre (dk)", duration, 130), Ui.Field("Ağırlık (kg)", load, 130)));
            }
            allDone.IsCheckedChanged += (_, _) => { foreach (var check in boxes) check.IsChecked = allDone.IsChecked; };
            editor.Children.Add(Ui.Text("Saat ve antrenman verileri", 18, Ui.Ink, true));
            editor.Children.Add(Ui.Text("Saat verilerini elle girebilirsin. Nabız / kalori bilinmiyorsa 0 bırak; cihaz bağlantısı gerekmez.", 12, Ui.Muted));
            editor.Children.Add(Ui.Row(Ui.Field("Toplam süre (dk)", minutes), Ui.Field("Yakılan kalori (kcal)", calories), Ui.Field("Ortalama nabız (BPM)", avg), Ui.Field("Maksimum nabız (BPM)", max), Ui.Field("Adım sayısı", steps), Ui.Field("Mesafe (km)", distance)));
            editor.Children.Add(Ui.Text("Antrenman sonrası", 18, Ui.Ink, true));
            editor.Children.Add(Ui.Row(Ui.Field("Yorgunluk (1-10)", fatigue), Ui.Field("Zorluk", difficulty), Ui.Field("Ağrı bölgesi", pain), Ui.Field("Yorulan kas grubu", tired)));
            editor.Children.Add(Ui.Field("Kısa not", note, 700)); editor.Children.Add(done);
            editor.Children.Add(Ui.AsyncButton(old == null ? "Antrenmanı kaydet" : "Kaydı güncelle", () => Run(() =>
            {
                service.RecordWorkout(new WorkoutLog { Location=draft?.Location??old?.Location??"Salon", PlanDayId = day.Id, Completed = done.IsChecked == true, Minutes = Ui.Int(minutes), Calories = Ui.Int(calories), AverageHeart = Ui.Int(avg), MaxHeart = Ui.Int(max), Steps = Ui.Int(steps), Distance = Ui.Value(distance), Fatigue = Ui.Int(fatigue), Difficulty = Ui.Selected(difficulty), Pain = Ui.Selected(pain), TiredMuscle = Ui.Selected(tired), Note = note.Text ?? "", Exercises = exerciseResults }); Refresh();
            }), true));
        }
        choice.SelectionChanged += (_, _) => Render(); Render();
        var history = Ui.Table(service.Logs(Me).OrderByDescending(l => l.Date).Select(l => new WorkoutRow(l.Date.ToString("dd.MM"), l.Completed ? "Tamamlandı" : "Eksik", l.Minutes, l.Calories, l.AverageHeart, l.Distance, l.Fatigue)), ("Tarih", "Date"), ("Durum", "Done"), ("Dakika", "Minutes"), ("Kalori", "Calories"), ("Ort. nabız", "Heart"), ("Km", "Distance"), ("Yorgunluk", "Fatigue"));
        return Ui.Page(Ui.Card(Ui.Stack(Ui.Field("Kayıt günü", choice, 700), editor)), Ui.Card(Ui.Stack(Ui.Text("Antrenman geçmişi", 18, Ui.Ink, true), history)));
    }
}
