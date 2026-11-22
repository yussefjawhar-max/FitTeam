using Avalonia;
using System.Globalization;
namespace FitTeam.Desktop;
internal static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        LanguageView.Load();
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }
    public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<App>().UsePlatformDetect().With(new MacOSPlatformOptions {DisableDefaultApplicationMenuItems=true}).LogToTrace();
}
