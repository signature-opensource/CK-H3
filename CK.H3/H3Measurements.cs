namespace CK.H3;

/// <summary>Great-circle distance measurements between geographic coordinates.</summary>
public static class H3Measurements
{
    /// <summary>
    /// Returns the great-circle distance between two geographic points in kilometers,
    /// using the Haversine formula.
    /// </summary>
    public static double GreatCircleDistanceKm( LatLng a, LatLng b )
    {
        var na = a.ToNative();
        var nb = b.ToNative();
        return H3Native.greatCircleDistanceKm( in na, in nb );
    }

    /// <summary>
    /// Returns the great-circle distance between two geographic points in meters,
    /// using the Haversine formula.
    /// </summary>
    public static double GreatCircleDistanceM( LatLng a, LatLng b )
    {
        var na = a.ToNative();
        var nb = b.ToNative();
        return H3Native.greatCircleDistanceM( in na, in nb );
    }

    /// <summary>
    /// Returns the great-circle distance between two geographic points in radians,
    /// using the Haversine formula.
    /// </summary>
    public static double GreatCircleDistanceRads( LatLng a, LatLng b )
    {
        var na = a.ToNative();
        var nb = b.ToNative();
        return H3Native.greatCircleDistanceRads( in na, in nb );
    }
}
