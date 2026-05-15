using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace CK.H3;

// ─── Native structs ───────────────────────────────────────────────────────────

#pragma warning disable IDE1006 // Naming Styles

// Geographic coordinate in radians (internal use only — public API uses LatLng in degrees).
[StructLayout( LayoutKind.Sequential )]
internal struct LatLngNative
{
    internal double lat; // radians
    internal double lng; // radians
}

// Cell boundary as returned by cellToBoundary. MAX_CELL_BNDRY_VERTS = 10.
// 'fixed' buffers require primitive element types, so 10 LatLng pairs are stored as 20 doubles.
[StructLayout( LayoutKind.Sequential )]
internal unsafe struct CellBoundaryNative
{
    internal const int MaxVerts = 10;
    internal int numVerts;
    internal fixed double verts[20]; // MaxVerts × (lat, lng) consecutive doubles in radians

    internal readonly unsafe LatLngNative GetVert( int i )
    {
        fixed( double* p = verts )
        {
            return new LatLngNative { lat = p[i * 2], lng = p[i * 2 + 1] };
        }
    }
}

// Local IJ grid coordinates (internal use only).
[StructLayout( LayoutKind.Sequential )]
internal struct CoordIJNative
{
    internal int i;
    internal int j;
}

// C GeoLoop: one ring (exterior or hole) of a polygon.
// Pack=0: natural alignment inserts 4 bytes of padding between numVerts (int) and verts (ptr) on 64-bit,
// matching what the C compiler produces.
[StructLayout( LayoutKind.Sequential, Pack = 0 )]
internal unsafe struct GeoLoopNative
{
    internal int numVerts;
    // 4 bytes of implicit padding on 64-bit before the pointer field
    internal LatLngNative* verts; // caller-owned; must be pinned for the duration of any P/Invoke call
}

// C GeoPolygon: outer ring plus optional holes. Passed as GeoPolygonNative* to polygonToCells*.
// LibraryImport cannot auto-marshal structs with pointer fields — callers must use raw pointers.
[StructLayout( LayoutKind.Sequential, Pack = 0 )]
internal unsafe struct GeoPolygonNative
{
    internal GeoLoopNative geoloop;
    internal int numHoles;
    // 4 bytes of implicit padding on 64-bit before the pointer field
    internal GeoLoopNative* holes;
}

// Linked-list vertex node; allocated and owned by H3 (cellsToLinkedMultiPolygon).
[StructLayout( LayoutKind.Sequential )]
internal unsafe struct LinkedLatLngNative
{
    internal LatLngNative vertex;
    internal LinkedLatLngNative* next;
}

// Linked-list loop node; allocated and owned by H3.
[StructLayout( LayoutKind.Sequential )]
internal unsafe struct LinkedGeoLoopNative
{
    internal LinkedLatLngNative* first;
    internal LinkedLatLngNative* last;
    internal LinkedGeoLoopNative* next;
}

// Root of the linked multi-polygon returned by cellsToLinkedMultiPolygon.
// Must be freed with destroyLinkedMultiPolygon after traversal.
[StructLayout( LayoutKind.Sequential )]
internal unsafe struct LinkedGeoPolygonNative
{
    internal LinkedGeoLoopNative* first;
    internal LinkedGeoLoopNative* last;
    internal LinkedGeoPolygonNative* next;
}

#pragma warning restore IDE1006 // Naming Styles

// ─── P/Invoke declarations ────────────────────────────────────────────────────

internal static unsafe partial class H3Native
{
#pragma warning disable IDE1006 // Naming Styles
    const string LibraryName = "h3";
#pragma warning restore IDE1006 // Naming Styles

    // ── Cell indexing ──────────────────────────────────────────────────────────

    [LibraryImport( LibraryName )]
    [UnmanagedCallConv( CallConvs = [typeof( CallConvCdecl )] )]
    internal static partial H3ErrorCode latLngToCell( in LatLngNative g, int res, out ulong cell );

    [LibraryImport( LibraryName )]
    [UnmanagedCallConv( CallConvs = [typeof( CallConvCdecl )] )]
    internal static partial H3ErrorCode cellToLatLng( ulong cell, out LatLngNative g );

