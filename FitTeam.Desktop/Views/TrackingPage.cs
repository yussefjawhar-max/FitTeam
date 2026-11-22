using Avalonia.Controls;
using FitTeam.Core;
namespace FitTeam.Desktop.Views;
public partial class MainWindow
{
    private sealed record WeightRow(string Date, decimal Weight, string Change);
    private Control TrackingPage()
    {
        var today = DateOnly.FromDateTime(DateTime.Today); var date = Ui.Date(today, true); var weight = Ui.Number(Me.Profile.Weight, 30, 300, 1);
        var entries = service.Weights(Me); var rows = entries.Select((w, i) => new WeightRow(w.Date.ToString("dd.MM.yyyy"), w.Weight, i == 0 ? "İlk kayıt" : (w.Weight - entries[i - 1].Weight).ToString("+0.0;-0.0;0.0") + " kg"));
        var table = Ui.Table(rows.Reverse(), ("Tarih", "Date"), ("Kilo (kg)", "Weight"), ("Önceki kayda göre", "Change"));
        var water = service.WaterToday(Me); var add = Ui.Number(0.25m, 0.05m, 3, 2);
        var waterProgress = new ProgressBar { Value = Math.Min(100, (double)(water / Me.Profile.WaterGoal * 100)), Height = 12, Foreground = Ui.Green };
        return Ui.Page(Ui.Stats(Ui.Stat("Güncel kilo", $"{Me.Profile.Weight:0.0} kg"), Ui.Stat("Toplam kilo değişimi", $"{Me.Profile.Weight - Me.Profile.InitialWeight:+0.0;-0.0;0.0} kg"), Ui.Stat("Hedefe uzaklık", $"{Math.Abs(Me.Profile.Weight - Me.Profile.TargetWeight):0.0} kg")),
            WeightGoalCard(Me),
            Ui.Card(Ui.Stack(Ui.Text("Kilo kaydı", 20, Ui.Ink, true), Ui.Text("Haftada bir kayıt tutabilirsin. Aynı tarih yeniden kaydedilirse mevcut değer güncellenir.", 13, Ui.Muted), Ui.Row(Ui.Field("Tarih", date), Ui.Field("Kilo (kg)", weight), Ui.AsyncButton("Kilomu kaydet", () => Run(() => { service.SetWeight(Me, Ui.DateValue(date), Ui.Value(weight)); Refresh(); }), true)), table)),
            Ui.Card(Ui.Stack(Ui.Text("Bugünkü su takibi", 20, Ui.Ink, true), Ui.Text($"{water:0.00} / {Me.Profile.WaterGoal:0.0} litre", 28, Ui.Green, true), waterProgress, Ui.Text($"Hedefe kalan: {Math.Max(0, Me.Profile.WaterGoal - water):0.00} litre", 14, Ui.Muted),
                Ui.Row(Ui.AsyncButton("+ 250 ml", () => Run(() => { service.AddWater(0.25m); Refresh(); })), Ui.AsyncButton("+ 500 ml", () => Run(() => { service.AddWater(0.5m); Refresh(); })), Ui.Field("Eklenecek miktar (litre)", add), Ui.AsyncButton("Miktarı ekle", () => Run(() => { service.AddWater(Ui.Value(add)); Refresh(); }), true)))));
    }
}
