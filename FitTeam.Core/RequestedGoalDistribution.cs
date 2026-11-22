namespace FitTeam.Core;

public static class RequestedGoalDistribution
{
    public const int Revision=1;
    public static int Apply(AppData data,DateOnly today)
    {
        if(data.GoalDistributionRevision>=Revision)return 0;
        int changed=0,index=0;
        foreach(var user in data.Users.Where(u=>u.IsExample&&(u.ExampleBatch==RequestedClubStudents.Batch||u.ExampleBatch==RequestedExamples.Batch)).OrderBy(u=>u.Username,StringComparer.Ordinal))
        {
            var p=user.Profile;int variant=index++%4;
            // Önceden kişinin değiştirdiği bir hedefin üzerine yazılmaz.
            if(p.GoalStarted!=null||p.Weight is <30 or >300||p.Height is <100 or >230)continue;
            decimal lower=Math.Max(30,decimal.Ceiling(19m*(p.Height/100m)*(p.Height/100m)));
            bool gain=variant is 1 or 2||p.Weight-3<=lower;
            decimal change=variant==2?3:3+index%4;
            decimal target=gain?Math.Min(299,p.Weight+change):Math.Max(lower,p.Weight-change);
            if(target==p.Weight)continue;
            p.Goal=gain?variant==2?Choices.Goals[2]:Choices.Goals[1]:variant==3?Choices.Goals[4]:Choices.Goals[0];
            p.TargetWeight=target;p.GoalStartWeight=p.Weight;p.GoalStarted=today;changed++;
        }
        data.GoalDistributionRevision=Revision;return changed;
    }
}
