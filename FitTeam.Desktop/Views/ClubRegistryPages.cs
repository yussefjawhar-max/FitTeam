using Avalonia.Controls;
using FitTeam.Core;
namespace FitTeam.Desktop.Views;
public partial class MainWindow
{
    private ClubAccessTicket? pendingClubTicket;
    private Control ClubRegistryPage(bool chairpersonsOnly=false)
    {
        if(!service.HasAdministrationAccess)throw new InvalidOperationException("Yetkili hesabıyla giriş yap.");
        var body=Ui.Stack(Ui.Text(chairpersonsOnly?"Kulüp başkanları":"Kulüpler ve öğrenciler",26,Ui.Ink,true),Ui.Row(Ui.AsyncButton("+ Kulüp oluştur",()=>Guard(AdminAddClubDialog),true),Ui.Button(chairpersonsOnly?"Öğrenci sayılarını göster":"Kulüp başkanlarını göster",()=>ShowPage(chairpersonsOnly?"Kulüpler ve öğrenciler":"Kulüp başkanları"))));
        if(!chairpersonsOnly)body.Children.Add(StudentLookupCard());
        var clubs=service.Data.Teams.Where(t=>service.ShowClub(t)).ToList();
        if(clubs.Count==0)body.Children.Add(Ui.Card(Ui.Text("Henüz kulüp oluşturulmadı. + Kulüp oluştur düğmesinden başlayabilirsin.",16,Ui.Muted)));
        foreach(var team in clubs)
        {
            var chair=team.Chairperson;var counts=service.ClubCounts(team.Id);
            var card=Ui.Stack(Ui.Text(team.Name+" • "+team.SportBranch+" • "+(team.IsActive?"Aktif":"Çıkarıldı"),21,Ui.Ink,true),Ui.Text(string.IsNullOrEmpty(chair.Name)?"Kulüp başkanı bilgileri henüz girilmedi.":"Kulüp başkanı: "+chair.Name+"\nYaş: "+chair.Age+" • Ülke: "+chair.Country+" • Şehir: "+chair.City,15,Ui.Muted),Ui.Row(Ui.AsyncButton("Başkan bilgilerini düzenle",()=>Guard(()=>ChairpersonDialog(team))),Ui.AsyncButton("Yeni 8 haneli kulüp şifresi oluştur",()=>Guard(async()=>{string pass=IssueClubPassword(team.Id);await Ui.Message(this,team.Name+"\nYeni kulüp şifresi: "+pass+"\nBu şifreyi kulüp başkanına kendin ver. Önceki kulüp şifresi artık geçersiz. Şifre yetkili yenileyene kadar geçerlidir.");Refresh();}),true),Ui.AsyncButton("Kulübü çıkar / sıfırla",()=>Guard(async()=>{if(!await Ui.Confirm(this,team.Name+" kulübü, başkan bilgileri ve kulüp şifresi tamamen sıfırlansın mı? Öğrencilerin kişisel hesapları korunur, bu kulüple bağları kaldırılır. Aynı kulüp adını yeniden kullanabilirsin."))return;service.AdminRemoveClub(team.Id);Refresh();}))));
            card.Children.Add(Ui.Stats(Ui.Stat("Toplam öğrenci",counts.Total.ToString()),Ui.Stat("Kız",counts.Girls.ToString()),Ui.Stat("Erkek",counts.Boys.ToString()),Ui.Stat("Belirtilmedi",counts.Unspecified.ToString())));
            if(!chairpersonsOnly)
            {
                var students=service.ClubStudents(team.Id);
                foreach(var group in new[]{"Kız","Erkek","Belirtilmedi"})
                {
                    var list=students.Where(u=>group=="Kız"?u.Profile.Gender is "Kadın" or "Kız":group=="Erkek"?u.Profile.Gender=="Erkek":u.Profile.Gender is not ("Kadın" or "Kız" or "Erkek")).ToList();
                    var details=Ui.Stack();foreach(var student in list)details.Children.Add(Ui.AsyncButton(student.Profile.Name+" • "+student.Profile.Age+" yaş • "+student.Profile.Country+" / "+student.Profile.City,()=>Guard(()=>StudentDetailsDialog(student))));
                    if(list.Count==0)details.Children.Add(Ui.Text("Bu grupta öğrenci yok.",13,Ui.Muted));
                    card.Children.Add(new Expander{Header=group+" öğrenciler • "+list.Count,Content=details});
                }
            }
            body.Children.Add(Ui.Card(card));
        }
        return Ui.Page(body);
    }
    private async Task StudentDetailsDialog(User student)
    {
        if(!service.HasAdministrationAccess)throw new InvalidOperationException("Yetkili hesabıyla giriş yap.");
        var p=student.Profile;var team=service.Data.Teams.FirstOrDefault(t=>t.Id==student.TeamId);
        var panel=Ui.Stack(Ui.Text(p.Name,26,Ui.Ink,true),Ui.UserText(AppLanguage.Text("Kullanıcı adı:")+" "+student.Username,15),Ui.Text(student.Role==Choices.Roles[2]||student.Role==Choices.Roles[3]?"Antrenör kaydı":"Kişisel öğrenci kaydı",14,Ui.Green,true),Ui.Text("Yaş: "+p.Age+"\nCinsiyet: "+p.Gender+"\nÜlke: "+(p.Country.Length==0?"Belirtilmedi":CountryCatalog.ByCode(p.CountryCode)?.Name??p.Country)+"\nŞehir: "+(p.City.Length==0?"Belirtilmedi":p.City)+"\nTelefon: "+(student.Phone.Length==0?"Belirtilmedi":student.Phone)+"\nBranş: "+FitService.SportOf(student)+"\nKulüp: "+(team?.Name??"Kulübe bağlı değil")+"\nDeneyim: "+student.ExperienceYears+" yıl\nKan grubu: "+p.BloodGroup,16));
        panel.Children.Add(Ui.Text(TrainingWindow.Label(p),15,Ui.Green,true));
        panel.Children.Add(Ui.UserText(AppLanguage.Text("Ev adresi:")+" "+(p.HomeAddress.Length==0?AppLanguage.Text("Belirtilmedi"):p.HomeAddress),16));
        if(p.Address!=null)panel.Children.Add(Ui.Stack(Ui.UserText(AppLanguage.Text("İl:")+" "+p.Address.Province+" • "+AppLanguage.Text("İlçe:")+" "+p.Address.District),Ui.UserText(AppLanguage.Text("Mahalle:")+" "+p.Address.Neighborhood+" • "+AppLanguage.Text("Sokak:")+" "+p.Address.Street),Ui.UserText(AppLanguage.Text("Bina no:")+" "+p.Address.Building+" • "+AppLanguage.Text("Daire no:")+" "+p.Address.Apartment)));
        if(student.ProfileComplete)panel.Children.Add(Ui.Text("Boy: "+p.Height+" cm\nKilo: "+p.Weight+" kg\nHedef kilo: "+p.TargetWeight+" kg\nHedef: "+p.Goal+"\nSeviye: "+p.Level+"\nUygun günler: "+string.Join(", ",p.AvailableDays.Select(Choices.DayName)),16));
        else panel.Children.Add(Ui.Text("Profil bilgileri henüz tamamlanmadı.",14,Ui.Muted));
        if(student.ProfileComplete&&student.IsActive)panel.Children.Add(WeightGoalCard(student));
        panel.Children.Add(Ui.Text("Doğum tarihi: "+(p.BirthDate?.ToString("dd.MM.yyyy")??"Belirtilmedi")+"\nE-posta: "+(student.Email.Length==0?"Belirtilmedi":student.Email)+"\nKayıt durumu: "+(!student.IsActive?"Çıkarıldı":student.NeedsApproval?"Onay bekliyor":"Aktif"),15,Ui.Muted));
        if(p.HealthNotes.Length>0)panel.Children.Add(Ui.UserText(AppLanguage.Text("Sağlık notu:")+" "+p.HealthNotes,14));
        if(student.CoachNote.Length>0)panel.Children.Add(Ui.UserText(AppLanguage.Text("Hoca notu:")+" "+student.CoachNote,14));
        panel.Children.Add(PresidentPersonRecords(student));
        if(service.IsPresident&&codeAuth?.Identity?.IsOwner==true&&student.Id!=Me.Id&&!student.Email.Equals("jawharyusuf446@gmail.com",StringComparison.OrdinalIgnoreCase))panel.Children.Add(PresidentPersonCredentials(student));
        var w=Ui.Dialog("FitTeam • Kişi bilgileri",panel,850,850);panel.Children.Add(Ui.Button("Kapat",()=>w.Close(),true));await w.ShowDialog(this);
    }
    private async Task ChairpersonDialog(Team team)
    {
        var chair=team.Chairperson;var name=Ui.Input(chair.Name);var age=Ui.Input(chair.Age==0?"":chair.Age.ToString());var country=Ui.Input(chair.Country);var city=Ui.Input(chair.City);var error=Ui.Text("",13,Ui.Muted);
        var panel=Ui.Stack(Ui.Text(team.Name+" • Kulüp başkanı",24,Ui.Ink,true),Ui.Field("Ad soyad *",name,460),Ui.Field("Yaş *",age,460),Ui.Field("Ülke *",country,460),Ui.Field("Şehir *",city,460),error);var w=Ui.Dialog("FitTeam • Kulüp başkanı",panel,580,610);
        panel.Children.Add(Ui.Button("Bilgileri kaydet",()=>{try{service.AdminSetChairperson(team.Id,new ClubChairperson{Name=name.Text??"",Age=int.TryParse(age.Text,out var n)?n:0,Country=country.Text??"",City=city.Text??""});w.Close();Refresh();}catch(Exception ex){error.Text=Ui.ErrorMessage(ex);}},true));await w.ShowDialog(this);
    }
    private async Task ClubMemberEntry(ClubAccessTicket ticket)
    {
        var team=service.Data.Teams.First(t=>t.Id==ticket.TeamId);string? action=null;
        var panel=Ui.Stack(Ui.Text(team.Name,26,Ui.Ink,true),Ui.Text("Kulüp şifresi doğru. Kendi öğrenci hesabınla devam et; bilgilerin bu kulüpteki sana ait kayda bağlanır.",15,Ui.Muted));var w=Ui.Dialog("FitTeam • Kulübe katıl",panel,580,390);
        panel.Children.Add(Ui.Button("Kayıtlı öğrenci girişi",()=>{action="login";w.Close();},true));panel.Children.Add(Ui.Button("Yeni öğrenci kaydı",()=>{action="signup";w.Close();}));await w.ShowDialog(this);
        pendingClubTicket=ticket;
        try{if(action=="signup")await SignupWizardDialog(team.SportBranch);else if(action=="login")await ContactChoiceDialog(title:team.Name+" • Öğrenci girişi");}
        finally{pendingClubTicket=null;}
    }
}
