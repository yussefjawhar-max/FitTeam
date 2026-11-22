using FitTeam.Core;
using FitTeam.Persistence;
using System.Text.Json;

var settingsPath = Environment.GetEnvironmentVariable("FITTEAM_MYSQL_CONFIG")
    ?? throw new InvalidOperationException("Test için FITTEAM_MYSQL_CONFIG ayarlanmalı.");
var settings = MySqlSettings.Load(settingsPath);
int passed = 0;
void Check(bool condition, string label)
{
    if (!condition) throw new InvalidOperationException("BAŞARISIZ: " + label);
    Console.WriteLine("TAMAM " + ++passed + " • " + label);
}
var repository = new MySqlAppRepository(settings);
repository.EnsureSchema();
if (args.Contains("--seed-demo-owner"))
{
    var identity = new MySqlIdentityStore(settings);
    if (identity.Load() == null)
    {
        var owner = new User { Username = "ders_yetkilisi", Profile = new Profile { Name = "Ders Gösterimi" } };
        Passwords.Set(owner, "Classroom123!");
        identity.Save(JsonSerializer.Serialize(new { Members = Array.Empty<object>(), OwnerCredential = owner }));
    }
    Console.WriteLine("Sadece bağımsız sınıf test veritabanının örnek yetkilisi hazır.");
    return;
}
var store = new DataStore(repository);
var service = new FitService(store);
Check(store.UsesDatabase, "Kalıcı depo gerçek MySQL bağlantısı kullanıyor");
Check(service.Data.Users.Count > 0, "Örnek üyeler MySQL'e kaydedildi");
var before = JsonSerializer.Serialize(store.Data);
var reloaded = new DataStore(new MySqlAppRepository(settings));
Check(JsonSerializer.Serialize(reloaded.Data) == before, "Bütün kayıtlar ve ilişkili programlar MySQL'den kayıpsız tekrar okunuyor");
Check(service.Login("sporcu", "Fit123!"), "Kullanıcı adı ve parola MySQL üzerinden doğrulanıyor");
Check(!service.Login("sporcu", "wrong-password"), "Yanlış parola oturum açmıyor");
Check(!service.Login("' OR 1=1 --", "Fit123!"), "Parametreli giriş sorgusu SQL enjeksiyonunu reddediyor");
var coach = service.Data.Users.First(u => u.Role == Choices.Roles[2]);
var suffix = Guid.NewGuid().ToString("N")[..8];
var added = service.Register("db_" + suffix, "Classroom123!", "Veritabanı Test Üyesi", Choices.Roles[0], coach.Id);
Check(new DataStore(new MySqlAppRepository(settings)).Data.Users.Any(u => u.Id == added.Id), "Yeni üyelik MySQL'de kalıcı");
Check(service.Login(added.Username, "Classroom123!"), "Yeni kullanıcı yeniden giriş yapabiliyor");
service.SetWeight(added, DateOnly.FromDateTime(DateTime.Today), 77);
Check(new DataStore(new MySqlAppRepository(settings)).Data.Users.Single(u => u.Id == added.Id).Profile.Weight == 77, "Profil güncellemesi MySQL'de kalıcı");
bool denied = false;
try { service.AdminSetUserActive(coach.Id, false); } catch (InvalidOperationException) { denied = true; }
Check(denied, "Üye yönetici işlemine erişemiyor");
store.Data.Users.RemoveAll(u => u.Id == added.Id);
store.Data.Weights.RemoveAll(w => w.UserId == added.Id);
service.Save();
Check(!new DataStore(new MySqlAppRepository(settings)).Data.Users.Any(u => u.Id == added.Id), "Silinen test kaydı tekrar açıldığında geri gelmiyor");
await EmailControllerChecks.Run(service, Check);
Console.WriteLine("COURSE_INTEGRATION_PASSED=" + passed);