    [LibraryImport( LibraryName )]
    [UnmanagedCallConv( CallConvs = [typeof( CallConvCdecl )] )]
    internal static partial H3ErrorCode cellToBoundary( ulong cell, out CellBoundaryNative bndry );

    // ── Index inspection (return int, not H3ErrorCode) ─────────────────────────

    [LibraryImport( LibraryName )]
    [UnmanagedCallConv( CallConvs = [typeof( CallConvCdecl )] )]
    internal static partial int isValidCell( ulong h );

    [LibraryImport( LibraryName )]
    [UnmanagedCallConv( CallConvs = [typeof( CallConvCdecl )] )]
    internal static partial int isPentagon( ulong h );

    [LibraryImport( LibraryName )]
    [UnmanagedCallConv( CallConvs = [typeof( CallConvCdecl )] )]
    internal static partial int getResolution( ulong h );

    [LibraryImport( LibraryName )]
    [UnmanagedCallConv( CallConvs = [typeof( CallConvCdecl )] )]
    internal static partial int getBaseCellNumber( ulong h );

    // ── Hierarchy ──────────────────────────────────────────────────────────────

    [LibraryImport( LibraryName )]
    [UnmanagedCallConv( CallConvs = [typeof( CallConvCdecl )] )]
    internal static partial H3ErrorCode cellToParent( ulong h, int parentRes, out ulong parent );

    [LibraryImport( LibraryName )]
    [UnmanagedCallConv( CallConvs = [typeof( CallConvCdecl )] )]
    internal static partial H3ErrorCode cellToChildrenSize( ulong h, int childRes, out long count );

    [LibraryImport( LibraryName )]
    [UnmanagedCallConv( CallConvs = [typeof( CallConvCdecl )] )]
    internal static partial H3ErrorCode cellToChildren( ulong h, int childRes, ulong* children );

    [LibraryImport( LibraryName )]
    [UnmanagedCallConv( CallConvs = [typeof( CallConvCdecl )] )]
    internal static partial H3ErrorCode cellToCenterChild( ulong h, int childRes, out ulong child );

    [LibraryImport( LibraryName )]
    [UnmanagedCallConv( CallConvs = [typeof( CallConvCdecl )] )]
    internal static partial H3ErrorCode compactCells( ulong* cellSet, ulong* compactedSet, long numHexes );

    [LibraryImport( LibraryName )]
    [UnmanagedCallConv( CallConvs = [typeof( CallConvCdecl )] )]
    internal static partial H3ErrorCode uncompactCellsSize( ulong* compactedSet, long numCompacted, int res, out long count );

    [LibraryImport( LibraryName )]
    [UnmanagedCallConv( CallConvs = [typeof( CallConvCdecl )] )]
    internal static partial H3ErrorCode uncompactCells( ulong* compactedSet, long numCompacted, ulong* outSet, long numOut, int res );

    // ── Traversal ──────────────────────────────────────────────────────────────

    [LibraryImport( LibraryName )]
    [UnmanagedCallConv( CallConvs = [typeof( CallConvCdecl )] )]
    internal static partial H3ErrorCode maxGridDiskSize( int k, out long count );

    [LibraryImport( LibraryName )]
    [UnmanagedCallConv( CallConvs = [typeof( CallConvCdecl )] )]
    internal static partial H3ErrorCode gridDisk( ulong origin, int k, ulong* outCells );

    [LibraryImport( LibraryName )]
    [UnmanagedCallConv( CallConvs = [typeof( CallConvCdecl )] )]
    internal static partial H3ErrorCode gridDiskDistances( ulong origin, int k, ulong* outCells, int* distances );

    [LibraryImport( LibraryName )]
    [UnmanagedCallConv( CallConvs = [typeof( CallConvCdecl )] )]
    internal static partial H3ErrorCode gridRingUnsafe( ulong origin, int k, ulong* outCells );

    [LibraryImport( LibraryName )]
    [UnmanagedCallConv( CallConvs = [typeof( CallConvCdecl )] )]
    internal static partial H3ErrorCode gridPathCellsSize( ulong origin, ulong destination, out long count );

    [LibraryImport( LibraryName )]
    [UnmanagedCallConv( CallConvs = [typeof( CallConvCdecl )] )]
    internal static partial H3ErrorCode gridPathCells( ulong origin, ulong destination, ulong* outCells );

