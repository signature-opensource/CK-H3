using System;

namespace CK.H3;

/// <summary>
/// The boundary polygon of an H3 cell. Contains up to 10 vertices
/// (pentagons have 5, hexagons have 6).
/// </summary>
public readonly struct CellBoundary
{
    readonly LatLng[] _verts;

    CellBoundary( LatLng[] verts, int numVerts )
    {
        _verts = verts;
        NumVerts = numVerts;
    }

    /// <summary>The number of boundary vertices (5 for pentagons, 6 for hexagons).</summary>
    public int NumVerts { get; }

    /// <summary>The boundary vertices as geographic coordinates in degrees.</summary>
    public ReadOnlySpan<LatLng> Verts => _verts.AsSpan( 0, NumVerts );

    internal static unsafe CellBoundary FromNative( in CellBoundaryNative n )
    {
        var verts = new LatLng[n.numVerts];
        for( int i = 0; i < n.numVerts; i++ )
        {
            verts[i] = LatLng.FromNative( n.GetVert( i ) );
        }
        return new CellBoundary( verts, n.numVerts );
    }
}
