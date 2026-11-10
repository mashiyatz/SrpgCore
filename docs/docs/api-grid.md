# API: SrpgCore.Grid

> Generated from `../SrpgCore` sources by `scripts/build-api.ts` — do not edit by hand.

Guide: [grid](grid.md).

## Board

Pure-logic board holding terrain layout (Width, Height, Terrains, and a per-tile terrain key grid). Units and obstacles are not stored here; callers overlay them as blocked/ally sets when querying range and paths.

### Board.Width

Gets the board width in tiles.

```csharp
public int Width { get; }
```

Type: `int`

### Board.Height

Gets the board height in tiles.

```csharp
public int Height { get; }
```

Type: `int`

### Board.Terrains

Gets the terrain definitions keyed by terrain key.

```csharp
public IReadOnlyDictionary<string, TerrainType> Terrains { get; }
```

Type: `IReadOnlyDictionary<string, TerrainType>`

### Board constructor

Initializes a board filled with the default terrain.

```csharp
public Board(int width, int height, string defaultTerrain, IReadOnlyDictionary<string, TerrainType> terrains)
```

| Parameter | Type | Description |
| --- | --- | --- |
| width | `int` | The board width in tiles. |
| height | `int` | The board height in tiles. |
| defaultTerrain | `string` | The terrain key filling every tile. |
| terrains | `IReadOnlyDictionary<string, TerrainType>` | The terrain definitions keyed by terrain key. |

### Board.InBounds

Returns true when the point lies inside the board bounds.

```csharp
public bool InBounds(GridPoint p)
```

| Parameter | Type | Description |
| --- | --- | --- |
| p | `GridPoint` | The point to test. |

Returns: `bool` — True when inside bounds, otherwise false.

### Board.SetTerrain

Sets the terrain key at the given point.

```csharp
public void SetTerrain(GridPoint p, string terrainKey)
```

| Parameter | Type | Description |
| --- | --- | --- |
| p | `GridPoint` | The point to update. |
| terrainKey | `string` | The new terrain key. |

### Board.GetTerrainKey

Gets the terrain key at the given point.

```csharp
public string GetTerrainKey(GridPoint p)
```

| Parameter | Type | Description |
| --- | --- | --- |
| p | `GridPoint` | The point to read. |

Returns: `string` — The terrain key stored at the point.

### Board.GetTerrain

Gets the terrain definition at the given point.

```csharp
public TerrainType GetTerrain(GridPoint p)
```

| Parameter | Type | Description |
| --- | --- | --- |
| p | `GridPoint` | The point to read. |

Returns: `TerrainType` — The terrain definition at the point.

### Board.FromRows

Builds a board from ASCII rows where each character maps through the legend, throwing on ragged rows or unknown glyphs.

```csharp
public static Board FromRows(string[] rows, string defaultTerrain, IDictionary<char, string> legend, IReadOnlyDictionary<string, TerrainType> terrains)
```

| Parameter | Type | Description |
| --- | --- | --- |
| rows | `string[]` | The ASCII rows with one character per tile. |
| defaultTerrain | `string` | The terrain key filling the board before glyphs are applied. |
| legend | `IDictionary<char, string>` | The map from glyph character to terrain key (BMP characters only). |
| terrains | `IReadOnlyDictionary<string, TerrainType>` | The terrain definitions keyed by terrain key. |

Returns: `Board` — A new board with terrain taken from the given rows.

## GridPoint

Zero-based square-grid coordinate. Values: X column (east) and Y row (south), Manhattan distance and dictionary hashing.

### GridPoint.X

Gets the zero-based column (east).

```csharp
public int X { get; }
```

Type: `int`

### GridPoint.Y

Gets the zero-based row (south).

```csharp
public int Y { get; }
```

Type: `int`

### GridPoint constructor

Initializes a grid coordinate.

```csharp
public GridPoint(int x, int y)
```

| Parameter | Type | Description |
| --- | --- | --- |
| x | `int` | The zero-based column. |
| y | `int` | The zero-based row. |

### GridPoint.ManhattanDistance

Returns the Manhattan distance to another point.

```csharp
public int ManhattanDistance(GridPoint other)
```

| Parameter | Type | Description |
| --- | --- | --- |
| other | `GridPoint` | The other point. |

Returns: `int` — The Manhattan distance to the other point.

### GridPoint.Equals

Returns true when the coordinates match.

```csharp
public bool Equals(GridPoint other)
```

| Parameter | Type | Description |
| --- | --- | --- |
| other | `GridPoint` | The point to compare. |

Returns: `bool` — True when the points are equal.

```csharp
public override bool Equals(object? obj)
```

| Parameter | Type | Description |
| --- | --- | --- |
| obj | `object?` | The object to compare. |

Returns: `bool` — True when the object is an equal grid point.

