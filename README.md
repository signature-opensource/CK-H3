# CK.H3

[![License](https://img.shields.io/badge/license-Apache%202.0-blue.svg)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-8.0-purple.svg)](https://dotnet.microsoft.com)

A comprehensive, idiomatic C# wrapper around Uber's [H3](https://h3geo.org/) hierarchical hexagonal geospatial indexing library (v4.4.1+), targeting .NET 8.

H3 partitions the Earth's surface into a multi-resolution grid of hexagonal cells. Each cell has a unique 64-bit index, enabling efficient spatial queries, aggregation, and traversal at any of the 16 available resolutions (global coverage at resolution 0 down to ~1 m² cells at resolution 15).

---

## Features

- **Full H3 API coverage** — cell indexing, hierarchy navigation, grid traversal, polygon rasterization/reconstruction, directed edges, vertices, local IJ coordinates, and geospatial measurements
- **Idiomatic C# API** — strongly typed structs (`H3Index`, `LatLng`, `CellBoundary`, `CoordIJ`), clear naming conventions, and transparent degree/radian conversion (callers always work in degrees)
- **Zero managed dependencies** — no external NuGet packages required
- **Multi-platform native binaries** — prebuilt for `win-x64`, `linux-x64`, `linux-arm64`; any other platform is supported by bringing your own compiled H3 library
- **Safe public surface** — all P/Invoke and pointer arithmetic is encapsulated; the public API is fully safe
- **Structured error handling** — native errors surface as `H3Exception` with a typed `H3ErrorCode`

---

## Packages

The solution ships as two complementary NuGet packages:

| Package | Description |
|---|---|
| `CK.H3` | Managed wrapper only. Use this when you manage native binaries yourself or target a custom runtime. |
| `CK.H3.Binaries` | Includes `CK.H3` and the prebuilt native `h3` library for `win-x64`, `linux-x64`, and `linux-arm64`. **Recommended for most projects.** |

### Installation

```bash
dotnet add package CK.H3.Binaries
```

---

## Quick Start

```csharp
using CK.H3;

// Index a geographic coordinate at resolution 9
H3Index cell = H3Index.FromLatLng(37.3615593, -122.0553238, resolution: 9);

Console.WriteLine(cell);              // 8928308280fffff
Console.WriteLine(cell.Resolution);  // 9
Console.WriteLine(cell.IsPentagon);  // False

// Get the geographic center of the cell
LatLng center = cell.ToLatLng();
Console.WriteLine($"{center.Lat:F6}, {center.Lng:F6}");

// Get the parent cell at resolution 5
H3Index parent = cell.ToParent(5);

// Get all cells within radius 2 (k-ring)
IReadOnlyList<H3Index> disk = cell.GridDisk(k: 2);
Console.WriteLine(disk.Count); // 19

// Get the boundary polygon vertices
CellBoundary boundary = cell.ToBoundary();
foreach (LatLng vertex in boundary.Verts)
    Console.WriteLine($"  {vertex.Lat:F6}, {vertex.Lng:F6}");
```

---

## API Overview

### `H3Index`

The central type. An `H3Index` is a `readonly struct` wrapping a `ulong` that represents a cell, directed edge, or vertex.

#### Indexing

| Method | Description |
|---|---|
| `H3Index.FromLatLng(lat, lng, res)` | Get the cell containing a coordinate |
| `ToLatLng()` | Geographic center of the cell (degrees) |
| `ToBoundary()` | Boundary polygon (5–6 vertices) |
| `Resolution` | Resolution level (0–15) |
| `BaseCellNumber` | Base cell number (0–121) |
| `ResolutionResDigit` | Resolution digit at the current resolution level (0–7) |
| `IsValid` | Validates the index |
| `IsPentagon` | `true` for the 12 icosahedron-vertex cells |

#### Hierarchy

| Method | Description |
|---|---|
| `ToParent(res)` | Coarser-resolution parent |
| `ToChildren(res)` | All finer-resolution children |
| `ToCenterChild(res)` | Center child closest to the parent center |
| `H3Index.Compact(cells)` | Compress redundant children into parents |
| `Uncompact(res)` | Expand coarse cells to target resolution |

#### Grid Traversal

| Method | Description |
|---|---|
| `GridDisk(k)` | All cells within k steps (filled disk) |
| `GridRing(k)` | Cells at exactly k steps (ring) |
| `GridPathTo(destination)` | Shortest path between two cells |
| `GridDistanceTo(other)` | Step distance between two cells |

#### Edges & Vertices

| Method | Description |
|---|---|
| `IsNeighborWith(other)` | Check adjacency |
| `DirectedEdgeTo(neighbor)` | Directed edge index toward a neighbor |
| `DirectedEdgeToCells()` | Origin and destination from an edge index |
| `GetDirectedEdges()` | All outbound directed edges |
| `GetVertex(n)` / `GetVertexes()` | Vertex indexes |
| `VertexToLatLng()` | Geographic position of a vertex |

#### Local Coordinates

| Method | Description |
|---|---|
| `ToLocalIJ(origin)` | `CoordIJ` relative to an origin cell |
| `CoordIJ.ToCell(origin)` | Convert local coordinates back to `H3Index` |

#### Serialization

```csharp
H3Index cell = H3Index.Parse("8928308280fffff");
string hex  = cell.ToString(); // "8928308280fffff"

bool ok = H3Index.TryParse(input, out H3Index result);
```

---

### `GeoPolygon`

Polygon rasterization and cell-set reconstruction.

```csharp
// Rasterize a polygon into H3 cells
LatLng[] ring = [ new(37.813, -122.408), new(37.813, -122.373),
                  new(37.784, -122.373), new(37.784, -122.408) ];

IReadOnlyList<H3Index> cells = GeoPolygon.ToCells(ring, holes: [], resolution: 9);

// Reconstruct the geographic polygon from a set of cells
IReadOnlyList<IReadOnlyList<LatLng[]>> polygons = GeoPolygon.CellsToMultiPolygon(cells);
```

---

### `H3Measurements`

Great-circle distance between two geographic points.

```csharp
var sf  = new LatLng(37.7749, -122.4194);
var nyc = new LatLng(40.7128,  -74.0060);

double km   = H3Measurements.GreatCircleDistanceKm(sf, nyc);
double m    = H3Measurements.GreatCircleDistanceM(sf, nyc);
double rads = H3Measurements.GreatCircleDistanceRads(sf, nyc);
```

Cell area and edge length are available directly on `H3Index`:

```csharp
double areaKm2 = H3Index.CellAreaKm2(cell);
double areaM2  = H3Index.CellAreaM2(cell);
double edgeKm  = H3Index.ExactEdgeLengthKm(directedEdge);
```

---

### `H3` (Global Functions)

```csharp
long total = H3.GetNumCells(resolution: 5);       // Total cell count at this resolution
IReadOnlyList<H3Index> base  = H3.GetRes0Cells();  // All 122 base cells
IReadOnlyList<H3Index> pentagons = H3.GetPentagons(resolution: 5); // 12 pentagons
```

---

## Supported Platforms

The `CK.H3.Binaries` package includes prebuilt native binaries for the following platforms:

| Runtime Identifier | Architecture | OS |
|---|---|---|
| `win-x64` | x64 | Windows |
| `linux-x64` | x64 | Linux |
| `linux-arm64` | ARM64 | Linux |

**Other platforms (macOS, Linux ARM32, etc.):** use the `CK.H3` package alone and provide the native H3 library yourself. Compile H3 from [source](https://github.com/uber/h3) and make sure the resulting binary (`h3.dll`, `libh3.so`, or `libh3.dylib`) is resolvable at runtime — either placed next to your application's output or available on the system library path (`PATH` on Windows, `LD_LIBRARY_PATH` or `ldconfig` on Linux/macOS). No code or configuration changes are needed in `CK.H3` itself.

---

## Error Handling

Native H3 errors are thrown as `H3Exception` with a typed `ErrorCode` property:

```csharp
try
{
    H3Index edge = cell.DirectedEdgeTo(nonNeighbor);
}
catch (H3Exception ex) when (ex.ErrorCode == H3ErrorCode.NotNeighbors)
{
    // handle gracefully
}
```

The full list of error codes is available in `H3ErrorCode`.

---

## License

This library is licensed under the [Apache License 2.0](LICENSE).

It wraps the [Uber H3 library](https://github.com/uber/h3) (Apache 2.0) and incorporates work from [DGGRID](https://github.com/sahrk/DGGRID) (Copyright 2015 Southern Oregon University). See [NOTICE](NOTICE) for full attribution details.
