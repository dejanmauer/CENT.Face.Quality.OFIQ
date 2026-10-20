using System.Globalization;
using CENT.Face.Quality.OFIQ;

namespace CENT.Face.Quality.OFIQ.Sample;

public static class Program
{
    public static int Main(string[] args)
    {
        if (args.Length == 0)
        {
            PrintUsage();
            return 2;
        }

        string? runtime = Environment.GetEnvironmentVariable("OFIQ_RUNTIME_DIR");
        var images = new List<string>();
        var landmarks = false;
        var options = true;
        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            if (options && arg == "--") options = false;
            else if (options && (arg == "--help" || arg == "-h"))
            {
                PrintUsage();
                return 0;
            }
            else if (options && arg == "--landmarks") landmarks = true;
            else if (options && arg == "--runtime")
            {
                if (++i == args.Length || args[i].StartsWith("--", StringComparison.Ordinal))
                    return UsageError("--runtime requires a directory.");
                runtime = args[i];
            }
            else if (options && arg.StartsWith("-", StringComparison.Ordinal))
                return UsageError($"Unknown option: {arg}");
            else images.Add(arg);
        }

        if (images.Count == 0) return UsageError("Provide at least one image path.");
        foreach (var image in images)
            if (!File.Exists(image)) return UsageError($"Image not found: {image}");
        if (string.IsNullOrWhiteSpace(runtime))
            return UsageError("Set OFIQ_RUNTIME_DIR or use --runtime <directory>.");
        if (!OperatingSystem.IsWindows() || !Environment.Is64BitProcess)
            return UsageError("The OFIQ sample requires Windows and an x64 process.");

        try
        {
            // Initialisation loads the models; reuse this engine for all images.
            using var engine = OfiqEngine.Create(runtime);
            Console.WriteLine($"OFIQ {engine.NativeVersion}");
            var exitCode = 0;
            foreach (var image in images)
            {
                try
                {
                    var assessment = engine.Assess(File.ReadAllBytes(image), includeLandmarks: landmarks);
                    Console.WriteLine($"\n{image} ({assessment.ImageWidth} x {assessment.ImageHeight})");
                    Console.WriteLine($"Unified quality: {Format(assessment.UnifiedQualityScore)} / 100");
                    Console.WriteLine($"Assessed face: {assessment.Face}");
                    Console.WriteLine($"Assessment time: {assessment.Elapsed.TotalMilliseconds:F0} ms");
                    Console.WriteLine($"{' ',-2}{"Measure",-34} {"Raw",12} {"Scalar",10}  Status");
                    foreach (var measure in assessment.Measures)
                        Console.WriteLine($"  {measure.Name,-34} {Format(measure.RawScore),12} {Format(measure.Scalar),10}  {measure.Status}");
                    if (landmarks)
                    {
                        Console.WriteLine($"Faces: {assessment.Faces.Count}; landmarks: {assessment.Landmarks.Count}");
                        foreach (var point in assessment.Landmarks)
                            Console.WriteLine($"  ({point.X}, {point.Y})");
                    }
                }
                catch (Exception ex) when (ex is OfiqException or IOException or UnauthorizedAccessException)
                {
                    Console.Error.WriteLine($"{image}: {ex.Message}");
                    exitCode = 1;
                }
            }
            return exitCode;
        }
        catch (Exception ex) when (ex is OfiqException or IOException or UnauthorizedAccessException
            or ArgumentException or DllNotFoundException or EntryPointNotFoundException or BadImageFormatException)
        {
            Console.Error.WriteLine($"OFIQ setup failed: {ex.Message}");
            return 1;
        }
    }

    private static string Format(double value) => value.ToString("0.###", CultureInfo.InvariantCulture);

    private static int UsageError(string message)
    {
        Console.Error.WriteLine(message);
        PrintUsage();
        return 2;
    }

    private static void PrintUsage()
    {
        Console.WriteLine("Usage: CENT.Face.Quality.OFIQ.Sample [--runtime <directory>] [--landmarks] <image> [<image> ...]");
        Console.WriteLine("Runtime defaults to OFIQ_RUNTIME_DIR. Use -- before paths beginning with a dash.");
        Console.WriteLine("Exit codes: 0 success; 1 setup/assessment failed; 2 invalid arguments.");
    }
}
