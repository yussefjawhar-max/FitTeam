using FitTeam.Core;
using System.Text.Json;

var directory = Path.Combine(Path.GetTempPath(), "FitTeam_checks_" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(directory);
var path = Path.Combine(directory, "fitteam.json");
int count = 0;
void Check(bool condition, string name) { if (!condition) throw new Exception("BAŞARISIZ: " + name); count++; Console.WriteLine("GEÇTİ: " + name); }
void Reject(Action action, string name) { bool failed = false; try { action(); } catch (ArgumentException) { failed = true; } catch (InvalidOperationException) { failed = true; } Check(failed, name); }
try
{
    var store = new DataStore(path); var service = new FitService(store); var today = DateOnly.FromDateTime(DateTime.Today);
    Check(store.Data.Users.Count == 17, "İlk açılışta örnek hesaplar oluşturulur");
    Check(!service.Login("sporcu", "yanlış"), "Yanlış parola reddedilir");
    Check(service.Login(" SPORCU ", "Fit123!"), "Giriş kullanıcı adında büyük/küçük harf ve boşlukları destekler");
    var athlete = service.RequireCurrent(); var summary = Analytics.Summarize(service, athlete);
    Check(summary.Completed > 0 && summary.Percent <= 100, "Örnek kayıtlar rapora yansır");
    Check(!athlete.PasswordHash.Contains("Fit123!"), "Parola düz metin olarak saklanmaz");
    var outsider = store.Data.Users.First(u => u.Username == "oyuncu");
    Reject(() => service.Logs(outsider), "Bireysel kullanıcı başka sporcunun kayıtlarına erişemez");
    service.Register("yenisporcu", "test123", "Yeni Sporcu", Choices.Roles[0], store.Data.Users.First(u => u.Username == "koc").Id);
    Reject(() => service.Register("yenisporcu", "test123", "Yeni Sporcu", Choices.Roles[0]), "Yinelenen kullanıcı adı reddedilir");
    service.Login("yenisporcu", "test123"); var user = service.RequireCurrent();
    Reject(() => service.GeneratePlan(today), "Profil doldurulmadan program üretilmez");
    service.UpdateProfile(new Profile { Name = "Yeni Sporcu", Age = 25, Height = 180, Weight = 90, TargetWeight = 80, AvailableDays = Choices.Days.ToList(), Level = "İleri", Goal = "Kas geliştirmek" });
    Check(user.Profile.InitialWeight == 90 && service.Weights(user).Single().Weight == 90, "İlk gerçek profil kaydı başlangıç kilosunu belirler");
    var plan = service.GeneratePlan(today);
    Check(plan.Days.Count == 30 && plan.Days.Select(d => d.Date).Distinct().Count() == 30 && plan.Days.All(d => d.Exercises.Count > 0), "Program tam 30 farklı gün oluşturur");
    var low = FitService.BuildExercises(new Profile { Age = 60, Height = 170, Weight = 100, Level = "İleri" }, false, 0);
    var high = FitService.BuildExercises(new Profile { Age = 25, Height = 180, Weight = 75, Level = "İleri" }, false, 0);
    Check(low[0].Sets < high[0].Sets && low[0].Minutes < high[0].Minutes, "Yaş ve BMI örnek yükü etkiler");
    var todayDay = plan.Days[0]; var tomorrow = plan.Days[1];
    WorkoutLog Log(PlanDay day, string difficulty, int fatigue, bool done = true) => new() { PlanDayId = day.Id, Completed = done, Minutes = 45, Fatigue = fatigue, Difficulty = difficulty, AverageHeart = 130, MaxHeart = 170, Calories = 400, Distance = 4.5m, Exercises = day.Exercises.Select(e => new ExerciseResult { Name = e.Name, Done = true, Sets = e.Sets, Reps = e.Reps, Minutes = e.Minutes }).ToList() };
    Reject(() => service.RecordWorkout(Log(tomorrow, "Normal", 5)), "Gelecek günün antrenmanı kaydedilemez");
    service.RecordWorkout(Log(todayDay, "Zor", 9));
    Check(tomorrow.Exercises[0].Sets == tomorrow.Baseline[0].Sets - 1, "Yüksek yorgunluk sonraki antrenmanı hafifletir");
    service.RecordWorkout(Log(todayDay, "Zor", 9));
    Check(service.Logs(user).Count == 1 && tomorrow.Exercises[0].Sets == tomorrow.Baseline[0].Sets - 1, "Güncelleme antrenman sayısını veya uyarlamayı çoğaltmaz");
    service.RecordWorkout(Log(todayDay, "Kolay", 2));
    Check(tomorrow.Exercises[0].Reps == tomorrow.Baseline[0].Reps + 2 && tomorrow.Exercises[0].Sets == tomorrow.Baseline[0].Sets, "Kolay antrenman tekrar artırır; önceki uyarlama sıfırlanır");
    var bad = Log(todayDay, "Normal", 5); bad.MaxHeart = 100;
    Reject(() => service.RecordWorkout(bad), "Maksimum nabız ortalamadan düşük olamaz");
    bad = Log(todayDay, "Normal", 5); bad.Exercises[0].Done = false;
    Reject(() => service.RecordWorkout(bad), "Eksik hareketli antrenman tamamlandı olarak kaydedilemez");
    Reject(() => service.SetCustomDay(todayDay.Id, [new Exercise { Name = "Squat", Muscle = "Bacak" }], false), "Kayıtlı antrenmanın planı değiştirilemez");
    service.SetCustomDay(tomorrow.Id, [new Exercise { Name = "Squat", Muscle = "Bacak" }], false);
    var tired = Log(todayDay, "Zor", 9); tired.TiredMuscle = "Bacak"; service.RecordWorkout(tired);
    Check(tomorrow.Exercises.All(e => e.Muscle != "Bacak"), "Çok yorgun bacak yerine üst vücut seçilir");
    Reject(() => service.GeneratePlan(today), "Aktif dönemde antrenman kayıtları olan plan korunur");
    Reject(() => service.StartNextPlan(today), "30 gün bitmeden yeni dönem başlatılamaz");
    service.SetWeight(user, today, 88); service.SetWeight(user, today, 87.5m);
    Check(service.Weights(user).Count == 1 && user.Profile.Weight == 87.5m && user.Profile.InitialWeight == 90, "Aynı tarihli kilo güncellenir; başlangıç kilosu sabit kalır");
    Reject(() => service.SetWeight(user, today.AddDays(1), 88), "Gelecek tarihli kilo kaydı reddedilir");
    service.AddWater(0.25m); service.AddWater(0.5m); Check(service.WaterToday(user) == 0.75m, "Su eklemeleri toplanır");
    Reject(() => service.AddWater(-1), "Negatif su miktarı reddedilir");
    Check(Analytics.Summarize(service, user).Completed == 1, "Raporda güncellenen kayıt tek antrenman sayılır");
    var restore = new FitService(new DataStore(path)); restore.Login("yenisporcu", "test123");
    Check(restore.RequireCurrent().Profile.Weight == 87.5m && restore.Logs(restore.RequireCurrent()).Count == 1, "Veriler uygulama yeniden açılınca korunur");
    service.Login("koc", "Fit123!"); Check(service.ManagedUsers().Count == 3, "Seçilen koç yeni üyeyi otomatik olarak görür");
    service.AssignMember("yenisporcu", null); Check(service.ManagedUsers().Count == 3, "Mevcut üye bağlantısı yinelenmez");
    service.SetCoachNote(user, "Düzenli kayıt tut."); service.RemoveMember(user); Check(user.CoachId == null && user.CoachNote != "", "Listeden çıkarma hesabı ve notları silmez");
    Reject(() => service.AssignMember("futbolcu", null), "Fitness koçu futbolcuyu bireysel üye olarak ekleyemez");
    service.Login("antrenor", "Fit123!"); Check(service.ManagedUsers().Count == 2, "Antrenör kendi takımını görür");
    var team = service.CreateTeam("Yeni Takım", "Kadın"); Check(team.Name == "Yeni Takım", "Antrenör takım oluşturur");
    service.Login("futbolcu", "Fit123!"); var player = service.RequireCurrent();
    service.AddMatch(new MatchLog { Date = today, Opponent = "Test FK", Minutes = 90, Goals = 1, Rating = 8, AverageHeart = 140, MaxHeart = 180 });
    Check(service.Matches(player).Last().Goals == 1, "Futbolcu maç performansı kaydeder");
    Reject(() => service.AddMatch(new MatchLog { Date = today.AddDays(1), Opponent = "Test FK" }), "Gelecek tarihli maç reddedilir");
    service.Login("sporcu", "Fit123!");
    Reject(() => service.AddMatch(new MatchLog { Date = today, Opponent = "Test" }), "Bireysel kullanıcı maç kaydı ekleyemez");
    // Taşıma için seçili uygun bir boş gün hazırlanır; sınır dışına veya dolu güne taşınmaz.
    var demoPlan = service.Plan(service.RequireCurrent())!;
    var missed = demoPlan.Days.First(d => d.Date < today && d.Status == "Tamamlandı");
    store.Data.Workouts.RemoveAll(l => l.PlanDayId == missed.Id); missed.Status = "Planlandı";
    var target = demoPlan.Days.First(d => d.Date >= today && d.Status == "Planlandı"); target.Status = "Dinlenme"; target.Exercises = []; target.Baseline = [];
    var moved = service.MissDay(missed.Id, true);
    Check(moved == target.Date && missed.Status == "Kaçırıldı" && target.MovedFrom == missed.Id && target.Exercises.Count > 0, "Kaçırılan antrenman uygun boş güne iz bırakarak taşınır");
    Check(demoPlan.Days.Count == 30, "Taşıma 30 günlük takvimi büyütmez");
    Reject(() => service.MissDay(missed.Id, true), "Aynı kaçırılan antrenman tekrar taşınamaz");
    Check(Analytics.HtmlReport(service, service.RequireCurrent()).Contains("<!doctype html>"), "Rapor dışa aktarımı HTML belgesi oluşturur");
    var backup = Path.Combine(directory, "backup.json"); service.Backup(backup); Check(JsonSerializer.Deserialize<AppData>(File.ReadAllText(backup))!.Users.Count == store.Data.Users.Count, "Veri yedeği yeniden okunabilir");
    var corrupt = Path.Combine(directory, "corrupt.json"); File.WriteAllText(corrupt, "bozuk veri"); bool rejected = false;
    try { _ = new DataStore(corrupt); } catch (JsonException) { rejected = true; }
    Check(rejected && File.ReadAllText(corrupt) == "bozuk veri", "Bozuk veri dosyası silinmez veya örnek veriyle ezilmez");
    // Koç seçimi, kişisel gün ve katılım bildirimi aynı akışta doğrulanır.
    service.Login("yenisporcu", "test123"); var firstCoach = store.Data.Users.First(u => u.Username == "koc"); var secondCoach = store.Data.Users.First(u => u.Username == "koc2");
    service.SelectCoach(secondCoach.Id);
    service.SetAttendance(true); Check(service.AttendanceStatus(user, today) == "Geleceğim" && service.Logs(user).Count == 1, "Geleceğim bildirimi antrenman tamamlanması sayılmaz");
    service.SetAttendance(false); Check(store.Data.Attendance.Count(a => a.UserId == user.Id && a.Date == today) == 1 && service.AttendanceStatus(user, today) == "Gelmeyeceğim", "Katılım kararı tek günlük kayıtta güncellenir");
    Reject(() => service.SetAttendance(true, today.AddDays(1)), "Gelecek güne bugünkü bildirim verilemez");
    Reject(() => service.SelectCoach(outsider.Id), "Futbolcu koç olarak seçilemez");
    Reject(() => service.Register("kocsuz", "test123", "Koçsuz Üye", Choices.Roles[0]), "Yeni bireysel kayıt koç seçimi gerektirir");
    service.Login("koc", "Fit123!"); Reject(() => service.Plan(user), "Önceki koç başka koça geçen üyeyi göremez");
    Reject(() => service.AssignMember("yenisporcu", null), "Koç üyenin seçimini zorla değiştiremez");
    service.Login("koc2", "Fit123!"); Check(service.ManagedUsers().Single().Id == user.Id, "Yeni koç yalnızca kendisini seçen üyeyi görür");
    var future = service.Plan(user)!.Days[2];
    service.SetCustomDay(user, future.Id, [new Exercise { Name = "Göğüs çalışması", Muscle = "Göğüs", Sets = 3, Reps = 10, Minutes = 8 }], false, "Tolga ile göğüs günü");
    Check(future.Custom && future.Title == "Tolga ile göğüs günü", "Koç üyeye adı olan özel antrenman hazırlayabilir");
    Check(service.Changes(user).Any(c => c.ActorId == secondCoach.Id && c.After.Contains("Göğüs çalışması")), "Program değişikliğinde koç ve önce/sonra kaydedilir");
    var repeated = service.SetWeeklyTemplate(user, future.Date.DayOfWeek, [new Exercise { Name = "Göğüs çalışması", Muscle = "Göğüs" }], false, "Haftalık göğüs");
    Check(repeated >= 4 && service.Plan(user)!.Days.Where(d => d.Date >= today && d.Date.DayOfWeek == future.Date.DayOfWeek).All(d => d.Title == "Haftalık göğüs"), "Şablon kalan aynı hafta günlerine uygulanır");
    int completedCount = service.Logs(user).Count; var customTitle = future.Title;
    service.SetAvailableDays(user, [today.DayOfWeek]);
    Check(service.Logs(user).Count == completedCount && future.Title == customTitle, "Gün seçimi kayıtlı antrenmanı ve özel programı korur");
    var template = service.SaveTemplate("Göğüs örneği", future.Exercises);
    Check(template.OwnerId == secondCoach.Id && service.MyTemplates().Count == 1, "Koç hareketleri yeniden kullanılabilir şablonda saklar");
    service.Login("sporcu", "Fit123!"); Check(service.MyTemplates().Count == 0, "Kişisel şablonlar başka kullanıcıya açılmaz");
    service.Login("yenisporcu", "test123");
    var hard = Log(todayDay, "Zor", 9); service.RecordWorkout(hard);
    service.Login("koc2", "Fit123!"); Check(service.PendingReviews().Any(x => x.User.Id == user.Id && x.Log.PlanDayId == todayDay.Id), "Yüksek yorgunluk koçun inceleme kutusuna düşer");
    service.ReviewWorkout(user, todayDay.Id); Check(service.PendingReviews().Count == 0, "İncelenen antrenman bekleyen listesinden çıkar");
    service.Login("yenisporcu", "test123");
    Check(service.WeeklyMuscles(user).Sum(x => x.Sets) == hard.Exercises.Sum(e => e.Sets), "Haftalık kas dağılımı yapılan setleri toplar");
    Reject(() => service.StartSession(tomorrow.Id), "Canlı oturum gelecekteki gün için başlatılamaz");
    // Bugün için yeni bir kayıtsız test sporcusu kullanılır.
    var sessionUser = service.Register("canliuye", "test123", "Canlı Üye", Choices.Roles[0], secondCoach.Id); service.Login("canliuye", "test123");
    service.UpdateProfile(new Profile { Name = "Canlı Üye", AvailableDays = Choices.Days.ToList() }); var sessionPlan = service.GeneratePlan(today); var sessionDay = sessionPlan.Days[0];
    var draft = service.StartSession(sessionDay.Id); draft.ElapsedSeconds = 72; draft.Exercises[0].Sets = 1; draft.Exercises[0].LoadKg = 5; service.SaveDraft(draft);
    var reload = new FitService(new DataStore(path)); reload.Login("canliuye", "test123");
    Check(reload.Draft(sessionDay.Id)!.ElapsedSeconds == 72 && reload.Draft(sessionDay.Id)!.Exercises[0].LoadKg == 5, "Canlı antrenman süre/set/ağırlık taslağı yeniden açılınca korunur");
    Reject(() => service.SetCustomDay(sessionDay.Id, future.Exercises, false), "Başlatılmış antrenmanın hareketleri değiştirilmez");
    Reject(() => service.GeneratePlan(today), "Başlatılmış oturum varken program sıfırlanmaz");
    Reject(() => service.MissDay(sessionDay.Id, false), "Başlatılmış oturum kaçırılan gün olarak değiştirilemez");
    draft.Exercises[0].Sets = sessionDay.Exercises[0].Sets + 1; Reject(() => service.SaveDraft(draft), "Taslak planlanan set sayısını aşamaz"); draft.Exercises[0].Sets = 1;
    foreach (var (e, i) in draft.Exercises.Select((e, i) => (e, i))) { e.Sets = sessionDay.Exercises[i].Sets; e.Done = true; e.Minutes = 1; }
    var sessionLog = new WorkoutLog { PlanDayId = sessionDay.Id, Completed = true, Minutes = 2, Fatigue = 4, Exercises = draft.Exercises };
    service.RecordWorkout(sessionLog); Check(service.Draft(sessionDay.Id) == null && service.Logs(sessionUser).Count == 1, "Oturum sonucunu kaydetmek taslağı gerçek kayda dönüştürür");
    var contactUser = service.RegisterContact("student@example.com", "test123", "Eposta Üye", Choices.Roles[0], secondCoach.Id);
    Check(service.Login("STUDENT@example.com", "test123") && !contactUser.EmailVerified, "E-posta ile kayıt ve giriş desteklenir; doğrulandı diye işaretlenmez");
    var phoneUser = service.RegisterContact("+90 555 123 45 67", "test123", "Telefon Üye", Choices.Roles[0], secondCoach.Id);
    Check(service.Login("+90 (555) 123-45-67", "test123") && phoneUser.Phone == "+905551234567" && !phoneUser.PhoneVerified, "Telefon normalize edilir; SMS alınmadan doğrulandı gösterilmez");
    Reject(() => service.RegisterContact("bozuk@", "test123", "Hatalı Üye", Choices.Roles[0], secondCoach.Id), "Geçersiz e-posta reddedilir");
    Reject(() => service.RegisterContact("abc123", "test123", "Hatalı Üye", Choices.Roles[0], secondCoach.Id), "Geçersiz telefon reddedilir");
    Check(ExerciseLibrary.ReadyWorkouts().Count == 5 && ExerciseLibrary.ReadyWorkouts().All(t => t.Exercises.Count == 4), "Beş hazır antrenman geçerli kütüphane hareketlerinden oluşur");
    var cloudSettings = new FirebaseSettings { ApiKey = "test-key", ProjectId = "test-project" };
    var handler = new VerificationFixture(false); var cloud = new FirebaseEmailClient(cloudSettings, new HttpClient(handler));
    bool unverifiedRejected = false; try { await cloud.LoginVerified("fixture@gmail.com", "test123"); } catch (InvalidOperationException) { unverifiedRejected = true; }
    Check(unverifiedRejected, "Firebase doğrulanmamış e-posta girişini reddeder");
    cloud = new FirebaseEmailClient(cloudSettings, new HttpClient(new VerificationFixture(true)));
    var identity = await cloud.LoginVerified("fixture@gmail.com", "test123");
    Check(identity.Uid == "fixture-user" && identity.ProjectId == "test-project", "Doğrulanan e-posta kimliği Firebase UID ve projeyle bağlanır");
    var registrationHandler = new VerificationFixture(false); cloud = new FirebaseEmailClient(cloudSettings, new HttpClient(registrationHandler));
    var registrationUid = await cloud.RegisterAndSendVerification("fixture@gmail.com", "test123");
    Check(registrationUid == "fixture-user" && registrationHandler.Operations.SequenceEqual(new[] { "signUp", "sendOobCode" }), "Gerçek doğrulama akışı hesabı oluşturup VERIFY_EMAIL isteği yapar");
    Check(LoginContact.Gmail(" JawharYusuf446 ")=="jawharyusuf446@gmail.com","Gmail alanı hazır uzantıya yalnız kullanıcı adını ekler");
    Reject(()=>LoginContact.Gmail("user@other.com"),"Gmail alanına farklı uzantı eklenemez");
    Reject(()=>LoginContact.Gmail("a..b"),"Geçersiz Gmail kullanıcı adı reddedilir");
    Check(LoginContact.TurkishMobile("32 123 45 67")=="+905321234567","Hazır +90 5 önekiyle Türk cep telefonu tamamlanır");
    Reject(()=>LoginContact.TurkishMobile("32123"),"Eksik telefon numarası gönderilmez");
    Reject(()=>LoginContact.TurkishMobile("32123456a"),"Telefon alanına harf eklenemez");
    var testClock=new CheckClock();var codes=new CodeChallenges(testClock);string captured="";
    var request=await codes.Send("fixture@gmail.com",code=>{captured=code;return Task.CompletedTask;});
    Check(codes.ExpiresAt("fixture@gmail.com",request)==testClock.GetUtcNow().AddMinutes(2),"Sunucu kodun iki dakikalık bitiş zamanını verir");
    Check(captured.Length==6 && captured.All(char.IsDigit), "E-posta kodu 6 rakamdan oluşur");
    Reject(()=>codes.Verify("fixture@gmail.com",request,captured=="000000"?"111111":"000000"),"Yanlış rakamlı kod giriş açmaz");
    Reject(()=>codes.Verify("other@gmail.com",request,captured),"Kod başka hesaba aktarılamaz");
    codes.Verify("fixture@gmail.com",request,captured);Reject(()=>codes.Verify("fixture@gmail.com",request,captured),"Doğru kod yalnız bir kez kullanılır");
    bool limited=false;try{await codes.Send("fixture@gmail.com",_=>Task.CompletedTask);}catch(InvalidOperationException){limited=true;}Check(limited,"Yeni kod gönderimi 60 saniyeyle sınırlıdır");
    testClock.Advance(TimeSpan.FromMinutes(1));request=await codes.Send("fixture@gmail.com",code=>{captured=code;return Task.CompletedTask;});
    testClock.Advance(TimeSpan.FromMinutes(2));Reject(()=>codes.Verify("fixture@gmail.com",request,captured),"Tam iki dakikada kodun süresi dolar");
    request=await codes.Send("fixture@gmail.com",code=>{captured=code;return Task.CompletedTask;});
    testClock.Advance(TimeSpan.FromSeconds(119));codes.Verify("fixture@gmail.com",request,captured);
    Check(true,"İki dakikadan önce doğru kod kabul edilir");
    request=await codes.Send("fixture@gmail.com",code=>{captured=code;return Task.CompletedTask;});
    for(int i=0;i<5;i++)Reject(()=>codes.Verify("fixture@gmail.com",request,"invalid"),"Hatalı kod denemesi "+(i+1));
    Reject(()=>codes.Verify("fixture@gmail.com",request,captured),"Beş hatalı denemeden sonra doğru kod da reddedilir");
    bool failedSend=false;try{await codes.Send("failed@gmail.com",_=>throw new Exception("fixture failure"));}catch(InvalidOperationException){failedSend=true;}Check(failedSend,"Gönderim başarısızsa gönderildi gibi gösterilmez");
    var ordinaryAuth=new CodeAuthClient(new Uri("http://127.0.0.1:12345/"),new HttpClient(new CodeFixture("fixture@gmail.com",false)));
    service.LoginCode(await ordinaryAuth.Verify("fixture@gmail.com","email","fixture-request","123456"));Check(!service.IsPresident,"Normal doğrulanan üye başkan yetkisi kazanmaz");
    var presidentAuth=new CodeAuthClient(new Uri("http://127.0.0.1:12345/"),new HttpClient(new CodeFixture("jawharyusuf446@gmail.com",true)));
    service.LoginCode(await presidentAuth.Verify("jawharyusuf446@gmail.com","email","fixture-request","123456"));Check(service.IsPresident,"Sunucunun doğruladığı sahip hesabı başkan panelini açar");
    service.Logout();Check(!service.IsPresident,"Çıkışta başkan yetkisi kapanır");
    service.LoginCode(await presidentAuth.PresidentLogin("fixture-president","test-password"));service.Login("sporcu","Fit123!");Check(!service.IsPresident,"Başkan oturumu örnek hesaba taşınmaz");
    var badOwner=new CodeAuthClient(new Uri("http://127.0.0.1:12345/"),new HttpClient(new CodeFixture("outsider@gmail.com",true)));
    bool wrongOwner=false;try{await badOwner.Verify("outsider@gmail.com","email","fixture-request","123456");}catch(InvalidOperationException){wrongOwner=true;}Check(wrongOwner,"Farklı hesaba gelen başkan iddiası reddedilir");
    service.Login("yenisporcu","test123");service.SelectCoaches([firstCoach.Id,secondCoach.Id,firstCoach.Id]);
    Check(FitService.CoachLinks(user).Count==2,"Bir üye birden fazla hocayı seçer; yinelenen seçim tek kaydedilir");
    service.Login("koc","Fit123!");Check(service.ManagedUsers().Any(u=>u.Id==user.Id),"Birinci seçilen hoca üyeyi görür");
    service.Login("koc2","Fit123!");Check(service.ManagedUsers().Any(u=>u.Id==user.Id),"İkinci seçilen hoca aynı üyeyi görür");
    service.RemoveMember(user);Check(FitService.CoachLinks(user).SequenceEqual([firstCoach.Id]),"Hocanın listeden çıkarması yalnız kendi bağlantısını kaldırır");
    Reject(()=>service.Plan(user),"Çıkarılan hocanın erişimi kapanır");
    service.Login("koc","Fit123!");Check(service.ManagedUsers().Any(u=>u.Id==user.Id),"Diğer seçilmiş hocanın erişimi korunur");
    service.Login("yenisporcu","test123");Reject(()=>service.SelectCoaches([]),"Boş hoca seçimi reddedilir");Reject(()=>service.SelectCoaches([outsider.Id]),"Hoca olmayan kişi çoklu seçimde reddedilir");
    Reject(()=>service.AddCoach("Yeni Hoca","coach@example.com","Fitness"),"Normal üye başkanın hoca ekleme işlemini kullanamaz");
    service.LoginCode(await presidentAuth.PresidentLogin("fixture-president","test-password"));var addedCoach=service.AddCoach("Yeni Hoca","coach@example.com","Kondisyon");
    Check(addedCoach.Role==Choices.Roles[2]&&!addedCoach.EmailVerified&&string.IsNullOrEmpty(addedCoach.PasswordHash),"Başkan hoca ekler; e-posta doğrulandı veya parola verilmiş gibi gösterilmez");
    Reject(()=>service.AddCoach("Aynı Hoca","coach@example.com","Fitness"),"Aynı hoca e-posta hesabı ikinci kez eklenmez");
    using var rsa=System.Security.Cryptography.RSA.Create(2048);var parameters=rsa.ExportParameters(false);
    string B64(byte[] bytes)=>Convert.ToBase64String(bytes).TrimEnd('=').Replace('+','-').Replace('/','_');
    var jwks=JsonSerializer.Serialize(new {keys=new[]{new {kid="fixture",kty="RSA",n=B64(parameters.Modulus!),e=B64(parameters.Exponent!)}}});
    string MakeToken(string audience="fixture-client",string nonce="fixture-nonce",bool verifiedEmail=true,long? expiry=null)
    {
        var head=B64(System.Text.Encoding.UTF8.GetBytes("{\"alg\":\"RS256\",\"kid\":\"fixture\"}"));
        var body=B64(System.Text.Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new {aud=audience,iss="https://accounts.google.com",exp=expiry??DateTimeOffset.UtcNow.AddMinutes(5).ToUnixTimeSeconds(),iat=DateTimeOffset.UtcNow.ToUnixTimeSeconds(),nonce,email_verified=verifiedEmail,sub="fixture-subject",email="fixture@gmail.com",name="Fixture Name"})));
        var input=head+"."+body;return input+"."+B64(rsa.SignData(System.Text.Encoding.ASCII.GetBytes(input),System.Security.Cryptography.HashAlgorithmName.SHA256,System.Security.Cryptography.RSASignaturePadding.Pkcs1));
    }
    var google=new GoogleTokenVerifier(new HttpClient(new JwksFixture(jwks)));var googleProof=await google.Verify(MakeToken(),"fixture-client","fixture-nonce");
    Check(googleProof.Subject=="fixture-subject"&&googleProof.Email=="fixture@gmail.com","Google kimliği RSA imzası, audience ve nonce ile doğrulanır");
    async Task GoogleReject(string token,string label){bool fail=false;try{await google.Verify(token,"fixture-client","fixture-nonce");}catch(InvalidOperationException){fail=true;}Check(fail,label);}
    await GoogleReject(MakeToken(audience:"wrong-client"),"Başka Google istemcisinin kimlik belgesi reddedilir");
    await GoogleReject(MakeToken(nonce:"wrong-nonce"),"Başka Google giriş isteğinin nonce değeri reddedilir");
    await GoogleReject(MakeToken(verifiedEmail:false),"Doğrulanmamış Google e-postası reddedilir");
    await GoogleReject(MakeToken(expiry:DateTimeOffset.UtcNow.AddMinutes(-1).ToUnixTimeSeconds()),"Süresi geçmiş Google kimlik belgesi reddedilir");
    var mailFixture=new GmailFixture();var mail=new GoogleMailDelivery(new HttpClient(mailFixture));
    await mail.VerifyAccount("fixture-access",new GoogleProof("fixture-subject","fixture@gmail.com","Fixture"));
    Check(mailFixture.Auth=="Bearer fixture-access" && mailFixture.Url=="https://openidconnect.googleapis.com/v1/userinfo","Gmail izni yalnız resmi Google sunucusunda Authorization başlığıyla doğrulanır");
    async Task MailReject(Func<Task> action,string label){bool failed=false;try{await action();}catch(Exception ex) when(ex is InvalidOperationException or ArgumentException){failed=true;}Check(failed,label);}
    await MailReject(()=>mail.VerifyAccount("fixture-access",new GoogleProof("other-subject","fixture@gmail.com","Fixture")),"Başka Google hesabının gönderim erişimi reddedilir");
    await MailReject(()=>mail.VerifyAccount("fixture-access",new GoogleProof("fixture-subject","other@gmail.com","Fixture")),"Google gönderim hesabının e-postası eşleşmelidir");
    await mail.Send("fixture-access","fixture@gmail.com","123456");
    var raw=JsonDocument.Parse(mailFixture.Body).RootElement.GetProperty("raw").GetString()!;var raw64=raw.Replace('-','+').Replace('_','/');
    var mime=System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(raw64.PadRight(raw64.Length+(4-raw64.Length%4)%4,'=')));
    Check(mailFixture.Url=="https://gmail.googleapis.com/gmail/v1/users/me/messages/send"&&mime.Contains("From: fixture@gmail.com\r\nTo: fixture@gmail.com\r\n")&&!mime.Contains("Bcc:"),"Gmail kodu yalnız doğrulanan hesabın kendi adresine gönderilir");
    var body=System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(mime.Split("\r\n\r\n")[1]));
    Check(body.Contains("123456")&&body.Contains("2 dakika"),"Gmail MIME mesajı kodu ve geçerlilik süresini taşır");
    await MailReject(()=>mail.Send("fixture-access","fixture@gmail.com\r\nBcc: other@gmail.com","123456"),"Gmail mesajına başka alıcı ekleyen başlık enjeksiyonu reddedilir");
    await MailReject(()=>mail.Send("fixture-access","fixture@gmail.com","12345X"),"Gmail gönderiminde geçersiz kod reddedilir");
    mailFixture.Fail=true;await MailReject(()=>mail.Send("fixture-access","fixture@gmail.com","123456"),"Google gönderimi başarısızsa teslim edildi sonucu verilmez");
    await AdministrationChecks.Run(Check);
    LanguageChecks.Run(Check);
    await SignupChecks.Run(Check);
    await ClubAccessChecks.Run(Check);
    LanguageAndLocalSignupChecks.Run(Check);
    CountryExampleChecks.Run(Check);
    ClubRosterUpgradeChecks.Run(Check);
    RequestedDistributionChecks.Run(Check);
    await PresidentOverviewChecks.Run(Check);
    PasswordCopyChecks.Run(Check);
    WeightGoalChecks.Run(Check);
    NutritionChecks.Run(Check);
    FitnessCountsChecks.Run(Check);
    MealSuggestionChecks.Run(Check);
    PersonalMealChecks.Run(Check);
    await RecoveryAndMealUpgradeChecks.Run(Check);
    FullDeputyChecks.Run(Check);
    GuidedWorkoutChecks.Run(Check);
    VisualEnrollmentChecks.Run(Check);
    await DeputyChecks.Run(Check);
    FitTeam.Checks.MemberPortalChecks.Run(Check);
    await FitTeam.Checks.PortalCompletionChecks.Run(Check);
    await TwilioVerifyChecks.Run(Check);
    Console.WriteLine($"\n{count} kontrol başarıyla geçti.");
}
finally { Directory.Delete(directory, true); }

