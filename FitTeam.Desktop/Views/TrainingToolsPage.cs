using Avalonia.Controls;
using FitTeam.Core;
namespace FitTeam.Desktop.Views;
public partial class MainWindow
{
    private sealed record LibraryRow(string Name, string Muscle, string Equipment, string Level);
    private sealed record TemplateChoice(WorkoutTemplate Template) { public override string ToString() => AppLanguage.Text(Template.Name); }
    private async Task<List<Exercise>?> PickExercises()
    {
        List<Exercise>? result = null;
        var search = Ui.Input("", "Hareket ara"); var muscle = Ui.Combo(new[] { "Tüm bölgeler" }.Concat(ExerciseLibrary.All.Select(e => e.Muscle).Distinct()));
        var selected = new List<LibraryExercise>(); var count = Ui.Text("0 hareket seçildi", 14, Ui.Green);
        var table = Ui.Table(ExerciseLibrary.All.Select(e => new LibraryRow(e.Name, e.Muscle, e.Equipment, e.Level)), ("Hareket", "Name"), ("Bölge", "Muscle"), ("Ekipman", "Equipment"), ("Seviye", "Level"));
        var w = Ui.Dialog("Hareket kütüphanesi", new StackPanel(), 820, 700);
        void Filter() => table.ItemsSource = ExerciseLibrary.All.Where(e => (e.Name.Contains(search.Text ?? "", StringComparison.OrdinalIgnoreCase)||AppLanguage.Text(e.Name).Contains(search.Text??"",StringComparison.OrdinalIgnoreCase)) && (Ui.Selected(muscle) == "Tüm bölgeler" || e.Muscle == Ui.Selected(muscle))).Select(e => new LibraryRow(e.Name, e.Muscle, e.Equipment, e.Level)).ToList();
        search.TextChanged += (_, _) => Filter(); muscle.SelectionChanged += (_, _) => Filter();
        w.Content = new Border { Padding = new Avalonia.Thickness(24), Child = Ui.Stack(Ui.Text("Hareket seç ve programını kur", 23, Ui.Ink, true), Ui.Row(Ui.Field("Ara", search, 320), Ui.Field("Kas bölgesi", muscle, 240)), table,
            Ui.Row(Ui.Button("Seçili hareketi ekle", () => { if (table.SelectedItem is LibraryRow row) { var e = ExerciseLibrary.All.First(x => x.Name == row.Name); if (!selected.Contains(e)) selected.Add(e); count.Text = selected.Count + " hareket seçildi: " + string.Join(", ", selected.Select(x => x.Name)); } }), Ui.Button("Seçimi temizle", () => { selected.Clear(); count.Text = "0 hareket seçildi"; })), count,
            Ui.Button("Programa ekle", () => { if (selected.Count == 0) return; result = selected.Select(ExerciseLibrary.Create).ToList(); w.Close(); }, true)) };
        await w.ShowDialog(this); return result;
    }
    private IEnumerable<WorkoutTemplate> TemplateOptions() => ExerciseLibrary.ReadyWorkouts().Concat(service.MyTemplates());
    private async Task ChooseReadyWorkout(WorkoutTemplate template)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var plan = service.Plan(Me);
        if (plan == null) plan = service.GeneratePlan(today);
        else if (plan.Start.AddDays(29) < today) plan = service.StartNextPlan(today);
        var days = plan.Days.Where(d => d.Date >= today && (d.Status == "Planlandı" || d.Status == "Dinlenme")).Select(d => new DayChoice(d)).ToList();
        if (days.Count == 0) throw new ArgumentException("Düzenlenebilecek gün bulunmuyor.");
        var selector = new ComboBox { ItemsSource = days, SelectedIndex = 0 };
        var panel = Ui.Stack(Ui.Text(template.Name, 24, Ui.Ink, true), Ui.Text(string.Join(" / ", template.Exercises.Select(e => e.Name))), Ui.Field("Hangi gün yapacaksın?", selector, 480));
        var w = Ui.Dialog("Antrenman seç", panel, 590, 420);
        panel.Children.Add(Ui.AsyncButton("Bu antrenmanı seç", () => Run(() => { var day = ((DayChoice)selector.SelectedItem!).Day; service.SetCustomDay(Me, day.Id, template.Exercises, false, template.Name); selectedLogDay = day.Id; w.Close(); ShowPage("Antrenmanım"); }), true));
        await w.ShowDialog(this);
    }
    private Control LibraryPage()
    {
        var search = Ui.Input("", "Hareket adı veya ekipman"); var muscle = Ui.Combo(new[] { "Tüm bölgeler" }.Concat(ExerciseLibrary.All.Select(e => e.Muscle).Distinct()));
        var table = Ui.Table(Array.Empty<LibraryRow>(), ("Hareket", "Name"), ("Kas bölgesi", "Muscle"), ("Ekipman", "Equipment"), ("Seviye", "Level"));
        void Filter() => table.ItemsSource = ExerciseLibrary.All.Where(e => (e.Name + " " + e.Equipment).Contains(search.Text ?? "", StringComparison.OrdinalIgnoreCase) && (Ui.Selected(muscle) == "Tüm bölgeler" || e.Muscle == Ui.Selected(muscle))).Select(e => new LibraryRow(e.Name, e.Muscle, e.Equipment, e.Level)).ToList();
        search.TextChanged += (_, _) => Filter(); muscle.SelectionChanged += (_, _) => Filter(); Filter();
        var ready = new StackPanel { Spacing = 12 };
        foreach (var template in ExerciseLibrary.ReadyWorkouts()) ready.Children.Add(Ui.Card(Ui.Stack(Ui.Text(template.Name, 21, Ui.Ink, true), Ui.Text(string.Join(" / ", template.Exercises.Select(e => e.Name)), 13, Ui.Muted), IsCoach ? Ui.Text("Üyenin program editöründeki şablon listesinden yükleyebilirsin.", 13, Ui.Muted) : Ui.AsyncButton("Bu antrenmanı seç", () => Guard(() => ChooseReadyWorkout(template)), true))));
        var templates = service.MyTemplates(); var templatePanel = new StackPanel { Spacing = 10 };
        foreach (var template in templates) templatePanel.Children.Add(Ui.Card(Ui.Stack(Ui.Text(template.Name, 18, Ui.Ink, true), Ui.Text(string.Join(" / ", template.Exercises.Select(e => e.Name)), 13, Ui.Muted))));
        if (templates.Count == 0) templatePanel.Children.Add(Ui.Text("Bir program gününü düzenlerken şablon olarak kaydedebilirsin.", 14, Ui.Muted));
        return Ui.Page(Ui.Card(Ui.Stack(Ui.Text("Hazır antrenmanlardan seç", 23, Ui.Ink, true), ready)), Ui.Card(Ui.Stack(Ui.Text("26 hareket • Bölge ve ekipman filtresi", 19, Ui.Ink, true), Ui.Row(Ui.Field("Ara", search, 340), Ui.Field("Bölge", muscle, 280)), table)), Ui.Card(Ui.Stack(Ui.Text("Antrenman şablonlarım", 21, Ui.Ink, true), templatePanel)));
    }
    private Control CoachInboxPage()
    {
        var panel = new StackPanel { Spacing = 14 }; var pending = service.PendingReviews();
        foreach (var (user, log) in pending)
        {
            panel.Children.Add(Ui.Card(Ui.Stack(Ui.Row(Ui.UserText(user.Profile.Name, 21, Ui.Ink, true), Ui.Text(log.Date.ToString("dd.MM"), 13, Ui.Muted)), Ui.Text($"Yorgunluk: {log.Fatigue}/10 • Zorluk: {log.Difficulty} • Ağrı bölgesi: {log.Pain}", 15, Ui.Green), Ui.Text(string.IsNullOrWhiteSpace(log.Note) ? "Üye not eklememiş." : log.Note),
                Ui.Row(Ui.Button("Programını incele / düzenle", () => OpenMemberPlan(user), true), Ui.AsyncButton("İncelendi olarak işaretle", () => Run(() => { service.ReviewWorkout(user, log.PlanDayId); Refresh(); }))))));
        }
        foreach (var user in service.ManagedUsers().Where(u => !u.ProfileComplete)) panel.Children.Add(Ui.Card(Ui.Stack(Ui.Text(user.Profile.Name + " • Yeni üye", 20, Ui.Ink, true), Ui.Text("Seni koç seçti. Profil bilgileri henüz tamamlanmamış."), Ui.Button("Üye sayfasını aç", () => OpenMemberPlan(user), true))));
        if (panel.Children.Count == 0) panel.Children.Add(Ui.Card(Ui.Text("Bekleyen inceleme yok. Son 7 gündeki zor, yüksek yorgunluklu veya ağrı bölgesi girilmiş kayıtlar burada görünür.", 15)));
        return Ui.Page(Ui.Stats(Ui.Stat("Bekleyen geri bildirim", pending.Count.ToString()), Ui.Stat("Bugün gelecek", service.ManagedUsers().Count(u => service.AttendanceStatus(u, DateOnly.FromDateTime(DateTime.Today)) == "Geleceğim").ToString())), panel);
    }
    private Control ChangeHistory(User user)
    {
        var panel = new StackPanel { Spacing = 12 }; var changes = service.Changes(user).Take(12).ToList();
        foreach (var change in changes)
        {
            string actor = service.Data.Users.FirstOrDefault(u => u.Id == change.ActorId)?.Profile.Name ?? "Kullanıcı";
            panel.Children.Add(Ui.Card(Ui.Stack(Ui.Text($"{change.Day:dd.MM} • {change.Reason}", 16, Ui.Ink, true), Ui.Text($"{actor} • {change.ChangedAt.ToLocalTime():dd.MM.yyyy HH:mm}", 12, Ui.Muted), Ui.Text("Önce: " + change.Before, 13, Ui.Muted), Ui.Text("Sonra: " + change.After, 13, Ui.Green))));
        }
        if (changes.Count == 0) panel.Children.Add(Ui.Text("Program düzenlendiğinde değişiklikler burada görünecek.", 14, Ui.Muted));
        return Ui.Card(Ui.Stack(Ui.Text("Programım neden değişti?", 21, Ui.Ink, true), panel));
    }
}
