using Avalonia.Controls;
using FitTeam.Core;
namespace FitTeam.Desktop.Views;
public partial class MainWindow
{
    private sealed record MatchRow(string Date, string Opponent, int Minutes, int Goals, int Assists, int Shots, decimal Distance, decimal Rating, string Cards);
    private Control MatchPage()
    {
        var matches = service.Matches(Me);
        var table = Ui.Table(matches.OrderByDescending(m => m.Date).Select(m => new MatchRow(m.Date.ToString("dd.MM"), m.Opponent, m.Minutes, m.Goals, m.Assists, m.Shots, m.Distance, m.Rating, $"{m.Yellow} / {m.Red}")), ("Tarih", "Date"), ("Rakip", "Opponent"), ("Dk", "Minutes"), ("Gol", "Goals"), ("Asist", "Assists"), ("Şut", "Shots"), ("Km", "Distance"), ("Puan", "Rating"), ("Sarı/kırmızı", "Cards"));
        return Ui.Page(Ui.Stats(Ui.Stat("Maç sayısı", matches.Count.ToString()), Ui.Stat("Gol / asist", $"{matches.Sum(m => m.Goals)} / {matches.Sum(m => m.Assists)}"), Ui.Stat("Ortalama puan", (matches.Count == 0 ? 0 : matches.Average(m => m.Rating)).ToString("0.0"), "Kullanıcının girdiği puan / 10")),
            Ui.Card(Ui.Stack(Ui.Row(Ui.Text("Maç kayıtların", 20, Ui.Ink, true), Ui.AsyncButton("+ Maç ekle", () => Guard(AddMatchDialog), true)), table)));
    }
    private async Task AddMatchDialog()
    {
        var date = Ui.Date(DateOnly.FromDateTime(DateTime.Today), true); var opponent = Ui.Input(); var minutes = Ui.Number(90, 0, 130); var goals = Ui.Number(0, 0, 30); var assists = Ui.Number(0, 0, 30); var shots = Ui.Number(0, 0, 100);
        var distance = Ui.Number(0, 0, 30, 2); var calories = Ui.Number(0, 0, 5000); var avg = Ui.Number(0, 0, 250); var max = Ui.Number(0, 0, 250); var yellow = Ui.Number(0, 0, 2); var red = Ui.Number(0, 0, 1); var rating = Ui.Number(7, 0, 10, 1);
        var w = Ui.Dialog("FitTeam • Maç kaydı", new StackPanel(), 780, 750);
        w.Content = new Border { Padding = new Avalonia.Thickness(24), Child = new ScrollViewer { Content = Ui.Stack(Ui.Text("Maç performansını kaydet", 24, Ui.Ink, true), Ui.Row(Ui.Field("Tarih", date), Ui.Field("Rakip takım", opponent), Ui.Field("Oynanan dakika", minutes), Ui.Field("Gol", goals), Ui.Field("Asist", assists), Ui.Field("Şut", shots), Ui.Field("Mesafe (km)", distance), Ui.Field("Kalori", calories), Ui.Field("Ort. nabız", avg), Ui.Field("Maks. nabız", max), Ui.Field("Sarı kart", yellow), Ui.Field("Kırmızı kart", red), Ui.Field("Performans puanı (0-10)", rating)), Ui.AsyncButton("Maçı kaydet", () => Run(() => { service.AddMatch(new MatchLog { Date = Ui.DateValue(date), Opponent = opponent.Text ?? "", Minutes = Ui.Int(minutes), Goals = Ui.Int(goals), Assists = Ui.Int(assists), Shots = Ui.Int(shots), Distance = Ui.Value(distance), Calories = Ui.Int(calories), AverageHeart = Ui.Int(avg), MaxHeart = Ui.Int(max), Yellow = Ui.Int(yellow), Red = Ui.Int(red), Rating = Ui.Value(rating) }); w.Close(); Refresh(); }), true)) } };
        await w.ShowDialog(this);
    }
}
