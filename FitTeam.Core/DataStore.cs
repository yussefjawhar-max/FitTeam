using System.Security.Cryptography;
using System.Text.Json;

namespace FitTeam.Core;

// Veriler kullanıcıya ait yerel klasörde tutulur. Bozuk dosya sessizce sıfırlanmaz.
public sealed class DataStore
{
    private readonly IAppRepository? repository;
    public string FilePath { get; }
    public AppData Data { get; }
    public bool UsesDatabase => repository != null;
    public string StorageDescription => repository?.StorageId ?? (FilePath.Length == 0 ? "Örnek veriler" : "Eski yerel dosya");
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };
    public DataStore(string? filePath = null)
    {
        FilePath = filePath ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FitTeam", "fitteam.json");
        Data = File.Exists(FilePath)
            ? JsonSerializer.Deserialize<AppData>(File.ReadAllText(FilePath), Options) ?? throw new InvalidDataException("Veri dosyası boş veya geçersiz.")
            : DemoData.Create();
        if (Data.Version != 1) throw new InvalidDataException("Bu veri dosyası sürümü desteklenmiyor.");
        TurkishDisplay.Apply(Data);
        GymMigration.Apply(Data);
        var deputySeed=FilePath.Length>0?Path.Combine(Path.GetDirectoryName(Path.GetFullPath(FilePath))!,"private-deputy-setup.json"):"";
        if(deputySeed.Length>0&&File.Exists(deputySeed))
        {
            PrivateDeputySetup.Apply(Data,PrivateDeputySetup.Read(deputySeed));Save();
            File.Move(deputySeed,deputySeed+".imported",true);
        }
        if(FilePath.Length>0&&Data.FitnessCoachExpansionRevision<1&&(Data.Users.Any(user=>user.ExampleBatch==RequestedExamples.Batch)||!File.Exists(FilePath)))
        {
            AdditionalFitnessCoaches.Apply(Data);Save();
        }
        if(FilePath.Length>0&&Data.ClubRosterRevision<RequestedClubRoster.Revision&&Data.Users.Any(u=>u.ExampleBatch==RequestedClubStudents.Batch))
        {
            var added=RequestedClubRoster.Upgrade(Data,DateOnly.FromDateTime(DateTime.Today));
            var reportPath=FilePath+".club-credentials.txt";
            if(added.Count>0)
            {
                var lines=added.Select(a=>"Kulüp: "+a.Club+" | Ad soyad: "+a.Name+"\nKullanıcı adı: "+a.Username+" | Şifre: "+a.Password);
                File.WriteAllText(reportPath,string.Join("\n\n",lines));
                if(!OperatingSystem.IsWindows())File.SetUnixFileMode(reportPath,UnixFileMode.UserRead|UnixFileMode.UserWrite);
            }
            Save();
        }
        if(Data.AddressDistributionRevision<RequestedAddressDistribution.Revision&&Data.Users.Any(u=>u.ExampleBatch==RequestedClubStudents.Batch))
        {
            RequestedAddressDistribution.Apply(Data);Save();
        }
        if(Data.GoalDistributionRevision<RequestedGoalDistribution.Revision&&Data.Users.Any(u=>u.ExampleBatch==RequestedClubStudents.Batch))
        {
            RequestedGoalDistribution.Apply(Data,DateOnly.FromDateTime(DateTime.Today));Save();
        }
        if (!File.Exists(FilePath)) Save();
    }
    public DataStore(AppData previewData) { FilePath = ""; Data = previewData; }
    public DataStore(IAppRepository repository, string? legacyFile = null)
    {
        this.repository = repository;
        FilePath = repository.StorageId;
        var loaded = repository.Load();
        Data = loaded ?? (legacyFile != null && File.Exists(legacyFile)
            ? JsonSerializer.Deserialize<AppData>(File.ReadAllText(legacyFile), Options) ?? throw new InvalidDataException("Eski veri dosyası okunamadı; asıl dosya korunuyor.")
            : DemoData.Create());
        if (Data.Version != 1) throw new InvalidDataException("Bu veri sürümü desteklenmiyor.");
        TurkishDisplay.Apply(Data);
        GymMigration.Apply(Data);
        if (loaded == null) repository.Save(Data);
    }
    public User? FindLoginUser(string identifier)
    {
        if (repository == null)
            return Data.Users.FirstOrDefault(u => u.Username.Equals(identifier, StringComparison.OrdinalIgnoreCase)
                || u.Email.Length > 0 && u.Email.Equals(identifier, StringComparison.OrdinalIgnoreCase)
                || u.Phone.Length > 0 && u.Phone == identifier);
        var fresh = repository.FindUser(identifier);
        if (fresh == null) return null;
        int index = Data.Users.FindIndex(u => u.Id == fresh.Id);
        if (index < 0) { Data.Users.Add(fresh); return fresh; }
        var existing = Data.Users[index];
        foreach (var property in typeof(User).GetProperties().Where(p => p.CanRead && p.CanWrite))
            property.SetValue(existing, property.GetValue(fresh));
        return existing;
    }
    public static DataStore Preview() => new(DemoData.Create());
    public void Save()
    {
        if (repository != null) { repository.Save(Data); return; }
        if (FilePath.Length == 0) return;
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(FilePath))!);
        string temp = FilePath + ".tmp";
        File.WriteAllText(temp, JsonSerializer.Serialize(Data, Options));
        if (File.Exists(FilePath)) File.Copy(FilePath, FilePath + ".bak", true);
        File.Move(temp, FilePath, true);
    }
    public void ExportBackup(string path) => File.WriteAllText(path, JsonSerializer.Serialize(Data, Options));
}
public static class Passwords
{
    public static IAccountPasswordProtector? CopyProtector {get;set;}
    public static void Set(User user, string password)
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        var hash=Convert.ToBase64String(Rfc2898DeriveBytes.Pbkdf2(password,salt,100_000,HashAlgorithmName.SHA256,32));
        var protectedPassword=CopyProtector?.Protect(user.Id,password)??"";
        user.Salt = Convert.ToBase64String(salt);
        user.PasswordHash=hash;user.ProtectedPassword=protectedPassword;
    }
    public static bool Verify(User user, string password)
    {
        var actual = Rfc2898DeriveBytes.Pbkdf2(password, Convert.FromBase64String(user.Salt), 100_000, HashAlgorithmName.SHA256, 32);
        return CryptographicOperations.FixedTimeEquals(actual, Convert.FromBase64String(user.PasswordHash));
    }
    public static string? ReadCopy(User user)
    {
        var value=CopyProtector?.Unprotect(user.Id,user.ProtectedPassword);
        return value!=null&&user.Salt.Length>0&&user.PasswordHash.Length>0&&Verify(user,value)?value:null;
    }
    public static bool RememberVerified(User user,string password)
    {
        if(CopyProtector==null||ReadCopy(user)==password||!Verify(user,password))return false;
        user.ProtectedPassword=CopyProtector.Protect(user.Id,password);return true;
    }
}
