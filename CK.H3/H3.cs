namespace CK.H3;

/// <summary>Global H3 library functions that are not associated with a specific cell index.</summary>
public static class H3
{
    /// <summary>
    /// Returns the total number of unique cells at the given resolution.
    /// Values range from 122 at resolution 0 to approximately 569 trillion at resolution 15.
    /// </summary>
    /// <param name="resolution">H3 resolution (0–15).</param>
    public static long GetNumCells( int resolution )
    {
        H3Native.getNumCells( resolution, out long count ).ThrowIfError();
        return count;
    }

    /// <summary>
    /// Returns all 122 resolution-0 base cells.
    /// </summary>
    public static unsafe H3Index[] GetRes0Cells()
    {
        int count = H3Native.res0CellCount();
        var buf = new H3Index[count];
        fixed( H3Index* p = buf )
        {
            H3Native.getRes0Cells( (ulong*)p ).ThrowIfError();
        }
        return buf;
    }

    /// <summary>
    /// Returns all 12 pentagon cells at the given resolution.
    /// One pentagon exists per icosahedron vertex per resolution.
    /// </summary>
    /// <param name="resolution">H3 resolution (0–15).</param>
    public static unsafe H3Index[] GetPentagons( int resolution )
    {
        int count = H3Native.pentagonCount();
        var buf = new H3Index[count];
        fixed( H3Index* p = buf )
        {
            H3Native.getPentagons( resolution, (ulong*)p ).ThrowIfError();
        }
        return buf;
    }

    /// <summary>
    /// Returns the maximum number of icosahedron faces the given cell may intersect.
    /// Most cells intersect 1 face; cells near face boundaries may intersect 2.
    /// </summary>
    public static int GetNumIcosahedronFaces( H3Index cell )
    {
        H3Native.maxFaceCount( cell, out int count ).ThrowIfError();
        return count;
    }
}
