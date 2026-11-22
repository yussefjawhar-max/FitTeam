using Avalonia.Controls;
using FitTeam.Core;
namespace FitTeam.Desktop.Views;
public partial class MainWindow
{
    private Control TrainingWindowCard()
    {
        var start=Ui.Input(Me.Profile.TrainingStart?.ToString("HH:mm")??"","18:00");var hours=Ui.Combo(new[]{"2","3"},Me.Profile.TrainingHours.ToString());var status=Ui.Text(TrainingWindow.Label(Me.Profile),14,Ui.Muted);
        return Ui.Card(Ui.Stack(Ui.Text("Ders / randevu saatim",20,Ui.Ink,true),Ui.Row(Ui.Field("Başlangıç saati",start),Ui.Field("Aralık (saat)",hours)),status,
            Ui.Button("Ders saatimi kaydet",()=>{try{if(!TimeOnly.TryParseExact(LoginContact.NormalizeDigits(start.Text??""),"HH:mm",out var time))throw new ArgumentException("Saati 18:00 biçiminde yaz.");service.SetTrainingWindow(time,int.Parse(Ui.Selected(hours)));status.Text=TrainingWindow.Label(Me.Profile);}catch(Exception ex){status.Text=Ui.ErrorMessage(ex);}},true),
            Ui.Text("Bu süre ders/randevu aralığıdır. Hareket ve dinlenme süreleri programda ayrı takip edilir.",13,Ui.Muted)));
    }
    private Control ClubRequestCard()
    {
        var clubs=service.Data.Teams.Where(t=>t.IsActive&&service.ShowClub(t)&&t.SportBranch==FitService.SportOf(Me)).Select(t=>new TeamChoice(t)).ToList();
        var selector=new ComboBox{ItemsSource=clubs,SelectedIndex=-1,PlaceholderText=AppLanguage.Text("Kulüp seç"),ItemTemplate=new Avalonia.Controls.Templates.FuncDataTemplate<TeamChoice>((c,_)=>Ui.UserText(c?.Team.Name??""))};
        var requested=service.Data.Teams.FirstOrDefault(t=>t.Id==Me.RequestedClubId);var status=Ui.UserText(requested==null?AppLanguage.Text("Kulüp üyeliği için istek gönderebilirsin."):AppLanguage.Text("Onay bekleyen kulüp:")+" "+requested.Name,14,Ui.Muted);
        return Ui.Card(Ui.Stack(Ui.Text("Kulübe katılmak istiyorum",20,Ui.Ink,true),Ui.Field("Katılmak istediğin kulüp",selector,450),status,Ui.Button("Kulüp isteği gönder",()=>{try{if(selector.SelectedItem is not TeamChoice club)throw new ArgumentException("Kulüp seç.");service.RequestClub(club.Team.Id);status.Text=AppLanguage.Text("Onay bekleyen kulüp:")+" "+club.Team.Name;}catch(Exception ex){status.Text=Ui.ErrorMessage(ex);}},true)));
    }
}
