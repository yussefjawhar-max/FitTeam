using Avalonia.Controls;
using FitTeam.Core;
namespace FitTeam.Desktop.Views;
public partial class MainWindow
{
    private Control DeputiesPage()
    {
        if(!service.HasFullAdministrationView)throw new InvalidOperationException("Yetkili hesabıyla giriş yap.");
        var body=Ui.Stack(Ui.Text("Yetkili yardımcıları",26,Ui.Ink,true),Ui.Text("Yardımcı, Yetkili girişi ekranında kendi kullanıcı adı ve şifresiyle girer. Üye, hoca ve kulüp işlemlerini yönetir; asıl yetkilinin hesabını veya başka yardımcı hesaplarını değiştiremez.",14,Ui.Muted),
            service.IsPresident?Ui.AsyncButton("+ Yetkili yardımcısı ekle",()=>Guard(AddDeputyDialog),true):Ui.Text("Yardımcı hesapları",14,Ui.Muted));
        foreach(var user in service.Data.Users.Where(u=>u.Role==Choices.DeputyRole))
        {
            body.Children.Add(Ui.Card(Ui.Stack(Ui.UserText(user.Profile.Name,20,Ui.Ink,true),Ui.UserText(user.Username+" • "+AppLanguage.Text(user.IsActive?"Aktif":"Çıkarıldı"),15),
                service.IsPresident?Ui.Row(Ui.AsyncButton("Şifre ver / Sıfırla",()=>Guard(()=>AdminPasswordDialog(user))),Ui.Button(user.IsActive?"Yetkisini kapat":"Yetkisini aç",()=>{service.AdminSetUserActive(user.Id,!user.IsActive);Refresh();})):Ui.UserText(user.Profile.Name),
                Ui.AsyncButton("Geçici şifreyi göster",()=>Guard(async()=>{var password=IssuedCredentialVault.UserPassword(service,user);await Ui.Message(this,password??"Görüntülenebilir geçici şifre yok; yeni şifre verebilirsin.");})))));
        }
        return Ui.Page(body);
    }
    private async Task AddDeputyDialog()
    {
        if(!service.IsPresident)throw new InvalidOperationException("Yardımcı hesaplarını yalnız asıl yetkili yönetebilir.");
        var name=Ui.Input();var username=Ui.Input();username.MaxLength=20;var password=Ui.Input();password.PasswordChar='●';var repeat=Ui.Input();repeat.PasswordChar='●';var error=Ui.Text("",13,Ui.Muted);
        var panel=Ui.Stack(Ui.Text("Yetkili yardımcısı oluştur",24,Ui.Ink,true),Ui.Field("Ad soyad *",name,460),Ui.Field("Kullanıcı adı * (3–20 karakter)",username,460),Ui.Field("Geçici şifre * (en az 8 karakter)",password,460),Ui.Field("Şifre tekrar *",repeat,460),
            Ui.Text("Bu kişi bütün üye, hoca ve kulüp kayıtlarına yönetim erişimi kazanır.",14,Ui.Muted),error);
        var window=Ui.Dialog("FitTeam • Yetkili yardımcısı",panel,580,690);
        panel.Children.Add(Ui.Button("Yardımcı hesabını oluştur",()=>{try{if(password.Text!=repeat.Text)throw new ArgumentException("Şifreler eşleşmiyor.");var created=service.AdminCreateDeputy(name.Text??"",username.Text??"",password.Text??"");IssuedCredentialVault.Save(service,"user:"+created.Id,password.Text??"");password.Text=repeat.Text="";window.Close();Refresh();}catch(Exception ex){error.Text=Ui.ErrorMessage(ex);}},true));
        await window.ShowDialog(this);
    }
}
