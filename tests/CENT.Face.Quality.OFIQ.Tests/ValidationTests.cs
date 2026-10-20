using CENT.Face.Quality.OFIQ;
using Xunit;

public class ValidationTests
{
    [Theory]
    [InlineData(0, 2, 8)]
    [InlineData(2, 0, 8)]
    [InlineData(-1, 2, 8)]
    [InlineData(65536, 1, 8)]
    [InlineData(1, 65536, 8)]
    [InlineData(2, 2, 32)]
    public void Invalid_image_dimensions_or_depth_are_rejected(int width, int height, int depth) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => new OfiqImage(width, height, depth, Array.Empty<byte>()));

    [Fact]
    public void Missing_config_reports_an_ofiq_error()
    {
        var folder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var ex = Assert.Throws<OfiqException>(() => OfiqEngine.Create(folder));
        Assert.Equal(OfiqReturnCode.MissingConfigFileError, ex.Code);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Missing_runtime_argument_is_rejected(string? folder) =>
        Assert.Throws<ArgumentException>(() => OfiqEngine.Create(folder!));
}
