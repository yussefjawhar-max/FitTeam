using Avalonia.Controls;
using Avalonia.Data;
using FitTeam.Core;
namespace FitTeam.Desktop;
internal static class CountryPicker
{
    public static AutoCompleteBox Create(string value="",string code="")
    {
        var selected=CountryCatalog.ByCode(code)??CountryCatalog.Resolve(value);
        var box=new AutoCompleteBox{ItemsSource=CountryCatalog.All.OrderBy(c=>c.Name).ToList(),MinimumPrefixLength=1,MinimumPopulateDelay=TimeSpan.FromMilliseconds(100),IsTextCompletionEnabled=false,MaxDropDownHeight=260,FilterMode=AutoCompleteFilterMode.Custom,ItemFilter=(search,item)=>item is CountryOption country&&CountryCatalog.Matches(country,search??""),ValueMemberBinding=new Binding(nameof(CountryOption.Name)),ItemTemplate=new Avalonia.Controls.Templates.FuncDataTemplate<CountryOption>((item,_)=>Ui.UserText(item?.Name??"")),Text=selected?.Name??value,SelectedItem=selected,Watermark=AppLanguage.Text("Ülke yaz ve listeden seç")};
        Avalonia.Automation.AutomationProperties.SetName(box,AppLanguage.Text("Ülke"));return box;
    }
    public static CountryOption Selected(AutoCompleteBox box)=>CountryCatalog.Resolve(box.Text)??throw new ArgumentException("Listeden bir ülke seç.");
}
