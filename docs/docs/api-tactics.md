# API: SrpgCore.Tactics

> Generated from `../SrpgCore` sources by `scripts/build-api.ts` — do not edit by hand.

Guide: [tactics](tactics.md).

## AiRules

Data-driven enemy scoring weights (VH and FE style additive scoring).

### AiRules.KillBonus

Flat bonus for a secured kill, added to damage.

```csharp
public int KillBonus { get; set; }
```

Type: `int`

### AiRules.CounterWeight

Weight on expected counter damage subtracted as risk.

```csharp
public double CounterWeight { get; set; }
```

Type: `double`

### AiRules.ParkingThreatWeight

Weight on threat count when parking where no strike lands.

```csharp
public double ParkingThreatWeight { get; set; }
```

Type: `double`

### AiRules.ParkingAvoidWeight

Bonus per terrain avoid point when parking.

```csharp
public double ParkingAvoidWeight { get; set; }
```

Type: `double`

### AiRules.ParkingDefenseWeight

Bonus per terrain defense point when parking.

```csharp
public double ParkingDefenseWeight { get; set; }
```

Type: `double`

### AiRules constructor

Builds default scoring weights.

```csharp
public AiRules()
```

## AllyAi

Ally-phase brain: the same greedy EnemyAi machinery, but with counterWeight 0.25 instead of the enemy default 0.5 — GBA greens wade in and discount counter-risk (tuning, not a formula change). Hosts call this for Affiliation.Ally units on the ally phase; fog filtering passes straight through to the enemy implementation.

### AllyAi.Decide

Chooses the greedy move-then-strike action for one ally unit with counter weight 0.25.

```csharp
public static AiAction Decide(Battlefield field, PlacedUnit self, string movementGroup, int movePoints, EnemyAi.TerrainOf? terrainOf = null, EnemyAi.DefenderWeaponOf? defenderWeaponOf = null, CombatRules? rules = null, SupportLedger? supports = null, HashSet<GridPoint>? visibleTiles = null)
```

| Parameter | Type | Description |
| --- | --- | --- |
| field | `Battlefield` | Board plus placements (reads blocking and ally tiles). |
| self | `PlacedUnit` | Acting ally placement (position and inventory are read). |
| movementGroup | `string` | Movement group key for reachable tiles. |
| movePoints | `int` | Movement budget for this turn. |
| terrainOf | `EnemyAi.TerrainOf?` | Optional avoid and defense lookup per tile. |
| defenderWeaponOf | `EnemyAi.DefenderWeaponOf?` | Optional foe weapon picker by distance. |
| rules | `CombatRules?` | Combat tunables (defaults when null). |
| supports | `SupportLedger?` | Optional bond bonuses for strike and counter forecasts. |
| visibleTiles | `HashSet<GridPoint>?` | Optional fog set with foes outside skipped. |

Returns: `AiAction` — The chosen move target, weapon, and foe (or wait).

## BattleTriggers

Host-callable trigger variable builders for the battle loop. Each helper returns the vars dictionary a GameEvent condition reads (nested string/object dictionaries, same shape as the hand-built death vars in Verify: unit.nid, unit2.nid, item, position.x/y, region). Pure: no mutation, no RNG. Hosts feed the result to EventDispatcher.Dispatch with the matching EventTrigger (CombatStart, CombatEnd, UnitLevelUp, UnitWait, UnitDeath, RegionEntered).

### BattleTriggers.CombatStartVars

Vars for EventTrigger.CombatStart: unit, unit2, item, position.

```csharp
public static Dictionary<string, object?> CombatStartVars(Unit attacker, Unit defender, string weaponKey, GridPoint pos)
```

| Parameter | Type | Description |
| --- | --- | --- |
| attacker | `Unit` | Attacking unit. |
| defender | `Unit` | Defending unit. |
| weaponKey | `string` | Attacking weapon key. |
| pos | `GridPoint` | Tile the attacker strikes from. |

Returns: `Dictionary<string, object?>` — Vars dictionary for the CombatStart trigger.

### BattleTriggers.CombatEndVars

Vars for EventTrigger.CombatEnd: combat-start vars plus defender_died.

```csharp
public static Dictionary<string, object?> CombatEndVars(Unit attacker, Unit defender, string weaponKey, GridPoint pos, bool defenderDied)
```

| Parameter | Type | Description |
| --- | --- | --- |
| attacker | `Unit` | Attacking unit. |
| defender | `Unit` | Defending unit. |
| weaponKey | `string` | Attacking weapon key. |
| pos | `GridPoint` | Tile the attacker struck from. |
| defenderDied | `bool` | True when the defender fell in the exchange. |

Returns: `Dictionary<string, object?>` — Vars dictionary for the CombatEnd trigger.

### BattleTriggers.LevelUpVars

Vars for EventTrigger.UnitLevelUp: unit (nid, level, hp).

```csharp
public static Dictionary<string, object?> LevelUpVars(Unit unit)
```

| Parameter | Type | Description |
| --- | --- | --- |
| unit | `Unit` | Unit that leveled up. |

Returns: `Dictionary<string, object?>` — Vars dictionary for the UnitLevelUp trigger.

### BattleTriggers.UnitWaitVars

Vars for EventTrigger.UnitWait: unit, position.

```csharp
public static Dictionary<string, object?> UnitWaitVars(Unit unit, GridPoint pos)
```

| Parameter | Type | Description |
| --- | --- | --- |
| unit | `Unit` | Unit that ended its turn. |
| pos | `GridPoint` | Tile the unit waited on. |

Returns: `Dictionary<string, object?>` — Vars dictionary for the UnitWait trigger.

### BattleTriggers.UnitDeathVars

Vars for EventTrigger.UnitDeath: unit, position.

```csharp
public static Dictionary<string, object?> UnitDeathVars(Unit unit, GridPoint pos)
```

| Parameter | Type | Description |
| --- | --- | --- |
| unit | `Unit` | Unit that died. |
| pos | `GridPoint` | Tile the unit died on. |

Returns: `Dictionary<string, object?>` — Vars dictionary for the UnitDeath trigger.

### BattleTriggers.RegionEnteredVars

Vars for EventTrigger.RegionEntered: unit, region, position.

```csharp
public static Dictionary<string, object?> RegionEnteredVars(Unit unit, Region region, GridPoint pos)
```

| Parameter | Type | Description |
| --- | --- | --- |
| unit | `Unit` | Unit that entered the region. |
| region | `Region` | Region entered. |
| pos | `GridPoint` | Tile inside the region. |

Returns: `Dictionary<string, object?>` — Vars dictionary for the RegionEntered trigger.

### BattleTriggers.TurnVars

Vars for phase triggers carrying the turn number.

```csharp
public static Dictionary<string, object?> TurnVars(int turn)
```

| Parameter | Type | Description |
| --- | --- | --- |
| turn | `int` | Current turn number. |

Returns: `Dictionary<string, object?>` — Vars dictionary containing turn.

### BattleTriggers.DetectRegionEntries

Region-entry detection for EventTrigger.RegionEntered: reports (unit, region, pos) for every placed unit whose current tile is inside a region its previous tile was outside of. Units with no previous position (fresh spawns) and units that stayed put or left a region never report. Pure; order follows placements.

```csharp
public static List<(PlacedUnit unit, Region region, GridPoint pos)> DetectRegionEntries(IEnumerable<PlacedUnit> placed, IReadOnlyDictionary<string, GridPoint> previousPositions, IEnumerable<Region> regions)
```

| Parameter | Type | Description |
| --- | --- | --- |
| placed | `IEnumerable<PlacedUnit>` | Current placements to test. |
| previousPositions | `IReadOnlyDictionary<string, GridPoint>` | Tile per unit nid before movement. |
| regions | `IEnumerable<Region>` | Regions to test entries against. |

Returns: `List<(PlacedUnit unit, Region region, GridPoint pos)>` — One entry per unit and region newly entered.

## PlacedUnit

A unit plus its current board tile. Values: Unit (stats, inventory, HP, skills, statuses) and mutable Position.

### PlacedUnit.Unit

The deployed unit data.

```csharp
public Unit Unit { get; }
```

Type: `Unit`

### PlacedUnit.Position

Current board tile of the unit.

```csharp
public GridPoint Position { get; set; }
```

Type: `GridPoint`

### PlacedUnit constructor

Places an existing unit on a tile.

```csharp
public PlacedUnit(Unit unit, GridPoint position)
```

| Parameter | Type | Description |
| --- | --- | --- |
| unit | `Unit` | Unit to place (must not be null). |
| position | `GridPoint` | Starting board tile. |

## Battlefield

Board + unit placements with ONE coherent blocking rule: enemies block pass-through, allies are pass-through but cannot be landed on. Values: Board geometry, Placed placements (PlacedUnit = Unit + Position), and helpers: Deploy/Remove, AreHostile/LiveEnemiesOf, BlockingTiles (hostile positions), AllyTiles, Reachable/ReachableWithRemaining. See ChapterState for the live runtime tying this to phases, RNG, and objectives.

