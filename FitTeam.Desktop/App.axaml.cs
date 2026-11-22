using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using FitTeam.Core;
using FitTeam.Desktop.Views;
namespace FitTeam.Desktop;
public partial class App : Application
{
    public override void Initialize() {AvaloniaXamlLoader.Load(this);RefreshNativeMenu();}
    internal void RefreshNativeMenu()
    {
        if(!OperatingSystem.IsMacOS())return;
        var about=new Avalonia.Controls.NativeMenuItem{Header=FitTeam.Core.AppLanguage.Text("FitTeam hakkında")};
        about.Click+=async(_,_)=>{if(ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop&&desktop.MainWindow!=null)await Ui.Message(desktop.MainWindow,"FitTeam • Masaüstü antrenman ve gelişim takibi");};
        var quit=new Avalonia.Controls.NativeMenuItem{Header=FitTeam.Core.AppLanguage.Text("FitTeam’den çık"),Gesture=new Avalonia.Input.KeyGesture(Avalonia.Input.Key.Q,Avalonia.Input.KeyModifiers.Meta)};
        quit.Click+=(_,_)=>{if(ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)desktop.Shutdown();};
        var menu=new Avalonia.Controls.NativeMenu();menu.Items.Add(about);menu.Items.Add(new Avalonia.Controls.NativeMenuItemSeparator());menu.Items.Add(quit);Avalonia.Controls.NativeMenu.SetMenu(this,menu);
    }
    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var loading = Ui.Dialog("FitTeam • Açılıyor", Ui.Stack(
                Ui.Text("FitTeam hazırlanıyor", 25, Ui.Ink, true),
                Ui.Text("MySQL bağlantısı kuruluyor. İlk açılışta veritabanının hazırlanması biraz sürebilir.", 15, Ui.Muted),
                new Avalonia.Controls.ProgressBar { IsIndeterminate = true, Height = 8 }), 580, 310);
            desktop.MainWindow = loading;
            loading.Opened += async (_, _) =>
            {
                try
                {
                    var model = await Task.Run(CourseRuntime.Start);
                    if (!loading.IsVisible) return;
                    var main = new MainWindow(model); desktop.MainWindow = main; main.Show(); loading.Close();
                }
                catch (Exception error)
                {
                    if (!loading.IsVisible) return;
                    var failure = Ui.ErrorWindow("Veritabanı açılamadı", Ui.ErrorMessage(error));
                    desktop.MainWindow = failure; failure.Show(); loading.Close();
                }
            };
        }
        RefreshNativeMenu();
        base.OnFrameworkInitializationCompleted();
    }
}
