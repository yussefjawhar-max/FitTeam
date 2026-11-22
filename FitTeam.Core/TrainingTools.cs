namespace FitTeam.Core;

public record LibraryExercise(string Name, string Muscle, string Equipment, string Level);
public static class ExerciseLibrary
{
    public static readonly List<LibraryExercise> All =
    [
        new("Duvar / diz üstü şınav", "Göğüs", "Vücut ağırlığı", "Başlangıç"),
        new("Yatarak halter itişi", "Göğüs", "Halter", "Orta"), new("Dambılla göğüs açış", "Göğüs", "Dambıl", "Orta"),
        new("Dambılla sırt çekişi", "Sırt", "Dambıl", "Başlangıç"), new("Yukarıdan sırt çekişi", "Sırt", "Makine", "Orta"),
        new("Oturarak sırt çekişi", "Sırt", "Makine", "Orta"), new("Sandalyeye çömelme", "Bacak", "Vücut ağırlığı", "Başlangıç"),
        new("Kalça köprüsü", "Bacak", "Vücut ağırlığı", "Başlangıç"), new("Parmak ucuna yükselme", "Bacak", "Vücut ağırlığı", "Başlangıç"),
        new("Dambıl tutarak çömelme", "Bacak", "Dambıl", "Orta"), new("Makinede bacak itişi", "Bacak", "Makine", "Orta"),
        new("Dambılla omuz itişi", "Omuz", "Dambıl", "Orta"), new("Yana omuz açış", "Omuz", "Dambıl", "Orta"),
        new("Dambılla kol bükme", "Kol", "Dambıl", "Başlangıç"), new("Makarada arka kol itişi", "Kol", "Makine", "Orta"),
        new("Dirsek üzerinde sabit duruş", "Karın", "Vücut ağırlığı", "Başlangıç"), new("Dört ayak üzerinde çapraz uzanma", "Karın", "Vücut ağırlığı", "Başlangıç"),
        new("Sırtüstü çapraz kol ve bacak uzatma", "Karın", "Vücut ağırlığı", "Başlangıç"), new("Tempolu yürüyüş", "Kardiyo", "Ekipmansız", "Başlangıç"),
        new("Sabit bisiklet", "Kardiyo", "Makine", "Başlangıç"), new("Dinamik ısınma", "Tüm vücut", "Ekipmansız", "Başlangıç"),
        new("Hafif üst vücut hareketliliği", "Üst vücut", "Ekipmansız", "Başlangıç"),
        new("Top sürme ve pas", "Teknik", "Top", "Başlangıç"), new("Reaksiyon ve ayak çalışması", "Teknik", "Top", "Orta"),
        new("Aralıklı koşu", "Bacak", "Ekipmansız", "Orta"), new("Çeviklik parkuru", "Bacak", "Koni", "Orta")
    ];
    public static List<WorkoutTemplate> ReadyWorkouts()
    {
        WorkoutTemplate T(string name, params string[] names) => new() { Name = name, Exercises = names.Select(n => Create(All.First(e => e.Name == n))).ToList() };
        return [T("Evde Tüm Vücut", "Duvar / diz üstü şınav", "Sandalyeye çömelme", "Kalça köprüsü", "Dirsek üzerinde sabit duruş"),
            T("Göğüs ve Sırt", "Yatarak halter itişi", "Dambılla göğüs açış", "Dambılla sırt çekişi", "Yukarıdan sırt çekişi"),
            T("Bacak Günü", "Dambıl tutarak çömelme", "Makinede bacak itişi", "Kalça köprüsü", "Parmak ucuna yükselme"),
            T("Kardiyo ve Kondisyon", "Dinamik ısınma", "Tempolu yürüyüş", "Sabit bisiklet", "Dirsek üzerinde sabit duruş"),
            T("Futbol Teknik ve Çeviklik", "Dinamik ısınma", "Top sürme ve pas", "Çeviklik parkuru", "Aralıklı koşu")];
    }
    public static Exercise Create(LibraryExercise item) => new() { Name = item.Name, Muscle = item.Muscle, Sets = item.Level == "Başlangıç" ? 2 : 3, Reps = 12, Minutes = 8, RestSeconds = 60 };
}
public sealed partial class FitService
{
    private static string Describe(PlanDay day) => day.Title + " • " + day.Status + " • " + string.Join(" / ", day.Exercises.Select(e => $"{e.Name}: {e.Sets}×{e.Reps}, {e.Minutes} dk, {e.RestSeconds} sn"));
    private void TrackChange(User user, PlanDay day, string before, string reason)
    {
        var after = Describe(day);
        if (before == after) return;
        Data.Changes.Add(new ProgramChange { UserId = user.Id, ActorId = RequireCurrent().Id, Day = day.Date, Before = before, After = after, Reason = reason });
    }
    public List<ProgramChange> Changes(User user) { EnsureAccess(user); return Data.Changes.Where(c => c.UserId == user.Id).OrderByDescending(c => c.ChangedAt).ToList(); }
    public List<WorkoutTemplate> MyTemplates() => Data.Templates.Where(t => t.OwnerId == RequireCurrent().Id).ToList();
    public WorkoutTemplate SaveTemplate(string name, List<Exercise> exercises)
    {
        name = name.Trim(); ValidateExercises(exercises, false, name);
        if (name.Length < 2) throw new ArgumentException("Şablon adını girin.");
        var template = new WorkoutTemplate { OwnerId = RequireCurrent().Id, Name = name, Exercises = exercises.Select(e => e.Copy()).ToList() }; Data.Templates.Add(template); store.Save(); return template;
    }
    public WorkoutDraft? Draft(Guid dayId)
    {
        _ = RequireDay(dayId); return Data.Drafts.FirstOrDefault(d => d.UserId == RequireCurrent().Id && d.PlanDayId == dayId);
    }
    public WorkoutDraft StartSession(Guid dayId)
    {
        var user = RequireCurrent(); var day = RequireDay(dayId);
        if (day.Exercises.Count == 0 || day.Date != DateOnly.FromDateTime(DateTime.Today) || (day.Status != "Planlandı" && day.Status != "Eksik")) throw new InvalidOperationException("Canlı antrenman bugünün planlanan veya eksik günü için açılır.");
        var draft = Draft(dayId); if (draft != null) return draft;
        draft = new WorkoutDraft { UserId = user.Id, PlanDayId = dayId, Exercises = day.Exercises.Select(e => new ExerciseResult { Name = e.Name, Muscle = e.Muscle, Sets = 0, Reps = e.Reps }).ToList() }; Data.Drafts.Add(draft); store.Save(); return draft;
    }
    public void SaveDraft(WorkoutDraft draft)
    {
        if(draft.Location is not ("Salon" or "Ev")||!double.IsFinite(draft.ActiveSeconds)||!double.IsFinite(draft.RestRemaining)||draft.ActiveSeconds<0||draft.RestRemaining is <0 or >600)throw new ArgumentException("Antrenman sayaç değerleri geçersiz.");
        if(draft.Exercises.Any(e=>!double.IsFinite(e.GuidedSetSeconds)||!double.IsFinite(e.GuidedActiveSeconds)||e.GuidedSetSeconds<0||e.GuidedActiveSeconds<0))throw new ArgumentException("Hareket süresi geçersiz.");
        var day = RequireDay(draft.PlanDayId);
        if (draft.UserId != RequireCurrent().Id || Data.Drafts.All(d => !ReferenceEquals(d, draft))) throw new InvalidOperationException("Bu antrenman taslağına erişemezsiniz.");
        if (draft.Exercises.Count != day.Exercises.Count || draft.ElapsedSeconds < 0 || draft.Exercises.Where((e, i) => e.Name != day.Exercises[i].Name || e.Sets < 0 || e.Sets > day.Exercises[i].Sets || e.Reps is < 1 or > 100 || e.Minutes < 0 || e.Done != (e.Sets == day.Exercises[i].Sets) || e.LoadKg is < 0 or > 500).Any()) throw new ArgumentException("Taslak değerleri geçersiz.");
        store.Save();
    }
    public List<(User User, WorkoutLog Log)> PendingReviews()
    {
        var result = new List<(User, WorkoutLog)>(); var since = DateOnly.FromDateTime(DateTime.Today).AddDays(-6);
        foreach (var user in ManagedUsers())
            foreach (var log in Logs(user).Where(l => l.Date >= since && l.ReviewedBy == null && (l.Fatigue >= 8 || l.Pain != "Yok" || l.Difficulty == "Zor"))) result.Add((user, log));
        return result.OrderByDescending(x => x.Item2.Date).ToList();
    }
    public void ReviewWorkout(User user, Guid dayId)
    {
        if (!ManagedUsers().Any(u => u.Id == user.Id)) throw new InvalidOperationException("Bu üye sizin listenizde değil.");
        var log = Logs(user).FirstOrDefault(l => l.PlanDayId == dayId) ?? throw new ArgumentException("Antrenman kaydı bulunamadı.");
        log.ReviewedBy = RequireCurrent().Id; store.Save();
    }
    public List<(string Muscle, int Sets)> WeeklyMuscles(User user)
    {
        EnsureAccess(user); var since = DateOnly.FromDateTime(DateTime.Today).AddDays(-6);
        return Logs(user).Where(l => l.Date >= since).SelectMany(l => l.Exercises.Where(e => e.Done)).GroupBy(e => string.IsNullOrWhiteSpace(e.Muscle) ? ExerciseLibrary.All.FirstOrDefault(x => x.Name == e.Name)?.Muscle ?? "Diğer" : e.Muscle).Select(g => (g.Key, g.Sum(e => e.Sets))).OrderByDescending(x => x.Item2).ToList();
    }
}
