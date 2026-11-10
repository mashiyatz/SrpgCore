# AGENTS.md — SrpgCore

Portable SRPG core: a Fire Emblem-style tactics engine as a pure C# library. No
rendering, no engine API, no third-party packages — any host (Unity, Godot, Blazor,
a web backend, Prowl, ...) consumes it the same way. This is the IP; keep it engine-free.

## Layout

- `SrpgCore/` — the library itself: `netstandard2.0`, zero packages, zero engine
  references. Namespaces: `Grid` (board, data-driven movement groups, Dijkstra
  range, A* path, shared attack-ring predicate), `Data` (units/classes/weapons/
  terrains/chapters, markdown chapter format + loader, terrain HP effects),
  `Combat` (triangle, per-difficulty hit modes, GBA-style forecast + seeded
  resolver), `Events` (LT-style trigger/condition/`verb;args` commands, priority
  dispatch), `Regions`, `Random` (seeded streams + save/restore), `Tactics` (phase
  cycler, rout/survive/protect objectives, battlefield placements with one
  blocking rule, threat ranges, greedy enemy AI, chapter runtime: state, `<=`
  reinforcement spawns, headless runner).
- `SrpgCore.Verify/` — offline console verifier (no test packages, runs anywhere
  `dotnet` exists). References `SrpgCore` via a plain `<ProjectReference>`.
- `SrpgCore.slnx` — solution for this repo's two projects only.
- `bin/`, `obj/`, `.vs/` — generated, gitignored.

## Commands

- `dotnet build SrpgCore.slnx` — build the library and verifier.
- `dotnet run --project SrpgCore.Verify/SrpgCore.Verify.csproj` — run all checks
  (306 checks across grid, RNG, combat, events, phases, objectives, AI, chapters,
  durability/effectiveness, experience, staves, new event verbs, terrain-aware AI,
  seize, saves, promotion, fog, rescue, skills, support, statuses, staff accuracy,
  combat gains, skill/support battle math, battle triggers, phase tick, deployment
  selection, talk, inventory verbs, interactables, weapon ranks, ally AI,
  fog-filtered threat/AI, AI tie-breaks, dance, canto, shove/swap/steal/healer/hold/regen/convoy/support-tick/replay-hash, warp/torch, set_terrain, MaxInventory, ReversesTriangle, FormationRegion, BecomesTerrain; exits non-zero on failure). Keep this figure in sync with
  `../SRPGDocs/docs/overview.md`.
- No other CLI build/test/lint exists for this project.

## Conventions

- Deterministic: one seeded RNG stream per concern (combat, growths), no
  wall-clock reads, no static mutable state. Pure functions for forecast/range/
  pathing; engine effects only ever sit behind an interface (e.g. `IEventExecutor`).
- Combat formula defaults are GBA FE7/8; tunables live in `CombatRules`, not
  scattered constants.
- Event commands are `verb;arg;...` text validated by `CommandCatalog` (fail
  before any side effect). New verbs need a catalog entry + executor handling +
  a Verify check.
- Fail fast for authors: map glyphs, event verbs, chapter references, and
  conditions are validated with positions and names, never silently ignored.
- Public API carries XML docs: every public method/ctor/delegate needs
  `<summary>` + a `<param>` per argument + `<returns>` (non-void); every public
  property/field/enum member needs `<summary>`. Tag text stays on one line with
  no `|` characters. SRPGDocs generates its per-function API reference from
  these (`scripts/build-api.ts`, warns on gaps) — keep them accurate.
- Do not create README, test harness, or CI config unprompted — this project has
  none beyond `SrpgCore.Verify`.

## Next steps

Grounded in what's actually in the source, not a wishlist — update this section
as items land or new gaps turn up.