### Battlefield.Board

The board geometry movement and range read.

```csharp
public Board Board { get; }
```

Type: `Board`

### Battlefield.Placed

All deployed placements in deploy order.

```csharp
public IReadOnlyList<PlacedUnit> Placed => _placed;
```

Type: `IReadOnlyList<PlacedUnit>`

### Battlefield constructor

Creates a battlefield over an existing board.

```csharp
public Battlefield(Board board)
```

| Parameter | Type | Description |
| --- | --- | --- |
| board | `Board` | Board geometry (must not be null). |

### Battlefield.Deploy

Deploys a unit on a tile and tracks the placement.

```csharp
public PlacedUnit Deploy(Unit unit, GridPoint position)
```

| Parameter | Type | Description |
| --- | --- | --- |
| unit | `Unit` | Unit to deploy. |
| position | `GridPoint` | Destination tile (must be in bounds). |

Returns: `PlacedUnit` — The new placement wrapping the unit.

### Battlefield.Remove

Removes a placement from the board.

```csharp
public bool Remove(PlacedUnit placed)
```

| Parameter | Type | Description |
| --- | --- | --- |
| placed | `PlacedUnit` | Placement to remove. |

Returns: `bool` — True when the placement was tracked and removed.

### Battlefield.AreHostile

Player and Ally fight the Enemy; Other (neutral) fights nobody.

```csharp
public static bool AreHostile(Affiliation a, Affiliation b)
```

| Parameter | Type | Description |
| --- | --- | --- |
| a | `Affiliation` | First side. |
| b | `Affiliation` | Second side. |

Returns: `bool` — True when the two sides treat each other as foes.

### Battlefield.LiveEnemiesOf

Lists live hostile placements for a unit.

```csharp
public List<PlacedUnit> LiveEnemiesOf(PlacedUnit unit)
```

| Parameter | Type | Description |
| --- | --- | --- |
| unit | `PlacedUnit` | Viewing unit whose foes are collected. |

Returns: `List<PlacedUnit>` — Live placements hostile to the given unit.

### Battlefield.BlockingTiles

Tiles that block pass-through (live hostile positions).

```csharp
public HashSet<GridPoint> BlockingTiles(PlacedUnit unit)
```

| Parameter | Type | Description |
| --- | --- | --- |
| unit | `PlacedUnit` | Moving unit whose foes count as blocking. |

Returns: `HashSet<GridPoint>` — Positions enemies occupy for this unit.

### Battlefield.AllyTiles

Tiles occupied by live non-hostiles (pass-through but not landable).

```csharp
public HashSet<GridPoint> AllyTiles(PlacedUnit unit)
```

| Parameter | Type | Description |
| --- | --- | --- |
| unit | `PlacedUnit` | Moving unit whose allies are collected. |

Returns: `HashSet<GridPoint>` — Positions allies occupy for this unit.

### Battlefield.Reachable

Tiles the unit may end movement on (origin always included).

```csharp
public HashSet<GridPoint> Reachable(PlacedUnit unit, string movementGroup, int movePoints)
```

| Parameter | Type | Description |
| --- | --- | --- |
| unit | `PlacedUnit` | Moving unit (origin and side are read). |
| movementGroup | `string` | Movement group key for terrain costs. |
| movePoints | `int` | Movement budget for this turn. |

Returns: `HashSet<GridPoint>` — Landable tiles excluding ally-occupied tiles except the origin.

### Battlefield.ReachableWithRemaining

Reachable tiles mapped to movement remaining on arrival (includes ally tiles).

```csharp
public Dictionary<GridPoint, int> ReachableWithRemaining(PlacedUnit unit, string movementGroup, int movePoints)
```

| Parameter | Type | Description |
| --- | --- | --- |
| unit | `PlacedUnit` | Moving unit (origin and side are read). |
| movementGroup | `string` | Movement group key for terrain costs. |
| movePoints | `int` | Movement budget for this turn. |

Returns: `Dictionary<GridPoint, int>` — Every reachable tile with the movement left after arriving.

## Canto

Canto (GBA mounted/flying move-again): after acting, a skilled unit may spend whatever movement it had left before the action. Host-tracked: hosts own the acted flags and the spent-move accounting — this class is the shared predicate plus the leftover-move math, so every host grants the same tiles. Canto status travels as skill id "canto" (markdown `skill:canto`, matched case-insensitively). Hosts pass laden totals through Rescue.EffectiveMove.

### Canto.CantoSkillId

Skill id granting canto repositioning.

```csharp
public const string CantoSkillId = ;
```

Type: `string`

### Canto.HasCanto

Reports whether a unit carries the canto skill (case-insensitive).

```csharp
public static bool HasCanto(Data.Unit unit)
```

| Parameter | Type | Description |
| --- | --- | --- |
| unit | `Data.Unit` | Unit to test (null reads as no canto). |

Returns: `bool` — True when the unit lists the canto skill.

### Canto.RemainingMove

Leftover movement after acting (total minus spent before the action).

```csharp
public static int RemainingMove(int moveTotal, int moveSpentBeforeAction)
```

| Parameter | Type | Description |
| --- | --- | --- |
| moveTotal | `int` | Full movement total for the turn. |
| moveSpentBeforeAction | `int` | Movement already spent before acting. |

Returns: `int` — Remaining movement clamped at zero.

### Canto.CanCanto

Reports whether a unit may canto after acting (skill plus leftover move).

```csharp
public static bool CanCanto(Data.Unit unit, int moveTotal, int moveSpentBeforeAction)
```

| Parameter | Type | Description |
| --- | --- | --- |
| unit | `Data.Unit` | Unit to test. |
| moveTotal | `int` | Full movement total for the turn. |
| moveSpentBeforeAction | `int` | Movement already spent before acting. |

Returns: `bool` — True when the skilled unit has movement left to spend.

## ChapterRunner

Headless event executor over a ChapterState: every catalog verb does something observable (move/teleport, spawn, flags, result, dialogue log). Lets chapters run without a renderer — Verify chapters end-to-end here; hosts replace movement and dialogue with animated versions. Unknown-at-runtime failures (missing unit, template, armory entry) throw: authoring errors must be loud.

### ChapterRunner.State

Live chapter runtime this runner mutates.

```csharp
public ChapterState State { get; }
```

Type: `ChapterState`

### ChapterRunner.StageLog

Every executed command, in order — the headless trace.

```csharp
public List<string> StageLog { get; }
```

Type: `List<string>`

### ChapterRunner.Dialogue

Spoken lines (speaker, text) for dialogue tests.

```csharp
public List<(string speaker, string text)> Dialogue { get; }
```

Type: `List<(string speaker, string text)>`

### ChapterRunner.Aside

Ambient lines (speaker, text): comments during play, no advance.

```csharp
public List<(string speaker, string text)> Aside { get; }
```

Type: `List<(string speaker, string text)>`

### ChapterRunner.Carried

Rescued passengers held off-map (nid -> unit) aliasing State.Carried so saves round-trip.

```csharp
public Dictionary<string, Unit> Carried => State.Carried;
```

Type: `Dictionary<string, Unit>`

### ChapterRunner.Convoy

Shared convoy storage — aliases State.Convoy so saves round-trip.

```csharp
public List<InventoryEntry> Convoy => State.Convoy;
```

Type: `List<InventoryEntry>`

### ChapterRunner.Visited

Visited village names, derived from State.Vars ("visited_<name>") — no extra state is held here; the Vars dictionary is the source of truth so saves round-trip it.

```csharp
public HashSet<string> Visited
```

Type: `HashSet<string>`

### ChapterRunner constructor

Creates a headless runner over an existing chapter state.

```csharp
public ChapterRunner(ChapterState state)
```

| Parameter | Type | Description |
| --- | --- | --- |
| state | `ChapterState` | Chapter runtime to execute commands against. |

### ChapterRunner.Execute

Executes one validated event command against the chapter state and logs it.

```csharp
public void Execute(EventCommand command)
```

| Parameter | Type | Description |
| --- | --- | --- |
| command | `EventCommand` | Catalog-validated command to apply. |

## ObjectiveOutcome

Chapter state after polling objectives: still fighting, won, or lost.

| Name | Value | Description |
| --- | --- | --- |
| Ongoing | 0 | No victory or defeat spec is met yet. |
| Victory | 1 | A victory spec (or scripted win) is met. |
| Defeat | 2 | A defeat spec (or scripted loss) is met. |

## ChapterState

Live chapter runtime: the Chapter definition plus Field placements, Phases turn manager, CombatRng/GrowthRng seeded streams, Vars level flags, and spawn bookkeeping. StartNew deploys turn-1 units; call SpawnDue() after each phase advance — spawn comparison is <= (a skipped turn number must never drop reinforcements). EvaluateObjectives polls scripted result, then defeat, then victory specs; AllUnits and SpawnTurnOf feed the checks.

### ChapterState.DefaultTeams

Default phase order when hosts supply no team list. Returns a read-only view so hosts cannot mutate the shared table.

