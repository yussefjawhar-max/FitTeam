using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using FitTeam.Core;
using FitTeam.Controllers;
namespace FitTeam.Desktop.Views;

public partial class MainWindow : Window
{
    private readonly FitTeamController service;
    private Control? shell;
    private readonly Dictionary<string, Button> navigation = [];
    private string currentPage = "";
    private bool memberWorkspace;
    private readonly Avalonia.Threading.DispatcherTimer portalTimer=new(){Interval=TimeSpan.FromMinutes(1)};
    private User? memberPlanTarget;
    public MainWindow() { InitializeComponent(); service = null!; }
    public MainWindow(FitService model)
    {
        InitializeComponent(); service = new FitTeamController(model, CourseRuntime.EmailSender, CourseRuntime.MailStore); shell = Content as Control; FlowDirection=LanguageView.Direction;Title=AppLanguage.Text(Title);if(shell!=null)LanguageView.Apply(shell);
        LogoutButton.Click += (_, _) => { if(service.IsPreview){Close();return;}practiceExercises=null;practiceDraft=null;sessionDayId=null;codeAuth?.Logout(); service.Logout(); ShowLogin(); };
        WorkspaceButton.Click+=(_,_)=>ToggleWorkspace();
        NotificationButton.Click+=(_,_)=>ShowPage("Bildirimler");
        portalTimer.Tick+=(_,_)=>{if(service.Current!=null){var due=service.TickReminders(DateTimeOffset.Now);UpdateNotificationLabel();if(due.Count>0&&currentPage=="Bildirimler")Refresh();}};
        Closed += (_, _) => {portalTimer.Stop();identityServer.Dispose();};
        ShowLogin();
    }
    private User Me => service.RequireCurrent();
    private bool IsCoach => Me.Role == Choices.Roles[2] || Me.Role == Choices.Roles[3];
    private async Task Guard(Func<Task> action)
    {
        try { await action(); }
        catch (Exception ex) { await Ui.Message(this, Ui.ErrorMessage(ex)); }
    }
    private Task Run(Action action) => Guard(() => { action(); return Task.CompletedTask; });
    private void ShowLogin()
    {
        memberWorkspace=false;currentPage = ""; memberPlanTarget = null;FlowDirection=LanguageView.Direction;Title=AppLanguage.Text("FitTeam • Antrenman ve Sporcu Takibi");
        var boxes = WelcomeEntryBoxes(
            ("Yetkili girişi", "Hesap ve kulüp yönetimi", () => Guard(PresidentLoginDialog)),
            ("Yeni üye kaydı", "İlk kez katılıyorum", () => Guard(NewMemberEntryDialog)),
            ("Normal giriş", "Kayıtlı üye hesabım", () => Guard(() => ContactChoiceDialog())),
            ("Antrenörler", "Branşına göre giriş yap", () => Guard(TrainerEntryDialog)),
            ("Kulüp girişi", "Kulüp adı ve 8 haneli şifre", () => Guard(ClubLoginDialog)));
        var right = Ui.Card(Ui.Stack(Ui.Text("Hoş geldin", 28, Ui.Ink, true),
            Ui.Text("Devam etmek istediğin bölümü seç.",14,Ui.Muted),LanguageSelector(),boxes,Ui.Button("Örnek üye panelini incele",OpenMemberPreview),Ui.Button("Örnek yetkili panelini incele",OpenAdministrationPreview)));
        right.MaxWidth = 470; right.HorizontalAlignment = HorizontalAlignment.Stretch; right.VerticalAlignment = VerticalAlignment.Center;
        var hero = Ui.Stack(Ui.Text("FitTeam", 40, Brushes.White, true), Ui.Text("Sana uyum sağlayan\nbir antrenman yolculuğu.", 34, Brushes.White, true), Ui.Text("Planla. Kaydet. Gelişimini gör.", 18, Brush.Parse("#A6CBC7")),
            new Border { Height = 28 }, Ui.Text("30 günlük kişisel program", 18, Brushes.White, true), Ui.Text("Yorgunluğuna göre güncellenen antrenmanlar,\nkilo ve su takibi, futbol performansı ve koç paneli.", 15, Brush.Parse("#BECDD9")), new Border { Height = 30 }, Ui.Text("04  kullanıcı rolü     /     01  ortak hedef", 13, Brush.Parse("#88CDBA")));
        hero.VerticalAlignment = VerticalAlignment.Center;
        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("*,*"), Background = Brush.Parse("#F3F6FA") };
        grid.Children.Add(new Border { Background = Ui.Ink, Padding = new Thickness(54), Child = hero });
        var r = new Border { Padding = new Thickness(30), Child = new ScrollViewer {Content=right,VerticalScrollBarVisibility=ScrollBarVisibility.Auto,HorizontalScrollBarVisibility=ScrollBarVisibility.Disabled} }; Grid.SetColumn(r, 1); grid.Children.Add(r); Content = grid;
    }
    private Task RegisterDialog() => SignupWizardDialog();
    private void ShowShell()
    {
        if(Me.NeedsApproval&&!service.IsPresident){ShowPendingMembership();return;}
        FlowDirection=LanguageView.Direction;if(shell!=null)LanguageView.Apply(shell);Content = shell; Navigation.Children.Clear(); navigation.Clear(); SidebarName.Text = Me.Profile.Name; SidebarRole.Text = service.IsPresident ? memberWorkspace?"Başkan • Kişisel panel":"Başkan" : FitService.RoleLabel(Me); DateLabel.Text = DateTime.Today.ToString("dd MMM yyyy");
        string[] pages = service.HasAdministrationAccess ? ["Başkan paneli", "Üye listesi", "Hocalar", "Kulüp başkanları", "Kulüpler ve öğrenciler", "Kişiler ve kulüpler", "Yeni üyeler", "Hesaplar ve şifreler"] : IsCoach ? ["Genel bakış", "Üyeler ve takım", "Koç gelen kutusu", "Egzersiz kütüphanesi", "Gelişim grafikleri", "Raporlar", "Profilim", "Şifrem", "Çalışma saatlerim"] : Me.Role == Choices.Roles[1]
            ? ["Genel bakış", "Antrenmanım", "Evde antrenman", "30 günlük program", "Antrenman kaydı", "Kilo ve su takibi", "Kalori ve beslenme", "Egzersiz kütüphanesi", "Maç performansı", "Gelişim grafikleri", "Raporlar", "Profilim", "Şifrem"]
            : ["Genel bakış", "Antrenmanım", "Evde antrenman", "30 günlük program", "Antrenman kaydı", "Kilo ve su takibi", "Kalori ve beslenme", "Egzersiz kütüphanesi", "Gelişim grafikleri", "Raporlar", "Profilim", "Şifrem"];
        if(Me.Role==Choices.Roles[1]&&FitService.SportOf(Me)!="Futbol")pages=pages.Where(p=>p!="Maç performansı").ToArray();
        if(service.HasAdministrationAccess)pages=[..pages,"Fitness yönetimi","Ülke ve takvim raporu","E-posta ayarları","MySQL tabloları"];
        if(service.HasFullAdministrationView)pages=[..pages,"Yetkili yardımcıları","Şifrem"];else if(service.HasAdministrationAccess)pages=[..pages.Where(p=>service.HasFullAdministrationView||p!="Hesaplar ve şifreler"),"Şifrem"];
        if(memberWorkspace||!service.HasAdministrationAccess&&!IsCoach){
            pages=["Panelim","Programlarım","Beslenme Listem","Kalori ve beslenme","Gıda Rehberi","Günlüğüm","Ölçümlerim","Hatırlatmalar","QR Okut","Mesajlar","Antrenörüm","Spor Salonum","Profilim","Şifrem","Antrenmanım","Evde antrenman","Egzersiz kütüphanesi","Gelişim grafikleri","Raporlar"];
            if(Me.Role==Choices.Roles[1]&&FitService.SportOf(Me)=="Futbol")pages=[..pages,"Maç performansı"];
        }else pages=[..pages,"Mesajlar","Salon ve beslenme yönetimi"];
        pages=[..pages,"Bildirimler","Uygulama dili","Yardım ve destek"];
        WorkspaceButton.IsVisible=service.HasAdministrationAccess||IsCoach;WorkspaceButton.Content=AppLanguage.Text(memberWorkspace?"Yönetim panelim":"Kişisel panelim");
        service.TickReminders(DateTimeOffset.Now);portalTimer.Start();UpdateNotificationLabel();
        foreach (var page in pages) { var b = Ui.Button(page, () => ShowPage(page)); b.Classes.Add("nav"); b.HorizontalAlignment = HorizontalAlignment.Stretch; navigation[page] = b; Navigation.Children.Add(b); }
        ShowPage(memberWorkspace?"Panelim":service.HasAdministrationAccess ? "Başkan paneli" : IsCoach?"Genel bakış":"Panelim");
    }
    private void ShowPage(string page)
    {
        currentPage = page; foreach (var entry in navigation) entry.Value.Classes.Set("active", entry.Key == page || page == "Üye programı" && entry.Key == "Üyeler ve takım" || page == "Canlı antrenman" && entry.Key == "Antrenmanım");
        PageTitle.Text = page == "Üye programı" && memberPlanTarget != null ? memberPlanTarget.Profile.Name + " • Antrenman sayfası" : page; SidebarName.Text = Me.Profile.Name;
        PageSubtitle.Text = page switch { "Panelim"=>"Antrenmanın, kayıtların ve gelişimin tek yerde.", "Günlüğüm"=>"Günlük alışkanlıklarını ve aktivitelerini kaydet.", "Ölçümlerim"=>"Ölçümlerini karşılaştır, gelişimini takip et.", "Hatırlatmalar"=>"Su ve spor saatlerini belirle.", "Bildirimler"=>"Hatırlatmaların ve hesap güncellemelerin.", "Başkan paneli" or "Üye listesi" => service.LoginMethod, "Genel bakış" => $"Merhaba {Me.Profile.Name.Split(' ')[0]}, bugün de bir adım ileri.", "Canlı antrenman" => "Setleri işaretle, dinlenmeni takip et, sonucunu kaydet.", "Egzersiz kütüphanesi" => "Hareketlerini filtrele ve şablonlarını tekrar kullan.", "Koç gelen kutusu" => "Üyelerin geri bildirimlerini incele ve programlarına yansıt.", "Antrenmanım" => "Sana özel günlük antrenman sayfan.", "Üye programı" => "Günleri seç, antrenmanını hazırla, katılım bildirimini gör.", "30 günlük program" => "Seçtiğin günlere göre hazırlanır, antrenman kayıtlarınla güncellenir.", "Antrenman kaydı" => "Yaptığın hareketleri ve saat verilerini kaydet.", "Kilo ve su takibi" => "Küçük alışkanlıklar, görünür gelişim.", "Maç performansı" => "Sahadaki katkını ve maç istatistiklerini takip et.", "Gelişim grafikleri" => "Kayıtlarından oluşan gelişim çizgisi.", "Üyeler ve takım" => "Sporcularının durumunu karşılaştır, not bırak.", "Raporlar" => "30 günlük gelişimi incele ve dışa aktar.", _ => "Bilgilerini ve uygun olduğun günleri düzenle." };
        try { PageContent.Content = page switch { "E-posta ayarları" => EmailSettingsPage(), "MySQL tabloları" => DatabaseTablesPage(), "Panelim" => PortalDashboard(), "Programlarım"=>PlanPage(), "Beslenme Listem"=>DietPage(), "Gıda Rehberi"=>FoodGuidePage(), "Günlüğüm"=>DiaryPage(), "Ölçümlerim"=>MeasurementsPage(), "Hatırlatmalar"=>RemindersPage(), "QR Okut"=>QrPage(), "Mesajlar"=>MessagesPage(), "Antrenörüm"=>MyCoachesPage(), "Spor Salonum"=>MyGymPage(), "Bildirimler"=>NotificationsPage(), "Salon ve beslenme yönetimi"=>PortalAdminPage(), "Başkan paneli" => PresidentPage(), "İsim listesi" => PresidentNameList(), "Üye listesi" => PresidentPage(true), "Hocalar" => PresidentCoachesPage(), "Kişiler ve kulüpler" => AdministrationPage(), "Fitness yönetimi" => FitnessManagementPage(), "Yeni üyeler" => NewMembersPage(), "Yetkili yardımcıları" => DeputiesPage(), "Hesaplar ve şifreler" => OwnerDirectoryPage(), "Kulüp başkanları" => ClubRegistryPage(true), "Kulüpler ve öğrenciler" => ClubRegistryPage(), "Ülke ve takvim raporu" => ParticipationReportPage(), "Uygulama dili" => LanguageSettingsPage(), "Genel bakış" => Dashboard(), "Profilim" => MemberProfilePage(), "Gelişmiş profil"=>ProfilePage(), "Yardım ve destek"=>HelpPage(), "Şifrem" => service.IsPresident?PresidentPasswordPage():OwnPasswordPage(), "Çalışma saatlerim" => CoachHoursPage(), "30 günlük program" => PlanPage(), "Antrenmanım" => WorkoutDetailPage(), "Evde antrenman" => HomeWorkoutPage(), "Canlı antrenman" => SessionPlayerPage(), "Egzersiz kütüphanesi" => LibraryPage(), "Koç gelen kutusu" => CoachInboxPage(), "Üye programı" => memberPlanTarget == null ? Ui.Text("Üye seçin.") : PlanPage(memberPlanTarget), "Antrenman kaydı" => WorkoutPage(), "Kilo ve su takibi" => TrackingPage(), "Kalori ve beslenme" => NutritionPage(), "Maç performansı" => MatchPage(), "Üyeler ve takım" => MembersPage(), "Gelişim grafikleri" => ChartsPage(), "Raporlar" => ReportsPage(), _ => Ui.Text("Sayfa bulunamadı.") }; }
        catch (Exception ex) { PageContent.Content = Ui.Card(Ui.Text(Ui.ErrorMessage(ex))); }
    }
    private void UpdateNotificationLabel(){NotificationButton.Content=AppLanguage.Text("Bildirimler")+" · "+service.Notifications().Count(n=>n.ReadAt==null);}
    private void Refresh() {UpdateNotificationLabel();ShowPage(currentPage);}
    private Control Dashboard()
    {
        if (IsCoach) return CoachDashboard();
        var s = Analytics.Summarize(service, Me); var plan = service.Plan(Me); var today = DateOnly.FromDateTime(DateTime.Today); var day = plan?.Days.FirstOrDefault(d => d.Date == today); var water = service.WaterToday(Me);
        var progress = new ProgressBar { Value = s.Percent, Minimum = 0, Maximum = 100, Height = 12, Foreground = Ui.Green };
        string next = day == null ? "Henüz bir programın yok." : $"{day.Title} • {day.Status}";
        var todayCard = Ui.Card(Ui.Stack(Ui.Row(Ui.Text("BUGÜNÜN PLANI", 12, Ui.Muted, true), Ui.Text(today.ToString("dd MMMM, dddd"), 12, Ui.Green)), Ui.Text(next, 22, Ui.Ink, true), Ui.Text(day == null || day.Exercises.Count == 0 ? "Yeni bir program oluşturabilir veya dinlenme gününün keyfini çıkarabilirsin." : string.Join("  /  ", day.Exercises.Select(e => e.Name)), 14, Ui.Muted), Ui.Row(Ui.Button("Antrenman sayfamı aç", () => ShowPage("Antrenmanım"), true), Ui.Button("Antrenman kaydet", () => ShowPage("Antrenman kaydı")))));
        return Ui.Page(Ui.Card(Ui.Text(service.LoginMethod,13,Ui.Green)), AttendanceCard(), todayCard,
            Ui.Stats(Ui.Stat("Güncel kilo", $"{Me.Profile.Weight:0.0} kg", $"Hedef: {Me.Profile.TargetWeight:0.0} kg"), Ui.Stat("Tamamlanan", s.Completed.ToString(), "Bu 30 günlük dönemde"), Ui.Stat("Bugünkü su", $"{water:0.0} L", $"Hedef: {Me.Profile.WaterGoal:0.0} L")),
            WeightGoalCard(Me,true),
            Ui.Card(Ui.Stack(Ui.Text($"30 günlük ilerleme • {s.ProgressDays}/30 gün • %{s.Percent}", 18, Ui.Ink, true), progress)),
            Ui.Card(Ui.Stack(Ui.Text("Son koç notu", 18, Ui.Ink, true), Ui.Text(string.IsNullOrWhiteSpace(Me.CoachNote) ? "Henüz bir not yok." : Me.CoachNote))),
            Ui.Card(Ui.Stack(Ui.Text("Başarıların", 18, Ui.Ink, true), Ui.Text(string.Join("\n\n", Analytics.Badges(service, Me)), 14))));
    }
    private void OpenMemberPlan(User user)
    {
        memberPlanTarget = user; ShowPage("Üye programı");
    }
    private async Task SaveFile(string title, string extension, string text)
    {
        var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions { Title = AppLanguage.Text(title), SuggestedFileName = "FitTeam_" + DateTime.Today.ToString("yyyyMMdd") + "." + extension, DefaultExtension = extension, FileTypeChoices = [new FilePickerFileType(extension switch {"html"=>"Rapor sayfası","csv"=>"Tablo dosyası","txt"=>"Metin dosyası","json"=>"Veri yedeği",_=>"Dosya"}) { Patterns = ["*." + extension] }] });
        if (file == null) return;
        await using var stream = await file.OpenWriteAsync(); stream.SetLength(0); await using var writer = new StreamWriter(stream, new System.Text.UTF8Encoding(true)); await writer.WriteAsync(text);
        await Ui.Message(this, "Dosya kaydedildi.");
    }
}
