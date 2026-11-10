# API: SrpgCore.Regions

> Generated from `../SrpgCore` sources by `scripts/build-api.ts` — do not edit by hand.

Guide: [chapter-format](chapter-format.md).

## RegionKind

Gameplay meaning of a named tile set.

| Name | Value | Description |
| --- | --- | --- |
| Event |  | Raises RegionEntered for the event system. |
| Formation |  | Allowed deployment tiles. |

## Region

Named set of tiles with a gameplay meaning.

### Region.Name

Unique region name referenced by events and deployment.

```csharp
public string Name { get; }
```

Type: `string`

### Region.Kind

Gameplay meaning of this tile set.

```csharp
public RegionKind Kind { get; }
```

Type: `RegionKind`

### Region.Tiles

Tile membership set for this region.

```csharp
public ISet<GridPoint> Tiles => _tiles;
```

Type: `ISet<GridPoint>`

### Region constructor

Creates a named region with the given gameplay kind.

```csharp
public Region(string name, RegionKind kind)
```

| Parameter | Type | Description |
| --- | --- | --- |
| name | `string` | Unique region name. |
| kind | `RegionKind` | Gameplay meaning of the tile set. |

### Region.Add

Adds a tile to the region set.

```csharp
public void Add(GridPoint p)
```

| Parameter | Type | Description |
| --- | --- | --- |
| p | `GridPoint` | The tile to add. |

### Region.Contains

Checks whether a tile belongs to the region.

```csharp
public bool Contains(GridPoint p)
```

| Parameter | Type | Description |
| --- | --- | --- |
| p | `GridPoint` | The tile to test. |

Returns: `bool` — True when the tile is a member of the region.
