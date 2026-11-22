namespace FitTeam.Core;

public static class Choices
{
    public const string DeputyRole = "Yetkili Yardımcısı";
    public static readonly string[] Roles = ["Bireysel Kullanıcı", "Futbolcu", "Fitness Koçu", "Futbol Takımı Antrenörü"];
    public static readonly string[] Goals = ["Kilo vermek", "Kilo almak", "Kas geliştirmek", "Formunu korumak", "Kilo verip kas geliştirmek", "Kondisyon geliştirmek", "Dayanıklılık artırmak"];
    public static readonly string[] Levels = ["Başlangıç", "Orta", "İleri"];
    public static readonly string[] Positions = ["Kaleci", "Defans", "Orta saha", "Kanat", "Forvet"];
    public static string DayName(DayOfWeek day) => day switch
    {
        DayOfWeek.Monday => "Pazartesi", DayOfWeek.Tuesday => "Salı", DayOfWeek.Wednesday => "Çarşamba",
        DayOfWeek.Thursday => "Perşembe", DayOfWeek.Friday => "Cuma", DayOfWeek.Saturday => "Cumartesi", _ => "Pazar"
    };
    public static readonly DayOfWeek[] Days = [DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday];
}
public class User
{
    public DateTimeOffset? RegisteredAt { get; set; }
    public Guid? RequestedClubId { get; set; }
    public Guid Id { get; set; } = Guid.NewGuid();
    public string SportBranch {get;set;}="";
    public bool NeedsApproval {get;set;}
    public string RequestedRole {get;set;}="";
    public int ExperienceYears {get;set;}
    public List<CoachHours> WorkHours {get;set;}=[];
    public bool IsActive { get; set; } = true;
    public string ExampleBatch {get;set;}="";
    public bool IsExample { get; set; }
    public bool IsClubAccount { get; set; }
    public bool MustChangePassword { get; set; }
    public bool FullAdministrationView {get;set;}
    public string Username { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public string ProtectedPassword {get;set;}="";
    public string Salt { get; set; } = "";
    public string Role { get; set; } = Choices.Roles[0];
    public Profile Profile { get; set; } = new();
    public Guid? CoachId { get; set; }
    public List<Guid> CoachIds { get; set; } = [];
    public string CoachSpecialty { get; set; } = "Fitness";
    public Guid? TeamId { get; set; }
    public string CoachNote { get; set; } = "";
    public bool ProfileComplete { get; set; }
    public string Email { get; set; } = "";
    public string Phone { get; set; } = "";
    public bool EmailVerified { get; set; }
    public string AuthenticationUid { get; set; } = "";
    public string AuthenticationProject { get; set; } = "";
    public bool PhoneVerified { get; set; }
}
public class Profile
{
    public DateOnly? BirthDate { get; set; }
    public string AvatarBase64 { get; set; } = "";
    public string HealthNotes { get; set; } = "";
    public string ActivityLevel { get; set; } = "";
    public bool NutritionNeedsSpecialCare { get; set; }
    public decimal? GoalStartWeight { get; set; }
    public DateOnly? GoalStarted { get; set; }
    public PostalAddress? Address { get; set; }
    public TimeOnly? TrainingStart { get; set; }
    public int TrainingHours { get; set; } = 2;
    public string BloodGroup {get;set;}="Bilmiyorum";
    public string Country {get;set;}="";
    public MealRoutine? MealRoutine {get;set;}
    public string CountryCode {get;set;}="";
    public string HomeAddress {get;set;}="";
    public string City {get;set;}="";
    public string Name { get; set; } = "";
    public int Age { get; set; } = 20;
    public string Gender { get; set; } = "Erkek";
    public decimal Height { get; set; } = 175;
    public decimal Weight { get; set; } = 75;
    public decimal InitialWeight { get; set; } = 75;
    public decimal TargetWeight { get; set; } = 70;
    public string Goal { get; set; } = Choices.Goals[0];
    public string Level { get; set; } = Choices.Levels[0];
    public List<DayOfWeek> AvailableDays { get; set; } = [DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Friday];
    public string Position { get; set; } = Choices.Positions[0];
    public int Jersey { get; set; } = 1;
    public decimal WaterGoal { get; set; } = 3;
}
public class Exercise
{
    public string Name { get; set; } = "";
    public string Muscle { get; set; } = "";
    public int Sets { get; set; } = 3;
    public int Reps { get; set; } = 12;
    public int Minutes { get; set; } = 10;
    public int RestSeconds { get; set; } = 60;
    public Exercise Copy() => (Exercise)MemberwiseClone();
}
public class PlanDay
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateOnly Date { get; set; }
    public string Status { get; set; } = "Planlandı";
    public string Title { get; set; } = "";
    public List<Exercise> Exercises { get; set; } = [];
    public List<Exercise> Baseline { get; set; } = [];
    public Guid? MovedFrom { get; set; }
    public string Adjustment { get; set; } = "";
    public bool Custom { get; set; }
}
public record CoachInfo(Guid Id, string Name);
public class AttendanceIntent
{
    public Guid UserId { get; set; }
    public DateOnly Date { get; set; }
    public bool Coming { get; set; }
}
public class TrainingPlan
{
    public Guid UserId { get; set; }
    public DateOnly Start { get; set; }
    public List<PlanDay> Days { get; set; } = [];
}
public class ExerciseResult
{
    public double GuidedSetSeconds {get;set;}
    public double GuidedActiveSeconds {get;set;}
    public string PerformedName { get; set; } = "";
    public string Name { get; set; } = "";
    public bool Done { get; set; }
    public int Sets { get; set; }
    public int Reps { get; set; }
    public int Minutes { get; set; }
    public string Muscle { get; set; } = "";
    public decimal LoadKg { get; set; }
}
public class WorkoutLog
{
    public string Location { get; set; } = "Salon";
    public Guid UserId { get; set; }
    public Guid PlanDayId { get; set; }
    public DateOnly Date { get; set; }
    public bool Completed { get; set; }
    public int Minutes { get; set; }
    public int Calories { get; set; }
    public int AverageHeart { get; set; }
    public int MaxHeart { get; set; }
    public int Steps { get; set; }
    public decimal Distance { get; set; }
    public int Fatigue { get; set; } = 5;
    public string Difficulty { get; set; } = "Normal";
    public string Pain { get; set; } = "Yok";
    public string TiredMuscle { get; set; } = "Yok";
    public string Note { get; set; } = "";
    public List<ExerciseResult> Exercises { get; set; } = [];
    public Guid? ReviewedBy { get; set; }
}
public class WeightEntry { public Guid UserId { get; set; } public DateOnly Date { get; set; } public decimal Weight { get; set; } }
public class FoodEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public DateOnly Date { get; set; }
    public string Meal { get; set; } = "";
    public string Name { get; set; } = "";
    public decimal CaloriesPerPortion { get; set; }
    public decimal Portions { get; set; }
    public decimal Calories => Math.Round(CaloriesPerPortion * Portions, 1);
}
public class WaterEntry { public Guid UserId { get; set; } public DateOnly Date { get; set; } public decimal Litres { get; set; } }
public class Team
{
    public string CheckInCode { get; set; } = "";
    public string City { get; set; } = "";
    public string ExampleBatch {get;set;}="";
    public ClubChairperson Chairperson {get;set;}=new();
    public string JoinPasswordHash {get;set;}="";
    public string JoinPasswordSalt {get;set;}="";
    public int JoinFailures {get;set;}
    public DateTimeOffset JoinBlockedUntil {get;set;}
    public string SportBranch {get;set;}="Futbol";
    public bool IsActive { get; set; } = true;
    public string AccessCodeHash { get; set; } = "";
    public DateTimeOffset AccessCodeExpires { get; set; }
    public int AccessCodeAttempts { get; set; }
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CoachId { get; set; }
    public string Name { get; set; } = "";
    public string Gender { get; set; } = "Erkek";
}
public class MatchLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public DateOnly Date { get; set; }
    public string Opponent { get; set; } = "";
    public int Minutes { get; set; }
    public int Goals { get; set; }
    public int Assists { get; set; }
    public int Shots { get; set; }
    public decimal Distance { get; set; }
    public int Calories { get; set; }
    public int AverageHeart { get; set; }
    public int MaxHeart { get; set; }
    public int Yellow { get; set; }
    public int Red { get; set; }
    public decimal Rating { get; set; }
}
public class AppData
{
    public int FitnessCoachExpansionRevision {get;set;}
    public int GoalDistributionRevision {get;set;}
    public int AddressDistributionRevision {get;set;}
    public int ClubRosterRevision {get;set;}
    public string OwnerLoginUsername { get; set; } = "";
    public bool ShowExampleData {get;set;}
    public int Version { get; set; } = 1;
    public List<DiaryEntry> Diary { get; set; } = [];
    public List<BodyMeasurement> Measurements { get; set; } = [];
    public List<MemberReminder> Reminders { get; set; } = [];
    public List<PanelNotification> Notifications { get; set; } = [];
    public List<MemberMessage> Messages { get; set; } = [];
    public List<MealAssignment> MealAssignments { get; set; } = [];
    public List<PersonalMealPlan> PersonalMeals { get; set; } = [];
    public List<MealIntake> MealIntakes {get;set;}=[];
    public List<Gym> Gyms { get; set; } = [];
    public List<GymMembership> GymMemberships { get; set; } = [];
    public List<GymVisit> GymVisits { get; set; } = [];
    public List<User> Users { get; set; } = [];
    public List<TrainingPlan> Plans { get; set; } = [];
    public List<WorkoutLog> Workouts { get; set; } = [];
    public List<WeightEntry> Weights { get; set; } = [];
    public List<FoodEntry> Foods { get; set; } = [];
    public List<WaterEntry> Water { get; set; } = [];
    public List<Team> Teams { get; set; } = [];
    public List<MatchLog> Matches { get; set; } = [];
    public List<AttendanceIntent> Attendance { get; set; } = [];
    public List<WorkoutTemplate> Templates { get; set; } = [];
    public List<WorkoutDraft> Drafts { get; set; } = [];
    public List<ProgramChange> Changes { get; set; } = [];
}

public class WorkoutTemplate
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OwnerId { get; set; }
    public string Name { get; set; } = "";
    public List<Exercise> Exercises { get; set; } = [];
}
public class WorkoutDraft
{
    public string Location { get; set; } = "Salon";
    public double ActiveSeconds { get; set; }
    public double RestRemaining { get; set; }
    public Guid UserId { get; set; }
    public Guid PlanDayId { get; set; }
    public int ElapsedSeconds { get; set; }
    public List<ExerciseResult> Exercises { get; set; } = [];
}
public class ProgramChange
{
    public Guid UserId { get; set; }
    public Guid ActorId { get; set; }
    public DateOnly Day { get; set; }
    public DateTimeOffset ChangedAt { get; set; } = DateTimeOffset.UtcNow;
    public string Before { get; set; } = "";
    public string After { get; set; } = "";
    public string Reason { get; set; } = "";
}

public sealed class CoachHours
{
    public DayOfWeek Day {get;set;}
    public TimeOnly Start {get;set;}=new(9,0);
    public TimeOnly End {get;set;}=new(17,0);
}
