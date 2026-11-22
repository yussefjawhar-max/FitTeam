using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using FitTeam.Core;
namespace FitTeam.Desktop.Views;

public partial class MainWindow
{
    private static Control WelcomeEntryBoxes(params (string Title,string Hint,Func<Task> Action)[] entries)
    {
        var grid=new Grid {ColumnDefinitions=new ColumnDefinitions("*,*"),RowDefinitions=new RowDefinitions(string.Join(",",Enumerable.Repeat("Auto",(entries.Length+1)/2))),ColumnSpacing=12,RowSpacing=12};
        for(int index=0;index<entries.Length;index++)
        {
            var entry=entries[index];bool main=entry.Title=="Normal giriş",wide=entries.Length%2==1&&index==entries.Length-1;
            var icon=entry.Title switch {
                "Yetkili girişi"=>"M12 2L21 6V12C21 17 17 21 12 23C7 21 3 17 3 12V6Z M8 12L11 15L16 9",
                "Yeni üye kaydı"=>"M14 7A4 4 0 1 1 6 7A4 4 0 1 1 14 7 M2 22V19C2 15 5 13 10 13C13 13 15 14 16 16 M20 10V18 M16 14H24",
                "Antrenörler"=>"M2 8V16 M6 5V19 M6 12H18 M18 5V19 M22 8V16",
                "Kulüp girişi"=>"M2 21V8L12 2L22 8V21Z M8 21V13H16V21 M7 8H7.1 M12 8H12.1 M17 8H17.1",
                _=>"M14 3H21V21H14 M3 12H16 M10 6L16 12L10 18"
            };
            var button=Ui.AsyncButton(entry.Title,entry.Action);
            button.Classes.Add(main?"welcome-main":"welcome-card");
            Avalonia.Automation.AutomationProperties.SetName(button,AppLanguage.Text(entry.Title));
            button.HorizontalAlignment=HorizontalAlignment.Stretch;button.HorizontalContentAlignment=HorizontalAlignment.Stretch;
            button.Padding=new Thickness(18);button.MinHeight=wide?98:142;button.CornerRadius=new CornerRadius(18);
            button.Background=main?Ui.Ink:Brushes.White;button.BorderBrush=Brush.Parse(main?"#161B20":"#DFE4D8");button.BorderThickness=new Thickness(1);
            var symbol=new Border {Width=36,Height=36,CornerRadius=new CornerRadius(11),Background=Brush.Parse("#D7FF3C"),Padding=new Thickness(8),Child=WelcomeIcon(icon),HorizontalAlignment=HorizontalAlignment.Left};
            var arrow=WelcomeIcon(LanguageView.Direction==FlowDirection.RightToLeft?"M20 12H4 M10 6L4 12L10 18":"M4 12H20 M14 6L20 12L14 18",main?Brush.Parse("#D7FF3C"):Brush.Parse("#7B8373"));
            var heading=Ui.Text(entry.Title,18,main?Brush.Parse("#D7FF3C"):Ui.Ink,true);
            var hint=Ui.Text(entry.Hint,12,main?Brush.Parse("#C2C7BE"):Brush.Parse("#697364"));
            if(wide)
            {
                var content=new Grid {ColumnDefinitions=new ColumnDefinitions("Auto,*,Auto"),ColumnSpacing=14};
                symbol.VerticalAlignment=VerticalAlignment.Center;content.Children.Add(symbol);
                var text=Ui.Stack(heading,hint);text.Spacing=5;Grid.SetColumn(text,1);content.Children.Add(text);Grid.SetColumn(arrow,2);content.Children.Add(arrow);button.Content=content;
            }
            else
            {
                var top=new Grid {ColumnDefinitions=new ColumnDefinitions("*,Auto")};top.Children.Add(symbol);Grid.SetColumn(arrow,1);top.Children.Add(arrow);
                var content=Ui.Stack(top,heading,hint);content.Spacing=7;button.Content=content;
            }
            Grid.SetRow(button,index/2);Grid.SetColumn(button,index%2);if(wide)Grid.SetColumnSpan(button,2);grid.Children.Add(button);
        }
        return grid;
    }
    private static Control WelcomeIcon(string geometry,IBrush? stroke=null)
    {
        var canvas=new Canvas {Width=24,Height=24};canvas.Children.Add(new Avalonia.Controls.Shapes.Path {Data=Geometry.Parse(geometry),Stroke=stroke??Ui.Ink,StrokeThickness=1.8,StrokeLineCap=PenLineCap.Round,StrokeJoin=PenLineJoin.Round});
        return new Viewbox {Width=22,Height=22,Stretch=Stretch.Uniform,Child=canvas,VerticalAlignment=VerticalAlignment.Center};
    }
}
