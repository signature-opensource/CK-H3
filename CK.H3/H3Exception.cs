using System;

namespace CK.H3;

/// <summary>Thrown when an H3 library call returns a non-success error code.</summary>
public sealed class H3Exception : Exception
{
    /// <summary>The H3 error code returned by the native function.</summary>
    public H3ErrorCode ErrorCode { get; }

    /// <summary>
    /// Initializes a new instance of <see cref="H3Exception"/> with the given error code.
    /// </summary>
    public H3Exception( H3ErrorCode code )
        : base( $"H3 error: {code}" )
    {
        ErrorCode = code;
    }
}