// Ağ kullanılmaz; bu sınıf yalnızca protokol kontrolleri içindir.
sealed class VerificationFixture(bool verified) : HttpMessageHandler
{
    public List<string> Operations { get; } = [];
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var operation = request.RequestUri!.AbsolutePath.Split(':').Last(); Operations.Add(operation);
        var payload = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("{\"aud\":\"test-project\",\"iss\":\"https://securetoken.google.com/test-project\",\"sub\":\"fixture-user\"}")).TrimEnd('=').Replace('+','-').Replace('/','_');
        string fixtureToken = "test." + payload + ".test";
        string json = operation == "lookup" ? "{\"users\":[{\"localId\":\"fixture-user\",\"email\":\"fixture@gmail.com\",\"emailVerified\":" + (verified ? "true" : "false") + "}]}" : "{\"localId\":\"fixture-user\",\"idToken\":\"" + fixtureToken + "\"}";
        return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK) { Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json") });
    }
}

sealed class CheckClock:TimeProvider {private DateTimeOffset now=DateTimeOffset.UtcNow;public override DateTimeOffset GetUtcNow()=>now;public void Advance(TimeSpan duration)=>now+=duration;}
sealed class CodeFixture(string contact,bool owner):HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken cancellationToken)
    {
        var result=request.RequestUri!.AbsolutePath=="/session"?JsonSerializer.Serialize(new {contact,channel="email",isOwner=owner}):JsonSerializer.Serialize(new {token="fixture-only"});
        return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK){Content=new StringContent(result,System.Text.Encoding.UTF8,"application/json")});
    }
}

sealed class JwksFixture(string json):HttpMessageHandler {protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken cancellationToken)=>Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK){Content=new StringContent(json,System.Text.Encoding.UTF8,"application/json")});}

sealed class GmailFixture:HttpMessageHandler
{
    public string Auth="",Url="",Body="";public bool Fail;
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken cancellationToken)
    {
        Auth=request.Headers.Authorization?.ToString()??"";Url=request.RequestUri!.ToString();Body=request.Content==null?"":await request.Content.ReadAsStringAsync(cancellationToken);
        return new HttpResponseMessage(Fail?System.Net.HttpStatusCode.Forbidden:System.Net.HttpStatusCode.OK){Content=new StringContent(Url.EndsWith("userinfo")?"{\"sub\":\"fixture-subject\",\"email\":\"fixture@gmail.com\",\"email_verified\":true}":"{\"id\":\"fixture-delivery\"}",System.Text.Encoding.UTF8,"application/json")};
    }
}
