# Architecture

## Namespaces

| Namespace | Responsibility | Key types |
| --- | --- | --- |
| `Grid` | Board geometry and movement math | [`Board`](api-grid.md#board), [`GridPoint`](api-grid.md#gridpoint), [`TerrainType`](api-grid.md#terraintype), [`MovementGroups`](api-grid.md#movementgroups), [`RangeFinder`](api-grid.md#rangefinder) |
| `Data` | Definitions and live unit state | [`Stats`](api-data.md#stats), [`UnitClass`](api-data.md#unitclass), [`Weapon`](api-data.md#weapon), [`Unit`](api-data.md#unit), [`Chapter`](api-data.md#chapter), [`StandardTerrains`](api-data.md#standardterrains), [`TerrainEffects`](api-data.md#terraineffects), [`Statuses`](api-data.md#statuses), [`ClassDatabase`](api-data.md#classdatabase), [`Promotion`](api-data.md#promotion) |
| `Combat` | Battle math and resolution | [`CombatRules`](api-combat.md#combatrules), [`HitMode`](api-combat.md#hitmode), [`WeaponTriangle`](api-combat.md#weapontriangle), [`CombatResolver`](api-combat.md#combatresolver), [`SkillDef`](api-combat.md#skilldef), [`SkillCatalog`](api-combat.md#skillcatalog), [`SkillHook`](api-combat.md#skillhook) |
| `Events` | Trigger/condition/command scripting | [`EventTrigger`](api-events.md#eventtrigger), [`GameEvent`](api-events.md#gameevent), [`EventCommand`](api-events.md#eventcommand), [`CommandCatalog`](api-events.md#commandcatalog), [`EventRunner`](api-events.md#eventrunner), [`EventDispatcher`](api-events.md#eventdispatcher), [`SimpleConditionEvaluator`](api-events.md#simpleconditionevaluator) |
| `Regions` | Named tile sets | [`Region`](api-regions.md#region), [`RegionKind`](api-regions.md#regionkind) |
| `Random` | Seeded determinism | [`SeededRng`](api-random.md#seededrng) |
| `Tactics` | Chapter runtime: phases, objectives, AI | [`TurnManager`](api-tactics.md#turnmanager), [`Objectives`](api-tactics.md#objectives), [`Battlefield`](api-tactics.md#battlefield), [`ThreatCalculator`](api-tactics.md#threatcalculator), [`EnemyAi`](api-tactics.md#enemyai), [`ChapterState`](api-tactics.md#chapterstate), [`ChapterRunner`](api-tactics.md#chapterrunner), [`Experience`](api-tactics.md#experience), [`GrowthRates`](api-tactics.md#growthrates), [`SaveSystem`](api-tactics.md#savesystem), snapshots, [`FogCalculator`](api-tactics.md#fogcalculator), [`Rescue`](api-tactics.md#rescue), [`SupportLedger`](api-tactics.md#supportledger) |

Dependencies flow one way: `Tactics` → `Events`/`Combat`/`Data`/`Grid`/`Random`.
`Grid` and `Random` depend on nothing. Nothing references an engine.

Per-function reference with signatures, arguments, and returns (generated from
source, see below): [API: Grid](api-grid), [API: Data](api-data),
[API: Combat](api-combat), [API: Events](api-events), [API: Regions](api-regions),
[API: Random](api-random), [API: Tactics](api-tactics).

```csharp
// A host turn only ever calls downward: Tactics orchestrates, Combat/Data answer.
var range = field.Reachable(foe, foe.Unit.MovementGroup, foe.Unit.MovePoints); // Grid
CombatForecast fc = CombatResolver.Forecast(...);                              // Combat
CombatResult r = CombatResolver.Resolve(...);                                  // Combat
CombatGains.ApplyAfterCombat(...);                                             // Tactics over Data
PhaseTick.Apply(state, team);                                                  // Tactics over Data
EventDispatcher.Dispatch(trigger, vars, chapter.Events, runner);               // Events
```

## Chapter runtime flow

```
Chapter (data) ──StartNew(seed)──▶ ChapterState
                                        │ Battlefield (placements)
                                        │ TurnManager (phases, turn #)
                                        │ CombatRng / GrowthRng (streams)
                                        │ Vars (level flags)
                                        ▼
loop: AdvancePhase ─▶ SpawnDue ─▶ player/AI actions ─▶ events ─▶ EvaluateObjectives
                                                            Ongoing / Victory / Defeat
```

- **Phases** cycle team order, skipping teams with no live units; the turn counter
  advances each return to the first team.
- **Spawning** deploys everything whose spawn turn has been reached (`<=`, never `==`).
- **Actions** use [`RangeFinder`](api-grid.md#rangefinder) (move), [`CombatResolver`](api-combat.md#combatresolver) (fight), [`EnemyAi`](api-tactics.md#enemyai) (foes).
- **Events** fire on triggers with conditions over trigger variables; `win`/`lose`
  commands set the scripted result.
- **Objectives** are polled: scripted result first, then defeat specs, then victory specs.

## What the core does not do

Rendering, animation, input, audio, file I/O, and networking are host concerns. The
core exposes data ([`CombatForecast`](api-combat.md#combatforecast) for the forecast window, `HashSet<GridPoint>` for
range highlights and threat display) and interfaces ([`IEventExecutor`](api-events.md#ieventexecutor)) so any host —
Unity, Godot, Blazor, a web front-end, or the headless `ChapterRunner` used in tests —
can drive it. See [hosting](hosting.md) for worked examples.
