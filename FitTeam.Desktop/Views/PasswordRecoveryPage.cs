namespace FitTeam.Desktop.Views;

public partial class MainWindow
{
    private async Task PasswordRecoveryDialog(string identifier="",string? requiredRole=null,string? requiredSport=null)
    {
        var panel=Ui.Stack(Ui.Text("Şifre yardımı",25,Ui.Ink,true),
            Ui.Text("Üye veya antrenör şifreni unuttuysan uygulamanın yetkilisine başvur. Yetkili, hesabını kontrol ettikten sonra sana yeni bir geçici FitTeam şifresi verebilir. Giriş yaptıktan sonra Şifrem sayfasından kendi şifreni belirle.",14,Ui.Muted),
            Ui.Text("Başkan parolası bu ekrandan sıfırlanamaz. Başkan, mevcut parolasıyla giriş yaptıktan sonra kendi şifresini değiştirebilir.",14,Ui.Muted));
        var dialog=Ui.Dialog("FitTeam • Şifre yardımı",panel,570,420);
        panel.Children.Add(Ui.Button("Tamam",()=>dialog.Close(),true));
        await dialog.ShowDialog(this);
    }
}
