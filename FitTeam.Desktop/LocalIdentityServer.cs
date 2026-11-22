using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text.Json;
using FitTeam.Core;
namespace FitTeam.Desktop;

// Yerel yetkili parola hizmeti yalnız yetkili girişi gerektiğinde açılır. Sadece loopback bağlantı dinler.
public sealed class LocalIdentityServer : IDisposable
{
    private readonly object lifecycleGate=new();
    private readonly CancellationTokenSource shutdown=new();
    private Task<CodeAuthClient>? starting;
    private Process? process;
    private HttpClient? clientHttp;
    private bool disposed;
    public CodeAuthClient? Client {get;private set;}
    public string SetupToken {get;}=Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
    public Task<CodeAuthClient> Start()
    {
        lock(lifecycleGate)
        {
            ObjectDisposedException.ThrowIf(disposed,this);
            if(Client!=null)return Task.FromResult(Client);
            // Bütün giriş düğmeleri aynı açılışı bekler; yardımcı süreç bir kez başlatılır.
            return starting??=Task.Run(StartCore);
        }
    }
    private async Task<CodeAuthClient> StartCore()
    {
        Process? ownedProcess=null;
        HttpClient? ownedHttp=null;
        var succeeded=false;
        try
        {
            var name=OperatingSystem.IsWindows()?"FitTeam.AuthServer.exe":"FitTeam.AuthServer";
            var exe=Path.Combine(AppContext.BaseDirectory,"AuthServer",name);
            if(!File.Exists(exe)) throw new InvalidOperationException("Giriş hizmeti bu pakette yok. Güncel FitTeam uygulamasını açın.");
            var listener=new TcpListener(IPAddress.Loopback,0); listener.Start(); var port=((IPEndPoint)listener.LocalEndpoint).Port;listener.Stop();
            var start=new ProcessStartInfo(exe) { UseShellExecute=false,CreateNoWindow=true,WorkingDirectory=Path.GetDirectoryName(exe)! };
            start.Environment["ASPNETCORE_URLS"]="http://127.0.0.1:"+port;
            start.Environment["FITTEAM_SETUP_TOKEN"]=SetupToken;
            lock(lifecycleGate)
            {
                ObjectDisposedException.ThrowIf(disposed,this);
                ownedProcess=Process.Start(start)??throw new InvalidOperationException("Giriş hizmeti başlatılamadı.");
                process=ownedProcess;
            }
            var endpoint=new Uri("http://127.0.0.1:"+port+"/");
            await WaitForHealth(endpoint,ownedProcess,shutdown.Token);
            // Açılış sorgularının kısa süresi normal doğrulama isteklerine uygulanmaz.
            ownedHttp=new HttpClient {Timeout=TimeSpan.FromSeconds(30)};
            var candidate=new CodeAuthClient(endpoint,ownedHttp);
            lock(lifecycleGate)
            {
                ObjectDisposedException.ThrowIf(disposed,this);
                Client=candidate;
                clientHttp=ownedHttp;
                succeeded=true;
            }
            return candidate;
        }
        finally
        {
            if(!succeeded)
            {
                lock(lifecycleGate)
                {
                    if(ReferenceEquals(process,ownedProcess))process=null;
                }
                ownedHttp?.Dispose();
                StopOwnedProcess(ownedProcess);
                lock(lifecycleGate)starting=null;
            }
        }
    }
    private static async Task WaitForHealth(Uri endpoint,Process ownedProcess,CancellationToken shutdownToken)
    {
        using var deadline=CancellationTokenSource.CreateLinkedTokenSource(shutdownToken);
        deadline.CancelAfter(TimeSpan.FromSeconds(30));
        using var probeHttp=new HttpClient {Timeout=Timeout.InfiniteTimeSpan};
        try
        {
            while(!deadline.IsCancellationRequested)
            {
                if(ownedProcess.HasExited)break;
                using var probe=CancellationTokenSource.CreateLinkedTokenSource(deadline.Token);
                probe.CancelAfter(TimeSpan.FromSeconds(1));
                try
                {
                    using var response=await probeHttp.GetAsync(new Uri(endpoint,"health"),HttpCompletionOption.ResponseHeadersRead,probe.Token);
                    response.EnsureSuccessStatusCode();
                    using var health=JsonDocument.Parse(await response.Content.ReadAsStringAsync(probe.Token));
                    var root=health.RootElement;
                    if(root.GetProperty("ready").GetBoolean())
                    {
                        _=root.GetProperty("email").GetBoolean();
                        _=root.GetProperty("phone").GetBoolean();
                        return;
                    }
                }
                catch(Exception error) when(error is HttpRequestException or OperationCanceledException or JsonException or KeyNotFoundException or InvalidOperationException)
                {
                    // Soğuk açılışta bağlantı henüz dinlenmiyorsa aynı süreci bekle.
                }
                await Task.Delay(150,deadline.Token);
            }
        }
        catch(OperationCanceledException) when(!shutdownToken.IsCancellationRequested) { }
        shutdownToken.ThrowIfCancellationRequested();
        throw new InvalidOperationException("Giriş hizmeti açılamadı.");
    }
    private static void StopOwnedProcess(Process? ownedProcess)
    {
        if(ownedProcess==null)return;
        try {if(!ownedProcess.HasExited)ownedProcess.Kill(true);}
        catch(InvalidOperationException) { }
        catch(System.ComponentModel.Win32Exception) { }
        finally {ownedProcess.Dispose();}
    }
    public void Dispose()
    {
        Process? ownedProcess;
        HttpClient? ownedHttp;
        CodeAuthClient? ownedClient;
        lock(lifecycleGate)
        {
            if(disposed)return;
            disposed=true;
            ownedProcess=process;process=null;
            ownedHttp=clientHttp;clientHttp=null;
            ownedClient=Client;Client=null;
        }
        shutdown.Cancel();
        ownedClient?.Logout();
        ownedHttp?.Dispose();
        StopOwnedProcess(ownedProcess);
    }
}