```csharp
public static IReadOnlyList<string> DefaultTeams => Array.AsReadOnly(_defaultTeamsBacking);
```

Type: `IReadOnlyList<string>`

### ChapterState.Chapter

Authored chapter data (Board, Deployments, Events, Objectives).

```csharp
public Chapter Chapter { get; }
```

Type: `Chapter`

### ChapterState.Field

Board plus live placements (LiveEnemiesOf/BlockingTiles/AllyTiles).

```csharp
public Battlefield Field { get; }
```

Type: `Battlefield`

### ChapterState.Phases

Phase cycler tracking turn number and scripted result.

```csharp
public TurnManager Phases { get; }
```

Type: `TurnManager`

### ChapterState.CombatRng

Seeded stream for combat rolls.

```csharp
public SeededRng CombatRng { get; }
```

Type: `SeededRng`

### ChapterState.GrowthRng

Seeded stream for growth rolls.

```csharp
public SeededRng GrowthRng { get; }
```

Type: `SeededRng`

### ChapterState.ProcRng

Seeded stream for skill proc rolls (stream 2). Deterministic and save-scoped.

```csharp
public SeededRng ProcRng { get; }
```

Type: `SeededRng`

### ChapterState.SkillDefs

Snapshot of skill definitions at chapter start for deterministic saves/replays.

```csharp
public Dictionary<string, SkillDef> SkillDefs { get; }
```

Type: `Dictionary<string, SkillDef>`

### ChapterState.SkillCatalogInstance

Read-only view of the snapshotted skill catalog for CombatResolver.

```csharp
public IReadOnlyDictionary<string, SkillDef> SkillCatalogInstance => SkillDefs;
```

Type: `IReadOnlyDictionary<string, SkillDef>`

### ChapterState.Vars

Level flag storage (set_flag, visited_*, opened_*) read by event conditions.

```csharp
public Dictionary<string, object?> Vars { get; }
```

Type: `Dictionary<string, object?>`

### ChapterState.Convoy

Shared convoy storage (host-persisted; also held per-runner headlessly).

```csharp
public List<InventoryEntry> Convoy { get; }
```

Type: `List<InventoryEntry>`

### ChapterState.PartyGold

Party gold for shop buy and sell (gold;N adjusts, buys deduct, sells pay half).

```csharp
public int PartyGold { get; set; }
```

Type: `int`

### ChapterState.Supports

Support point ledger (host-persisted across saves).

```csharp
public SupportLedger Supports { get; }
```

Type: `SupportLedger`

### ChapterState.Carried

Rescued passengers held off-map keyed by nid.

```csharp
public Dictionary<string, Unit> Carried { get; }
```

Type: `Dictionary<string, Unit>`

### ChapterState.StartNew

Builds a chapter runtime and deploys turn-1 units.

```csharp
public static ChapterState StartNew(Chapter chapter, uint seed, IReadOnlyList<string>? teams = null)
```

| Parameter | Type | Description |
| --- | --- | --- |
| chapter | `Chapter` | Authored chapter with board and deployments. |
| seed | `uint` | Base seed for the combat and growth streams. |
| teams | `IReadOnlyList<string>?` | Phase order (defaults to player enemy ally other). |

Returns: `ChapterState` — The new live chapter runtime.

### ChapterState.SpawnDue

Deploy everything due (spawn turn reached, not yet spawned). Returns new arrivals.

```csharp
public List<PlacedUnit> SpawnDue()
```

Returns: `List<PlacedUnit>` — Placements deployed by this call.

### ChapterState.SpawnedNidsSnapshot

Lists nids of deployments already spawned.

```csharp
public HashSet<string> SpawnedNidsSnapshot()
```

Returns: `HashSet<string>` — Nids of spawned deployments.

### ChapterState.SpawnTurnOf

Looks up the scheduled spawn turn for a unit nid (1 when unknown).

```csharp
public int SpawnTurnOf(string nid)
```

| Parameter | Type | Description |
| --- | --- | --- |
| nid | `string` | Unit nid to look up. |

Returns: `int` — The spawn turn recorded for the nid.

### ChapterState.AllUnits

Collects every placed unit on the board.

```csharp
public List<Unit> AllUnits()
```

Returns: `List<Unit>` — Units in placement order.

### ChapterState.EvaluateObjectives

Polls defeat specs then victory specs after honoring any scripted result.

```csharp
public ObjectiveOutcome EvaluateObjectives()
```

Returns: `ObjectiveOutcome` — Ongoing, Victory, or Defeat for the current state.

## CombatGains

Post-combat experience grant: computes the payout from the exchange and banks it on the attacker (level-ups roll the growth RNG stream). Hosts call this after CombatResolver.Resolve with the summed damage dealt and the kill flag. Pure except the growth RNG stream and attacker mutation (same pattern as CombatResolver: same inputs + same seed always give the same result).

### CombatGains.ApplyAfterCombat

Grants post-combat experience and weapon experience to the attacker.

```csharp
public static (int expGained, int levelsGained) ApplyAfterCombat(Unit attacker, Unit defender, int damageDealt, bool didKill, GrowthRates growths, SeededRng growthRng, ExperienceRules? rules = null, Weapon? usedWeapon = null, int wexpOnUse = 1, int wexpOnKill = 3)
```

| Parameter | Type | Description |
| --- | --- | --- |
| attacker | `Unit` | Unit receiving experience. |
| defender | `Unit` | Foe whose level shapes the payout. |
| damageDealt | `int` | Summed damage dealt in the exchange. |
| didKill | `bool` | True when the exchange killed the defender. |
| growths | `GrowthRates` | Growth rates for level-up rolls. |
| growthRng | `SeededRng` | Seeded growth stream. |
| rules | `ExperienceRules?` | Experience tunables (defaults when null). |
| usedWeapon | `Weapon?` | Weapon to grant weapon experience to (skipped when null). |
| wexpOnUse | `int` | Weapon experience granted on a non-killing use. |
| wexpOnKill | `int` | Weapon experience granted on a kill. |

Returns: `(int expGained, int levelsGained)` — (expGained, levelsGained) for the attacker.

## Dance

Dance (FE dancer role): a skilled unit refreshes an adjacent ally's turn. Host-tracked: the core holds no acted flags, so hosts own exhaustion state — this class is the shared legality check (Rescue/TalkHelper shape) plus the `dance;dancer;target` verb the headless runner validates. On success hosts clear their acted flag for the target and mark the dancer acted. Dancer status travels as skill id "dance" (markdown `skill:dance`; matched case-insensitively, no SkillCatalog registration needed since dance never touches a forecast).

### Dance.DanceSkillId

Skill id granting the dancer role.

```csharp
public const string DanceSkillId = ;
```

Type: `string`

### Dance.IsDancer

Reports whether a unit carries the dance skill (case-insensitive).

```csharp
public static bool IsDancer(Data.Unit dancer)
```

| Parameter | Type | Description |
| --- | --- | --- |
| dancer | `Data.Unit` | Unit to test (null reads as no dance). |

Returns: `bool` — True when the unit lists the dance skill.

### Dance.CanDance

Reports whether a dancer may refresh an adjacent ally this turn.

```csharp
public static bool CanDance(PlacedUnit dancer, PlacedUnit target)
```

| Parameter | Type | Description |
| --- | --- | --- |
| dancer | `PlacedUnit` | Dancing unit. |
| target | `PlacedUnit` | Ally to refresh. |

Returns: `bool` — True when skill, liveness, side, and adjacency all allow it.

### Dance.CheckDance

Throwing validator for the dance verb (fail fast, like Rescue.Grab).

```csharp
public static void CheckDance(PlacedUnit dancer, PlacedUnit target)
```

| Parameter | Type | Description |
| --- | --- | --- |
| dancer | `PlacedUnit` | Dancing unit. |
| target | `PlacedUnit` | Ally to refresh. |

## DeploymentSelector

Pre-battle roster picker: resolves requested unit nids against a chapter's deployments and reports authoring/selection errors without throwing, so the (future) deployment screen can surface them. Pure apart from no mutation at all — chosen entries alias the chapter's deployments.

### DeploymentSelector.ChooseDeployments

Resolves requested nids against chapter deployments without throwing.

```csharp
public static (List<Deployment> chosen, List<string> errors) ChooseDeployments(Chapter chapter, IEnumerable<string> nids)
```

| Parameter | Type | Description |
| --- | --- | --- |
| chapter | `Chapter` | Chapter whose deployments are picked from. |
| nids | `IEnumerable<string>` | Requested unit nids in preference order. |

Returns: `(List<Deployment> chosen, List<string> errors)` — (chosen deployments in request order, error strings).

## AiAction

Chosen move-then-strike action for one AI unit (or wait).

### AiAction.MoveTo

Destination tile (null means act or wait from the current tile).

```csharp
public GridPoint? MoveTo { get; set; }
```

Type: `GridPoint?`

### AiAction.Weapon

Chosen weapon entry (null when waiting or moving only).

```csharp
public InventoryEntry? Weapon { get; set; }
```

Type: `InventoryEntry?`

