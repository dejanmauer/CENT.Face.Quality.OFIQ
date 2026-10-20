using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using CENT.Face.Quality.OFIQ.Native;

namespace CENT.Face.Quality.OFIQ;

/// <summary>
/// One initialised OFIQ instance: the nine models loaded, ready to assess images.
/// </summary>
/// <remarks>
/// <para>Create it once per process with <see cref="Create"/> and keep it: initialisation loads about
/// 400 MB of ONNX models and takes seconds. Assessment itself is CPU-bound and takes in the order of a
/// second per image, so it belongs on a worker, never on a UI thread.</para>
/// <para>Calls on one engine are serialised by an internal lock. OFIQ 1.1.0 made the library thread-safe,
/// but the ONNX sessions share CPU anyway and a single queue keeps memory bounded.</para>
/// <para>The runtime folder must hold <c>ofiq_c.dll</c>, <c>ofiq_lib.dll</c>, <c>onnxruntime.dll</c>, the config file
/// (default <c>ofiq_config.jaxn</c>) and the <c>models\</c> tree the config refers to. The whole process
/// shares one copy of the native library, so every engine in a process must come from the same folder.</para>
/// </remarks>
public sealed class OfiqEngine : IDisposable
{
    private static readonly object LoadLock = new();
    private static string? _loadedFrom;

    private readonly object _callLock = new();
    private IntPtr _handle;
    private bool _disposed;

    /// <summary>Folder the native library and models were loaded from.</summary>
    public string RuntimeDirectory { get; }

    /// <summary>Config file (inside <see cref="RuntimeDirectory"/>) this engine was initialised with.</summary>
    public string ConfigFileName { get; }

    /// <summary>Version of the loaded ofiq_lib.dll.</summary>
    public Version NativeVersion { get; }

    private OfiqEngine(IntPtr handle, string runtimeDirectory, string configFileName, Version nativeVersion)
    {
        _handle = handle;
        RuntimeDirectory = runtimeDirectory;
        ConfigFileName = configFileName;
        NativeVersion = nativeVersion;
    }

    /// <summary>Loads the native library from <paramref name="runtimeDirectory"/> (first call only) and initialises OFIQ.</summary>
    /// <exception cref="OfiqException">The folder is incomplete, the DLLs cannot be loaded, or OFIQ refuses the config/models.</exception>
    public static OfiqEngine Create(string runtimeDirectory, string configFileName = "ofiq_config.jaxn")
    {
        if (string.IsNullOrWhiteSpace(runtimeDirectory)) throw new ArgumentException("A runtime folder is required.", nameof(runtimeDirectory));
        runtimeDirectory = Path.GetFullPath(runtimeDirectory);

        var configPath = Path.Combine(runtimeDirectory, configFileName);
        if (!File.Exists(configPath))
            throw new OfiqException(OfiqReturnCode.MissingConfigFileError, $"OFIQ config file not found: {configPath}");

        EnsureLoaded(runtimeDirectory);

        OfiqNative.OFIQGetVersion(out var major, out var minor, out var patch);
        var version = new Version(major, minor, patch);

        // Model paths in the config ("models/…") resolve against the config folder
        // (Configuration::getDataDir() + "/" + modelPath), so the host's working directory does not matter.
        int rc;
        IntPtr handle;
        lock (LoadLock)
        {
            rc = OfiqNative.OFIQInitialize(out handle, runtimeDirectory, configFileName);
        }
        Check(rc, $"OFIQ {version} failed to initialise from {configPath}");
        if (handle == IntPtr.Zero)
            throw new OfiqException(OfiqReturnCode.UnknownError, "OFIQInitialize returned Success but no handle.");

        return new OfiqEngine(handle, runtimeDirectory, configFileName, version);
    }

    /// <summary>Decodes an encoded image (JPEG, PNG, BMP, JPEG 2000 … whatever OFIQ's OpenCV build reads) and assesses it.</summary>
    /// <param name="encodedImage">The file bytes.</param>
    /// <param name="includeLandmarks">Also return the detected faces and the 98 landmarks (slightly more marshalling, same compute).</param>
    public OfiqAssessment Assess(byte[] encodedImage, bool includeLandmarks = false)
    {
        if (encodedImage is null || encodedImage.Length == 0) throw new ArgumentException("An image is required.", nameof(encodedImage));
        ThrowIfDisposed();

        var image = new OfiqNative.Image();
        var rc = OfiqNative.OFIQReadImageFromByteArray(encodedImage, (UIntPtr)encodedImage.Length, ref image);
        Check(rc, "OFIQ could not decode the image");
        try
        {
            return AssessNative(ref image, includeLandmarks);
        }
        finally
        {
            OfiqNative.OFIQFreeImage(ref image);
        }
    }