Landed (each Verify-pinned):
- **Status regen + hold AI + replay hash** — `PhaseTickRules.RegenHeal` for `regen` status, `Unit.AiType=hold` gate in `EnemyAi`, `Tactics.ReplayHash` deterministic hash.
- **Support tick** — `SupportLedger.TickAdjacent` grants 1 pt per adjacent alive pair per call (GBA-flavored), hosts call at phase end.
- **Save convoy/support** — `ChapterState.Convoy`/`Supports` now round-trip through `SaveSystem` (AllPairs/SetPoints), armory keys are the save contract.
- **Healer AI** — `EnemyAi` healer branch: when a heal staff can reach a wounded ally, heals the most-missing-HP ally (reachable-tile scan, staff range, fog-aware).
- **Steal** — `Tactics.Steal` + `Weapon.CanSteal`: adjacent steal when thief Speed > target and item Weight ≤ thief Con, plus `steal;thief;target;weapon_key` verb.
- **Shove/Swap** — `Tactics.Shove`: adjacent push (Con ≤ carrier Con+2, dest free/in-bounds) and position swap, plus `shove;shover;target` and `swap;a;b` verbs.
- **Dance** — `Tactics.Dance`: adjacent refresh for `skill:dance` units with
  host-tracked acted flags, plus a `dance;dancer;target` verb (catalog entry +
  runner handling that fails fast). Hosts clear their own acted flag for the
  target on success; headless runs only validate + log.
- **Canto** — `Tactics.Canto`: leftover-move repositioning for `skill:canto`
  units (`RemainingMove` = total − spent, clamped at 0, negatives throw). Hosts
  pass laden totals through `Rescue.EffectiveMove`.
- **EnemyAi tie-break** — `ScoreTile` now breaks score ties by remaining move,
  then nearest target, then coordinate order, matching `SRPGDocs/docs/tactics.md`
  and the class doc comment (previously fell out of iteration order past
  remaining move).
- **`RegionKind.Status` dropped** — was declared but unread, with no effect
  payload in the format; the loader now rejects `status` kinds ("Bad region
  kind"). Revisit only with an effect-payload design (format + PhaseTick + docs).

Not a next step — a deliberate, already-documented tradeoff, don't "fix" without
profiling evidence:
- `Grid/RangeFinder.cs`'s open list is a linear scan rather than a heap, because
  `netstandard2.0` has no `PriorityQueue` and the maps here are small (see the
  comment at the top of `ComputeReachable`). `SRPGDocs/docs/grid.md` already
  documents this; it isn't missing work.

## Consumers (outside this repo, same machine)

This library has no privileged host — every consumer below is a peer, reached
through `Assets/Plugins/SrpgCore.dll`, a `<ProjectReference>`, or an ordinary
NuGet-style reference, never by copying source:

- `C:\Users\mashi\Desktop\game-engine-projects\Prowl\SRPGEditor` — a Prowl-engine
  spike consuming this core via a handwritten `Assets/Plugins/SrpgCore.dll` copy
  (Prowl's script compiler ignores `<ProjectReference>`). Read its `AGENTS.md`
  before touching it; refresh procedure for the DLL copy lives there.
- `C:\Users\mashi\Desktop\game-engine-projects\Prowl\SRPGDocs` — searchable docs
  site for this library (Solid.js + Vite + bun), including worked hosting examples
  for Unity/Godot/Blazor/web clients. Read its `AGENTS.md` before touching it.
  Keep its doc claims in sync with this library's actual behavior — the Verify
  suite here is the authority, not the docs.
- `C:\Users\mashi\Desktop\game-engine-projects\LTMaker` — canonical LT-Maker
  reference: `lt-maker/` engine+editor source (Python/pygame-ce + PyQt5, branch
  `dandylion`), `lt-games/` sample `.ltproj` content, `lt-maker-runtime/` web
  experiment. Read its `AGENTS.md` before touching it (codegen-first: run
  `python -m app.engine.codegen.source_generator` before tests; Python 3.11.8).
  Use for: event-command semantics, database (`nid` cross-reference) shapes,
  editor-scope reference. Do not port its Python; mirror behavior here.
- `C:\Users\mashi\Desktop\web-projects\kanji-srpg` — working Solid.js web SRPG
  (kanji-glyph tiles). Its `src/movement.ts` (BFS/A*/threat ranges), `src/combat.ts`,
  `src/ai.ts`, `src/events.ts` (pure trigger→scene lookup) parallel this library —
  use as cross-implementation check and as a web playtest vehicle. Read its
  `AGENTS.md` before touching it (pnpm 9, ESLint zero-warnings, `tsc --noEmit`).