### AiAction.Target

Chosen foe (null means wait).

```csharp
public PlacedUnit? Target { get; set; }
```

Type: `PlacedUnit?`

### AiAction.Facing

Faced tile after acting (null means keep current facing).

```csharp
public GridPoint? Facing { get; set; }
```

Type: `GridPoint?`

### AiAction.IsWait

True when no foe is targeted.

```csharp
public bool IsWait => Target == null;
```

Type: `bool`

### AiAction.Wait

Builds the wait action (no move, no weapon, no target).

```csharp
public static AiAction Wait()
```

Returns: `AiAction` — An action whose target is null.

## EnemyAi

Greedy enemy brain with lethality-first scoring: (1) strike from current tile, (2) move then strike, (3) approach nearest foe. Enumerates every usable weapon across every reachable tile and foe. Terrain-aware when a terrain lookup is supplied; otherwise all bonuses read 0. Counter-risk is subtracted as expected counter damage times CounterWeight (0 = ignore, 1 = full). Pass a SupportLedger to weight bonded hit/avoid bonuses into both the strike and the counter-risk forecasts. Deterministic: ties break by least movement spent then nearest target then move tile foe nid and weapon key.

### EnemyAi.TerrainOf

Avoid and defense bonuses for a unit standing on a tile.

```csharp
public delegate (int avoid, int defense) TerrainOf(PlacedUnit unit, GridPoint tile)
```

| Parameter | Type | Description |
| --- | --- | --- |
| unit | `PlacedUnit` | Placed unit the bonuses apply to. |
| tile | `GridPoint` | Tile the bonuses are read from. |

Returns: `(int avoid, int defense)` — Avoid and defense bonuses at that tile.

### EnemyAi.DefenderWeaponOf

Picks the weapon a defender answers with at a distance.

```csharp
public delegate Weapon? DefenderWeaponOf(PlacedUnit foe, int distance)
```

| Parameter | Type | Description |
| --- | --- | --- |
| foe | `PlacedUnit` | Defending placement. |
| distance | `int` | Manhattan distance of the exchange. |

Returns: `Weapon?` — The defending weapon or null when the foe cannot counter.

### EnemyAi.Decide

Chooses the greedy move-then-strike action for one enemy unit.

```csharp
public static AiAction Decide(Battlefield field, PlacedUnit self, string movementGroup, int movePoints, CombatRules? rules = null)
```

| Parameter | Type | Description |
| --- | --- | --- |
| field | `Battlefield` | Board plus placements (reads blocking and ally tiles). |
| self | `PlacedUnit` | Acting enemy placement (position and inventory are read). |
| movementGroup | `string` | Movement group key for reachable tiles. |
| movePoints | `int` | Movement budget for this turn. |
| rules | `CombatRules?` | Combat tunables (defaults when null). |

Returns: `AiAction` — The chosen move target, weapon, and foe (or wait).

```csharp
public static AiAction Decide(Battlefield field, PlacedUnit self, string movementGroup, int movePoints, TerrainOf? terrainOf, DefenderWeaponOf? defenderWeaponOf, double counterWeight = 0.5, CombatRules? rules = null, SupportLedger? supports = null, HashSet<GridPoint>? visibleTiles = null, AiRules? aiRules = null)
```

| Parameter | Type | Description |
| --- | --- | --- |
| field | `Battlefield` | Board plus placements (reads blocking and ally tiles). |
| self | `PlacedUnit` | Acting enemy placement (position and inventory are read). |
| movementGroup | `string` | Movement group key for reachable tiles. |
| movePoints | `int` | Movement budget for this turn. |
| terrainOf | `TerrainOf?` | Optional avoid and defense lookup per tile. |
| defenderWeaponOf | `DefenderWeaponOf?` | Optional foe weapon picker by distance. |
| counterWeight | `double` | Expected counter-damage weight from 0 to 1. |
| rules | `CombatRules?` | Combat tunables (defaults when null). |
| supports | `SupportLedger?` | Optional bond bonuses for strike and counter forecasts. |
| visibleTiles | `HashSet<GridPoint>?` | Optional fog set with foes outside skipped. |
| aiRules | `AiRules?` | Scoring weights, or null for defaults. |

Returns: `AiAction` — The chosen move target, weapon, and foe (or wait).

## Evaluators

Port of TBSF evaluators (DamageDealtPosition/Target, Distance, Health) onto SrpgCore forecasts. Used by behaviour-tree nodes.

### Evaluators.DamageDealt

Expected damage dealt from tile to target (SrpgCore Forecast Damage times hit percent).

```csharp
public static double DamageDealt(Unit atk, Weapon w, Unit def, Weapon defW, GridPoint aPos, GridPoint dPos, Board board, CombatRules r)
```

| Parameter | Type | Description |
| --- | --- | --- |
| atk | `Unit` | Attacking unit. |
| w | `Weapon` | Attacker weapon. |
| def | `Unit` | Defending unit. |
| defW | `Weapon` | Defender weapon (null when disarmed). |
| aPos | `GridPoint` | Attacker tile. |
| dPos | `GridPoint` | Defender tile. |
| board | `Board` | Board for terrain lookups. |
| r | `CombatRules` | Combat rules (null selects defaults). |

Returns: `double` — Expected damage including crit bonus.

### Evaluators.Distance

Manhattan distance evaluator.

```csharp
public static int Distance(GridPoint a, GridPoint b)
```

| Parameter | Type | Description |
| --- | --- | --- |
| a | `GridPoint` | First tile. |
| b | `GridPoint` | Second tile. |

Returns: `int` — Manhattan distance between tiles.

### Evaluators.HealthRatio

Health ratio 0-1.

```csharp
public static double HealthRatio(Unit u)
```

| Parameter | Type | Description |
| --- | --- | --- |
| u | `Unit` | Unit to measure. |

Returns: `double` — Current HP divided by MaxHP (0 when MaxHP is 0).

## GrowthRates

Per-stat level-up growth chances (0-100).

### GrowthRates.HP

MaxHP growth chance (0-100) rolled on the growth RNG stream.

```csharp
public int HP { get; set; }
```

Type: `int`

### GrowthRates.Power

Power growth chance (0-100) rolled on the growth RNG stream.

```csharp
public int Power { get; set; }
```

Type: `int`

### GrowthRates.Skill

Skill growth chance (0-100) rolled on the growth RNG stream.

```csharp
public int Skill { get; set; }
```

Type: `int`

### GrowthRates.Speed

Speed growth chance (0-100) rolled on the growth RNG stream.

```csharp
public int Speed { get; set; }
```

Type: `int`

### GrowthRates.Luck

Luck growth chance (0-100) rolled on the growth RNG stream.

```csharp
public int Luck { get; set; }
```

Type: `int`

### GrowthRates.Defense

Defense growth chance (0-100) rolled on the growth RNG stream.

```csharp
public int Defense { get; set; }
```

Type: `int`

### GrowthRates.Resistance

Resistance growth chance (0-100) rolled on the growth RNG stream.

```csharp
public int Resistance { get; set; }
```

Type: `int`

### GrowthRates.Clone

Copies these growth chances.

```csharp
public GrowthRates Clone()
```

Returns: `GrowthRates` — An independent copy with the same per-stat chances.

## ExperienceRules

Tunable experience constants.

### ExperienceRules.BaseChipExp

Experience paid for a non-killing chip hit before bonuses.

```csharp
public int BaseChipExp { get; set; }
```

Type: `int`

### ExperienceRules.BaseKillExp

Experience paid for a killing blow before bonuses.

```csharp
public int BaseKillExp { get; set; }
```

Type: `int`

### ExperienceRules.LevelDiffBonus

Extra experience per level the defender outranks the attacker.

```csharp
public int LevelDiffBonus { get; set; }
```

Type: `int`

### ExperienceRules.MaxLevelDiff

Cap on the counted attacker-defender level difference.

```csharp
public int MaxLevelDiff { get; set; }
```

Type: `int`

### ExperienceRules.MinExp

Floor clamping every experience award.

```csharp
public int MinExp { get; set; }
```

Type: `int`

### ExperienceRules.MaxExp

Ceiling clamping every experience award.

```csharp
public int MaxExp { get; set; }
```

Type: `int`

### ExperienceRules.ExpPerLevel

Accumulated experience needed for one level-up.

```csharp
public int ExpPerLevel { get; set; }
```

Type: `int`

## Experience

GBA-flavored experience: chip hits pay a base, kills pay more, higher-level victims pay a capped level-difference bonus. Pure functions; LevelUp consumes the growth RNG stream (1RN per stat) and heals MaxHP gains into CurrentHP.

### Experience.Gain

Computes the experience award for one attack outcome.

```csharp
public static int Gain(bool didKill, int attackerLevel, int defenderLevel, int damageDealt, ExperienceRules? rules = null)
```

| Parameter | Type | Description |
| --- | --- | --- |
| didKill | `bool` | True when the attack killed the defender. |
| attackerLevel | `int` | Current level of the attacking unit. |
| defenderLevel | `int` | Current level of the defending unit. |
| damageDealt | `int` | Damage dealt, adding a small chip bonus when non-lethal. |
| rules | `ExperienceRules?` | Tunable constants (defaults to GBA-flavored values). |

