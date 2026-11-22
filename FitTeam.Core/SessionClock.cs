namespace FitTeam.Core;
public readonly record struct SessionTime(double Elapsed, double Active, double Rest, bool Running);
public sealed class SessionClock(TimeProvider? provider=null, double elapsed=0, double active=0, double rest=0)
{
    private readonly TimeProvider time=provider??TimeProvider.System;
    private double elapsedBase=elapsed,activeBase=active,restBase=rest;
    private long? started;
    public SessionTime Read()
    {
        double delta=started==null?0:Math.Max(0,time.GetElapsedTime(started.Value).TotalSeconds);
        return new(elapsedBase+delta,activeBase+Math.Max(0,delta-restBase),Math.Max(0,restBase-delta),started!=null);
    }
    private void Commit(){var value=Read();elapsedBase=value.Elapsed;activeBase=value.Active;restBase=value.Rest;if(started!=null)started=time.GetTimestamp();}
    public void Start(){if(started==null)started=time.GetTimestamp();}
    public void Pause(){Commit();started=null;}
    public void BeginRest(int seconds){Commit();restBase=Math.Max(0,seconds);}
    public void SkipRest(){Commit();restBase=0;}
}
