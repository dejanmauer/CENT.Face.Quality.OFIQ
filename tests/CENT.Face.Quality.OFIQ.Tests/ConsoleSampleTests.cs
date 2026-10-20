using System.Diagnostics;
using Xunit;

public class ConsoleSampleTests
{
    [Theory]
    [InlineData("--help", 0, "Usage:")]
    [InlineData("", 2, "Usage:")]
    [InlineData("--runtime", 2, "requires a directory")]
    [InlineData("--unknown", 2, "Unknown option")]
    [InlineData("-- --help", 2, "Image not found")]
    [InlineData("-- -h", 2, "Image not found")]
    [InlineData("--runtime --help", 2, "requires a directory")]
    [InlineData("--runtime nowhere missing.jpg", 2, "Image not found")]
    public async Task Command_line_reports_usage_and_errors(string arguments, int expectedExit, string expectedOutput)
    {
        var start = new ProcessStartInfo("dotnet")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        start.ArgumentList.Add(typeof(CENT.Face.Quality.OFIQ.Sample.Program).Assembly.Location);
        foreach (var arg in arguments.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            start.ArgumentList.Add(arg);
        start.Environment.Remove("OFIQ_RUNTIME_DIR");
        using var process = Process.Start(start)!;
        var stdout = process.StandardOutput.ReadToEndAsync();
        var stderr = process.StandardError.ReadToEndAsync();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        try
        {
            await process.WaitForExitAsync(timeout.Token);
        }
        catch (OperationCanceledException)
        {
            process.Kill(entireProcessTree: true);
            throw;
        }
        var output = await stdout + await stderr;
        Assert.Equal(expectedExit, process.ExitCode);
        Assert.Contains(expectedOutput, output);
    }
}
