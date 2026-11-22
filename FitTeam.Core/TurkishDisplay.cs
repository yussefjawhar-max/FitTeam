namespace FitTeam.Core;
internal static class TurkishDisplay
{
    private static readonly Dictionary<string,string> LegacyExercises=new()
    {
        ["Bench press"]="Yatarak halter itişi",
        ["Dambıl fly"]="Dambılla göğüs açış",
        ["Dambıl row"]="Dambılla sırt çekişi",
        ["Lat pulldown"]="Yukarıdan sırt çekişi",
        ["Seated row"]="Oturarak sırt çekişi",
        ["Sandalyeye squat"]="Sandalyeye çömelme",
        ["Glute bridge"]="Kalça köprüsü",
        ["Calf raise"]="Parmak ucuna yükselme",
        ["Goblet squat"]="Dambıl tutarak çömelme",
        ["Leg press"]="Makinede bacak itişi",
        ["Dambıl omuz press"]="Dambılla omuz itişi",
        ["Lateral raise"]="Yana omuz açış",
        ["Dambıl curl"]="Dambılla kol bükme",
        ["Triceps pushdown"]="Makarada arka kol itişi",
        ["Plank"]="Dirsek üzerinde sabit duruş",
        ["Bird dog"]="Dört ayak üzerinde çapraz uzanma",
        ["Dead bug"]="Sırtüstü çapraz kol ve bacak uzatma",
        ["İnterval koşu"]="Aralıklı koşu",
        ["Hafif üst vücut mobilitesi"]="Hafif üst vücut hareketliliği"
    };
    private static string ExerciseName(string name)=>LegacyExercises.TryGetValue(name,out var turkish)?turkish:name;
    public static void Apply(AppData data)
    {
        // Eski hazır hareket adlarını görüntülemek için çevirir; kullanıcı notlarına dokunmaz.
        foreach(var e in data.Plans.SelectMany(p=>p.Days).SelectMany(d=>d.Exercises.Concat(d.Baseline)).Concat(data.Templates.SelectMany(t=>t.Exercises)))e.Name=ExerciseName(e.Name);
        foreach(var e in data.Workouts.SelectMany(w=>w.Exercises).Concat(data.Drafts.SelectMany(d=>d.Exercises)))e.Name=ExerciseName(e.Name);
    }
}
