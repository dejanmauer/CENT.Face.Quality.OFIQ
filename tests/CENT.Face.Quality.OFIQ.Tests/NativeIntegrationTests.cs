using CENT.Face.Quality.OFIQ;
using Xunit;

public sealed class NativeRuntime : IDisposable
{
    public OfiqEngine? Engine { get; }

    public NativeRuntime()
    {
        var runtime = Environment.GetEnvironmentVariable("OFIQ_RUNTIME_DIR");
        if (OperatingSystem.IsWindows() && !string.IsNullOrWhiteSpace(runtime))
            Engine = OfiqEngine.Create(runtime);
    }

    public void Dispose() => Engine?.Dispose();
}

public sealed class NativeFactAttribute : FactAttribute
{
    public NativeFactAttribute() => Skip = NativeTestAvailability.SkipReason;
}

public sealed class NativeTheoryAttribute : TheoryAttribute
{
    public NativeTheoryAttribute() => Skip = NativeTestAvailability.SkipReason;
}

internal static class NativeTestAvailability
{
    public static string? SkipReason => !OperatingSystem.IsWindows()
        ? "Native assessments require Windows x64."
        : string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("OFIQ_RUNTIME_DIR"))
            ? "Set OFIQ_RUNTIME_DIR to enable native assessments."
            : null;
}

public class NativeIntegrationTests(NativeRuntime runtime) : IClassFixture<NativeRuntime>
{
    private OfiqEngine Engine => runtime.Engine!;
    private static byte[] Image(string name) => File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "test-images", name));

    [NativeFact]
    public void Runtime_is_the_supported_version() => Assert.Equal(new Version(1, 2, 0), Engine.NativeVersion);

    [NativeTheory]
    [InlineData("eileen-collins.jpg")]
    [InlineData("michael-collins.jpg")]
    public void Public_portrait_returns_scores_face_and_landmarks(string filename)
    {
        var a = Engine.Assess(Image(filename), includeLandmarks: true);
        Assert.InRange(a.UnifiedQualityScore, 0, 100);
        Assert.Equal(28, a.Measures.Count);
        Assert.NotNull(a.Face);
        Assert.True(a.Face.Width > 0 && a.Face.Height > 0);
        Assert.NotEmpty(a.Faces);
        Assert.Equal(98, a.Landmarks.Count);
        Assert.All(a.Measures.Where(m => m.Status == OfiqMeasureStatus.Success), m => Assert.InRange(m.Scalar, 0, 100));
        Assert.NotNull(a[OfiqMeasure.Sharpness]);
    }

    [NativeFact]
    public void Scalar_and_vector_quality_agree()
    {
        var encoded = Image("eileen-collins.jpg");
        var a = Engine.Assess(encoded);
        Assert.Equal(a.UnifiedQualityScore, Engine.ScalarQuality(encoded), 6);
        Assert.Empty(a.Landmarks);
        Assert.Empty(a.Faces);
    }

    [NativeFact]
    public void Invalid_image_bytes_report_decode_error()
    {
        var ex = Assert.Throws<OfiqException>(() => Engine.Assess(new byte[] { 1, 2, 3, 4 }));
        Assert.Equal(OfiqReturnCode.ImageReadingError, ex.Code);
    }

    [NativeFact]
    public void Blank_pixels_report_assessment_error()
    {
        var image = new OfiqImage(256, 256, 8, Enumerable.Repeat((byte)128, 256 * 256).ToArray());
        var ex = Assert.Throws<OfiqException>(() => Engine.Assess(image));
        Assert.Equal(OfiqReturnCode.QualityAssessmentError, ex.Code);
    }

    [NativeFact]
    public void Disposed_engine_rejects_assessment()
    {
        using var engine = OfiqEngine.Create(Engine.RuntimeDirectory);
        engine.Dispose();
        engine.Dispose();
        Assert.Throws<ObjectDisposedException>(() => engine.Assess(Image("eileen-collins.jpg")));
    }
}
