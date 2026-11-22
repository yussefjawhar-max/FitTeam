using System.Text.RegularExpressions;
namespace FitTeam.Core;

public static class LoginContact
{
    public static void RequireEmailChannel(string channel)
    {
        if(channel!="email")throw new ArgumentException("Kodla doğrulama için Gmail kullanın.");
    }
    public static string NormalizeDigits(string value)=>new(value.Select(c=>c is >= '\u0660' and <= '\u0669'?(char)('0'+c-'\u0660'):c is >= '\u06F0' and <= '\u06F9'?(char)('0'+c-'\u06F0'):c).ToArray());
    public static string Gmail(string localPart)
    {
        var value=localPart.Trim().ToLowerInvariant();
        if(value.Length is < 1 or > 64 || !Regex.IsMatch(value,@"^[a-z0-9]+(?:[a-z0-9.]*[a-z0-9])?$") || value.Contains(".."))
            throw new ArgumentException("Gmail adresinin yalnız @gmail.com öncesindeki kısmını yaz.");
        return value+"@gmail.com";
    }
    public static string TurkishMobile(string remainingDigits)
    {
        var digits=Regex.Replace(NormalizeDigits(remainingDigits),@"[\s()-]","");
        if(!Regex.IsMatch(digits,@"^[0-9]{9}$"))
            throw new ArgumentException("+90 5 sonrasındaki 9 rakamı yaz. Örnek: 32 123 45 67.");
        return "+905"+digits;
    }
}
