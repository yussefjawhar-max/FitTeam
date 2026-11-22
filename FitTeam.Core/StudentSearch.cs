using System.Globalization;
using System.Text;
namespace FitTeam.Core;

public static class StudentSearch
{
    private static string Normalize(string value) => new(value.Normalize(NormalizationForm.FormD)
        .Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
        .Select(c => char.ToLowerInvariant(c == 'ı' ? 'i' : c)).ToArray());

    public static List<User> Find(IEnumerable<User> students, string? query)
    {
        var words = Normalize(query ?? "").Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        if (words.Length == 0) return [];
        return students.Where(user =>
        {
            var text = Normalize(user.Profile.Name + " " + user.Username);
            return words.All(word => text.Contains(word, StringComparison.Ordinal));
        }).OrderBy(user => user.Profile.Name).ThenBy(user => user.Username).ToList();
    }
}