### GridPoint.GetHashCode

Returns a hash code for dictionary use.

```csharp
public override int GetHashCode()
```

Returns: `int` — A hash code combining X and Y.

### GridPoint.ToString

Returns the coordinate formatted as (X,Y).

```csharp
public override string ToString()
```

Returns: `string` — The coordinate formatted as (X,Y).

### GridPoint operator ==

Compares two points for equality.

```csharp
public static bool operator ==(GridPoint a, GridPoint b)
```

| Parameter | Type | Description |
| --- | --- | --- |
| a | `GridPoint` | The first point. |
| b | `GridPoint` | The second point. |

Returns: `bool` — True when the points are equal.

### GridPoint operator !=

Compares two points for inequality.

```csharp
public static bool operator !=(GridPoint a, GridPoint b)
```

| Parameter | Type | Description |
| --- | --- | --- |
| a | `GridPoint` | The first point. |
| b | `GridPoint` | The second point. |

Returns: `bool` — True when the points differ.

## MoveType

Movement class. Drives per-terrain move costs (fliers ignore most terrain).

| Name | Value | Description |
| --- | --- | --- |
| Infantry | 0 | Standard foot movement. |
| Armor | 1 | Slow heavily armored movement. |
| Cavalry | 2 | Mounted movement. |
| Flier | 3 | Flying movement ignoring most terrain costs. |

## MovementGroups

Named movement groups kept data-driven so new groups need only a cost column per terrain.

### MovementGroups.Infantry

Gets the infantry movement group name.

```csharp
public const string Infantry = ;
```

Type: `string`

### MovementGroups.Armor

Gets the armor movement group name.

```csharp
public const string Armor = ;
```

Type: `string`

### MovementGroups.Cavalry

Gets the cavalry movement group name.

```csharp
public const string Cavalry = ;
```

Type: `string`

### MovementGroups.Flier

Gets the flier movement group name.

```csharp
public const string Flier = ;
```

Type: `string`

### MovementGroups.For

Maps a move type to its movement group name.

```csharp
public static string For(MoveType moveType)
```

| Parameter | Type | Description |
| --- | --- | --- |
| moveType | `MoveType` | The move type to map. |

Returns: `string` — The movement group name for the move type.

## RangeFinder

Dijkstra movement-range, A-star pathfinding, and shared Manhattan attack-ring logic (ComputeReachable/FindPath/AttackTiles). All pure and deterministic.

### RangeFinder.ComputeReachable

Computes reachable tiles via Dijkstra over tile entry costs.

```csharp
public static Dictionary<GridPoint, int> ComputeReachable(Board board, GridPoint origin, int movePoints, MoveType moveType, ISet<GridPoint>? blocked = null)
```

| Parameter | Type | Description |
| --- | --- | --- |
| board | `Board` | The board holding the terrain layout. |
| origin | `GridPoint` | The start tile, always included even when blocked. |
| movePoints | `int` | The movement budget spent on tile entry costs. |
| moveType | `MoveType` | The move type mapped to a movement group. |
| blocked | `ISet<GridPoint>?` | Tiles to exclude as occupied, or null for none. |

Returns: `Dictionary<GridPoint, int>` — Map of reachable tile to movement points remaining on arrival.

```csharp
public static Dictionary<GridPoint, int> ComputeReachable(Board board, GridPoint origin, int movePoints, string movementGroup, ISet<GridPoint>? blocked = null)
```

| Parameter | Type | Description |
| --- | --- | --- |
| board | `Board` | The board holding the terrain layout. |
| origin | `GridPoint` | The start tile, always included even when blocked. |
| movePoints | `int` | The movement budget spent on tile entry costs. |
| movementGroup | `string` | The movement group name used for terrain costs. |
| blocked | `ISet<GridPoint>?` | Tiles to exclude as occupied, or null for none. |

Returns: `Dictionary<GridPoint, int>` — Map of reachable tile to movement points remaining on arrival.

### RangeFinder.FindPath

Finds the cheapest path within budget using A-star with a Manhattan heuristic.

```csharp
public static List<GridPoint>? FindPath(Board board, GridPoint from, GridPoint to, int movePoints, MoveType moveType, ISet<GridPoint>? blocked = null)
```

| Parameter | Type | Description |
| --- | --- | --- |
| board | `Board` | The board holding the terrain layout. |
| from | `GridPoint` | The start tile. |
| to | `GridPoint` | The destination tile, allowed even when blocked. |
| movePoints | `int` | The movement budget limiting total entry cost. |
| moveType | `MoveType` | The move type mapped to a movement group. |
| blocked | `ISet<GridPoint>?` | Tiles to exclude except the destination, or null for none. |

Returns: `List<GridPoint>?` — Cheapest path including both ends, or null when unreachable within budget.

