using Avalonia.Controls;
using FitTeam.Core;
namespace FitTeam.Desktop.Views;
public partial class MainWindow
{
    private sealed record MemberRow(Guid Id, string Name, string Team, string Intent, string Attendance, int Minutes, decimal Distance, int Calories, int AverageHeart, int MaxHeart, decimal Weight, int Fatigue, decimal Rating, string Completion, string Weekly);
    private List<MemberRow> MemberRows(IEnumerable<User> users) => users.Select(u =>
    {
        var summary = Analytics.Summarize(service, u); var logs = service.Logs(u); var today = DateOnly.FromDateTime(DateTime.Today); var last = logs.LastOrDefault(); var daily = logs.LastOrDefault(l => l.Date == today); var day = service.Plan(u)?.Days.FirstOrDefault(d => d.Date == today);
        string status = daily != null ? daily.Completed ? daily.Location=="Ev"?"Evde tamamladı":"Katıldı" : "Eksik" : day?.Status == "Kaçırıldı" ? "Katılmadı" : day?.Status == "Dinlenme" ? "Dinlenme" : "Kayıt yok";
        var weights = service.Weights(u); var week = weights.LastOrDefault(w => w.Date <= today.AddDays(-7));
        return new MemberRow(u.Id, u.Profile.Name, service.Data.Teams.FirstOrDefault(t => t.Id == u.TeamId)?.Name ?? "-", service.AttendanceStatus(u, today), status, summary.Minutes, summary.Distance, summary.Calories, last?.AverageHeart ?? 0, last?.MaxHeart ?? 0, u.Profile.Weight, last?.Fatigue ?? 0, summary.MatchRating, $"{summary.ProgressDays}/30 (%{summary.Percent})", week == null ? "Kayıt yok" : (u.Profile.Weight - week.Weight).ToString("+0.0;-0.0;0.0") + " kg");
    }).ToList();
    private Control CoachDashboard()
    {
        var members = service.ManagedUsers(); var rows = MemberRows(members); var today = DateOnly.FromDateTime(DateTime.Today);
        var summary = members.Select(u => Analytics.Summarize(service, u)).ToList();
        var table = Ui.Table(rows, ("Sporcu", "Name"), ("Gelecek mi?", "Intent"), ("Katılım", "Attendance"), ("Kilo", "Weight"), ("Yorgunluk¹", "Fatigue"), ("İlerleme", "Completion"), ("Puan", "Rating"));
        var leaders = new List<string>();
        if (members.Count > 0)
        {
            var distance = rows.MaxBy(r => r.Distance)!; var calorie = rows.MaxBy(r => r.Calories)!;
            var regular = members.OrderByDescending(u => { var s = Analytics.Summarize(service, u); return s.Planned == 0 ? 0 : s.Completed / (decimal)s.Planned; }).First();
            var improving = members.MaxBy(u => { var s = Analytics.Summarize(service, u); return Math.Abs(s.StartWeight - u.Profile.TargetWeight) - Math.Abs(s.EndWeight - u.Profile.TargetWeight); })!;
            leaders.Add($"En fazla koşan: {distance.Name} • {distance.Distance:0.0} km"); leaders.Add($"En fazla kalori kaydeden: {calorie.Name} • {calorie.Calories:N0} kcal");
            leaders.Add($"Programa en düzenli katılan: {regular.Profile.Name}"); leaders.Add($"Kilo hedefine en fazla yaklaşan: {improving.Profile.Name}");
        }
        return Ui.Page(Ui.Stats(Ui.Stat("Sporcu sayısı", members.Count.ToString(), Me.Role == Choices.Roles[3] ? service.Data.Teams.Count(t => t.IsActive && t.CoachId == Me.Id) + " takım" : "Sana bağlı üyeler"), Ui.Stat("Bugün gelecek", rows.Count(r => r.Intent == "Geleceğim").ToString(), rows.Count(r => r.Attendance == "Katıldı") + " kişi tamamladı"), Ui.Stat("Tamamlanan antrenman", summary.Sum(s => s.Completed).ToString(), "Sporcuların aktif dönemleri"), Ui.Stat("Toplam mesafe", summary.Sum(s => s.Distance).ToString("0.0") + " km")),
            Ui.Card(Ui.Stack(Ui.Row(Ui.Text("Sporcularına genel bakış", 20, Ui.Ink, true), Ui.Button("Üyeleri yönet", () => ShowPage("Üyeler ve takım"), true), Ui.Button("Geri bildirimler (" + service.PendingReviews().Count + ")", () => ShowPage("Koç gelen kutusu"))), table, Ui.Text("¹ Yorgunluk son antrenman kaydından alınır. Süre ve mesafe aktif 30 günlük dönem toplamıdır.", 12, Ui.Muted))),
            Ui.Card(Ui.Stack(Ui.Text("Oyuncu / üye karşılaştırması", 20, Ui.Ink, true), Ui.Text(leaders.Count == 0 ? "Üye eklediğinde karşılaştırmalar burada görünecek." : string.Join("\n\n", leaders)))));
    }
    private Control MembersPage()
    {
        var teams = service.Data.Teams.Where(t => t.IsActive && t.CoachId == Me.Id).ToList();
        var selectTeam = new ComboBox { ItemsSource = new[] { "Tüm takımlar" }.Concat(teams.Select(t => t.Name)).ToList(), SelectedIndex = 0 };
        var search = Ui.Input("", "Sporcu adı ara"); var table = Ui.Table(Array.Empty<MemberRow>(), ("Sporcu", "Name"), ("Gelecek mi?", "Intent"), ("Katılım", "Attendance"), ("Dk¹", "Minutes"), ("Km¹", "Distance"), ("Kalori¹", "Calories"), ("Ort.²", "AverageHeart"), ("Maks.²", "MaxHeart"), ("Kilo", "Weight"), ("Yorg.²", "Fatigue"), ("Puan¹", "Rating"));
        var detail = new StackPanel { Spacing = 12 };
        void Filter()
        {
            var users = service.ManagedUsers().Where(u => u.Profile.Name.Contains(search.Text ?? "", StringComparison.OrdinalIgnoreCase));
            if (Me.Role == Choices.Roles[3] && selectTeam.SelectedIndex > 0) users = users.Where(u => u.TeamId == teams[selectTeam.SelectedIndex - 1].Id);
            table.ItemsSource = MemberRows(users); detail.Children.Clear();
        }
        table.SelectionChanged += (_, _) =>
        {
            detail.Children.Clear(); if (table.SelectedItem is not MemberRow row) return;
            var user = service.ManagedUsers().First(u => u.Id == row.Id); var summary = Analytics.Summarize(service, user); var note = Ui.Input(user.CoachNote); note.AcceptsReturn = true; note.MinHeight = 90;
            detail.Children.Add(Ui.UserText(user.Profile.Name, 22, Ui.Ink, true));
            detail.Children.Add(Ui.Text($"Kullanıcı adı: {user.Username}  •  {row.Team}\nHaftalık kilo değişimi: {row.Weekly}  •  Program: {row.Completion}\nSon antrenman: {service.Logs(user).LastOrDefault()?.Date.ToString("dd.MM.yyyy") ?? "Kayıt yok"}"));
            detail.Children.Add(Ui.Button("Üyenin antrenman sayfasını aç", () => OpenMemberPlan(user), true));
            detail.Children.Add(Ui.Text("Uygun günler: " + string.Join(", ", user.Profile.AvailableDays.Select(Choices.DayName)), 13, Ui.Muted));
            detail.Children.Add(Ui.Field("Kısa koç notu / öneri", note, 700));
            detail.Children.Add(Ui.Row(Ui.AsyncButton("Notu kaydet", () => Run(() => { service.SetCoachNote(user, note.Text ?? ""); Refresh(); }), true), Ui.AsyncButton("Gelişim raporunu aç", () => Guard(async () => { await Ui.Dialog(user.Profile.Name + " • Rapor", Ui.Text(Analytics.Report(service, user)), 720, 740).ShowDialog(this); })), Ui.AsyncButton("Listeden çıkar", () => Guard(async () => { if (!await Ui.Confirm(this, $"{user.Profile.Name} listenizden çıkarılsın mı? Sporcunun hesabı ve kayıtları korunur.")) return; service.RemoveMember(user); Refresh(); }))));
        };
        search.TextChanged += (_, _) => Filter(); selectTeam.SelectionChanged += (_, _) => Filter(); Filter();
        var controls = Ui.Row(Ui.Field("Ara", search, 280), Ui.Button("Listeyi yenile", Filter));
        if (Me.Role == Choices.Roles[2]) controls.Children.Add(Ui.Text("Seni koç seçen yeni bireysel üyeler burada görünür.", 13, Ui.Muted));
        if (Me.Role == Choices.Roles[3]) { controls.Children.Add(Ui.AsyncButton("+ Oyuncu ekle", () => Guard(AddMemberDialog), true)); controls.Children.Add(Ui.Field("Takım filtresi", selectTeam)); controls.Children.Add(Ui.AsyncButton("+ Takım oluştur", () => Guard(CreateTeamDialog))); }
        return Ui.Page(Ui.Card(Ui.Stack(controls, table, Ui.Text("Gelecek mi? = üyenin bildirimi. Katılım = antrenman kaydı. ¹ Aktif 30 günlük dönem toplamları; puan maçların ortalamasıdır. ² Son antrenmanın nabız ve yorgunluk kaydıdır.", 12, Ui.Muted))), Ui.Card(detail));
    }
    private sealed record TeamChoice(Team Team) { public override string ToString() => Team.Name; }
    private async Task AddMemberDialog()
    {
        var username = Ui.Input("", "Örn. sporcu"); var teams = service.Data.Teams.Where(t => t.IsActive && t.CoachId == Me.Id).Select(t => new TeamChoice(t)).ToList(); var choice = new ComboBox { ItemsSource = teams, SelectedIndex = teams.Count > 0 ? 0 : -1 };
        var panel = Ui.Stack(Ui.Text("Sporcuyu listene ekle", 24, Ui.Ink, true), Ui.Text("Sporcunun önceden hesap oluşturması gerekir. Fitness koçu bireysel kullanıcıları, antrenör futbolcuları ekler."), Ui.Field("Kullanıcı adı", username, 430));
        if (Me.Role == Choices.Roles[3]) panel.Children.Add(Ui.Field("Takım", choice, 430));
        var w = Ui.Dialog("FitTeam • Sporcu ekle", panel, 540, 440);
        panel.Children.Add(Ui.AsyncButton("Ekle", () => Run(() => { service.AssignMember(username.Text ?? "", (choice.SelectedItem as TeamChoice)?.Team.Id); w.Close(); Refresh(); }), true)); await w.ShowDialog(this);
    }
    private async Task CreateTeamDialog()
    {
        var name = Ui.Input(); var gender = Ui.Combo(["Kadın", "Erkek"]); var panel = Ui.Stack(Ui.Text("Yeni takım", 24, Ui.Ink, true), Ui.Field("Takım adı", name, 430), Ui.Field("Takım türü", gender, 430));
        var w = Ui.Dialog("FitTeam • Takım oluştur", panel, 540, 420);
        panel.Children.Add(Ui.AsyncButton("Takımı oluştur", () => Run(() => { service.CreateTeam(name.Text ?? "", Ui.Selected(gender)); w.Close(); Refresh(); }), true)); await w.ShowDialog(this);
    }
}
