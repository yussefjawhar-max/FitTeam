using System.Net;
using System.Text;
using System.Text.Json;
using FitTeam.Core;

static class PresidentOverviewChecks
{
    public static async Task Run(Action<bool,string> check)
    {
        var store=DataStore.Preview();var data=store.Data;var service=new FitService(store);var today=new DateOnly(2026,10,10);
        var student=new User{Username="adlıogrenci",Role=Choices.Roles[0],IsExample=true,ExampleBatch=RequestedExamples.Batch,Profile=new(){Name="Aranan Öğrenci"}};
        var coach=new User{Username="kulupkoc",Role=Choices.Roles[3],IsClubAccount=true,IsExample=true,ExampleBatch=RequestedExamples.Batch,Profile=new(){Name="Kulüp Hocası"}};
        data.Users.AddRange([student,coach]);student.CoachIds=[coach.Id];
        var removed=new User{Role=Choices.Roles[0],IsActive=false,Profile=new(){Name="Çıkarılan"}};data.Users.Add(removed);
        service.Login("sporcu","Fit123!");bool blocked=false;try{service.AdministrationUsers(AdministrationGroup.Students);}catch(InvalidOperationException){blocked=true;}
        check(blocked,"Normal üye başkanın tam kişi listesini açamaz");blocked=false;try{service.AdministrationDay(student.Id,today);}catch(InvalidOperationException){blocked=true;}
        check(blocked,"Normal üye başka kişinin günlük bilgilerine başkan yöntemiyle erişemez");
        var auth=new CodeAuthClient(new Uri("http://127.0.0.1:12345/"),new HttpClient(new OwnerFixture()));service.LoginCode(await auth.Verify("","email","fixture","123456"));
        check(service.AdministrationUsers(AdministrationGroup.Students).Select(u=>u.Id).SequenceEqual(new[]{student.Id})&&service.AdministrationUsers(AdministrationGroup.Coaches).Any(u=>u.Id==coach.Id)&&!service.AdministrationUsers(AdministrationGroup.People).Any(u=>u.Id==removed.Id),"Başkan sayıları ve açılan listeler aynı aktif üyeleri içerir; kulüp antrenörleri listeden düşmez");
        var plan=new PlanDay{Date=today,Title="Öğrenciye ait plan"};data.Plans.Add(new(){UserId=student.Id,Days=[plan]});data.Plans.Add(new(){UserId=coach.Id,Days=[new(){Date=today,Title="Başka plan"}]});
        data.Foods.AddRange([new(){UserId=student.Id,Date=today,Name="Bugünkü öğün",CaloriesPerPortion=400,Portions=1},new(){UserId=student.Id,Date=today.AddDays(-1),Name="Dünkü öğün",CaloriesPerPortion=200,Portions=1},new(){UserId=coach.Id,Date=today,Name="Başkasının öğünü",CaloriesPerPortion=600,Portions=1}]);
        data.Workouts.Add(new(){UserId=student.Id,Date=today,Completed=true});data.Water.Add(new(){UserId=student.Id,Date=today,Litres=2});int foodCount=data.Foods.Count;
        var snapshot=service.AdministrationDay(student.Id,today);
        check(snapshot.Plans.Single().Id==plan.Id&&snapshot.Foods.Single().Name=="Bugünkü öğün"&&snapshot.Workouts.Count==1&&snapshot.Coaches.Single().Id==coach.Id&&snapshot.Water==2,"Başkan kişi ekranı yalnız seçilen kişinin ve seçilen günün plan, yemek, antrenman, hoca ve suyunu getirir");
        check(service.AdministrationDay(student.Id,today.AddDays(-1)).Foods.Single().Name=="Dünkü öğün"&&data.Foods.Count==foodCount,"Tarih değiştirerek geçmiş günlük kayıtlar okunur; öneri veya görüntüleme yeni tüketim kaydı oluşturmaz");
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
