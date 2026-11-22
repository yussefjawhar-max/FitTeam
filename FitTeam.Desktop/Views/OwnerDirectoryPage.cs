using Avalonia.Controls;
using FitTeam.Core;
namespace FitTeam.Desktop.Views;
public partial class MainWindow
{
    private string ownerLoginName="";
    private string IssueClubPassword(Guid id){var password=service.AdminRotateClubPassword(id);IssuedCredentialVault.Save(service,"club:"+id,password);return password;}
    private Control OwnerDirectoryPage()
    {
        if(!service.HasFullAdministrationView||service.IsPresident&&!service.IsPreview&&codeAuth?.Identity?.IsOwner!=true)throw new InvalidOperationException("Yetkili hesabıyla giriş yap.");
        var ownSecret=Ui.UserText("••••••••",16,Ui.Green,true);bool ownVisible=false;
        var body=Ui.Stack(Ui.Text("Hesaplar ve şifreler",26,Ui.Ink,true),Ui.Text("Kullanıcı adına ve hesaba ait geçerli şifreyi açabilirsin. Eski bir şifrenin korumalı kopyası yoksa kişinin bir sonraki başarılı girişinde veya şifre değişikliğinde görünür.",14,Ui.Muted),StudentLookupCard(),
            Ui.Card(Ui.Stack(Ui.Text("Yetkili • "+Me.Profile.Name,21,Ui.Ink,true),Ui.UserText(ownerLoginName.Length==0?Me.Username:ownerLoginName,16),ownSecret,Ui.Button("Şifreyi göster / gizle",()=>{ownVisible=!ownVisible;ownSecret.Text=ownVisible?IssuedCredentialVault.OwnerPassword(service)??AppLanguage.Text("Şifre kopyası sonraki başarılı girişte kaydedilir."):"••••••••";}),service.IsPresident?Ui.AsyncButton("Kendi yetkili şifremi değiştir",()=>Guard(SetPresidentPasswordDialog)):Ui.Button("Kendi şifremi değiştir",()=>ShowPage("Şifrem")))));
        body.Children.Add(Ui.AsyncButton("Kullanıcı adı ve şifre listesini kaydet",()=>Guard(ExportOwnerPasswords),true));
        string Role(User user)=>user.NeedsApproval&&user.RequestedRole.Length>0?user.RequestedRole:user.Role;
        var users=service.Data.Users.Where(u=>u.Id!=Me.Id&&!u.Email.Equals("jawharyusuf446@gmail.com",StringComparison.OrdinalIgnoreCase)&&service.ShowRecord(u)).ToList();
        foreach(var group in new[]{"Yetkili yardımcıları","Kadın antrenörler","Erkek antrenörler","Diğer antrenörler","Fitness öğrencileri","Kulüp öğrencileri"})
        {
            var list=users.Where(u=>group=="Yetkili yardımcıları"?Role(u)==Choices.DeputyRole:group=="Fitness öğrencileri"?Role(u)==Choices.Roles[0]:group=="Kulüp öğrencileri"?Role(u)==Choices.Roles[1]:
                (Role(u)==Choices.Roles[2]||Role(u)==Choices.Roles[3])&&(group=="Kadın antrenörler"?u.Profile.Gender is "Kadın" or "Kız":group=="Erkek antrenörler"?u.Profile.Gender=="Erkek":u.Profile.Gender is not ("Kadın" or "Kız" or "Erkek"))).OrderBy(u=>u.Profile.Name).ToList();
            var panel=Ui.Stack();foreach(var user in list)
            {
                var secret=Ui.UserText("••••••••",16,Ui.Green,true);bool visible=false;
                var row=Ui.Stack(Ui.UserText(user.Profile.Name,18,Ui.Ink,true),Ui.UserText(AppLanguage.Text("Kullanıcı adı:")+" "+user.Username+" • "+AppLanguage.Text(FitService.RoleLabel(user)),14),secret,
                    Ui.Row(Ui.Button("Şifreyi göster / gizle",()=>{visible=!visible;secret.Text=!visible?"••••••••":IssuedCredentialVault.UserPassword(service,user)??AppLanguage.Text("Şifre kopyası sonraki başarılı girişte kaydedilir.");}),
                        Ui.AsyncButton("Bilgilerini aç",()=>Guard(()=>StudentDetailsDialog(user))),Ui.AsyncButton("Şifre ver / Sıfırla",()=>Guard(()=>AdminPasswordDialog(user)))));
                panel.Children.Add(Ui.Card(row));
            }
            body.Children.Add(new Expander{Header=group+" • "+list.Count,Content=panel});
        }
        var clubs=Ui.Stack();foreach(var club in service.Data.Teams.Where(service.ShowClub))
        {
            var secret=Ui.UserText("••••••••",16,Ui.Green,true);bool visible=false;
            var account=service.Data.Users.FirstOrDefault(u=>u.Id==club.CoachId);var accountSecret=Ui.UserText("••••••••",16,Ui.Green);bool accountVisible=false;
            var accountPanel=Ui.Stack();if(account!=null)accountPanel.Children.Add(Ui.Stack(Ui.UserText(AppLanguage.Text("Kulüp hesabı:")+" "+account.Username,15),accountSecret,Ui.Button("Hesap şifresini göster / gizle",()=>{accountVisible=!accountVisible;accountSecret.Text=accountVisible?IssuedCredentialVault.UserPassword(service,account)??AppLanguage.Text("Şifre kopyası sonraki başarılı girişte kaydedilir."):"••••••••";})));
            clubs.Children.Add(Ui.Card(Ui.Stack(Ui.UserText(club.Name,18,Ui.Ink,true),Ui.UserText(AppLanguage.Text("Kulüp başkanı:")+" "+club.Chairperson.Name,15),accountPanel,secret,Ui.Row(Ui.Button("Kulüp şifresini göster / gizle",()=>{visible=!visible;secret.Text=!visible?"••••••••":IssuedCredentialVault.ClubPassword(service,club)??AppLanguage.Text("Görüntülenebilir kulüp şifresi yok; yeni şifre oluşturabilirsin.");}),
                Ui.AsyncButton("Yeni 8 haneli kulüp şifresi oluştur",()=>Guard(async()=>{var pass=IssueClubPassword(club.Id);await Ui.Message(this,club.Name+"\nYeni kulüp şifresi: "+pass);}))))));
        }
        body.Children.Add(new Expander{Header="Kulüpler • "+service.Data.Teams.Count(service.ShowClub),Content=clubs});return Ui.Page(body);
    }
    private async Task ExportOwnerPasswords()
    {
        if(!service.HasFullAdministrationView||service.IsPresident&&!service.IsPreview&&codeAuth?.Identity?.IsOwner!=true)throw new InvalidOperationException("Yetkili hesabıyla giriş yap.");
        var file=await StorageProvider.SaveFilePickerAsync(new(){Title=AppLanguage.Text("Hesap ve şifre listesini kaydet"),SuggestedFileName="FitTeam_Hesap_Sifreleri.txt",DefaultExtension="txt"});if(file==null)return;
        var users=service.Data.Users.Where(u=>u.Id!=Me.Id&&!u.Email.Equals("jawharyusuf446@gmail.com",StringComparison.OrdinalIgnoreCase)&&service.ShowRecord(u)).OrderBy(u=>u.Profile.Name).ToList();
        var clubs=service.Data.Teams.Where(service.ShowClub).OrderBy(t=>t.Name).ToList();
        string Missing()=>AppLanguage.Text("Şifre kopyası sonraki başarılı girişte kaydedilir.");
        var text=await Task.Run(()=>
        {
            var lines=new List<string>{"FitTeam • "+AppLanguage.Text("Hesaplar ve şifreler"),DateTime.Now.ToString("dd.MM.yyyy HH:mm"),"",Me.Profile.Name,"Kullanıcı adı: "+service.Data.OwnerLoginUsername,"Şifre: "+(IssuedCredentialVault.OwnerPassword(service)??Missing()),""};
            foreach(var user in users){lines.Add(user.Profile.Name+" • "+AppLanguage.Text(FitService.RoleLabel(user)));lines.Add("Kullanıcı adı: "+user.Username);lines.Add("Şifre: "+(IssuedCredentialVault.UserPassword(service,user)??Missing()));lines.Add("");}
            foreach(var club in clubs){lines.Add(club.Name+" • "+AppLanguage.Text("Kulüp başkanı:")+" "+club.Chairperson.Name);lines.Add("Kulüp şifresi: "+(IssuedCredentialVault.ClubPassword(service,club)??Missing()));lines.Add("");}
            return string.Join(Environment.NewLine,lines);
        });
        if(!service.HasFullAdministrationView||service.IsPresident&&!service.IsPreview&&codeAuth?.Identity?.IsOwner!=true)throw new InvalidOperationException("Yetkili hesabıyla giriş yap.");
        await using(var stream=await file.OpenWriteAsync()){stream.SetLength(0);await using var writer=new System.IO.StreamWriter(stream);await writer.WriteAsync(text);}
        var local=Avalonia.Platform.Storage.StorageProviderExtensions.TryGetLocalPath(file);if(local!=null&&!OperatingSystem.IsWindows())System.IO.File.SetUnixFileMode(local,System.IO.UnixFileMode.UserRead|System.IO.UnixFileMode.UserWrite);
        await Ui.Message(this,"Hesap ve şifre listesi kaydedildi.");
    }
    private Control NewMembersPage()
    {
        if(!service.HasAdministrationAccess)throw new InvalidOperationException("Yetkili hesabıyla giriş yap.");
        var body=Ui.Stack(Ui.Text("Yeni üyeler",26,Ui.Ink,true),StudentLookupCard());
        var users=service.Data.Users.Where(u=>u.Id!=Me.Id&&u.Role!=Choices.DeputyRole&&!u.Email.Equals("jawharyusuf446@gmail.com",StringComparison.OrdinalIgnoreCase)&&!u.IsClubAccount&&!FitService.IsExample(u)&&service.ShowRecord(u)).OrderByDescending(u=>u.RegisteredAt).ToList();
        foreach(var user in users)
        {
            var requested=service.Data.Teams.FirstOrDefault(t=>t.Id==user.RequestedClubId);
            var row=Ui.Stack(Ui.UserText(user.Profile.Name,20,Ui.Ink,true),Ui.UserText(user.Username+" • "+AppLanguage.Text(FitService.RoleLabel(user)),14),
                Ui.Text(user.RegisteredAt==null?"Kayıt tarihi önceki sürümde saklanmamış.":"Kayıt tarihi: "+user.RegisteredAt.Value.ToLocalTime().ToString("dd.MM.yyyy HH:mm"),13,Ui.Muted),
                Ui.AsyncButton("Bilgilerini aç",()=>Guard(()=>StudentDetailsDialog(user))));
            if(user.NeedsApproval)row.Children.Add(Ui.Button("Üyeliği onayla",()=>{service.AdminApproveUser(user.Id);Refresh();},true));
            if(requested!=null)row.Children.Add(Ui.Stack(Ui.UserText(AppLanguage.Text("Katılmak istediği kulüp:")+" "+requested.Name,15),Ui.Row(Ui.Button("Kulüp isteğini onayla",()=>{service.AdminApproveClubRequest(user.Id);Refresh();},true),Ui.Button("Kulüp isteğini reddet",()=>{service.AdminRejectClubRequest(user.Id);Refresh();}))));
            body.Children.Add(Ui.Card(row));
        }
        if(users.Count==0)body.Children.Add(Ui.Text("Henüz yeni üye kaydı yok.",15,Ui.Muted));return Ui.Page(body);
    }
}
