using Avalonia.Controls;
using Avalonia.Data;
using FitTeam.Core;
using FitTeam.Persistence;

namespace FitTeam.Desktop.Views;

public partial class MainWindow
{
    private async Task<bool> VerifyEmailDialog(Window owner, string address)
    {
        bool verified = false;
        string requestId = "";
        var code = Ui.Input("", "6 haneli kod"); code.MaxLength = 6;
        var status = Ui.Text("Kodun e-posta adresine gönderilir. Gelen kutunu ve spam klasörünü kontrol et.", 14, Ui.Muted);
        var panel = Ui.Stack(Ui.Text("E-posta adresini doğrula", 24, Ui.Ink, true), Ui.UserText(address, 16, Ui.Ink, true), status);
        var dialog = Ui.Dialog("FitTeam • E-posta doğrulama", panel, 560, 510);
        panel.Children.Add(Ui.AsyncButton("Kod gönder", async () =>
        {
            try
            {
                status.Text = "Kod gönderiliyor…";
                requestId = await service.Email.SendCode(address);
                status.Text = "Kod e-posta sunucusuna gönderildi. Gelen kutunu veya spam klasörünü kontrol et. Kod 2 dakika geçerli.";
                code.Focus();
            }
            catch (Exception error) { status.Text = Ui.ErrorMessage(error); }
        }, true));
        panel.Children.Add(Ui.Field("Doğrulama kodu", code, 430));
        panel.Children.Add(Ui.Button("Kodu doğrula", () =>
        {
            try
            {
                if (requestId.Length == 0) throw new ArgumentException("Önce Kod gönder düğmesine basın.");
                service.Email.VerifyCode(address, requestId, code.Text ?? "");
                verified = true; code.Text = ""; dialog.Close();
            }
            catch (Exception error) { status.Text = Ui.ErrorMessage(error); }
        }, true));
        panel.Children.Add(Ui.Button("Vazgeç", () => dialog.Close()));
        await dialog.ShowDialog(owner);
        return verified;
    }

    private Control EmailSettingsPage()
    {
        if (!service.HasAdministrationAccess || service.IsPreview)
            return Ui.Page(Ui.Card(Ui.Text("E-posta ayarları için asıl uygulamada yetkili hesabıyla giriş yapın.")));
        if (CourseRuntime.EmailSettings == null) return Ui.Page(Ui.Card(Ui.Text("E-posta ayar deposu açılamadı.")));
        var current = CourseRuntime.EmailSettings.Load();
        var host = Ui.Input(current?.Host ?? "smtp.gmail.com");
        var port = Ui.Number(current?.Port ?? 587, 1, 65535, 1);
        var sender = Ui.Input(current?.Sender ?? Me.Email);
        var username = Ui.Input(current?.Username ?? Me.Email);
        var password = Ui.Input(); password.PasswordChar = '●';
        var recipient = Ui.Input(Me.Email);
        var status = Ui.Text(service.Email.IsConfigured ? "E-posta hizmeti kurulmuş. Gerçek gönderimi test edebilirsin." : "E-posta hizmeti henüz kurulmamış.", 14, Ui.Muted);
        var history = new StackPanel { Spacing = 8 };
        void RenderHistory()
        {
            history.Children.Clear();
            foreach (var delivery in service.Email.DeliveryHistory().Take(12))
                history.Children.Add(Ui.Card(Ui.Stack(Ui.UserText(delivery.Recipient, 15, Ui.Ink, true), Ui.UserText(delivery.Subject),
                    Ui.Text(delivery.Status + " • " + delivery.CreatedAt.ToLocalTime().ToString("dd.MM.yyyy HH:mm"), 13, delivery.SentAt == null ? Ui.Muted : Ui.Green),
                    Ui.UserText(delivery.ErrorMessage.Length > 0 ? delivery.ErrorMessage : delivery.MessageId, 12, Ui.Muted))));
        }
        var panel = Ui.Stack(Ui.Text("E-posta bağlantısı", 25, Ui.Ink, true),
            Ui.Text("Kayıt doğrulama kodlarını bu gönderici hesabından yollarız. Gmail için normal hesap şifresi yerine uygulama parolası kullanılır.", 14, Ui.Muted),
            Ui.Field("SMTP sunucusu", host, 500), Ui.Field("STARTTLS portu", port, 500), Ui.Field("Gönderen e-posta adresi", sender, 500),
            Ui.Field("SMTP kullanıcı adı", username, 500), Ui.Field("Uygulama / SMTP parolası", password, 500),
            Ui.Text(current == null ? "Parolan yalnız bu bilgisayarda korumalı olarak saklanır." : "Parola alanını boş bırakırsan kayıtlı parola korunur.", 13, Ui.Muted), status);
        panel.Children.Add(Ui.Button("Ayarları güvenli kaydet", () =>
        {
            try
            {
                var settings = new SmtpSettings { Host = host.Text ?? "", Port = (int)Ui.Value(port), Sender = sender.Text ?? "", Username = username.Text ?? "", Password = password.Text?.Length > 0 ? password.Text : current?.Password ?? "", EnableTls = true };
                CourseRuntime.EmailSettings.Save(settings); current = settings; password.Text = "";
                status.Text = "E-posta ayarları kaydedildi. Aşağıdan gerçek bir test mesajı gönderebilirsin.";
            }
            catch (Exception error) { status.Text = Ui.ErrorMessage(error); }
        }, true));
        panel.Children.Add(Ui.Field("Test mesajının gideceği e-posta", recipient, 500));
        panel.Children.Add(Ui.AsyncButton("Gerçek test e-postası gönder", async () =>
        {
            try
            {
                status.Text = "Test e-postası gönderiliyor…";
                var receipt = await service.Email.SendTest(recipient.Text ?? "");
                status.Text = "SMTP sunucusu mesajı kabul etti. Gelen kutunu kontrol ederek teslimi doğrula. Gönderim: " + receipt.SentAt?.ToLocalTime().ToString("dd.MM.yyyy HH:mm");
            }
            catch (Exception error) { status.Text = Ui.ErrorMessage(error); }
            finally { RenderHistory(); }
        }, true));
        panel.Children.Add(Ui.Text("Gönderim geçmişi", 22, Ui.Ink, true)); panel.Children.Add(history); RenderHistory();
        return Ui.Page(Ui.Card(panel));
    }

