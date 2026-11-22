using FitTeam.Core;
using System.Net;
using System.Text.Json;

static class RecoveryAndMealUpgradeChecks
{
    public static async Task Run(Action<bool,string> check)
    {
        var path=Path.Combine(Path.GetTempPath(),"fitteam-recovery-meals-"+Guid.NewGuid()+".json");
        try
        {
            var service=new FitService(new DataStore(path));var me=service.Data.Users.Single(user=>user.Username=="sporcu");
            me.Email="recoveryfixture@gmail.com";me.Phone="+905551234567";service.Save();
            async Task<CodeIdentity> Proof(string email="recoveryfixture@gmail.com",string channel="google+email",string phone="",bool owner=false)
            {
                var auth=new CodeAuthClient(new Uri("http://127.0.0.1:12345/"),new HttpClient(new RecoveryFixture(email,channel,phone,owner)));
                return await auth.Verify(email,channel,"fixture","000000");
            }
            void Reject(Action action,string label){bool rejected=false;try{action();}catch(Exception ex)when(ex is ArgumentException or InvalidOperationException){rejected=true;}check(rejected,label);}
            var count=service.Data.Users.Count;
            Reject(()=>service.BeginPasswordRecovery("sporcu",Proof("otherfixture@gmail.com").GetAwaiter().GetResult()),"Başka Gmail kanıtı üyenin şifresini kurtaramaz");
            Reject(()=>service.BeginPasswordRecovery("sporcu",Proof(channel:"google").GetAwaiter().GetResult()),"Kod tamamlanmayan Google girişi şifre kurtarma yetkisi vermez");
            Reject(()=>service.BeginPasswordRecovery("sporcu",Proof(channel:"google+phone",phone:"+905559999999").GetAwaiter().GetResult()),"Hesaba kayıtlı olmayan SMS telefonu kurtarma için reddedilir");
            Reject(()=>service.BeginPasswordRecovery("bilinmeyen",Proof().GetAwaiter().GetResult()),"Kurtarma bilinmeyen hesap oluşturmaz");
            check(service.Data.Users.Count==count&&Passwords.Verify(me,"Fit123!"),"Yanlış kurtarma denemeleri kullanıcıları ve şifreleri değiştirmez");
            var proof=await Proof();var ticket=service.BeginPasswordRecovery("sporcu",proof);
            Reject(()=>service.ResetRecoveredPassword(ticket,"YeniFixture-84!","farklı"),"Tekrar yazılan şifre eşleşmeden değişiklik yapılmaz");
            Reject(()=>service.ResetRecoveredPassword(ticket,"kısa","kısa"),"Kısa kurtarma şifresi reddedilir");
            service.ResetRecoveredPassword(ticket,"YeniFixture-84!","YeniFixture-84!");
            check(service.Current==null&&!service.Login("sporcu","Fit123!")&&service.Login("sporcu","YeniFixture-84!"),"Doğrulanmış kurtarmadan sonra eski şifre kapanır, yeni şifreyle normal giriş gerekir");
            Reject(()=>service.ResetRecoveredPassword(ticket,"Yeniden-84!","Yeniden-84!"),"Kullanılmış kurtarma bileti tekrar kullanılamaz");
            Reject(()=>service.BeginPasswordRecovery("sporcu",proof),"Aynı doğrulama kanıtı ikinci kurtarma bileti açamaz");
            var expiredClock=new RecoveryClock();var expiringService=new FitService(new DataStore(path),expiredClock);
            var expired=expiringService.BeginPasswordRecovery("sporcu",await Proof());expiredClock.Advance(TimeSpan.FromMinutes(6));
            Reject(()=>expiringService.ResetRecoveredPassword(expired,"FixtureNext-84!","FixtureNext-84!"),"Beş dakikalık kurtarma biletinin süresi dolunca sıfırlama reddedilir");
            var blocked=service.BeginPasswordRecovery("sporcu",await Proof());me.IsActive=false;
            Reject(()=>service.ResetRecoveredPassword(blocked,"FixtureNext-84!","FixtureNext-84!"),"Doğrulamadan sonra çıkarılan hesap yeniden açılamaz");me.IsActive=true;
            var batch=new AppData();var existing=new User{Username="fitness-ek01",Role=Choices.Roles[2],Profile=new(){Name="Gerçek Hoca",Age=50}};batch.Users.Add(existing);
            check(AdditionalFitnessCoaches.Apply(batch)==10&&batch.Users.Count(user=>user.ExampleBatch==AdditionalFitnessCoaches.Batch&&user.Profile.Gender=="Kadın")==5&&batch.Users.Count(user=>user.ExampleBatch==AdditionalFitnessCoaches.Batch&&user.Profile.Gender=="Erkek")==5,"Beş kadın ve beş erkek hoca örnek hesap olarak eklenir");
            check(batch.Users.Where(user=>user.ExampleBatch==AdditionalFitnessCoaches.Batch).All(user=>user.Profile.Age is >=25 and <=44&&user.IsExample&&user.CoachSpecialty.Length>0),"Ek hocaların yaşları ve uzmanlıkları doludur");
            check(existing.Profile.Name=="Gerçek Hoca"&&AdditionalFitnessCoaches.Apply(batch)==0&&batch.Users.Select(user=>user.Username).Distinct().Count()==batch.Users.Count,"Hoca artırımı gerçek hesabı değiştirmez ve tekrar çalışınca çoğalmaz");
            service.Login("sporcu","YeniFixture-84!");var today=DateOnly.FromDateTime(DateTime.Today);
            var routine=new MealRoutine{FoodPreferencesSet=true,PreferredFoods=FoodPreferences.Options.Select(option=>option.Id).ToList(),Hours=MealRoutine.DefaultHours(me.Profile),ExcludedFoods=["Brokoli","Yumurta"]};
            service.SetMealRoutine(routine,"low",false);var plan=MealSuggestions.Create(me.Profile,today);
            check(plan.State==NutritionState.Ready&&plan.Meals.SelectMany(meal=>meal.Portions).All(portion=>!routine.ExcludedFoods.Contains(portion.Food.Name)),"Sevilmeyen yiyecekler önerilerden ve düzenleme seçeneklerinden çıkarılır");
            check(PersonalMealFoods.Available(routine).All(food=>!routine.ExcludedFoods.Contains(food.Name)),"Sevilmeyen besin düzenleme listesinden tekrar seçilemez");
            var breakfast=plan.Meals[0];service.RecordMealIntake(today,breakfast,"Yarısını yedim");
            check(service.Data.Foods.Single(food=>food.UserId==me.Id&&food.Date==today&&food.Meal==breakfast.Meal).Calories==Math.Round(breakfast.Calories*0.5m,1),"Yarım öğün toplam kaloriye yarım porsiyon olarak yazılır");
            service.RecordMealIntake(today,breakfast,"Tamamını yedim");
            check(service.Data.Foods.Count(food=>food.UserId==me.Id&&food.Date==today&&food.Meal==breakfast.Meal)==1,"Porsiyon düzeltmesi aynı öğünü iki kez saymaz");
            service.RecordMealIntake(today,breakfast,"Öğünü atladım");
            check(!service.Data.Foods.Any(food=>food.UserId==me.Id&&food.Date==today&&food.Meal==breakfast.Meal)&&service.MealIntakes(me,today).Single().Fraction==0,"Atlanan öğün tüketilen kaloriye eklenmez");
            service.UndoMealIntake(today,breakfast.Meal);check(service.MealIntakes(me,today).Count==0,"Geri alma yanlış tüketim kaydını kaldırır");
            service.RecordMealIntake(today,breakfast,"Çeyreğini yedim");var storedId=service.Data.Foods.Single(food=>food.UserId==me.Id&&food.Meal==breakfast.Meal&&food.Date==today).Id;service.RemoveFood(storedId);
            check(service.MealIntakes(me,today).Count==0,"Tüketim kaydı silinince öğün durumu da güncellenir");
            Reject(()=>service.RecordMealIntake(today.AddDays(1),breakfast,"Tamamını yedim"),"Geleceğin öğünü yenmiş olarak kaydedilemez");
            var shopping=MealShopping.Create([plan,plan]);
            check(shopping.Count>0&&shopping.All(line=>line.Quantity==plan.Meals.SelectMany(meal=>meal.Portions).Where(portion=>portion.Food.Name==line.Name).Sum(portion=>portion.Grams)*2),"Alışveriş listesi tekrar eden yiyeceklerin gramlarını toplar");
            var coach=service.Data.Users.Single(user=>user.Username=="koc");service.Login("koc","Fit123!");
            check(service.ApplyPersonalMeals(me,today,plan,routine).Meals.Count==4,"Bağlı hoca üyenin yemek programını inceleyebilir");
            service.Login("koc2","Fit123!");Reject(()=>service.MealIntakes(me,today),"Bağlı olmayan hoca üyenin tüketim kaydını göremez");
            service.Login("sporcu","YeniFixture-84!");routine.AllergyNotes="Örnek alerji notu";service.SetMealRoutine(routine,"low",false);
            check(me.Profile.NutritionNeedsSpecialCare&&MealSuggestions.Create(me.Profile,today,specialCare:false).State==NutritionState.NeedsSpecialCare,"Alerji notu varsa kutu kapalı olsa da otomatik öneri oluşturulmaz");
            var restored=new DataStore(path).Data;
            check(restored.Users.Single(user=>user.Id==me.Id).Profile.MealRoutine!.AllergyNotes==routine.AllergyNotes,"Alerji ve beslenme tercihleri yeniden açılışta korunur");
        }
        finally{foreach(var suffix in new[]{"",".bak",".tmp"})File.Delete(path+suffix);}
    }
    private sealed class RecoveryClock:TimeProvider
    {
        private DateTimeOffset now=DateTimeOffset.UtcNow;
        public override DateTimeOffset GetUtcNow()=>now;
        public void Advance(TimeSpan by)=>now+=by;
    }
    private sealed class RecoveryFixture(string email,string channel,string phone,bool owner):HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken cancellationToken)
        {
            var json=request.RequestUri!.AbsolutePath=="/session"?JsonSerializer.Serialize(new{contact=email,channel,phone,isOwner=owner,googleSubject="recovery-fixture-subject"}):JsonSerializer.Serialize(new{token="fixture-only"});
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK){Content=new StringContent(json,System.Text.Encoding.UTF8,"application/json")});
        }
    }
}
