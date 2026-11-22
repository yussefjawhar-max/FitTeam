using Avalonia.Controls;
using FitTeam.Core;
namespace FitTeam.Desktop.Views;
public partial class MainWindow
{
    private Task NewMemberEntryDialog() => SignupWizardDialog();
    private static Control EntryBoxes(params (string Title,string Hint,Func<Task> Action)[] entries)
    {
        var grid=new Grid{ColumnDefinitions=new ColumnDefinitions("*,*"),RowDefinitions=new RowDefinitions(string.Join(",",Enumerable.Repeat("Auto",(entries.Length+1)/2)))};
        int index=0;
        foreach(var entry in entries)
        {
            var button=Ui.AsyncButton(entry.Title,entry.Action,true);
            Avalonia.Automation.AutomationProperties.SetName(button,AppLanguage.Text(entry.Title));
            button.Content=Ui.Stack(Ui.Text(entry.Title,18,Ui.Ink,true),Ui.Text(entry.Hint,12,Avalonia.Media.Brush.Parse("#38440A")));
            button.HorizontalAlignment=Avalonia.Layout.HorizontalAlignment.Stretch;
            button.HorizontalContentAlignment=Avalonia.Layout.HorizontalAlignment.Left;
            button.MinHeight=120;button.Margin=new Avalonia.Thickness(5);button.Padding=new Avalonia.Thickness(18);
            Grid.SetRow(button,index/2);Grid.SetColumn(button,index%2);
            if(entries.Length%2==1&&index==entries.Length-1)Grid.SetColumnSpan(button,2);
            grid.Children.Add(button);index++;
        }
        return grid;
    }
    private async Task TrainerEntryDialog()
    {
        string? sport=null;
        var panel=Ui.Stack(Ui.Text("Antrenörler",26,Ui.Ink,true),Ui.Text("Branşını seç. Her bölüm kendi antrenör hesabına açılır.",14,Ui.Muted));
        var w=Ui.Dialog("FitTeam • Antrenörler",panel,620,480);
        panel.Children.Add(EntryBoxes(new[]{"Fitness","Basketbol","Futbol","Voleybol"}.Select(branch=>(branch+" antrenörü","Giriş / kayıt",(Func<Task>)(()=>{sport=branch;w.Close();return Task.CompletedTask;}))).ToArray()));
        await w.ShowDialog(this);
        if(sport!=null)await TrainerRoleEntryDialog(sport);
    }
    private async Task TrainerRoleEntryDialog(string sport)
    {
        string? action=null;var panel=Ui.Stack(Ui.Text(sport+" antrenörü",26,Ui.Ink,true),Ui.Text("Kayıtlıysan normal giriş yap. Yeni kayıtta yaşın ve antrenörlük deneyimin sorulacak.",14,Ui.Muted));
        var w=Ui.Dialog("FitTeam • "+sport+" antrenörü",panel,620,430);
        panel.Children.Add(EntryBoxes(("Normal giriş","Kayıtlı antrenör hesabım",()=>{action="login";w.Close();return Task.CompletedTask;}),("Yeni kayıt","Antrenör bilgilerini doldur",()=>{action="signup";w.Close();return Task.CompletedTask;})));
        await w.ShowDialog(this);
        if(action=="signup")await SignupWizardDialog(sport,true);else if(action=="login")await LocalPasswordEntryDialog(sport=="Fitness"?Choices.Roles[2]:Choices.Roles[3],sport);
    }
    private async Task LocalPasswordEntryDialog(string? requiredRole=null,string? requiredSport=null)
    {
        var username=Ui.Input("","Kullanıcı adın");var password=Ui.Input();password.PasswordChar='●';var error=Ui.Text("",13,Ui.Muted);
        var panel=Ui.Stack(Ui.Text("Normal giriş",24,Ui.Ink,true),Ui.Text("Kendi kullanıcı adın ve FitTeam şifrenle giriş yap. Yeniysen Yeni üye kaydı bölümünden hesabını oluştur.",14,Ui.Muted),Ui.Field("Kullanıcı adı",username,440),Ui.Field("FitTeam şifresi",password,440),error);var w=Ui.Dialog("FitTeam • Giriş",panel,560,540);
        panel.Children.Add(Ui.AsyncButton("Giriş yap",async()=>{try{codeAuth?.Logout();if(!service.Login(username.Text??"",password.Text??""))throw new ArgumentException("Kullanıcı adı veya şifre yanlış ya da hesap çıkarıldı.");
            if(requiredRole!=null&&(!FitService.MatchesTrainerEntry(Me,requiredRole,requiredSport))){service.Logout();throw new ArgumentException("Bu hesap seçilen antrenör branşına yetkili değil.");}
            password.Text="";if(await CompleteLocalLogin()){if(pendingClubTicket!=null)service.JoinClub(pendingClubTicket);w.Close();ShowShell();}
        }catch(Exception ex){error.Text=Ui.ErrorMessage(ex);}},true));
        bool recovery=false;
        panel.Children.Add(Ui.Button("Şifremi unuttum",()=>{recovery=true;password.Text="";w.Close();}));
        await w.ShowDialog(this);
        if(recovery)await PasswordRecoveryDialog(username.Text??"",requiredRole,requiredSport);
    }
    private void ShowPendingMembership()
    {
        Content=Ui.Page(Ui.Card(Ui.Stack(Ui.Text("Üyeliğin onay bekliyor",28,Ui.Ink,true),Ui.Text(Me.Profile.Name+", bilgilerin kaydedildi. Yetkili hesabından üyeliğin onaylandıktan sonra tekrar giriş yapabilirsin.",16,Ui.Muted),Ui.Button("Giriş ekranına dön",()=>{codeAuth?.Logout();service.Logout();ShowLogin();},true))));
    }
    private Control CoachHoursPage()=>Ui.Page(Ui.Card(Ui.Stack(Ui.Text("Çalışma saatlerim",24,Ui.Ink,true),Ui.Text(HoursLabel(Me),15,Ui.Muted),Ui.AsyncButton("Çalışma saatlerimi düzenle",()=>Guard(()=>CoachHoursDialog(Me)),true))));
    private static string HoursLabel(User user)=>user.WorkHours.Count==0?"Çalışma saati henüz tanımlanmadı.":string.Join("\n",user.WorkHours.OrderBy(h=>h.Day).ThenBy(h=>h.Start).Select(h=>Choices.DayName(h.Day)+" • "+h.Start.ToString("HH:mm")+" – "+h.End.ToString("HH:mm")));
    private async Task CoachHoursDialog(User target)
    {
        var hours=target.WorkHours.Select(h=>new CoachHours{Day=h.Day,Start=h.Start,End=h.End}).ToList();
        var day=Ui.Combo(Choices.Days.Select(Choices.DayName));var start=Ui.Input("09:00");var end=Ui.Input("17:00");var list=new StackPanel{Spacing=8};var error=Ui.Text("",13,Ui.Muted);
        void Render(){list.Children.Clear();foreach(var h in hours.ToList())list.Children.Add(Ui.Row(Ui.Text(Choices.DayName(h.Day)+" • "+h.Start.ToString("HH:mm")+" – "+h.End.ToString("HH:mm")),Ui.Button("Kaldır",()=>{hours.Remove(h);Render();})));}
        var panel=Ui.Stack(Ui.Text(target.Profile.Name+" • Çalışma saatleri",23,Ui.Ink,true),Ui.Text("Haftanın günlerini ve başlangıç/bitiş saatlerini ekle. Saat biçimi 09:00 gibi olmalı.",13,Ui.Muted),Ui.Field("Gün",day,440),Ui.Row(Ui.Field("Başlangıç saati",start),Ui.Field("Bitiş saati",end)),error,list);
        var w=Ui.Dialog("FitTeam • Antrenör saatleri",panel,620,660);
        panel.Children.Add(Ui.Button("Saat aralığı ekle",()=>{if(!TimeOnly.TryParseExact(start.Text,"HH:mm",out var a)||!TimeOnly.TryParseExact(end.Text,"HH:mm",out var b)||a>=b){error.Text="Başlangıç ve bitişi HH:mm olarak gir; bitiş sonra olmalı.";return;}hours.Add(new CoachHours{Day=Choices.Days[day.SelectedIndex],Start=a,End=b});error.Text="";Render();}));
        panel.Children.Add(Ui.Button("Saatleri kaydet",()=>{try{service.SetCoachHours(target,hours);w.Close();Refresh();}catch(Exception ex){error.Text=Ui.ErrorMessage(ex);}},true));Render();await w.ShowDialog(this);
    }
}
