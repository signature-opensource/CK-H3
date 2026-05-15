using System;

namespace CK.H3;

/// <summary>Geographic coordinates in degrees (WGS84).</summary>
public readonly struct LatLng : IEquatable<LatLng>
{
    /// <summary>Latitude in degrees [-90, 90].</summary>
    public double Lat { get; }

    /// <summary>Longitude in degrees [-180, 180].</summary>
    public double Lng { get; }

    /// <summary>Initializes a new <see cref="LatLng"/> with the given latitude and longitude in degrees.</summary>
    public LatLng( double lat, double lng )
    {
        Lat = lat;
        Lng = lng;
    }

    /// <inheritdoc/>
    public bool Equals( LatLng other ) => Lat == other.Lat && Lng == other.Lng;

    /// <inheritdoc/>
    public override bool Equals( object? obj ) => obj is LatLng other && Equals( other );

    /// <inheritdoc/>
    public override int GetHashCode() => HashCode.Combine( Lat, Lng );

    /// <inheritdoc/>
    public override string ToString() => $"({Lat}, {Lng})";

    // Converts degrees to radians for native calls.
    internal LatLngNative ToNative() => new()
    {
        lat = Lat * ( Math.PI / 180.0 ),
        lng = Lng * ( Math.PI / 180.0 ),
    };

    // Converts radians from a native result back to degrees.
    internal static LatLng FromNative( LatLngNative n ) => new( n.lat * ( 180.0 / Math.PI ), n.lng * ( 180.0 / Math.PI ) );

    public static bool operator ==( LatLng left, LatLng right ) => left.Equals( right );

    public static bool operator !=( LatLng left, LatLng right ) => !(left == right);
}