Returns: `int` — Clamped experience award between MinExp and MaxExp.

### Experience.AddExp

Adds experience and applies level-ups while the threshold is met.

```csharp
public static void AddExp(Unit unit, int exp, GrowthRates growths, SeededRng growthRng, ExperienceRules? rules = null)
```

| Parameter | Type | Description |
| --- | --- | --- |
| unit | `Unit` | Unit gaining experience (mutates level, stats, and HP). |
| exp | `int` | Experience points to add. |
| growths | `GrowthRates` | Per-stat growth chances consumed on each level-up. |
| growthRng | `SeededRng` | Growth RNG stream (1RN per stat per level). |
| rules | `ExperienceRules?` | Tunable constants (defaults to GBA-flavored values). |

### Experience.LevelUp

Raises one level, rolling each growth chance and healing MaxHP gains.

```csharp
public static void LevelUp(Unit unit, GrowthRates growths, SeededRng growthRng)
```

| Parameter | Type | Description |
| --- | --- | --- |
| unit | `Unit` | Unit leveling up (mutates level, stats, and HP). |
| growths | `GrowthRates` | Per-stat growth chances for this level. |
| growthRng | `SeededRng` | Growth RNG stream (1RN per stat). |

## FogCalculator

Fog of war: a tile is visible to a side when within any live unit's Vision (Manhattan radius). No line-of-sight blocking in this model — walls still cost movement but do not stop sight (documented simplification). Pure set math.

### FogCalculator.VisibleTiles

Unions Manhattan vision balls of every live unit of a side.

```csharp
public static HashSet<GridPoint> VisibleTiles(Battlefield field, Affiliation side)
```

| Parameter | Type | Description |
| --- | --- | --- |
| field | `Battlefield` | Battlefield holding board bounds and placements. |
| side | `Affiliation` | Side whose sight to compute. |

Returns: `HashSet<GridPoint>` — In-bounds tiles visible to that side.

### FogCalculator.IsVisible

Tests whether one tile is visible to a side.

```csharp
public static bool IsVisible(Battlefield field, Affiliation viewerSide, GridPoint tile)
```

| Parameter | Type | Description |
| --- | --- | --- |
| field | `Battlefield` | Battlefield holding board bounds and placements. |
| viewerSide | `Affiliation` | Side whose sight to test. |
| tile | `GridPoint` | Tile to test for visibility. |

Returns: `bool` — True when the tile lies in the side's visible set.

### FogCalculator.HiddenEnemiesOf

Lists live foes of a viewer standing outside its side's sight.

```csharp
public static List<PlacedUnit> HiddenEnemiesOf(Battlefield field, PlacedUnit viewer)
```

| Parameter | Type | Description |
| --- | --- | --- |
| field | `Battlefield` | Battlefield holding board bounds and placements. |
| viewer | `PlacedUnit` | Placed unit whose side defines visibility and whose foes are listed. |

Returns: `List<PlacedUnit>` — Live enemies hidden from the viewer's side.

## Objectives

Standard chapter-end evaluators: rout, survive-N, protect-tags, seize, escape. Pure predicates over unit state; the TurnManager/event layer decides when to poll (after every elimination checkpoint; survive only on turn advance).

### Objectives.IsRouted

Rout completes when no live unit of a side remains.

```csharp
public static bool IsRouted(IEnumerable<Unit> units, Affiliation side)
```

| Parameter | Type | Description |
| --- | --- | --- |
| units | `IEnumerable<Unit>` | Units to scan for survivors. |
| side | `Affiliation` | Side tested for elimination. |

Returns: `bool` — True when that side has no live unit left.

### Objectives.HasSurvived

"Survive N" completes N full rounds: victory once TurnNumber passes N.

```csharp
public static bool HasSurvived(int turnNumber, int turnsToSurvive)
```

| Parameter | Type | Description |
| --- | --- | --- |
| turnNumber | `int` | Current turn number (starts at 1). |
| turnsToSurvive | `int` | Full rounds that must complete. |

Returns: `bool` — True once the required rounds have elapsed.

### Objectives.IsProtectFailed

Protect fails when a listed tag (unit nid) is dead AND already spawned. Future spawns (spawnTurn > turnNumber) never trip it. Use <= (not ==) for spawn comparison: skipped turn numbers must not drop the check.

```csharp
public static bool IsProtectFailed(IEnumerable<Unit> units, IEnumerable<string> tags, int turnNumber, Func<string, int> spawnTurnOf)
```

| Parameter | Type | Description |
| --- | --- | --- |
| units | `IEnumerable<Unit>` | Units keyed by nid for liveness lookup. |
| tags | `IEnumerable<string>` | Protected unit nids to check. |
| turnNumber | `int` | Current turn number for the spawned comparison. |
| spawnTurnOf | `Func<string, int>` | Maps a tag to its spawn turn. |

Returns: `bool` — True when a spawned protected unit is dead or missing.

### Objectives.IsSeized

Seize succeeds when the listed unit is alive and standing on the region. Pure over placements; the caller resolves the region by name.

```csharp
public static bool IsSeized(IEnumerable<PlacedUnit> placed, string unitNid, Regions.Region region)
```

| Parameter | Type | Description |
| --- | --- | --- |
| placed | `IEnumerable<PlacedUnit>` | Placements scanned for the seizing unit. |
| unitNid | `string` | Nid of the unit that must seize the region. |
| region | `Regions.Region` | Region the unit must stand on. |

Returns: `bool` — True when that live unit stands on the region.

### Objectives.IsEscape

Escape succeeds when the listed unit is alive and standing on the escape region. Mirrors seize semantics; LT Maker escape is reaching a designated region alive.

```csharp
public static bool IsEscape(IEnumerable<PlacedUnit> placed, string unitNid, Regions.Region region)
```

| Parameter | Type | Description |
| --- | --- | --- |
| placed | `IEnumerable<PlacedUnit>` | Placements scanned for the escaping unit. |
| unitNid | `string` | Nid of the unit that must escape through the region. |
| region | `Regions.Region` | Escape region the unit must stand on. |

Returns: `bool` — True when that live unit stands on the escape region.

## PhaseTickRules

Tunable phase-boundary attrition. Values: PoisonDamage per poisoned unit, RegenHeal per regenerating unit, TickStatuses toggle, ApplyTerrain toggle.

### PhaseTickRules.PoisonDamage

HP lost per poisoned unit per tick (GBA-flavored default).

```csharp
public int PoisonDamage { get; set; }
```

Type: `int`

### PhaseTickRules.RegenHeal

HP gained per regenerating unit per tick.

```csharp
public int RegenHeal { get; set; }
```

Type: `int`

### PhaseTickRules.TickStatuses

Decrement status durations and report expirations.

```csharp
public bool TickStatuses { get; set; }
```

Type: `bool`

### PhaseTickRules.ApplyTerrain

Apply per-tile TerrainEffects (fort heal, marsh damage).

```csharp
public bool ApplyTerrain { get; set; }
```

Type: `bool`

## PhaseTick

Phase-boundary upkeep for one team: terrain HP swing, then poison damage, then a status tick per unit, in deterministic coordinate order (X, then Y — the same order EnemyAi enumerates tiles). Hosts call Apply when a team's phase begins; per-unit report is (nid, total hpDelta, expired status names). Ticks never kill: a unit alive at tick start is left at 1 HP minimum; combat and objectives remain the only deaths. Skips dead units. Turn pipeline order: PhaseTick.Apply then SpawnDue (less or equal) then EvaluateObjectives then AI or input. Traps and events run as an explicit post-action stage after the commit.

### PhaseTick.Apply

Runs phase-boundary upkeep for one team: terrain swing, poison, status ticks.

```csharp
public static List<(string nid, int hpDelta, string[] expiredStatuses)> Apply(ChapterState state, string team, PhaseTickRules? rules = null)
```

| Parameter | Type | Description |
| --- | --- | --- |
| state | `ChapterState` | Live chapter runtime (mutates HP and status durations). |
| team | `string` | Team name (player, enemy, ally, other). |
| rules | `PhaseTickRules?` | Toggles for terrain, poison damage, and status ticks. |

Returns: `List<(string nid, int hpDelta, string[] expiredStatuses)>` — Per-unit (nid, total hpDelta, expired status names) in tick order.

## ReplayHash

Deterministic hash of chapter runtime for replay verification.

### ReplayHash.Compute

Computes a deterministic hash of placements, HP, vars, RNG states, and skill catalog.

```csharp
public static string Compute(ChapterState state)
```

| Parameter | Type | Description |
| --- | --- | --- |
| state | `ChapterState` | Chapter runtime to hash. |

Returns: `string` — Hash string.

## Rescue

