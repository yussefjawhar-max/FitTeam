using Avalonia.Controls;
using FitTeam.Core;
namespace FitTeam.Desktop.Views;
public partial class MainWindow
{
    private Control ProfilePage(User? targetUser = null)
    {
        var user = targetUser ?? Me; service.EnsureAccess(user);
        var p = user.Profile;var country=CountryPicker.Create(p.Country,p.CountryCode);var city=Ui.Input(p.City);var address=Ui.Input(p.HomeAddress);address.AcceptsReturn=true;var postal=new AddressEditor(country,p.Address,p.City); var blood=Ui.Combo(SignupApplication.BloodGroups,p.BloodGroup); var name = Ui.Input(p.Name); var age = Ui.Number(p.Age, 13, 90); var gender = Ui.Combo(["Kadın", "Erkek", "Belirtmek istemiyorum"], p.Gender);
        var height = Ui.Number(p.Height, 100, 230, 1); var weight = Ui.Number(p.Weight, 30, 300, 1); var target = Ui.Number(p.TargetWeight, 30, 300, 1);
        var goal = Ui.Combo(Choices.Goals, p.Goal); var level = Ui.Combo(Choices.Levels, p.Level); var water = Ui.Number(p.WaterGoal, 0.5m, 8, 1);
        var position = Ui.Combo(Choices.Positions, p.Position); var jersey = Ui.Number(p.Jersey, 1, 99);
        var days = Choices.Days.Select(d => new CheckBox { Content = Choices.DayName(d), IsChecked = p.AvailableDays.Contains(d), Tag = d }).ToList();
        var fields = new WrapPanel();
        foreach (var c in new[] { Ui.Field("Ad soyad", name), Ui.Field("Yaş", age), Ui.Field("Kan grubu",blood), Ui.Field("Cinsiyet", gender),Ui.Field("Ülke",country),Ui.Field("Boy (cm)", height), Ui.Field("Güncel kilo (kg)", weight), Ui.Field("Hedef kilo (kg)", target), Ui.Field("Hedef", goal), Ui.Field("Spor seviyesi", level), Ui.Field("Günlük su hedefi (litre)", water) }) fields.Children.Add(c);
        if(targetUser==null||service.IsPresident)fields.Children.Add(postal.View);
        if (user.Role == Choices.Roles[1]&&FitService.SportOf(user)=="Futbol") { fields.Children.Add(Ui.Field("Mevki", position)); fields.Children.Add(Ui.Field("Forma numarası", jersey)); }
        string team = service.Data.Teams.FirstOrDefault(t => t.Id == user.TeamId)?.Name ?? "Henüz bir takıma bağlı değil";
        var save = Ui.AsyncButton(targetUser == null ? "Bilgilerimi kaydet" : "Üye bilgilerini kaydet", () => Run(() =>
        {
            service.UpdateProfile(user, new Profile { Country=CountryPicker.Selected(country).Turkish,CountryCode=CountryPicker.Selected(country).Code,Address=targetUser==null||service.IsPresident?postal.Value():p.Address,HomeAddress=targetUser==null||service.IsPresident?postal.Value().Display:p.HomeAddress,City=targetUser==null||service.IsPresident?postal.Value().Province:p.City,HealthNotes=p.HealthNotes,BloodGroup=Ui.Selected(blood), Name = name.Text ?? "", Age = Ui.Int(age), Gender = Ui.Selected(gender), Height = Ui.Value(height), Weight = Ui.Value(weight), TargetWeight = Ui.Value(target), Goal = Ui.Selected(goal), Level = Ui.Selected(level), WaterGoal = Ui.Value(water), Position = Ui.Selected(position), Jersey = Ui.Int(jersey), AvailableDays = days.Where(c => c.IsChecked == true).Select(c => (DayOfWeek)c.Tag!).ToList() }); Refresh();
        }), true);
        bool UsernamesSafe(string value){try{Usernames.Validate(value);return true;}catch(ArgumentException){return false;}}
        var username=Ui.Input(UsernamesSafe(user.Username)?user.Username:"");username.MaxLength=20;var usernameStatus=Ui.Text("",13,Ui.Muted);
        var usernameCard=Ui.Card(Ui.Stack(Ui.Text("Kullanıcı adım",18,Ui.Ink,true),Ui.Field("3–20 karakter",username,400),usernameStatus,Ui.Button("Kullanıcı adımı kaydet",()=>{try{service.ChangeOwnUsername(username.Text??"");usernameStatus.Text="Kullanıcı adın kaydedildi. Sonraki girişinde bu adı kullan.";}catch(Exception ex){usernameStatus.Text=Ui.ErrorMessage(ex);}})));
        usernameCard.IsVisible=targetUser==null&&!service.IsPresident;
        var coachControls = user.Role == Choices.Roles[0] && user.Id == Me.Id ? CoachSelectionCard() : Ui.Card(Ui.Text("Profil ve antrenman bilgileri", 16, Ui.Ink, true));
        var windowCard=TrainingWindowCard();windowCard.IsVisible=targetUser==null&&!IsCoach&&!service.IsPresident;
        var clubCard=targetUser==null&&Me.Role==Choices.Roles[1]&&!service.IsPresident?ClubRequestCard():new Border{IsVisible=false};
        return Ui.Page(targetUser==null?HealthNotesCard():new Border{IsVisible=false},usernameCard,coachControls,windowCard,clubCard, Ui.Card(Ui.Stack(fields, Ui.Text("Antrenman yapabileceğin günler", 16, Ui.Ink, true), Ui.Row(days.ToArray()), Ui.Text("Gün değişikliği yeni programda ve kaçırılan antrenmanın taşınmasında kullanılır. Mevcut planı değiştirmek için program ekranındaki kişisel düzenlemeyi kullan.", 12, Ui.Muted), save)),
            Ui.Stats(Ui.Stat("Vücut kitle indeksi", Analytics.Bmi(p).ToString("0.0"), Analytics.BmiLabel(p)), Ui.Stat("İlk kayıt kilosu", $"{p.InitialWeight:0.0} kg", "İlk profil kaydında belirlendi"), Ui.Stat("Hedefe uzaklık", $"{Math.Abs(p.Weight - p.TargetWeight):0.0} kg", $"Toplam değişim: {p.Weight - p.InitialWeight:+0.0;-0.0;0.0} kg")),
            WeightGoalCard(user,targetUser==null),
            Ui.Card(Ui.Stack(Ui.Text("Hesap bilgileri", 18, Ui.Ink, true), Ui.Text($"E-posta / telefon / kullanıcı adı: {user.Username}\nKullanım türü: {FitService.RoleLabel(user)}" + (user.Role == Choices.Roles[1] ? "\nTakım: " + team : "")), Ui.Text("Vücut kitle indeksi, boy ve kilodan hesaplanan basit bir göstergedir. Program uyarlaması ders projesine ait örnek kurallardır.", 12, Ui.Muted))));
    }
}