    [LibraryImport( LibraryName )]
    [UnmanagedCallConv( CallConvs = [typeof( CallConvCdecl )] )]
    internal static partial H3ErrorCode gridDistance( ulong origin, ulong h, out long distance );

    // ── Edges ──────────────────────────────────────────────────────────────────

    [LibraryImport( LibraryName )]
    [UnmanagedCallConv( CallConvs = [typeof( CallConvCdecl )] )]
    internal static partial H3ErrorCode areNeighborCells( ulong origin, ulong destination, out int isNeighbor );

    [LibraryImport( LibraryName )]
    [UnmanagedCallConv( CallConvs = [typeof( CallConvCdecl )] )]
    internal static partial H3ErrorCode cellsToDirectedEdge( ulong origin, ulong destination, out ulong edge );

    // Output buffer must be exactly [2]: [origin, destination].
    [LibraryImport( LibraryName )]
    [UnmanagedCallConv( CallConvs = [typeof( CallConvCdecl )] )]
    internal static partial H3ErrorCode directedEdgeToCells( ulong edge, ulong* originDestination );

    // Output buffer must be exactly [6]; unused slots (pentagons) are filled with 0.
    [LibraryImport( LibraryName )]
    [UnmanagedCallConv( CallConvs = [typeof( CallConvCdecl )] )]
    internal static partial H3ErrorCode originToDirectedEdges( ulong origin, ulong* edges );

    // ── Vertexes ───────────────────────────────────────────────────────────────

    [LibraryImport( LibraryName )]
    [UnmanagedCallConv( CallConvs = [typeof( CallConvCdecl )] )]
    internal static partial H3ErrorCode cellToVertex( ulong cell, int vertexNum, out ulong vertex );

    // Output buffer must be exactly [6]; unused slots (pentagons) are filled with 0.
    [LibraryImport( LibraryName )]
    [UnmanagedCallConv( CallConvs = [typeof( CallConvCdecl )] )]
    internal static partial H3ErrorCode cellToVertexes( ulong cell, ulong* vertexes );

    [LibraryImport( LibraryName )]
    [UnmanagedCallConv( CallConvs = [typeof( CallConvCdecl )] )]
    internal static partial H3ErrorCode vertexToLatLng( ulong vertex, out LatLngNative point );

    // ── Local IJ coordinates ──────────────────────────────────────────────────

    [LibraryImport( LibraryName )]
    [UnmanagedCallConv( CallConvs = [typeof( CallConvCdecl )] )]
    internal static partial H3ErrorCode cellToLocalIj( ulong origin, ulong h, uint mode, out CoordIJNative coord );

    [LibraryImport( LibraryName )]
    [UnmanagedCallConv( CallConvs = [typeof( CallConvCdecl )] )]
    internal static partial H3ErrorCode localIjToCell( ulong origin, in CoordIJNative ij, uint mode, out ulong cell );

    // ── Global functions ───────────────────────────────────────────────────────

    [LibraryImport( LibraryName )]
    [UnmanagedCallConv( CallConvs = [typeof( CallConvCdecl )] )]
    internal static partial H3ErrorCode getNumCells( int res, out long count );

    // Output buffer size must equal res0CellCount() = 122.
    [LibraryImport( LibraryName )]
    [UnmanagedCallConv( CallConvs = [typeof( CallConvCdecl )] )]
    internal static partial H3ErrorCode getRes0Cells( ulong* outCells );

    [LibraryImport( LibraryName )]
    [UnmanagedCallConv( CallConvs = [typeof( CallConvCdecl )] )]
    internal static partial int res0CellCount();

    // Output buffer size must equal pentagonCount() = 12.
    [LibraryImport( LibraryName )]
    [UnmanagedCallConv( CallConvs = [typeof( CallConvCdecl )] )]
    internal static partial H3ErrorCode getPentagons( int res, ulong* outCells );

    [LibraryImport( LibraryName )]
    [UnmanagedCallConv( CallConvs = [typeof( CallConvCdecl )] )]
    internal static partial int pentagonCount();

    [LibraryImport( LibraryName )]
    [UnmanagedCallConv( CallConvs = [typeof( CallConvCdecl )] )]
    internal static partial H3ErrorCode maxFaceCount( ulong h, out int count );

