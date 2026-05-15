namespace CK.H3;

/// <summary>Error codes returned by H3 library functions.</summary>
public enum H3ErrorCode : uint
{
    /// <summary>No error.</summary>
    Success = 0,

    /// <summary>The operation failed but a more specific error is not available.</summary>
    Failed = 1,

    /// <summary>Argument was outside of acceptable range (argument domain error).</summary>
    Domain = 2,

    /// <summary>Latitude or longitude arguments were outside of acceptable range.</summary>
    LatLngDomain = 3,

    /// <summary>Resolution argument was outside of acceptable range.</summary>
    ResDomain = 4,

    /// <summary>H3Index cell argument was not valid.</summary>
    CellInvalid = 5,

    /// <summary>H3Index directed edge argument was not valid.</summary>
    DirEdgeInvalid = 6,

    /// <summary>H3Index undirected edge argument was not valid.</summary>
    UndirEdgeInvalid = 7,

    /// <summary>H3Index vertex argument was not valid.</summary>
    VertexInvalid = 8,

    /// <summary>Pentagon distortion was encountered which the algorithm could not handle.</summary>
    Pentagon = 9,

    /// <summary>Duplicate input was encountered in the arguments.</summary>
    DuplicateInput = 10,

    /// <summary>H3Index cell arguments were not neighbors.</summary>
    NotNeighbors = 11,

    /// <summary>H3Index cell arguments had incompatible resolutions.</summary>
    ResMismatch = 12,

    /// <summary>Necessary memory allocation failed.</summary>
    MemoryAlloc = 13,

    /// <summary>Bounds of provided memory were not large enough.</summary>
    MemoryBounds = 14,

    /// <summary>Mode or flags argument was not valid.</summary>
    OptionInvalid = 15,

    /// <summary>H3Index argument was not valid.</summary>
    IndexInvalid = 16,

    /// <summary>Base cell number was outside of acceptable range.</summary>
    BaseCellDomain = 17,

    /// <summary>Child indexing digits are invalid.</summary>
    DigitDomain = 18,

    /// <summary>Child indexing digits refer to a deleted subsequence.</summary>
    DeletedDigit = 19,
}
