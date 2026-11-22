using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
namespace FitTeam.Desktop.Controls;

public sealed class TrendChart : Control
{
    public List<(string Label, double Value)> Points { get; set; } = [];
    public string Unit { get; set; } = "";
    public TrendChart() { Height = 280; MinWidth = 350; }
    public override void Render(DrawingContext context)
    {
        base.Render(context); double width = Bounds.Width; double height = Bounds.Height;
        const double left = 62, top = 20; double right = width - 25, bottom = height - 42;
        void Text(string value, double x, double y, IBrush? brush = null) => context.DrawText(new FormattedText(value, CultureInfo.GetCultureInfo("tr-TR"), FlowDirection.LeftToRight, Typeface.Default, 12, brush ?? Ui.Muted), new Point(x, y));
        if (Points.Count == 0) { Text("Henüz kayıt yok. Kayıtlar eklendikçe grafik oluşur.", left, height / 2); return; }
        double min = Math.Floor(Points.Min(p => p.Value)); double max = Math.Ceiling(Points.Max(p => p.Value));
        if (max <= min) { max = min + 1; min = Math.Max(0, min - 1); }
        double pad = (max - min) * 0.1; min = Math.Max(0, min - pad); max += pad;
        var gridPen = new Pen(Brush.Parse("#E7EDF3"), 1); var linePen = new Pen(Ui.Green, 3);
        for (int i = 0; i <= 4; i++) { double y = top + (bottom - top) * i / 4; context.DrawLine(gridPen, new Point(left, y), new Point(right, y)); Text((max - (max - min) * i / 4).ToString("0.#"), 4, y - 7); }
        Text(Unit, 4, 0, Ui.Green);
        Point Coord(int i) => new(left + (right - left) * (Points.Count == 1 ? 0.5 : i / (double)(Points.Count - 1)), bottom - (Points[i].Value - min) / (max - min) * (bottom - top));
        for (int i = 0; i < Points.Count; i++)
        {
            var point = Coord(i); if (i > 0) context.DrawLine(linePen, Coord(i - 1), point);
            context.DrawEllipse(Ui.Green, null, point, 4, 4);
            int step = Math.Max(1, (int)Math.Ceiling(Points.Count / 7d));
            if (i % step == 0 || i == Points.Count - 1) Text(Points[i].Label, point.X - 17, bottom + 15);
        }
    }
}
