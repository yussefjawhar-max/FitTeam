using System.Net;
using System.Text;
using System.Text.Json;
using FitTeam.Core;

static class TwilioVerifyChecks
{
    private const string Account = "ACaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
    private const string Service = "VAbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb";
    private const string Verification = "VEcccccccccccccccccccccccccccccccc";
    private const string Phone = "+905550000001";
    private const string Token = "fixture-auth-token-with-no-real-credential";
    public static async Task Run(Action<bool, string> check)
    {
        async Task Reject(Func<Task> action, string name)
        {
            bool rejected = false;
            try { await action(); } catch (ArgumentException) { rejected = true; } catch (InvalidOperationException) { rejected = true; }
            check(rejected, name);
        }
        var fixture = new Fixture();
        using var http = new HttpClient(fixture);
        using var delivery = new TwilioVerifyDelivery(Account, Token, Service, http);
        check(await delivery.Send(Phone) == Verification, "Verify gönderim onayı gerçek sağlayıcı istek kimliği döndürür");
        check(fixture.Calls[0].Url == "https://verify.twilio.com/v2/Services/" + Service + "/Verifications" && fixture.Calls[0].Method == HttpMethod.Post,
            "Verify gönderimi resmi HTTPS uç noktasına POST yapar");
        check(fixture.Calls[0].Form.Count == 2 && fixture.Calls[0].Form["To"] == Phone && fixture.Calls[0].Form["Channel"] == "sms",
            "Verify gönderiminde yerel kod, özel SMS metni ve gönderici numarası bulunmaz");
        check(fixture.Calls.All(c => c.Auth == "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes(Account + ":" + Token))), "Verify yalnız hesap kimliğiyle yetkilendirilmiş istek yapar");
        fixture.Status = "approved";
        check(await delivery.Check(Phone, Verification, "123456"), "Verify yalnız approved yanıtıyla SMS kodunu onaylar");
        var call = fixture.Calls.Last();
        check(call.Url == "https://verify.twilio.com/v2/Services/" + Service + "/VerificationCheck" && call.Form.Count == 2 && call.Form["To"] == Phone && call.Form["Code"] == "123456",
            "Verify kontrolü deneme hesabının izin verdiği To ve Code alanlarını gönderir");
        var before = fixture.Calls.Count;
        check(!await delivery.Check(Phone, Verification, "bad") && !await delivery.Check(Phone, "VEinvalid", "123456") && fixture.Calls.Count == before,
            "Biçimsiz kod veya sağlayıcı kimliği Twilio'ya gönderilmez");
        fixture.Status = "pending";
        check(!await delivery.Check(Phone, Verification, "123456"), "Verify pending yanıtı giriş açmaz");
        fixture.Status = "approved";
        foreach (var field in new[] { "sid", "service_sid", "account_sid", "to", "channel" })
        {
            fixture.OverrideField = field;
            check(!await delivery.Check(Phone, Verification, "123456"), "Verify farklı " + field + " yanıtını reddeder");
        }
        fixture.OverrideField = "";
        foreach (var status in new[] { HttpStatusCode.BadRequest, HttpStatusCode.NotFound, HttpStatusCode.TooManyRequests })
        {
            fixture.HttpStatus = status;
            check(!await delivery.Check(Phone, Verification, "123456"), "Verify " + (int)status + " yanıtı hesabı doğrulamaz");
        }
        await Reject(() => delivery.Send(Phone), "Başarısız Verify gönderimi challenge oluşturmak için kimlik vermez");
        fixture.HttpStatus = HttpStatusCode.OK;
        fixture.Status = "approved";
        await Reject(() => delivery.Send(Phone), "Gönderim sırasında approved yanıtı yeni SMS onayı sayılmaz");
        fixture.Status = "pending"; fixture.OverrideField = "to";
        await Reject(() => delivery.Send(Phone), "Verify başka telefon için gönderim onayı veremez");
        fixture.OverrideField = ""; fixture.InvalidJson = true;
        await Reject(() => delivery.Send(Phone), "Bozuk sağlayıcı yanıtı kod gönderildi sayılmaz");
        fixture.InvalidJson = false; fixture.HttpStatus = HttpStatusCode.Unauthorized;
        string error = "";
        try { await delivery.Send(Phone); } catch (InvalidOperationException ex) { error = ex.Message; }
        check(error.Length > 0 && !error.Contains("provider-secret") && !error.Contains(Token), "SMS hata metni sağlayıcı gövdesini veya gizli anahtarı sızdırmaz");

        var clock = new Clock(); var challenges = new CodeChallenges(clock);
        const string bound = "fixture-google-subject:phone:" + Phone;
        var request = await challenges.SendExternal(bound, () => Task.FromResult(Verification));
        check(request.Length == 48 && request != Verification && challenges.ExpiresAt(bound, request) == clock.GetUtcNow().AddMinutes(2), "Harici kod sağlayıcı kimliği yerine özel ve iki dakikalık istek kimliği alır");
        await Reject(() => challenges.VerifyExternal("other-subject:phone:" + Phone, request, "123456", (_, _) => Task.FromResult(true)), "Verify kodu başka Google hesabına aktarılamaz");
        await Reject(() => challenges.VerifyExternal("fixture-google-subject:phone:+905550000002", request, "123456", (_, _) => Task.FromResult(true)), "Verify kodu başka telefon numarasına aktarılamaz");
        await Reject(() => challenges.VerifyExternal(bound, "wrong-request", "123456", (_, _) => Task.FromResult(true)), "Verify eski veya farklı istek kimliğini kabul etmez");
        await Reject(() => challenges.SendExternal(bound, () => Task.FromResult(Verification)), "Verify yeniden SMS göndermede 60 saniye sınırını korur");
        bool providerCalled = false;
        await Reject(() => challenges.VerifyExternal(bound, request, "bad", (_, _) => { providerCalled = true; return Task.FromResult(true); }), "Verify biçimsiz denemeyi yerel olarak reddeder");
        check(!providerCalled, "Biçimsiz kod için sağlayıcı kontrolü çalıştırılmaz");
        await Reject(() => challenges.VerifyExternal(bound, request, "123456", (_, _) => Task.FromResult(false)), "Sağlayıcının reddettiği SMS kodu yerel olarak kabul edilemez");
        await challenges.VerifyExternal(bound, request, "123456", (provider, code) => { check(provider == Verification && code == "123456", "Verify isteği saklanan sağlayıcı kimliğine bağlı kalır"); return Task.FromResult(true); });
        await Reject(() => challenges.VerifyExternal(bound, request, "123456", (_, _) => Task.FromResult(true)), "Onaylanan Verify kodu yalnız bir kez giriş açar");
        await Reject(() => Task.Run(() => challenges.Verify(bound, request, "123456")), "Harici SMS kodu yerel hash doğrulamasına geçirilemez");
        clock.Advance(TimeSpan.FromMinutes(1)); request = await challenges.SendExternal(bound, () => Task.FromResult(Verification));
        for (int i = 0; i < 5; i++) await Reject(() => challenges.VerifyExternal(bound, request, "123456", (_, _) => Task.FromResult(false)), "Verify hatalı deneme " + (i + 1));
        await Reject(() => challenges.VerifyExternal(bound, request, "123456", (_, _) => Task.FromResult(true)), "Verify beş hatalı denemeden sonra doğru kodu da reddeder");
        clock.Advance(TimeSpan.FromMinutes(1)); request = await challenges.SendExternal(bound, () => Task.FromResult(Verification));
        clock.Advance(TimeSpan.FromMinutes(2));
        await Reject(() => challenges.VerifyExternal(bound, request, "123456", (_, _) => Task.FromResult(true)), "Verify sağlayıcı süresi daha uzun olsa bile tam iki dakikada sona erer");

        var failedCodes = new CodeChallenges(clock);
        await Reject(() => failedCodes.SendExternal(bound, () => Task.FromException<string>(new InvalidOperationException("Gönderim reddedildi."))), "Gönderim başarısızsa harici kod aktif olmaz");
        await Reject(() => failedCodes.SendExternal(bound, () => Task.FromResult(Verification)), "Başarısız gönderimden sonra da tekrar gönderim sınırı korunur");
        var concurrent = new CodeChallenges(clock);
        var startSend = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        var sending = concurrent.SendExternal(bound, () => startSend.Task);
        await Reject(() => concurrent.VerifyExternal(bound, "unknown", "123456", (_, _) => Task.FromResult(true)), "Gönderim onayı gelmeden Verify kodu kullanılamaz");
        startSend.SetResult(Verification); request = await sending;
        var startCheck = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously); int providerCalls = 0;
        async Task<bool> Provider(string _, string __) { Interlocked.Increment(ref providerCalls); return await startCheck.Task; }
        var first = concurrent.VerifyExternal(bound, request, "123456", Provider);
        var second = concurrent.VerifyExternal(bound, request, "123456", Provider);
        startCheck.SetResult(true); await first;
        await Reject(() => second, "Eşzamanlı iki doğru Verify denemesi yalnız bir oturum açar");
        check(providerCalls == 1, "Tüketilen Verify isteği için ikinci sağlayıcı isteği yapılmaz");

        clock.Advance(TimeSpan.FromMinutes(1)); request = await concurrent.SendExternal(bound, () => Task.FromResult(Verification));
        var slowCheck = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var slow = concurrent.VerifyExternal(bound, request, "123456", (_, _) => slowCheck.Task);
        clock.Advance(TimeSpan.FromMinutes(2)); slowCheck.SetResult(true);
        await Reject(() => slow, "Sağlayıcı yanıtı beklenirken süresi dolan kod giriş açmaz");
        request = await concurrent.SendExternal(bound, () => Task.FromResult(Verification));
        slowCheck = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        slow = concurrent.VerifyExternal(bound, request, "123456", (_, _) => slowCheck.Task);
        clock.Advance(TimeSpan.FromMinutes(1));
        var replacement = await concurrent.SendExternal(bound, () => Task.FromResult("VEdddddddddddddddddddddddddddddddd"));
        slowCheck.SetResult(true);
        await Reject(() => slow, "Yeni SMS gönderildikten sonra önceki sağlayıcı onayı giriş açmaz");
        await concurrent.VerifyExternal(bound, replacement, "123456", (_, _) => Task.FromResult(true));
        check(true, "Yeni Verify isteği önceki onay bekleyişinden etkilenmeden doğrulanır");
    }
    private sealed class Clock : TimeProvider
    {
        private DateTimeOffset now = DateTimeOffset.Parse("2026-10-10T00:00:00Z");
        public override DateTimeOffset GetUtcNow() => now;
        public void Advance(TimeSpan duration) => now += duration;
    }
    private sealed record Call(string Url, HttpMethod Method, string Auth, Dictionary<string, string> Form);
    private sealed class Fixture : HttpMessageHandler
    {
        public List<Call> Calls { get; } = [];
        public string Status = "pending", OverrideField = "";
        public bool InvalidJson;
        public HttpStatusCode HttpStatus = HttpStatusCode.OK;
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var body = await request.Content!.ReadAsStringAsync(cancellationToken);
            var form = body.Split('&').ToDictionary(p => Uri.UnescapeDataString(p.Split('=')[0].Replace('+', ' ')), p => Uri.UnescapeDataString(p.Split('=')[1].Replace('+', ' ')));
            Calls.Add(new(request.RequestUri!.AbsoluteUri, request.Method, request.Headers.Authorization!.ToString(), form));
            var data = new Dictionary<string, string> { ["sid"] = Verification, ["service_sid"] = Service, ["account_sid"] = Account, ["to"] = Phone, ["channel"] = "sms", ["status"] = Status };
            if (OverrideField.Length > 0) data[OverrideField] = "mismatch";
            var payload = HttpStatus != HttpStatusCode.OK ? "provider-secret must never be shown" : InvalidJson ? "broken-json" : JsonSerializer.Serialize(data);
            return new HttpResponseMessage(HttpStatus) { Content = new StringContent(payload, Encoding.UTF8, "application/json") };
        }
    }
}
