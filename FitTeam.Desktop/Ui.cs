using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Layout;
using Avalonia.Media;
using FitTeam.Core;
namespace FitTeam.Desktop;

internal static class Ui
{
    public static readonly IBrush Ink = Brush.Parse("#161B20");
    public static readonly IBrush Muted = Brush.Parse("#747C84");
    public static readonly IBrush Green = Brush.Parse("#698900");
    public static TextBlock Text(string text, double size = 14, IBrush? color = null, bool bold = false)
    {
        var block=new TextBlock {Text=text,FontSize=size,Foreground=color??Ink,FontWeight=bold?FontWeight.SemiBold:FontWeight.Normal,TextWrapping=TextWrapping.Wrap};LanguageView.Text(block);return block;
    }
    public static TextBlock UserText(string text,double size=14,IBrush? color=null,bool bold=false)
    {
        var block=new TextBlock {Text=text,FontSize=size,Foreground=color??Ink,FontWeight=bold?FontWeight.SemiBold:FontWeight.Normal,TextWrapping=TextWrapping.Wrap};LanguageView.Text(block,true);return block;
    }
    public static StackPanel Stack(params Control[] controls) { var panel = new StackPanel { Spacing = 12 }; foreach (var c in controls) {LanguageView.Apply(c);panel.Children.Add(c);} return panel; }
    public static WrapPanel Row(params Control[] controls) { var panel = new WrapPanel { Orientation = Orientation.Horizontal }; foreach (var c in controls) { LanguageView.Apply(c); c.Margin = new Thickness(0, 0, 12, 10); panel.Children.Add(c); } return panel; }
    public static Border Card(Control content) => new() { Background = Brushes.White, BorderBrush = Brush.Parse("#E7EBEF"), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(22), Padding = new Thickness(22), Child = content };
    public static Border Stat(string label, string value, string hint = "") => Card(Stack(Text(label, 13, Muted), Text(value, 29, Ink, true), Text(hint, 12, Muted)));
    public static UniformGrid Stats(params Control[] controls) { var grid = new UniformGrid { Columns = controls.Length, Margin = new Thickness(0, 0, 0, 12) }; foreach (var c in controls) { LanguageView.Apply(c); c.Margin = new Thickness(0, 0, 12, 0); grid.Children.Add(c); } return grid; }
    public static Button Button(string label, Action click, bool primary = false) { var b = new Button { Content = label }; if (primary) b.Classes.Add("primary"); b.Click += (_, _) => click(); LanguageView.Apply(b); return b; }
    public static Button AsyncButton(string label, Func<Task> click, bool primary = false) { var b = new Button { Content = label }; if (primary) b.Classes.Add("primary"); b.Click += async (_, _) => await click(); LanguageView.Apply(b); return b; }
    public static TextBox Input(string value = "", string watermark = "") {var input=new TextBox {Text=value,Watermark=watermark,HorizontalAlignment=HorizontalAlignment.Stretch};LanguageView.Input(input);return input;}
    public static NumericUpDown Number(decimal value, decimal min, decimal max, int decimals = 0) => new() { Value = Math.Clamp(value, min, max), Minimum = min, Maximum = max, Increment = decimals == 0 ? 1 : 0.1m, FormatString = decimals == 0 ? "0" : "0." + new string('0', decimals), HorizontalAlignment = HorizontalAlignment.Stretch };
    public static decimal Value(NumericUpDown n) => n.Value ?? 0;
    public static int Int(NumericUpDown n) => (int)Value(n);
    public static ComboBox Combo(IEnumerable<string> items, string? selected = null) { var list = items.ToList(); return new ComboBox { ItemsSource = list, SelectedItem = selected ?? list.FirstOrDefault(),ItemTemplate=new Avalonia.Controls.Templates.FuncDataTemplate<string>((item,_)=>Text(item??"")) }; }
    public static string Selected(ComboBox combo) => combo.SelectedItem?.ToString() ?? "";
    public static StackPanel Field(string label, Control control, double width = 210) => new() { Width = width, Spacing = 6, Margin = new Thickness(0, 0, 16, 16), Children = { Text(label, 13, Muted), control } };
    public static ScrollViewer Page(params Control[] controls) => new() { Content = new StackPanel { Spacing = 18, Children = { Stack(controls) } }, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled };
    public static CalendarDatePicker Date(DateOnly value, bool pastOnly = false) => new() { SelectedDate = value.ToDateTime(TimeOnly.MinValue), DisplayDateEnd = pastOnly ? DateTime.Today : null, HorizontalAlignment = HorizontalAlignment.Stretch };
    public static DateOnly DateValue(CalendarDatePicker date) => DateOnly.FromDateTime(date.SelectedDate ?? throw new ArgumentException("Tarih seçin."));
    public static DataGrid Table<T>(IEnumerable<T> rows, params (string Header, string Property)[] columns)
    {
        var grid = new DataGrid { ItemsSource = rows.ToList(), AutoGenerateColumns = false, IsReadOnly = true, SelectionMode = DataGridSelectionMode.Single, Height = 320, HorizontalAlignment = HorizontalAlignment.Stretch };
        foreach (var c in columns) grid.Columns.Add(new DataGridTextColumn { Header = AppLanguage.Text(c.Header), Binding = new Binding(c.Property) {Converter=new LanguageView.CellText(c.Property,typeof(T).Name)}, Width = new DataGridLength(1, DataGridLengthUnitType.Star) });
        return grid;
    }
    public static Window Dialog(string title,Control content,double width=620,double height=680)
    {
        var window=new Window {Title=AppLanguage.Text(title),FlowDirection=LanguageView.Direction,Width=width,Height=height,MinWidth=Math.Min(width,500),MinHeight=400,WindowStartupLocation=WindowStartupLocation.CenterOwner,Background=Brush.Parse("#F3F6FA"),Content=new Border {Padding=new Thickness(24),Child=new ScrollViewer {Content=content}}};
        window.Opened+=(_,_)=>LanguageView.Apply(window);return window;
    }
    public static string ErrorMessage(Exception error)=>error switch
    {
        HttpRequestException=>"Bağlantı kurulamadı. İnternet bağlantını kontrol edip tekrar dene.",
        OperationCanceledException or TimeoutException=>"İşlem zamanında tamamlanamadı. Tekrar deneyebilirsin.",
        System.Text.Json.JsonException=>"Veriler beklenen biçimde okunamadı. Bağlantıyı veya veri dosyasını kontrol et.",
        UnauthorizedAccessException=>"Bu işlem için erişim izni yok. Dosya ve hesap izinlerini kontrol et.",
        System.IO.IOException=>"Dosya işlemi tamamlanamadı. Dosyanın konumunu ve erişim iznini kontrol et.",
        KeyNotFoundException=>"İşlem için beklenen bilgi bulunamadı. Hesabını ve bağlantı ayarlarını kontrol et.",
        FormatException=>"Girilen bilgi beklenen biçimde değil. Alanları kontrol edip tekrar dene.",
        _ when error.Source?.StartsWith("FitTeam",StringComparison.Ordinal)==true=>error.Message,
        _=>"İşlem tamamlanamadı. Bilgileri kontrol edip tekrar dene."
    };
    public static Window ErrorWindow(string title, string message) => Dialog(title, Stack(Text(title, 24, Ink, true), Text(message)), 650, 400);
    public static async Task Message(Window owner, string message)
    {
        var window = Dialog("FitTeam", new StackPanel(), 500, 270);
        window.Content = new Border { Padding = new Thickness(24), Child = Stack(Text(message, 15), Button("Tamam", () => window.Close(), true)) };
        await window.ShowDialog(owner);
    }
    public static async Task<bool> Confirm(Window owner, string message)
    {
        bool confirmed = false; var window = Dialog("Onay", new StackPanel(), 520, 270);
        window.Content = new Border { Padding = new Thickness(24), Child = Stack(Text(message, 15), Row(Button("Vazgeç", () => window.Close()), Button("Devam et", () => { confirmed = true; window.Close(); }, true))) };
        await window.ShowDialog(owner); return confirmed;
    }
}
