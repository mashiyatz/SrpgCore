# Grid

## Coordinates

[`GridPoint`](api-grid.md#gridpoint) is a zero-based immutable `(X, Y)` struct: X is the column (east), Y is the
row (south). It offers Manhattan distance, value equality, and hashing, so it works as
a dictionary key throughout the library.

## Board

[`Board`](api-grid.md#board) holds a terrain-key layout plus a terrain dictionary. It validates bounds and
terrain keys on write, and throws `ArgumentOutOfRangeException` on out-of-bounds reads.

[`Board.FromRows`](api-grid.md#board-fromrows) authors maps from ASCII art:

```csharp
var board = Board.FromRows(
    new[] { ".....", ".FF..", "..WT." },
    defaultTerrain: "plain",
    legend: new Dictionary<char, string> { ['.'] = "plain", ['F'] = "forest", ['W'] = "wall", ['T'] = "throne" },
    terrains: StandardTerrains.Create());
```

Rows must be rectangular; unknown glyphs throw `FormatException` with `(x, y)`
coordinates. Legend keys are `char` (BMP glyphs only).

## Terrain and movement groups

[`TerrainType`](api-grid.md#terraintype) carries per-group move costs, `Avoid`, `Defense`, and [`HpEffectPercent`](api-grid.md#terraintype-hpeffectpercent).
Costs are keyed by **movement-group name** (`"infantry"`, `"armor"`, `"cavalry"`,
`"flier"`, or any custom group such as `"pirate"`), so new movement types are data
rows, not code branches. Unknown groups default to cost 1. Cost `< 0` or `>= 99`
means impassable.

`HpEffectPercent` is the per-round HP swing as a percentage of max HP: positive heals,
negative damages, zero does nothing (see [data](data.md) for application timing).

## Range and path

[`RangeFinder`](api-grid.md#rangefinder) provides three pure operations:

- [`ComputeReachable`](api-grid.md#rangefinder-computereachable) — Dijkstra flood over tile-entry costs, returning each reachable
  tile mapped to movement points remaining on arrival. The `blocked` set excludes
  tiles (units); the origin is always allowed.
- [`FindPath`](api-grid.md#rangefinder-findpath) — A\* with a Manhattan heuristic, returning the cheapest path within
  budget or `null`. The heuristic assumes per-tile costs stay `>= 1`; zero-cost custom
  terrain still yields a valid path but it may be suboptimal.
- [`AttackTiles`](api-grid.md#rangefinder-attacktiles) — the Manhattan ring between min and max range. Rendering and range
  checks share this one predicate so they can never diverge.

Both search overloads accept either a `MoveType` or a raw group name. The open list is
a linear scan rather than a heap — maps are small and `netstandard2.0` has no
`PriorityQueue`; replace only with profiling evidence.

## Example

```csharp
// Movement range, path preview, and strike ring for one unit.
var blocked = field.BlockingTiles(unit);
var range = RangeFinder.ComputeReachable(
    board, unit.Position, unit.MovePoints, unit.MovementGroup, blocked);
foreach (var tile in range.Keys) Highlight(tile); // tile -> move remaining

var path = RangeFinder.FindPath(
    board, unit.Position, cursor, unit.MovePoints, unit.MovementGroup, blocked);
if (path != null) Animate(path); // cheapest route within budget, null if unreachable

var ring = RangeFinder.AttackTiles(board, unit.Position, sword.MinRange, sword.MaxRange);
bool canStrike = ring.Contains(foe.Position);
```

## Blocking rule

Exactly one rule, enforced in [`Battlefield`](api-tactics.md#battlefield): **enemies block pass-through; allies are
pass-through but cannot be landed on.**
