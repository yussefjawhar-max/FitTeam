using Avalonia.Controls;
using FitTeam.Core;
namespace FitTeam.Desktop.Views;

public partial class MainWindow
{
    private Control StudentLookupCard()
    {
        if (!service.HasAdministrationAccess) throw new InvalidOperationException("Yetkili hesabıyla giriş yap.");
        var search = Ui.Input("", "Öğrencinin adını, soyadını veya kullanıcı adını yaz");
        var status = Ui.Text("Adını yazdıkça öğrenciler burada görünür.", 13, Ui.Muted);
        var results = new StackPanel { Spacing = 8 };
        var scroll = new ScrollViewer { Content = results, MaxHeight = 340, IsVisible = false };
        void Filter()
        {
            results.Children.Clear();
            var students = service.Data.Users.Where(u => u.Id != Me.Id && !u.Email.Equals("jawharyusuf446@gmail.com",StringComparison.OrdinalIgnoreCase) && !u.IsClubAccount && service.ShowRecord(u)
                && (u.Role == Choices.Roles[0] || u.Role == Choices.Roles[1]));
            var matches = StudentSearch.Find(students, search.Text);
            scroll.IsVisible = matches.Count > 0;
            status.Text = string.IsNullOrWhiteSpace(search.Text) ? "Adını yazdıkça öğrenciler burada görünür."
                : matches.Count == 0 ? "Bu adla eşleşen öğrenci bulunamadı."
                : matches.Count > 50 ? $"{matches.Count} öğrenci bulundu. İlk 50 gösteriliyor; soyadını da yazarak daraltabilirsin."
                : $"{matches.Count} öğrenci bulundu. Bilgilerini açmak için adına bas.";
            foreach (var student in matches.Take(50))
            {
                var club = service.Data.Teams.FirstOrDefault(t => t.Id == student.TeamId)?.Name;
                var caption = Ui.Stack(Ui.UserText(student.Profile.Name, 17, Ui.Ink, true),
                    Ui.UserText(student.Username + " • " + (club ?? AppLanguage.Text("Bireysel üye")), 13, Ui.Muted));
                if (!student.IsActive || student.NeedsApproval)
                    caption.Children.Add(Ui.Text(student.IsActive ? "Onay bekliyor" : "Çıkarıldı", 12, Ui.Muted));
                var button = new Button { Content = caption, HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch,
                    HorizontalContentAlignment = Avalonia.Layout.HorizontalAlignment.Stretch };
                button.Click += async (_, _) => await Guard(() => StudentDetailsDialog(student));
                results.Children.Add(button);
            }
            LanguageView.Apply(results);
        }
        search.TextChanged += (_, _) => Filter();
        return Ui.Card(Ui.Stack(Ui.Text("Öğrenci ara", 21, Ui.Ink, true),
            Ui.Row(Ui.Field("Ad veya soyad", search, 460), Ui.Button("Aramayı temizle", () => { search.Text = ""; search.Focus(); })), status, scroll));
    }
}
