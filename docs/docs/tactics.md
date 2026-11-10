# Tactics

## Phases

[`TurnManager`](api-tactics.md#turnmanager) cycles a team list in order (default `player, enemy, ally, other`).
Teams with no live units are skipped — except the first team, which is never skipped.
`TurnNumber` starts at 1 and advances each time play returns to the first team, so
"survive N" means completing N full rounds. [`SetResult`](api-tactics.md#turnmanager-setresult) records a scripted
win/lose flag that `EvaluateObjectives` honors before any computed check.

```csharp
// One phase step of the canonical host loop.
bool TeamAlive(string t) => state.Field.Placed.Any(p => p.Unit.IsAlive && TeamOf(p.Unit.Side) == t);
var (team, wrapped) = state.Phases.AdvancePhase(TeamAlive);
state.SpawnDue();                 // <= comparison: skipped turns still reinforce
PhaseTick.Apply(state, team);     // terrain + poison + status tick for that team
switch (state.EvaluateObjectives()) {
    case ObjectiveOutcome.Victory: /* banner, campaign advance */ break;
    case ObjectiveOutcome.Defeat:   /* game over */ break;
}
```

## Objectives

| Spec | Met when |
| --- | --- |
| `Rout(side)` | no live unit of that side remains |
| `Survive(n)` | `TurnNumber > n` |
| `Protect(tags)` | a listed nid is dead **and** already spawned |
| `Seize(unit, region)` | that live unit stands on that region |
| `EventOnly` | never — `win`/`lose` commands decide |

The protect rule needs emphasis: future spawns (`spawnTurn > turn`) never trip it,
and the spawn comparison is `<=`, so a skipped turn number can neither drop a
reinforcement nor a protection check. Poll defeat specs before victory specs, and
poll after every elimination checkpoint (post-combat, turn end).

## Battlefield and threat

[`Battlefield`](api-tactics.md#battlefield) holds the board plus placements and answers occupancy questions:
[`LiveEnemiesOf`](api-tactics.md#battlefield-liveenemiesof), [`BlockingTiles`](api-tactics.md#battlefield-blockingtiles) (live hostiles), [`AllyTiles`](api-tactics.md#battlefield-allytiles) (live non-hostiles).
Hostility: Player and Ally fight the Enemy; Other fights nobody.

[`ThreatCalculator.ThreatRange`](api-tactics.md#threatcalculator-threatrange) unions every enemy's attack ring over every tile it can
reach — the danger zone for "show all threats" UI and defensive scoring. Like GBA
games, tiles adjacent to an enemy count as threatened even when impassable; tiles no
reachable position can strike stay out.

## Enemy AI

[`EnemyAi.Decide`](api-tactics.md#enemyai-decide) is greedy, in three steps:

1. **Strike in place.** If any usable weapon reaches any foe from the current tile,
   take the best shot — no movement.
2. **Move, then strike.** Score every (tile, weapon, target) triple and take the best
   landable tile.
3. **Approach.** Otherwise walk the reachable tile nearest to the closest foe; if
   already closest, wait.

Healer priority runs first: if a heal staff can reach a wounded ally (staff range from any reachable tile, fog-aware), the most-missing-HP ally is healed (move included). Otherwise scoring prefers secured kills (`10000 + damage`) over expected damage
(`damage × hit% + 2 × damage × crit%`), minus `CounterWeight × expected counter
damage` when a defender weapon is known (default weight 0.5 in the terrain-aware
overload; the legacy overload ignores counters). Pass a [`TerrainOf`](api-tactics.md#enemyai-terrainof) lookup
(usually [`CombatResolver.TerrainAt`](api-combat.md#combatresolver-terrainat) over the board) so fort/throne defense and
avoid shape picks; pass a [`DefenderWeaponOf`](api-tactics.md#enemyai-defenderweaponof) selector or the AI falls back to the
foe's first in-range weapon. Ties break toward least movement spent, then
nearest target, then coordinate order — never RNG or hash order. Every usable
(unbroken) weapon is considered. Pass an optional [`SupportLedger`](api-tactics.md#supportledger) to weight bonded
hit/avoid bonuses into the strike forecast and (swapped) into the counter-risk
forecast; the units' own `Skills` always shape both forecasts via the same hook as
`CombatResolver`. Staff behavior is future work.

```csharp
EnemyAi.TerrainOf terrainOf = (u, t) => {
    CombatResolver.TerrainAt(field.Board, t, out int avo, out int def);
    return (avo, def);
};
AiAction act = EnemyAi.Decide(field, foe, foe.Unit.MovementGroup,
    Rescue.EffectiveMove(foe.Unit), terrainOf,
    defenderWeaponOf: null, counterWeight: 0.5, rules: null,
    supports: ledger, visibleTiles: FogCalculator.VisibleTiles(field, foe.Unit.Side));
if (act.IsWait) { /* end unit */ }
else {
    if (act.MoveTo != null) MoveToken(foe, act.MoveTo.Value);
    if (act.Target != null) Strike(foe, act.Weapon!, act.Target);
}
```

## Battle triggers

[`BattleTriggers`](api-tactics.md#battletriggers) builds the vars dictionaries hosts hand to [`EventDispatcher`](api-events.md#eventdispatcher) —
pure, no mutation, shaped for the condition language (`unit.nid`, `position.x/y`):

| Helper | Trigger | Vars |
| --- | --- | --- |
| `CombatStartVars(attacker, defender, weaponKey, pos)` | `CombatStart` | `unit`, `unit2`, `item`, `position` |
| `CombatEndVars(..., defenderDied)` | `CombatEnd` | above plus `defender_died` |
| `LevelUpVars(unit)` | `UnitLevelUp` | `unit` (nid, level, hp) |
| `UnitWaitVars(unit, pos)` | `UnitWait` | `unit`, `position` |
| `DetectRegionEntries(placed, previousPositions, regions)` | `RegionEntered` | one `(unit, region, pos)` per true entry |

`DetectRegionEntries` reports only false→true region transitions: units that stayed
put, left a region, or have no previous position (fresh spawns) never report. See
[`BattleTriggers.DetectRegionEntries`](api-tactics.md#battletriggers-detectregionentries) for the exact shapes.

```csharp
// Around one exchange: announce it, resolve it, announce the result.
EventDispatcher.Dispatch(EventTrigger.CombatStart,
    BattleTriggers.CombatStartVars(atk.Unit, def.Unit, blade.Weapon.Key, atk.Position),
    chapter.Events, runner);
CombatResult result = CombatResolver.Resolve(...);
EventDispatcher.Dispatch(EventTrigger.CombatEnd,
    BattleTriggers.CombatEndVars(atk.Unit, def.Unit, blade.Weapon.Key, atk.Position,
        defenderDied: !def.Unit.IsAlive),
    chapter.Events, runner);
if (levelsGained > 0)
    EventDispatcher.Dispatch(EventTrigger.UnitLevelUp,
        BattleTriggers.LevelUpVars(atk.Unit), chapter.Events, runner);

// After movement: snapshot positions before, compare after.
var before = field.Placed.ToDictionary(p => p.Unit.Nid, p => p.Position);
... move tokens ...
foreach (var (unit, region, pos) in BattleTriggers.DetectRegionEntries(field.Placed, before, chapter.Regions))
    EventDispatcher.Dispatch(EventTrigger.RegionEntered,
        new Dictionary<string, object?> {
            ["unit"] = new Dictionary<string, object?> { ["nid"] = unit.Unit.Nid },
            ["region"] = region.Name,
            ["position"] = new Dictionary<string, object?> { ["x"] = pos.X, ["y"] = pos.Y },
        }, chapter.Events, runner);
```

## Talk

[`TalkHelper`](api-tactics.md#talkhelper) finds adjacent conversation partners: [`TalkCandidates(field, unit)`](api-tactics.md#talkhelper-talkcandidates)
lists every adjacent live placed unit of any side (ally recruits and enemy
parleys alike — hosts gate pairs with Talk-trigger conditions), [`CanTalk(a,
posA, b, posB)`](api-tactics.md#talkhelper-cantalk) is the pure adjacency+liveness predicate, and
[`TalkVars(talker, listener, talkerPos)`](api-tactics.md#talkhelper-talkvars) builds the `unit`/`unit2`/`position` vars
for `EventTrigger.Talk`. On acceptance hosts run `recruit;unit2` (or any
scripted effect).

```csharp
// Talk menu for the selected unit; pair gating lives in event conditions.
foreach (PlacedUnit other in TalkHelper.TalkCandidates(field, selected))
    ShowTalkOption(other); // e.g. enabled when a Talk event matches the pair
// On accept:
EventDispatcher.Dispatch(EventTrigger.Talk,
    TalkHelper.TalkVars(selected.Unit, other.Unit, selected.Position),
    chapter.Events, runner); // conditions like unit.nid == 'eirika' && unit2.nid == 'garcia'
```

## Interactables

`Chapter.Interactables` holds doors, chests, and villages placed on the map;
`Validate` checks in-bounds tiles and duplicate names, and the chapter format
adds an `INTERACTABLES` section. At runtime the headless [`ChapterRunner`](api-tactics.md#chapterrunner) drives
them with `visit;unit` (must stand on an unvisited village; once-only, recorded
as `Vars["visited_<name>"]` so hosts fire village events once) and
`open;unit;x,y` (opener adjacent to a closed door/chest; a locked one needs the
`KeyId` item in the opener's inventory and consumes it; recorded as
`Vars["opened_<name>"]` plus the data-side `Opened` mirror). The runner holds no
extra state beyond `Vars` — `Visited` derives the visited names from it, so
saves round-trip everything.

## Ally AI and fog-aware targeting

[`AllyAi.Decide`](api-tactics.md#allyai-decide) is the `EnemyAi` greedy loop with `counterWeight` 0.25 instead
of the enemy default 0.5 — GBA greens wade in and discount counter-risk. Hosts
call it for Ally-side units on the ally phase. Both it and the enemy
terrain-aware `Decide` take an optional `visibleTiles` set: foes outside it are
skipped entirely (targeting and approach), so blind units wait instead of
sniping the unseen. [`ThreatCalculator.SideThreat`](api-tactics.md#threatcalculator-sidethreat) takes an optional
`visibleOnly` set that drops threat tiles outside sight — pass
[`FogCalculator.VisibleTiles`](api-tactics.md#fogcalculator-visibletiles) through both to keep AI and danger zones honest in
fog.

## Dance and canto

Post-strike unit actions, both skill-gated (`skill:dance` / `skill:canto` on the
markdown unit row, matched case-insensitively, no catalog registration needed).
The core holds no acted flags — hosts own exhaustion state; [`Tactics.Dance`](api-tactics.md#dance) and
[`Tactics.Canto`](api-tactics.md#canto) are the shared legality predicates so every host grants the same
moves.

[`Dance.CanDance(dancer, target)`](api-tactics.md#dance-candance) needs a living skilled dancer adjacent to a
living non-hostile target that isn't itself (`CheckDance` is the throwing
validator). The `dance;dancer;target` event verb enforces the same rules and
fails fast headlessly; on success hosts clear their acted flag for the target
and mark the dancer acted.

[`Canto.CanCanto(unit, moveTotal, moveSpentBeforeAction)`](api-tactics.md#canto-cancanto) needs the skill plus
unspent move; [`Canto.RemainingMove`](api-tactics.md#canto-remainingmove) is the leftover (`total - spent`, clamped at
0, negatives throw). Pass laden totals through [`Rescue.EffectiveMove`](api-tactics.md#rescue-effectivemove).

```csharp
// Dancer's turn: shared legality check, then the host flips its own acted flags.
Dance.CheckDance(dancer, ally); // throws when illegal (skill, range, side, liveness)
ClearActed(ally.Unit.Nid);
MarkActed(dancer.Unit.Nid);

// Rider attacks, then spends whatever move is left (laden totals via Rescue).
int total = Rescue.EffectiveMove(rider.Unit);
if (Canto.CanCanto(rider.Unit, total, spentBeforeAttack))
    AllowMove(rider, Canto.RemainingMove(total, spentBeforeAttack));
```

## Shove, swap, and steal

[`Shove.CanShove/ExecuteShove`](api-tactics.md#shove-canshove) pushes an adjacent target one tile away when `target.Con ≤ shover.Con + 2` and the destination is in-bounds and vacant; [`Shove.CanSwap/ExecuteSwap`](api-tactics.md#shove-canswap) swaps two adjacent live units. [`Steal.CanSteal/Execute`](api-tactics.md#steal-cansteal) steals a light item when adjacent, thief Speed > target Speed, `Weapon.CanSteal` true, and `Weight ≤ thief.Con`. Verbs `shove;`, `swap;`, `steal;` enforce the same rules headlessly.

## Support tick and AI types

`SupportLedger.TickAdjacent` grants 1 point per adjacent alive pair; `Unit.AiType` (`rush` default, `hold` never moves, `patrol` pings between `PatrolA`/`PatrolB`) gates `EnemyAi.Decide`. `ReplayHash.Compute` hashes placements+HP+RNG for deterministic replay checks. `Statuses.IsSilenced` blocks staff/magic; `ChapterRunner` `equip` now enforces `WeaponRanks.CanEquip`; convoy aliases `ChapterState.Convoy` so saves round-trip.

## Phase tick

[`PhaseTick.Apply(state, team, rules?)`](api-tactics.md#phasetick-apply) runs upkeep for one team at its phase
boundary, in deterministic X-then-Y tile order: terrain HP swing
([`TerrainEffects.Apply`](api-data.md#terraineffects-apply)), then poison damage (`PoisonDamage`, default 3) for units
with the `poison` status, then `Statuses.Tick`. Returns per-unit
`(nid, hpDelta, expiredStatuses)`. Ticks never kill — a unit alive at tick start is
left at 1 HP minimum. `PhaseTickRules` toggles each step (`PoisonDamage`,
`TickStatuses`, `ApplyTerrain`).

## Deployment picker

[`DeploymentSelector.ChooseDeployments(chapter, nids)`](api-tactics.md#deploymentselector-choosedeployments) resolves a requested roster
against the chapter's deployments and returns `(chosen, errors)` without throwing:
unknown nids error, `MaxDeploy` is enforced when positive, and when any
`Formation`-kind region exists each pick must stand inside one. Hosts surface the
errors in the deployment screen.

```csharp
var (chosen, errors) = DeploymentSelector.ChooseDeployments(chapter, requestedNids);
if (errors.Count > 0) ShowDeploymentErrors(errors); // unknown nid, MaxDeploy, formation
else StartChapter(chosen);
```

## Experience

Kills and chips feed [`Experience.Gain`](api-tactics.md#experience-gain) / `AddExp` / `LevelUp` on the growth RNG
stream; see [data](data.md). Hosts grant XP after combat via
[`CombatGains.ApplyAfterCombat`](api-tactics.md#combatgains-applyaftercombat), which returns `(expGained, levelsGained)` — the
headless runner itself still never auto-grants.

## Saves

[`SaveSystem.Save(state)`](api-tactics.md#savesystem-save) snapshots positions, HP, level/XP, inventory uses, turn and
phase index, vars, and both RNG states. `Restore(chapter, snap, baseSeed)` rebuilds
an equivalent state; armory keys are the save contract (missing keys throw), and
already-placed deployments are marked so `SpawnDue` never duplicates. DTOs are plain
data — hosts serialize with whatever they have (`System.Text.Json`, Unity
`JsonUtility`, `localStorage`). Snapshots now include vision, skills, statuses, and
rescue state.

```csharp
// Suspend/resume (or turnwheel): snapshot, serialize with your stack, restore.
ChapterSnapshot snap = SaveSystem.Save(state);
File.WriteAllText(slot, JsonSerializer.Serialize(snap)); // host's serializer
ChapterState back = SaveSystem.Restore(chapter,
    JsonSerializer.Deserialize<ChapterSnapshot>(File.ReadAllText(slot))!, seed);
```

## Fog

[`FogCalculator.VisibleTiles(field, side)`](api-tactics.md#fogcalculator-visibletiles) unions Manhattan balls of radius
`Unit.Vision` over live units of that side; [`HiddenEnemiesOf`](api-tactics.md#fogcalculator-hiddenenemiesof) lists live foes outside
it. No line-of-sight blocking — walls cost movement but not sight. Hosts hide tokens,
skip threat from hidden units, and gate targeting on visibility.

```csharp
var seen = FogCalculator.VisibleTiles(field, Affiliation.Player);
tokenRenderer.SetVisible(foe, seen.Contains(foe.Position));
var danger = ThreatCalculator.SideThreat(field, enemies, LoadoutOf, visibleOnly: seen);
```

## Rescue

[`Rescue.CanGrab/Grab/Drop`](api-tactics.md#rescue-cangrab) implement GBA-simplified carry: adjacent grab when the
carrier's hands are free and passenger Con fits (`<= carrier Con + 2`), one passenger
at a time, laden move halved ([`EffectiveMove`](api-tactics.md#rescue-effectivemove)), passenger off-map while carried
(untargetable — hosts hide the token). Event verbs `rescue;`/`drop;` drive it; the
headless runner holds passengers in `Carried`.

```csharp
if (Rescue.CanGrab(pegasus.Unit, eirika.Unit, pegasus.Position, eirika.Position))
    Rescue.Grab(field, pegasus, eirika); // passenger token leaves the map
int move = Rescue.EffectiveMove(pegasus.Unit); // halved while laden
Rescue.Drop(field, pegasus, eirika.Unit, tile); // tile must be adjacent and free
```

## Support

[`SupportLedger`](api-tactics.md#supportledger) tracks points per unit pair and ranks them C (30) / B (80) / A (150).
[`BonusFor`](api-tactics.md#supportledger-bonusfor) grants +5 hit/avoid per rank step per bonded ally within 3 tiles
(C +5, B +10, A +15). Pure over placements — add the returned bonus to forecast hit
and avoid on both sides before display and AI scoring.

```csharp
ledger.AddPoints("eirika", "seth", 80); // rank B
var (hit, avoid) = ledger.BonusFor(eirika, eirikaPos, field.Placed);
// hit/avoid feed Forecast/Resolve supportHit/supportAvoid (see combat).
```
