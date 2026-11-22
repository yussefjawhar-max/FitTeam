namespace FitTeam.Core;

public sealed partial class FitService(DataStore store,TimeProvider? clock=null)
{
    public AppData Data => store.Data;
    public bool IsPreview => store.FilePath.Length == 0;
    public bool UsesDatabase => store.UsesDatabase;
    public string StorageDescription => store.StorageDescription;
    public void LoginPreviewAdministrator()
    {
        if(!IsPreview)throw new InvalidOperationException("Önizleme yalnız örnek verilerle açılabilir.");
        foreach(var user in Data.Users)user.ExampleBatch="classroom-preview";
        var owner=new User{Email="jawharyusuf446@gmail.com",Username="ornek-yetkili",Profile=new(){Name="Örnek Yetkili"},ProfileComplete=true,IsExample=true,ExampleBatch="classroom-preview"};
        Data.Users.Add(owner);Current=owner;IsPresident=true;LoginMethod="Örnek yönetim paneli";
    }
    public User? Current { get; private set; }
    public bool Login(string username, string password)
    {
        IsPresident = false; LoginMethod = "Yerel hesap";
        var user = store.FindLoginUser(username.Trim());
        if (user == null)
            user = store.FindLoginUser(System.Text.RegularExpressions.Regex.Replace(LoginContact.NormalizeDigits(username.Trim()), @"[\s()-]", ""));
        Current = user != null && user.IsActive && !string.IsNullOrEmpty(user.Salt) && !string.IsNullOrEmpty(user.PasswordHash) && Passwords.Verify(user, password) ? user : null;
        if(Current!=null&&!IsPreview&&Passwords.RememberVerified(Current,password))store.Save();
        return Current != null;
    }
    public void Logout() { Current = null; IsPresident = false; LoginMethod = "Yerel hesap"; }
    public User Register(string username, string password, string name, string role, Guid? coachId = null)
    {
        username = username.Trim(); name = name.Trim();
        if (username.Length < 3 || name.Length < 2 || password.Length < 6) throw new ArgumentException("Kullanıcı adı en az 3, ad soyad 2, parola 6 karakter olmalı.");
        if (!Choices.Roles.Contains(role)) throw new ArgumentException("Geçerli bir rol seçin.");
        if (Data.Users.Any(u => u.Username.Equals(username, StringComparison.OrdinalIgnoreCase))) throw new ArgumentException("Bu kullanıcı adı kullanılıyor.");
        if (role == Choices.Roles[0] && !Data.Users.Any(u => u.Id == coachId && u.Role == Choices.Roles[2]))
            throw new ArgumentException("Bireysel hesap için bir fitness koçu seçin.");
        var user = new User { Username = username, Role = role, CoachId = role == Choices.Roles[0] ? coachId : null, Profile = new Profile { Name = name } };
        Passwords.Set(user, password); Data.Users.Add(user);
        AddWeightInternal(user, DateOnly.FromDateTime(DateTime.Today), user.Profile.Weight);
        store.Save(); return user;
    }
    public void Save() => store.Save();
    public void Backup(string path) => store.ExportBackup(path);
    public List<User> ManagedUsers()
    {
        var user = RequireCurrent();
        if (user.Role == Choices.Roles[2]) return Data.Users.Where(u => u.IsActive && !u.NeedsApproval && CoachLinks(u).Contains(user.Id) && u.Role == Choices.Roles[0]).ToList();
        if (user.Role == Choices.Roles[3])
        {
            var ids = Data.Teams.Where(t => t.IsActive && t.CoachId == user.Id).Select(t => t.Id).ToHashSet();
            return Data.Users.Where(u => u.IsActive && !u.NeedsApproval && u.TeamId.HasValue && ids.Contains(u.TeamId.Value) && u.Role == Choices.Roles[1]).ToList();
        }
        return [];
    }
    public User RequireCurrent() => Current is {IsActive:true} ? Current : throw new InvalidOperationException("Hesap çıkarıldı veya oturum yok. Tekrar giriş yapın.");
    public void EnsureAccess(User target)
    {
        if(HasAdministrationAccess){_ = RequireCurrent();return;}
        if (target.Id != RequireCurrent().Id && !ManagedUsers().Any(u => u.Id == target.Id))
            throw new InvalidOperationException("Bu kullanıcıya erişim yetkiniz yok.");
    }
    public TrainingPlan? Plan(User user) { EnsureAccess(user); return Data.Plans.FirstOrDefault(p => p.UserId == user.Id); }
    public List<WorkoutLog> Logs(User user) { EnsureAccess(user); return Data.Workouts.Where(l => l.UserId == user.Id).OrderBy(l => l.Date).ToList(); }
    public List<WeightEntry> Weights(User user) { EnsureAccess(user); return Data.Weights.Where(l => l.UserId == user.Id).OrderBy(l => l.Date).ToList(); }
    public List<MatchLog> Matches(User user) { EnsureAccess(user); return Data.Matches.Where(l => l.UserId == user.Id).OrderBy(l => l.Date).ToList(); }

