namespace FitTeam.Core;
public static class TrainingWindow
{
    public static void Validate(TimeOnly start,int hours)
    {
        if(hours is not (2 or 3)||start.ToTimeSpan()+TimeSpan.FromHours(hours)>TimeSpan.FromDays(1))throw new ArgumentException("Başlangıç saati ve 2 veya 3 saatlik ders aralığını kontrol et.");
    }
    public static string Label(Profile profile)
    {
        if(profile.TrainingStart==null)return "Ders saati henüz seçilmedi.";
        var end=profile.TrainingStart.Value.ToTimeSpan()+TimeSpan.FromHours(profile.TrainingHours);
        return profile.TrainingStart.Value.ToString("HH:mm")+" – "+(end==TimeSpan.FromDays(1)?"24:00":TimeOnly.FromTimeSpan(end).ToString("HH:mm"))+" • "+profile.TrainingHours+" saat";
    }
}
public sealed partial class FitService
{
    public void SetTrainingWindow(TimeOnly start,int hours)
    {
        var user=RequireCurrent();TrainingWindow.Validate(start,hours);user.Profile.TrainingStart=start;user.Profile.TrainingHours=hours;store.Save();
    }
}
