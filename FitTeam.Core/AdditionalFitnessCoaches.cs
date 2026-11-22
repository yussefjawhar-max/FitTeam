using System.Security.Cryptography;
namespace FitTeam.Core;

public static class AdditionalFitnessCoaches
{
    public const string Batch="extra-fitness-coaches-2026-10-11";
    public static readonly (string Name,int Age,string Specialty,string Gender)[] Examples=
    [("Emre Acar",26,"Kondisyon","Erkek"),("Burak Tekin",30,"Kuvvet ve kas gelişimi","Erkek"),("Can Erdem",34,"Kilo yönetimi","Erkek"),
     ("Onur Kılıç",39,"Hareket ve esneklik","Erkek"),("Serkan Polat",44,"Genel fitness","Erkek"),
     ("Elif Karaman",25,"Genel fitness","Kadın"),("Derya Yalçın",29,"Kuvvet ve kas gelişimi","Kadın"),
     ("Ece Koç",33,"Kilo yönetimi","Kadın"),("İpek Aydın",38,"Hareket ve esneklik","Kadın"),("Zeynep Demir",43,"Kondisyon","Kadın")];
    public static int Apply(AppData data)
    {
        if(data.FitnessCoachExpansionRevision>=1)return 0;
        int added=0;
        foreach(var example in Examples)
        {
            if(data.Users.Any(user=>user.ExampleBatch==Batch&&user.Profile.Name==example.Name))continue;
            var root="fitness-ek"+(Array.IndexOf(Examples,example)+1).ToString("D2");var username=root;int suffix=2;
            while(data.Users.Any(user=>user.Username.Equals(username,StringComparison.OrdinalIgnoreCase)))username=root+"-"+suffix++;
            var user=new User{Username=username,Role=Choices.Roles[2],SportBranch="Fitness",IsExample=true,ExampleBatch=Batch,ProfileComplete=true,
                RegisteredAt=DateTimeOffset.UtcNow,ExperienceYears=example.Age-22,CoachSpecialty=example.Specialty,
                Profile=new(){Name=example.Name,Age=example.Age,Gender=example.Gender,Country="Türkiye",CountryCode="TR",City="İstanbul"}};
            Passwords.Set(user,Convert.ToHexString(RandomNumberGenerator.GetBytes(16)));data.Users.Add(user);added++;
        }
        data.FitnessCoachExpansionRevision=1;return added;
    }
}
