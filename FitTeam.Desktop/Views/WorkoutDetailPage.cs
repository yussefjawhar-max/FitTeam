using Avalonia.Controls;
using Avalonia.Layout;
using FitTeam.Core;
using FitTeam.Desktop.Controls;
namespace FitTeam.Desktop.Views;
public partial class MainWindow
{
    private sealed record CoachChoice(CoachInfo Coach) { public override string ToString() => Coach.Name; }
    private ComboBox CoachSelector(Guid? selected = null)
    {
        var list = service.Coaches().Select(c => new CoachChoice(c)).ToList();
        return new ComboBox { ItemsSource = list, SelectedItem = list.FirstOrDefault(c => c.Coach.Id == selected), PlaceholderText = "Koçunu seç", HorizontalAlignment = HorizontalAlignment.Stretch };
    }
    private string CoachLabel(User user)
    {
        if (user.Role == Choices.Roles[0]) return string.Join(", ",service.SelectedCoaches(user).Select(c=>c.Name)) is {Length:>0} names ? names : "Henüz hoca seçilmedi";
        var team = service.Data.Teams.FirstOrDefault(t => t.Id == user.TeamId);
        return team == null ? "Henüz takıma bağlı değilsin" : service.Data.Users.FirstOrDefault(u => u.Id == team.CoachId)?.Profile.Name ?? "Antrenör bilgisi yok";
    }
    private List<CheckBox> CoachChecks(IEnumerable<Guid>? selected=null)
    {
        var ids=(selected??[]).ToHashSet();return service.Coaches().Select(c=>new CheckBox {Content=c.Name+" • "+service.Data.Users.Single(user=>user.Id==c.Id).Profile.Age+" "+AppLanguage.Text("yaş"),Tag=c.Id,IsChecked=ids.Contains(c.Id)}).ToList();
    }
    private Control CoachSelectionCard()
    {
        var choices=CoachChecks(FitService.CoachLinks(Me));
        return Ui.Card(Ui.Stack(Ui.Text("Hocalarım",20,Ui.Ink,true),Ui.Text("Birden fazla hoca seçebilirsin. Seçtiğin hocalar kayıtlarını görür ve programını düzenleyebilir. Bir hocanın seçimini kaldırınca o hocanın erişimi kapanır.",13,Ui.Muted),Ui.Stack(choices.ToArray()),Ui.AsyncButton("Hoca seçimlerimi kaydet",()=>Run(()=>{service.SelectCoaches(choices.Where(c=>c.IsChecked==true).Select(c=>(Guid)c.Tag!));Refresh();}),true)));
    }
    private Control AttendanceCard()
    {
        var today = DateOnly.FromDateTime(DateTime.Today); var status = service.AttendanceStatus(Me, today);
        return Ui.Card(Ui.Stack(Ui.Row(Ui.Text("Bugün antrenmana geliyor musun?", 20, Ui.Ink, true), Ui.Text(status, 16, Ui.Green, true)),
            Ui.Text($"{(Me.Role == Choices.Roles[0] ? "Koçun" : "Antrenörün")}: {CoachLabel(Me)}", 13, Ui.Muted),Ui.Text(TrainingWindow.Label(Me.Profile),15,Ui.Green,true),
            Ui.Row(Ui.AsyncButton(status == "Geleceğim" ? "✓ Bugün geleceğim" : "Bugün geleceğim", () => Run(() => { service.SetAttendance(true); Refresh(); }), true),
                Ui.AsyncButton(status == "Gelmeyeceğim" ? "✓ Bugün gelmeyeceğim" : "Bugün gelmeyeceğim", () => Run(() => { service.SetAttendance(false); Refresh(); }))),
            Ui.Text("Bu bildirim koçunun listesinde görünür. Antrenmanı tamamlamak için ayrıca antrenman kaydı gir.", 12, Ui.Muted)));
    }
    private Control WorkoutDetailPage()
    {
        var plan = service.Plan(Me); var today = DateOnly.FromDateTime(DateTime.Today);
        if (plan == null) return Ui.Page(AttendanceCard(), Ui.Card(Ui.Stack(Ui.Text(Me.Profile.Name + " • Antrenman sayfam", 24, Ui.Ink, true), Ui.Text("Henüz bir programın yok. Profilini tamamlayıp koçuna bildirebilir veya kendin program oluşturabilirsin."), Ui.Row(Ui.Button("Profilimi doldur", () => ShowPage("Profilim")), Ui.Button("Program oluştur", () => ShowPage("30 günlük program"), true)))));
        var choices = plan.Days.Select(d => new DayChoice(d)).ToList(); var selector = new ComboBox { ItemsSource = choices, SelectedItem = choices.FirstOrDefault(c => c.Day.Id == selectedLogDay) ?? choices.FirstOrDefault(c => c.Day.Date == today) ?? choices[0] };
        var details = new StackPanel { Spacing = 16 };
        void Render()
        {
            details.Children.Clear(); if (selector.SelectedItem is not DayChoice selected) return;
            var day = selected.Day; var log = service.Logs(Me).FirstOrDefault(l => l.PlanDayId == day.Id);
            details.Children.Add(Ui.Text(day.Title, 26, Ui.Ink, true));
            details.Children.Add(Ui.Text($"{day.Date:dd MMMM yyyy} • {Choices.DayName(day.Date.DayOfWeek)} • {day.Status}", 14, Ui.Muted));
            if (!string.IsNullOrWhiteSpace(day.Adjustment)) details.Children.Add(Ui.Text(day.Adjustment, 14, Ui.Green));
            if (day.Exercises.Count == 0) details.Children.Add(Ui.Card(Ui.Text("Bu gün dinlenme için ayrılmış. Gelmeyi planlıyorsan bugünkü bildirimini gönder; koçun günlük programını düzenleyebilir.", 15)));
            int n = 0;
            foreach (var e in day.Exercises)
            {
                var result = log?.Exercises.FirstOrDefault(r => r.Name == e.Name);
                details.Children.Add(Ui.Card(Ui.Stack(Ui.Row(Ui.Text($"{++n:00}", 24, Ui.Green, true), Ui.Text(e.Name, 21, Ui.Ink, true)), Ui.Text(e.Muscle, 13, Ui.Muted),new ExerciseIllustration{Movement=ExerciseGuides.For(e.Name)?.Movement??"",Height=220},Ui.Text(ExerciseGuides.For(e.Name)?.Instructions??"Bu özel hareket için antrenörünün açıklamasını kullan.",14,Ui.Muted),
                    Ui.Row(Ui.Text($"{e.Sets} set × {e.Reps} tekrar", 16, Ui.Ink, true), Ui.Text($"{e.Minutes} dakika", 16), Ui.Text($"{e.RestSeconds} sn dinlenme", 16)),
                    Ui.Text(result == null ? "Antrenman sonrası yaptığın set ve tekrarları kaydet." : result.Done ? $"✓ Yapıldı • {result.Sets} set × {result.Reps} tekrar • {result.Minutes} dk" : "Henüz tamamlanmadı", 13, result?.Done == true ? Ui.Green : Ui.Muted))));
            }
            if (day.Exercises.Count > 0 && day.Date == today && day.Status is "Planlandı" or "Eksik") details.Children.Add(Ui.Button(service.Draft(day.Id) == null ? "▶ Antrenmana başla" : "▶ Antrenmana devam et", () => OpenSession(day.Id,service.Draft(day.Id)?.Location=="Ev"), true));
            if(day.Exercises.Count>0&&day.Date==today&&day.Status is "Planlandı" or "Eksik")details.Children.Add(Ui.Button("Evde antrenman yap",()=>OpenSession(day.Id,true),true));
            if (day.Exercises.Count > 0) details.Children.Add(Ui.Row(Ui.Button("Antrenman sonucumu kaydet", () => OpenWorkoutLog(day.Id), true), Ui.AsyncButton("Bu antrenmanı düzenle", () => Guard(async () => { await EditDay(day); }))));
        }
        selector.SelectionChanged += (_, _) => Render(); Render();
        return Ui.Page(AttendanceCard(), Ui.Card(Ui.Stack(Ui.Text(Me.Profile.Name + " • Antrenman sayfam", 22, Ui.Ink, true), Ui.Row(Ui.Field("Antrenman günü", selector, 700), Ui.Button("Hazır antrenman seç", () => ShowPage("Egzersiz kütüphanesi"))), details)),
            Ui.Card(Ui.Stack(Ui.Text("Son koç notu", 18, Ui.Ink, true), Ui.Text(string.IsNullOrWhiteSpace(Me.CoachNote) ? "Henüz not yok." : Me.CoachNote))), ChangeHistory(Me));
    }
    private Guid? selectedLogDay;
    private void OpenWorkoutLog(Guid id) { selectedLogDay = id; ShowPage("Antrenman kaydı"); }
    private Control MemberPlanIntro(User user)
    {
        var boxes = Choices.Days.Select(d => new CheckBox { Content = Choices.DayName(d), IsChecked = user.Profile.AvailableDays.Contains(d), Tag = d }).ToList();
        var plan = service.Plan(user); var today = DateOnly.FromDateTime(DateTime.Today);
        var panel = Ui.Stack(Ui.Row(Ui.Button("← Üyelere dön", () => ShowPage("Üyeler ve takım")), Ui.UserText(user.Profile.Name, 22, Ui.Ink, true)),
            Ui.Text($"Bugünkü bildirim: {service.AttendanceStatus(user, today)} • Profil: {(user.ProfileComplete ? "Tamamlandı" : "Bilgiler eksik")}", 14, Ui.Green),
            Ui.Text("Hangi günler gelecek?", 18, Ui.Ink, true), Ui.Row(boxes.ToArray()),
            Ui.Row(Ui.AsyncButton("Günleri programa uygula", () => Run(() => { service.SetAvailableDays(user, boxes.Where(b => b.IsChecked == true).Select(b => (DayOfWeek)b.Tag!).ToList()); Refresh(); }), true),
                Ui.AsyncButton("Üye bilgilerini düzenle", () => Guard(async () => { await Ui.Dialog(user.Profile.Name + " • Profil", ProfilePage(user), 1000, 800).ShowDialog(this); Refresh(); }))),
            Ui.Text("Seçim, gelecekteki otomatik günleri düzenler. Kayıtlı, taşınmış ve özel hazırlanmış antrenmanlar korunur.", 12, Ui.Muted));
        if (plan != null)
        {
            var day = plan.Days.FirstOrDefault(d => d.Date >= today && (d.Status == "Planlandı" || d.Status == "Dinlenme"));
            if (day != null) panel.Children.Add(Ui.AsyncButton("+ Özel antrenman sayfası oluştur", () => Guard(async () => { await EditDay(day, user); }), true));
        }
        return Ui.Card(panel);
    }
}
