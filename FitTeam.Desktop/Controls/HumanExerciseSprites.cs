using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using System.Text.Json;
namespace FitTeam.Desktop.Controls;
internal static class HumanExerciseSprites
{
    private sealed class Strip {public string Asset {get;set;}="";public double[][] Frames {get;set;}=[];}
    private static readonly Dictionary<string,Strip> strips=Load();
    private static readonly Dictionary<string,Bitmap> images=[];
    private static Dictionary<string,Strip> Load()
    {
        try{using var stream=AssetLoader.Open(new Uri("avares://FitTeam/Assets/trainer-motion.json"));return JsonSerializer.Deserialize<Dictionary<string,Strip>>(stream,new JsonSerializerOptions{PropertyNameCaseInsensitive=true})??[];}catch{return [];}
    }
    public static bool Draw(DrawingContext context,Rect bounds,string movement,double seconds)
    {
        if(!strips.TryGetValue(movement,out var strip)||strip.Frames.Length!=3)return false;
        try
        {
            if(!images.TryGetValue(strip.Asset,out var image)){using var stream=AssetLoader.Open(new Uri("avares://FitTeam/Assets/"+strip.Asset));images[strip.Asset]=image=new Bitmap(stream);}
            int step=(int)(Math.Max(0,seconds)/0.75)%4,index=step==3?1:step;var f=strip.Frames[index];
            var source=new Rect(f[0]*image.Size.Width/image.PixelSize.Width,f[1]*image.Size.Height/image.PixelSize.Height,f[2]*image.Size.Width/image.PixelSize.Width,f[3]*image.Size.Height/image.PixelSize.Height);
            double scale=Math.Min((bounds.Width-28)/source.Width,(bounds.Height-20)/source.Height);
            var dest=new Rect((bounds.Width-source.Width*scale)/2,(bounds.Height-source.Height*scale)/2,source.Width*scale,source.Height*scale);
            context.DrawImage(image,source,dest);return true;
        }
        catch{return false;}
    }
}