    /// <summary>Assesses already decoded pixels (8-bit grey or 24-bit BGR, no row padding).</summary>
    public OfiqAssessment Assess(OfiqImage image, bool includeLandmarks = false)
    {
        if (image is null) throw new ArgumentNullException(nameof(image));
        ThrowIfDisposed();

        var pinned = GCHandle.Alloc(image.Pixels, GCHandleType.Pinned);
        try
        {
            var native = new OfiqNative.Image
            {
                Width = (ushort)image.Width,
                Height = (ushort)image.Height,
                Depth = (byte)image.Depth,
                Data = pinned.AddrOfPinnedObject(),
            };
            return AssessNative(ref native, includeLandmarks);
        }
        finally
        {
            pinned.Free();
        }
    }

    /// <summary>The unified quality score alone (0…100, -1 on failure), without the component vector.</summary>
    public double ScalarQuality(byte[] encodedImage)
    {
        if (encodedImage is null || encodedImage.Length == 0) throw new ArgumentException("An image is required.", nameof(encodedImage));
        ThrowIfDisposed();

        var image = new OfiqNative.Image();
        Check(OfiqNative.OFIQReadImageFromByteArray(encodedImage, (UIntPtr)encodedImage.Length, ref image), "OFIQ could not decode the image");
        try
        {
            lock (_callLock)
            {
                ThrowIfDisposed();
                Check(OfiqNative.OFIQScalarQuality(_handle, ref image, out var quality), "OFIQ scalar quality failed");
                return quality;
            }
        }
        finally
        {
            OfiqNative.OFIQFreeImage(ref image);
        }
    }

    /// <summary>OFIQ's own name for a measure (the spelling used in the config file and the conformance table).</summary>
    public static string MeasureName(OfiqMeasure measure)
    {
        EnsureLoadedForNames();
        var p = OfiqNative.OFIQGetQualityMeasureName((int)measure);
        return p == IntPtr.Zero ? measure.ToString() : Marshal.PtrToStringAnsi(p) ?? measure.ToString();
    }

    private OfiqAssessment AssessNative(ref OfiqNative.Image image, bool includeLandmarks)
    {
        var qa = new OfiqNative.FaceImageQualityAssessment();
        var pre = new OfiqNative.PreprocessingResult();
        var sw = Stopwatch.StartNew();
        lock (_callLock)
        {
            ThrowIfDisposed();
            try
            {
                var rc = includeLandmarks
                    ? OfiqNative.OFIQVectorQualityExt(_handle, ref image, ref qa, ref pre)
                    : OfiqNative.OFIQVectorQuality(_handle, ref image, ref qa);
                sw.Stop();
                Check(rc, "OFIQ assessment failed");

                var measures = ReadMeasures(qa);
                var face = qa.BoundingBox.Width > 0 && qa.BoundingBox.Height > 0
                    ? new OfiqBoundingBox(qa.BoundingBox.XLeft, qa.BoundingBox.YTop, qa.BoundingBox.Width, qa.BoundingBox.Height)
                    : null;

                IReadOnlyList<OfiqBoundingBox> faces = Array.Empty<OfiqBoundingBox>();
                IReadOnlyList<OfiqPoint> landmarks = Array.Empty<OfiqPoint>();
                if (includeLandmarks)
                {
                    faces = ReadFaces(pre);
                    landmarks = ReadLandmarks(pre.Landmarks);
                }

                return new OfiqAssessment(measures, face, faces, landmarks, image.Width, image.Height, sw.Elapsed);
            }
            finally
            {
                if (qa.Assessments != IntPtr.Zero) OfiqNative.OFIQFreeFaceImageQualityAssessment(ref qa);
                if (includeLandmarks) OfiqNative.OFIQFreePreprocessingResult(ref pre);
            }
        }
    }

    private static IReadOnlyList<OfiqMeasureResult> ReadMeasures(in OfiqNative.FaceImageQualityAssessment qa)
    {
        var count = (int)qa.NumAssessments.ToUInt64();
        if (count <= 0 || qa.Assessments == IntPtr.Zero) return Array.Empty<OfiqMeasureResult>();

        var size = Marshal.SizeOf<OfiqNative.QualityMeasureResult>();
        var list = new OfiqMeasureResult[count];
        for (var i = 0; i < count; i++)
        {
            var r = Marshal.PtrToStructure<OfiqNative.QualityMeasureResult>(qa.Assessments + i * size);
            list[i] = new OfiqMeasureResult((OfiqMeasure)r.Measure, r.RawScore, r.Scalar, (OfiqMeasureStatus)r.Code);
        }
        return list;
    }

