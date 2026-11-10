# Chapter format

Chapters are single Markdown files (`.md`), one per chapter.
[`MarkdownChapterLoader.Load(text, fileName)`](api-data.md#markdownchapterloader-load) parses a file
into a [`Chapter`](api-data.md#chapter), collecting every error with file + line instead of failing fast.

## Layout

```markdown
# CH1: First Blood

## META
format: 1
objective: Rout the foe. Keep Eirika alive.
victory: rout
defeat: all_dead
defeat: protect eirika
next: ch2
maxDeploy: 4

## LEGEND
.: plain
F: forest
W: wall
T: throne

## MAP
.....
.FF..
..WT.

## ARMORY
iron-sword Iron_Sword sword E 5 90 0 5 1-1 40
fire Fire anima E 5 90 0 4 1-2 40 magic
hammer Hammer axe E 5 70 0 8 1-1 40 effective:armor
mend Mend staff E 0 100 0 0 1-1 20 heal:10

## TEMPLATES
brigand enemy brigand 1 22 6 4 3 1 3 0 5 items:iron-axe

## UNITS
0,0 hero player mercenary 1 20 5 6 7 3 4 1 8 items:iron-sword
4,0 foe2 enemy brigand 1 22 6 4 3 1 3 0 9 turn:2 group:alpha

## TERRAINS
marsh Marsh 2 2 3 1 10 0 hp:-10 pirate:1

## REGIONS
village-door event 3,0 3,0

## INTERACTABLES
gate door 3,1 locked brass-key
box chest 1,2
haven village 0,0

## EVENTS
### intro on start priority 10
- Narrator: Defend the village.
### foe-down on unit_death if unit.nid == 'foe1'
set_flag;foe1_down;true
### turn3 on turn 3 - player
- Narrator: They keep coming!
```

## Rules

- First `# ...` line is the title (`nid: name`); any other single-`#` line is a
  comment. `<!-- -->` blocks are stripped. Sections arrive in any order.
- `META`: `format: 1` is required (newer formats fail loudly). `victory:` is `rout`,
  `survive N`, `seize <unit> <region>`, or `event`; `defeat:` lines accumulate (`all_dead`, `protect a,b`,
  `event`). `objective:` is display text only; `next:` links chapters. `maxDeploy:` caps the turn-1 player roster (0 = everyone); `maxinventory:` / `max_inventory:` caps per-unit inventory (default 5, validated against deployments and templates).
- `LEGEND` maps one character to a terrain key; `MAP` rows must be rectangular.
  Standard terrain keys work without declaration.
- `ARMORY` rows: `key name type rank might hit crit wt minR-maxR uses [magic] [effective:a,b] [heal:N] [status:name:N] [price:N]`
  (`Iron_Sword` underscores become spaces; `price:N` lists the weapon in village shops, 0/absent = spawn pool only).
  (`Iron_Sword` underscores become spaces). `TEMPLATES` and `UNITS` share the row
  shape `x,y nid side class lvl hp pow skl spd lck def res con` (position omitted for
  templates) plus `turn:N group:G move:M type:T name:N vision:V skill:a,b items:a,b ai:rush|hold|patrol patrol:x,y~x,y` options. `ai` sets `Unit.AiType`; `patrol` sets `PatrolA/B` waypoints (hex-ready via `TbsGridAdapter`).
- `TERRAINS` rows: `key name inf arm cav fly avoid def [hp:N] [group:cost]`.
  Custom keys must be new; unknown groups default to cost 1.
- `REGIONS` rows: `name kind x0,y0 x1,y1` (inclusive rect; kind is
  event/formation — [`RegionKind`](api-regions.md#regionkind)).
- `INTERACTABLES` rows: `name kind x,y [locked keyId]` (kind is
  door/chest/village; `locked keyId` only for doors/chests — the key is an
  inventory item matched by weapon key and consumed on use). Villages are
  visited with `visit;unit`, doors/chests with `open;unit;x,y`.
- Unit `items:` entries feed the `equip` (reorder first) and `trade` (adjacent
  swap) verbs; `drop_item`/`take_item` move entries through the runner convoy.

```csharp
// Load one chapter file: every error carries file + line, and a failed load
// returns no chapter so hosts never run half-parsed data.
ChapterParseResult res = MarkdownChapterLoader.Load(File.ReadAllText("ch1.md"), "ch1.md");
if (!res.Ok || res.Chapter == null) {
    foreach (ChapterParseError e in res.Errors) Console.WriteLine(e); // ch1.md:12: ...
    return;
}
Chapter chapter = res.Chapter;
if (chapter.Validate().Count > 0) { /* surface in the editor */ }
ChapterState state = ChapterState.StartNew(chapter, seed: 4242);
```

[`ChapterParseResult`](api-data.md#chapterparseresult) carries `Ok`, `Chapter`, and `Errors`; [`ChapterParseError`](api-data.md#chapterparseerror) formats as `file:line: message`.
- `EVENTS`: `### <name> on <trigger> [if <cond>] [priority N]`, then raw
  `verb;args` lines or `- speaker: text` dialogue sugar (desugars to `speak`).
  Trigger aliases include `start`, `end`, `death`, `talk`; `turn N - player|enemy`
  auto-adds a `turn == N` condition.
- Names, keys, and nids are whitespace-separated cells, so multi-word display names
  use underscores (`Iron_Sword` becomes "Iron Sword", `name:` works the same way).
  Event lines split on `;`, so dialogue text must avoid semicolons — an extra `;`
  breaks the verb's arity and fails the load. Every violation reports
  `file:line: message`, and a failed load returns no chapter.
