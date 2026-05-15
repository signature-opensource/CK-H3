using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace CK.H3;

/// <summary>Operations for converting between H3 cells and geographic polygons.</summary>
public static class GeoPolygon
{
    /// <summary>
    /// Returns the set of H3 cells at <paramref name="resolution"/> that are contained
    /// within the given polygon (outer boundary plus optional holes).
    /// </summary>
    /// <param name="outerBoundary">Vertices of the outer ring in degrees, in order.</param>
    /// <param name="holes">Optional list of hole rings; each hole is a list of vertices in degrees.</param>
    /// <param name="resolution">H3 resolution (0–15).</param>
    public static H3Index[] ToCells( IReadOnlyList<LatLng> outerBoundary,
                                     IReadOnlyList<IReadOnlyList<LatLng>>? holes,
                                     int resolution )
    {
        // Convert degrees → radians for the native call.
        var outerNative = ToNativeArray( outerBoundary );
        int numHoles = holes?.Count ?? 0;
        var holesNative = new LatLngNative[numHoles][];
        for( int h = 0; h < numHoles; h++ )
        {
            holesNative[h] = ToNativeArray( holes![h] );
        }

        return ToCellsCore( outerNative, holesNative, numHoles, resolution );
    }

    /// <summary>
    /// Reconstructs a (multi-)polygon from the given set of H3 cells.
    /// Returns a list of polygons; each polygon is a list of rings (outer boundary first,
    /// then holes); each ring is an array of <see cref="LatLng"/> vertices in degrees.
    /// </summary>
    public static unsafe IReadOnlyList<IReadOnlyList<LatLng[]>> CellsToMultiPolygon( IReadOnlyList<H3Index> cells )
    {
        var cellsArray = new H3Index[cells.Count];
        for( int i = 0; i < cells.Count; i++ )
        {
            cellsArray[i] = cells[i];
        }

        fixed( H3Index* pCells = cellsArray )
        {
            LinkedGeoPolygonNative result = default;
            H3Native.cellsToLinkedMultiPolygon( (ulong*)pCells, cells.Count, &result ).ThrowIfError();
            try
            {
                return TraverseLinkedPolygon( &result );
            }
            finally
            {
                H3Native.destroyLinkedMultiPolygon( &result );
            }
        }
    }

    // ─── Private helpers ───────────────────────────────────────────────────────

    static LatLngNative[] ToNativeArray( IReadOnlyList<LatLng> coords )
    {
        var arr = new LatLngNative[coords.Count];
        for( int i = 0; i < coords.Count; i++ )
        {
            arr[i] = coords[i].ToNative();
        }
        return arr;
    }

    static unsafe H3Index[] ToCellsCore( LatLngNative[] outerNative,
                                         LatLngNative[][] holesNative,
                                         int numHoles,
                                         int resolution )
    {
        // All LatLngNative arrays must remain pinned for the duration of both native calls.
        // GCHandle.Alloc is used because 'fixed' cannot pin a variable number of arrays.
        var handles = new GCHandle[1 + numHoles];
        try
        {
            handles[0] = GCHandle.Alloc( outerNative, GCHandleType.Pinned );
            for( int h = 0; h < numHoles; h++ )
            {
                handles[h + 1] = GCHandle.Alloc( holesNative[h], GCHandleType.Pinned );
            }

            var outerLoop = new GeoLoopNative
            {
                numVerts = outerNative.Length,
                verts = (LatLngNative*)handles[0].AddrOfPinnedObject(),
            };

            GeoPolygonNative poly;
            poly.geoloop = outerLoop;
            poly.numHoles = numHoles;
            poly.holes = null;

            GCHandle holesArrayHandle = default;
            try
            {
                if( numHoles > 0 )
                {
                    var holesArray = new GeoLoopNative[numHoles];
                    for( int h = 0; h < numHoles; h++ )
                    {
                        holesArray[h] = new GeoLoopNative
                        {
                            numVerts = holesNative[h].Length,
                            verts = (LatLngNative*)handles[h + 1].AddrOfPinnedObject(),
                        };
                    }
                    holesArrayHandle = GCHandle.Alloc( holesArray, GCHandleType.Pinned );
                    poly.holes = (GeoLoopNative*)holesArrayHandle.AddrOfPinnedObject();
                }

                H3Native.maxPolygonToCellsSize( &poly, resolution, 0, out long count ).ThrowIfError();
                var output = new H3Index[count];
                fixed( H3Index* p = output )
                {
                    H3Native.polygonToCells( &poly, resolution, 0, (ulong*)p ).ThrowIfError();
                }
                return output;
            }
            finally
            {
                if( holesArrayHandle.IsAllocated ) holesArrayHandle.Free();
            }
        }
        finally
        {
            foreach( var h in handles )
            {
                if( h.IsAllocated ) h.Free();
            }
        }
    }

    static unsafe IReadOnlyList<IReadOnlyList<LatLng[]>> TraverseLinkedPolygon( LinkedGeoPolygonNative* poly )
    {
        var polygons = new List<IReadOnlyList<LatLng[]>>();
        LinkedGeoPolygonNative* curPoly = poly;
        while( curPoly != null )
        {
            var loops = new List<LatLng[]>();
            LinkedGeoLoopNative* curLoop = curPoly->first;
            while( curLoop != null )
            {
                var verts = new List<LatLng>();
                LinkedLatLngNative* curVert = curLoop->first;
                while( curVert != null )
                {
                    verts.Add( LatLng.FromNative( curVert->vertex ) );
                    curVert = curVert->next;
                }
                loops.Add( verts.ToArray() );
                curLoop = curLoop->next;
            }
            polygons.Add( loops );
            curPoly = curPoly->next;
        }
        return polygons;
    }
}
