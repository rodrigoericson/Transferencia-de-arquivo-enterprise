using System.Text.RegularExpressions;

namespace STA.Worker.Services;

public interface IFileMaskMatcher
{
    bool Match(string filename, string mask);
    bool MatchSimples(string filename, string mask);
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

    public bool MatchSimples(string filename, string mask)
    {
        if (string.IsNullOrWhiteSpace(filename) || string.IsNullOrWhiteSpace(mask))
            return false;

        if (mask == "*" || mask == "*.*")
            return true;

        var maskParts = mask.Split('.');
        var fileParts = filename.Split('.');

        if (maskParts.Length == 0 || fileParts.Length == 0)
            return false;

        var maskExt = maskParts.Length > 1 ? maskParts[^1] : "";
        var fileExt = fileParts.Length > 1 ? fileParts[^1] : "";

        if (maskExt != "*" && !maskExt.Equals(fileExt, StringComparison.OrdinalIgnoreCase))
            return false;

        var maskName = string.Join('.', maskParts.Take(maskParts.Length - 1));
        var fileName = string.Join('.', fileParts.Take(fileParts.Length - 1));

        if (string.IsNullOrEmpty(maskName) || maskName == "*")
            return true;

        if (maskName.StartsWith('*'))
            return fileName.EndsWith(maskName[1..], StringComparison.OrdinalIgnoreCase);

        if (maskName.EndsWith('*'))
            return fileName.StartsWith(maskName[..^1], StringComparison.OrdinalIgnoreCase);

        var starIndex = maskName.IndexOf('*');
        if (starIndex >= 0)
        {
            var prefix = maskName[..starIndex];
            var suffix = maskName[(starIndex + 1)..];
            return fileName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
                && fileName.EndsWith(suffix, StringComparison.OrdinalIgnoreCase)
                && fileName.Length >= prefix.Length + suffix.Length;
        }

        return maskName.Equals(fileName, StringComparison.OrdinalIgnoreCase);
    }
}
