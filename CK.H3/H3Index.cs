using System;
using System.Collections.Generic;
using System.Globalization;

namespace CK.H3;

/// <summary>
/// An H3 index. Represents a cell, a directed edge, or a vertex in the H3 hierarchical
/// hexagonal geospatial indexing system. The underlying value is a 64-bit unsigned integer.
/// Implicit conversions to and from <see cref="ulong"/> are provided for interoperability.
/// </summary>
public readonly struct H3Index : IEquatable<H3Index>, IComparable<H3Index>, IFormattable
{
    readonly ulong _value;

    /// <summary>The invalid H3 index (value 0). Returned by functions that produce no valid result.</summary>
    public static readonly H3Index Invalid = new( 0 );

    H3Index( ulong value ) => _value = value;

    // ── Implicit conversions ──────────────────────────────────────────────────

    /// <summary>Implicitly converts an <see cref="H3Index"/> to its underlying <see cref="ulong"/> value.</summary>
    public static implicit operator ulong( H3Index h ) => h._value;

    /// <summary>Implicitly converts a <see cref="ulong"/> to an <see cref="H3Index"/>.</summary>
    public static implicit operator H3Index( ulong v ) => new( v );

    // ── Inspection ────────────────────────────────────────────────────────────

    /// <summary>
    /// Gets a value indicating whether this index represents a valid H3 cell, directed edge, or vertex.
    /// </summary>
    public bool IsValid => H3Native.isValidCell( _value ) != 0;

    /// <summary>Gets a value indicating whether this cell is one of the 12 pentagons at its resolution.</summary>
    public bool IsPentagon => H3Native.isPentagon( _value ) != 0;

    /// <summary>Gets the H3 resolution of this index (0–15).</summary>
    public int Resolution => H3Native.getResolution( _value );

    /// <summary>Gets the base cell number of this index (0–121).</summary>
    public int BaseCellNumber => H3Native.getBaseCellNumber( _value );

    /// <summary>Gets the resolution res integer digit (0-7) of this index.</summary>
    public int ResolutionResDigit => (int)((_value >> ((_maxH3Res - Resolution) * _h3DigitOffset)) & _h3DigitMask);

    /// <summary>Max H3 resolution; H3 version 1 has 16 resolutions, numbered 0 through 15.</summary>
    const int _maxH3Res = 15;
    /// <summary>The number of bits in a single H3 resolution digit.</summary>
    const int _h3DigitOffset = 3;
    /// <summary>1's in the 3 bits of res 15 digit bits, 0's everywhere else.</summary>
    const ulong _h3DigitMask = 7;

    // ── Cell indexing ─────────────────────────────────────────────────────────

    /// <summary>
    /// Returns the H3 cell index containing the given latitude/longitude at the specified resolution.
    /// </summary>
    /// <param name="lat">Latitude in degrees.</param>
    /// <param name="lng">Longitude in degrees.</param>
    /// <param name="resolution">H3 resolution (0–15).</param>
    public static H3Index FromLatLng( double lat, double lng, int resolution )
    {
        var g = new LatLng( lat, lng ).ToNative();
        H3Native.latLngToCell( in g, resolution, out ulong cell ).ThrowIfError();
        return cell;
    }

    /// <summary>
    /// Returns the geographic center of this cell as latitude/longitude coordinates in degrees.
    /// </summary>
    public LatLng ToLatLng()
    {
        H3Native.cellToLatLng( _value, out LatLngNative g ).ThrowIfError();
        return LatLng.FromNative( g );
    }

    /// <summary>Returns the boundary polygon of this cell.</summary>
    public unsafe CellBoundary ToBoundary()
    {
        H3Native.cellToBoundary( _value, out CellBoundaryNative bndry ).ThrowIfError();
        return CellBoundary.FromNative( in bndry );
    }

    // ── Hierarchy ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns the parent cell of this cell at the given resolution.
    /// </summary>
    /// <param name="resolution">The target parent resolution (must be ≤ this cell's resolution).</param>
    public H3Index ToParent( int resolution )
    {
        H3Native.cellToParent( _value, resolution, out ulong parent ).ThrowIfError();
        return parent;
    }

    /// <summary>
    /// Returns all children of this cell at the given resolution.
    /// </summary>
    /// <param name="resolution">The target child resolution (must be ≥ this cell's resolution).</param>
    public unsafe H3Index[] ToChildren( int resolution )
    {
        H3Native.cellToChildrenSize( _value, resolution, out long count ).ThrowIfError();
        var buf = new H3Index[count];
        fixed( H3Index* p = buf )
        {
            H3Native.cellToChildren( _value, resolution, (ulong*)p ).ThrowIfError();
        }
        return buf;
    }

    /// <summary>
    /// Returns the center child of this cell at the given resolution.
    /// The center child is the child whose center is closest to this cell's center.
    /// </summary>
    /// <param name="resolution">The target child resolution (must be ≥ this cell's resolution).</param>
    public H3Index ToCenterChild( int resolution )
    {
        H3Native.cellToCenterChild( _value, resolution, out ulong child ).ThrowIfError();
        return child;
    }

    /// <summary>
    /// Compacts the given set of cells as efficiently as possible, replacing groups of children
    /// with their parent wherever all children at a given resolution are present.
    /// </summary>
    /// <param name="cells">The cells to compact. All cells must be at the same resolution.</param>
    public static unsafe H3Index[] Compact( IEnumerable<H3Index> cells )
    {
        var list = new List<H3Index>( cells );
        if( list.Count == 0 ) return [];
        H3Index[] input = list.ToArray();
        var output = new H3Index[input.Length];
        fixed( H3Index* pIn = input )
        fixed( H3Index* pOut = output )
        {
            H3Native.compactCells( (ulong*)pIn, (ulong*)pOut, input.Length ).ThrowIfError();
        }
        var result = new List<H3Index>( input.Length );
        foreach( var h in output )
        {
            if( (ulong)h != 0 ) result.Add( h );
        }
        return result.ToArray();
    }

    /// <summary>
    /// Uncompacts this (possibly coarser) cell to all cells at the given finer resolution.
    /// </summary>
    /// <param name="resolution">The target resolution (must be ≥ this cell's resolution).</param>
    public unsafe H3Index[] Uncompact( int resolution )
    {
        var input = new H3Index[] { this };
        fixed( H3Index* pIn = input )
        {
            H3Native.uncompactCellsSize( (ulong*)pIn, 1, resolution, out long count ).ThrowIfError();
            var output = new H3Index[count];
            fixed( H3Index* pOut = output )
            {
                H3Native.uncompactCells( (ulong*)pIn, 1, (ulong*)pOut, count, resolution ).ThrowIfError();
            }
            return output;
        }
    }

    // ── Traversal ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns all cells within grid distance <paramref name="k"/> of this cell, including this cell.
    /// </summary>
    /// <param name="k">The grid distance radius (0 returns only this cell).</param>
    public unsafe H3Index[] GridDisk( int k )
    {
        H3Native.maxGridDiskSize( k, out long count ).ThrowIfError();
        var buf = new H3Index[count];
        fixed( H3Index* p = buf )
        {
            H3Native.gridDisk( _value, k, (ulong*)p ).ThrowIfError();
        }
        return buf;
    }

    /// <summary>
    /// Returns all cells at exactly grid distance <paramref name="k"/> from this cell (the ring at distance k).
    /// Returns only this cell when k is 0. May throw <see cref="H3Exception"/> with
    /// <see cref="H3ErrorCode.Pentagon"/> if the ring crosses a pentagon.
    /// </summary>
    /// <param name="k">The grid distance (0 returns only this cell).</param>
    public unsafe H3Index[] GridRing( int k )
    {
        if( k == 0 ) return [this];
        var buf = new H3Index[6 * k];
        fixed( H3Index* p = buf )
        {
            H3Native.gridRingUnsafe( _value, k, (ulong*)p ).ThrowIfError();
        }
        return buf;
    }

    /// <summary>
    /// Returns the cells on the shortest path between this cell and <paramref name="destination"/>,
    /// inclusive of both endpoints.
    /// </summary>
    public unsafe H3Index[] GridPathTo( H3Index destination )
    {
        H3Native.gridPathCellsSize( _value, destination, out long count ).ThrowIfError();
        var buf = new H3Index[count];
        fixed( H3Index* p = buf )
        {
            H3Native.gridPathCells( _value, destination, (ulong*)p ).ThrowIfError();
        }
        return buf;
    }

    /// <summary>
    /// Returns the grid distance (number of cells) between this cell and <paramref name="other"/>.
    /// </summary>
    public long GridDistanceTo( H3Index other )
    {
        H3Native.gridDistance( _value, other, out long distance ).ThrowIfError();
        return distance;
    }

    // ── Edges ─────────────────────────────────────────────────────────────────

    /// <summary>Returns <see langword="true"/> if this cell and <paramref name="other"/> share an edge.</summary>
    public bool IsNeighborWith( H3Index other )
    {
        H3Native.areNeighborCells( _value, other, out int result ).ThrowIfError();
        return result != 0;
    }

    /// <summary>
    /// Returns the directed edge index from this cell to the neighboring <paramref name="neighbor"/> cell.
    /// </summary>
    /// <exception cref="H3Exception">Thrown with <see cref="H3ErrorCode.NotNeighbors"/> if the cells are not neighbors.</exception>
    public H3Index DirectedEdgeTo( H3Index neighbor )
    {
        H3Native.cellsToDirectedEdge( _value, neighbor, out ulong edge ).ThrowIfError();
        return edge;
    }

    /// <summary>
    /// Returns the origin and destination cells of this directed edge.
    /// </summary>
    public unsafe (H3Index origin, H3Index destination) DirectedEdgeToCells()
    {
        ulong* buf = stackalloc ulong[2];
        H3Native.directedEdgeToCells( _value, buf ).ThrowIfError();
        return (buf[0], buf[1]);
    }

    /// <summary>
    /// Returns all directed edges originating from this cell (up to 6; pentagons have 5).
    /// </summary>
    public unsafe H3Index[] GetDirectedEdges()
    {
        ulong* buf = stackalloc ulong[6];
        H3Native.originToDirectedEdges( _value, buf ).ThrowIfError();
        var result = new List<H3Index>( 6 );
        for( int i = 0; i < 6; i++ )
        {
            if( buf[i] != 0 ) result.Add( buf[i] );
        }
        return result.ToArray();
    }

    // ── Vertexes ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns the H3 vertex index for the given vertex number of this cell.
    /// </summary>
    /// <param name="vertexNum">Vertex number (0–5 for hexagons, 0–4 for pentagons).</param>
    public H3Index GetVertex( int vertexNum )
    {
        H3Native.cellToVertex( _value, vertexNum, out ulong vertex ).ThrowIfError();
        return vertex;
    }

    /// <summary>
    /// Returns all H3 vertex indexes for this cell (6 for hexagons, 5 for pentagons).
    /// </summary>
    public unsafe H3Index[] GetVertexes()
    {
        ulong* buf = stackalloc ulong[6];
        H3Native.cellToVertexes( _value, buf ).ThrowIfError();
        var result = new List<H3Index>( 6 );
        for( int i = 0; i < 6; i++ )
        {
            if( buf[i] != 0 ) result.Add( buf[i] );
        }
        return result.ToArray();
    }

    /// <summary>
    /// Returns the geographic position of this vertex as latitude/longitude in degrees.
    /// This cell must be a vertex index (obtained via <see cref="GetVertex"/> or <see cref="GetVertexes"/>).
    /// </summary>
    public LatLng VertexToLatLng()
    {
        H3Native.vertexToLatLng( _value, out LatLngNative point ).ThrowIfError();
        return LatLng.FromNative( point );
    }

    // ── Local IJ coordinates ──────────────────────────────────────────────────

    /// <summary>
    /// Returns the local IJ coordinates of this cell relative to the given <paramref name="origin"/> cell.
    /// Both cells must be within the same H3 base cell face.
    /// </summary>
    public CoordIJ ToLocalIJ( H3Index origin )
    {
        H3Native.cellToLocalIj( origin, _value, 0, out CoordIJNative coord ).ThrowIfError();
        return CoordIJ.FromNative( coord );
    }

    // ── Cell area and edge length (static) ────────────────────────────────────

    /// <summary>Returns the exact area of the given cell in square kilometers.</summary>
    public static double CellAreaKm2( H3Index cell )
    {
        H3Native.cellAreaKm2( cell, out double area ).ThrowIfError();
        return area;
    }

    /// <summary>Returns the exact area of the given cell in square meters.</summary>
    public static double CellAreaM2( H3Index cell )
    {
        H3Native.cellAreaM2( cell, out double area ).ThrowIfError();
        return area;
    }

    /// <summary>Returns the exact length of the given directed edge in kilometers.</summary>
    public static double ExactEdgeLengthKm( H3Index edge )
    {
        H3Native.edgeLengthKm( edge, out double length ).ThrowIfError();
        return length;
    }

    // ── Serialization ─────────────────────────────────────────────────────────

    /// <summary>
    /// Converts this index to its canonical lowercase hexadecimal string representation
    /// (e.g. <c>"8928308280fffff"</c>).
    /// </summary>
    public override string ToString() => _value.ToString( "x" );

    /// <inheritdoc cref="ToString()"/>
    public string ToString( string? format, IFormatProvider? formatProvider )
        => _value.ToString( "x", formatProvider );

    /// <summary>
    /// Parses a lowercase hexadecimal string into an <see cref="H3Index"/>.
    /// </summary>
    /// <exception cref="FormatException">Thrown if the string is not a valid hexadecimal number.</exception>
    public static H3Index Parse( string s )
        => ulong.Parse( s, NumberStyles.HexNumber, CultureInfo.InvariantCulture );

    /// <summary>
    /// Tries to parse a hexadecimal string into an <see cref="H3Index"/>.
    /// Returns <see langword="false"/> and sets <paramref name="result"/> to <see cref="Invalid"/> on failure.
    /// </summary>
    public static bool TryParse( string s, out H3Index result )
    {
        if( ulong.TryParse( s, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out ulong value ) )
        {
            result = value;
            return true;
        }
        result = Invalid;
        return false;
    }

    // ── Equality and comparison ───────────────────────────────────────────────

    /// <inheritdoc/>
    public bool Equals( H3Index other ) => _value == other._value;

    /// <inheritdoc/>
    public override bool Equals( object? obj ) => obj is H3Index other && Equals( other );

    /// <inheritdoc/>
    public override int GetHashCode() => _value.GetHashCode();

    /// <inheritdoc/>
    public int CompareTo( H3Index other ) => _value.CompareTo( other._value );

    /// <summary>Returns <see langword="true"/> if two indexes represent the same H3 index.</summary>
    public static bool operator ==( H3Index left, H3Index right ) => left._value == right._value;

    /// <summary>Returns <see langword="true"/> if two indexes represent different H3 indexes.</summary>
    public static bool operator !=( H3Index left, H3Index right ) => left._value != right._value;
}
