# Chapter

Chapters can be authored as Markdown files — see [chapter format](chapter-format.md)
([`MarkdownChapterLoader`](api-data.md#markdownchapterloader)), which parses boards, deployments, armory, regions, and
events with file-and-line errors.

## Definition vs runtime

- [`Chapter`](api-data.md#chapter) is data: nid, title, board, deployments, regions, events, victory/defeat
  specs, armory, unit templates, `MaxDeploy`, formation region, next-chapter link.
- [`ChapterState`](api-tactics.md#chapterstate) is the live run: battlefield, turn manager, two RNG streams, level
  vars, and spawn bookkeeping. [`StartNew(chapter, seed)`](api-tactics.md#chapterstate-startnew) deploys turn-1 units.

## Lifecycle

1. `StartNew` — turn-1 deployments appear; streams derive from the seed
   (`CreateStream(seed, 0)` for combat, `(seed, 1)` for growths).
2. [`AdvancePhase`](api-tactics.md#turnmanager-advancephase) + [`SpawnDue`](api-tactics.md#chapterstate-spawndue) — after every phase advance, deploy everything whose
   spawn turn has been reached. The comparison is `<=`: jumping from turn 1 to turn
   3 still spawns turn-2 reinforcements.
3. Actions and events — movement, combat, AI, and trigger dispatch.
4. [`EvaluateObjectives`](api-tactics.md#chapterstate-evaluateobjectives) — scripted result first, then defeat specs, then victory
   specs; otherwise ongoing.

`MaxDeploy` caps turn-1 player deployments (0 means everyone); [`Validate`](api-data.md#chapter-validate) flags
over-deployment along with every other authoring error class.

```csharp
// The canonical host lifecycle: open, step phases, close on outcome.
ChapterState state = ChapterState.StartNew(chapter, seed: 4242);
var runner = new ChapterRunner(state);
EventDispatcher.Dispatch(EventTrigger.LevelStart, new Dictionary<string, object?>(),
    chapter.Events, runner);

while (state.EvaluateObjectives() == ObjectiveOutcome.Ongoing) {
    state.Phases.AdvancePhase(team => TeamAlive(state, team));
    state.SpawnDue();
    PhaseTick.Apply(state, state.Phases.CurrentTeam);
    RunPhaseActions(state, runner); // movement, combat, AI, trigger dispatch
}
```

## Headless runner

[`ChapterRunner`](api-tactics.md#chapterrunner) implements [`IEventExecutor`](api-events.md#ieventexecutor) over a state so chapters play without a
renderer — this is how chapters are tested today, and the same contract any host's
presentation layer implements (see [hosting](hosting.md)):

| Command | Headless behavior |
| --- | --- |
| `speak` | appended to the dialogue log |
| `add_portrait`, `remove_portrait`, `wait` | recorded, no-op (presentation-only) |
| `move_unit` | teleports the placed unit |
| `spawn_unit` | clones the template, suffixing nids (`brigand`, `brigand#2`, …) |
| `remove_unit` | extracts the placement |
| `give_item` | adds the armory weapon to inventory |
| `set_flag` | stores true/false in level vars |
| `recruit` / `promote` / `heal` / `damage` | side flip, class rekey, HP swing |
| `apply_status` / `clear_status` / `teach_skill` | status and skill bookkeeping |
| `rescue` / `drop` | carry into `Carried`, drop back onto the map |
| `equip` / `trade` / `drop_item` / `take_item` | reorder, swap adjacent, convoy stash/pull |
| `visit` / `open` | once-only village flag, door/chest unlock (consumes key) |
| `dance` | validates dancer skill, adjacency, and side; hosts refresh the target's turn |
| `win` / `lose` | sets the scripted result |

Every executed command is also appended to the stage log, giving a complete
headless trace. Runtime failures (missing unit, template, or armory entry) throw —
authoring errors must be loud, never silent. Passengers ride in [`Carried`](api-tactics.md#chapterrunner-carried),
stashed entries in [`Convoy`](api-tactics.md#chapterrunner-convoy), visited villages derive from [`Visited`](api-tactics.md#chapterrunner-visited).
