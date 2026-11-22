using Avalonia.Controls;
using FitTeam.Core;
namespace FitTeam.Desktop.Views;
public partial class MainWindow
{
    private Control LanguageSelector()
    {
        var panel=Ui.Stack(Ui.Text("Uygulama dili",13,Ui.Muted));
        var buttons=Ui.Row();
        foreach(var (code,label) in new[]{("tr","Türkçe"),("ar","العربية"),("en","English")})
        {
            string chosen=code;
            buttons.Children.Add(Ui.Button(label,()=>{try{LanguageView.Select(chosen);if(shell!=null)LanguageView.Refresh(shell);ShowLogin();}catch(Exception ex){_ = Ui.Message(this,Ui.ErrorMessage(ex));}},AppLanguage.Code==code));
        }
        panel.Children.Add(buttons);return panel;
    }
    private Control LanguageSettingsPage()
    {
        var panel=Ui.Stack(Ui.Text("Uygulama dili",26,Ui.Ink,true),Ui.Text("Dil seçimini bu bilgisayarda hatırla.",14,Ui.Muted));
        foreach(var (code,label) in new[]{("tr","Türkçe"),("ar","العربية"),("en","English")})
        {
            string selected=code;var button=Ui.Button(label,()=>{try{LanguageView.Select(selected);FlowDirection=LanguageView.Direction;Title=AppLanguage.Text("FitTeam • Antrenman ve Sporcu Takibi");if(shell!=null)LanguageView.Apply(shell);ShowShell();ShowPage("Uygulama dili");}catch(Exception ex){_ = Ui.Message(this,Ui.ErrorMessage(ex));}},AppLanguage.Code==code);
            button.Content=Ui.UserText(label,18,Ui.Ink,true);panel.Children.Add(button);
        }
        return Ui.Page(Ui.Card(panel));
    }

}
