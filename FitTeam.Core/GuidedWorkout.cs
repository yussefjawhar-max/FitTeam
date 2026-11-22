namespace FitTeam.Core;
public sealed class GuidedWorkout
{
    private readonly SessionClock clock;private readonly WorkoutDraft draft;private readonly List<Exercise> exercises;private double lastActive;
    public int Index {get;private set;}
    public bool Finished=>draft.Exercises.All(e=>e.Done);
    public GuidedWorkout(SessionClock clock,WorkoutDraft draft,List<Exercise> exercises)
    {
        if(exercises.Count==0||draft.Exercises.Count!=exercises.Count)throw new ArgumentException("Antrenman hareketleri eşleşmiyor.");
        this.clock=clock;this.draft=draft;this.exercises=exercises;Index=Math.Max(0,draft.Exercises.FindIndex(e=>!e.Done));lastActive=clock.Read().Active;
    }
    public static int SetDuration(Exercise exercise)
    {
        string movement=ExerciseGuides.For(exercise.Name)?.Movement??"";
        return movement is "plank" or "walk" or "run" or "cycle" or "warmup"?Math.Max(10,exercise.Minutes*60/Math.Max(1,exercise.Sets)):Math.Max(3,exercise.Reps*3);
    }
    public double Remaining=>Finished?0:Math.Max(0,SetDuration(exercises[Index])-draft.Exercises[Index].GuidedSetSeconds);
    public void Select(int index){Index=Math.Clamp(index,0,exercises.Count-1);lastActive=clock.Read().Active;}
    public bool Tick(bool automatic=true)
    {
        var value=clock.Read();double delta=Math.Max(0,value.Active-lastActive);lastActive=value.Active;
        if(!value.Running||Finished||delta==0)return false;
        if(draft.Exercises[Index].Done){Index=Math.Max(0,draft.Exercises.FindIndex(e=>!e.Done));return true;}
        var result=draft.Exercises[Index];result.GuidedSetSeconds+=delta;result.GuidedActiveSeconds+=delta;
        if(automatic&&Remaining<=0)return CompleteSet();return false;
    }
    public bool CompleteSet()
    {
        var result=draft.Exercises[Index];var planned=exercises[Index];if(result.Sets>=planned.Sets)return false;
        result.Sets++;result.Done=result.Sets==planned.Sets;result.GuidedSetSeconds=0;result.Minutes=Math.Max(1,(int)Math.Ceiling(result.GuidedActiveSeconds/60));
        if(Finished)clock.Pause();else{clock.BeginRest(planned.RestSeconds);if(result.Done)Index=Math.Max(0,draft.Exercises.FindIndex(e=>!e.Done));}
        lastActive=clock.Read().Active;return true;
    }
    public void UndoSet(){var result=draft.Exercises[Index];result.Sets=Math.Max(0,result.Sets-1);result.Done=false;result.GuidedSetSeconds=0;lastActive=clock.Read().Active;}
}
