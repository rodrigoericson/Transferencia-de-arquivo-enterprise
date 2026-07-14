namespace STA.Worker.Services;

public interface IFileLockChecker
{
    bool IsFileLocked(string filePath);
}

public class FileLockChecker : IFileLockChecker
{
    public bool IsFileLocked(string filePath)
    {
        try
        {
            using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.None);
            return false;
        }
        catch (IOException)
        {
            return true;
        }
    }
}
