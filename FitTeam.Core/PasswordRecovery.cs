namespace FitTeam.Core;

public sealed class PasswordRecoveryTicket
{
    public string Name {get;}
    public DateTimeOffset ExpiresAt {get;}
    internal PasswordRecoveryTicket(string name,DateTimeOffset expires){Name=name;ExpiresAt=expires;}
}
public sealed partial class FitService
{
    private sealed record RecoveryGrant(Guid UserId,string Hash,string Salt,CodeIdentity Proof);
    private readonly object recoveryGate=new();
    private readonly Dictionary<PasswordRecoveryTicket,RecoveryGrant> recoveryGrants=[];
    private readonly HashSet<CodeIdentity> usedRecoveryProofs=[];
    public User RecoveryAccount(string identifier,CodeIdentity proof,string? role=null,string? sport=null)
    {
        const string error="Hesap bilgileri ve doğrulanan Gmail veya telefon eşleşmiyor. Bilgilerini kontrol et.";
        var key=LoginContact.NormalizeDigits(identifier.Trim());
        var users=Data.Users.Where(user=>user.Username.Equals(key,StringComparison.OrdinalIgnoreCase)||
            user.Email.Length>0&&user.Email.Equals(key,StringComparison.OrdinalIgnoreCase)||user.Phone.Length>0&&user.Phone==key||
            Data.OwnerLoginUsername.Length>0&&key.Equals(Data.OwnerLoginUsername,StringComparison.OrdinalIgnoreCase)&&user.Email.Equals("jawharyusuf446@gmail.com",StringComparison.OrdinalIgnoreCase)).ToList();
        if(users.Count!=1)throw new InvalidOperationException(error);
        var target=users[0];
        bool email=proof.Channel is "email" or "google+email" or "google+phone";
        bool phone=proof.Channel=="phone";
        if(!target.IsActive||target.IsClubAccount||(!email&&!phone)||
            email&&(target.Email.Length==0||!target.Email.Equals(proof.Contact,StringComparison.OrdinalIgnoreCase))||
            phone&&(target.Phone.Length==0||target.Phone!=proof.Contact)||
            proof.Channel.StartsWith("google+")&&string.IsNullOrEmpty(proof.GoogleSubject)||
            proof.Channel=="google+phone"&&(target.Phone.Length==0||target.Phone!=proof.Phone)||
            target.AuthenticationProject=="google"&&target.AuthenticationUid.Length>0&&proof.GoogleSubject.Length>0&&target.AuthenticationUid!=proof.GoogleSubject||
            role!=null&&!MatchesTrainerEntry(target,role,sport))throw new InvalidOperationException(error);
        bool owner=target.Email.Equals("jawharyusuf446@gmail.com",StringComparison.OrdinalIgnoreCase);
        if(email&&Data.Users.Count(user=>user.Email.Equals(proof.Contact,StringComparison.OrdinalIgnoreCase))!=1||phone&&Data.Users.Count(user=>user.Phone==proof.Contact)!=1)throw new InvalidOperationException(error);
        if(proof.IsOwner!=owner)throw new InvalidOperationException(error);
        return target;
    }
    public PasswordRecoveryTicket BeginPasswordRecovery(string identifier,CodeIdentity proof,string? role=null,string? sport=null)
    {
        var target=RecoveryAccount(identifier,proof,role,sport);
        if(proof.IsOwner)throw new InvalidOperationException("Başkan şifresini doğrulanan başkan hesabının kurtarma ekranından değiştir.");
        lock(recoveryGate)
        {
            if(!usedRecoveryProofs.Add(proof))throw new InvalidOperationException("Bu doğrulama kullanıldı. Yeniden Gmail kodu iste.");
            foreach(var old in recoveryGrants.Where(pair=>pair.Value.UserId==target.Id||pair.Key.ExpiresAt<=(clock??TimeProvider.System).GetUtcNow()).Select(pair=>pair.Key).ToList())recoveryGrants.Remove(old);
            var ticket=new PasswordRecoveryTicket(target.Profile.Name,(clock??TimeProvider.System).GetUtcNow().AddMinutes(5));
            recoveryGrants[ticket]=new(target.Id,target.PasswordHash,target.Salt,proof);return ticket;
        }
    }
    public void ResetRecoveredPassword(PasswordRecoveryTicket ticket,string password,string confirmation)
    {
        lock(recoveryGate)
        {
            if(!recoveryGrants.TryGetValue(ticket,out var grant)||ticket.ExpiresAt<=(clock??TimeProvider.System).GetUtcNow())
                throw new InvalidOperationException("Şifre kurtarma süresi doldu veya kullanıldı. Yeniden doğrula.");
            var user=Data.Users.SingleOrDefault(person=>person.Id==grant.UserId);
            if(user==null||!user.IsActive||user.PasswordHash!=grant.Hash||user.Salt!=grant.Salt)
                throw new InvalidOperationException("Hesap değişti. Şifre kurtarmayı yeniden başlat.");
            RecoveryAccount(user.Username,grant.Proof);
            if(password!=confirmation)throw new ArgumentException("Şifreler eşleşmiyor.");
            if(password.Length<8||password.Length>128)throw new ArgumentException("Yeni FitTeam şifren 8–128 karakter olmalı.");
            if(user.Salt.Length>0&&Passwords.Verify(user,password))throw new ArgumentException("Yeni şifre mevcut şifreden farklı olmalı.");
            Passwords.Set(user,password);user.MustChangePassword=false;
            foreach(var old in recoveryGrants.Where(pair=>pair.Value.UserId==user.Id).Select(pair=>pair.Key).ToList())recoveryGrants.Remove(old);
            Logout();Save();
        }
    }
}
