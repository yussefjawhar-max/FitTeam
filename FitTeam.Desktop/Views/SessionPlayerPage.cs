using Avalonia.Controls;
using Avalonia.Threading;
using FitTeam.Core;
using FitTeam.Desktop.Controls;
namespace FitTeam.Desktop.Views;
public partial class MainWindow
{
    private Guid? sessionDayId;private bool sessionAutoStart;
    private List<Exercise>? practiceExercises;private WorkoutDraft? practiceDraft;private string practiceTitle="";
    private void OpenPractice(IEnumerable<Exercise> exercises,string title){practiceExercises=exercises.Select(e=>e.Copy()).ToList();practiceDraft=null;practiceTitle=title;sessionDayId=null;sessionAutoStart=true;ShowPage("Canlı antrenman");}
    private void OpenSession(Guid id,bool home=false)
    {
        practiceExercises=null;practiceDraft=null;var draft=service.StartSession(id);draft.Location=home?"Ev":"Salon";
        foreach(var result in draft.Exercises)result.PerformedName=ExerciseGuides.PerformedName(result.Name,home);
        service.SaveDraft(draft);if(home)service.SetAttendance(false);sessionDayId=id;sessionAutoStart=true;ShowPage("Canlı antrenman");
    }
    private Control SessionPlayerPage()
    {
        bool practice=practiceExercises!=null;
        if(!practice&&sessionDayId==null)return Ui.Text("Önce bugünkü antrenmanı seç.");
        var day=practice?new PlanDay{Title=practiceTitle,Exercises=practiceExercises!}:service.Plan(Me)!.Days.First(d=>d.Id==sessionDayId);
        var draft=practice?practiceDraft??=new WorkoutDraft{UserId=Me.Id,Location="Ev",Exercises=day.Exercises.Select(e=>new ExerciseResult{Name=e.Name,PerformedName=ExerciseGuides.PerformedName(e.Name,true),Reps=e.Reps}).ToList()}:service.StartSession(sessionDayId!.Value);
        bool home=draft.Location=="Ev";int index=Math.Max(0,draft.Exercises.FindIndex(e=>!e.Done));
        var clock=new SessionClock(elapsed:draft.ElapsedSeconds,active:draft.ActiveSeconds,rest:draft.RestRemaining);var guideClock=new GuidedWorkout(clock,draft,day.Exercises);index=guideClock.Index;
        if(sessionAutoStart&&!guideClock.Finished){clock.Start();sessionAutoStart=false;}var automatic=new CheckBox{Content="Setleri süreyle otomatik ilerlet",IsChecked=true};
        var elapsed=Ui.Text("00:00",40,Ui.Green,true);var restLabel=Ui.Text("Hazır",18,Ui.Muted);var stateLabel=Ui.Text("Başlat düğmesine bas.",14,Ui.Muted);
        var exercise=new StackPanel{Spacing=14};var progress=new ProgressBar{Height=12,Foreground=Ui.Green};var info=Ui.Text("",14,Ui.Muted);
        ExerciseIllustration? picture=null;Button? complete=null;Button? pause=null;Button? preview=null;double movementStart=0;int picturedIndex=-1;bool previewing=false;long previewStarted=0;
        void UpdateTime()
        {
            bool changed=guideClock.Tick(automatic.IsChecked==true);index=guideClock.Index;var value=clock.Read();draft.ElapsedSeconds=(int)Math.Min(int.MaxValue,value.Elapsed);draft.ActiveSeconds=value.Active;draft.RestRemaining=value.Rest;
            var span=TimeSpan.FromSeconds(draft.ElapsedSeconds);elapsed.Text=$"{(int)span.TotalMinutes:00}:{span.Seconds:00}";
            restLabel.Text=value.Rest>0?$"Dinlenme: {Math.Ceiling(value.Rest):0} saniye":"Yeni set için hazırsın";
            stateLabel.Text=!value.Running?value.Elapsed>0?"Duraklatıldı • hareket ve sayaçlar durdu":"Başlat düğmesine bas.":value.Rest>0?"Dinlenme zamanı":$"Setin bitmesine: {Math.Ceiling(guideClock.Remaining):0} saniye";
            if(pause!=null){pause.Content=value.Running?"Duraklat":value.Elapsed>0?"Devam et":"Başlat";pause.IsEnabled=!draft.Exercises.All(e=>e.Done);}
            if(draft.Exercises.All(e=>e.Done))stateLabel.Text="Antrenman tamamlandı.";
            if(previewing)stateLabel.Text="Hareket gösterimi • antrenman süren başlamadı.";
            if(preview!=null){preview.Content=previewing?"Gösterimi durdur":"Önce hareketi izle";preview.IsEnabled=!value.Running;}
            if(picture!=null){picture.Seconds=previewing?System.Diagnostics.Stopwatch.GetElapsedTime(previewStarted).TotalSeconds:Math.Max(0,value.Active-movementStart);picture.InvalidateVisual();}
            if(complete!=null)complete.IsEnabled=value.Running&&value.Rest<=0&&draft.Exercises[index].Sets<day.Exercises[index].Sets;
            if(changed)Render();
        }
        void Save(){UpdateTime();if(!practice)service.SaveDraft(draft);}
        void Render()
        {
            previewing=false;
            if(picturedIndex!=index){movementStart=clock.Read().Active;picturedIndex=index;}
            exercise.Children.Clear();var planned=day.Exercises[index];var result=draft.Exercises[index];
            int total=day.Exercises.Sum(e=>e.Sets),done=draft.Exercises.Sum(e=>e.Sets);progress.Value=total==0?0:done*100d/total;
            info.Text=$"{done}/{total} set • {draft.Exercises.Count(e=>e.Done)}/{draft.Exercises.Count} hareket";
            string performed=ExerciseGuides.PerformedName(planned.Name,home);var guide=ExerciseGuides.For(planned.Name,home);
            exercise.Children.Add(Ui.Text($"Hareket {index+1}/{day.Exercises.Count}",13,Ui.Muted));exercise.Children.Add(Ui.Text(performed,28,Ui.Ink,true));
            if(performed!=planned.Name)exercise.Children.Add(Ui.Text("Programdaki hareket: "+planned.Name+" • evde karşılığı gösteriliyor.",13,Ui.Muted));
            picture=new ExerciseIllustration{Movement=guide?.Movement??""};exercise.Children.Add(picture);
            preview=Ui.Button("Önce hareketi izle",()=>{if(clock.Read().Running)return;previewing=!previewing;if(previewing)previewStarted=System.Diagnostics.Stopwatch.GetTimestamp();UpdateTime();});preview.IsVisible=guide!=null;exercise.Children.Add(preview);
            exercise.Children.Add(Ui.Text(guide?.Instructions??"Bu özel hareketin görseli henüz yok. Tekniğini antrenöründen öğren.",15,Ui.Muted));
            exercise.Children.Add(Ui.Text($"{planned.Sets} set × {planned.Reps} tekrar • {planned.RestSeconds} sn dinlenme",17));
            exercise.Children.Add(Ui.Text($"Yapılan set: {result.Sets}/{planned.Sets}",24,Ui.Green,true));
            var reps=Ui.Number(result.Reps,1,100);var load=Ui.Number(result.LoadKg,0,500,1);
            reps.ValueChanged+=(_,_)=>result.Reps=Ui.Int(reps);load.ValueChanged+=(_,_)=>result.LoadKg=Ui.Value(load);
            exercise.Children.Add(Ui.Row(Ui.Field("Gerçek tekrar",reps),Ui.Field("Ağırlık (kg, isteğe bağlı)",load)));
            complete=Ui.AsyncButton("✓ Seti tamamladım",()=>Run(()=>
            {
                if(result.Sets>=planned.Sets)return;result.PerformedName=performed;guideClock.CompleteSet();index=guideClock.Index;
                Save();Render();
            }),true);
            exercise.Children.Add(Ui.Row(complete,Ui.AsyncButton("Son seti geri al",()=>Run(()=>{guideClock.UndoSet();Save();Render();})),
                Ui.Button("← Önceki hareket",()=>{index=Math.Max(0,index-1);guideClock.Select(index);Render();}),Ui.Button("Sonraki hareket →",()=>{index=Math.Min(day.Exercises.Count-1,index+1);guideClock.Select(index);Render();})));
            UpdateTime();
        }
        pause=Ui.Button("Başlat",()=>{previewing=false;if(clock.Read().Running)clock.Pause();else clock.Start();UpdateTime();});Render();
        var timer=new DispatcherTimer{Interval=TimeSpan.FromMilliseconds(100)};int ticks=0;
        timer.Tick+=(_,_)=>{UpdateTime();if(++ticks%150==0){try{Save();}catch{timer.Stop();clock.Pause();}}};
        var page=Ui.Page(Ui.Card(Ui.Stack(Ui.Text(home?"Evde antrenman":"Canlı antrenman",24,Ui.Ink,true),Ui.Text(day.Title,18,Ui.Ink,true),Ui.Text(practice?"Serbest çalışma • geçmiş takvim kayıtlarını değiştirmez.":"Bugünün programı • sonuçlarını kaydedebilirsin.",13,Ui.Muted),
            Ui.Row(elapsed,pause,restLabel,Ui.Button("Dinlenmeyi bitir",()=>{clock.SkipRest();UpdateTime();})),stateLabel,automatic,progress,info)),Ui.Card(exercise),
            Ui.Card(Ui.Stack(Ui.Row(Ui.AsyncButton(practice?"Çalışmayı bitir":"Bitir ve sonuçları kaydet",()=>Run(()=>{clock.Pause();Save();if(practice)ShowPage("Evde antrenman");else OpenWorkoutLog(day.Id);}),true),
                Ui.Button("Kaydet, sonra devam edeceğim",()=>{try{clock.Pause();Save();ShowPage(practice?"Evde antrenman":"Antrenmanım");}catch(Exception ex){_=Ui.Message(this,Ui.ErrorMessage(ex));}})),
                Ui.Text("Duraklatınca hareket gösterimi, süre ve dinlenme sayacı birlikte durur. Devam et ile kaldığın yerden sürer.",13,Ui.Muted),Ui.Text("Otomatik ilerleme süreyi takip eder. Sonuçları kaydederken gerçekte yaptığın setleri kontrol et.",12,Ui.Muted))));
        page.AttachedToVisualTree+=(_,_)=>timer.Start();page.DetachedFromVisualTree+=(_,_)=>{timer.Stop();clock.Pause();try{UpdateTime();if(service.Current?.Id==draft.UserId)Save();else service.Save();}catch{}};
        return page;
    }
}
