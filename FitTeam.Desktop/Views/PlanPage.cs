using Avalonia;
using Avalonia.Controls;
using FitTeam.Core;
namespace FitTeam.Desktop.Views;
public partial class MainWindow
{
    private sealed record PlanRow(Guid Id, string Date, string Day, string Title, string Status, string Minutes, string Note);
    private Control PlanPage(User? targetUser = null)
    {
        var user = targetUser ?? Me; var coaching = user.Id != Me.Id; service.EnsureAccess(user);
        var plan = service.Plan(user); var start = Ui.Date(DateOnly.FromDateTime(DateTime.Today));
        var generate = Ui.AsyncButton(plan == null ? "30 günlük program oluştur" : "Yeni program oluştur", () => Guard(async () =>
        {
            if (plan != null && !await Ui.Confirm(this, "Mevcut programın yerine yenisi oluşturulacak. Kayıtlı antrenmanlar korunur; aktif dönemde kayıt varsa program yenilenemez. Devam edilsin mi?")) return;
            if (plan != null && plan.Start.AddDays(29) < DateOnly.FromDateTime(DateTime.Today)) service.StartNextPlan(user, Ui.DateValue(start)); else service.GeneratePlan(user, Ui.DateValue(start)); Refresh();
        }), true);
        var header = Ui.Card(Ui.Stack(Ui.Text("Programını oluştur", 18, Ui.Ink, true), Ui.Row(Ui.Field("Başlangıç tarihi", start), generate), Ui.Text("Profilindeki hedef, yaş, vücut kitle indeksi, seviye ve uygun günler kullanılır. Seçilmeyen günler dinlenme olarak planlanır.", 13, Ui.Muted)));
        if (plan == null) return Ui.Page(coaching ? MemberPlanIntro(user) : Ui.Card(Ui.Text("Antrenman günlerini Profilim ekranında seçebilirsin.")), header, Ui.Card(Ui.Text("Önce sporcu profilini doldur, ardından program oluştur.")));
        var table = Ui.Table(plan.Days.Select(d => new PlanRow(d.Id, d.Date.ToString("dd.MM.yyyy"), Choices.DayName(d.Date.DayOfWeek), d.Title, d.Status, d.Exercises.Sum(e => e.Minutes) + " dk", d.Adjustment)), ("Tarih", "Date"), ("Gün", "Day"), ("Bölge", "Title"), ("Durum", "Status"), ("Süre", "Minutes"));
        var detail = new StackPanel { Spacing = 10 };
        void Detail()
        {
            detail.Children.Clear(); if (table.SelectedItem is not PlanRow selected) return;
            var d = plan.Days.First(x => x.Id == selected.Id);
            detail.Children.Add(Ui.Text(d.Date.ToString("dd MMMM") + " • " + d.Title, 20, Ui.Ink, true));
            if (!string.IsNullOrEmpty(d.Adjustment)) detail.Children.Add(Ui.Text(d.Adjustment, 13, Ui.Green));
            foreach (var e in d.Exercises) detail.Children.Add(Ui.Text($"{e.Name}  •  {e.Sets} set × {e.Reps} tekrar  •  {e.Minutes} dk  •  {e.RestSeconds} sn dinlenme  •  {e.Muscle}"));
            if (d.Exercises.Count == 0) detail.Children.Add(Ui.Text("Bu gün dinlenme için ayrıldı.", 14, Ui.Muted));
        }
        table.SelectionChanged += (_, _) => Detail();
        table.SelectedIndex = Math.Max(0, plan.Days.FindIndex(d => d.Date == DateOnly.FromDateTime(DateTime.Today)));
        Detail();
        Guid Selection() => (table.SelectedItem as PlanRow)?.Id ?? throw new ArgumentException("Tablodan bir gün seçin.");
        var actions = Ui.Row(Ui.AsyncButton(coaching ? "Günün antrenmanını hazırla" : "Kendi programımı düzenle", () => Guard(async () => { await EditDay(plan.Days.First(d => d.Id == Selection()), user); })),
            Ui.AsyncButton("Gelemedim • başka güne taşı", () => Guard(async () =>
            {
                var id = Selection(); if (!await Ui.Confirm(this, "Seçili antrenman kaçırıldı olarak kaydedilecek ve uygun boş güne taşınacak. Devam edilsin mi?")) return;
                var date = service.MissDay(id, true); Refresh(); await Ui.Message(this, $"Antrenman {date:dd.MM.yyyy} tarihine taşındı.");
            })),
            Ui.AsyncButton("Yalnızca kaçırıldı kaydet", () => Guard(async () => { var id = Selection(); if (!await Ui.Confirm(this, "Bu gün kaçırıldı olarak kaydedilsin mi?")) return; service.MissDay(id, false); Refresh(); })),
            Ui.Button("Antrenman kaydı", () => ShowPage("Antrenman kaydı"), true));
        if (coaching) { actions.Children.RemoveAt(actions.Children.Count - 1); actions.Children.RemoveAt(2); actions.Children.RemoveAt(1); }
        else actions.Children.Add(Ui.Button("Antrenman sayfamı aç", () => ShowPage("Antrenmanım")));
        return Ui.Page(coaching ? MemberPlanIntro(user) : Ui.Card(Ui.Text("Takvimden bir gün seçerek antrenmanını düzenleyebilirsin.")), header, Ui.Card(Ui.Stack(Ui.Text($"{plan.Start:dd.MM.yyyy} - {plan.Start.AddDays(29):dd.MM.yyyy}", 18, Ui.Ink, true), table, actions)), Ui.Card(detail), ChangeHistory(user));
    }
    private async Task EditDay(PlanDay day, User? targetUser = null)
    {
        var user = targetUser ?? Me;
        var title = Ui.Input(day.Status == "Dinlenme" ? "Özel antrenman" : day.Title);
        var repeat = new CheckBox { Content = $"Dönemde kalan tüm {Choices.DayName(day.Date.DayOfWeek)} günlerine uygula" };
        var exercises = day.Exercises.Select(e => e.Copy()).ToList(); var rest = new CheckBox { Content = "Bu gün dinlenme olsun", IsChecked = day.Status == "Dinlenme" };
        var templates = new ComboBox { ItemsSource = TemplateOptions().Select(t => new TemplateChoice(t)).ToList(), PlaceholderText = "Kayıtlı şablon seç", MinWidth = 300 };
        var editor = new StackPanel { Spacing = 12 };
        var w = Ui.Dialog("Kişisel program • " + day.Date.ToString("dd.MM"), new StackPanel(), 840, 780);
        void Render()
        {
            editor.Children.Clear();
            foreach (var e in exercises.ToArray())
            {
                var name = Ui.Input(e.Name); name.TextChanged += (_, _) => e.Name = name.Text ?? "";
                var muscle = Ui.Combo(["Göğüs", "Sırt", "Bacak", "Omuz", "Kol", "Karın", "Kardiyo", "Teknik", "Tüm vücut", "Üst vücut"], e.Muscle); muscle.SelectionChanged += (_, _) => e.Muscle = Ui.Selected(muscle);
                var sets = Ui.Number(e.Sets, 1, 20); sets.ValueChanged += (_, _) => e.Sets = Ui.Int(sets);
                var reps = Ui.Number(e.Reps, 1, 100); reps.ValueChanged += (_, _) => e.Reps = Ui.Int(reps);
                var minutes = Ui.Number(e.Minutes, 1, 180); minutes.ValueChanged += (_, _) => e.Minutes = Ui.Int(minutes);
                var pause = Ui.Number(e.RestSeconds, 0, 600); pause.ValueChanged += (_, _) => e.RestSeconds = Ui.Int(pause);
                editor.Children.Add(Ui.Card(Ui.Stack(Ui.Row(Ui.Field("Hareket adı", name, 320), Ui.Field("Kas bölgesi", muscle, 180)), Ui.Row(Ui.Field("Set", sets, 115), Ui.Field("Tekrar", reps, 115), Ui.Field("Süre (dk)", minutes, 115), Ui.Field("Dinlenme (sn)", pause, 115), Ui.Button("Sil", () => { exercises.Remove(e); Render(); })))));
            }
        }
        Render();
        w.Content = new Border { Padding = new Thickness(24), Child = new ScrollViewer { Content = Ui.Stack(Ui.Text("Seçili günün hareketlerini düzenle", 22, Ui.Ink, true), Ui.Field("Antrenman adı", title, 700), Ui.Row(Ui.AsyncButton("Kütüphaneden hareket ekle", () => Guard(async () => { var list = await PickExercises(); if (list != null) { exercises.AddRange(list); rest.IsChecked = false; Render(); } })), templates, Ui.Button("Şablonu yükle", () => { if (templates.SelectedItem is TemplateChoice t) { exercises.Clear(); exercises.AddRange(t.Template.Exercises.Select(e => e.Copy())); title.Text = t.Template.Name; rest.IsChecked = false; Render(); } })), rest, repeat, editor,
            Ui.Button("+ Hareket ekle", () => { exercises.Add(new Exercise { Name = "Yeni hareket", Muscle = "Tüm vücut" }); rest.IsChecked = false; Render(); }),
            Ui.AsyncButton("Şablon olarak sakla", () => Run(() => { service.SaveTemplate(title.Text ?? "", exercises); templates.ItemsSource = TemplateOptions().Select(t => new TemplateChoice(t)).ToList(); })),
            Ui.AsyncButton("Programı kaydet", () => Guard(() => { if (repeat.IsChecked == true) service.SetWeeklyTemplate(user, day.Date.DayOfWeek, exercises, rest.IsChecked == true, title.Text); else service.SetCustomDay(user, day.Id, exercises, rest.IsChecked == true, title.Text); w.Close(); Refresh(); return Task.CompletedTask; }), true)) } };
        await w.ShowDialog(this);
    }
}
