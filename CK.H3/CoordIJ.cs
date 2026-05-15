namespace CK.H3;

/// <summary>
/// Local IJ coordinates for an H3 cell relative to an origin cell.
/// The I and J axes are not orthogonal — they are at a 120-degree angle.
/// These coordinates are only meaningful within a single H3 base cell face.
/// </summary>
public readonly struct CoordIJ
{
    /// <summary>I coordinate.</summary>
    public int I { get; }

    /// <summary>J coordinate.</summary>
    public int J { get; }

    /// <summary>Initializes a new <see cref="CoordIJ"/> with the given coordinates.</summary>
    public CoordIJ( int i, int j )
    {
        I = i;
        J = j;
    }

    /// <summary>
    /// Converts these local IJ coordinates back to an H3 cell index,
    /// using <paramref name="origin"/> as the reference frame.
    /// </summary>
    /// <param name="origin">The origin cell that defines the local coordinate frame.</param>
    public H3Index ToCell( H3Index origin )
    {
        var native = ToNative();
        H3Native.localIjToCell( origin, in native, 0, out ulong cell ).ThrowIfError();
        return cell;
    }

    internal CoordIJNative ToNative() => new() { i = I, j = J };

    internal static CoordIJ FromNative( CoordIJNative n ) => new( n.i, n.j );

    /// <inheritdoc/>
    public override string ToString() => $"({I}, {J})";
}
