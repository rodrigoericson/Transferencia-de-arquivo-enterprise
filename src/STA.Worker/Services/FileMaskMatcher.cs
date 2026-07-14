using System.Text.RegularExpressions;

namespace STA.Worker.Services;

public interface IFileMaskMatcher
{
    bool Match(string filename, string mask);
}

public class FileMaskMatcher : IFileMaskMatcher
{
    public bool Match(string filename, string mask)
    {
        if (string.IsNullOrWhiteSpace(filename) || string.IsNullOrWhiteSpace(mask))
            return false;

        if (mask == "*" || mask == "*.*")
            return true;

        var pattern = "^" + Regex.Escape(mask).Replace("\\*", ".*").Replace("\\?", ".") + "$";
        return Regex.IsMatch(filename, pattern, RegexOptions.IgnoreCase);
    }
}
