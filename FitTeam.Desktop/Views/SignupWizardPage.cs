using Avalonia;
using Avalonia.Controls;
using FitTeam.Core;
namespace FitTeam.Desktop.Views;
public partial class MainWindow
{
    private async Task SignupWizardDialog(string? sport=null,bool trainer=false)
    {
        if(sport==null&&pendingClubTicket!=null)sport=service.Data.Teams.First(t=>t.Id==pendingClubTicket.TeamId).SportBranch;
        var application=new SignupApplication{Sport=sport??"",Trainer=trainer,ClubTicket=pendingClubTicket};
        string[] labels=["Adın *","Soyadın *","Yaşın *","Spor branşın *","Kaç yıldır bu sporla ilgileniyorsun? *","E-posta adresin (isteğe bağlı)","Telefon numaran (isteğe bağlı)","Kan grubun"];
        string[] hints=["Adını yaz.","Soyadını yaz.","Yaşını rakamla yaz.","Katılacağın branşı seç.","Yıl olarak yaz. Yeni başlıyorsan 0 yaz.","E-posta yazarsan adresine gerçek doğrulama kodu gönderilir. Kodla adresini onayladıktan sonra devam edersin. Girişte kullanıcı adını ve FitTeam şifreni kullanırsın.","Telefon isteğe bağlı. Boş bırakabilir veya +90 5 sonrasındaki 9 rakamı yazabilirsin.","Bilmiyorsan Bilmiyorum seçebilirsin."];
        if(trainer){labels[4]="Kaç yıldır antrenörlük yapıyorsun? *";hints[4]="Antrenörlük deneyimini yıl olarak yaz. Yeni başlıyorsan 0 yaz.";}
        var fields=new Control[]{Ui.Input(),Ui.Input(),Ui.Input(),Ui.Combo(FitService.Branches,application.Sport),Ui.Input(),Ui.Input(),Ui.Input(),Ui.Combo(SignupApplication.BloodGroups)};
        for(int i=0;i<fields.Length;i++)Avalonia.Automation.AutomationProperties.SetName(fields[i],AppLanguage.Text(labels[i]));
        if(sport==null)((ComboBox)fields[3]).SelectedIndex=-1;else fields[3].IsEnabled=false;
        var displayed=fields.ToArray();
        var phoneRow=new Grid{ColumnDefinitions=new ColumnDefinitions("Auto,*"),FlowDirection=Avalonia.Media.FlowDirection.LeftToRight};
        var phonePrefix=Ui.Text("+90 5",18,Ui.Ink,true);phonePrefix.VerticalAlignment=Avalonia.Layout.VerticalAlignment.Center;phonePrefix.Margin=new Thickness(8);
        phoneRow.Children.Add(phonePrefix);Grid.SetColumn(fields[6],1);phoneRow.Children.Add(fields[6]);displayed[6]=phoneRow;
        var gender=Ui.Combo(new[]{"Kadın","Erkek","Belirtmek istemiyorum"});gender.SelectedIndex=-1;var country=CountryPicker.Create();var postal=new AddressEditor(country);
        var height=Ui.Number(175,100,230,1);var weight=Ui.Number(75,30,300,1);var target=Ui.Number(70,30,300,1);var goal=Ui.Combo(Choices.Goals);
        var days=Choices.Days.Select(d=>new CheckBox{Content=Choices.DayName(d),Tag=d,IsChecked=application.Days.Contains(d)}).ToList();
        var coachUsers=service.Data.Users.Where(u=>u.Role==Choices.Roles[2]&&service.ShowRecord(u)&&u.IsActive&&!u.NeedsApproval).ToList();
        var coaches=coachUsers.Select(u=>new CheckBox{Content=u.Profile.Name+" • "+HoursLabel(u),Tag=u.Id}).ToList();
        var coachPanel=Ui.Stack(Ui.Text("Hocalarını seç * (birden fazla seçebilirsin)",16,Ui.Ink,true));
        foreach(var kind in new[]{"Kadın","Erkek","Belirtmek istemiyorum"})
        {
            var list=coachUsers.Where(u=>kind=="Kadın"?u.Profile.Gender is "Kadın" or "Kız":kind=="Erkek"?u.Profile.Gender=="Erkek":u.Profile.Gender is not ("Kadın" or "Kız" or "Erkek")).ToList();
            coachPanel.Children.Add(Ui.Text((kind=="Kadın"?"Kadın hocalar":kind=="Erkek"?"Erkek hocalar":"Diğer hocalar")+" • "+list.Count,15,Ui.Ink,true));
            coachPanel.Children.Add(Ui.Stack(coaches.Where(c=>list.Any(u=>u.Id==(Guid)c.Tag!)).ToArray()));
        }
        if(coaches.Count==0)coachPanel.Children.Add(Ui.Text("Henüz aktif fitness hocası yok. Kayıt için yetkilinin hoca eklemesi gerekir.",13,Ui.Muted));
        var clubChoice=new ComboBox{PlaceholderText=AppLanguage.Text("İsteğe bağlı kulüp seç"),ItemTemplate=new Avalonia.Controls.Templates.FuncDataTemplate<TeamChoice>((c,_)=>Ui.UserText(c?.Team.Name??""))};
        var clubPanel=Ui.Field("Hangi kulübe katılmak istiyorsun?",clubChoice,520);
        var start=Ui.Input("","18:00");var hours=Ui.Combo(new[]{"2","3"});
        void FilterSport(){bool fitness=Ui.Selected((ComboBox)fields[3])=="Fitness";coachPanel.IsVisible=fitness;clubPanel.IsVisible=!fitness&&!trainer&&application.ClubTicket==null;clubChoice.ItemsSource=service.Data.Teams.Where(t=>t.IsActive&&service.ShowClub(t)&&t.SportBranch==Ui.Selected((ComboBox)fields[3])).Select(t=>new TeamChoice(t)).ToList();clubChoice.SelectedIndex=-1;}
        ((ComboBox)fields[3]).SelectionChanged+=(_,_)=>FilterSport();FilterSport();
        var details=Ui.Stack(Ui.Text("Kişisel bilgilerin",22,Ui.Ink,true),Ui.Field("Cinsiyet *",gender,520),Ui.Field("Ülke *",country,520),postal.View);
        if(!trainer){details.Children.Add(Ui.Row(Ui.Field("Boy (cm)",height),Ui.Field("Kilo (kg)",weight)));details.Children.Add(Ui.Row(Ui.Field("Hedef kilo (kg)",target),Ui.Field("Hedefin",goal)));details.Children.Add(Ui.Text("Gelebileceğin günleri seç *",16,Ui.Ink,true));details.Children.Add(Ui.Row(days.ToArray()));details.Children.Add(coachPanel);details.Children.Add(clubPanel);details.Children.Add(Ui.Text("Kulüp seçimi bir üyelik isteğidir; yetkili onayından sonra kulübe bağlanırsın.",13,Ui.Muted));details.Children.Add(Ui.Row(Ui.Field("Ders başlangıç saati *",start,245),Ui.Field("Ders aralığı (saat) *",hours,245)));details.Children.Add(Ui.Text("Bu aralık ders/randevu saatidir; hareket süreleri antrenmanında ayrı gösterilir.",13,Ui.Muted));}
        var username=Ui.Input();username.MaxLength=20;
        var pass=Ui.Input();pass.PasswordChar='●';var repeat=Ui.Input();repeat.PasswordChar='●';
        int reviewStep=9,passwordStep=reviewStep+1,step=0;bool busy=false;
        var panel=new StackPanel{Spacing=18};var w=Ui.Dialog("FitTeam • "+(trainer?"Antrenör kaydı":"Yeni üye kaydı"),panel,640,720);
        void Capture()
        {
            application.FirstName=((TextBox)fields[0]).Text??"";application.LastName=((TextBox)fields[1]).Text??"";
            application.Age=((TextBox)fields[2]).Text??"";application.Sport=Ui.Selected((ComboBox)fields[3]);application.Experience=((TextBox)fields[4]).Text??"";
            application.EmailAddress=((TextBox)fields[5]).Text??"";application.MobileDigits=((TextBox)fields[6]).Text??"";application.BloodGroup=Ui.Selected((ComboBox)fields[7]);
            application.Height=Ui.Value(height);application.Weight=Ui.Value(weight);application.TargetWeight=Ui.Value(target);application.Goal=Ui.Selected(goal);
            application.Days=days.Where(d=>d.IsChecked==true).Select(d=>(DayOfWeek)d.Tag!).ToList();application.CoachIds=coaches.Where(d=>d.IsChecked==true).Select(d=>(Guid)d.Tag!).ToList();application.TeamId=application.ClubTicket?.TeamId;application.Gender=Ui.Selected(gender);application.Country=country.Text??"";application.CountryCode=CountryCatalog.Resolve(country.Text)?.Code??"";if(step>=8){application.Address=postal.Read();application.HomeAddress=application.Address.Display;application.City=application.Address.Province;}
            application.Username=username.Text??"";application.RequestedClubId=(clubChoice.SelectedItem as TeamChoice)?.Team.Id;application.TrainingStart=TimeOnly.TryParseExact(LoginContact.NormalizeDigits(start.Text??""),"HH:mm",out var time)?time:null;application.TrainingHours=int.Parse(Ui.Selected(hours));application.Password=pass.Text??"";application.PasswordConfirmation=repeat.Text??"";
        }
        void Render()
        {
            panel.Children.Clear();panel.Children.Add(Ui.Text(trainer?"Antrenör kaydı":"Yeni üye kaydı",26,Ui.Ink,true));
            panel.Children.Add(Ui.Text($"Adım {step+1} / {passwordStep+1}",14,Ui.Green,true));
            panel.Children.Add(new ProgressBar{Minimum=0,Maximum=passwordStep+1,Value=step+1,Height=8,Foreground=Ui.Green});
            if(step<8){panel.Children.Add(Ui.Text(hints[step],15,Ui.Muted));panel.Children.Add(displayed[step]);panel.Children.Add(Ui.Text(labels[step],14,Ui.Ink,true));}
            else if(step<reviewStep)panel.Children.Add(details);
            else if(step==reviewStep)
            {
                panel.Children.Add(Ui.Text("Bilgilerini kontrol et",22,Ui.Ink,true));
                panel.Children.Add(Ui.Stack(Ui.UserText(application.FirstName+" "+application.LastName,16),Ui.Text("Yaş: "+application.Age+"\nBranş: "+application.Sport+"\nDeneyim: "+application.Experience+" yıl"+(application.Email.Length>0?"\nE-posta: "+application.Email:"")+(application.Phone.Length>0?"\nTelefon: "+application.Phone:"")+"\nKan grubu: "+application.BloodGroup,16)));
                panel.Children.Add(Ui.Text("Düzeltmek için Geri’ye bas. Sonraki adımda kendi FitTeam şifreni belirle.",13,Ui.Muted));
            }
            else panel.Children.Add(Ui.Stack(Ui.Text("Kullanıcı adını ve şifreni belirle",22,Ui.Ink,true),Ui.Field("Kullanıcı adın * (3–20 karakter)",username,520),Ui.Text("Her kişinin kendine ait benzersiz bir kullanıcı adı olur.",13,Ui.Muted),Ui.Text("En az 8 karakter kullan. Bu FitTeam şifrendir. Şifrem sayfasından istediğinde değiştirebilirsin.",14,Ui.Muted),Ui.Text("FitTeam şifren uygulamanın başkanı tarafından görülebilir. Başka hesaplarında kullandığın şifreyi burada kullanma.",13,Ui.Muted),Ui.Field("Şifren *",pass,520),Ui.Field("Şifreni tekrar yaz *",repeat,520)));
            var error=Ui.Text("",13,Avalonia.Media.Brushes.Firebrick);panel.Children.Add(error);
            var next=Ui.AsyncButton(step==passwordStep?"Kaydı tamamla ve giriş yap":"Sonraki",async()=>
            {
                if(busy)return;
                try
                {
                    Capture();if(step<8)application.ValidateStep(step);else if(step<reviewStep)application.ValidateTraining();
                    if((step==5||step==passwordStep)&&application.Email.Length>0&&!service.Email.IsVerified(application.Email)){busy=true;if(!await VerifyEmailDialog(w,application.Email))return;}
                    if(step<passwordStep){step++;Render();return;}
                    application.Validate();busy=true;codeAuth?.Logout();service.RegisterLocalSignup(application);ShowShell();
                    pass.Text=repeat.Text="";application.Password=application.PasswordConfirmation="";w.Close();
                }
                catch(Exception ex){error.Text=Ui.ErrorMessage(ex);}
                finally{busy=false;}
            },true);
            panel.Children.Add(next);
            if(step>0)panel.Children.Add(Ui.Button("Geri",()=>{if(busy)return;Capture();step--;Render();}));
            panel.Children.Add(Ui.Button("Vazgeç",()=>{if(!busy)w.Close();}));
            panel.Children.Add(Ui.Text(trainer?"* Zorunlu alan. Antrenör yetkisi için yetkili onayı gerekir.":"* Zorunlu alan.",12,Ui.Muted));
            if(step<8)fields[step].Focus();
        }
        w.Closed+=(_,_)=>{pass.Text=repeat.Text="";application.Password=application.PasswordConfirmation="";};
        Render();await w.ShowDialog(this);
    }
}
