namespace FitTeam.Core;

// Yalnız uygulamanın oluşturduğu kurgu kayıtlarındaki eski otomatik adresleri çeşitlendirir.
public static class RequestedAddressDistribution
{
    public const int Revision=1;
    private static readonly (string City,string District,string Neighborhood,string Street)[] Places=
    [
        ("İstanbul","Bağcılar","15 Temmuz","1024. Sokak"),
        ("İstanbul","Bağcılar","Güneşli","Çınar Sokak"),
        ("İstanbul","Bağcılar","Mahmutbey","Çam Sokak"),
        ("İstanbul","Sarıyer","Sarıyer Merkez","Deniz Sokak"),
        ("İstanbul","Sarıyer","İstinye","Yalı Sokak"),
        ("İstanbul","Güngören","Mehmet Nesih Özmen","Gül Sokak"),
        ("İstanbul","Güngören","Abdurrahman Nafiz Gürman","Çiçek Sokak"),
        ("İstanbul","Zeytinburnu","Sümer","34. Sokak"),
        ("İstanbul","Zeytinburnu","Kazlıçeşme","Sahil Sokak"),
        ("İstanbul","Kadıköy","Bostancı","Bahar Sokak"),
        ("Ankara","Çankaya","Bahçelievler","22. Sokak"),
        ("İzmir","Karşıyaka","Bostanlı","2018. Sokak"),
        ("Bursa","Nilüfer","Görükle","Menekşe Sokak"),
        ("Antalya","Muratpaşa","Meltem","376. Sokak")
    ];
    public static int Apply(AppData data)
    {
        if(data.AddressDistributionRevision>=Revision)return 0;
        int updated=0,index=0;
        foreach(var user in data.Users.Where(u=>u.IsExample&&(u.ExampleBatch==RequestedExamples.Batch||u.ExampleBatch==RequestedClubStudents.Batch)).OrderBy(u=>u.Username,StringComparer.Ordinal))
        {
            var p=user.Profile;int position=index++;
            if(p.CountryCode!="TR"||!IsOriginalAddress(p))continue;
            var place=Places[position%Places.Length];int cycle=position/Places.Length;
            var address=new PostalAddress{Province=place.City,District=place.District,Neighborhood=place.Neighborhood,
                Street=place.Street,Building=(1+cycle%72).ToString(),Apartment=(1+(cycle*3+position)%16).ToString()};
            address.Validate("TR");p.Address=address;p.City=address.Province;p.HomeAddress=address.Display;updated++;
        }
        data.AddressDistributionRevision=Revision;return updated;
    }
    private static bool IsOriginalAddress(Profile p)
    {
        if(p.Address==null)return p.HomeAddress.StartsWith("Yenişehir Mahallesi, Spor Sokak No: ",StringComparison.Ordinal);
        var a=p.Address;if(a.Street!="Spor Sokak"||p.HomeAddress!=a.Display)return false;
        var province=AddressCatalog.Province(a.Province);if(province==null)return false;
        var district=province.Name=="İstanbul"?AddressCatalog.District(province.Id,"Bağcılar"):AddressCatalog.Districts.FirstOrDefault(d=>d.ProvinceId==province.Id);
        return district!=null&&a.District==district.Name&&a.Neighborhood==AddressCatalog.Neighborhoods.First(n=>n.DistrictId==district.Id).Name;
    }
}
