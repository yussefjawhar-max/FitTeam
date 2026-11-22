using FitTeam.Core;
static class GuidedWorkoutChecks
{
    public static void Run(Action<bool,string> check)
    {
        var time=new GuideTime();var clock=new SessionClock(time);var moves=new List<Exercise>{new(){Name="Duvar / diz üstü şınav",Sets=1,Reps=2,Minutes=1,RestSeconds=90},new(){Name="Sandalyeye çömelme",Sets=1,Reps=2,Minutes=1,RestSeconds=90}};
        var draft=new WorkoutDraft{Exercises=moves.Select(e=>new ExerciseResult{Name=e.Name,Reps=e.Reps}).ToList()};var guide=new GuidedWorkout(clock,draft,moves);
        clock.Start();time.Advance(5);guide.Tick();check(draft.Exercises[0].Sets==0&&guide.Remaining==1,"Rehber ilk set süresini ilerletir; dolmadan seti tamamlamaz");
        time.Advance(1);guide.Tick();check(draft.Exercises[0].Done&&guide.Index==1&&clock.Read().Rest==90,"Set süresi bitince sıradaki hareket seçilir ve doksan saniyelik dinlenme başlar");
        time.Advance(15);guide.Tick();clock.Pause();time.Advance(900);guide.Tick();check(clock.Read().Rest==75&&guide.Remaining==6,"Duraklatınca otomatik ilerleme ve dinlenme birlikte durur");
        clock.Start();time.Advance(75);guide.Tick();check(clock.Read().Rest==0&&guide.Remaining==6,"Dinlenme süresi çalışma süresine yazılmaz");
        time.Advance(6);guide.Tick();check(guide.Finished&&!clock.Read().Running&&draft.Exercises.All(e=>e.Done),"Son hareket süresi bitince bütün rehber durur; sonuç yalnız taslakta oluşur");
        var manualClock=new SessionClock(time);var manual=new WorkoutDraft{Exercises=[new(){Name=moves[0].Name,Reps=2}]};var manualGuide=new GuidedWorkout(manualClock,manual,[moves[0]]);manualClock.Start();time.Advance(30);manualGuide.Tick(false);check(manual.Exercises[0].Sets==0,"Otomatik ilerleme kapatıldığında süre kullanıcı yerine set onaylamaz");
        check(GuidedWorkout.SetDuration(new(){Name="Dirsek üzerinde sabit duruş",Sets=2,Minutes=2,Reps=1})==60,"Süreye dayalı hareketler tekrar temposu yerine program dakikasını kullanır");
    }
    private sealed class GuideTime:TimeProvider {private long ticks;public override long TimestampFrequency=>TimeSpan.TicksPerSecond;public override long GetTimestamp()=>ticks;public void Advance(int seconds)=>ticks+=TimeSpan.FromSeconds(seconds).Ticks;}
}
