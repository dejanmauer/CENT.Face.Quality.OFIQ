namespace CENT.Face.Quality.OFIQ;

/// <summary>Return codes of the OFIQ C API (ofiq_structs_c.h, <c>ReturnCode</c>). Integer values match 1.2.0.</summary>
public enum OfiqReturnCode
{
    Success = 0,
    ImageReadingError = 1,
    ImageWritingError = 2,
    MissingConfigFileError = 3,
    MissingConfigParamError = 4,
    UnknownConfigParamError = 5,
    FaceDetectionError = 6,
    FaceLandmarkExtractionError = 7,
    FaceOcclusionSegmentationError = 8,
    FaceParsingError = 9,
    UnknownError = 10,
    QualityAssessmentError = 11,
    NotImplemented = 12,
}

/// <summary>
/// Quality measures of ISO/IEC 29794-5 as numbered by OFIQ (<c>QualityMeasure</c> in ofiq_structs_c.h).
/// The positive values are the quality component identifiers of the standard (0x41 … 0x5c); the negative
/// ones are OFIQ's "common" measures that only appear in the configuration file and expand to several
/// components (Luminance → mean + variance, CropOfTheFaceImage → four margins, HeadPose → yaw/pitch/roll).
/// </summary>
public enum OfiqMeasure
{
    UnifiedQualityScore = 0x41,
    BackgroundUniformity = 0x42,
    IlluminationUniformity = 0x43,
    Luminance = -0x44,
    LuminanceMean = 0x44,
    LuminanceVariance = 0x45,
    UnderExposurePrevention = 0x46,
    OverExposurePrevention = 0x47,
    DynamicRange = 0x48,
    Sharpness = 0x49,
    CompressionArtifacts = 0x4a,
    NaturalColour = 0x4b,
    SingleFacePresent = 0x4c,
    EyesOpen = 0x4d,
    MouthClosed = 0x4e,
    EyesVisible = 0x4f,
    MouthOcclusionPrevention = 0x50,
    FaceOcclusionPrevention = 0x51,
    InterEyeDistance = 0x52,
    HeadSize = 0x53,
    CropOfTheFaceImage = -0x54,
    LeftwardCropOfTheFaceImage = 0x54,
    RightwardCropOfTheFaceImage = 0x55,
    MarginAboveOfTheFaceImage = 0x56,
    MarginBelowOfTheFaceImage = 0x57,
    HeadPose = -0x58,
    HeadPoseYaw = 0x58,
    HeadPosePitch = 0x59,
    HeadPoseRoll = 0x5a,
    ExpressionNeutrality = 0x5b,
    NoHeadCoverings = 0x5c,
    NotSet = -1,
}

/// <summary>Per-measure status (<c>QualityMeasureReturnCode</c>).</summary>
public enum OfiqMeasureStatus
{
    Success = 0,
    FailureToAssess = 1,
    NotInitialized = 2,
}
