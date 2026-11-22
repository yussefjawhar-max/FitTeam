using Avalonia.Controls;
using FitTeam.Core;
namespace FitTeam.Desktop;
internal sealed class AddressEditor
{
    public AutoCompleteBox Province {get;} = Pick("İl / şehir");
    public AutoCompleteBox District {get;} = Pick("İlçe");
    public AutoCompleteBox Neighborhood {get;} = Pick("Mahalle / köy");
    public TextBox Street {get;} = Ui.Input("","Sokak veya cadde adı");
    public TextBox Building {get;} = Ui.Input("","Bina numarası");
    public TextBox Apartment {get;} = Ui.Input("","Daire numarası; müstakilse 0");
    public Control View {get;}
    private readonly AutoCompleteBox country;
    private bool updating;
    public AddressEditor(AutoCompleteBox country, PostalAddress? address=null, string city="")
    {
        this.country=country;Province.Text=address?.Province??city;District.Text=address?.District??"";Neighborhood.Text=address?.Neighborhood??"";
        Street.Text=address?.Street??"";Building.Text=address?.Building??"";Apartment.Text=address?.Apartment??"";
        View=Ui.Stack(Ui.Text("Adres bilgilerin",18,Ui.Ink,true),Ui.Row(Ui.Field("İl / şehir *",Province,245),Ui.Field("İlçe *",District,245)),
            Ui.Row(Ui.Field("Mahalle / köy *",Neighborhood,245),Ui.Field("Sokak / cadde *",Street,245)),
            Ui.Row(Ui.Field("Bina no *",Building,245),Ui.Field("Daire no *",Apartment,245)));
        UpdateLists();
        country.TextChanged+=(_,_)=>{if(updating)return;updating=true;Province.Text=District.Text=Neighborhood.Text="";Street.Text=Building.Text=Apartment.Text="";updating=false;UpdateLists();};
        Province.TextChanged+=(_,_)=>{if(updating)return;updating=true;District.Text=Neighborhood.Text="";Street.Text=Building.Text=Apartment.Text="";updating=false;UpdateLists();};
        District.TextChanged+=(_,_)=>{if(updating)return;updating=true;Neighborhood.Text="";Street.Text=Building.Text=Apartment.Text="";updating=false;UpdateLists();};
    }
    private static AutoCompleteBox Pick(string label)=>new(){Watermark=AppLanguage.Text(label),MinimumPrefixLength=1,MinimumPopulateDelay=TimeSpan.FromMilliseconds(100),IsTextCompletionEnabled=false,MaxDropDownHeight=240,
        FilterMode=AutoCompleteFilterMode.Custom,ItemFilter=(query,item)=>item is string value&&AddressCatalog.Key(value).Contains(AddressCatalog.Key(query??"")),
        ItemTemplate=new Avalonia.Controls.Templates.FuncDataTemplate<string>((item,_)=>Ui.UserText(item??""))};
    private void UpdateLists()
    {
        bool turkey=CountryCatalog.Resolve(country.Text)?.Code=="TR";
        Province.ItemsSource=turkey?AddressCatalog.Provinces.Select(p=>p.Name).Order().ToList():[];
        var p=turkey?AddressCatalog.Province(Province.Text??""):null;
        District.ItemsSource=p==null?[]:AddressCatalog.Districts.Where(d=>d.ProvinceId==p.Id).Select(d=>d.Name).Order().ToList();
        var d=p==null?null:AddressCatalog.District(p.Id,District.Text??"");
        Neighborhood.ItemsSource=d==null?[]:AddressCatalog.Neighborhoods.Where(n=>n.DistrictId==d.Id).Select(n=>n.Name).Order().ToList();
    }
    public PostalAddress Read() => new(){Province=Province.Text??"",District=District.Text??"",Neighborhood=Neighborhood.Text??"",Street=Street.Text??"",Building=Building.Text??"",Apartment=Apartment.Text??""};
    public PostalAddress Value()
    {
        var address=Read();
        address.Validate(CountryPicker.Selected(country).Code);return address;
    }
}
