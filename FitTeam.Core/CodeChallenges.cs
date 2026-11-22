using System.Security.Cryptography;
using System.Text;
namespace FitTeam.Core;

// Bu sınıf yalnız kimlik sunucusunda kullanılır. Kodlar istemciye veya günlüğe döndürülmez.
public sealed class CodeChallenges(TimeProvider? clock = null)
{
    public static readonly TimeSpan Validity = TimeSpan.FromMinutes(2);
    private readonly TimeProvider time = clock ?? TimeProvider.System;
    private readonly Dictionary<string, Challenge> pending = new();
    private readonly object registryGate = new();
    private sealed class Challenge
    {
        public required string Id; public byte[] Hash = []; public required DateTimeOffset Sent;
        public int Attempts; public bool Delivered; public bool External; public string ProviderId = "";
        public readonly SemaphoreSlim CheckGate = new(1, 1);
    }
    public async Task<string> Send(string destination, Func<string, Task> deliver)
    {
        var code = RandomNumberGenerator.GetInt32(0, 1000000).ToString("D6");
        var next = Reserve(destination, SHA256.HashData(Encoding.UTF8.GetBytes(code)), false);
        try { await deliver(code); CompleteDelivery(destination, next); return next.Id; }
        catch { throw new InvalidOperationException("Kod gönderilemedi. Gönderim hesabını ve bağlantıyı kontrol edin."); }
    }
    public async Task<string> SendExternal(string destination, Func<Task<string>> deliver)
    {
        // Sağlayıcı kendi kodunu üretir; burada yalnız özel istek kimliği oluşturulur.
        var next = Reserve(destination, [], true);
        var providerId = await deliver();
        if (string.IsNullOrWhiteSpace(providerId)) throw new InvalidOperationException("SMS hizmetinden geçerli gönderim onayı alınamadı. Yeni kod isteyin.");
        CompleteDelivery(destination, next, providerId);
        return next.Id;
    }
    private Challenge Reserve(string destination, byte[] hash, bool external)
    {
        lock (registryGate)
        {
            var now = time.GetUtcNow();
            foreach (var item in pending.Where(x => now - x.Value.Sent >= Validity).ToArray()) pending.Remove(item.Key);
            if (pending.TryGetValue(destination, out var old) && now - old.Sent < TimeSpan.FromSeconds(60))
                throw new InvalidOperationException("Yeni kod için 60 saniye bekleyin.");
            if (pending.Count >= 500 && !pending.ContainsKey(destination)) throw new InvalidOperationException("Şu an çok fazla istek var. Biraz sonra tekrar deneyin.");
            var next = new Challenge { Id = Convert.ToHexString(RandomNumberGenerator.GetBytes(24)), Hash = hash, Sent = now, External = external };
            pending[destination] = next;
            return next;
        }
    }
    private void CompleteDelivery(string destination, Challenge challenge, string providerId = "")
    {
        lock (registryGate)
        {
            if (!IsCurrent(destination, challenge) || time.GetUtcNow() - challenge.Sent >= Validity)
                throw new InvalidOperationException("Kodun süresi dolmuş veya geçersiz. Yeni kod isteyin.");
            challenge.ProviderId = providerId; challenge.Delivered = true;
        }
    }
    public DateTimeOffset ExpiresAt(string destination, string requestId)
    {
        lock (registryGate)
        {
            if (!pending.TryGetValue(destination, out var challenge) || challenge.Id != requestId)
                throw new InvalidOperationException("Yeni bir doğrulama kodu isteyin.");
            return challenge.Sent + Validity;
        }
    }
    public void Verify(string destination, string requestId, string code)
    {
        lock (registryGate)
        {
            var c = Find(destination, requestId, false);
            c.Attempts++;
            if (!CodeFormat(code) || !CryptographicOperations.FixedTimeEquals(c.Hash, SHA256.HashData(Encoding.UTF8.GetBytes(code)))) throw new InvalidOperationException("Doğrulama kodu yanlış.");
            Consume(c);
        }
    }
    public async Task VerifyExternal(string destination, string requestId, string code, Func<string, string, Task<bool>> check)
    {
        Challenge c;
        lock (registryGate) c = Find(destination, requestId, true);
        // Aynı istek için eşzamanlı onaylar tek oturum açabilir.
        await c.CheckGate.WaitAsync();
        try
        {
            lock (registryGate)
            {
                if (!ReferenceEquals(c, Find(destination, requestId, true))) throw new InvalidOperationException("Yeni bir doğrulama kodu isteyin.");
                c.Attempts++;
                if (!CodeFormat(code)) throw new InvalidOperationException("Doğrulama kodu yanlış.");
            }
            var approved = await check(c.ProviderId, code);
            lock (registryGate)
            {
                if (!IsCurrent(destination, c) || !c.Delivered || time.GetUtcNow() - c.Sent >= Validity) throw new InvalidOperationException("Kodun süresi dolmuş veya geçersiz. Yeni kod isteyin.");
                if (!approved) throw new InvalidOperationException("Doğrulama kodu yanlış.");
                Consume(c);
            }
        }
        finally { c.CheckGate.Release(); }
    }
    private Challenge Find(string destination, string requestId, bool external)
    {
        if (!pending.TryGetValue(destination, out var c)) throw new InvalidOperationException("Yeni bir doğrulama kodu isteyin.");
        if (c.Id != requestId || c.External != external || !c.Delivered || c.Attempts >= 5 || time.GetUtcNow() - c.Sent >= Validity)
            throw new InvalidOperationException("Kodun süresi dolmuş veya geçersiz. Yeni kod isteyin.");
        return c;
    }
    private bool IsCurrent(string destination, Challenge c) => pending.TryGetValue(destination, out var current) && ReferenceEquals(current, c);
    private static bool CodeFormat(string code) => code.Length == 6 && code.All(ch => ch >= '0' && ch <= '9');
    private static void Consume(Challenge c) { c.Attempts = 5; c.Delivered = false; }
}
