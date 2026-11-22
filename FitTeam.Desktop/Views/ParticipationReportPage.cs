using Avalonia.Controls;
using FitTeam.Core;
namespace FitTeam.Desktop.Views;
public partial class MainWindow
{
    private Control ParticipationReportPage()
    {
        if(!service.HasAdministrationAccess)throw new InvalidOperationException("Yetkili hesabıyla giriş yap.");
        var scope=Ui.Combo(["Tüm üyeler","Fitness üyeleri"],"Fitness üyeleri");
        var club=new ComboBox{ItemsSource=new[]{new ReportClub(null)}.Concat(service.Data.Teams.Where(service.ShowClub).Select(t=>new ReportClub(t))).ToList(),SelectedIndex=0};
        var body=new StackPanel{Spacing=18};var panel=Ui.Stack(Ui.Text("Ülke ve takvim raporu",26,Ui.Ink,true),Ui.Text("Ülke yüzdeleri seçilen gruptaki kayıt sayısına göredir. Takvim uyumu, son 30 günde tarihi gelen antrenmanlardan tamamlananların oranıdır. Dinlenme ve gelecek günler hesaba katılmaz.",14,Ui.Muted),Ui.Row(Ui.Field("Kayıt grubu",scope,280),Ui.Field("Kulüp filtresi",club,340)),body);
        void Render()
        {
            var selection=Ui.Selected(scope);Guid? id=(club.SelectedItem as ReportClub)?.Team?.Id;
            var users=service.Data.Users.Where(u=>u.Id!=Me.Id&&u.IsActive&&!u.NeedsApproval&&!u.IsClubAccount&&service.ShowRecord(u)&&(u.Role==Choices.Roles[0]||u.Role==Choices.Roles[1])&&(id==null||u.TeamId==id)).Where(u=>selection=="Fitness üyeleri"?u.Role==Choices.Roles[0]:true).ToList();
            var standings=users.Select(u=>ParticipationAnalytics.Standing(service.Data,u,DateOnly.FromDateTime(DateTime.Today))).ToList();body.Children.Clear();
            body.Children.Add(Ui.Stats(Ui.Stat("Kişi sayısı",users.Count.ToString()),Ui.Stat("Ülke sayısı",ParticipationAnalytics.Countries(standings).Count.ToString()),Ui.Stat("Ortalama takvim uyumu",(standings.Count==0?0:standings.Average(s=>s.Compliance)).ToString("0.0")+"%")));
            body.Children.Add(Ui.Text("Ülke dağılımı",21,Ui.Ink,true));body.Children.Add(Ui.Table(ParticipationAnalytics.Countries(standings).Select(c=>new{Country=c.Name,Count=c.Count,Share=c.Share.ToString("0.0")+"%",Compliance=c.AverageCompliance.ToString("0.0")+"%",Loss=c.AverageWeightLoss.ToString("0.0")+" kg"}),("Ülke","Country"),("Kişi","Count"),("Oran","Share"),("Takvim uyumu","Compliance"),("Ortalama kilo kaybı","Loss")));
            body.Children.Add(Ui.Text("Takvimine en düzenli uyanlar",21,Ui.Ink,true));
            var ranked=standings.Where(s=>s.Planned>0).OrderByDescending(s=>s.Compliance).ThenByDescending(s=>s.Completed).ThenBy(s=>s.User.Profile.Name).ToList();
            body.Children.Add(Ui.Table(ranked.Select((s,i)=>new{Rank=i+1,Name=s.User.Profile.Name,Country=CountryCatalog.ByCode(s.User.Profile.CountryCode)?.Name??s.User.Profile.Country,Completed=s.Completed,Planned=s.Planned,Percent=s.Compliance.ToString("0.0")+"%",Change=s.WeightLoss.ToString("0.0")+" kg"}),("Sıra","Rank"),("Ad soyad","Name"),("Ülke","Country"),("Tamamlanan","Completed"),("Planlanan","Planned"),("Uyum","Percent"),("Kilo kaybı","Change")));
            if(ranked.Count==0)body.Children.Add(Ui.Text("Bu grup için geçmiş antrenman kaydı yok.",14,Ui.Muted));
        }
        scope.SelectionChanged+=(_,_)=>Render();club.SelectionChanged+=(_,_)=>Render();Render();return Ui.Page(panel);
    }
    private sealed record ReportClub(Team? Team){public override string ToString()=>Team?.Name??AppLanguage.Text("Tüm kulüpler");}
}