    private Control DatabaseTablesPage()
    {
        var databaseSettings = CourseRuntime.DatabaseSettings;
        if (!service.HasAdministrationAccess || service.IsPreview || databaseSettings == null)
            return Ui.Page(Ui.Card(Ui.Text("MySQL tablolarını görmek için asıl uygulamada yetkili hesabıyla giriş yapın.")));
        var inspector = new DatabaseInspector(databaseSettings);
        var tables = inspector.Tables();
        var choice = Ui.Combo(tables.Select(t => t.Name));
        var summary = Ui.Text("", 14, Ui.Muted);
        var columns = new DataGrid { IsReadOnly = true, AutoGenerateColumns = true, Height = 230 };
        var records = new DataGrid { IsReadOnly = true, AutoGenerateColumns = false, Height = 360 };
        void Render()
        {
            string selected = Ui.Selected(choice);
            var table = tables.Single(t => t.Name == selected);
            summary.Text = "MySQL • " + databaseSettings.Database + " • " + table.Name + " • " + table.Rows + " kayıt";
            columns.ItemsSource = inspector.Columns(selected);
            var sample = inspector.Sample(selected); records.Columns.Clear();
            foreach (var name in sample.Columns) records.Columns.Add(new DataGridTextColumn { Header = name, Binding = new Binding("[" + name + "]") });
            records.ItemsSource = sample.Rows;
        }
        choice.SelectionChanged += (_, _) => { try { Render(); } catch (Exception error) { summary.Text = Ui.ErrorMessage(error); } };
        Render();
        return Ui.Page(Ui.Card(Ui.Stack(Ui.Text("MySQL veritabanı", 25, Ui.Ink, true), Ui.Text("Canlı tablo yapısı ve ilk 50 kayıt gösterilir. Parola değerleri bu ekranda görüntülenmez.", 14, Ui.Muted),
            Ui.Field("Tablo", choice, 520), summary, Ui.Text("Tablo yapısı", 20, Ui.Ink, true), columns, Ui.Text("Kayıtlar", 20, Ui.Ink, true), records)));
    }
}
