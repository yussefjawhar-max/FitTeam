using Avalonia;
using Avalonia.Controls;
using FitTeam.Core;
namespace FitTeam.Desktop.Views;
public partial class MainWindow
{
    private async Task<bool> NewMemberProfileDialog()
    {
        bool saved=false;var parts=Me.Profile.Name.Split(' ',2,StringSplitOptions.RemoveEmptyEntries);
        var first=Ui.Input(parts.FirstOrDefault()??"");var last=Ui.Input(parts.Length>1?parts[1]:"");
        var age=Ui.Number(Me.Profile.Age,13,90);var height=Ui.Number(175,100,230,1);var weight=Ui.Number(75,30,300,1);var target=Ui.Number(70,30,300,1);var goal=Ui.Combo(Choices.Goals);var level=Ui.Combo(Choices.Levels);var role=Ui.Combo([Choices.Roles[0],Choices.Roles[1]],Me.Role);
        var sport=Ui.Combo(FitService.Branches,FitService.SportOf(Me));
        var gender=Ui.Combo(["Kadın","Erkek","Belirtmek istemiyorum"],Me.Profile.Gender);var country=CountryPicker.Create(Me.Profile.Country,Me.Profile.CountryCode);var city=Ui.Input(Me.Profile.City);var address=Ui.Input(Me.Profile.HomeAddress);address.AcceptsReturn=true;
        var coaches=CoachChecks(FitService.CoachLinks(Me));var coachPanel=Ui.Stack(Ui.Text("Hangi hocalara katılmak istiyorsun?",18,Ui.Ink,true),Ui.Text("Birden fazla hoca seçebilirsin. Seçtiğin hocalar antrenman ve gelişim kayıtlarını görebilir.",13,Ui.Muted),Ui.Stack(coaches.ToArray()));
        sport.SelectionChanged+=(_,_)=>{role.SelectedItem=Ui.Selected(sport)=="Fitness"?Choices.Roles[0]:Choices.Roles[1];coachPanel.IsVisible=Ui.Selected(sport)=="Fitness";};coachPanel.IsVisible=Ui.Selected(sport)=="Fitness";
        var clubChoice=new ComboBox();
        void FilterClubs(){clubChoice.ItemsSource=service.Data.Teams.Where(t=>t.IsActive&&t.SportBranch==Ui.Selected(sport)).Select(t=>new TeamChoice(t)).ToList();clubChoice.SelectedIndex=-1;}
        sport.SelectionChanged+=(_,_)=>FilterClubs();FilterClubs();
        var clubPanel=Ui.Field("Katılmak istediğin kulüp (isteğe bağlı)",clubChoice,450);clubPanel.IsVisible=Ui.Selected(sport)!="Fitness";
        sport.SelectionChanged+=(_,_)=>clubPanel.IsVisible=Ui.Selected(sport)!="Fitness";
        var days=Choices.Days.Select(d=>new CheckBox {Content=Choices.DayName(d),Tag=d,IsChecked=Me.Profile.AvailableDays.Contains(d)}).ToList();var error=Ui.Text("",13,Ui.Muted);
        var fields=new WrapPanel();foreach(var f in new[] {Ui.Field("Adın",first),Ui.Field("Soyadın",last),Ui.Field("Yaşın",age),Ui.Field("Cinsiyet",gender),Ui.Field("Ülke",country),Ui.Field("Şehir",city),Ui.Field("Ev adresin *",address,440),Ui.Field("Boyun (cm)",height),Ui.Field("Kilon (kg)",weight),Ui.Field("Hedef kilon (kg)",target),Ui.Field("Ne yapmak istiyorsun?",goal),Ui.Field("Spor seviyen",level),Ui.Field("Spor branşı",sport)})fields.Children.Add(f);
        var panel=Ui.Stack(Ui.Text("Seni tanıyalım",28,Ui.Ink,true),Ui.Text("İlk girişinde bu bilgileri tamamla; sana uygun programı hazırlayalım.",14,Ui.Muted),fields,coachPanel,Ui.Text("Hangi günler gelebilirsin?",18,Ui.Ink,true),Ui.Row(days.ToArray()),error);
        var w=Ui.Dialog("FitTeam • İlk giriş profili",panel,860,800);
        panel.Children.Add(Ui.Button("Profilimi tamamla • Uygulamaya gir",()=>{
            try {string fullName=(first.Text??"").Trim()+" "+(last.Text??"").Trim();if((first.Text??"").Trim().Length<2||(last.Text??"").Trim().Length<2)throw new ArgumentException("Adını ve soyadını gir.");
                if(CountryCatalog.Resolve(country.Text)==null||(city.Text??"").Trim().Length<2||(address.Text??"").Trim().Length<8)throw new ArgumentException("Ülke ve şehir bilgilerini doldur.");
                var ids=coaches.Where(c=>c.IsChecked==true).Select(c=>(Guid)c.Tag!).ToList();var selectedRole=Ui.Selected(sport)=="Fitness"?Choices.Roles[0]:Choices.Roles[1];
                if(selectedRole==Choices.Roles[0]&&ids.Count==0)throw new ArgumentException("En az bir hoca seç; birden fazla seçebilirsin.");
                // Bütün alanlar doğrulanmadan rol veya hoca bağlantıları değiştirilmez.
                var profile=new Profile {Gender=Ui.Selected(gender),Country=CountryPicker.Selected(country).Turkish,CountryCode=CountryPicker.Selected(country).Code,HomeAddress=address.Text??"",City=city.Text??"",Name=fullName,Age=Ui.Int(age),Height=Ui.Value(height),Weight=Ui.Value(weight),TargetWeight=Ui.Value(target),Goal=Ui.Selected(goal),Level=Ui.Selected(level),BloodGroup=Me.Profile.BloodGroup,AvailableDays=days.Where(d=>d.IsChecked==true).Select(d=>(DayOfWeek)d.Tag!).ToList()};
                service.UpdateProfile(profile);Me.SportBranch=Ui.Selected(sport);Me.Role=selectedRole;Me.TeamId=selectedRole==Choices.Roles[1]?pendingClubTicket?.TeamId??Me.TeamId:null;if(selectedRole==Choices.Roles[0])service.SelectCoaches(ids);else {Me.CoachIds=[];Me.CoachId=null;service.Save();}
                saved=true;w.Close();
            }catch(Exception ex){error.Text=Ui.ErrorMessage(ex);}
        },true));await w.ShowDialog(this);return saved;
    }
    private Task AddCoachDialog() => AdminAddUserDialog(Choices.Roles[2]);
    private Control PresidentCoachesPage()
    {
        if(!service.HasAdministrationAccess)throw new InvalidOperationException("Başkan hesabıyla giriş yapın.");
        var coaches=service.Data.Users.Where(u=>u.IsActive&&!u.NeedsApproval&&service.ShowRecord(u)&&(u.Role==Choices.Roles[2]||u.Role==Choices.Roles[3])).ToList();
        var body=Ui.Stack(Ui.Row(Ui.Text("Hocalar • "+coaches.Count,24,Ui.Ink,true),Ui.AsyncButton("+ Hoca ekle",()=>Guard(()=>AdminAddUserDialog(Choices.Roles[2])),true)));
        foreach(var kind in new[]{"Kadın","Erkek","Diğer"})
        {
            var list=coaches.Where(u=>kind=="Kadın"?u.Profile.Gender is "Kadın" or "Kız":kind=="Erkek"?u.Profile.Gender=="Erkek":u.Profile.Gender is not ("Kadın" or "Kız" or "Erkek")).ToList();
            var panel=Ui.Stack(Ui.Text((kind=="Kadın"?"Kadın antrenörler":kind=="Erkek"?"Erkek antrenörler":"Diğer antrenörler")+" • "+list.Count,22,Ui.Ink,true));
            foreach(var coach in list)
            {
                var card=Ui.Stack(Ui.AsyncButton(coach.Profile.Name+" • "+FitService.SportOf(coach),()=>Guard(()=>StudentDetailsDialog(coach)),true),Ui.Text("Yaş: "+coach.Profile.Age+" • Deneyim: "+coach.ExperienceYears+" yıl • "+HoursLabel(coach),14,Ui.Muted));
                foreach(var team in service.Data.Teams.Where(t=>t.IsActive&&service.ShowClub(t)&&t.CoachId==coach.Id))
                {
                    var chair=string.IsNullOrWhiteSpace(team.Chairperson.Name)?AppLanguage.Text("Belirtilmedi"):team.Chairperson.Name;
                    card.Children.Add(Ui.UserText(AppLanguage.Text("Kulüp:")+" "+team.Name+" • "+AppLanguage.Text("Başkan:")+" "+chair,15));
                    card.Children.Add(Ui.Text("Kulüp öğrencisi: "+service.Data.Users.Count(u=>u.IsActive&&!u.NeedsApproval&&u.TeamId==team.Id&&u.Role==Choices.Roles[1]),13,Ui.Muted));
                }
                panel.Children.Add(Ui.Card(card));
            }
            body.Children.Add(Ui.Card(panel));
        }
        return Ui.Page(body);
    }
}