    private static IReadOnlyList<OfiqBoundingBox> ReadFaces(in OfiqNative.PreprocessingResult pre)
    {
        var count = (int)pre.NumFaces.ToUInt64();
        if (count <= 0 || pre.Faces == IntPtr.Zero) return Array.Empty<OfiqBoundingBox>();

        var size = Marshal.SizeOf<OfiqNative.BoundingBox>();
        var list = new OfiqBoundingBox[count];
        for (var i = 0; i < count; i++)
        {
            var b = Marshal.PtrToStructure<OfiqNative.BoundingBox>(pre.Faces + i * size);
            list[i] = new OfiqBoundingBox(b.XLeft, b.YTop, b.Width, b.Height);
        }
        return list;
    }

    private static IReadOnlyList<OfiqPoint> ReadLandmarks(in OfiqNative.FaceLandmarks lm)
    {
        if (lm.NumLandmarks <= 0 || lm.Landmarks == IntPtr.Zero) return Array.Empty<OfiqPoint>();

        var size = Marshal.SizeOf<OfiqNative.LandmarkPoint>();
        var list = new OfiqPoint[lm.NumLandmarks];
        for (var i = 0; i < lm.NumLandmarks; i++)
        {
            var p = Marshal.PtrToStructure<OfiqNative.LandmarkPoint>(lm.Landmarks + i * size);
            list[i] = new OfiqPoint(p.X, p.Y);
        }
        return list;
    }

    // ── Native library binding ────────────────────────────────────────────────

    private static void EnsureLoaded(string runtimeDirectory)
    {
        lock (LoadLock)
        {
            if (_loadedFrom != null)
            {
                if (!string.Equals(_loadedFrom, runtimeDirectory, StringComparison.OrdinalIgnoreCase))
                    throw new OfiqException(OfiqReturnCode.UnknownError,
                        $"OFIQ is already loaded from {_loadedFrom}; a process can use one OFIQ runtime folder only.");
                return;
            }

            if (IntPtr.Size != 8)
                throw new OfiqException(OfiqReturnCode.UnknownError, "OFIQ ships as x64 only; the host process is 32-bit.");

            var onnx = Path.Combine(runtimeDirectory, OfiqNative.OnnxRuntimeName);
            var core = Path.Combine(runtimeDirectory, OfiqNative.CoreLibraryName);
            var lib = Path.Combine(runtimeDirectory, OfiqNative.LibraryName);
            if (!File.Exists(onnx)) throw new OfiqException(OfiqReturnCode.UnknownError, $"Missing {onnx}");
            if (!File.Exists(core)) throw new OfiqException(OfiqReturnCode.UnknownError, $"Missing {core}");
            if (!File.Exists(lib)) throw new OfiqException(OfiqReturnCode.UnknownError, $"Missing {lib}");
            if (!Directory.Exists(Path.Combine(runtimeDirectory, "models")))
                throw new OfiqException(OfiqReturnCode.MissingConfigParamError, $"Missing models folder under {runtimeDirectory}");

            Load(onnx);
            Load(core);
            Load(lib);
            _loadedFrom = runtimeDirectory;
        }
    }

    private static void EnsureLoadedForNames()
    {
        lock (LoadLock)
        {
            if (_loadedFrom != null) return;
        }
        throw new OfiqException(OfiqReturnCode.UnknownError, "Create an OfiqEngine before asking for measure names: the native library is not loaded.");
    }

    private static void Load(string path)
    {
        var h = OfiqNative.LoadLibraryEx(path, IntPtr.Zero, OfiqNative.LOAD_WITH_ALTERED_SEARCH_PATH);
        if (h != IntPtr.Zero) return;
        var error = Marshal.GetLastWin32Error();
        throw new OfiqException(OfiqReturnCode.UnknownError,
            $"Could not load {path} (Win32 error {error}: {new Win32Exception(error).Message}). " +
            "On a clean machine this usually means the Visual C++ 2019+ x64 redistributable is missing.",
            new Win32Exception(error));
    }

    private static void Check(int rc, string what)
    {
        if (rc == (int)OfiqReturnCode.Success) return;
        var code = Enum.IsDefined(typeof(OfiqReturnCode), rc) ? (OfiqReturnCode)rc : OfiqReturnCode.UnknownError;
        throw new OfiqException(code, $"{what}: {code} ({rc}).");
    }

    private void ThrowIfDisposed()
    {
        if (_disposed) throw new ObjectDisposedException(nameof(OfiqEngine));
    }

    public void Dispose()
    {
        lock (_callLock)
        {
            if (_disposed) return;
            _disposed = true;
            if (_handle != IntPtr.Zero)
            {
                OfiqNative.OFIQCleanup(ref _handle);
                _handle = IntPtr.Zero;
            }
        }
    }
}