Rescue (GBA simplify): a carrier grabs an adjacent non-hostile passenger whose Con fits (passenger Con <= carrier Con + 2), carries one at a time, moves at half MovePoints while carrying, and the passenger leaves the map (returns on drop). Rescued units are untargetable because they are not placed; hosts hide their tokens. Cross-side carries are refused: enemies block pass-through, so they can never be scooped either.

### Rescue.CanGrab

Tests whether a carrier can grab an adjacent passenger.

```csharp
public static bool CanGrab(Unit carrier, Unit passenger, GridPoint carrierPos, GridPoint passengerPos)
```

| Parameter | Type | Description |
| --- | --- | --- |
| carrier | `Unit` | Unit attempting the grab (must have hands free). |
| passenger | `Unit` | Unit to be carried (Con must fit). |
| carrierPos | `GridPoint` | Current tile of the carrier. |
| passengerPos | `GridPoint` | Current tile of the passenger. |

Returns: `bool` — True when adjacent, alive, same side, hands-free, and within Con capacity.

### Rescue.EffectiveMove

Move allowance for a carrier, halved while carrying a passenger.

```csharp
public static int EffectiveMove(Unit carrier)
```

| Parameter | Type | Description |
| --- | --- | --- |
| carrier | `Unit` | Carrier whose allowance is computed (null reads as 0). |

Returns: `int` — Full move when unladen, half move when laden.

### Rescue.Grab

Grab: records the carry and removes the passenger token. Returns the removed token.

```csharp
public static PlacedUnit Grab(Battlefield field, PlacedUnit carrier, PlacedUnit passenger)
```

| Parameter | Type | Description |
| --- | --- | --- |
| field | `Battlefield` | Battlefield holding both tokens. |
| carrier | `PlacedUnit` | Placed carrier doing the grabbing. |
| passenger | `PlacedUnit` | Placed passenger being picked up. |

Returns: `PlacedUnit` — The removed passenger token for host bookkeeping.

### Rescue.Drop

Drop the carried nid on an adjacent free tile. Needs the passenger Unit (hosts hold it).

```csharp
public static PlacedUnit Drop(Battlefield field, PlacedUnit carrier, Unit passenger, GridPoint tile)
```

| Parameter | Type | Description |
| --- | --- | --- |
| field | `Battlefield` | Battlefield receiving the dropped passenger. |
| carrier | `PlacedUnit` | Placed carrier doing the dropping. |
| passenger | `Unit` | Passenger unit held by the host. |
| tile | `GridPoint` | Adjacent free tile receiving the passenger. |

Returns: `PlacedUnit` — The newly deployed passenger token.

## UnitSnapshot

Plain DTOs for saves/turnwheel/suspend (shape: Nid/Name/Side/ClassKey/Level/Experience/Stats/CurrentHP/MovePoints/MovementGroup/Vision/Skills/Statuses/Wexp/CarryingNid and Inventory keys with uses left). No JSON dependency: hosts serialize these with whatever they have.

### UnitSnapshot.Nid

Unit nid key.

```csharp
public string Nid = ;
```

Type: `string`

### UnitSnapshot.Name

Display name.

```csharp
public string Name = ;
```

Type: `string`

### UnitSnapshot.Side

Side affiliation.

```csharp
public Affiliation Side;
```

Type: `Affiliation`

### UnitSnapshot.ClassKey

Class key.

```csharp
public string ClassKey = ;
```

Type: `string`

### UnitSnapshot.Level

Current level.

```csharp
public int Level;
```

Type: `int`

### UnitSnapshot.Experience

Accumulated experience toward the next level.

```csharp
public int Experience;
```

Type: `int`

### UnitSnapshot.Stats

Cloned stat block.

```csharp
public Stats Stats = new Stats();
```

Type: `Stats`

### UnitSnapshot.CurrentHP

Current hit points.

```csharp
public int CurrentHP;
```

Type: `int`

### UnitSnapshot.MovePoints

Base movement allowance.

```csharp
public int MovePoints;
```

Type: `int`

### UnitSnapshot.MovementGroup

Movement group key for terrain costs.

```csharp
public string MovementGroup = ;
```

Type: `string`

### UnitSnapshot.Vision

Sight radius in Manhattan tiles.

```csharp
public int Vision = 5;
```

Type: `int`

### UnitSnapshot.Skills

Skill keys.

```csharp
public List<string> Skills = new List<string>();
```

Type: `List<string>`

### UnitSnapshot.Statuses

Active statuses with remaining durations.

```csharp
public Dictionary<string, int> Statuses = new Dictionary<string, int>();
```

Type: `Dictionary<string, int>`

### UnitSnapshot.Wexp

Weapon experience per weapon type.

```csharp
public Dictionary<WeaponType, int> Wexp = new Dictionary<WeaponType, int>();
```

Type: `Dictionary<WeaponType, int>`

### UnitSnapshot.CarryingNid

Nid of the carried passenger (empty when hands-free).

```csharp
public string CarryingNid = ;
```

Type: `string`

### UnitSnapshot.AiType

AI type (rush/hold) persisted across saves.

```csharp
public string AiType = ;
```

Type: `string`

### UnitSnapshot.PatrolA

Patrol waypoint A for patrol AI.

```csharp
public GridPoint PatrolA;
```

Type: `GridPoint`

### UnitSnapshot.PatrolB

Patrol waypoint B for patrol AI.

```csharp
public GridPoint PatrolB;
```

Type: `GridPoint`

### UnitSnapshot.Inventory

Inventory as armory weapon keys with uses left.

```csharp
public List<InventorySnapshot> Inventory = new List<InventorySnapshot>();
```

Type: `List<InventorySnapshot>`

## InventorySnapshot

One inventory entry for JSON-safe saves.

### InventorySnapshot.WeaponKey

Armory weapon key.

```csharp
public string WeaponKey = ;
```

Type: `string`

### InventorySnapshot.UsesLeft

Uses remaining.

```csharp
public int UsesLeft;
```

Type: `int`

## SupportPointSnapshot

One support pair for JSON-safe saves.

### SupportPointSnapshot.A

First nid.

```csharp
public string A = ;
```

Type: `string`

### SupportPointSnapshot.B

Second nid.

```csharp
public string B = ;
```

Type: `string`

### SupportPointSnapshot.Points

Accumulated points.

```csharp
public int Points;
```

Type: `int`

## PlacedSnapshot

Unit snapshot plus its board tile (Unit + X/Y).

### PlacedSnapshot.Unit

Snapshot of the placed unit.

```csharp
public UnitSnapshot Unit = new UnitSnapshot();
```

Type: `UnitSnapshot`

### PlacedSnapshot.X

Board X coordinate.

```csharp
public int X;
```

Type: `int`

### PlacedSnapshot.Y

Board Y coordinate.

```csharp
public int Y;
```

Type: `int`

## SkillSnapshot

One skill definition for save snapshot.

### SkillSnapshot.Id

Skill id.

```csharp
public string Id = ;
```

Type: `string`

### SkillSnapshot.Hook

Hook side.

```csharp
public int Hook;
```

Type: `int`

### SkillSnapshot.DamageBonus

Damage bonus.

```csharp
public int DamageBonus;
```

Type: `int`

### SkillSnapshot.HitBonus

Hit bonus.

```csharp
public int HitBonus;
```

Type: `int`

### SkillSnapshot.CritBonus

Crit bonus.

```csharp
public int CritBonus;
```

Type: `int`

### SkillSnapshot.DamageTakenPenalty

Damage taken penalty.

```csharp
public int DamageTakenPenalty;
```

Type: `int`

### SkillSnapshot.ProcChance

Proc chance 0-100.

```csharp
public int ProcChance;
```

Type: `int`

## ChapterSnapshot

Full chapter save: TurnNumber/CurrentTeamIndex/BattleResult turn state, CombatRngState/GrowthRngState/ProcRngState, Vars flags, Placed placements with positions, and SpawnedNids to suppress re-spawns on restore.

### ChapterSnapshot.TurnNumber

Turn number at save time.

```csharp
public int TurnNumber = 1;
```

Type: `int`

### ChapterSnapshot.CurrentTeamIndex

Index of the acting team at save time.

```csharp
public int CurrentTeamIndex;
```

Type: `int`

### ChapterSnapshot.BattleResult

Scripted battle result (null while ongoing).

```csharp
public bool? BattleResult;
```

Type: `bool?`

### ChapterSnapshot.CombatRngState

Saved combat RNG stream state.

```csharp
public uint CombatRngState;
```

Type: `uint`

### ChapterSnapshot.GrowthRngState

Saved growth RNG stream state.

```csharp
public uint GrowthRngState;
```

Type: `uint`

### ChapterSnapshot.ProcRngState

Saved proc RNG stream state.

```csharp
public uint ProcRngState;
```

Type: `uint`

### ChapterSnapshot.Vars

Chapter vars dictionary copy.

```csharp
public Dictionary<string, object?> Vars = new Dictionary<string, object?>();
```

Type: `Dictionary<string, object?>`

### ChapterSnapshot.Placed

Placed unit snapshots.

```csharp
public List<PlacedSnapshot> Placed = new List<PlacedSnapshot>();
```

