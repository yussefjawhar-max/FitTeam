using System.Globalization;
using System.Text;
using System.Text.Json;
namespace FitTeam.Core;
public sealed class AddressPlace
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public int ProvinceId { get; set; }
    public int DistrictId { get; set; }
}
public static class AddressCatalog
{
    public static readonly List<AddressPlace> Provinces = Load("provinces");
    public static readonly List<AddressPlace> Districts = Load("districts");
    public static readonly List<AddressPlace> Neighborhoods = Load("neighborhoods");
    private static List<AddressPlace> Load(string name)
    {
        using var stream = typeof(AddressCatalog).Assembly.GetManifestResourceStream("FitTeam.Core.Addresses." + name + ".json")!;
        return JsonSerializer.Deserialize<List<AddressPlace>>(stream)!;
    }
    public static string Key(string value) => new(value.Normalize(NormalizationForm.FormD)
        .Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
        .Select(c => char.ToLowerInvariant(c == 'ı' ? 'i' : c)).ToArray());
    public static AddressPlace? Province(string name) => Provinces.FirstOrDefault(p => Key(p.Name) == Key(name.Trim()));
    public static AddressPlace? District(int provinceId, string name) => Districts.FirstOrDefault(p => p.ProvinceId == provinceId && Key(p.Name) == Key(name.Trim()));
}
public sealed class PostalAddress
{
    public string Province { get; set; } = "";
    public string District { get; set; } = "";
    public string Neighborhood { get; set; } = "";
    public string Street { get; set; } = "";
    public string Building { get; set; } = "";
    public string Apartment { get; set; } = "";
    public string Display => $"{Neighborhood} Mahallesi, {Street}, Bina No: {Building}, Daire No: {Apartment}, {District} / {Province}";
    public void Validate(string countryCode)
    {
        Province=Province.Trim();District=District.Trim();Neighborhood=Neighborhood.Trim();Street=Street.Trim();
        Building=LoginContact.NormalizeDigits(Building.Trim());Apartment=LoginContact.NormalizeDigits(Apartment.Trim());
        if(new[]{Province,District,Neighborhood,Street}.Any(v=>v.Length is <2 or >160)||new[]{Building,Apartment}.Any(v=>v.Length is <1 or >12||v.Any(c=>!char.IsLetterOrDigit(c)&&c is not ('/' or '-'))))
            throw new ArgumentException("İl, ilçe, mahalle, sokak, bina ve daire numarasını doldur.");
        if(countryCode=="TR")
        {
            var province=AddressCatalog.Province(Province)??throw new ArgumentException("Listeden Türkiye ilini seç.");
            var district=AddressCatalog.District(province.Id,District)??throw new ArgumentException("Seçtiğin ile bağlı ilçeyi seç.");
            Province=province.Name;District=district.Name;
        }
    }
}
