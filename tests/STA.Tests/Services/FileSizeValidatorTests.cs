using STA.Worker.Services;
using Xunit;

namespace STA.Tests.Services;

public class FileSizeValidatorTests
{
    private readonly FileSizeValidator _validator = new();

    [Theory]
    [InlineData(500, 0, 0, true)]
    [InlineData(100, 100, 0, true)]
    [InlineData(99, 100, 0, false)]
    [InlineData(1000, 0, 1000, true)]
    [InlineData(1001, 0, 1000, false)]
    [InlineData(500, 100, 1000, true)]
    [InlineData(100, 100, 1000, true)]
    [InlineData(1000, 100, 1000, true)]
    [InlineData(99, 100, 1000, false)]
    [InlineData(1001, 100, 1000, false)]
    [InlineData(0, 0, 0, true)]
    [InlineData(1073741824, 1, 1073741824, true)]
    public void IsWithinRange_Cenarios(long fileSize, long min, long max, bool expected)
    {
        Assert.Equal(expected, _validator.IsWithinRange(fileSize, min, max));
    }
}