    // Output buffer size must equal maxFaceCount(h).
    [LibraryImport( LibraryName )]
    [UnmanagedCallConv( CallConvs = [typeof( CallConvCdecl )] )]
    internal static partial H3ErrorCode getIcosahedronFaces( ulong h, int* faces );

    // ── Polygon ────────────────────────────────────────────────────────────────

    // Raw pointer required: GeoPolygonNative contains pointer fields that the source generator
    // cannot marshal automatically.
    [LibraryImport( LibraryName )]
    [UnmanagedCallConv( CallConvs = [typeof( CallConvCdecl )] )]
    internal static partial H3ErrorCode maxPolygonToCellsSize( GeoPolygonNative* polygon, int res, uint flags, out long count );

    [LibraryImport( LibraryName )]
    [UnmanagedCallConv( CallConvs = [typeof( CallConvCdecl )] )]
    internal static partial H3ErrorCode polygonToCells( GeoPolygonNative* polygon, int res, uint flags, ulong* outCells );

    // Raw pointer required: LinkedGeoPolygonNative contains pointer fields.
    // The caller must call destroyLinkedMultiPolygon after traversing the result.
    [LibraryImport( LibraryName )]
    [UnmanagedCallConv( CallConvs = [typeof( CallConvCdecl )] )]
    internal static partial H3ErrorCode cellsToLinkedMultiPolygon( ulong* h3Set, int numHexes, LinkedGeoPolygonNative* outPolygon );

    [LibraryImport( LibraryName )]
    [UnmanagedCallConv( CallConvs = [typeof( CallConvCdecl )] )]
    internal static partial void destroyLinkedMultiPolygon( LinkedGeoPolygonNative* polygon );

    // ── Area / length / distance ───────────────────────────────────────────────

    [LibraryImport( LibraryName )]
    [UnmanagedCallConv( CallConvs = [typeof( CallConvCdecl )] )]
    internal static partial H3ErrorCode cellAreaKm2( ulong h, out double area );

    [LibraryImport( LibraryName )]
    [UnmanagedCallConv( CallConvs = [typeof( CallConvCdecl )] )]
    internal static partial H3ErrorCode cellAreaM2( ulong h, out double area );

    [LibraryImport( LibraryName )]
    [UnmanagedCallConv( CallConvs = [typeof( CallConvCdecl )] )]
    internal static partial H3ErrorCode cellAreaRads2( ulong h, out double area );

    [LibraryImport( LibraryName )]
    [UnmanagedCallConv( CallConvs = [typeof( CallConvCdecl )] )]
    internal static partial H3ErrorCode edgeLengthKm( ulong edge, out double length );

    [LibraryImport( LibraryName )]
    [UnmanagedCallConv( CallConvs = [typeof( CallConvCdecl )] )]
    internal static partial H3ErrorCode edgeLengthM( ulong edge, out double length );

    [LibraryImport( LibraryName )]
    [UnmanagedCallConv( CallConvs = [typeof( CallConvCdecl )] )]
    internal static partial H3ErrorCode edgeLengthRads( ulong edge, out double length );

    [LibraryImport( LibraryName )]
    [UnmanagedCallConv( CallConvs = [typeof( CallConvCdecl )] )]
    internal static partial double greatCircleDistanceKm( in LatLngNative a, in LatLngNative b );

    [LibraryImport( LibraryName )]
    [UnmanagedCallConv( CallConvs = [typeof( CallConvCdecl )] )]
    internal static partial double greatCircleDistanceM( in LatLngNative a, in LatLngNative b );

    [LibraryImport( LibraryName )]
    [UnmanagedCallConv( CallConvs = [typeof( CallConvCdecl )] )]
    internal static partial double greatCircleDistanceRads( in LatLngNative a, in LatLngNative b );
}

// ─── ThrowIfError extension ───────────────────────────────────────────────────

internal static class H3ErrorCodeExtensions
{
    [MethodImpl( MethodImplOptions.AggressiveInlining )]
    internal static H3ErrorCode ThrowIfError( this H3ErrorCode code )
    {
        if( code != H3ErrorCode.Success ) Throw( code );
        return code;
    }

    // NoInlining keeps the exception-construction path out of the JIT-compiled hot path.
    [DoesNotReturn]
    [MethodImpl( MethodImplOptions.NoInlining )]
    static void Throw( H3ErrorCode code ) => throw new H3Exception( code );
}
