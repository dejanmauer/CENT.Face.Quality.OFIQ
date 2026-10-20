namespace CENT.Face.Quality.OFIQ;

/// <summary>Raised when the OFIQ native library reports anything other than <see cref="OfiqReturnCode.Success"/>,
/// or when the runtime folder cannot be loaded.</summary>
public sealed class OfiqException : Exception
{
    public OfiqReturnCode Code { get; }

    public OfiqException(OfiqReturnCode code, string message) : base(message) => Code = code;

    public OfiqException(OfiqReturnCode code, string message, Exception inner) : base(message, inner) => Code = code;
}
