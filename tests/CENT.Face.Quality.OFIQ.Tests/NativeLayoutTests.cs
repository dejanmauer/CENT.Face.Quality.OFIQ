using System.Runtime.InteropServices;
using CENT.Face.Quality.OFIQ;
using CENT.Face.Quality.OFIQ.Native;
using Xunit;

/// <summary>
/// The marshalled structs must match the MSVC x64 layout of ofiq_structs_c.h byte for byte, or every
/// field after the first mismatch reads garbage. These sizes were derived from the headers of OFIQ 1.2.0
/// (enum class = int, size_t = 8, natural alignment) and need no native library to check.
/// </summary>
public class NativeLayoutTests
{
    [Fact] public void Image_is_16_bytes()                      => Assert.Equal(16, Marshal.SizeOf<OfiqNative.Image>());
    [Fact] public void QualityMeasureResult_is_32_bytes()       => Assert.Equal(32, Marshal.SizeOf<OfiqNative.QualityMeasureResult>());
    [Fact] public void BoundingBox_is_12_bytes()                => Assert.Equal(12, Marshal.SizeOf<OfiqNative.BoundingBox>());
    [Fact] public void LandmarkPoint_is_4_bytes()               => Assert.Equal(4,  Marshal.SizeOf<OfiqNative.LandmarkPoint>());
    [Fact] public void FaceLandmarks_is_24_bytes()              => Assert.Equal(24, Marshal.SizeOf<OfiqNative.FaceLandmarks>());
    [Fact] public void FaceImageQualityAssessment_is_32_bytes() => Assert.Equal(32, Marshal.SizeOf<OfiqNative.FaceImageQualityAssessment>());
    [Fact] public void PreprocessingResult_is_88_bytes()        => Assert.Equal(88, Marshal.SizeOf<OfiqNative.PreprocessingResult>());

    [Fact]
    public void Image_data_pointer_sits_at_offset_8()
    {
        Assert.Equal((IntPtr)8, Marshal.OffsetOf<OfiqNative.Image>(nameof(OfiqNative.Image.Data)));
    }

    [Fact]
    public void QualityMeasureResult_doubles_sit_at_8_and_16()
    {
        Assert.Equal((IntPtr)8,  Marshal.OffsetOf<OfiqNative.QualityMeasureResult>(nameof(OfiqNative.QualityMeasureResult.RawScore)));
        Assert.Equal((IntPtr)16, Marshal.OffsetOf<OfiqNative.QualityMeasureResult>(nameof(OfiqNative.QualityMeasureResult.Scalar)));
        Assert.Equal((IntPtr)24, Marshal.OffsetOf<OfiqNative.QualityMeasureResult>(nameof(OfiqNative.QualityMeasureResult.Code)));
    }

    [Fact]
    public void Measure_ids_match_the_standard()
    {
        // ISO/IEC 29794-5 quality component identifiers, as OFIQ numbers them.
        Assert.Equal(0x41, (int)OfiqMeasure.UnifiedQualityScore);
        Assert.Equal(0x49, (int)OfiqMeasure.Sharpness);
        Assert.Equal(0x52, (int)OfiqMeasure.InterEyeDistance);
        Assert.Equal(0x58, (int)OfiqMeasure.HeadPoseYaw);
        Assert.Equal(0x5c, (int)OfiqMeasure.NoHeadCoverings);
        Assert.Equal(-0x58, (int)OfiqMeasure.HeadPose);
    }

    [Fact]
    public void OfiqImage_rejects_a_pixel_buffer_of_the_wrong_size()
    {
        Assert.Throws<ArgumentException>(() => new OfiqImage(4, 4, 24, new byte[4 * 4 * 3 - 1]));
        Assert.Throws<ArgumentOutOfRangeException>(() => new OfiqImage(4, 4, 32, new byte[4 * 4 * 4]));
        _ = new OfiqImage(4, 4, 8, new byte[16]);
    }
}
