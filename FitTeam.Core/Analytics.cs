using System.Globalization;
using System.Net;
using System.Text;
namespace FitTeam.Core;

public record Summary(decimal StartWeight, decimal EndWeight, int Completed, int Missed, int Rest, int ProgressDays, int Planned, int Minutes, int Calories, double AverageHeart, decimal Distance, decimal MatchRating)
{
    public int Percent => Math.Min(100, (int)Math.Round(ProgressDays * 100m / 30));
    public decimal WeightChange => EndWeight - StartWeight;
}
public static class Analytics
{
    public static decimal Bmi(Profile p) => Math.Round(p.Weight / ((p.Height / 100m) * (p.Height / 100m)), 1);
    public static string BmiLabel(Profile p) => p.Age < 18 ? "18 yaş altı için sınıflandırma gösterilmez" : Bmi(p) switch { < 18.5m => "Düşük kilo", < 25m => "Normal aralık", < 30m => "Fazla kilo", _ => "Yüksek vücut kitle indeksi" };
    public static Summary Summarize(FitService service, User user)
    {
        var plan = service.Plan(user); var today = DateOnly.FromDateTime(DateTime.Today);
        var start = plan?.Start ?? today.AddDays(-29); var end = start.AddDays(29);
        var logs = service.Logs(user).Where(l => l.Date >= start && l.Date <= end).ToList();
        var weights = service.Weights(user);
        var baseline = weights.LastOrDefault(w => w.Date <= start)?.Weight ?? weights.FirstOrDefault(w => w.Date <= end)?.Weight ?? user.Profile.InitialWeight;
        var final = weights.LastOrDefault(w => w.Date <= end)?.Weight ?? baseline;
        var matches = service.Matches(user).Where(m => m.Date >= start && m.Date <= end).ToList();
        var days = plan?.Days ?? [];
        int restDone = days.Count(d => d.Status == "Dinlenme" && d.Date <= today);
        return new Summary(baseline, final, logs.Count(l => l.Completed), days.Count(d => d.Status == "Kaçırıldı"), days.Count(d => d.Status == "Dinlenme"), logs.Count(l => l.Completed) + restDone, days.Count(d => d.Status != "Dinlenme"), logs.Sum(l => l.Minutes), logs.Sum(l => l.Calories), logs.Where(l => l.AverageHeart > 0).Select(l => (double)l.AverageHeart).DefaultIfEmpty().Average(), logs.Sum(l => l.Distance), matches.Count > 0 ? matches.Average(m => m.Rating) : 0);
    }
    public static List<string> Badges(FitService service, User user)
    {
        var logs = service.Logs(user).Where(l => l.Completed).ToList(); var result = new List<string>();
        if (logs.Count > 0) result.Add("İlk adım • İlk antrenman tamamlandı");
        if (logs.Count >= 10) result.Add("İstikrar • 10 antrenman tamamlandı");
        int run = 0, longest = 0; DateOnly? prev = null;
        foreach (var date in logs.Select(l => l.Date).Distinct().Order()) { run = prev?.AddDays(1) == date ? run + 1 : 1; longest = Math.Max(longest, run); prev = date; }
        if (longest >= 7) result.Add("7 gün • Aralıksız antrenman serisi");
        if (Math.Abs(user.Profile.Weight - user.Profile.TargetWeight) <= 1) result.Add("Hedefe yakın • 1 kg veya daha az kaldı");
        var weights = service.Weights(user); var last = weights.LastOrDefault(); var week = last == null ? null : weights.LastOrDefault(w => w.Date <= last.Date.AddDays(-7));
        if (week != null && last != null) result.Add($"Haftalık kayıt • {last.Weight - week.Weight:+0.0;-0.0;0.0} kg değişim");
        if (service.WaterToday(user) >= user.Profile.WaterGoal) result.Add("Su hedefi • Bugünkü hedef tamamlandı");
        if (result.Count == 0) result.Add("İlk antrenmanını kaydet, ilk rozetini kazan.");
        return result;
    }
    public static string Report(FitService service, User user)
    {
        var s = Summarize(service, user); var plan = service.Plan(user); var end = plan?.Start.AddDays(29);
        bool ended = end != null && end < DateOnly.FromDateTime(DateTime.Today);
        return $"FitTeam | {(ended ? "30 Gün Sonu Raporu" : "30 Günlük Ara Rapor")}\n" +
        $"Sporcu: {user.Profile.Name}\nRol: {user.Role}\nDönem: {(plan == null ? "Henüz program oluşturulmadı" : $"{plan.Start:dd.MM.yyyy} - {end:dd.MM.yyyy}")}\n\n" +
        $"Başlangıç kilosu: {s.StartWeight:0.0} kg\nSon kayıtlı kilo: {s.EndWeight:0.0} kg\nKilo değişimi: {s.WeightChange:+0.0;-0.0;0.0} kg\n" +
        $"Tamamlanan antrenman: {s.Completed}\nKaçırılan gün: {s.Missed}\nDinlenme günü: {s.Rest}\nToplam süre: {s.Minutes} dakika\n" +
        $"Kaydedilen kalori: {s.Calories} kcal\nOrtalama nabız: {s.AverageHeart:0} BPM\nToplam mesafe: {s.Distance:0.0} km\n" +
        $"Program ilerlemesi: {s.ProgressDays} / 30 gün (%{s.Percent})\nOrtalama maç puanı: {s.MatchRating:0.0}\n" +
        $"Hedefe uzaklık: {Math.Abs(user.Profile.Weight - user.Profile.TargetWeight):0.0} kg\n" +
        $"Genel gelişim: {s.Completed} antrenman kaydedildi; kilo hedefiyle başlangıç farkı {Math.Abs(s.StartWeight - user.Profile.TargetWeight):0.0} kg, son fark {Math.Abs(s.EndWeight - user.Profile.TargetWeight):0.0} kg.\n\n" +
        "İlerleme = tamamlanan antrenman günleri + tarihi gelen dinlenme günleri.\nNabız ortalamasına yalnızca girilen (>0) değerler katılır.\nKalori ve saat değerleri kullanıcı tarafından girilmiştir.\n\n" +
        "Başarılar\n" + string.Join("\n", Badges(service, user)) + "\n\nKoç notu\n" + (string.IsNullOrWhiteSpace(user.CoachNote) ? "Henüz not yok." : user.CoachNote);
    }
    public static string HtmlReport(FitService service, User user) => "<!doctype html><html lang='tr'><meta charset='utf-8'><title>FitTeam Gelişim Raporu</title><style>body{font:16px system-ui;max-width:850px;margin:48px auto;color:#172b43;padding:24px}h1{color:#168578}pre{white-space:pre-wrap;font:16px/1.8 system-ui}@media print{body{margin:0}}</style><h1>FitTeam</h1><pre>" + WebUtility.HtmlEncode(Report(service, user)) + "</pre></html>";
    public static string Csv(FitService service, User user)
    {
        string Escape(string s) => "\"" + s.Replace("\"", "\"\"") + "\"";
        var b = new StringBuilder("Tarih;Tamamlandı;Süre (dk);Kalori;Ort. nabız;Maks. nabız;Adım;Mesafe (km);Yorgunluk;Zorluk;Not\n");
        foreach (var l in service.Logs(user)) b.AppendLine(string.Join(";", l.Date.ToString("yyyy-MM-dd"), l.Completed ? "Evet" : "Hayır", l.Minutes, l.Calories, l.AverageHeart, l.MaxHeart, l.Steps, l.Distance.ToString(CultureInfo.GetCultureInfo("tr-TR")), l.Fatigue, Escape(l.Difficulty), Escape(l.Note)));
        return b.ToString();
    }
}