Type: `List<PlacedSnapshot>`

### ChapterSnapshot.SpawnedNids

Already-spawned nids so restore never duplicates reinforcements.

```csharp
public List<string> SpawnedNids = new List<string>();
```

Type: `List<string>`

### ChapterSnapshot.Convoy

Convoy items as weapon keys with uses left.

```csharp
public List<InventorySnapshot> Convoy = new List<InventorySnapshot>();
```

Type: `List<InventorySnapshot>`

### ChapterSnapshot.PartyGold

Party gold for the shop (buy deducts, sell pays half).

```csharp
public int PartyGold;
```

Type: `int`

### ChapterSnapshot.SupportPoints

Support points per bonded pair.

```csharp
public List<SupportPointSnapshot> SupportPoints = new List<SupportPointSnapshot>();
```

Type: `List<SupportPointSnapshot>`

### ChapterSnapshot.SkillDefs

Skill catalog snapshot.

```csharp
public List<SkillSnapshot> SkillDefs = new List<SkillSnapshot>();
```

Type: `List<SkillSnapshot>`

### ChapterSnapshot.Carried

Rescued passengers held off-map.

```csharp
public List<UnitSnapshot> Carried = new List<UnitSnapshot>();
```

Type: `List<UnitSnapshot>`

### ChapterSnapshot.BoardWidth

Board width in tiles for the terrain grid.

```csharp
public int BoardWidth;
```

Type: `int`

### ChapterSnapshot.BoardHeight

Board height in tiles for the terrain grid.

```csharp
public int BoardHeight;
```

Type: `int`

### ChapterSnapshot.TerrainKeys

Terrain keys in row major order.

```csharp
public List<string> TerrainKeys = new List<string>();
```

Type: `List<string>`

## SaveSystem

Save/restore over a ChapterState. Units serialize fully (no template lookup on load); weapons resolve through chapter.Armory by key, so armory keys are the save contract. Deterministic: RNG stream states round-trip, so post-load rolls continue the sequence.

### SaveSystem.Save

Snapshots positions, stats, inventory uses, turn state, vars, and RNG states.

```csharp
public static ChapterSnapshot Save(ChapterState state)
```

| Parameter | Type | Description |
| --- | --- | --- |
| state | `ChapterState` | Live chapter runtime to snapshot. |

Returns: `ChapterSnapshot` — Plain DTO snapshot the host can serialize.

### SaveSystem.Restore

Rebuilds an equivalent chapter state from a snapshot; armory keys are the save contract.

```csharp
public static ChapterState Restore(Chapter chapter, ChapterSnapshot snap, uint baseSeed, IReadOnlyList<string>? teams = null)
```

| Parameter | Type | Description |
| --- | --- | --- |
| chapter | `Chapter` | Chapter providing board, armory, and event definitions. |
| snap | `ChapterSnapshot` | Snapshot to restore. |
| baseSeed | `uint` | Base seed for the fresh state before RNG states are applied. |
| teams | `IReadOnlyList<string>?` | Phase order override (defaults to the chapter order). |

Returns: `ChapterState` — Live chapter state equivalent to the save point.

## Shove

Adjacent shove/swap: pushes or exchanges adjacent units when Con/weight allows.

### Shove.CanShove

True when shover can push target one tile away (adjacent, target destination free, Con sufficient).

```csharp
public static bool CanShove(Unit shover, Unit target, GridPoint shoverPos, GridPoint targetPos, Battlefield field)
```

| Parameter | Type | Description |
| --- | --- | --- |
| shover | `Unit` | Unit doing the shoving. |
| target | `Unit` | Unit being shoved. |
| shoverPos | `GridPoint` | Tile of the shover. |
| targetPos | `GridPoint` | Tile of the target. |
| field | `Battlefield` | Battlefield to test destination vacancy and bounds. |

Returns: `bool` — True when the push is legal.

### Shove.ExecuteShove

Executes a shove: moves target one tile away from shover.

```csharp
public static void ExecuteShove(Battlefield field, PlacedUnit shover, PlacedUnit target)
```

| Parameter | Type | Description |
| --- | --- | --- |
| field | `Battlefield` | Battlefield containing both units. |
| shover | `PlacedUnit` | Placed shover. |
| target | `PlacedUnit` | Placed target. |

### Shove.CanSwap

True when two adjacent units can swap positions.

```csharp
public static bool CanSwap(Unit a, Unit b, GridPoint aPos, GridPoint bPos)
```

| Parameter | Type | Description |
| --- | --- | --- |
| a | `Unit` | First unit. |
| b | `Unit` | Second unit. |
| aPos | `GridPoint` | Tile of first. |
| bPos | `GridPoint` | Tile of second. |

Returns: `bool` — True when adjacent and both alive.

### Shove.ExecuteSwap

Swaps positions of two adjacent placed units.

```csharp
public static void ExecuteSwap(PlacedUnit a, PlacedUnit b)
```

| Parameter | Type | Description |
| --- | --- | --- |
| a | `PlacedUnit` | First placed unit. |
| b | `PlacedUnit` | Second placed unit. |

## Steal

GBA-style steal: adjacent thief with higher speed takes a light stealable item.

### Steal.CanSteal

True when thief can steal weapon at slot from target.

```csharp
public static bool CanSteal(Unit thief, Unit target, GridPoint thiefPos, GridPoint targetPos, int weaponIndex)
```

| Parameter | Type | Description |
| --- | --- | --- |
| thief | `Unit` | Unit doing the stealing. |
| target | `Unit` | Unit being stolen from. |
| thiefPos | `GridPoint` | Thief tile. |
| targetPos | `GridPoint` | Target tile. |
| weaponIndex | `int` | Inventory index on target. |

Returns: `bool` — True when adjacent, alive, faster, and item light enough.

### Steal.Execute

Moves inventory entry from target to thief.

```csharp
public static void Execute(Unit thief, Unit target, int weaponIndex)
```

| Parameter | Type | Description |
| --- | --- | --- |
| thief | `Unit` | Thief unit receiving the item. |
| target | `Unit` | Target unit losing the item. |
| weaponIndex | `int` | Slot on target. |

```csharp
public static void Execute(Battlefield field, PlacedUnit thief, PlacedUnit target, string weaponKey)
```

| Parameter | Type | Description |
| --- | --- | --- |
| field | `Battlefield` | Battlefield for position lookup. |
| thief | `PlacedUnit` | Placed thief. |
| target | `PlacedUnit` | Placed target. |
| weaponKey | `string` | Weapon key to steal (first match). |

## SupportRank

Support rank between a pair of units (GBA C/B/A shape).

| Name | Value | Description |
| --- | --- | --- |
| None | 0 | No support bond. |
| C | 1 | C rank (30 points, plus 5 hit and avoid). |
| B | 2 | B rank (80 points, plus 10 hit and avoid). |
| A | 3 | A rank (150 points, plus 15 hit and avoid). |

## SupportLedger

Adjacency support ledger: bonds keyed by ordered nid pair, points accumulate to ranks. Bonus: each bonded ally within 3 Manhattan tiles grants +5 hit/avoid per rank step (C +5, B +10, A +15), GBA-flavored. Pure over placements + ledger. Fork stacks additively across multiple bonded allies; GBA FE7/8 applies only the single highest support bonus in range.

### SupportLedger.PointsForRank

Points threshold required to reach a rank.

```csharp
public static int PointsForRank(SupportRank rank)
```

| Parameter | Type | Description |
| --- | --- | --- |
| rank | `SupportRank` | Rank whose threshold is requested. |

Returns: `int` — Points needed for that rank (0 for None).

### SupportLedger.RankForPoints

Derives the rank for an accumulated point total.

```csharp
public static SupportRank RankForPoints(int points)
```

| Parameter | Type | Description |
| --- | --- | --- |
| points | `int` | Accumulated bond points. |

Returns: `SupportRank` — Highest rank whose threshold is met.

### SupportLedger.AllPairs

All bonded pairs with their accumulated points (for saves).

```csharp
public IEnumerable<((string a, string b) key, int points)> AllPairs()
```

Returns: `IEnumerable<((string a, string b) key, int points)>` — Every canonical pair key with its point total.

### SupportLedger.SetPoints

Restores points for a pair (for save restore).

```csharp
public void SetPoints(string a, string b, int points)
```

| Parameter | Type | Description |
| --- | --- | --- |
| a | `string` | First nid. |
| b | `string` | Second nid. |
| points | `int` | Points to restore. |

### SupportLedger.AddPoints

Accumulates bond points for an ordered pair (stored canonically).

```csharp
public void AddPoints(string a, string b, int points)
```

| Parameter | Type | Description |
| --- | --- | --- |
| a | `string` | First unit nid. |
| b | `string` | Second unit nid. |
| points | `int` | Points to add (non-positive values are ignored). |

### SupportLedger.RankOf

Reads the current rank for a unit pair.

```csharp
public SupportRank RankOf(string a, string b)
```

| Parameter | Type | Description |
| --- | --- | --- |
| a | `string` | First unit nid. |
| b | `string` | Second unit nid. |

