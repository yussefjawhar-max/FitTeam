using FitTeam.Core;

namespace FitTeam.Controllers;

/// <summary>Ortak controller davranışı. Yetki ve doğrulama kuralları Model katmanında da uygulanır.</summary>
public abstract class ControllerBase
{
    protected FitService Model { get; }
    protected ControllerBase(FitService model) => Model = model ?? throw new ArgumentNullException(nameof(model));
    protected User RequireUser() => Model.RequireCurrent();
    protected void RequireAdministration()
    {
        _ = RequireUser();
        if (!Model.HasAdministrationAccess) throw new InvalidOperationException("Bu işlem için yönetici yetkisi gerekiyor.");
    }
}

public sealed class AccountController(FitService model, EmailVerificationController email) : ControllerBase(model)
{
    public bool Login(string username, string password)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            throw new ArgumentException("Kullanıcı adı ve şifre alanlarını doldurun.");
        return Model.Login(username.Trim(), password);
    }
    public void Logout() => Model.Logout();
    public User Register(SignupApplication application)
    {
        application.Validate();
        if (application.Email.Length > 0 && !email.IsVerified(application.Email))
            throw new ArgumentException("Kaydolmadan önce e-posta adresinize gönderilen kodu doğrulayın.");
        var user = Model.RegisterLocalSignup(application, application.Email.Length > 0);
        if (application.Email.Length > 0) email.ConsumeVerification(application.Email);
        return user;
    }
}

/// <summary>Masaüstü View olaylarını Model servislerine yönlendiren ana MVC controller.</summary>
public sealed partial class FitTeamController
{
    private readonly FitService model;
    public AccountController Accounts { get; }
    public EmailVerificationController Email { get; }
    public FitTeamController(FitService model, IEmailSender? sender = null, IEmailDeliveryStore? mailAudit = null)
    {
        this.model = model ?? throw new ArgumentNullException(nameof(model));
        Email = new EmailVerificationController(model, sender ?? new UnconfiguredEmailSender(), mailAudit);
        Accounts = new AccountController(model, Email);
    }
    // Salt okunur analiz araçları mevcut Model nesnesiyle çalışır.
    public static implicit operator FitService(FitTeamController controller) => controller.model;
}
