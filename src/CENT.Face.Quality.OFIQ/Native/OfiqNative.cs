using System.Runtime.InteropServices;

namespace CENT.Face.Quality.OFIQ.Native;

/// <summary>
/// P/Invoke surface of OFIQ 1.2.0's C API (ofiq_libc.h / ofiq_structs_c.h), x64 only. The C API is its
/// own DLL, ofiq_c.dll, a thin layer over ofiq_lib.dll (the C++ library), which in turn needs onnxruntime.dll.
/// Struct layouts mirror the MSVC x64 ABI of the C headers: every <c>enum class</c> is a 32-bit int,
/// <c>size_t</c> is <see cref="UIntPtr"/>, C++ references are passed as pointers (<c>ref</c>).
/// Memory handed out by the library (image pixels, assessments, landmarks, faces) is owned by the
/// library and must be returned through the matching <c>OFIQFree*</c> call.
/// </summary>
internal static class OfiqNative
{
    public const string LibraryName = "ofiq_c.dll";
    public const string CoreLibraryName = "ofiq_lib.dll";
    public const string OnnxRuntimeName = "onnxruntime.dll";

    // ── Structs ───────────────────────────────────────────────────────────────

    /// <c>struct Image { uint16_t width; uint16_t height; uint8_t depth; uint8_t* data; }</c> → 16 bytes on x64.
    [StructLayout(LayoutKind.Sequential)]
    public struct Image
    {
        public ushort Width;
        public ushort Height;
        public byte Depth;
        public IntPtr Data;
    }

    /// <c>struct QualityMeasureResult { QualityMeasure measure; double rawScore; double scalar; QualityMeasureReturnCode code; }</c> → 32 bytes.
    [StructLayout(LayoutKind.Sequential)]
    public struct QualityMeasureResult
    {
        public int Measure;
        public double RawScore;
        public double Scalar;
        public int Code;
    }

    /// <c>struct BoundingBox { int16_t xleft, ytop, width, height; FaceDetectorType faceDetector; }</c> → 12 bytes.
    [StructLayout(LayoutKind.Sequential)]
    public struct BoundingBox
    {
        public short XLeft;
        public short YTop;
        public short Width;
        public short Height;
        public int FaceDetector;
    }

    /// <c>struct LandmarkPoint { int16_t x; int16_t y; }</c> → 4 bytes.
    [StructLayout(LayoutKind.Sequential)]
    public struct LandmarkPoint
    {
        public short X;
        public short Y;
    }

    /// <c>struct FaceLandmarks { LandmarkType type; LandmarkPoint* landmarks; int numLandmarks; }</c> → 24 bytes.
    [StructLayout(LayoutKind.Sequential)]
    public struct FaceLandmarks
    {
        public int Type;
        public IntPtr Landmarks;
        public int NumLandmarks;
    }

    /// <c>struct FaceImageQualityAssessment { QualityMeasureResult* qAssessments; size_t numAssessments; BoundingBox boundingBox; }</c> → 32 bytes.
    [StructLayout(LayoutKind.Sequential)]
    public struct FaceImageQualityAssessment
    {
        public IntPtr Assessments;
        public UIntPtr NumAssessments;
        public BoundingBox BoundingBox;
    }

    /// <c>struct PreprocessingResult { BoundingBox* faces; size_t numFaces; FaceLandmarks landmarks; Image segmentationMask, occlusionMask, landmarkedRegionMask; }</c> → 88 bytes.
    [StructLayout(LayoutKind.Sequential)]
    public struct PreprocessingResult
    {
        public IntPtr Faces;
        public UIntPtr NumFaces;
        public FaceLandmarks Landmarks;
        public Image SegmentationMask;
        public Image OcclusionMask;
        public Image LandmarkedRegionMask;
    }

    // ── Functions ─────────────────────────────────────────────────────────────

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true)]
    public static extern int OFIQInitialize(out IntPtr handle, string configDir, string configFileName);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int OFIQCleanup(ref IntPtr handle);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int OFIQReadImageFromByteArray(byte[] imageData, UIntPtr dataLength, ref Image image);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int OFIQFreeImage(ref Image image);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int OFIQScalarQuality(IntPtr handle, ref Image face, out double quality);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int OFIQVectorQuality(IntPtr handle, ref Image faceImage, ref FaceImageQualityAssessment qualityAssessment);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int OFIQVectorQualityExt(IntPtr handle, ref Image faceImage, ref FaceImageQualityAssessment qualityAssessment, ref PreprocessingResult preprocessingResult);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int OFIQFreeFaceImageQualityAssessment(ref FaceImageQualityAssessment qa);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int OFIQFreePreprocessingResult(ref PreprocessingResult preprocRes);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void OFIQGetVersion(out int major, out int minor, out int patch);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr OFIQGetQualityMeasureName(int measure);

    // ── Library loading (kernel32) ────────────────────────────────────────────
    // netstandard2.0 has no NativeLibrary, so the runtime folder is bound by pre-loading the three
    // DLLs (onnxruntime -> ofiq_lib -> ofiq_c) with LOAD_WITH_ALTERED_SEARCH_PATH; the DllImports above
    // then resolve to the already loaded module of the same name, wherever the host process lives.

    public const uint LOAD_WITH_ALTERED_SEARCH_PATH = 0x00000008;

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    public static extern IntPtr LoadLibraryEx(string lpFileName, IntPtr hFile, uint dwFlags);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    public static extern IntPtr GetModuleHandle(string lpModuleName);
}