Returns: `SupportRank` — Rank for the pair (None when unbonded).

### SupportLedger.BonusForRank

Hit and avoid bonus granted by one bond of a rank.

```csharp
public static int BonusForRank(SupportRank rank)
```

| Parameter | Type | Description |
| --- | --- | --- |
| rank | `SupportRank` | Bond rank. |

Returns: `int` — Five per rank step (C 5, B 10, A 15).

### SupportLedger.TickAdjacent

Grants 1 support point per adjacent alive ally pair (GBA-flavored tick).

```csharp
public int TickAdjacent(Battlefield field)
```

| Parameter | Type | Description |
| --- | --- | --- |
| field | `Battlefield` | Battlefield to read adjacency from. |

Returns: `int` — Number of pairs that gained a point.

### SupportLedger.BonusFor

Sums hit and avoid from bonded allies within 3 Manhattan tiles (fork: additive stacking across allies; GBA uses single highest).

```csharp
public (int hit, int avoid) BonusFor(Unit unit, GridPoint pos, IEnumerable<PlacedUnit> allies)
```

| Parameter | Type | Description |
| --- | --- | --- |
| unit | `Unit` | Unit receiving the bonus. |
| pos | `GridPoint` | Tile the unit is evaluated from. |
| allies | `IEnumerable<PlacedUnit>` | Candidate bonded allies. |

Returns: `(int hit, int avoid)` — (hitBonus, avoidBonus) summed across all bonded allies in range.

## TalkHelper

Talk-command helpers: who can talk to whom, and the vars a Talk event reads. Any adjacent live unit qualifies regardless of side — ally recruits and enemy parleys alike — so hosts gate specific pairs with Talk-trigger conditions (e.g. `unit.nid == 'eirika' && unit2.nid == 'garcia'`). On acceptance hosts run `recruit;unit2` (or any scripted effect). Pure: no mutation, no RNG.

### TalkHelper.CanTalk

Pure adjacency+liveness predicate for one ordered pair (positions supplied by the caller so hosts can preview without moving tokens).

```csharp
public static bool CanTalk(Unit a, GridPoint posA, Unit b, GridPoint posB)
```

| Parameter | Type | Description |
| --- | --- | --- |
| a | `Unit` | First unit. |
| posA | `GridPoint` | Tile of the first unit. |
| b | `Unit` | Second unit. |
| posB | `GridPoint` | Tile of the second unit. |

Returns: `bool` — True when both are alive and orthogonally adjacent.

### TalkHelper.TalkCandidates

Every adjacent live placed unit (exclude self), in placement order.

```csharp
public static List<PlacedUnit> TalkCandidates(Battlefield field, PlacedUnit unit)
```

| Parameter | Type | Description |
| --- | --- | --- |
| field | `Battlefield` | Battlefield holding placements. |
| unit | `PlacedUnit` | Placed unit seeking conversation partners. |

Returns: `List<PlacedUnit>` — Adjacent live units in placement order.

### TalkHelper.TalkVars

Vars for EventTrigger.Talk: unit (talker), unit2 (listener), position (the talker's tile) — same shape as BattleTriggers combat vars.

```csharp
public static Dictionary<string, object?> TalkVars(Unit talker, Unit listener, GridPoint position)
```

| Parameter | Type | Description |
| --- | --- | --- |
| talker | `Unit` | Unit initiating the talk. |
| listener | `Unit` | Unit being talked to. |
| position | `GridPoint` | Talker's tile. |

Returns: `Dictionary<string, object?>` — Vars dictionary for the Talk trigger.

## ThreatCalculator

Danger-zone computation (Battlefield helper): ThreatRange per enemy and SideThreat union, optionally clipped by FogCalculator.VisibleTiles. Values are tile sets; callers supply movement and weapon range via LoadoutOf.

### ThreatCalculator.ThreatRange

Danger zone for one enemy: attack rings over every tile it can reach.

```csharp
public static HashSet<GridPoint> ThreatRange(Battlefield field, PlacedUnit enemy, string movementGroup, int movePoints, int minRange, int maxRange)
```

| Parameter | Type | Description |
| --- | --- | --- |
| field | `Battlefield` | Battlefield holding board and placements. |
| enemy | `PlacedUnit` | Placed enemy whose threat is computed. |
| movementGroup | `string` | Movement group key for reachability. |
| movePoints | `int` | Movement allowance. |
| minRange | `int` | Weapon minimum range. |
| maxRange | `int` | Weapon maximum range. |

Returns: `HashSet<GridPoint>` — Every tile the enemy could strike.

### ThreatCalculator.LoadoutOf

Resolves a unit's movement group, move allowance, and weapon ranges.

```csharp
public delegate (string group, int move, int minRange, int maxRange) LoadoutOf(PlacedUnit unit)
```

| Parameter | Type | Description |
| --- | --- | --- |
| unit | `PlacedUnit` | Placed unit whose loadout is requested. |

Returns: `(string group, int move, int minRange, int maxRange)` — Movement group, move allowance, minimum range, and maximum range.

### ThreatCalculator.SideThreat

Unions threat ranges of many enemies, optionally clipped to sight.

```csharp
public static HashSet<GridPoint> SideThreat(Battlefield field, IEnumerable<PlacedUnit> enemies, LoadoutOf loadout, HashSet<GridPoint>? visibleOnly = null)
```

| Parameter | Type | Description |
| --- | --- | --- |
| field | `Battlefield` | Battlefield holding board and placements. |
| enemies | `IEnumerable<PlacedUnit>` | Enemies whose threat is unioned. |
| loadout | `LoadoutOf` | Resolves each enemy's movement and range loadout. |
| visibleOnly | `HashSet<GridPoint>?` | Optional visible set; threat outside it is dropped. |

Returns: `HashSet<GridPoint>` — Combined danger zone.

### ThreatCalculator.SideThreatValues

Counts covering enemies per threatened tile.

```csharp
public static Dictionary<GridPoint, int> SideThreatValues(Battlefield field, IEnumerable<PlacedUnit> enemies, LoadoutOf loadout, HashSet<GridPoint>? visibleOnly = null)
```

| Parameter | Type | Description |
| --- | --- | --- |
| field | `Battlefield` | Battlefield holding board and placements. |
| enemies | `IEnumerable<PlacedUnit>` | Enemies whose threat is counted. |
| loadout | `LoadoutOf` | Resolves each enemy's movement and range loadout. |
| visibleOnly | `HashSet<GridPoint>?` | Optional visible set; threat outside it is dropped. |

Returns: `Dictionary<GridPoint, int>` — Threat count per tile.

## TurnManager

Phase cycler: teams act in list order, teams with no live units are skipped (player/index 0 never skipped), TurnNumber advances each time play returns to the first team. Win/loss is script-driven: events call SetResult (via win/lose commands) and the host polls BattleResult at event end.

### TurnManager.Teams

Teams in phase order.

```csharp
public IReadOnlyList<string> Teams { get; }
```

Type: `IReadOnlyList<string>`

### TurnManager.CurrentTeamIndex

Index of the currently acting team.

```csharp
public int CurrentTeamIndex { get; private set; }
```

Type: `int`

### TurnManager.CurrentTeam

Currently acting team name.

```csharp
public string CurrentTeam => Teams[CurrentTeamIndex];
```

Type: `string`

### TurnManager.TurnNumber

Round number (starts at 1, advances on return to the first team).

```csharp
public int TurnNumber { get; private set; }
```

Type: `int`

### TurnManager.BattleResult

null = ongoing, true = victory, false = defeat.

```csharp
public bool? BattleResult { get; private set; }
```

Type: `bool?`

### TurnManager constructor

Creates a phase cycler over the given team list.

```csharp
public TurnManager(IReadOnlyList<string> teams)
```

| Parameter | Type | Description |
| --- | --- | --- |
| teams | `IReadOnlyList<string>` | Team names in phase order (first entry is never skipped). |

### TurnManager.SetResult

Records the scripted win or loss flag.

```csharp
public void SetResult(bool won)
```

| Parameter | Type | Description |
| --- | --- | --- |
| won | `bool` | True for victory, false for defeat. |

### TurnManager.Restore

Restore saved progress (saves/turnwheel). Hosts only; Verify covers it.

```csharp
public void Restore(int teamIndex, int turnNumber, bool? battleResult)
```

| Parameter | Type | Description |
| --- | --- | --- |
| teamIndex | `int` | Saved acting-team index. |
| turnNumber | `int` | Saved turn number. |
| battleResult | `bool?` | Saved scripted result (null while ongoing). |

### TurnManager.AdvancePhase

Advances to the next live team, skipping empty teams except the first.

```csharp
public (string team, bool roundAdvanced) AdvancePhase(Func<string, bool> teamHasLiveUnits)
```

| Parameter | Type | Description |
| --- | --- | --- |
| teamHasLiveUnits | `Func<string, bool>` | Tests whether a team has live units. |

Returns: `(string team, bool roundAdvanced)` — New current team and whether a fresh round began (turn advanced).
