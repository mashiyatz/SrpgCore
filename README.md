# SrpgCore

Deterministic Fire Emblem-style tactics engine in pure C#. No rendering, no engine API, no third-party packages — any host (Unity, Godot, Blazor, web backend, Prowl, MonoGame, …) consumes it the same way.

- Target: `netstandard2.0`, zero dependencies. Runs anywhere .NET runs.
- Deterministic: one seeded RNG stream per concern (combat, growths). Same seed + same inputs = same battle, every time.
- Tested: `SrpgCore.Verify` offline console verifier — **463 checks, 0 failures**. No test packages, runs anywhere `dotnet` exists.

## Layout

| Path | Contents |
| --- | --- |
| `SrpgCore/` | The library: `Grid`, `Data`, `Combat`, `Events`, `Regions`, `Random`, `Tactics` |
| `SrpgCore.Verify/` | Offline verifier (plain `<ProjectReference>`, no test SDK) |
| `SrpgCore.slnx` | Solution for these two projects |

## Quick start

```bash
dotnet build SrpgCore.slnx
dotnet run --project SrpgCore.Verify/SrpgCore.Verify.csproj
# expect: 463 passed, 0 failed
```

Reference the library from any host:

```xml
<!-- netstandard2.0 is binary-compatible with Unity, Godot, Blazor, Prowl, MonoGame -->
<ProjectReference Include="path/to/SrpgCore/SrpgCore.csproj" />
```

Or drop the built `SrpgCore.dll` under Unity's `Assets/Plugins/` (Prowl's script compiler ignores `<ProjectReference>`, so it takes the DLL copy).

## Sixty-second battle

```csharp
using SrpgCore.Combat;
using SrpgCore.Data;
using SrpgCore.Grid;
using SrpgCore.Random;

var terrains = StandardTerrains.Create();
var board = Board.FromRows(
    new[] { ".....", ".FF..", "..WT." }, "plain",
    new Dictionary<string, string> { ['.'] = "plain", ['F'] = "forest", ['W'] = "wall", ['T'] = "throne" },
    terrains);

var sword = new Weapon("iron-sword", "Iron Sword", WeaponType.Sword, WeaponRank.E,
    might: 5, hit: 90, crit: 0, weight: 5, minRange: 1, maxRange: 1, maxUses: 40);
var axe = new Weapon("iron-axe", "Iron Axe", WeaponType.Axe, WeaponRank.E,
    might: 6, hit: 80, crit: 0, weight: 8, minRange: 1, maxRange: 1, maxUses: 40);

var atk = new Unit("hero", "Hero", Affiliation.Player, "mercenary",
    new Stats { MaxHP = 20, Power = 5, Skill = 6, Speed = 7, Luck = 3, Defense = 4, Resistance = 1, Constitution = 8 });
var def = new Unit("foe", "Foe", Affiliation.Enemy, "brigand",
    new Stats { MaxHP = 22, Power = 6, Skill = 4, Speed = 3, Luck = 1, Defense = 3, Resistance = 0, Constitution = 9 });
atk.Inventory.Add(new InventoryEntry(sword));
def.Inventory.Add(new InventoryEntry(axe));

var rng = new SeededRng(42); // or ChapterState.StartNew(chapter, seed).CombatRng

CombatForecast fc = CombatResolver.Forecast(atk, sword, def, axe, 0, 0, 0, 0);
CombatResult result = CombatResolver.Resolve(
    atk, atk.Inventory[0], def, def.Inventory[0],
    distance: 1, atkAvo: 0, atkDef: 0, defAvo: 0, defDef: 0, rng);
```

`Forecast` is pure (forecast windows, AI scoring, and tests share it). `Resolve` consumes the RNG and mutates HP: attacker strikes (possibly twice at 4+ attack-speed lead), a living defender in range counters between strikes.

## What it covers

