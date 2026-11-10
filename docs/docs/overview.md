# Overview

SrpgCore is an engine-agnostic Fire Emblem-style SRPG core library in C#. Pure logic —
grid, data, combat, events, tactics, chapters — with no rendering, no engine API, and
no third-party packages. It targets `netstandard2.0` so the same assembly runs under
Unity, Godot, Blazor, Flax, Prowl, or any other .NET host.

## Design rules

- **Deterministic.** One seeded RNG stream per concern (combat, growths). Same seed +
  same inputs always give the same battle, so forecasts, AI, replays, and tests share
  one code path. No wall-clock reads, no static mutable state.
- **Pure where it matters.** Forecasts, ranges, paths, and objective checks are pure
  functions. Anything that touches the outside world (dialogue, movement animation,
  spawning visuals) sits behind an interface such as [`IEventExecutor`](api-events.md#ieventexecutor).
- **Data over code.** Movement groups, terrain costs, hit modes, and formula tunables
  are data or config. New unit types and terrain need no code changes.
- **Fail fast for authors.** Map glyphs, event verbs, chapter references, and
  conditions are validated with positions and names, never silently ignored.

## Repository layout

SrpgCore lives in its own project, separate from any engine host:

| Location | Contents |
| --- | --- |
| `SrpgCore/` | The library (`Grid`, `Data`, `Combat`, `Events`, `Regions`, `Random`, `Tactics`) |
| `SrpgCore.Verify/` | Offline console verifier, no test packages — run anywhere `dotnet` exists |
| `SrpgCore.slnx` | Solution for this project only |

Engine hosts are separate sibling projects that reference the built library —
see [hosting](hosting.md) for how Unity, Godot, Blazor, and web clients each pull
it in. None of them vendors or duplicates this source.

## Commands

```bash
dotnet build SrpgCore.slnx
dotnet run --project SrpgCore.Verify/SrpgCore.Verify.csproj
```

The verifier runs 463 checks across grid, RNG, combat, events, phases, objectives, AI,
chapters, durability/effectiveness, experience, staves, new event verbs,
terrain-aware AI, seize, saves, promotion, fog, rescue, skills, support, statuses,
staff accuracy, combat gains, skill/support battle math, battle triggers, phase tick,
deployment selection, talk, inventory verbs, interactables, weapon ranks, ally AI,
fog-filtered threat/AI, AI tie-breaks, dance, canto, shove/swap/steal/healer/hold/regen/convoy/support-tick/replay-hash, warp/torch, set_terrain, MaxInventory, ReversesTriangle, FormationRegion, BecomesTerrainKey, shop gold/buy/sell, and exits non-zero on failure.

## Sixty-second battle

```csharp
using SrpgCore.Combat; using SrpgCore.Data; using SrpgCore.Grid; using SrpgCore.Tactics;

var terrains = StandardTerrains.Create();
var board = Board.FromRows(new[] { ".....", ".FF..", "..WT." }, "plain",
    new Dictionary<char, string> { ['.'] = "plain", ['F'] = "forest", ['W'] = "wall", ['T'] = "throne" },
    terrains);
var sword = new Weapon("iron-sword", "Iron Sword", WeaponType.Sword, WeaponRank.E,
    might: 5, hit: 90, crit: 0, weight: 5, minRange: 1, maxRange: 1, maxUses: 40);
var atk = new Unit("hero", "Hero", Affiliation.Player, "mercenary",
    new Stats { MaxHP = 20, Power = 5, Skill = 6, Speed = 7, Luck = 3, Defense = 4, Resistance = 1, Constitution = 8 });
var blade = new InventoryEntry(sword);
atk.Inventory.Add(blade);

var rng = new SeededRng(42); // or ChapterState.StartNew(chapter, seed).CombatRng
CombatForecast fc = CombatResolver.Forecast(atk, sword, foe, null, 0, 0, 0, 0);
CombatResult result = CombatResolver.Resolve(atk, blade, foe, null, 1, 0, 0, 0, 0, rng);
```
