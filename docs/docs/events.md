# Events

Trigger/condition/command scripting: every event is a unique name plus a trigger, a condition,
and an ordered command list. Full member reference: [API: Events](api-events.md).

## Triggers

[`EventTrigger`](api-events.md#eventtrigger) values and the vars each fires with:

| Trigger | Fires with vars |
| --- | --- |
| `LevelStart`, `LevelEnd` | chapter open / close |
| `PlayerPhaseStart`, `EnemyPhaseStart` | phase boundaries |
| `UnitDeath` | `unit` nid, `position` |
| `UnitWait`, `UnitSelect` | `unit` nid, `position` |
| `UnitLevelUp` | `unit` nid |
| `CombatStart`, `CombatEnd` | `unit`, `unit2`, `item`, `position` |
| `Talk` | `unit`, `unit2`, `position` |
| `Support` | `unit`, `unit2`, `rank` |
| `RegionEntered` | `unit`, `region`, `position` |

## Commands

Text lines shaped `verb;arg;arg` (for example `speak;Eirika;Oh no!`). The catalog
fixes each verb's arity ([`CommandCatalog`](api-events.md#commandcatalog)):

| Verb | Args |
| --- | --- |
| `speak` | speaker, text |
| `add_portrait` / `remove_portrait` | unit; side for add |
| `move_unit` | unit, `x,y` |
| `spawn_unit` | template nid, faction, `x,y` |
| `remove_unit` | unit |
| `give_item` | unit, armory key |
| `set_flag` | flag, true/false |
| `recruit` | unit, optional faction (default player) — talk/recruit side flip |
| `heal` | unit, amount — clamped to MaxHP |
| `damage` | unit, amount — flat damage, floors at 0 |
| `promote` | unit, new class key — rekeys `ClassKey` |
| `apply_status` | unit, name, optional duration (default 3) |
| `clear_status` | unit, name |
| `rescue` | carrier, passenger — pick up adjacent passenger |
| `drop` | carrier, `x,y` — drop carried passenger adjacent |
| `teach_skill` | unit, skill id |
| `equip` | unit, weapon key — reorder that entry first (errors when missing) |
| `trade` | giver, taker, weapon key — move the entry; the pair must be adjacent |
| `drop_item` | unit, weapon key — stash the entry into the convoy |
| `take_item` | unit, weapon key — pull the entry out of the convoy |
| `gold` | amount — adjust party gold (negative spends; never below zero) |
| `buy` | unit, weapon key — buy priced armory stock (`price:N`) into inventory |
| `sell` | unit, weapon key — sell a carried entry for half price (floored) |
| `visit` | unit — visit the village under the unit; once-only (sets `visited_<name>`) |
| `open` | unit, `x,y` — open the adjacent door/chest at the tile; a locked one consumes the `KeyId` item from the opener (sets `opened_<name>`) |
| `dance` | dancer, target — refresh an adjacent ally's turn (hosts clear acted flags) |
| `shove` | shover, target — push adjacent target one tile away (Con ≤ shover Con+2, dest free) |
| `swap` | a, b — swap positions of two adjacent units |
| `steal` | thief, target, weapon key — steal adjacent light item when faster (weight ≤ thief Con) |
| `warp` | unit, `x,y` — teleport if InBounds and not blocked |
| `torch` | unit, amount — apply torch status 3 + Vision bump via [`CombatRules.TorchVisionBonus`](api-combat.md#combatrules-torchvisionbonus) |
| `win` / `lose` | (optional reason) |
| `wait` | seconds (presentation timing) |

## Conditions

Tiny expression language for gating ([`SimpleConditionEvaluator`](api-events.md#simpleconditionevaluator)): clauses joined by `&&`, each shaped
`path op literal` with `==`, `!=`, `>`, `<`, `>=`, `<=`. Paths resolve into nested
dictionaries (`unit.nid`), literals are `'quoted'`, numbers, or true/false. Unknown
paths are null and never equal a literal. Operators inside quotes are ignored, so
`name == 'a==b'` splits correctly. `&&` inside quotes is literal too
(`name == 'a && b'`), while a stray single `&` or an empty clause is a load-time
error. Conditions are bounded (length and clause caps in `EventLimits`);
overlong input fails fast with a position.

## Running

[`EventRunner.Run`](api-events.md#eventrunner-run) checks the condition, validates **every** command up front, then
dispatches in order — a broken verb fails before any side effect. [`EventDispatcher`](api-events.md#eventdispatcher)
runs all same-trigger events by descending priority (then name), and stops at the
first error. Effects reach the world only through [`IEventExecutor`](api-events.md#ieventexecutor), which the
headless [`ChapterRunner`](api-tactics.md#chapterrunner) and each host's own presentation layer implement (see
[hosting](hosting.md)).

## Hardening (limits, policy, orchestration)

Adversarial or malformed content fails fast instead of hanging or half-applying:

- `EventLimits` caps command length, condition
  length/clauses, commands per event, and events per chapter. The loader reports
  `file:line` for every breach; the evaluator throws `FormatException`.
- `wait` takes a finite numeric seconds value in `0..MaxWaitSeconds`
  (600s); `NaN`, infinities, negatives, and over-max values are rejected.
- Verbs are letter-led `letters/digits/underscore` tokens; dialogue text must
  avoid semicolons (they split command args).
- `EventPolicy` classifies high-impact verbs
  (`spawn_unit`, `remove_unit`, `set_terrain`, `gold`, `buy`, `sell`, `win`,
  `lose`). `Trusted` (default) allows the full catalog; `Restricted` denies
  those verbs at validation time. Hosts select the mode (e.g. SRPGMono's
  `--restricted-events` play flag) and thread it through dispatch, talk, and
  cutscene validation so all three agree.
- `EventOrchestrator` is the single
  authoritative pipeline: match trigger, sort priority/name, evaluate condition
  (trigger vars win over chapter state vars), validate every command up front
  with the active policy, then mark whether the event needs staged presentation.
  Hosts execute inline or staged per plan and stop at the first error plan;
  one-line diagnostics carry event, trigger, command index, verb, and mode.

```csharp
// Author inline, run headless, or dispatch by trigger with vars.
var ev = new GameEvent("eirika-falls", EventTrigger.UnitDeath, "unit.nid == 'eirika'");
ev.AddCommand("speak;Seth;No!");
ev.AddCommand("lose;Eirika fell.");

var runner = new ChapterRunner(state);
EventRunResult once = EventRunner.Run(ev, vars, runner); // condition + validate + run

var deaths = EventDispatcher.Dispatch(EventTrigger.UnitDeath,
    new Dictionary<string, object?> {
        ["unit"] = new Dictionary<string, object?> { ["nid"] = "eirika" },
    }, chapter.Events, runner); // every matching event, priority first
```
