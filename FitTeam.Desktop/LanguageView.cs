using System.Globalization;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.LogicalTree;
using Avalonia.Media;
using FitTeam.Core;
namespace FitTeam.Desktop;

internal static class LanguageView
{
    private static readonly ConditionalWeakTable<Control,Action> bound=new();
    private static readonly string path=System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"FitTeam","interface-language.txt");
    public static FlowDirection Direction=>AppLanguage.RightToLeft?FlowDirection.RightToLeft:FlowDirection.LeftToRight;
    public static void Load()
    {
        string code="tr";try {if(System.IO.File.Exists(path))code=System.IO.File.ReadAllText(path).Trim();}catch { }
        AppLanguage.Use(code is "tr" or "ar" or "en"?code:"tr");
    }
    public static void Select(string code)
    {
        AppLanguage.Use(code);if(Application.Current is App app)app.RefreshNativeMenu();System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path)!);System.IO.File.WriteAllText(path,code);
    }
    public static void Text(TextBlock text,bool preserve=false)
    {
        if(bound.TryGetValue(text,out var existing)){existing();return;}
        if(preserve||text.Name=="SidebarName"){bound.Add(text,()=>{});return;}
        bool changing=false;string? source=text.Text;
        void Update(){if(changing)return;changing=true;try{text.Text=AppLanguage.Text(source);text.TextAlignment=AppLanguage.RightToLeft?TextAlignment.Right:TextAlignment.Left;}finally{changing=false;}}
        bound.Add(text,Update);text.PropertyChanged+=(_,e)=>{if(e.Property==TextBlock.TextProperty&&!changing){source=text.Text;Update();}};Update();
    }
    public static void Input(TextBox input)
    {
        if(bound.TryGetValue(input,out var existing)){existing();return;}
        var watermark=input.Watermark;
        void Update()
        {
            input.Watermark=AppLanguage.Text(watermark);
            char first=(input.Text??"").FirstOrDefault(char.IsLetterOrDigit);
            bool arabic=first is >= '\u0600' and <= '\u06ff' or >= '\u0750' and <= '\u077f' or >= '\u08a0' and <= '\u08ff';
            input.FlowDirection=first=='\0'?Direction:arabic?FlowDirection.RightToLeft:FlowDirection.LeftToRight;
            input.TextAlignment=arabic||first=='\0'&&AppLanguage.RightToLeft?TextAlignment.Right:TextAlignment.Left;
        }
        bound.Add(input,Update);input.TextChanged+=(_,_)=>Update();Update();
    }
    public static void Apply(Control root)
    {
        if(root is TextBox input){Input(input);return;}
        if(root is TextBlock text)Text(text);
        if(root is ComboBox combo)combo.PlaceholderText=AppLanguage.Text(combo.PlaceholderText);
        if(root is Button||root is CheckBox||root is Expander)
        {
            if(bound.TryGetValue(root,out var existing))existing();
            else
            {
                bool changing=false;string? source=root is Expander e?e.Header as string:(root as ContentControl)?.Content as string;
                void Update()
                {
                    if(changing||source==null)return;changing=true;
                    try{if(root is Expander exp)exp.Header=AppLanguage.Text(source);else if(root is ContentControl c&&!(root is CheckBox cb&&cb.Tag is Guid))c.Content=AppLanguage.Text(source);}
                    finally{changing=false;}
                }
                bound.Add(root,Update);root.PropertyChanged+=(_,e)=>{if(!changing&&(e.Property==ContentControl.ContentProperty||e.Property==Expander.HeaderProperty)){source=root is Expander exp?exp.Header as string:(root as ContentControl)?.Content as string;Update();}};Update();
            }
        }
        foreach(var child in root.GetLogicalChildren().OfType<Control>())Apply(child);
    }
    public static void Refresh(Control root)=>Apply(root);
    public sealed class CellText(string property,string rowType):IValueConverter
    {
        public object? Convert(object? value,Type targetType,object? parameter,CultureInfo culture)
        {
            if(value is not string s)return value;
            if(property is "Contact" or "Username" or "Email" or "Phone" or "Opponent" or "Note" or "Team"||property=="Name"&&rowType!="LibraryRow")return s;
            return AppLanguage.Text(s);
        }
        public object ConvertBack(object? value,Type targetType,object? parameter,CultureInfo culture)=>throw new NotSupportedException();
    }
}
