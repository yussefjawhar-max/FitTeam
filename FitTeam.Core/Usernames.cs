using System.Text;
namespace FitTeam.Core;
public static class Usernames
{
    public static string Validate(string value)
    {
        value = value.Trim().Normalize(NormalizationForm.FormC);
        if (value.Length is < 3 or > 20 || value.Any(c => !char.IsLetterOrDigit(c) && c is not ('_' or '.' or '-')))
            throw new ArgumentException("Kullanıcı adı 3–20 karakter olmalı; harf, rakam, nokta, alt çizgi veya tire kullan.");
        return value;
    }
}
public sealed partial class FitService
{
    public string ValidateOwnerUsername(string username)
    {
        RequirePresident();username=Usernames.Validate(username);
        if(Data.Users.Any(u=>u.Id!=Current!.Id&&(u.Username.Equals(username,StringComparison.OrdinalIgnoreCase)||u.Email.Equals(username,StringComparison.OrdinalIgnoreCase)||u.Phone==username)))throw new ArgumentException("Bu kullanıcı adı başka bir hesaba ait. Başka bir ad seç.");
        return username;
    }
    public void ChangeOwnUsername(string username)
    {
        var user = RequireCurrent();
        if (IsPresident) throw new InvalidOperationException("Yetkili kullanıcı adını kendi parola ekranından değiştir.");
        username = Usernames.Validate(username);
        if(username.Equals(Data.OwnerLoginUsername,StringComparison.OrdinalIgnoreCase))throw new ArgumentException("Bu kullanıcı adı asıl yetkiliye ait.");
        if (Data.Users.Any(u => u.Id != user.Id && u.Username.Equals(username, StringComparison.OrdinalIgnoreCase)))
            throw new ArgumentException("Bu kullanıcı adı kullanılıyor. Başka bir ad seç.");
        user.Username = username; store.Save();
    }
}
