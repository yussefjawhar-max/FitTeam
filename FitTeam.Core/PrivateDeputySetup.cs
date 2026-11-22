using System.Text.Json;
namespace FitTeam.Core;

public sealed class DeputySeedRecord
{
    public Guid Id {get;set;}
    public string Username {get;set;}="";
    public string Name {get;set;}="";
    public string Salt {get;set;}="";
    public string PasswordHash {get;set;}="";
}
public static class PrivateDeputySetup
{
    public static List<DeputySeedRecord> Read(string path)
        =>JsonSerializer.Deserialize<List<DeputySeedRecord>>(File.ReadAllText(path))??throw new InvalidDataException("Yardımcı hesap dosyası okunamadı.");
    public static int Apply(AppData data,List<DeputySeedRecord> accounts)
    {
        if(accounts.Count!=4||accounts.Select(account=>account.Id).Distinct().Count()!=4||accounts.Select(account=>account.Username.ToLowerInvariant()).Distinct().Count()!=4)
            throw new InvalidDataException("Dört farklı yardımcı hesabı gerekli.");
        foreach(var account in accounts)
        {
            Usernames.Validate(account.Username);
            if(account.Id==Guid.Empty||account.Name.Trim().Length<2||Convert.FromBase64String(account.Salt).Length!=16||Convert.FromBase64String(account.PasswordHash).Length!=32||
                account.Username.Equals(data.OwnerLoginUsername,StringComparison.OrdinalIgnoreCase)||
                data.Users.Any(user=>user.Id==account.Id&&(user.Role!=Choices.DeputyRole||!user.Username.Equals(account.Username,StringComparison.OrdinalIgnoreCase)))||
                data.Users.Any(user=>user.Username.Equals(account.Username,StringComparison.OrdinalIgnoreCase)&&(user.Id!=account.Id||user.Role!=Choices.DeputyRole)))
                throw new InvalidDataException("Yardımcı hesabı başka bir kayıtla çakışıyor.");
        }
        int added=0;foreach(var account in accounts)
        {
            if(data.Users.Any(user=>user.Id==account.Id))continue;
            data.Users.Add(new User{Id=account.Id,Username=account.Username,Role=Choices.DeputyRole,FullAdministrationView=true,
                PasswordHash=account.PasswordHash,Salt=account.Salt,RegisteredAt=DateTimeOffset.UtcNow,
                Profile=new(){Name=account.Name,Age=0,Gender="Belirtmek istemiyorum"},ProfileComplete=false});added++;
        }
        return added;
    }
}
