using Avalonia.Controls;
using FitTeam.Core;
namespace FitTeam.Desktop.Views;
public partial class MainWindow
{
    private Control AdministrationPage()
    {
        if(!service.HasAdministrationAccess)throw new InvalidOperationException("Yetkili hesabıyla giriş yap.");
        var body=Ui.Stack(Ui.Text("Kişiler ve kulüpler",26,Ui.Ink,true),Ui.Text("Çıkarılan hesaplar giriş yapamaz. Kayıtları korunur; istersen yeniden etkinleştirebilirsin.",13,Ui.Muted),Ui.Row(Ui.AsyncButton("+ Kişi / Antrenör ekle",()=>Guard(()=>AdminAddUserDialog()),true),Ui.AsyncButton("+ Kulüp ekle",()=>Guard(AdminAddClubDialog),true)));
        var people=service.Data.Users.Where(u=>service.ShowRecord(u)&&!u.IsClubAccount).ToList();
        body.Children.Add(Ui.Stats(Ui.Stat("Aktif kişi",people.Count(u=>u.IsActive&&!u.NeedsApproval).ToString()),Ui.Stat("Onay bekleyen",people.Count(u=>u.NeedsApproval&&u.IsActive).ToString()),Ui.Stat("Antrenör",people.Count(u=>u.IsActive&&(u.Role==Choices.Roles[2]||u.Role==Choices.Roles[3])).ToString()),Ui.Stat("Kulüp",service.Data.Teams.Count(t=>t.IsActive&&service.ShowClub(t)).ToString())));
        body.Children.Add(StudentLookupCard());
        body.Children.Add(Ui.Text("Kişiler",22,Ui.Ink,true));
        foreach(var user in people)
        {
            var row=Ui.Stack(Ui.Text(user.Profile.Name+" • "+(user.Id==Me.Id?"Yetkili":user.NeedsApproval&&!string.IsNullOrEmpty(user.RequestedRole)?FitService.SportOf(user)+" antrenörü başvurusu":FitService.RoleLabel(user)),17,Ui.Ink,true),Ui.Text(user.Username+" • "+(user.IsActive?(user.NeedsApproval?"Onay bekliyor":"Aktif"):"Çıkarıldı")+" • "+user.Profile.Country+" / "+user.Profile.City,13,Ui.Muted));
            if(user.Id!=Me.Id&&(user.Role==Choices.Roles[0]||user.Role==Choices.Roles[1]))row.Children.Add(Ui.AsyncButton("Öğrenci bilgilerini aç",()=>Guard(()=>StudentDetailsDialog(user))));
            if((service.IsPresident||user.Role!=Choices.DeputyRole)&&user.Id!=Me.Id&&!user.Email.Equals("jawharyusuf446@gmail.com",StringComparison.OrdinalIgnoreCase))
                row.Children.Add(Ui.Row(Ui.AsyncButton("Şifre ver / Sıfırla",()=>Guard(()=>AdminPasswordDialog(user))),Ui.AsyncButton(user.IsActive?"Listeden çıkar":"Tekrar ekle",()=>Guard(async()=>{if(user.IsActive&&!await Ui.Confirm(this,user.Profile.Name+" girişe kapatılsın mı? Kayıtları korunur."))return;service.AdminSetUserActive(user.Id,!user.IsActive);Refresh();}))));
            else if(user.Id==Me.Id)row.Children.Add(service.IsPresident?Ui.AsyncButton("Kendi yetkili şifremi değiştir",()=>Guard(SetPresidentPasswordDialog)):Ui.Button("Kendi şifremi değiştir",()=>ShowPage("Şifrem")));
            if(!string.IsNullOrEmpty(user.RequestedRole))row.Children.Add(Ui.Text("Yaş: "+user.Profile.Age+" • Deneyim: "+user.ExperienceYears+" yıl • Kan grubu: "+user.Profile.BloodGroup,13,Ui.Muted));
            if(user.NeedsApproval)row.Children.Add(Ui.Button("Üyeliği onayla",()=>{service.AdminApproveUser(user.Id);Refresh();},true));
            if(user.Role==Choices.Roles[2]||user.Role==Choices.Roles[3])row.Children.Add(Ui.Stack(Ui.Text(HoursLabel(user),13,Ui.Muted),Ui.AsyncButton("Çalışma saatlerini düzenle",()=>Guard(()=>CoachHoursDialog(user)))));
            body.Children.Add(Ui.Card(row));
        }
        body.Children.Add(Ui.Card(Ui.Stack(Ui.Text("Kulüpler ve başkanları",22,Ui.Ink,true),Ui.Row(Ui.Button("Kulüp başkanları",()=>ShowPage("Kulüp başkanları"),true),Ui.Button("Kulüpler ve öğrenciler",()=>ShowPage("Kulüpler ve öğrenciler"))))));
        return Ui.Page(body);
    }
    private async Task AdminAddUserDialog(string? chosenRole=null)
    {
        var username=Ui.Input();var name=Ui.Input();var gender=Ui.Combo(["Kadın","Erkek","Belirtmek istemiyorum"]);var age=Ui.Number(25,13,90);var experience=Ui.Number(0,0,60);var email=Ui.Input("","isteğe bağlı e-posta adresi");var phone=Ui.Input("","isteğe bağlı +905…");var role=Ui.Combo(Choices.Roles,chosenRole);var sport=Ui.Combo(FitService.Branches,chosenRole==Choices.Roles[3]?"Futbol":"Fitness");role.SelectionChanged+=(_,_)=>sport.SelectedItem=Ui.Selected(role)==Choices.Roles[0]||Ui.Selected(role)==Choices.Roles[2]?"Fitness":"Futbol";var pass=Ui.Input();pass.PasswordChar='●';var error=Ui.Text("",13,Ui.Muted);
        var panel=Ui.Stack(Ui.Text("Kişi veya antrenör ekle",24,Ui.Ink,true),Ui.Text("FitTeam şifresiyle hemen girebilir. İsterse giriş yaptıktan sonra Şifrem sayfasından değiştirir. Kişi kendi kullanıcı adıyla girer.",13,Ui.Muted),Ui.Field("Ad soyad",name,450),Ui.Field("Kullanıcı adı",username,450),Ui.Field("Kullanım türü",role,450),Ui.Field("Spor branşı",sport,450),Ui.Field("Cinsiyet",gender,450),Ui.Field("Yaş",age,450),Ui.Field("Deneyim yılı",experience,450),Ui.Field("Geçici FitTeam şifresi (en az 8 karakter)",pass,450),error);
        var w=Ui.Dialog("FitTeam • Kişi ekle",panel,570,760);
        panel.Children.Add(Ui.Button("Kişiyi ekle",()=>{try{var created=service.AdminAddUser(username.Text??"",name.Text??"",Ui.Selected(role),email.Text??"",phone.Text??"",pass.Text??"",Ui.Selected(sport));created.Profile.Gender=Ui.Selected(gender);created.Profile.Age=Ui.Int(age);created.ExperienceYears=Ui.Int(experience);created.RegisteredAt=DateTimeOffset.UtcNow;service.Save();IssuedCredentialVault.Save(service,"user:"+created.Id,pass.Text??"");pass.Text="";w.Close();Refresh();}catch(Exception ex){error.Text=Ui.ErrorMessage(ex);}},true));await w.ShowDialog(this);
    }
    private async Task AdminAddClubDialog()
    {
        var name=Ui.Input();var chair=Ui.Input();var age=Ui.Input();var country=Ui.Input();var city=Ui.Input();var gender=Ui.Combo(["Kadın","Erkek","Karma"]);var sport=Ui.Combo(["Futbol","Basketbol","Voleybol"]);var error=Ui.Text("",13,Ui.Muted);
        var panel=Ui.Stack(Ui.Text("Kulüp oluştur",24,Ui.Ink,true),Ui.Text("Kulüp oluşturulunca 8 haneli kulüp şifresi üretilecek. Yalnız yetkili yenileyebilir.",13,Ui.Muted),Ui.Field("Kulüp adı *",name,450),Ui.Field("Branş *",sport,450),Ui.Field("Takım türü",gender,450),Ui.Field("Kulüp başkanının adı soyadı *",chair,450),Ui.Field("Başkanın yaşı *",age,450),Ui.Field("Ülke *",country,450),Ui.Field("Şehir *",city,450),error);
        var w=Ui.Dialog("FitTeam • Kulüp oluştur",panel,600,780);
        panel.Children.Add(Ui.AsyncButton("Kulübü oluştur",async()=>{try{var team=service.AdminCreateClub(name.Text??"",Ui.Selected(sport),Ui.Selected(gender),new ClubChairperson{Name=chair.Text??"",Age=int.TryParse(age.Text,out var n)?n:0,Country=country.Text??"",City=city.Text??""});string password=IssueClubPassword(team.Id);w.Close();Refresh();await Ui.Message(this,team.Name+" oluşturuldu.\n8 haneli kulüp şifresi: "+password+"\nBu şifreyi kulüp başkanına kendin ver. Öğrenciler kendi hesaplarıyla katılır.");}catch(Exception ex){error.Text=Ui.ErrorMessage(ex);}},true));await w.ShowDialog(this);
    }
    private async Task AdminPasswordDialog(User target)
    {
        var pass=Ui.Input();pass.PasswordChar='●';var repeat=Ui.Input();repeat.PasswordChar='●';var error=Ui.Text("",13,Ui.Muted);
        var panel=Ui.Stack(Ui.Text(target.Profile.Name+" • FitTeam şifresi",23,Ui.Ink,true),Ui.Text("Kullanıcı adı: "+target.Username+"\nKişi bu şifreyle giriş yapabilir; isterse sonradan kendi şifresini belirler.",13,Ui.Muted),Ui.Field("Yeni geçici şifre",pass,440),Ui.Field("Tekrar",repeat,440),error);var w=Ui.Dialog("FitTeam • Şifre sıfırla",panel,560,480);
        panel.Children.Add(Ui.Button("Şifreyi kaydet",()=>{try{if(pass.Text!=repeat.Text)throw new ArgumentException("Şifreler eşleşmiyor.");service.AdminSetPassword(target.Id,pass.Text??"");IssuedCredentialVault.Save(service,"user:"+target.Id,pass.Text??"");pass.Text=repeat.Text="";w.Close();}catch(Exception ex){error.Text=Ui.ErrorMessage(ex);}},true));await w.ShowDialog(this);
    }
    private Control OwnPasswordPage()
    {
        var old=Ui.Input();old.PasswordChar='●';var pass=Ui.Input();pass.PasswordChar='●';var again=Ui.Input();again.PasswordChar='●';var status=Ui.Text("",13,Ui.Muted);
        var oldField=Ui.Field("Mevcut şifre",old,430);oldField.IsVisible=!string.IsNullOrEmpty(Me.PasswordHash);
        return Ui.Page(Ui.Card(Ui.Stack(Ui.Text("FitTeam şifrem",24,Ui.Ink,true),Ui.Text("FitTeam şifren uygulamanın başkanı tarafından görülebilir. Başka hesaplarında kullandığın şifreyi burada kullanma.",13,Ui.Muted),oldField,Ui.Field("Yeni şifre (en az 8 karakter)",pass,430),Ui.Field("Yeni şifre tekrar",again,430),status,Ui.Button("Şifremi değiştir",()=>{
            try{if(pass.Text!=again.Text)throw new ArgumentException("Şifreler eşleşmiyor.");if(!string.IsNullOrEmpty(Me.PasswordHash))service.ChangeOwnPassword(old.Text??"",pass.Text??"");else service.CreateOwnPassword(pass.Text??"");old.Text=pass.Text=again.Text="";oldField.IsVisible=true;status.Text="Şifren değiştirildi. Bir sonraki girişinde yeni şifreni kullan.";}
            catch(Exception ex){status.Text=Ui.ErrorMessage(ex);}
        },true),Ui.AsyncButton("Şifremi unuttum",()=>Guard(()=>PasswordRecoveryDialog(Me.Username))))));
    }
    private async Task<bool> CompleteLocalLogin()
    {
        if(Me.Role==Choices.DeputyRole&&!Me.ProfileComplete&&!await DeputyProfileSetupDialog()){service.Logout();return false;}
        if(!Me.ProfileComplete&&!IsCoach&&!service.HasAdministrationAccess&&!await NewMemberProfileDialog()){service.Logout();ShowLogin();return false;}
        return true;
    }
    private async Task ClubLoginDialog()
    {
        var name=Ui.Input("","Kulübünün adı");var password=Ui.Input("","8 haneli kulüp şifresi");password.MaxLength=8;password.PasswordChar='●';var error=Ui.Text("",13,Ui.Muted);
        var panel=Ui.Stack(Ui.Text("Kulüp girişi",24,Ui.Ink,true),Ui.Text("Kulüp adını ve kulüp başkanının sana verdiği 8 haneli şifreyi yaz.",14,Ui.Muted),Ui.Field("Kulüp adı",name,440),Ui.Field("8 haneli kulüp şifresi",password,440),error);var w=Ui.Dialog("FitTeam • Kulüp girişi",panel,560,460);
        panel.Children.Add(Ui.AsyncButton("Kulübe devam et",async()=>{try{var ticket=service.VerifyClubAccess(name.Text??"",password.Text??"");password.Text="";w.Close();await ClubMemberEntry(ticket);}catch(Exception ex){error.Text=Ui.ErrorMessage(ex);}},true));await w.ShowDialog(this);
    }
}
