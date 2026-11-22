using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Layout;
using Avalonia.Media;
using FitTeam.Core;
namespace FitTeam.Desktop.Views;

public partial class MainWindow
{
    private AdministrationGroup presidentGroup=AdministrationGroup.Students;
    private string presidentListKind="people";
    private Control PresidentCountCards()
    {
        Button Count(string title,int count,Action open)
        {
            var button=Ui.Button(title,open);button.Content=Ui.Stack(Ui.Text(title,14,Ui.Muted),Ui.Text(count.ToString(),32,Ui.Ink,true),Ui.Text("Listeyi aç →",12,Ui.Green));
            button.Padding=new Thickness(22);button.Background=Brushes.White;button.BorderBrush=Brush.Parse("#E7EBEF");button.BorderThickness=new Thickness(1);button.HorizontalAlignment=HorizontalAlignment.Stretch;button.HorizontalContentAlignment=HorizontalAlignment.Stretch;return button;
        }
        Button People(string title,AdministrationGroup group)=>Count(title,service.AdministrationUsers(group).Count,()=>{presidentGroup=group;presidentListKind="people";ShowPage("İsim listesi");});
        var clubs=service.Data.Teams.Where(t=>t.IsActive&&service.ShowClub(t)).ToList();
        return Ui.Stack(Ui.Stats(People("Katılımcı",AdministrationGroup.People),People("Öğrenci",AdministrationGroup.Students),People("Antrenör",AdministrationGroup.Coaches),Count("Kulüp",clubs.Count,()=>{presidentListKind="clubs";ShowPage("İsim listesi");})),
            Ui.Stats(Count("Kulüp başkanı",clubs.Count(t=>!string.IsNullOrWhiteSpace(t.Chairperson.Name)),()=>{presidentListKind="chairs";ShowPage("İsim listesi");}),People("Fitness hocası",AdministrationGroup.FitnessCoaches),People("Fitness üyesi",AdministrationGroup.FitnessMembers)));
    }
    private Control PresidentNameList()
    {
        if(!service.HasAdministrationAccess)throw new InvalidOperationException("Yetkili hesabıyla giriş yap.");
        string title=presidentListKind=="clubs"?"Kulüpler":presidentListKind=="chairs"?"Kulüp başkanları":presidentGroup switch{AdministrationGroup.Students=>"Öğrenciler",AdministrationGroup.Coaches=>"Antrenörler",AdministrationGroup.FitnessCoaches=>"Fitness hocaları",AdministrationGroup.FitnessMembers=>"Fitness üyeleri",_=>"Katılımcılar"};
        var search=Ui.Input("","Ad veya soyad yaz");var count=Ui.Text("",14,Ui.Muted);
        var list=new ListBox{Height=520,Background=Brushes.Transparent,BorderThickness=new Thickness(0)};
        if(presidentListKind=="people")list.ItemTemplate=new FuncDataTemplate<User>((person,_)=>person==null?Ui.Text(""):Ui.AsyncButton(person.Profile.Name,()=>Guard(()=>StudentDetailsDialog(person))));
        else list.ItemTemplate=new FuncDataTemplate<Team>((team,_)=>team==null?Ui.Text(""):Ui.Button(presidentListKind=="chairs"?team.Chairperson.Name:team.Name,()=>OpenPresidentClub(team)));
        void Filter()
        {
            string key=AddressCatalog.Key(search.Text??"");
            if(presidentListKind=="people")
            {
                var people=service.AdministrationUsers(presidentGroup).Where(u=>key.Length==0||AddressCatalog.Key(u.Profile.Name).Contains(key)||AddressCatalog.Key(u.Username).Contains(key)).ToList();list.ItemsSource=people;count.Text=people.Count+" "+AppLanguage.Text(title);
            }
            else
            {
                var clubs=service.Data.Teams.Where(t=>t.IsActive&&service.ShowClub(t)&&(presidentListKind!="chairs"||t.Chairperson.Name.Length>0)).Where(t=>key.Length==0||AddressCatalog.Key(t.Name+" "+t.Chairperson.Name).Contains(key)).OrderBy(t=>presidentListKind=="chairs"?t.Chairperson.Name:t.Name).ToList();list.ItemsSource=clubs;count.Text=clubs.Count+" "+AppLanguage.Text(title);
            }
        }
        search.TextChanged+=(_,_)=>Filter();Filter();
        return Ui.Page(Ui.Row(Ui.Text(title,26,Ui.Ink,true),Ui.Button("Başkan paneline dön",()=>ShowPage("Başkan paneli"))),Ui.Card(Ui.Stack(search,count,list)));
    }
    private void OpenPresidentClub(Team club)
    {
        if(!service.HasAdministrationAccess)throw new InvalidOperationException("Yetkili hesabıyla giriş yap.");
        var chair=club.Chairperson;var students=service.Data.Users.Where(u=>u.TeamId==club.Id&&u.IsActive&&!u.NeedsApproval&&service.ShowRecord(u)&&u.Role==Choices.Roles[1]).OrderBy(u=>u.Profile.Name).ToList();
        var coach=service.Data.Users.FirstOrDefault(u=>u.Id==club.CoachId&&service.ShowRecord(u));
        var body=Ui.Stack(Ui.Row(Ui.Text(club.Name,26,Ui.Ink,true),Ui.Button("İsim listesine dön",()=>ShowPage("İsim listesi"))),Ui.Card(Ui.Stack(Ui.Text(club.SportBranch+" • "+club.Gender,18,Ui.Green,true),Ui.Text("Kulüp başkanı",20,Ui.Ink,true),Ui.UserText(chair.Name,20,Ui.Ink,true),Ui.Text("Yaş: "+chair.Age+" • Ülke: "+chair.Country+" • Şehir: "+chair.City,15,Ui.Muted))),Ui.Stats(Ui.Stat("Toplam öğrenci",students.Count.ToString()),Ui.Stat("Kız",students.Count(u=>u.Profile.Gender is "Kadın" or "Kız").ToString()),Ui.Stat("Erkek",students.Count(u=>u.Profile.Gender=="Erkek").ToString())));
        if(coach!=null)body.Children.Add(Ui.Card(Ui.Stack(Ui.Text("Kulüp antrenörü",20,Ui.Ink,true),Ui.AsyncButton(coach.Profile.Name,()=>Guard(()=>StudentDetailsDialog(coach))),Ui.Text(HoursLabel(coach),14,Ui.Muted))));
        if(service.IsPresident&&codeAuth?.Identity?.IsOwner==true)
        {
            var secret=Ui.UserText("••••••••",16,Ui.Green);bool visible=false;
            body.Children.Add(Ui.Card(Ui.Stack(Ui.Text("Kulüp şifresi",19,Ui.Ink,true),secret,Ui.Button("Kulüp şifresini göster / gizle",()=>{visible=!visible;secret.Text=visible?IssuedCredentialVault.ClubPassword(service,club)??AppLanguage.Text("Görüntülenebilir kulüp şifresi yok; yeni şifre oluşturabilirsin."):"••••••••";}))));
        }
        var names=new ListBox{Height=440,ItemsSource=students,ItemTemplate=new FuncDataTemplate<User>((u,_)=>u==null?Ui.Text(""):Ui.AsyncButton(u.Profile.Name,()=>Guard(()=>StudentDetailsDialog(u))))};
        body.Children.Add(Ui.Card(Ui.Stack(Ui.Text("Öğrenciler",22,Ui.Ink,true),names)));PageTitle.Text=club.Name;PageContent.Content=Ui.Page(body);LanguageView.Apply(PageContent);
    }
}