| Namespace | Responsibility | Key types |
| --- | --- | --- |
| `Grid` | Board geometry, movement costs, Dijkstra range, A\* path | `Board`, `GridPoint`, `RangeFinder`, `MovementGroups` |
| `Data` | Units, classes, weapons, terrains, chapter format + loader | `Unit`, `UnitClass`, `Weapon`, `Chapter`, `MarkdownChapterLoader` |
| `Combat` | GBA FE7/8 battle math, hit modes, forecast + seeded resolver | `CombatRules`, `WeaponTriangle`, `CombatResolver`, `SkillCatalog` |
| `Events` | Trigger / condition / `verb;args` scripting | `EventTrigger`, `GameEvent`, `CommandCatalog`, `EventRunner`, `EventDispatcher` |
| `Regions` | Named tile sets (event, formation) | `Region` |
| `Random` | Seeded streams + save/restore | `SeededRng` |
| `Tactics` | Chapter runtime: phases, objectives, AI, saves | `TurnManager`, `ChapterState`, `ChapterRunner`, `EnemyAi`, `SaveSystem` |

Formulas follow GBA FE7/8 with tunables centralized in `CombatRules` (triangle bonuses, doubling threshold, hit mode, effective multiplier) — never scattered constants. Per-difficulty hit models: 1RN, 2RN (default), 3RN, Fates curve. Crits are always 1RN and deal ×3.

Chapters are single Markdown files parsed by `MarkdownChapterLoader.Load(text, fileName)` — every error reports as `file:line`, and a failed load returns no chapter so hosts never run half-parsed data. Event commands are `verb;arg;…` text validated by `CommandCatalog` before any side effect; new verbs need a catalog entry + executor handling + a Verify check.

## Design rules

- **Deterministic.** Seeded RNG only, no wall-clock reads, no static mutable state.
- **Pure where it matters.** Forecasts, ranges, paths, and objective checks are pure functions. Engine effects sit behind interfaces (e.g. `IEventExecutor`).
- **Data over code.** Movement groups, terrain costs, hit modes, and formula tunables are data or config.
- **Fail fast for authors.** Map glyphs, event verbs, chapter references, and conditions are validated with positions and names, never silently ignored.
- **Hosts stay thin.** Rendering, input, audio, file I/O, and networking are host concerns. The core exposes data (`CombatForecast`, reachable-tile sets, `ChapterSnapshot` DTOs) and interfaces so Unity, Godot, Blazor, or a headless `ChapterRunner` can all drive the same code path the verifier covers.

## Hosting (all peers, none privileged)

| Concern | Core API | Host job |
| --- | --- | --- |
| Board + movement | `Board`, `RangeFinder`, `Battlefield.Reachable` | Tilemap, range highlights, path preview |
| Forecast UI | `CombatResolver.Forecast` + `TerrainAt` | Forecast window, effective/triangle badges |
| Combat | `Resolve` (durability) / `ResolveHeal` | Attack animation, HP bars |
| AI | `EnemyAi.Decide` + `ThreatCalculator` | Enemy turn driver, danger-zone overlay |
| Events | `EventRunner` / `EventDispatcher` + `IEventExecutor` | Dialogue, portraits, movement tweens |
| Objectives / fog / rescue / support / saves | `ChapterState`, `FogCalculator`, `Rescue`, `SupportLedger`, `SaveSystem` | Banners, hide tokens, carry/drop, bond bonuses, persistence |

Worked Unity / Godot / Blazor / logic-server examples live in the sibling `SRPGDocs` site (`docs/hosting.md`) — same core calls on every host.

## Full documentation

The searchable reference is hosted on GitHub Pages: **https://mashiyatz.github.io/SrpgCore/** —
architecture, chapter format, combat, tactics, hosting examples, and the
per-function API reference generated from the XML docs on every public member.
The same content is the sibling `SRPGDocs` project (`docs/*.md`, Solid.js + Vite)
for local edits; this README is the landing page. The `SrpgCore.Verify` suite is
the authority when they disagree.

## Consumers

Every consumer references the built library — never copies source:

- **SRPGMono** (MonoGame frontend + chapter editor) via `<ProjectReference>`.
- **SRPGEditor** (Prowl engine) via committed `Assets/Plugins/SrpgCore.dll` (Prowl's compiler ignores project references; refresh by building Release and copying the DLL).
