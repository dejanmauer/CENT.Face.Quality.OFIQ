namespace CENT.Face.Quality.OFIQ;

/// <summary>One quality component of the assessment.</summary>
/// <param name="Measure">Which component.</param>
/// <param name="RawScore">The native value of the component (its unit depends on the component:
/// degrees for head pose, pixels for inter-eye distance, a ratio for the margins, …). -1 when not assessed.</param>
/// <param name="Scalar">The quality component value mapped to 0…100 as the standard defines, higher is better.
/// -1 when the component failed to assess.</param>
/// <param name="Status">Whether the component was computed.</param>
public sealed record OfiqMeasureResult(OfiqMeasure Measure, double RawScore, double Scalar, OfiqMeasureStatus Status)
{
    /// <summary>The component name as OFIQ prints it (same spelling as the config file and the conformance table).</summary>
    public string Name => Measure.ToString();
}

/// <summary>Axis-aligned face box in image pixels.</summary>
public sealed record OfiqBoundingBox(int X, int Y, int Width, int Height);

/// <summary>A landmark point in image pixels.</summary>
public readonly record struct OfiqPoint(int X, int Y);

/// <summary>Result of one OFIQ assessment of one image.</summary>
public sealed class OfiqAssessment
{
    /// <summary>The unified quality score 0…100 (component 0x41), or -1 when it was not computed.</summary>
    public double UnifiedQualityScore { get; }

    /// <summary>All components the active configuration produced, in OFIQ's order.</summary>
    public IReadOnlyList<OfiqMeasureResult> Measures { get; }

    /// <summary>The face the assessment is about (the largest detected face). Null when no face was reported.</summary>
    public OfiqBoundingBox? Face { get; }

    /// <summary>Every face the detector found (only when landmarks were requested). Empty otherwise.</summary>
    public IReadOnlyList<OfiqBoundingBox> Faces { get; }

    /// <summary>The 98 ADNet landmarks of the assessed face (only when requested). Empty otherwise.</summary>
    public IReadOnlyList<OfiqPoint> Landmarks { get; }

    public int ImageWidth { get; }
    public int ImageHeight { get; }

    /// <summary>Wall-clock time of the native assessment (decoding excluded).</summary>
    public TimeSpan Elapsed { get; }

    public OfiqAssessment(
        IReadOnlyList<OfiqMeasureResult> measures,
        OfiqBoundingBox? face,
        IReadOnlyList<OfiqBoundingBox> faces,
        IReadOnlyList<OfiqPoint> landmarks,
        int imageWidth, int imageHeight,
        TimeSpan elapsed)
    {
        Measures = measures;
        Face = face;
        Faces = faces;
        Landmarks = landmarks;
        ImageWidth = imageWidth;
        ImageHeight = imageHeight;
        Elapsed = elapsed;
        UnifiedQualityScore = -1;
        foreach (var m in measures)
        {
            if (m.Measure == OfiqMeasure.UnifiedQualityScore && m.Status == OfiqMeasureStatus.Success)
            {
                UnifiedQualityScore = m.Scalar;
                break;
            }
        }
    }

    /// <summary>Looks a component up by measure; null when the configuration did not produce it.</summary>
    public OfiqMeasureResult? this[OfiqMeasure measure]
    {
        get
        {
            foreach (var m in Measures)
                if (m.Measure == measure) return m;
            return null;
        }
    }
}

/// <summary>A decoded image as OFIQ consumes it: 8-bit grey or 24-bit BGR, rows top-down, no padding.</summary>
public sealed class OfiqImage
{
    public int Width { get; }
    public int Height { get; }
    /// <summary>8 (grey) or 24 (BGR).</summary>
    public int Depth { get; }
    /// <summary>Width × Height × (Depth / 8) bytes.</summary>
    public byte[] Pixels { get; }

    public OfiqImage(int width, int height, int depth, byte[] pixels)
    {
        if (width <= 0 || width > ushort.MaxValue) throw new ArgumentOutOfRangeException(nameof(width));
        if (height <= 0 || height > ushort.MaxValue) throw new ArgumentOutOfRangeException(nameof(height));
        if (depth != 8 && depth != 24) throw new ArgumentOutOfRangeException(nameof(depth), "OFIQ accepts 8 (grey) or 24 (BGR) bits per pixel.");
        if (pixels is null) throw new ArgumentNullException(nameof(pixels));
        var expected = (long)width * height * (depth / 8);
        if (pixels.LongLength != expected)
            throw new ArgumentException($"Expected {expected} bytes for {width}x{height}x{depth / 8}, got {pixels.LongLength}.", nameof(pixels));
        Width = width; Height = height; Depth = depth; Pixels = pixels;
    }
}
