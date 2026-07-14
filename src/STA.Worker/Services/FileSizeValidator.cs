namespace STA.Worker.Services;

public interface IFileSizeValidator
{
    bool IsWithinRange(long fileSizeBytes, long minBytes, long maxBytes);
}

public class FileSizeValidator : IFileSizeValidator
{
    public bool IsWithinRange(long fileSizeBytes, long minBytes, long maxBytes)
    {
        if (minBytes == 0 && maxBytes == 0)
            return true;

        if (minBytes > 0 && maxBytes == 0)
            return fileSizeBytes >= minBytes;

        if (minBytes == 0 && maxBytes > 0)
            return fileSizeBytes <= maxBytes;

        return fileSizeBytes >= minBytes && fileSizeBytes <= maxBytes;
    }
}
