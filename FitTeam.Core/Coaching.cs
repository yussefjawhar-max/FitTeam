namespace FitTeam.Core;

public sealed partial class FitService
{
    public User RegisterContact(string contact, string password, string name, string role, Guid? coachId)
    {
        contact = contact.Trim(); string email = "", phone = "";
        if (contact.Contains('@'))
        {
            if (!System.Net.Mail.MailAddress.TryCreate(contact, out var address) || address.Address != contact || !address.Host.Contains('.')) throw new ArgumentException("Geçerli bir e-posta adresi girin.");
            email = contact.ToLowerInvariant(); contact = email;
        }
        else
        {
            phone = System.Text.RegularExpressions.Regex.Replace(contact, @"[\s()-]", "");
            if (!System.Text.RegularExpressions.Regex.IsMatch(phone, @"^\+[1-9][0-9]{8,14}$")) throw new ArgumentException("Telefonu ülke koduyla girin; örneğin +905551234567.");
            contact = phone;
        }
        var user = Register(contact, password, name, role, coachId);
        user.Email = email; user.Phone = phone; store.Save(); return user;
    }
    // Koç seçimi üyeye aittir. Eski koç yeni koça geçen üyeyi göremez.
    public List<CoachInfo> Coaches() => Data.Users.Where(u => u.IsActive && u.Role == Choices.Roles[2]).Select(u => new CoachInfo(u.Id, u.Profile.Name)).ToList();
    public static List<Guid> CoachLinks(User user) => user.CoachIds.Count > 0 ? user.CoachIds.Distinct().ToList() : user.CoachId.HasValue ? [user.CoachId.Value] : [];
    public List<CoachInfo> SelectedCoaches(User user)
    {
        EnsureAccess(user); var ids=CoachLinks(user);return Coaches().Where(c=>ids.Contains(c.Id)).ToList();
    }
    public CoachInfo? SelectedCoach(User user) => SelectedCoaches(user).FirstOrDefault();
    public void SelectCoach(Guid coachId) => SelectCoaches([coachId]);
    public void SelectCoaches(IEnumerable<Guid> coachIds)
    {
        var user=RequireCurrent();if(user.Role!=Choices.Roles[0])throw new InvalidOperationException("Hoca seçimi bireysel kullanıcılar içindir.");
        var ids=coachIds.Distinct().ToList();
        if(ids.Count==0 || ids.Any(id=>!Coaches().Any(c=>c.Id==id)))throw new ArgumentException("En az bir geçerli fitness hocası seçin.");
        user.CoachIds=ids;user.CoachId=ids[0];store.Save();
    }
    public User AddCoach(string name,string email,string specialty)
    {
        RequireAdministrator();
        if(name.Trim().Length<2 || !System.Net.Mail.MailAddress.TryCreate(email.Trim(),out var address) || address.Address!=email.Trim())throw new ArgumentException("Hocanın adını ve e-posta adresini kontrol edin.");
        if(Data.Users.Any(u=>u.Email.Equals(email.Trim(),StringComparison.OrdinalIgnoreCase)))throw new ArgumentException("Bu e-posta hesabı zaten kayıtlı.");
        var coach=new User {Username=email.Trim().ToLowerInvariant(),Email=email.Trim().ToLowerInvariant(),Role=Choices.Roles[2],CoachSpecialty=specialty.Trim(),Profile=new Profile {Name=name.Trim()}};
        Data.Users.Add(coach);store.Save();return coach;
    }
    public string AttendanceStatus(User user, DateOnly date)
    {
        EnsureAccess(user);
        var entry = Data.Attendance.FirstOrDefault(a => a.UserId == user.Id && a.Date == date);
        return entry == null ? "Bildirmedi" : entry.Coming ? "Geleceğim" : "Gelmeyeceğim";
    }
    public void SetAttendance(bool coming, DateOnly? date = null)
    {
        var user = RequireCurrent(); var today = DateOnly.FromDateTime(DateTime.Today); var when = date ?? today;
        if (user.Role != Choices.Roles[0] && user.Role != Choices.Roles[1]) throw new InvalidOperationException("Katılım bildirimi sporcular içindir.");
        if (when != today) throw new ArgumentException("Katılım bildirimi yalnızca bugün için yapılabilir.");
        var entry = Data.Attendance.FirstOrDefault(a => a.UserId == user.Id && a.Date == when);
        if (entry == null) Data.Attendance.Add(new AttendanceIntent { UserId = user.Id, Date = when, Coming = coming }); else entry.Coming = coming;
        store.Save();
    }
    private static void ValidateExercises(List<Exercise> exercises, bool rest, string? title)
    {
        if (title?.Trim().Length > 80) throw new ArgumentException("Antrenman adı en fazla 80 karakter olabilir.");
        if (!rest && (exercises.Count == 0 || exercises.Any(e => string.IsNullOrWhiteSpace(e.Name) || string.IsNullOrWhiteSpace(e.Muscle) || e.Sets is < 1 or > 20 || e.Reps is < 1 or > 100 || e.Minutes is < 1 or > 180 || e.RestSeconds is < 0 or > 600)))
            throw new ArgumentException("Hareket adı, kas bölgesi ve sayısal değerleri kontrol edin.");
    }
    private void ApplyCustom(User user, PlanDay day, List<Exercise> exercises, bool rest, string? title)
    {
        string before = Describe(day);
        day.Status = rest ? "Dinlenme" : "Planlandı"; day.Exercises = rest ? [] : exercises.Select(e => e.Copy()).ToList();
        day.Baseline = day.Exercises.Select(e => e.Copy()).ToList(); day.Custom = true;
        day.Adjustment = RequireCurrent().Role is "Fitness Koçu" or "Futbol Takımı Antrenörü" ? "Koç tarafından hazırlandı" : "Kişisel program";
        day.Title = rest ? "Dinlenme" : string.IsNullOrWhiteSpace(title) ? string.Join(" + ", exercises.Select(e => e.Muscle).Distinct()) : title.Trim();
        TrackChange(user, day, before, day.Adjustment);
    }
    public int SetWeeklyTemplate(User user, DayOfWeek weekday, List<Exercise> exercises, bool rest, string? title = null)
    {
        EnsureAccess(user); ValidateExercises(exercises, rest, title);
        var plan = Plan(user) ?? throw new InvalidOperationException("Önce program oluşturun.");
        var today = DateOnly.FromDateTime(DateTime.Today);
        var days = plan.Days.Where(d => d.Date >= today && d.Date.DayOfWeek == weekday && (d.Status == "Planlandı" || d.Status == "Dinlenme") && !Data.Workouts.Any(l => l.PlanDayId == d.Id) && !Data.Drafts.Any(x => x.UserId == user.Id && x.PlanDayId == d.Id)).ToList();
        if (days.Count == 0) throw new InvalidOperationException("Bu haftanın günü için düzenlenebilecek gelecek kayıt yok.");
        foreach (var d in days) ApplyCustom(user, d, exercises, rest, title);
        store.Save(); return days.Count;
    }
    public void SetAvailableDays(User user, List<DayOfWeek> days)
    {
        EnsureAccess(user);
        if (days.Count == 0 || days.Any(d => !Choices.Days.Contains(d))) throw new ArgumentException("En az bir geçerli gün seçin.");
        user.Profile.AvailableDays = days.Distinct().ToList();
        var plan = Plan(user); var today = DateOnly.FromDateTime(DateTime.Today);
        if (plan != null)
        {
            int index = 0;
            foreach (var d in plan.Days)
            {
                bool train = days.Contains(d.Date.DayOfWeek);
                var exercises = train ? BuildExercises(user.Profile, user.Role == Choices.Roles[1], index++) : [];
                // Kaydedilen, taşınan ve özel hazırlanan antrenmanlar korunur.
                if (d.Date < today || d.Status is "Tamamlandı" or "Kaçırıldı" or "Eksik" || d.Custom || d.MovedFrom != null || d.Adjustment == "Kişisel program" || Data.Workouts.Any(l => l.PlanDayId == d.Id) || Data.Drafts.Any(x => x.UserId == user.Id && x.PlanDayId == d.Id)) continue;
                var before = Describe(d);
                d.Status = train ? "Planlandı" : "Dinlenme"; d.Exercises = exercises; d.Baseline = exercises.Select(e => e.Copy()).ToList();
                d.Title = train ? string.Join(" + ", exercises.Select(e => e.Muscle).Distinct()) : "Aktif dinlenme"; d.Adjustment = "";
                TrackChange(user, d, before, "Gelinebilecek günler güncellendi");
            }
            AdaptNext(user);
        }
        store.Save();
    }
}
