using FitTeam.Core;
using System.Net;
using System.Text;
using System.Text.Json;
static class DeputyChecks
{
    public static async Task Run(Action<bool,string> check)
    {
        var path=Path.Combine(Path.GetTempPath(),"FitTeam_deputy_"+Guid.NewGuid().ToString("N")+".json");
        try
        {
            var service=new FitService(new DataStore(path));
            var client=new CodeAuthClient(new Uri("http://127.0.0.1:12345/"),new HttpClient(new OwnerFixture()));
            service.LoginCode(await client.Verify("","email","test","123456"));service.Data.OwnerLoginUsername="rootowner";
            bool reserved=false;try{service.AdminAddUser("ROOTOWNER","Yeni Kişi",Choices.Roles[0],"","","MemberOwn-82!");}catch(ArgumentException){reserved=true;}check(reserved,"Asıl yetkilinin kullanıcı adı yeni hesap tarafından alınamaz");
            var deputy=service.AdminCreateDeputy("Yardımcı Adı","yardimci","DeputyOwn-82!");
            check(deputy.Role==Choices.DeputyRole&&service.Login("yardimci","DeputyOwn-82!")&&service.HasAdministrationAccess&&!service.IsPresident,"Yardımcı kendi kullanıcı adı ve şifresiyle yönetici erişimi alır; asıl başkan olarak işaretlenmez");
            var added=service.AdminAddUser("member82","Yeni Üye",Choices.Roles[0],"","","MemberOwn-82!");
            check(added.IsActive&&service.Data.Users.Any(u=>u.Id==added.Id),"Yardımcı normal üye yönetimi yapabilir");
            bool rejected=false;try{service.AdminCreateDeputy("Başka Yardımcı","otherhelper","OtherOwn-82!");}catch(InvalidOperationException){rejected=true;}
            check(rejected,"Yardımcı kendisi başka yardımcı yetkisi veremez");
            rejected=false;try{service.ValidateOwnerUsername("member82");}catch(InvalidOperationException){rejected=true;}check(rejected,"Yardımcı asıl yetkilinin giriş adını değiştiremez");
            service.LoginCode(await client.Verify("","email","test","123456"));rejected=false;try{service.ValidateOwnerUsername("yardimci");}catch(ArgumentException){rejected=true;}check(rejected,"Başkan kullanıcı adı yardımcı hesabıyla çakışamaz");service.Login("yardimci","DeputyOwn-82!");
            service.EnsureAccess(added);check(service.Weights(added).Count==0,"Yardımcı yetkili panelindeki üye bilgilerine erişebilir");
            service.Login("sporcu","Fit123!");rejected=false;try{service.AdminCreateDeputy("Sahte Yardımcı","fakehelper","FakeOwn-82!");}catch(InvalidOperationException){rejected=true;}
            check(rejected&&!service.HasAdministrationAccess,"Normal üye yardımcı hesabı açamaz");
            rejected=false;try{service.Register("selfhelper","FakeOwn-82!","Kişi",Choices.DeputyRole);}catch(ArgumentException){rejected=true;}
            check(rejected,"Normal kayıt API’si yardımcı rolünü kabul etmez");
        }
        finally{foreach(var suffix in new[]{"",".bak",".tmp"})if(File.Exists(path+suffix))File.Delete(path+suffix);}
    }
    private sealed class OwnerFixture:HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken cancellationToken)
        {
            var payload=request.RequestUri!.AbsolutePath=="/session"?JsonSerializer.Serialize(new{contact="jawharyusuf446@gmail.com",channel="president-password",isOwner=true}):JsonSerializer.Serialize(new{token="fixture"});
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK){Content=new StringContent(payload,Encoding.UTF8,"application/json")});
        }
    }
}
