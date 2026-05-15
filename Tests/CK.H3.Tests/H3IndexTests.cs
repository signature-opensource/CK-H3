using NUnit.Framework;
using Shouldly;

namespace CK.H3.Tests;

[TestFixture]
public class H3IndexTests
{
    // San Francisco coordinates used as a reproducible test location.
#pragma warning disable IDE1006 // Naming Styles
    const double SfLat = 37.3615593;
    const double SfLng = -122.0553238;
#pragma warning restore IDE1006 // Naming Styles

    [Test]
    public void FromLatLng_ReturnsValidIndex()
    {
        var cell = H3Index.FromLatLng( SfLat, SfLng, 9 );
        cell.IsValid.ShouldBeTrue();
    }

    [Test]
    public void Invalid_IsNotValid()
    {
        H3Index.Invalid.IsValid.ShouldBeFalse();
    }

    [Test]
    public void Resolution_ReturnsRequestedResolution()
    {
        H3Index.FromLatLng( SfLat, SfLng, 9 ).Resolution.ShouldBe( 9 );
        H3Index.FromLatLng( SfLat, SfLng, 5 ).Resolution.ShouldBe( 5 );
    }

    [Test]
    public void BaseCell_IsInValidRange()
    {
        var cell = H3Index.FromLatLng( SfLat, SfLng, 5 );
        cell.BaseCell.ShouldBeInRange( 0, 121 );
    }

    [Test]
    public void ToLatLng_RoundTrip_IsCloseToOriginal()
    {
        var cell = H3Index.FromLatLng( SfLat, SfLng, 9 );
        var ll = cell.ToLatLng();
        // Center is within ~1 km of the original point at resolution 9.
        ll.Lat.ShouldBe( SfLat, tolerance: 0.01 );
        ll.Lng.ShouldBe( SfLng, tolerance: 0.01 );
    }

    [Test]
    public void ToBoundary_HexagonHas6Vertices()
    {
        var cell = H3Index.FromLatLng( SfLat, SfLng, 9 );
        cell.IsPentagon.ShouldBeFalse();
        cell.ToBoundary().NumVerts.ShouldBe( 6 );
    }

    [Test]
    public void ToParent_HasLowerResolutionAndIsValid()
    {
        var child = H3Index.FromLatLng( SfLat, SfLng, 9 );
        var parent = child.ToParent( 8 );
        parent.IsValid.ShouldBeTrue();
        parent.Resolution.ShouldBe( 8 );
    }

    [Test]
    public void ToChildren_ContainsChildOfOriginalCell()
    {
        var child = H3Index.FromLatLng( SfLat, SfLng, 9 );
        // Derive the res-8 parent from the child to avoid a double call to latLngToCell,
        // then verify that the child appears in the parent's children (ToParent / ToChildren round-trip).
        var parent = child.ToParent( 8 );
        var children = parent.ToChildren( 9 );
        children.ShouldContain( child );
    }

    [Test]
    public void GridDisk_k0_ReturnsSingleCell()
    {
        H3Index.FromLatLng( SfLat, SfLng, 8 ).GridDisk( 0 ).Length.ShouldBe( 1 );
    }

    [Test]
    public void GridDisk_k1_Returns7Cells()
    {
        H3Index.FromLatLng( SfLat, SfLng, 8 ).GridDisk( 1 ).Length.ShouldBe( 7 );
    }

    [Test]
    public void ToString_ParseRoundTrip()
    {
        var original = H3Index.FromLatLng( SfLat, SfLng, 9 );
        var parsed = H3Index.Parse( original.ToString() );
        parsed.ShouldBe( original );
    }

    [Test]
    public void TryParse_ValidString_ReturnsTrueAndCorrectValue()
    {
        var original = H3Index.FromLatLng( SfLat, SfLng, 9 );
        H3Index.TryParse( original.ToString(), out var result ).ShouldBeTrue();
        result.ShouldBe( original );
    }

    [Test]
    public void TryParse_InvalidString_ReturnsFalse()
    {
        H3Index.TryParse( "not-a-valid-h3-index", out _ ).ShouldBeFalse();
    }

    [Test]
    public void Compact_Uncompact_Roundtrip()
    {
        var parent = H3Index.FromLatLng( SfLat, SfLng, 8 );
        var children = parent.ToChildren( 9 );

        var compacted = H3Index.Compact( children );
        // A complete set of children compacts back to the parent (or fewer cells).
        compacted.Length.ShouldBeLessThanOrEqualTo( children.Length );

        // Uncompacting each compacted cell at resolution 9 reconstructs the original set.
        var uncompacted = new System.Collections.Generic.List<H3Index>();
        foreach( var c in compacted )
        {
            uncompacted.AddRange( c.Uncompact( 9 ) );
        }
        uncompacted.Count.ShouldBe( children.Length );
    }
}