```csharp
public static List<GridPoint>? FindPath(Board board, GridPoint from, GridPoint to, int movePoints, string movementGroup, ISet<GridPoint>? blocked = null)
```

| Parameter | Type | Description |
| --- | --- | --- |
| board | `Board` | The board holding the terrain layout. |
| from | `GridPoint` | The start tile. |
| to | `GridPoint` | The destination tile, allowed even when blocked. |
| movePoints | `int` | The movement budget limiting total entry cost. |
| movementGroup | `string` | The movement group name used for terrain costs. |
| blocked | `ISet<GridPoint>?` | Tiles to exclude except the destination, or null for none. |

Returns: `List<GridPoint>?` — Cheapest path including both ends, or null when unreachable within budget.

### RangeFinder.AttackTiles

Returns Manhattan ring attack tiles around the center, shared by rendering and checks.

```csharp
public static HashSet<GridPoint> AttackTiles(Board board, GridPoint center, int minRange, int maxRange)
```

| Parameter | Type | Description |
| --- | --- | --- |
| board | `Board` | The board used for bounds checks. |
| center | `GridPoint` | The center tile excluded from the result. |
| minRange | `int` | The minimum Manhattan distance included. |
| maxRange | `int` | The maximum Manhattan distance included. |

Returns: `HashSet<GridPoint>` — In-bounds attack tiles within the given range ring.

## TerrainType

Immutable terrain definition. Values: Key (unique id), Name, Avoid, Defense, HpEffectPercent (heal/damage percent), and per-group entry costs (MoveCost/IsPassable; <0 or >=99 means impassable).

### TerrainType.ImpassableThreshold

Gets the cost at or above which terrain counts as impassable.

```csharp
public const int ImpassableThreshold = 99;
```

Type: `int`

### TerrainType.Key

Gets the unique terrain key.

```csharp
public string Key { get; }
```

Type: `string`

### TerrainType.Name

Gets the display name.

```csharp
public string Name { get; }
```

Type: `string`

### TerrainType.Avoid

Gets the avoid bonus granted to units on this terrain.

```csharp
public int Avoid { get; }
```

Type: `int`

### TerrainType.Defense

Gets the defense bonus granted to units on this terrain.

```csharp
public int Defense { get; }
```

Type: `int`

### TerrainType.HpEffectPercent

Gets the per-round HP swing as percent of max HP (positive heals, negative damages, zero none).

```csharp
public int HpEffectPercent { get; }
```

Type: `int`

### TerrainType constructor

Initializes a terrain with classic four-group costs.

```csharp
public TerrainType(string key, string name, int infantry, int armor, int cavalry, int flier, int avoid, int defense, int hpEffectPercent = 0)
```

| Parameter | Type | Description |
| --- | --- | --- |
| key | `string` | The unique terrain key. |
| name | `string` | The display name. |
| infantry | `int` | The infantry entry cost. |
| armor | `int` | The armor entry cost. |
| cavalry | `int` | The cavalry entry cost. |
| flier | `int` | The flier entry cost. |
| avoid | `int` | The avoid bonus. |
| defense | `int` | The defense bonus. |
| hpEffectPercent | `int` | The per-round HP swing percent. |

```csharp
public TerrainType(string key, string name, IDictionary<string, int> moveCosts, int avoid, int defense, int hpEffectPercent = 0)
```

| Parameter | Type | Description |
| --- | --- | --- |
| key | `string` | The unique terrain key. |
| name | `string` | The display name. |
| moveCosts | `IDictionary<string, int>` | The entry costs keyed by movement group name. |
| avoid | `int` | The avoid bonus. |
| defense | `int` | The defense bonus. |
| hpEffectPercent | `int` | The per-round HP swing percent. |

### TerrainType.MoveCost

Returns the entry cost for the given movement group, defaulting to 1 for unknown groups.

```csharp
public int MoveCost(string movementGroup)
```

| Parameter | Type | Description |
| --- | --- | --- |
| movementGroup | `string` | The movement group name. |

Returns: `int` — The entry cost for the group.

```csharp
public int MoveCost(MoveType moveType)
```

| Parameter | Type | Description |
| --- | --- | --- |
| moveType | `MoveType` | The move type mapped to a movement group. |

Returns: `int` — The entry cost for the move type.

### TerrainType.IsPassable

Returns true when the terrain is passable for the given movement group.

```csharp
public bool IsPassable(string movementGroup)
```

| Parameter | Type | Description |
| --- | --- | --- |
| movementGroup | `string` | The movement group name. |

Returns: `bool` — True when the cost is at least 0 and below the impassable threshold.

```csharp
public bool IsPassable(MoveType moveType)
```

| Parameter | Type | Description |
| --- | --- | --- |
| moveType | `MoveType` | The move type mapped to a movement group. |

Returns: `bool` — True when passable for the move type.
