namespace FitTeam.Core;

internal static class DemoData
{
    public static AppData Create()
    {
        var data = new AppData(); var today = DateOnly.FromDateTime(DateTime.Today); var start = today.AddDays(-12);
        User Create(string username, string name, string role, decimal weight, decimal target)
        {
            var user = new User { IsExample=true, Username = username, Role = role, ProfileComplete = true, Profile = new Profile { Name = name, Weight = weight, InitialWeight = weight, TargetWeight = target, Goal = role == Choices.Roles[1] ? "Kondisyon geliştirmek" : "Kilo verip kas geliştirmek", Level = "Orta", Position = "Orta saha", Jersey = 8 } };
            Passwords.Set(user, "Fit123!"); data.Users.Add(user); return user;
        }
        var athlete = Create("sporcu", "Deniz Yılmaz", Choices.Roles[0], 82, 75);
        var player = Create("futbolcu", "Ege Demir", Choices.Roles[1], 74, 73);
        var coach = Create("koc", "Selin Kaya", Choices.Roles[2], 65, 65);
        var secondCoach = Create("koc2", "Tolga Arslan", Choices.Roles[2], 78, 78);
        var trainer = Create("antrenor", "Mert Aydın", Choices.Roles[3], 80, 80);
        var member = Create("uye", "Ada Şahin", Choices.Roles[0], 68, 64);
        var player2 = Create("oyuncu", "Ali Çelik", Choices.Roles[1], 78, 76);
        coach.Profile.Gender="Kadın";member.Profile.Gender="Kadın";
        athlete.CoachId = member.CoachId = coach.Id;
        athlete.CoachNote = "Bu hafta düzenli kayıt tutmaya devam et. Sonraki antrenmanını panelden kontrol edebilirsin.";
        var team = new Team { CoachId = trainer.Id, Name = "FitTeam FK", Gender = "Erkek" }; data.Teams.Add(team); player.TeamId = player2.TeamId = team.Id;
        data.Gyms.Add(new Gym{Id=Guid.NewGuid(),ManagerId=coach.Id,Name="FitTeam Spor Merkezi",City="İstanbul",Address="Örnek salon adresi",Hours="09:00–22:00",Description="Örnek salon • Deneme verisi",CheckInCode="FTEAM6"});
        int sample = 0;
        foreach (var user in new[] { athlete, player, member, player2 })
        {
            var plan = new TrainingPlan { UserId = user.Id, Start = start }; int n = 0;
            for (int i = 0; i < 30; i++)
            {
                var date = start.AddDays(i); bool training = user.Profile.AvailableDays.Contains(date.DayOfWeek);
                var exercises = training ? FitService.BuildExercises(user.Profile, user.Role == Choices.Roles[1], n++) : [];
                var day = new PlanDay { Date = date, Title = training ? string.Join(" + ", exercises.Select(e => e.Muscle).Distinct()) : "Aktif dinlenme", Status = training ? "Planlandı" : "Dinlenme", Exercises = exercises, Baseline = exercises.Select(e => e.Copy()).ToList() };
                plan.Days.Add(day);
                if (training && date < today)
                {
                    if (i == 5) { day.Status = "Kaçırıldı"; continue; }
                    day.Status = "Tamamlandı";
                    data.Workouts.Add(new WorkoutLog { UserId = user.Id, PlanDayId = day.Id, Date = date, Completed = true, Minutes = 45 + i, Calories = 310 + i * 12 + sample * 15, AverageHeart = 126 + i, MaxHeart = 164 + i, Steps = 4200 + i * 150, Distance = 3.2m + i / 10m + sample / 5m, Fatigue = 4 + i % 3, Note = "Örnek veri", Exercises = exercises.Select(e => new ExerciseResult { Name = e.Name, Done = true, Sets = e.Sets, Reps = e.Reps, Minutes = e.Minutes }).ToList() });
                }
            }
            data.Plans.Add(plan);
            for (int i = 0; i < 3; i++) data.Weights.Add(new WeightEntry { UserId = user.Id, Date = start.AddDays(i * 6), Weight = user.Profile.InitialWeight - i * 0.6m });
            user.Profile.Weight -= 1.2m;
            data.Water.Add(new WaterEntry { UserId = user.Id, Date = today, Litres = 1.25m + sample / 10m });
            if (user.Role == Choices.Roles[1]) for (int i = 0; i < 3; i++) data.Matches.Add(new MatchLog { UserId = user.Id, Date = today.AddDays(-10 + i * 3), Opponent = new[] { "Yıldız SK", "Kent FK", "Sahil Spor" }[i], Minutes = 80, Goals = i % 2, Assists = 1, Shots = 3 + i, Distance = 8.1m + i / 2m + sample / 5m, Calories = 620, AverageHeart = 145, MaxHeart = 178, Rating = 7 + i / 2m + sample / 10m });
            sample++;
        }
        foreach (var u in new[] { coach, secondCoach, trainer }) data.Weights.Add(new WeightEntry { UserId = u.Id, Date = today, Weight = u.Profile.Weight });
        return data;
    }
}