    public void UpdateProfile(Profile profile) => UpdateProfile(RequireCurrent(), profile);
    public void UpdateProfile(User user, Profile profile)
    {
        EnsureAccess(user);
        if (profile.Name.Trim().Length < 2 || profile.Age is < 13 or > 90 || profile.Height is < 100 or > 230 || profile.Weight is < 30 or > 300 || profile.TargetWeight is < 30 or > 300)
            throw new ArgumentException("Ad soyad, yaş, boy ve kilo bilgilerini kontrol edin.");
        if (profile.AvailableDays.Count == 0) throw new ArgumentException("En az bir antrenman günü seçin.");
        if (profile.WaterGoal is < 0.5m or > 8m) throw new ArgumentException("Su hedefi 0,5 ile 8 litre arasında olmalı.");
        if (!Choices.Goals.Contains(profile.Goal) || !Choices.Levels.Contains(profile.Level) || !Choices.Positions.Contains(profile.Position) || profile.AvailableDays.Any(d => !Choices.Days.Contains(d)))
            throw new ArgumentException("Profil seçimleri geçersiz.");
        if(!SignupApplication.BloodGroups.Contains(profile.BloodGroup))throw new ArgumentException("Kan grubu seçimi geçersiz.");
        profile.TrainingStart = user.Profile.TrainingStart;profile.TrainingHours = user.Profile.TrainingHours;
        profile.BirthDate = user.Profile.BirthDate;
        profile.AvatarBase64 = user.Profile.AvatarBase64;
        profile.HealthNotes = user.Profile.HealthNotes;
        profile.ActivityLevel = user.Profile.ActivityLevel;
        profile.MealRoutine = user.Profile.MealRoutine;
        profile.NutritionNeedsSpecialCare = user.Profile.NutritionNeedsSpecialCare;
        bool newWeightGoal = !user.ProfileComplete || profile.TargetWeight != user.Profile.TargetWeight;
        profile.GoalStartWeight = newWeightGoal ? profile.Weight : user.Profile.GoalStartWeight;
        profile.GoalStarted = newWeightGoal ? DateOnly.FromDateTime(DateTime.Today) : user.Profile.GoalStarted;
        profile.InitialWeight = user.ProfileComplete ? user.Profile.InitialWeight : profile.Weight;
        if (!user.ProfileComplete) Data.Weights.RemoveAll(w => w.UserId == user.Id);
        user.ProfileComplete = true;
        user.Profile = profile;
        AddWeightInternal(user, DateOnly.FromDateTime(DateTime.Today), profile.Weight);
        store.Save();
    }
    public void SetWeight(User user, DateOnly date, decimal weight)
    {
        EnsureAccess(user);
        if (weight is < 30 or > 300) throw new ArgumentException("Kilo 30 ile 300 kg arasında olmalı.");
        if (date > DateOnly.FromDateTime(DateTime.Today)) throw new ArgumentException("Gelecek tarih için kilo kaydedilemez.");
        AddWeightInternal(user, date, weight); store.Save();
    }
    private void AddWeightInternal(User user, DateOnly date, decimal weight)
    {
        var entry = Data.Weights.FirstOrDefault(w => w.UserId == user.Id && w.Date == date);
        if (entry == null) Data.Weights.Add(new WeightEntry { UserId = user.Id, Date = date, Weight = weight }); else entry.Weight = weight;
        user.Profile.Weight = Data.Weights.Where(w => w.UserId == user.Id).OrderBy(w => w.Date).Last().Weight;
    }
    public decimal WaterToday(User user) { EnsureAccess(user); return Data.Water.FirstOrDefault(w => w.UserId == user.Id && w.Date == DateOnly.FromDateTime(DateTime.Today))?.Litres ?? 0; }
    public void AddWater(decimal litres)
    {
        var user = RequireCurrent(); var today = DateOnly.FromDateTime(DateTime.Today);
        if (litres <= 0 || litres > 3) throw new ArgumentException("Tek seferde 0 ile 3 litre arasında miktar girin.");
        var entry = Data.Water.FirstOrDefault(w => w.UserId == user.Id && w.Date == today);
        if (entry == null) { entry = new WaterEntry { UserId = user.Id, Date = today }; Data.Water.Add(entry); }
        if (entry.Litres + litres > 15) throw new ArgumentException("Günlük kayıt 15 litreyi aşamaz; miktarı kontrol edin.");
        entry.Litres += litres; store.Save();
    }
    public TrainingPlan GeneratePlan(DateOnly start) => GeneratePlan(RequireCurrent(), start);
    public TrainingPlan GeneratePlan(User user, DateOnly start)
    {
        EnsureAccess(user);
        if (!user.ProfileComplete) throw new ArgumentException("Önce profil bilgilerinizi kaydedin.");
        if (user.Profile.AvailableDays.Count == 0) throw new ArgumentException("Önce profilinizde antrenman günlerini seçin.");
        var old = Data.Plans.FirstOrDefault(p => p.UserId == user.Id);
        if (old != null && Data.Workouts.Any(l => l.UserId == user.Id && old.Days.Any(d => d.Id == l.PlanDayId)) || old != null && Data.Drafts.Any(d => d.UserId == user.Id && old.Days.Any(x => x.Id == d.PlanDayId)))
            throw new InvalidOperationException("Kayıtlı antrenmanı olan program korunur. 30 gün bitince yeni program oluşturabilirsiniz.");
        if (start < DateOnly.FromDateTime(DateTime.Today)) throw new ArgumentException("Yeni program bugün veya daha sonra başlamalı.");
        return GenerateInternal(user, start);
    }
    public TrainingPlan StartNextPlan(DateOnly start) => StartNextPlan(RequireCurrent(), start);
    public TrainingPlan StartNextPlan(User user, DateOnly start)
    {
        EnsureAccess(user);
        if (!user.ProfileComplete) throw new ArgumentException("Önce sporcu profilini kaydedin.");
        var old = Plan(user);
        if (old != null && old.Start.AddDays(29) >= DateOnly.FromDateTime(DateTime.Today)) throw new InvalidOperationException("Mevcut 30 günlük dönem henüz bitmedi.");
        if (start < DateOnly.FromDateTime(DateTime.Today)) throw new ArgumentException("Yeni dönem geçmişte başlayamaz.");
        return GenerateInternal(user, start);
    }
    internal TrainingPlan GenerateInternal(User user, DateOnly start)
    {
        Data.Plans.RemoveAll(p => p.UserId == user.Id);
        var plan = new TrainingPlan { UserId = user.Id, Start = start };
        int workoutIndex = 0;
        for (int i = 0; i < 30; i++)
        {
            var date = start.AddDays(i); bool train = user.Profile.AvailableDays.Contains(date.DayOfWeek);
            var exercises = train ? BuildExercises(user.Profile, user.Role == Choices.Roles[1], workoutIndex++) : [];
            plan.Days.Add(new PlanDay { Date = date, Status = train ? "Planlandı" : "Dinlenme", Title = train ? string.Join(" + ", exercises.Select(e => e.Muscle).Distinct()) : "Aktif dinlenme", Exercises = exercises, Baseline = exercises.Select(e => e.Copy()).ToList() });
        }
        Data.Plans.Add(plan); store.Save(); return plan;
    }
    public static List<Exercise> BuildExercises(Profile p, bool football, int index)
    {
        int sets = p.Level == "İleri" ? 4 : p.Level == "Orta" ? 3 : 2;
        int reps = p.Goal.Contains("Kas") || p.Goal.Contains("kas") ? 10 : 12;
        int minutes = p.Level == "İleri" ? 14 : p.Level == "Orta" ? 11 : 8;
        // Yaş ve BMI yalnızca örnek programın yükünü sadeleştirmek için kullanılır.
        if (p.Age >= 55 || p.Weight / ((p.Height / 100m) * (p.Height / 100m)) >= 30) { sets = Math.Max(1, sets - 1); minutes = Math.Max(5, minutes - 2); }
        Exercise E(string name, string muscle, int? duration = null) => new() { Name = name, Muscle = muscle, Sets = sets, Reps = reps, Minutes = duration ?? minutes, RestSeconds = p.Level == "Başlangıç" ? 90 : 60 };
        if (football) return [E("Dinamik ısınma", "Tüm vücut", 8), E(p.Position == "Kaleci" ? "Reaksiyon ve ayak çalışması" : "Top sürme ve pas", "Teknik"), E(index % 2 == 0 ? "Aralıklı koşu" : "Çeviklik parkuru", "Bacak"), E("Dirsek üzerinde sabit duruş", "Karın", 5)];
        var result = (index % 3) switch
        {
            0 => new List<Exercise> { E("Duvar / diz üstü şınav", "Göğüs"), E("Dambılla omuz itişi", "Omuz"), E("Dirsek üzerinde sabit duruş", "Karın", 5) },
            1 => [E("Dambılla sırt çekişi", "Sırt"), E("Dambılla kol bükme", "Kol"), E("Dört ayak üzerinde çapraz uzanma", "Karın", 5)],
            _ => [E("Sandalyeye çömelme", "Bacak"), E("Kalça köprüsü", "Bacak"), E("Parmak ucuna yükselme", "Bacak")]
        };
        if (p.Goal.Contains("Kilo ver") || p.Goal.Contains("Kondisyon") || p.Goal.Contains("Dayanıklılık")) result.Add(E("Tempolu yürüyüş", "Kardiyo", minutes + 5));
        return result;
    }
    private PlanDay RequireDay(Guid id) => RequireDay(RequireCurrent(), id);
    private PlanDay RequireDay(User user, Guid id)
    {
        var plan = Plan(user) ?? throw new InvalidOperationException("Önce program oluşturun.");
        return plan.Days.FirstOrDefault(d => d.Id == id) ?? throw new InvalidOperationException("Program günü bulunamadı.");
    }
    public void SetCustomDay(Guid id, List<Exercise> exercises, bool rest) => SetCustomDay(RequireCurrent(), id, exercises, rest);
    public void SetCustomDay(User user, Guid id, List<Exercise> exercises, bool rest, string? title = null)
    {
        var day = RequireDay(user, id);
        if (day.Status is "Tamamlandı" or "Kaçırıldı" or "Eksik" || Data.Workouts.Any(l => l.PlanDayId == id) || Data.Drafts.Any(d => d.UserId == user.Id && d.PlanDayId == id)) throw new InvalidOperationException("Kaydı olan gün değiştirilemez.");
        if (day.Date < DateOnly.FromDateTime(DateTime.Today)) throw new InvalidOperationException("Geçmiş günün planı değiştirilemez.");
        ValidateExercises(exercises, rest, title);
        ApplyCustom(user, day, exercises, rest, title); store.Save();
    }
    public DateOnly? MissDay(Guid id, bool move)
    {
        var user = RequireCurrent(); var day = RequireDay(id);
        if (day.Status != "Planlandı" || Data.Workouts.Any(l => l.PlanDayId == id) || Data.Drafts.Any(d => d.UserId == user.Id && d.PlanDayId == id)) throw new InvalidOperationException("Yalnızca kayıtsız, planlanan antrenman taşınabilir.");
        if (day.Date > DateOnly.FromDateTime(DateTime.Today)) throw new InvalidOperationException("Gelecek antrenman kaçırılmış olarak işaretlenemez; kişisel planı düzenleyin.");
        PlanDay? target = null;
        if (move)
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            target = Plan(user)!.Days.FirstOrDefault(d => d.Date > day.Date && d.Date >= today && d.Status == "Dinlenme" && user.Profile.AvailableDays.Contains(d.Date.DayOfWeek));
            if (target == null) throw new InvalidOperationException("30 günlük dönemde seçili günlerinize uygun boş gün yok. Profilde başka bir gün seçebilir veya yalnızca kaçırıldı olarak kaydedebilirsiniz.");
        }
        day.Status = "Kaçırıldı";
        if (target != null)
        {
            target.Status = "Planlandı"; target.Title = day.Title; target.MovedFrom = day.Id;
            target.Exercises = day.Exercises.Select(e => e.Copy()).ToList(); target.Baseline = day.Baseline.Select(e => e.Copy()).ToList();
            target.Adjustment = $"{day.Date:dd.MM} gününden taşındı";
        }
        store.Save(); return target?.Date;
    }
    public void RecordWorkout(WorkoutLog log)
    {
        var user = RequireCurrent(); var day = RequireDay(log.PlanDayId);
        if (day.Status is "Dinlenme" or "Kaçırıldı") throw new InvalidOperationException("Bu gün antrenman kaydına açık değil.");
        if (day.Date > DateOnly.FromDateTime(DateTime.Today)) throw new InvalidOperationException("Gelecek günün antrenmanı kaydedilemez.");
        if (log.Minutes is < 1 or > 600 || log.Fatigue is < 1 or > 10 || log.Calories is < 0 or > 10000 || log.AverageHeart is < 0 or > 250 || log.MaxHeart is < 0 or > 250 || log.MaxHeart < log.AverageHeart || log.Distance is < 0 or > 100 || log.Steps is < 0 or > 100000)
            throw new ArgumentException("Süre, yorgunluk veya saat değerlerini kontrol edin. Maksimum nabız ortalamadan küçük olamaz.");
        if (log.Exercises.Count != day.Exercises.Count || log.Exercises.Where((e, i) => e.Name != day.Exercises[i].Name).Any() || log.Exercises.Any(e => e.Sets is < 0 or > 20 || e.Reps is < 0 or > 100 || e.Minutes is < 0 or > 180 || (e.Done && (e.Sets == 0 || e.Reps == 0 || e.Minutes == 0))))
            throw new ArgumentException("Hareket kayıtlarını kontrol edin; yapılan hareketin set, tekrar ve süresi sıfır olamaz.");
        if (log.Completed && log.Exercises.Any(e => !e.Done)) throw new ArgumentException("Tamamlandı için bütün hareketleri işaretleyin veya eksik olarak kaydedin.");
        if(log.Location is not ("Salon" or "Ev"))throw new ArgumentException("Antrenman yerini kontrol et.");
        for(int i=0;i<log.Exercises.Count;i++){var result=log.Exercises[i];if(log.Location=="Ev")result.PerformedName=ExerciseGuides.PerformedName(result.Name,true);result.Muscle=log.Location=="Ev"?ExerciseLibrary.All.FirstOrDefault(e=>e.Name==result.PerformedName)?.Muscle??day.Exercises[i].Muscle:day.Exercises[i].Muscle;}
        if (log.Exercises.Any(e => e.LoadKg < 0 || e.LoadKg > 500)) throw new ArgumentException("Hareket ağırlığı 0-500 kg arasında olmalı.");
        log.UserId = user.Id; log.Date = day.Date;
        Data.Workouts.RemoveAll(l => l.PlanDayId == day.Id); Data.Workouts.Add(log);
        day.Status = log.Completed ? "Tamamlandı" : "Eksik";
        Data.Drafts.RemoveAll(d => d.UserId == user.Id && d.PlanDayId == day.Id);
        AdaptNext(user); store.Save();
    }
    private void AdaptNext(User user)
    {
        var plan = Plan(user)!; var before = plan.Days.ToDictionary(d => d.Id, Describe); var today = DateOnly.FromDateTime(DateTime.Today);
        foreach (var d in plan.Days.Where(d => d.Status == "Planlandı" && d.Date >= today && !Data.Drafts.Any(x => x.UserId == user.Id && x.PlanDayId == d.Id)))
        {
            d.Exercises = d.Baseline.Select(e => e.Copy()).ToList();
            if (d.Adjustment.StartsWith("Uyarlama:")) d.Adjustment = "";
        }
        var last = Data.Workouts.Where(l => l.UserId == user.Id && plan.Days.Any(d => d.Id == l.PlanDayId)).OrderBy(l => l.Date).LastOrDefault();
        if (last == null) return;
        var next = plan.Days.FirstOrDefault(d => d.Date > last.Date && d.Date >= today && d.Status == "Planlandı" && !Data.Drafts.Any(x => x.UserId == user.Id && x.PlanDayId == d.Id));
        if (next == null) return;
        if (last.Fatigue >= 8 || last.Difficulty == "Zor" || last.Pain != "Yok")
        {
            foreach (var e in next.Exercises) { e.Sets = Math.Max(1, e.Sets - 1); e.Minutes = Math.Max(3, e.Minutes - 2); }
            next.Adjustment = "Uyarlama: yorgunluk / zorluk nedeniyle hafifletildi";
        }
        else if (last.Difficulty == "Kolay" && last.Fatigue <= 4 && last.Completed)
        {
            foreach (var e in next.Exercises) e.Reps = Math.Min(100, e.Reps + 2);
            next.Adjustment = "Uyarlama: kolay geldiği için tekrar artırıldı";
        }
        if (last.TiredMuscle == "Bacak" && last.Fatigue >= 7)
        {
            foreach (var e in next.Exercises.Where(e => e.Muscle == "Bacak")) { e.Name = "Hafif üst vücut hareketliliği"; e.Muscle = "Üst vücut"; e.Sets = 1; e.Reps = 8; e.Minutes = 5; }
            next.Adjustment += " • Bacak yerine üst vücut";
        }
        if (!next.Custom) next.Title = string.Join(" + ", next.Exercises.Select(e => e.Muscle).Distinct());
        foreach (var d in plan.Days.Where(d => d.Status == "Planlandı" && d.Date >= today))
            TrackChange(user, d, before[d.Id], "Antrenman geri bildirimi: " + last.Difficulty + ", yorgunluk " + last.Fatigue);
    }
    public void AddMatch(MatchLog match)
    {
        var user = RequireCurrent();
        if (user.Role != Choices.Roles[1]) throw new InvalidOperationException("Maç kaydı futbolcular içindir.");
        if (match.Date > DateOnly.FromDateTime(DateTime.Today) || string.IsNullOrWhiteSpace(match.Opponent) || match.Minutes is < 0 or > 130 || match.Rating is < 0 or > 10 || match.Goals is < 0 or > 30 || match.Assists is < 0 or > 30 || match.Shots is < 0 or > 100 || match.Yellow is < 0 or > 2 || match.Red is < 0 or > 1 || match.Distance is < 0 or > 30 || match.Calories is < 0 or > 5000 || match.AverageHeart is < 0 or > 250 || match.MaxHeart is < 0 or > 250 || match.MaxHeart < match.AverageHeart)
            throw new ArgumentException("Maç bilgilerini kontrol edin; gelecek tarih seçilemez.");
        match.UserId = user.Id; Data.Matches.Add(match); store.Save();
    }
    public Team CreateTeam(string name, string gender)
    {
        var coach = RequireCurrent();
        if (coach.Role != Choices.Roles[3]) throw new InvalidOperationException("Takımı antrenör oluşturabilir.");
        if (name.Trim().Length < 2 || !new[] { "Kadın", "Erkek" }.Contains(gender)) throw new ArgumentException("Takım adı ve türünü kontrol edin.");
        var team = new Team { CoachId = coach.Id, Name = name.Trim(), Gender = gender }; Data.Teams.Add(team); store.Save(); return team;
    }
    public void AssignMember(string username, Guid? teamId)
    {
        var coach = RequireCurrent(); var member = Data.Users.FirstOrDefault(u => u.Username.Equals(username.Trim(), StringComparison.OrdinalIgnoreCase)) ?? throw new ArgumentException("Kullanıcı bulunamadı. Önce kayıt ekranından sporcu hesabı oluşturun.");
        if (coach.Role == Choices.Roles[2] && member.Role == Choices.Roles[0])
        {
            if (!CoachLinks(member).Contains(coach.Id)) throw new InvalidOperationException("Üye önce Profilim ekranından sizi koç olarak seçmeli.");
        }
        else if (coach.Role == Choices.Roles[3] && member.Role == Choices.Roles[1])
        {
            var team = Data.Teams.FirstOrDefault(t => t.IsActive && t.Id == teamId && t.CoachId == coach.Id) ?? throw new ArgumentException("Kendi takımınızı seçin.");
            if (member.TeamId != null && member.TeamId != team.Id) throw new InvalidOperationException("Oyuncu başka bir takıma bağlı.");
            member.TeamId = team.Id;
        }
        else throw new InvalidOperationException("Fitness koçu bireysel kullanıcı, antrenör futbolcu ekleyebilir.");
        store.Save();
    }
    public void RemoveMember(User member)
    {
        if (!ManagedUsers().Any(u => u.Id == member.Id)) throw new InvalidOperationException("Bu üye sizin listenizde değil.");
        if (RequireCurrent().Role == Choices.Roles[2]) { var ids=CoachLinks(member);ids.Remove(RequireCurrent().Id);member.CoachIds=ids;member.CoachId=ids.Count==0?null:ids[0]; } else member.TeamId = null; store.Save();
    }
    public void SetCoachNote(User member, string note)
    {
        if (!ManagedUsers().Any(u => u.Id == member.Id)) throw new InvalidOperationException("Bu üye sizin listenizde değil.");
        member.CoachNote = note.Trim(); store.Save();
    }
}
