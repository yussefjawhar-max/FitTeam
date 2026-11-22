using System.Globalization;
using System.Text;
using System.Text.Json;
namespace FitTeam.Core;
public sealed class CountryOption
{
    public string Code {get;set;}="";
    public string Turkish {get;set;}="";
    public string English {get;set;}="";
    public string Arabic {get;set;}="";
    public string Name=>AppLanguage.Code=="ar"?Arabic:AppLanguage.Code=="en"?English:Turkish;
    public override string ToString()=>Name;
}
public static class CountryCatalog
{
    public static readonly IReadOnlyList<CountryOption> All=Load();
    private static List<CountryOption> Load()
    {
        using var stream=typeof(CountryCatalog).Assembly.GetManifestResourceStream("FitTeam.Core.Countries.countries.json")??throw new InvalidOperationException("Ülke listesi bulunamadı.");return JsonSerializer.Deserialize<List<CountryOption>>(stream)!;
    }
    private static string Normalize(string input)=>new(input.Normalize(NormalizationForm.FormD).Where(c=>CharUnicodeInfo.GetUnicodeCategory(c)!=UnicodeCategory.NonSpacingMark).Select(c=>char.ToLowerInvariant(c=='ı'?'i':c)).ToArray());
    public static bool Matches(CountryOption option,string query)
    {
        string key=Normalize(query.Trim());if(key.Length==0)return true;
        return new[]{option.Turkish,option.English,option.Arabic,option.Code,option.Code=="TR"?"Turkey":"",option.Code=="SA"?"سعودية السعودية":""}.Any(n=>Normalize(n).StartsWith(key,StringComparison.Ordinal)||n.Split(' ').Any(w=>Normalize(w).TrimStart('ا','ل').StartsWith(key,StringComparison.Ordinal)));
    }
    public static CountryOption? Resolve(string? value)
    {
        string key=Normalize(value?.Trim()??"");if(key.Length==0)return null;
        return All.FirstOrDefault(c=>new[]{c.Code,c.Turkish,c.English,c.Arabic,c.Code=="TR"?"Turkey":""}.Any(n=>Normalize(n)==key));
    }
    public static CountryOption? ByCode(string? code)=>All.FirstOrDefault(c=>c.Code.Equals(code,StringComparison.OrdinalIgnoreCase));
}
