using System.Diagnostics;
namespace FitTeam.Desktop;
internal static class QrImages
{
    private static async Task<string> Run(params string[] args)
    {
        var path=Path.Combine(AppContext.BaseDirectory,"FitTeam.Qr.app","Contents","MacOS","FitTeam.Qr");
        if(!OperatingSystem.IsMacOS()||!File.Exists(path))throw new InvalidOperationException("QR görseli okuma bu Mac paketinde bulunamadı. 6 haneli kodu elle girebilirsin.");
        var start=new ProcessStartInfo(path){UseShellExecute=false,RedirectStandardOutput=true,RedirectStandardError=true};foreach(var a in args)start.ArgumentList.Add(a);
        using var process=Process.Start(start)??throw new InvalidOperationException("QR görseli açılamadı.");var output=process.StandardOutput.ReadToEndAsync();await process.WaitForExitAsync();var text=(await output).Trim();if(process.ExitCode!=0||text.Length==0)throw new ArgumentException("Görselde okunabilir bir FitTeam salon QR kodu bulunamadı.");return text;
    }
    public static async Task<string?> Scan(){var result=await Run("camera");if(result=="CANCELLED")return null;if(result=="CAMERA_DENIED")throw new ArgumentException("Kamera açılamadı. Kamera iznini kontrol et veya kodu elle gir.");return result;}
    public static Task<string> Read(string path)=>Run("read",path);
    public static async Task Create(string code,string path)=>_ = await Run("create","FITTEAM:"+code,path);
}
