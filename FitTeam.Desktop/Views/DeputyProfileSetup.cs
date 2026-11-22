using Avalonia.Controls;
using FitTeam.Core;
namespace FitTeam.Desktop.Views;
public partial class MainWindow
{
    private async Task<bool> DeputyProfileSetupDialog()
    {
        bool saved=false;var name=Ui.Input("","Adın ve soyadın");var age=Ui.Number(20,13,90);var gender=Ui.Combo(["Kadın","Erkek","Belirtmek istemiyorum"],"Belirtmek istemiyorum");
        var email=Ui.Input(Me.Email,"E-posta adresin (isteğe bağlı)");var phone=Ui.Input(Me.Phone,"+905… (isteğe bağlı)");var city=Ui.Input(Me.Profile.City);
        var error=Ui.Text("",13,Ui.Muted);var panel=Ui.Stack(Ui.Text("Yardımcı hesabını tamamla",25,Ui.Ink,true),Ui.UserText(Me.Username,16),
            Ui.Text("Kendi bilgilerini doldur. Kullanıcı adın ve yönetim erişimin korunur.",14,Ui.Muted),Ui.Field("Ad soyad",name,450),Ui.Field("Yaş",age,450),Ui.Field("Cinsiyet",gender,450),Ui.Field("E-posta",email,450),Ui.Field("Telefon",phone,450),Ui.Field("Şehir",city,450),error);
        var w=Ui.Dialog("FitTeam • Yardımcı bilgileri",panel,590,750);
        panel.Children.Add(Ui.Button("Bilgilerimi kaydet",()=>{try{service.CompleteDeputyProfile(name.Text??"",Ui.Int(age),Ui.Selected(gender),email.Text??"",phone.Text??"",city.Text??"");saved=true;w.Close();}catch(Exception ex){error.Text=Ui.ErrorMessage(ex);}},true));
        panel.Children.Add(Ui.Button("Vazgeç",()=>w.Close()));await w.ShowDialog(this);return saved;
    }
}
