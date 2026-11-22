using Avalonia.Controls;
using FitTeam.Core;
using FitTeam.Desktop.Controls;
namespace FitTeam.Desktop.Views;
public partial class MainWindow
{
    private sealed record UserChoice(User User) { public override string ToString() => User.Profile.Name; }
    private ComboBox UserSelector()
    {
        var users = IsCoach ? service.ManagedUsers() : [Me];
        return new ComboBox { ItemsSource = users.Select(u => new UserChoice(u)).ToList(), SelectedIndex = users.Count > 0 ? 0 : -1, MinWidth = 280 };
    }
    private Control ChartsPage()
    {
        var user = UserSelector(); var metrics = Ui.Combo(["Kilo", "Yakılan kalori", "Antrenman süresi", "Ortalama kalp atışı", "Koşulan mesafe", "Tamamlanan antrenman sayısı", "Maç performansı", "Haftalık kas dağılımı", "Antrenman takip puanı"]); var chart = new TrendChart(); var description = Ui.Text("", 13, Ui.Muted);
        void Render()
        {
            chart.Points = []; description.Text = "";
            if (user.SelectedItem is not UserChoice selection) { description.Text = "Önce üyelerini / oyuncularını ekle."; chart.InvalidateVisual(); return; }
            var logs = service.Logs(selection.User); var metric = Ui.Selected(metrics);
            chart.Unit = metric switch { "Kilo" => "kg", "Yakılan kalori" => "kcal", "Antrenman süresi" => "dk", "Ortalama kalp atışı" => "BPM", "Koşulan mesafe" => "km", "Maç performansı" => "puan / 10", "Haftalık kas dağılımı" => "set", "Antrenman takip puanı" => "dk × yorgunluk", _ => "antrenman" };
            if (metric == "Haftalık kas dağılımı") chart.Points = service.WeeklyMuscles(selection.User).Select(x => (x.Muscle, (double)x.Sets)).ToList();
            else if (metric == "Antrenman takip puanı") chart.Points = logs.Select(l => (l.Date.ToString("dd.MM"), (double)(l.Minutes * l.Fatigue))).ToList();
            else if (metric == "Kilo") chart.Points = service.Weights(selection.User).Select(w => (w.Date.ToString("dd.MM"), (double)w.Weight)).ToList();
            else if (metric == "Maç performansı") chart.Points = service.Matches(selection.User).Select(m => (m.Date.ToString("dd.MM"), (double)m.Rating)).ToList();
            else if (metric == "Tamamlanan antrenman sayısı") chart.Points = logs.Where(l => l.Completed).Select((l, i) => (l.Date.ToString("dd.MM"), (double)(i + 1))).ToList();
            else chart.Points = logs.Where(l => metric != "Ortalama kalp atışı" || l.AverageHeart > 0).Select(l => (l.Date.ToString("dd.MM"), metric switch { "Yakılan kalori" => (double)l.Calories, "Antrenman süresi" => l.Minutes, "Ortalama kalp atışı" => l.AverageHeart, _ => (double)l.Distance })).ToList();
            description.Text = chart.Points.Count == 0 ? "Bu ölçüm için henüz kayıt bulunmuyor." : $"{selection.User.Profile.Name} • {chart.Points.Count} kayıt • Tüm kayıt geçmişi";
            chart.InvalidateVisual();
        }
        user.SelectionChanged += (_, _) => Render(); metrics.SelectionChanged += (_, _) => Render(); Render();
        return Ui.Page(Ui.Card(Ui.Stack(Ui.Row(Ui.Field("Sporcu", user, 320), Ui.Field("Ölçüm", metrics, 320)), chart, description)), Ui.Card(Ui.Stack(Ui.Text("Grafikleri nasıl okuyabilirsin?", 18, Ui.Ink, true), Ui.Text("Kilo grafiği tarihli tartı kayıtlarından oluşur. Kalori, süre, nabız ve mesafe antrenman kayıtlarından gelir. Antrenman sayısı birikimli gösterilir; maç puanı futbolcunun maç kayıtlarından alınır. Haftalık kas dağılımı son 7 gündeki tamamlanan hareketlerin setlerini toplar. Takip puanı, süre × girilen yorgunluk değeridir; tıbbi değerlendirme değildir. Yatay eksendeki noktalar kayıt sırasına göre eşit aralıklıdır.", 14, Ui.Muted))));
    }
    private Control ReportsPage()
    {
        var user = UserSelector(); var text = Ui.Text("", 15); text.LineHeight = 26; var panel = new StackPanel { Spacing = 12 };
        void Render()
        {
            panel.Children.Clear();
            if (user.SelectedItem is not UserChoice selection) { panel.Children.Add(Ui.Text("Rapor için önce bir sporcu ekle.")); return; }
            text.Text = Analytics.Report(service, selection.User); panel.Children.Add(text);
            panel.Children.Add(Ui.Row(Ui.AsyncButton("Raporu sayfa biçiminde kaydet", () => Guard(() => SaveFile("Gelişim raporunu kaydet", "html", Analytics.HtmlReport(service, selection.User))), true), Ui.AsyncButton("Metin raporunu kaydet", () => Guard(() => SaveFile("Metin raporunu kaydet", "txt", Analytics.Report(service, selection.User)))), Ui.AsyncButton("Antrenmanları tablo dosyasına aktar", () => Guard(() => SaveFile("Antrenmanları dışa aktar", "csv", Analytics.Csv(service, selection.User))))));
        }
        user.SelectionChanged += (_, _) => Render(); Render();
        return Ui.Page(Ui.Card(Ui.Field("Raporlanacak sporcu", user, 400)), Ui.Card(panel), Ui.Card(Ui.Stack(Ui.Text("Yerel veri yedeği", 18, Ui.Ink, true), Ui.Text("Yedek tüm hesapları ve spor kayıtlarını içerir. Uygulama kapalıyken bu dosyayı veri dosyasının yerine koyarak geri yükleyebilirsin.", 13, Ui.Muted), Ui.Text(new DataPathDisplay().Path, 12, Ui.Muted), Ui.AsyncButton("Veri yedeğini kaydet", () => Guard(async () =>
        {
            var file = await StorageProvider.SaveFilePickerAsync(new Avalonia.Platform.Storage.FilePickerSaveOptions { Title = "FitTeam veri yedeği", SuggestedFileName = "FitTeam_Yedek.json", DefaultExtension = "json" });
            if (file == null) return; var local = file.Path.LocalPath; service.Backup(local); await Ui.Message(this, "Yedek kaydedildi.");
        })))));
    }
    private sealed class DataPathDisplay
    {
        public string Path => System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FitTeam", "fitteam.json");
    }
}
