using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using FitTeam.Core;
namespace FitTeam.Desktop.Views;
public partial class MainWindow
{
    private void ToggleWorkspace(){memberWorkspace=!memberWorkspace;ShowShell();}
    private void EnterMemberWorkspace(){memberWorkspace=true;ShowShell();}
    private Control AvatarView()
    {
        Control content=Ui.UserText(Me.Profile.Name.FirstOrDefault().ToString(),25,Ui.Ink,true);
        try{if(Me.Profile.AvatarBase64.Length>0)content=new Image{Source=new Bitmap(new MemoryStream(Convert.FromBase64String(Me.Profile.AvatarBase64))),Stretch=Stretch.UniformToFill};}catch{ }
        return new Border{Width=64,Height=64,Background=Brush.Parse("#F2F6DF"),CornerRadius=new(32),ClipToBounds=true,Padding=Me.Profile.AvatarBase64.Length==0?new Thickness(15):new Thickness(0),Child=content};
    }
    private sealed record ActivityChoice(string Id,string Label){public override string ToString()=>AppLanguage.Text(Label);}
    private Control MemberProfilePage()
    {
        var p=Me.Profile;var name=Ui.Input(p.Name);var phone=Ui.Input(Me.Phone,"+90…");var city=Ui.Input(p.City);var email=Ui.Input(Me.Email);email.IsReadOnly=true;
        var birth=new CalendarDatePicker{Watermark=AppLanguage.Text("Doğum tarihi"),SelectedDate=p.BirthDate?.ToDateTime(TimeOnly.MinValue),DisplayDateEnd=DateTime.Today,HorizontalAlignment=HorizontalAlignment.Stretch};
        var gender=Ui.Combo(["Kadın","Erkek","Belirtmek istemiyorum"],p.Gender);var height=Ui.Number(p.Height,100,230,1);var weight=Ui.Number(p.Weight,30,300,1);var target=Ui.Number(p.TargetWeight,30,300,1);var goal=Ui.Combo(Choices.Goals,p.Goal);
        var activities=new[]{new ActivityChoice("","Belirtilmemiş")}.Concat(Nutrition.Activities.Select(a=>new ActivityChoice(a.Id,a.Label))).ToList();var activity=new ComboBox{ItemsSource=activities,SelectedItem=activities.FirstOrDefault(a=>a.Id==p.ActivityLevel)??activities[0]};
        var health=Notes(p.HealthNotes,"Antrenörünle paylaşmak istediğin bilgiler");health.MaxLength=3000;
        var person=Ui.Card(Ui.Stack(Ui.Row(AvatarView(),Ui.Text("Hesap bilgilerim",21,Ui.Ink,true)),Ui.AsyncButton("Fotoğraf seç",()=>Guard(async()=>{var file=(await StorageProvider.OpenFilePickerAsync(new(){Title=AppLanguage.Text("Profil fotoğrafı seç"),AllowMultiple=false,FileTypeFilter=[FilePickerFileTypes.ImageAll]})).FirstOrDefault();if(file==null)return;await using var stream=await file.OpenReadAsync();using var bitmap=Bitmap.DecodeToWidth(stream,240);using var png=new MemoryStream();bitmap.Save(png);service.SaveAvatar(png.ToArray());Refresh();})),Ui.Field("Ad soyad",name,350),Ui.Field("E-posta",email,350),Ui.Text("İletişim için kaydettiğin e-posta adresin burada gösterilir.",12,Ui.Muted),Ui.Field("Telefon",phone,350),Ui.Field("Şehir",city,350)));
        var metrics=Ui.Card(Ui.Stack(Ui.Text("Hedefim ve ölçülerim",21,Ui.Ink,true),Ui.Row(Ui.Field("Doğum tarihi",birth,210),Ui.Field("Cinsiyet",gender,175)),Ui.Text(p.BirthDate==null?"Doğum tarihini seçmezsen mevcut yaşın korunur.":"",12,Ui.Muted),Ui.Row(Ui.Field("Boy (cm)",height,175),Ui.Field("Kilo (kg)",weight,175)),Ui.Field("Hedef kilo (kg)",target,370),Ui.Field("Hedefim",goal,370),Ui.Field("Hareket düzeyim",activity,370)));
        var row=new Grid{ColumnDefinitions=new("*,*"),ColumnSpacing=22};row.Children.Add(person);Grid.SetColumn(metrics,1);row.Children.Add(metrics);
        return Ui.Page(row,Ui.Card(Ui.Stack(Ui.Text("Sağlık notlarım",21,Ui.Ink,true),Ui.Text("Notların profilinde saklanır; bağlı antrenörlerin ve yetkili yöneticiler inceleyebilir.",13,Ui.Muted),health)),Ui.Card(Ui.Row(Ui.Text("Değişiklikler kaydedilene kadar uygulanmaz.",13,Ui.Muted),Ui.AsyncButton("Kaydet",()=>Run(()=>{DateOnly? date=birth.SelectedDate.HasValue?DateOnly.FromDateTime(birth.SelectedDate.Value):null;service.SaveMemberBasics(name.Text??"",date,Ui.Selected(gender),Ui.Value(height),Ui.Value(weight),Ui.Value(target),Ui.Selected(goal),(activity.SelectedItem as ActivityChoice)?.Id??"",city.Text??"",phone.Text??"",health.Text??"");Refresh();}),true))),Ui.Row(Ui.Button("Şifremi değiştir",()=>ShowPage("Şifrem")),Ui.Button("Günler, adres ve gelişmiş profil",()=>ShowPage("Gelişmiş profil"))));
    }
    private async Task GymEditorDialog(Gym? gym=null)
    {
        var name=Ui.Input(gym?.Name??"");var city=Ui.Input(gym?.City??"");var address=Notes(gym?.Address??"","Salon adresi");var hours=Ui.Input(gym?.Hours??"","09:00–22:00");var description=Notes(gym?.Description??"","Salonu tanıt");description.MaxLength=1500;
        var people=service.Data.Users.Where(u=>u.IsActive&&!u.NeedsApproval&&(u.Id==Me.Id||u.Role==Choices.Roles[2]||u.Role==Choices.Roles[3])).ToList();var manager=new ComboBox{ItemsSource=people,SelectedItem=people.FirstOrDefault(u=>u.Id==(gym?.ManagerId??Me.Id)),ItemTemplate=new Avalonia.Controls.Templates.FuncDataTemplate<User>((u,_)=>Ui.UserText(u?.Profile.Name??""))};
        var panel=Ui.Stack(Ui.Text(gym==null?"Spor salonu ekle":"Spor salonunu düzenle",24,Ui.Ink,true),Ui.Field("Salon adı",name,480),Ui.Field("Şehir",city,480),address,Ui.Field("Çalışma saatleri",hours,480),description);if(service.HasAdministrationAccess)panel.Children.Add(Ui.Field("Salon yetkilisi",manager,480));var error=Ui.Text("",13,Ui.Muted);panel.Children.Add(error);var dialog=Ui.Dialog("Spor salonu",panel,590,750);
        panel.Children.Add(Ui.Button("Kaydet",()=>{try{service.SaveGym(gym?.Id,name.Text??"",city.Text??"",address.Text??"",hours.Text??"",description.Text??"",(manager.SelectedItem as User)?.Id);dialog.Close();Refresh();}catch(Exception ex){error.Text=Ui.ErrorMessage(ex);}},true));await dialog.ShowDialog(this);
    }
    private Control HelpPage()
    {
        var guide=Ui.Card(Ui.Stack(Ui.Text("Yardım ve destek",25,Ui.Ink,true),Ui.Text("FitTeam’de nasıl ilerleyebilirsin?",15,Ui.Muted),Ui.Text("Profilim bölümünde bilgilerini ve hedefini kaydet. Antrenörüm bölümünden bir veya birkaç hoca seç. Programlarım bölümünde antrenmanlarını aç. Günlüğüm ve Ölçümlerim bölümünde gelişimini kaydet. Mesajlar bölümünde hocanla konuş. Spor Salonum bölümünde başvur; onaydan sonra QR ile giriş yap.",15),Ui.Row(Ui.Button("Profilimi aç",()=>ShowPage("Profilim"),true),Ui.Button("Antrenörler",()=>ShowPage("Antrenörüm")),Ui.Button("Uygulama dili",()=>ShowPage("Uygulama dili")))));
        var data=Ui.Card(Ui.Stack(Ui.Text("Veriler ve yedekler",22,Ui.Ink,true),Ui.Text("Bu masaüstü sürümünde kayıtlar bu bilgisayarda saklanır. Kullanıcı adın ve FitTeam şifrenle giriş yaparsın.",14,Ui.Muted),Ui.Button("Raporlar ve yedekler",()=>ShowPage("Raporlar"))));
        return Ui.Page(guide,data);
    }
}
