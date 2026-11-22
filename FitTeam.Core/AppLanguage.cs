using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
namespace FitTeam.Core;

public static class AppLanguage
{
    public static string Code { get; private set; } = "tr";
    public static bool RightToLeft => Code == "ar";
    private static Dictionary<string,string> messages = [];
    private static List<(Regex Pattern,string Value)> templates = [];
    private static List<(Regex Pattern,string Value)> fragments = [];
    private static readonly Dictionary<string,string> cache=[];
    public static void Use(string code)
    {
        if(code is not ("tr" or "ar" or "en"))throw new ArgumentException("Desteklenmeyen dil.");
        Code=code;
        var culture=(CultureInfo)CultureInfo.GetCultureInfo(code=="ar"?"ar-EG":code=="en"?"en-US":"tr-TR").Clone();
        culture.DateTimeFormat.Calendar=new GregorianCalendar();
        CultureInfo.DefaultThreadCurrentCulture=culture;CultureInfo.DefaultThreadCurrentUICulture=culture;
        CultureInfo.CurrentCulture=culture;CultureInfo.CurrentUICulture=culture;
        messages=[];templates=[];fragments=[];cache.Clear();
        if(code=="tr")return;
        using var stream=typeof(AppLanguage).Assembly.GetManifestResourceStream("FitTeam.Core.Localization."+code+".json")??throw new InvalidOperationException("Dil dosyası bulunamadı.");
        messages=JsonSerializer.Deserialize<Dictionary<string,string>>(stream)!;
        using var extra=typeof(AppLanguage).Assembly.GetManifestResourceStream("FitTeam.Core.Localization."+code+".extra.json");
        if(extra!=null)foreach(var entry in JsonSerializer.Deserialize<Dictionary<string,string>>(extra)!)messages[entry.Key]=entry.Value;

        foreach(var entry in messages)
        {
            if(Regex.IsMatch(entry.Key,@"\{\d+\}"))
            {
                var pieces=Regex.Split(entry.Key,@"(\{\d+\})");
                var pattern="^"+string.Concat(pieces.Select(p=>Regex.IsMatch(p,@"^\{\d+\}$")?"(?<v"+p[1..^1]+">.*?)":Regex.Escape(p)))+"$";
                templates.Add((new Regex(pattern,RegexOptions.Singleline|RegexOptions.CultureInvariant,TimeSpan.FromMilliseconds(100)),entry.Value));
            }
            else if(entry.Key.Length>=3)fragments.Add((new Regex(@"(?<![\p{L}\p{N}])"+Regex.Escape(entry.Key)+@"(?![\p{L}\p{N}])",RegexOptions.CultureInvariant,TimeSpan.FromMilliseconds(100)),entry.Value));
        }
        fragments=fragments.OrderByDescending(e=>e.Pattern.ToString().Length).ToList();
    }
    public static string Text(string? value)
    {
        if(string.IsNullOrEmpty(value)||Code=="tr")return value??"";
        if(cache.TryGetValue(value,out var cached))return cached;
        string trimmed=value.Trim();
        if(messages.TryGetValue(trimmed,out var exact))return Remember(value,PreserveEdges(value,trimmed,exact));
        foreach(var (pattern,translation) in templates)
        {
            var match=pattern.Match(trimmed);if(!match.Success)continue;
            string result=Regex.Replace(translation,@"\{(\d+)\}",m=>match.Groups["v"+m.Groups[1].Value].Value);
            return Remember(value,PreserveEdges(value,trimmed,result));
        }
        // Çevrilebilir sabit parçaları değiştirir; kullanıcı adlarını ve notlarını yeniden yazmaz.
        string output=value;
        foreach(var entry in fragments)output=entry.Pattern.Replace(output,_=>entry.Value);
        return Remember(value,output);
    }
    private static string Remember(string source,string translated){if(cache.Count>5000)cache.Clear();cache[source]=translated;return translated;}
    private static string PreserveEdges(string original,string trimmed,string translated)
    {
        int start=original.IndexOf(trimmed,StringComparison.Ordinal);
        return original[..start]+translated+original[(start+trimmed.Length)..];
    }
}
