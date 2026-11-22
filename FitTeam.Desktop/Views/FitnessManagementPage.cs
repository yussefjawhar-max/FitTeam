using Avalonia.Controls;
using FitTeam.Core;
namespace FitTeam.Desktop.Views;
public partial class MainWindow
{
    private Control FitnessManagementPage()
    {
        if(!service.HasAdministrationAccess)throw new InvalidOperationException("Yetkili hesabıyla giriş yap.");
        var population=service.Data.Users.Where(service.ShowRecord).ToList();var summary=FitnessStatistics.Calculate(population);
        var members=population.Where(u=>u.IsActive&&!u.NeedsApproval&&!u.IsClubAccount&&u.Role==Choices.Roles[0]&&!u.Email.Equals("jawharyusuf446@gmail.com",StringComparison.OrdinalIgnoreCase)).ToList();
        var coaches=population.Where(u=>u.IsActive&&!u.NeedsApproval&&!u.IsClubAccount&&u.Role==Choices.Roles[2]).ToList();
        var body=Ui.Stack(Ui.Text("Fitness yönetimi",26,Ui.Ink,true),
            Ui.Stats(Ui.Stat("Fitness hocası",summary.Coaches.ToString()),Ui.Stat("Fitness üyesi",summary.Members.ToString()),Ui.Stat("Türkiye",summary.Turkey.ToString(),"%"+summary.TurkeyShare.ToString("0.#")),Ui.Stat("Diğer ülkeler",summary.OtherCountries.ToString(),"%"+summary.OtherShare.ToString("0.#"))),
            Ui.Text("Dağılım profildeki ülke bilgisine göre hesaplanır. Ülkesi belirtilmeyen: "+summary.UnknownCountry+". Çıkarılan ve onay bekleyen hesaplar sayılmaz.",13,Ui.Muted),
            Ui.Card(Ui.Row(Ui.Button("Kulüpler ve öğrenciler",()=>ShowPage("Kulüpler ve öğrenciler")),Ui.Button("Kulüp başkanları",()=>ShowPage("Kulüp başkanları")),Ui.Button("Ülke ve takvim raporu",()=>ShowPage("Ülke ve takvim raporu")))));
        var standings=members.Select(u=>ParticipationAnalytics.Standing(service.Data,u,DateOnly.FromDateTime(DateTime.Today))).ToList();
        body.Children.Add(Ui.Card(Ui.Stack(Ui.Text("Ülke dağılımı",21,Ui.Ink,true),Ui.Table(ParticipationAnalytics.Countries(standings).Select(c=>new{Country=c.Name,Count=c.Count,Share=c.Share.ToString("0.#")+"%"}),("Ülke","Country"),("Kişi","Count"),("Oran","Share")))));
        var teacherPanel=Ui.Stack(Ui.Row(Ui.Text("Fitness hocaları",22,Ui.Ink,true),Ui.AsyncButton("+ Hoca ekle",()=>Guard(()=>AdminAddUserDialog(Choices.Roles[2])),true)));
        foreach(var kind in new[]{"Kadın","Erkek","Diğer"})
        {
            var group=coaches.Where(u=>kind=="Kadın"?u.Profile.Gender is "Kadın" or "Kız":kind=="Erkek"?u.Profile.Gender=="Erkek":u.Profile.Gender is not ("Kadın" or "Kız" or "Erkek")).ToList();
            teacherPanel.Children.Add(Ui.Text((kind=="Kadın"?"Kadın antrenörler":kind=="Erkek"?"Erkek antrenörler":"Diğer antrenörler")+" • "+group.Count,17,Ui.Ink,true));
            foreach(var coach in group)teacherPanel.Children.Add(Ui.Row(Ui.AsyncButton(coach.Profile.Name,()=>Guard(()=>StudentDetailsDialog(coach))),Ui.Text("Yaş: "+coach.Profile.Age+" • Deneyim: "+coach.ExperienceYears+" yıl • "+(coach.IsExample?AppLanguage.Text("Örnek hesap")+" • ":"")+AppLanguage.Text("Bağlı üye:")+" "+members.Count(u=>FitService.CoachLinks(u).Contains(coach.Id))+" • "+HoursLabel(coach),13,Ui.Muted)));
        }
        body.Children.Add(Ui.Card(teacherPanel));
        var search=Ui.Input("","Öğrencinin adını, soyadını veya kullanıcı adını yaz");var list=new StackPanel{Spacing=9};var count=Ui.Text("",13,Ui.Muted);
        void Filter(){list.Children.Clear();var found=string.IsNullOrWhiteSpace(search.Text)?members.OrderBy(u=>u.Profile.Name).ToList():StudentSearch.Find(members,search.Text);count.Text=found.Count+" fitness üyesi";foreach(var member in found.Take(50))list.Children.Add(Ui.AsyncButton(member.Profile.Name+" • "+member.Username,()=>Guard(()=>StudentDetailsDialog(member))));LanguageView.Apply(list);}
        search.TextChanged+=(_,_)=>Filter();Filter();body.Children.Add(Ui.Card(Ui.Stack(Ui.Text("Fitness üyeleri",22,Ui.Ink,true),search,count,list)));
        return Ui.Page(body);
    }
}
