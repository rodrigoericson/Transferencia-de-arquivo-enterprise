namespace STA.Core.Services;

public static class PathSafety
{
    public static bool IsFileNameSafe(string fileName)
    {
        return !string.IsNullOrWhiteSpace(fileName)
            && !fileName.Contains("..")
            && !fileName.Contains('/')
            && !fileName.Contains('\\')
            && !Path.IsPathRooted(fileName);
    }
}
