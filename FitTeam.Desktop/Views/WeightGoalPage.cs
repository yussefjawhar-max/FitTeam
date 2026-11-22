using Avalonia.Controls;
using FitTeam.Core;
namespace FitTeam.Desktop.Views;

public partial class MainWindow
{
    private Control WeightGoalCard(User user, bool showRecordButton = false)
    {
        var weights = service.HasAdministrationAccess ? service.Data.Weights.Where(w => w.UserId == user.Id).ToList() : service.Weights(user);
        var estimate = WeightGoalForecast.Calculate(user.Profile, weights, DateOnly.FromDateTime(DateTime.Today));
        decimal baseline = user.Profile.GoalStartWeight ?? user.Profile.InitialWeight;
        var title = user.Profile.TargetWeight > baseline ? "Kilo alma hedefi"
            : user.Profile.TargetWeight < baseline ? "Kilo verme hedefi" : "Kilo hedefi";
        var body = Ui.Stack(Ui.Text("Hedefime ne zaman ulaşırım?", 22, Ui.Ink, true), Ui.Text(title, 14, Ui.Green, true),
            Ui.Stats(Ui.Stat("Son kayıtlı kilo", $"{estimate.Current:0.0} kg"), Ui.Stat("Hedef kilo", $"{estimate.Target:0.0} kg"),
                Ui.Stat("Hedefe kalan", $"{estimate.Remaining:0.0} kg")),
            new ProgressBar { Minimum = 0, Maximum = 100, Value = (double)estimate.Progress, Height = 12, Foreground = Ui.Green },
            Ui.Text($"Hedef ilerlemesi: %{estimate.Progress:0}", 13, Ui.Muted));
        if (estimate.State == WeightGoalState.Estimated)
        {
            body.Children.Add(Ui.Text("Tahmini hedef tarihi", 14, Ui.Muted));
            body.Children.Add(Ui.Text(estimate.TargetDate!.Value.ToString("dd MMMM yyyy"), 28, Ui.Green, true));
            body.Children.Add(Ui.Text($"Yaklaşık {decimal.Ceiling(estimate.DaysRemaining!.Value / 7m):0} hafta ({estimate.DaysRemaining} gün) kaldı.", 17, Ui.Ink, true));
        }
        else body.Children.Add(Ui.Text(estimate.State switch
        {
            WeightGoalState.Reached => "Kayıtlarına göre kilo hedefine ulaştın.",
            WeightGoalState.MoreRecordsNeeded => "Tarih hesaplamak için en az 7 gün arayla 2 kilo kaydı ekle.",
            WeightGoalState.OldRecords => "Son kilo kaydın 14 günden eski. Yeni tartı kaydıyla tahmini güncelle.",
            WeightGoalState.Stable => "Kayıtlarında belirgin bir kilo değişimi yok; hedef tarihi henüz hesaplanamıyor.",
            WeightGoalState.MovingAway => "Son kilo değişimin hedefinle ters yönde; bu hızla hedef tarihi hesaplanamıyor.",
            WeightGoalState.Under18 => "18 yaş altı için kilo hedef tarihi hesaplanmaz.",
            WeightGoalState.NeedsSpecialCare => "Özel beslenme durumunda otomatik kilo hedef tarihi hesaplanmaz.",
            _ => "Mevcut kayıtlarla bir yıl içinde hedef tarihi hesaplanamıyor. Yeni kayıtlarla tekrar güncellenir."
        }, 16, Ui.Ink, true));
        if (estimate.WeeklyChange != null)
            body.Children.Add(Ui.Text($"Kayıtlarına göre haftalık değişim: {estimate.WeeklyChange:+0.00;-0.00;0.00} kg", 14, Ui.Muted));
        body.Children.Add(Ui.Text("Son 56 gündeki tartı kayıtlarından hesaplanır. Aynı hızın süreceğini varsayan bir tahmindir; yeni kayıtlarla değişir.", 12, Ui.Muted));
        if (showRecordButton && user.Id == Me.Id) body.Children.Add(Ui.Button("Kilo kaydı ekle", () => ShowPage("Kilo ve su takibi"), true));
        return Ui.Card(body);
    }
}
