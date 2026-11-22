using Avalonia;
using Avalonia.Controls;
using FitTeam.Core;
namespace FitTeam.Desktop.Views;
public partial class MainWindow
{
    private readonly LocalIdentityServer identityServer = new();
    private CodeAuthClient? codeAuth;
    private async Task<CodeAuthClient> IdentityService() {if(service.IsPreview)throw new InvalidOperationException("Gerçek hesabına giriş yapmak için örnek pencereyi kapat ve asıl giriş ekranını kullan.");return codeAuth ??= await identityServer.Start();}
    private Task ContactChoiceDialog(string? requiredRole=null,string? requiredSport=null,string? title=null)
        => LocalPasswordEntryDialog(requiredRole,requiredSport);
    private async Task PresidentLoginDialog()
    {
        var auth=await IdentityService();
        bool initialized=await auth.PresidentInitialized();
        var username=Ui.Input("","Yetkili kullanıcı adın");var password=Ui.Input();password.PasswordChar='●';
        var repeat=Ui.Input();repeat.PasswordChar='●';var error=Ui.Text("",13,Ui.Muted);
        var panel=Ui.Stack(Ui.Text(initialized?"Yetkili girişi":"İlk yetkili hesabını oluştur",24,Ui.Ink,true),
            Ui.Text(initialized?"Yetkili kullanıcı adın ve FitTeam parolanla giriş yap. Parolanı başkan panelinden değiştirebilirsin.":"Bu bilgisayardaki FitTeam kayıtlarını yönetecek hesabı oluştur. Kullanıcı adını ve en az 8 karakterli FitTeam parolanı kendin belirle.",13,Ui.Muted),
            Ui.Field("Kullanıcı adı",username,430),Ui.Field(initialized?"Başkan parolası":"Yeni başkan parolası (en az 8 karakter)",password,430));
        if(!initialized)panel.Children.Add(Ui.Field("Parola tekrar",repeat,430));
        panel.Children.Add(error);
        var w=Ui.Dialog("FitTeam • Yetkili girişi",panel,540,initialized?500:580);
        panel.Children.Add(Ui.AsyncButton(initialized?"Yetkili paneline gir":"Yetkili hesabını oluştur",async ()=>{
            try
            {
                codeAuth?.Logout();
                if(initialized&&!(username.Text??"").Trim().Equals(service.Data.OwnerLoginUsername,StringComparison.OrdinalIgnoreCase)&&service.Login(username.Text??"",password.Text??"")&&Me.Role==Choices.DeputyRole)
                {
                    password.Text="";if(!await CompleteLocalLogin())return;w.Close();ShowShell();return;
                }
                service.Logout();
                if(!initialized)
                {
                    if(password.Text!=repeat.Text)throw new ArgumentException("Parolalar eşleşmiyor.");
                    var chosenUsername=Usernames.Validate(username.Text??"");
                    if(service.Data.Users.Any(user=>user.Username.Equals(chosenUsername,StringComparison.OrdinalIgnoreCase)||user.Email.Equals(chosenUsername,StringComparison.OrdinalIgnoreCase)||user.Phone==chosenUsername))
                        throw new ArgumentException("Bu kullanıcı adı başka bir hesaba ait. Başka bir ad seç.");
                    username.Text=chosenUsername;
                    await auth.InitializePresident(identityServer.SetupToken,chosenUsername,password.Text??"");
                    initialized=true;repeat.Text="";
                }
                service.LoginCode(await auth.PresidentLogin(username.Text??"",password.Text??""));
                ownerLoginName=(username.Text??"").Trim();service.Data.OwnerLoginUsername=ownerLoginName;service.Save();
                await auth.RegisterProfile(Me.Profile.Name,Me.Role);
                password.Text="";w.Close();ShowShell();ShowPage("Başkan paneli");
            }
            catch(Exception ex){error.Text=Ui.ErrorMessage(ex);}
        },true));
        bool recovery=false;
        if(initialized)panel.Children.Add(Ui.Button("Şifremi unuttum",()=>{recovery=true;password.Text="";w.Close();}));
        w.Closed+=(_,_)=>{password.Text=repeat.Text="";};
        await w.ShowDialog(this);
        if(recovery)await PasswordRecoveryDialog(username.Text??"");
    }
    private Control PresidentPage(bool listOnly=false)
    {
        if(!service.HasAdministrationAccess)throw new InvalidOperationException("Yetkili hesabıyla giriş yapın.");
        if(listOnly)return AdministrationPage();
        return Ui.Page(Ui.Card(Ui.Stack(Ui.Text("Yetkili • "+Me.Profile.Name,26,Ui.Ink,true),Ui.Text("Sayı kutusuna bas, isim listesini aç. Bir isme basarak bütün kayıtlarını incele.",15,Ui.Muted))),PresidentCountCards(),StudentLookupCard(),Ui.Card(Ui.Stack(Ui.Button("Kendi üye panelimi aç",EnterMemberWorkspace,true),Ui.Button("Fitness yönetimi",()=>ShowPage("Fitness yönetimi"),true),Ui.Button("Kulüp başkanları",()=>ShowPage("Kulüp başkanları"),true),Ui.Button("Kulüpler ve öğrenciler",()=>ShowPage("Kulüpler ve öğrenciler"),true),Ui.Button("Kişiler ve kulüpler",()=>ShowPage("Kişiler ve kulüpler")),service.IsPresident?Ui.AsyncButton("Kendi yetkili şifremi değiştir",()=>Guard(SetPresidentPasswordDialog)):Ui.Button("Kendi şifremi değiştir",()=>ShowPage("Şifrem")))));
    }
    private Control PresidentPasswordPage()
    {
        if(!service.IsPresident)throw new InvalidOperationException("Başkan yetkisi gerekiyor.");
        return Ui.Page(Ui.Card(Ui.Stack(
            Ui.Text("Başkan kullanıcı adım ve şifrem",26,Ui.Ink,true),
            Ui.Text("FitTeam kullanıcı adını ve şifreni bu sayfadan değiştirebilirsin.",14,Ui.Muted),
            Ui.AsyncButton("Kullanıcı adımı ve şifremi değiştir",()=>Guard(SetPresidentPasswordDialog),true))));
    }
    private async Task SetPresidentPasswordDialog()
    {
        if(service.IsPreview)throw new InvalidOperationException("Örnek panelde gerçek şifre değiştirilmez. Asıl giriş ekranını kullan.");
        if(!service.IsPresident)throw new InvalidOperationException("Başkan yetkisi gerekiyor.");
        var username=Ui.Input(ownerLoginName.Length==0?service.Data.OwnerLoginUsername:ownerLoginName);var pass=Ui.Input();pass.PasswordChar='●';var repeat=Ui.Input();repeat.PasswordChar='●';var error=Ui.Text("",13,Ui.Muted);
        var panel=Ui.Stack(Ui.Text("Başkan parolanı belirle",24,Ui.Ink,true),Ui.Field("Başkan kullanıcı adı",username,430),Ui.Field("Yeni parola (en az 8 karakter)",pass,430),Ui.Field("Parola tekrar",repeat,430),error);
        var w=Ui.Dialog("FitTeam • Özel başkan hesabı",panel,540,510);
        panel.Children.Add(Ui.AsyncButton("Kaydet",async ()=>{try {if(pass.Text!=repeat.Text)throw new ArgumentException("Parolalar eşleşmiyor.");var chosenUsername=service.ValidateOwnerUsername(username.Text??"");await codeAuth!.SetPresidentPassword(chosenUsername,pass.Text??"");ownerLoginName=chosenUsername;service.Data.OwnerLoginUsername=ownerLoginName;service.Save();pass.Text=repeat.Text="";w.Close();await Ui.Message(this,"Başkan hesabın hazır. Giriş ekranında Başkan girişi düğmesini kullanabilirsin.");}catch(Exception ex){error.Text=Ui.ErrorMessage(ex);}},true));await w.ShowDialog(this);
    }
}
