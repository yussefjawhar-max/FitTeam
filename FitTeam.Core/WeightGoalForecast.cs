namespace FitTeam.Core;

public enum WeightGoalState { Estimated, Reached, MoreRecordsNeeded, OldRecords, Stable, MovingAway, LongHorizon, Under18, NeedsSpecialCare }
public sealed record WeightGoalEstimate(WeightGoalState State, decimal Current, decimal Target, decimal Remaining,
    decimal Progress, decimal? WeeklyChange = null, int? DaysRemaining = null, DateOnly? TargetDate = null);

public static class WeightGoalForecast
{
    // This is an extrapolation of recorded weights, not a calorie/metabolism model or a prescribed pace.
    public static WeightGoalEstimate Calculate(Profile profile, IEnumerable<WeightEntry> weights, DateOnly today)
    {
        var entries = weights.Where(w => w.Date <= today && w.Weight is >= 30 and <= 300)
            .GroupBy(w => w.Date).Select(g => g.Last()).OrderBy(w => w.Date).ToList();
        decimal current = entries.LastOrDefault()?.Weight ?? profile.Weight;
        decimal target = profile.TargetWeight;
        decimal baseline = profile.GoalStartWeight ?? profile.InitialWeight;
        int direction = Math.Sign(target - baseline);
        if (direction == 0) direction = Math.Sign(target - current);
        bool reached = current == target || direction > 0 && current >= target || direction < 0 && current <= target;
        decimal remaining = reached ? 0 : Math.Abs(target - current);
        decimal initialDistance = Math.Abs(target - baseline);
        decimal progress = reached ? 100 : initialDistance == 0 ? 0
            : Math.Clamp(100m * (initialDistance - remaining) / initialDistance, 0, 100);
        WeightGoalEstimate Result(WeightGoalState state, decimal? weekly = null, int? days = null, DateOnly? date = null)
            => new(state, current, target, remaining, progress, weekly, days, date);
        if (profile.Age < 18) return Result(WeightGoalState.Under18);
        if (profile.NutritionNeedsSpecialCare) return Result(WeightGoalState.NeedsSpecialCare);
        if (entries.Count > 0 && today.DayNumber - entries[^1].Date.DayNumber > 14) return Result(WeightGoalState.OldRecords);
        if (reached) return Result(WeightGoalState.Reached);
        int start = Math.Max(0, today.DayNumber - 55);
        if (profile.GoalStarted != null) start = Math.Max(start, profile.GoalStarted.Value.DayNumber);
        var recent = entries.Where(w => w.Date.DayNumber >= start).ToList();
        if (recent.Count < 2 || recent[^1].Date.DayNumber - recent[0].Date.DayNumber < 7)
            return Result(WeightGoalState.MoreRecordsNeeded);
        int firstDay = recent[0].Date.DayNumber;
        decimal meanDay = recent.Average(w => (decimal)(w.Date.DayNumber - firstDay));
        decimal meanWeight = recent.Average(w => w.Weight);
        decimal denominator = recent.Sum(w => ((w.Date.DayNumber - firstDay) - meanDay) * ((w.Date.DayNumber - firstDay) - meanDay));
        decimal numerator = recent.Sum(w => ((w.Date.DayNumber - firstDay) - meanDay) * (w.Weight - meanWeight));
        decimal slope = numerator / denominator;
        decimal weekly = Math.Round(slope * 7, 4);
        if (Math.Abs(weekly) < 0.05m) return Result(WeightGoalState.Stable, weekly);
        if (Math.Sign(slope) != direction) return Result(WeightGoalState.MovingAway, weekly);
        decimal days = decimal.Ceiling(remaining * denominator / Math.Abs(numerator));
        // Avoid misleading dates years away from a short history and DateOnly overflow.
        if (days > 365 || days > DateOnly.MaxValue.DayNumber - today.DayNumber)
            return Result(WeightGoalState.LongHorizon, weekly);
        return Result(WeightGoalState.Estimated, weekly, (int)days, today.AddDays((int)days));
    }
}
