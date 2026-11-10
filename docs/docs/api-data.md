# API: SrpgCore.Data

> Generated from `../SrpgCore` sources by `scripts/build-api.ts` — do not edit by hand.

Guide: [data](data.md).

## Deployment

One unit's chapter entry: the Unit snapshot, Position tile it spawns on, SpawnTurn (1 = starting roster), and optional Group id for spawn_group events.

### Deployment.Unit

Deployed unit reference.

```csharp
public Unit Unit { get; }
```

Type: `Unit`

### Deployment.Position

Board tile the unit spawns on.

```csharp
public GridPoint Position { get; }
```

Type: `GridPoint`

### Deployment.SpawnTurn

Round the unit spawns on, 1 for starting roster.

```csharp
public int SpawnTurn { get; }
```

Type: `int`

### Deployment.Group

Reinforcement group id for spawn_group-style events, empty when none.

```csharp
public string Group { get; }
```

Type: `string`

### Deployment constructor

Binds a unit to a spawn tile, turn, and optional group.

```csharp
public Deployment(Unit unit, GridPoint position, int spawnTurn = 1, string group =)
```

| Parameter | Type | Description |
| --- | --- | --- |
| unit | `Unit` | Unit to deploy. |
| position | `GridPoint` | Board tile the unit spawns on. |
| spawnTurn | `int` | Round the unit spawns on, minimum 1. |
| group | `string` | Reinforcement group id, empty when none. |

## ObjectiveKind

Victory and defeat objective shapes evaluated by the chapter runtime.

| Name | Value | Description |
| --- | --- | --- |
| Rout |  | Win by destroying every live unit of the tagged side. |
| Survive |  | Win by completing the tagged number of full rounds. |
| Protect |  | Lose when any listed nid dies after spawning. |
| Seize |  | Win when a live tagged unit stands on the tagged region. |
| Escape |  | Win when a live tagged unit stands on the tagged escape region. |
| EventOnly |  | Never auto-evaluated, win and lose commands decide. |

## ObjectiveSpec

One victory or defeat rule: Kind selects shape, then Side for Rout, Turns for Survive, Tags for Protect (protected nids) and Seize or Escape (unit nid + region name), and EventOnly for scripted win/lose.

### ObjectiveSpec.Kind

Objective shape driving evaluation.

```csharp
public ObjectiveKind Kind { get; }
```

Type: `ObjectiveKind`

### ObjectiveSpec.Side

Target side for rout-style checks.

```csharp
public Affiliation Side { get; set; }
```

Type: `Affiliation`

### ObjectiveSpec.Turns

Full rounds required for survive objectives.

```csharp
public int Turns { get; set; }
```

Type: `int`

### ObjectiveSpec.Tags

Unit nids, region names, or seize pair in unit then region order.

```csharp
public List<string> Tags { get; }
```

Type: `List<string>`

### ObjectiveSpec.RoutEnemies

Builds a victory rule destroying every live enemy unit.

```csharp
public static ObjectiveSpec RoutEnemies()
```

Returns: `ObjectiveSpec` — Rout spec targeting the enemy side.

### ObjectiveSpec.Survive

Builds a victory rule completing a number of full rounds.

```csharp
public static ObjectiveSpec Survive(int turns)
```

| Parameter | Type | Description |
| --- | --- | --- |
| turns | `int` | Full rounds required, must be at least 1. |

Returns: `ObjectiveSpec` — Survive spec with the tagged turn count.

### ObjectiveSpec.Protect

Builds a defeat rule firing when any listed unit nid dies.

```csharp
public static ObjectiveSpec Protect(params string[] tags)
```

| Parameter | Type | Description |
| --- | --- | --- |
| tags | `params string[]` | Protected unit nids matched against deployments and templates. |

Returns: `ObjectiveSpec` — Protect spec carrying the tagged nids.

### ObjectiveSpec.Seize

Builds a victory rule for standing a live unit on a region.

```csharp
public static ObjectiveSpec Seize(string unitNid, string regionName)
```

| Parameter | Type | Description |
| --- | --- | --- |
| unitNid | `string` | Nid of the seizing unit. |
| regionName | `string` | Name of the target region. |

Returns: `ObjectiveSpec` — Seize spec carrying unit then region names.

### ObjectiveSpec.Escape

Builds a victory rule for escaping a live unit to a region.

```csharp
public static ObjectiveSpec Escape(string unitNid, string regionName)
```

| Parameter | Type | Description |
| --- | --- | --- |
| unitNid | `string` | Nid of the escaping unit. |
| regionName | `string` | Name of the escape region. |

Returns: `ObjectiveSpec` — Escape spec carrying unit then region names.

### ObjectiveSpec.AllDead

Builds a defeat rule firing when no live player unit remains.

```csharp
public static ObjectiveSpec AllDead()
```

Returns: `ObjectiveSpec` — Rout spec targeting the player side.

### ObjectiveSpec.EventOnly

Builds a rule never auto-evaluated so event commands decide.

```csharp
public static ObjectiveSpec EventOnly()
```

Returns: `ObjectiveSpec` — EventOnly spec for scripted outcomes.

## InteractableKind

Map interactable kind: doors/chests open, villages visit.

| Name | Value | Description |
| --- | --- | --- |
| Door |  | Openable door tile, optionally locked behind a key item. |
| Chest |  | Openable chest tile yielding its contents once. |
| Village |  | Visitable village tile granting its reward once. |

## Interactable

One tile interactable: a locked/unlocked door or chest (opened via the `open` verb, locked ones consuming a KeyId inventory item) or a village (visited once via the `visit` verb). Opened/visited state at runtime lives in ChapterState.Vars ("opened_<name>"/"visited_<name>") so saves round-trip; Opened mirrors the flag for data-side readers.

### Interactable.Name

Unique name matched by open and visit event verbs.

```csharp
public string Name { get; set; }
```

Type: `string`

### Interactable.Kind

Door, chest, or village behavior.

```csharp
public InteractableKind Kind { get; set; }
```

Type: `InteractableKind`

### Interactable.Position

Board tile this interactable sits on.

```csharp
public GridPoint Position { get; set; }
```

Type: `GridPoint`

### Interactable.Locked

True when opening consumes a key item.

```csharp
public bool Locked { get; set; }
```

Type: `bool`

### Interactable.KeyId

Inventory item id consumed to unlock, empty when keyless.

```csharp
public string KeyId { get; set; }
```

Type: `string`

### Interactable.Opened

Mirrors the opened or visited runtime flag for data-side readers.

```csharp
public bool Opened { get; set; }
```

Type: `bool`

### Interactable.BecomesTerrainKey

Terrain key the tile becomes after open or visit, empty when none.

```csharp
public string BecomesTerrainKey { get; set; }
```

Type: `string`

### Interactable constructor

Places one door, chest, or village on the map.

```csharp
public Interactable(string name, InteractableKind kind, GridPoint position)
```

| Parameter | Type | Description |
| --- | --- | --- |
| name | `string` | Unique name matched by event verbs. |
| kind | `InteractableKind` | Door, chest, or village behavior. |
| position | `GridPoint` | Board tile this interactable sits on. |

## Chapter

Chapter definition (data side): Nid/Title identity, Board, Deployments, Regions, Events, Victory/Defeat ObjectiveSpecs, Armory weapons by key for give_item, UnitTemplates for spawn_unit, MaxDeploy roster cap (0 = everyone), and NextChapter link. Validate() reports authoring errors for editors to surface; a clean chapter validates to an empty list.

### Chapter.Nid

Chapter id linking NextChapter references and save slots.

```csharp
public string Nid { get; set; }
```

Type: `string`

### Chapter.Title

Display title for banners and chapter screens.

```csharp
public string Title { get; set; }
```

Type: `string`

### Chapter.Board

Playable board, null until the loader assigns one.

```csharp
public Board? Board { get; set; }
```

Type: `Board?`

### Chapter.ObjectiveText

Display objective for banners/menus (not evaluated).

```csharp
public string ObjectiveText { get; set; }
```

Type: `string`

### Chapter.Deployments

Unit placements with spawn turns and reinforcement groups.

```csharp
public List<Deployment> Deployments { get; }
```

Type: `List<Deployment>`

### Chapter.Regions

Named tile rects for events, seize goals, and formation.

```csharp
public List<Region> Regions { get; }
```

Type: `List<Region>`

### Chapter.Events

Trigger-driven event scripts with validated verb commands.

```csharp
public List<GameEvent> Events { get; }
```

Type: `List<GameEvent>`

### Chapter.Victory

Victory rules, any one completing ends the chapter won.

```csharp
public List<ObjectiveSpec> Victory { get; }
```

Type: `List<ObjectiveSpec>`

### Chapter.Defeat

Defeat rules, any one completing ends the chapter lost.

```csharp
public List<ObjectiveSpec> Defeat { get; }
```

Type: `List<ObjectiveSpec>`

### Chapter.Armory

Weapons by key for unit items and give_item spawns.

```csharp
public Dictionary<string, Weapon> Armory { get; }
```

Type: `Dictionary<string, Weapon>`

### Chapter.UnitTemplates

Unit blueprints by nid for spawn_unit reinforcements.

```csharp
public Dictionary<string, Unit> UnitTemplates { get; }
```

Type: `Dictionary<string, Unit>`

### Chapter.Interactables

Doors, chests, and villages placed on the map (see Interactable).

```csharp
public List<Interactable> Interactables { get; }
```

Type: `List<Interactable>`

### Chapter.MaxDeploy

0 = deploy everyone; otherwise max player units on turn 1.

```csharp
public int MaxDeploy { get; set; }
```

Type: `int`

### Chapter.MaxInventory

Maximum inventory entries per unit, default 5.

```csharp
public int MaxInventory { get; set; }
```

Type: `int`

### Chapter.IsIso

Diamond (isometric) presentation when true; flat grid otherwise. Set once per chapter via META view, like Unity's 2D/3D project choice.

```csharp
public bool IsIso { get; set; }
```

Type: `bool`

### Chapter.FormationRegion

Name of the formation region limiting deployment tiles.

```csharp
public string FormationRegion { get; set; }
```

Type: `string`

### Chapter.NextChapter

Nid of the following chapter, empty when this chapter ends the chain.

```csharp
public string NextChapter { get; set; }
```

Type: `string`

### Chapter.Validate

Reports authoring errors for editors to surface, empty when clean.

```csharp
public List<string> Validate()
```

Returns: `List<string>` — One message per off-board tile, duplicate name, bad verb, or dangling tag.

## WeaponType

Weapon damage triangle and magic families plus staves.

| Name | Value | Description |
| --- | --- | --- |
| Sword | 0 | Beats axes, loses to lances in the GBA triangle. |
| Lance | 1 | Beats swords, loses to axes in the GBA triangle. |
| Axe | 2 | Beats lances, loses to swords in the GBA triangle. |
| Bow | 3 | Ranged physical weapon, never participates in triangle bonus. |
| Anima | 4 | Fire, thunder, and wind tomes. |
| Light | 5 | Light magic tomes. |
| Dark | 6 | Dark magic tomes. |
| Staff | 7 | Healing and status staves, targets allies. |

## WeaponRank

Weapon rank letter gating who can equip a weapon, E lowest through S highest.

| Name | Value | Description |
| --- | --- | --- |
| E | 0 | Entry rank, always reachable at zero wexp. |
| D | 1 | Unlocked at 20 wexp. |
| C | 2 | Unlocked at 50 wexp. |
| B | 3 | Unlocked at 80 wexp. |
| A | 4 | Unlocked at 120 wexp. |
| S | 5 | Top rank, unlocked at 180 wexp. |

## Affiliation

Which army a unit fights for. Values: Player (player-controlled), Enemy (hostile), Ally (NPC own phase), Other (neutral, fights nobody).

| Name | Value | Description |
| --- | --- | --- |
| Player | 0 | Player-controlled units. |
| Enemy | 1 | Hostile units acting on the enemy phase. |
| Ally | 2 | NPC ally with its own phase, never player-controlled. |
| Other | 3 | Neutral third party fighting nobody. |

## ChapterParseError

One loader complaint with file + 1-based line (0 = chapter-level).

### ChapterParseError.File

Source file name the complaint came from.

```csharp
public string File { get; }
```

Type: `string`

### ChapterParseError.Line

One-based line number, 0 for chapter-level problems.

```csharp
public int Line { get; }
```

Type: `int`

### ChapterParseError.Message

Human-readable complaint for editors to surface.

```csharp
public string Message { get; }
```

Type: `string`

### ChapterParseError constructor

Records one loader complaint with file and line.

```csharp
public ChapterParseError(string file, int line, string message)
```

| Parameter | Type | Description |
| --- | --- | --- |
| file | `string` | Source file name. |
| line | `int` | One-based line number, 0 for chapter-level problems. |
| message | `string` | Human-readable complaint. |

### ChapterParseError.ToString

Renders file, line, and message as one log line.

```csharp
public override string ToString()
```

Returns: `string` — Single-line file and line complaint string.

## ChapterParseResult

Outcome of one markdown load: a chapter on success plus every complaint.

### ChapterParseResult.Chapter

Parsed chapter, null when any error was collected.

```csharp
public Chapter? Chapter { get; set; }
```

Type: `Chapter?`

### ChapterParseResult.Errors

Every file and line complaint, empty on a clean load.

```csharp
public List<ChapterParseError> Errors { get; }
```

Type: `List<ChapterParseError>`

### ChapterParseResult.Ok

True when a chapter parsed with zero errors.

```csharp
public bool Ok => Errors.Count == 0 && Chapter != null;
```

Type: `bool`

## MarkdownChapterLoader

Markdown chapter format: one file per chapter with META / LEGEND / MAP / ARMORY / TEMPLATES / UNITS / TERRAINS / REGIONS / INTERACTABLES / EVENTS sections mapping 1:1 onto Chapter. Collects every error with file + line instead of throwing on the first, so editors can surface them all. Full spec: SRPGDocs chapter-format page. Current format: 1.

### MarkdownChapterLoader.CurrentFormat

Markdown format version this loader reads, required as format 1 in META.

```csharp
public const int CurrentFormat = 1;
```

Type: `int`

### MarkdownChapterLoader.Load

Parses one markdown chapter file using the standard terrain set.

```csharp
public static ChapterParseResult Load(string text, string fileName)
```

| Parameter | Type | Description |
| --- | --- | --- |
| text | `string` | Full markdown file contents. |
| fileName | `string` | File name stamped onto every error. |

Returns: `ChapterParseResult` — Result carrying the chapter on success or every complaint on failure.

```csharp
public static ChapterParseResult Load(string text, string fileName, IReadOnlyDictionary<string, TerrainType> baseTerrains)
```

| Parameter | Type | Description |
| --- | --- | --- |
| text | `string` | Full markdown file contents. |
| fileName | `string` | File name stamped onto every error. |
| baseTerrains | `IReadOnlyDictionary<string, TerrainType>` | Base terrain entries extended by TERRAINS rows. |

Returns: `ChapterParseResult` — Result carrying the chapter on success or every complaint on failure.

## ClassDatabase

Minimal class database: keyed UnitClass entries for promotion and movement lookup. Hosts own the data (markdown, JSON, editor DB); the core only resolves keys.

### ClassDatabase.Add

Registers one class entry keyed by its Key, replacing any same-key entry.

```csharp
public void Add(UnitClass cls)
```

| Parameter | Type | Description |
| --- | --- | --- |
| cls | `UnitClass` | Class entry to store. |

### ClassDatabase.TryGet

Looks up one class entry by key ignoring case.

```csharp
public bool TryGet(string key, out UnitClass? cls)
```

| Parameter | Type | Description |
| --- | --- | --- |
| key | `string` | Class key to resolve. |
| cls | `out UnitClass?` | Found entry, null when absent. |

Returns: `bool` — True when the key resolves to an entry.

### ClassDatabase.Get

Resolves one class entry by key, failing fast on unknown keys.

```csharp
public UnitClass Get(string key)
```

| Parameter | Type | Description |
| --- | --- | --- |
| key | `string` | Class key to resolve. |

Returns: `UnitClass` — The matching class entry.

## Promotion

Promotion: rekey ClassKey, adopt the new class movement, apply fixed gains. GBA per-class gains are host data; callers pass them as a Stats delta.

### Promotion.CanPromote

Tests whether a unit class chain offers a resolvable promotion target.

```csharp
public static bool CanPromote(Unit unit, ClassDatabase db)
```

| Parameter | Type | Description |
| --- | --- | --- |
| unit | `Unit` | Unit whose ClassKey starts the lookup. |
| db | `ClassDatabase` | Class database resolving both ends of the chain. |

Returns: `bool` — True when the current class promotes to a known class.

### Promotion.Apply

Promotes a unit to a new class, adopting its movement and stat gains.

```csharp
public static void Apply(Unit unit, string newClassKey, ClassDatabase db, Stats? gains = null)
```

| Parameter | Type | Description |
| --- | --- | --- |
| unit | `Unit` | Unit to rekey and mutate in place. |
| newClassKey | `string` | Target class key resolved through the database. |
| db | `ClassDatabase` | Class database resolving the target entry. |
| gains | `Stats?` | Host-supplied per-class stat delta, null for none. |

## StandardTerrains

Starter terrain set with GBA-flavored defaults. Hosts can replace the set with their own data; gameplay code must only depend on TerrainType. MoveCost -1 (const Impassable) blocks that move type.

### StandardTerrains.Impassable

Move cost blocking a movement group from entering a tile.

```csharp
public const int Impassable = -1;
```

Type: `int`

### StandardTerrains.Create

Builds the GBA-flavored starter terrain set keyed by terrain id.

```csharp
public static Dictionary<string, TerrainType> Create()
```

Returns: `Dictionary<string, TerrainType>` — Fresh dictionary of plain, road, forest, fort, and blocking tiles.

## Stats

GBA-style stat block: MaxHP, Power (Str or Mag — the weapon's IsMagic flag selects Def vs Res on the target), Skill (hit, crit), Speed (attack speed, relieved by Constitution against weapon weight), Luck (hit, avoid, crit dodge), Defense, Resistance, Constitution (carry capacity and attack-speed relief). Level-ups roll each stat against GrowthRates; promotion adds fixed gains passed by the caller.

### Stats.MaxHP

Maximum hit points, also the spawn value for CurrentHP.

```csharp
public int MaxHP { get; set; }
```

Type: `int`

### Stats.Power

Strength or magic power, selected by the weapon used.

```csharp
public int Power { get; set; }
```

Type: `int`

### Stats.Skill

Accuracy and crit-rate driver for GBA combat math.

```csharp
public int Skill { get; set; }
```

Type: `int`

### Stats.Speed

Attack speed driver and GBA doubling threshold stat.

```csharp
public int Speed { get; set; }
```

Type: `int`

### Stats.Luck

Luck driver for hit, avoid, and crit evade.

```csharp
public int Luck { get; set; }
```

Type: `int`

### Stats.Defense

Physical damage reduction against non-magic weapons.

```csharp
public int Defense { get; set; }
```

Type: `int`

### Stats.Resistance

Magical damage reduction against magic weapons.

```csharp
public int Resistance { get; set; }
```

Type: `int`

### Stats.Constitution

Weight relief against weapon slowdown for attack speed.

```csharp
public int Constitution { get; set; }
```

Type: `int`

### Stats.Clone

Copies every stat field into a fresh block.

```csharp
public Stats Clone()
```

Returns: `Stats` — A new Stats with identical field values.

## Statuses

Status effects (poison/sleep/silence/...): name -> turns remaining on Unit.Statuses. Pure helpers; hosts tick at phase boundaries and gate actions (sleep/silence) themselves.

### Statuses.Apply

Applies a named status for a positive duration, refreshing any existing entry.

```csharp
public static void Apply(Unit unit, string name, int duration)
```

| Parameter | Type | Description |
| --- | --- | --- |
| unit | `Unit` | Unit whose Statuses map to mutate. |
| name | `string` | Status name, trimmed and lowercased. |
| duration | `int` | Turns remaining, must be at least 1. |

### Statuses.Has

Tests whether a live status entry remains on the unit.

```csharp
public static bool Has(Unit unit, string name)
```

| Parameter | Type | Description |
| --- | --- | --- |
| unit | `Unit` | Unit whose Statuses map to read. |
| name | `string` | Status name, trimmed and lowercased. |

Returns: `bool` — True when the entry exists with turns remaining above zero.

### Statuses.IsSilenced

True when unit is silenced (blocks magic and staff).

```csharp
public static bool IsSilenced(Unit unit)
```

| Parameter | Type | Description |
| --- | --- | --- |
| unit | `Unit` | Unit to test. |

Returns: `bool` — True when silenced.

### Statuses.Clear

Removes one status entry regardless of remaining duration.

```csharp
public static bool Clear(Unit unit, string name)
```

| Parameter | Type | Description |
| --- | --- | --- |
| unit | `Unit` | Unit whose Statuses map to mutate. |
| name | `string` | Status name, trimmed and lowercased. |

Returns: `bool` — True when an entry was present and removed.

### Statuses.Tick

Decrement all durations by 1; expired entries are removed. Returns expired names in sorted order.

```csharp
public static List<string> Tick(Unit unit)
```

| Parameter | Type | Description |
| --- | --- | --- |
| unit | `Unit` | Unit whose Statuses map to tick. |

Returns: `List<string>` — Names that reached zero and were removed.

## TerrainEffects

Per-round terrain HP swing (fort heal, poison/marsh damage). Pure: returns the applied delta and mutates CurrentHP, clamped to [0, MaxHP]. Callers decide timing (once per round per team, at phase boundaries — not per activation). Nonzero percents always move at least 1 HP (ceiling rounding).

### TerrainEffects.Apply

Applies terrain HP swing to one unit, clamped at zero.

```csharp
public static int Apply(Unit unit, TerrainType terrain)
```

| Parameter | Type | Description |
| --- | --- | --- |
| unit | `Unit` | The unit standing on the tile (mutates CurrentHP). |
| terrain | `TerrainType` | The tile terrain definition carrying HpEffectPercent. |

Returns: `int` — Signed HP delta applied, zero when no effect or the unit is dead.

## InventoryEntry

A weapon carried by a unit, with remaining durability (UsesLeft starts at MaxUses; IsBroken at zero, and broken entries cannot strike).

### InventoryEntry.Weapon

Shared weapon definition for this carried item.

```csharp
public Weapon Weapon { get; }
```

Type: `Weapon`

### InventoryEntry.UsesLeft

Remaining durability, decremented per strike attempt.

```csharp
public int UsesLeft { get; set; }
```

Type: `int`

### InventoryEntry constructor

Carries one weapon with remaining durability defaulting to full.

```csharp
public InventoryEntry(Weapon weapon, int? usesLeft = null)
```

| Parameter | Type | Description |
| --- | --- | --- |
| weapon | `Weapon` | Shared weapon definition to carry. |
| usesLeft | `int?` | Remaining uses, defaults to the weapon maximum. |

### InventoryEntry.IsBroken

True when no durability remains.

```csharp
public bool IsBroken => UsesLeft <= 0;
```

Type: `bool`

## Unit

Battlefield unit: identity (Nid, Name, Side, ClassKey), growth state (Level, Experience toward 100 per level, Stats), live HP (CurrentHP, IsAlive while above zero), movement (MovePoints, MovementGroup), Vision fog radius, and collections: Inventory entries with durability, Skill ids, Statuses (name to turns left), Wexp per weapon type, CarryingNid ("" when hands-free). The constructor takes nid, name, side, class key, and stats (HP starts at MaxHP, Level at 1); Clone re-keys copies for reinforcements, staging, and saves.

### Unit.Nid

Unique id used by events, conditions, and spawn references.

```csharp
public string Nid { get; }
```

Type: `string`

### Unit.Name

Display name for dialogue and menus.

```csharp
public string Name { get; set; }
```

Type: `string`

### Unit.Side

Which army this unit fights for.

```csharp
public Affiliation Side { get; set; }
```

Type: `Affiliation`

### Unit.ClassKey

Class database key, rekeyed by promotion.

```csharp
public string ClassKey { get; set; }
```

Type: `string`

### Unit.SpriteKey

Per-unit art override key into the host pack sprite library ("" = class art). Display-only data like Name: rules never read it; hosts resolve it.

```csharp
public string SpriteKey { get; set; }
```

Type: `string`

### Unit.Level

Current level driving experience gain math.

```csharp
public int Level { get; set; }
```

Type: `int`

### Unit.Experience

Accumulated experience toward the next level at 100.

```csharp
public int Experience { get; set; }
```

Type: `int`

### Unit.Stats

Live stat block mutated by level-ups and promotion gains.

```csharp
public Stats Stats { get; set; }
```

Type: `Stats`

### Unit.CurrentHP

Current hit points, dead at zero.

```csharp
public int CurrentHP { get; set; }
```

Type: `int`

### Unit.MovePoints

Personal movement allowance in tiles (default 5). Promotion adopts the new class move; Rescue halves it while laden.

```csharp
public int MovePoints { get; set; }
```

Type: `int`

### Unit.MovementGroup

Movement group key into terrain costs (default infantry).

```csharp
public string MovementGroup { get; set; }
```

Type: `string`

### Unit.Vision

Fog sight radius in Manhattan tiles (default 5, GBA-ish).

```csharp
public int Vision { get; set; }
```

Type: `int`

### Unit.Skills

Skill ids on this unit (see Combat.SkillCatalog for effects).

```csharp
public List<string> Skills { get; }
```

Type: `List<string>`

### Unit.Statuses

Active status effects: name -> turns remaining (poison/sleep/silence/...).

```csharp
public Dictionary<string, int> Statuses { get; }
```

Type: `Dictionary<string, int>`

### Unit.CarryingNid

Nid of the rescued passenger being carried ("" = none).

```csharp
public string CarryingNid { get; set; }
```

Type: `string`

### Unit.AiType

AI behavior type for enemy units (rush, hold, patrol).

```csharp
public string AiType { get; set; }
```

Type: `string`

### Unit.PatrolA

Patrol waypoint A for patrol AI (ignored otherwise).

```csharp
public Grid.GridPoint PatrolA { get; set; }
```

Type: `Grid.GridPoint`

### Unit.PatrolB

Patrol waypoint B for patrol AI (ignored otherwise).

```csharp
public Grid.GridPoint PatrolB { get; set; }
```

Type: `Grid.GridPoint`

### Unit.Wexp

Weapon experience per type (see Data.WeaponRanks for thresholds).

```csharp
public Dictionary<WeaponType, int> Wexp { get; }
```

Type: `Dictionary<WeaponType, int>`

### Unit.Inventory

Carried weapons with per-entry durability.

```csharp
public IList<InventoryEntry> Inventory => _inventory;
```

Type: `IList<InventoryEntry>`

### Unit.IsAlive

True while CurrentHP is above zero.

```csharp
public bool IsAlive => CurrentHP > 0;
```

Type: `bool`

### Unit.Clone

Copy for reinforcements/staging; optionally re-keyed (nids must stay unique on a map).

```csharp
public Unit Clone(string? newNid = null)
```

| Parameter | Type | Description |
| --- | --- | --- |
| newNid | `string?` | Replacement nid, defaults to keeping the source nid. |

Returns: `Unit` — A deep copy with matching stats, inventory, wexp, skills, and statuses.

### Unit constructor

Creates a unit at full health from a stat block.

```csharp
public Unit(string nid, string name, Affiliation side, string classKey, Stats stats)
```

| Parameter | Type | Description |
| --- | --- | --- |
| nid | `string` | Unique id used by events and conditions. |
| name | `string` | Display name for dialogue and menus. |
| side | `Affiliation` | Which army this unit fights for. |
| classKey | `string` | Class database key. |
| stats | `Stats` | Stat block the unit shares by reference until cloned. |

## UnitClass

Class definition (database entry). Values: Key id, Name, MoveType family for terrain costs, Move base points, and optional PromotesTo target key.

### UnitClass.Key

Database key used by Unit.ClassKey and chapter class fields.

```csharp
public string Key { get; }
```

Type: `string`

### UnitClass.Name

Display name for menus and promotion screens.

```csharp
public string Name { get; }
```

Type: `string`

### UnitClass.MoveType

Movement family resolving terrain costs via MovementGroups.

```csharp
public MoveType MoveType { get; }
```

Type: `MoveType`

### UnitClass.Move

Base movement points adopted on promotion.

```csharp
public int Move { get; }
```

Type: `int`

### UnitClass.PromotesTo

Promotion target class key, null when this class cannot promote.

```csharp
public string? PromotesTo { get; }
```

Type: `string?`

### UnitClass constructor

Defines one class database entry with movement and promotion target.

```csharp
public UnitClass(string key, string name, MoveType moveType, int move, string? promotesTo = null)
```

| Parameter | Type | Description |
| --- | --- | --- |
| key | `string` | Database key other data references. |
| name | `string` | Display name for menus. |
| moveType | `MoveType` | Movement family for terrain costs. |
| move | `int` | Base movement points. |
| promotesTo | `string?` | Promotion target class key, null for no promotion. |

## Weapon

Weapon definition (database entry): Key, Name, Type, Rank (E-S gate via WeaponRanks), combat numbers (Might, Hit, Crit, Weight, MinRange/MaxRange in Manhattan tiles, MaxUses durability), and flags: IsMagic (targets Res), EffectiveAgainst class keys, HealPower and StatusEffect/StatusDuration for staves. Instances with durability live on Unit as InventoryEntry values; InRange tests a distance, IsStaff and IsEffectiveAgainst read the flags.

### Weapon.Key

Database key referenced by chapter armory and unit items lists.

```csharp
public string Key { get; }
```

Type: `string`

### Weapon.Name

Display name for menus and inventory screens.

```csharp
public string Name { get; }
```

Type: `string`

### Weapon.Type

Damage family driving triangle bonus and magic selection.

```csharp
public WeaponType Type { get; }
```

Type: `WeaponType`

### Weapon.Rank

Minimum rank letter required to equip this weapon.

```csharp
public WeaponRank Rank { get; }
```

Type: `WeaponRank`

### Weapon.Might

Base damage added before triangle and effectiveness bonuses.

```csharp
public int Might { get; }
```

Type: `int`

### Weapon.Hit

Base accuracy before skill, luck, and terrain avoid.

```csharp
public int Hit { get; }
```

Type: `int`

### Weapon.Crit

Base critical rate before skill bonus.

```csharp
public int Crit { get; }
```

Type: `int`

### Weapon.Weight

Heft slowing attack speed after constitution relief.

```csharp
public int Weight { get; }
```

Type: `int`

### Weapon.MinRange

Minimum Manhattan attack distance.

```csharp
public int MinRange { get; }
```

Type: `int`

### Weapon.MaxRange

Maximum Manhattan attack distance.

```csharp
public int MaxRange { get; }
```

Type: `int`

### Weapon.MaxUses

Full durability for fresh inventory entries.

```csharp
public int MaxUses { get; }
```

Type: `int`

### Weapon.IsMagic

True when damage targets resistance instead of defense.

```csharp
public bool IsMagic { get; }
```

Type: `bool`

### Weapon.CanCounter

True when this weapon can be used to counterattack (default true; set false for long-range siege or pacifist staves).

```csharp
public bool CanCounter { get; set; }
```

Type: `bool`

### Weapon.EffectiveAgainst

Class keys or tags this weapon is effective against (armor, flier, ...). Empty = none.

```csharp
public List<string> EffectiveAgainst { get; }
```

Type: `List<string>`

### Weapon.HealPower

Staff heal power (>0 = healing staff, uses Might-style base). 0 = damaging weapon.

```csharp
public int HealPower { get; set; }
```

Type: `int`

### Weapon.StatusEffect

Status staff effect name (sleep/silence/...) applied on hit. "" = none.

```csharp
public string StatusEffect { get; set; }
```

Type: `string`

### Weapon.StatusDuration

Duration in turns for StatusEffect.

```csharp
public int StatusDuration { get; set; }
```

Type: `int`

### Weapon.CanSteal

True when this weapon can be stolen (light items; heavy/legendary set false).

```csharp
public bool CanSteal { get; set; }
```

Type: `bool`

### Weapon.ReversesTriangle

True when this weapon reverses weapon-triangle advantage (reaver weapons, default false).

```csharp
public bool ReversesTriangle { get; set; }
```

Type: `bool`

### Weapon.Price

Shop price in gold. 0 = not for sale (spawn pool only).

```csharp
public int Price { get; set; }
```

Type: `int`

### Weapon.IsStaff

True for staff type or any healing or status payload (legacy broad check).

```csharp
public bool IsStaff => Type == WeaponType.Staff || HealPower > 0 || StatusEffect.Length > 0;
```

Type: `bool`

### Weapon.IsHealStaff

True when HealPower is positive (healing staff payload).

```csharp
public bool IsHealStaff => HealPower > 0;
```

Type: `bool`

### Weapon.IsStatusStaff

True when StatusEffect is set (status staff payload).

```csharp
public bool IsStatusStaff => StatusEffect.Length > 0;
```

Type: `bool`

### Weapon constructor

Defines one weapon database entry with combat and durability stats.

```csharp
public Weapon(string key, string name, WeaponType type, WeaponRank rank, int might, int hit, int crit, int weight, int minRange, int maxRange, int maxUses, bool isMagic = false)
```

| Parameter | Type | Description |
| --- | --- | --- |
| key | `string` | Database key referenced by armory rows. |
| name | `string` | Display name for menus. |
| type | `WeaponType` | Damage family driving triangle and magic selection. |
| rank | `WeaponRank` | Minimum rank letter required to equip. |
| might | `int` | Base damage. |
| hit | `int` | Base accuracy. |
| crit | `int` | Base critical rate. |
| weight | `int` | Heft slowing attack speed. |
| minRange | `int` | Minimum Manhattan attack distance. |
| maxRange | `int` | Maximum Manhattan attack distance. |
| maxUses | `int` | Full durability for fresh entries. |
| isMagic | `bool` | True when damage targets resistance. |

### Weapon.InRange

Tests whether a Manhattan distance falls inside the weapon range.

```csharp
public bool InRange(int distance)
```

| Parameter | Type | Description |
| --- | --- | --- |
| distance | `int` | Manhattan distance between attacker and target. |

Returns: `bool` — True when distance sits between MinRange and MaxRange inclusive.

### Weapon.CanCounterAt

Tests whether this weapon can counterattack at a distance.

```csharp
public bool CanCounterAt(int distance)
```

| Parameter | Type | Description |
| --- | --- | --- |
| distance | `int` | Manhattan distance of the exchange. |

Returns: `bool` — True when countering is enabled and distance is in range.

### Weapon.IsEffectiveAgainst

Tests effectiveness bonus against a defender class key.

```csharp
public bool IsEffectiveAgainst(string? defenderClassKey)
```

| Parameter | Type | Description |
| --- | --- | --- |
| defenderClassKey | `string?` | Defender class key or tag, null never matches. |

Returns: `bool` — True when the key matches an EffectiveAgainst entry ignoring case.

## WeaponRanks

Weapon experience (GBA-flavored fork): each combat use banks points per weapon type on the wielder; rank letters gate equipping (a unit needs the letter rank for the weapon it holds). Thresholds are data here, not scattered constants. Fork thresholds are E 0, D 20, C 50, B 80, A 120, S 180; GBA FE7/8 uses 0, 31, 71, 121, 181, 251 (S at 251). Wexp is capped at 255 (byte max) to match GBA and prevent overflow. Deterministic: pure functions over ints, no RNG.

### WeaponRanks.PointsForRank

Reads the wexp floor required for a rank letter.

```csharp
public static int PointsForRank(WeaponRank rank)
```

| Parameter | Type | Description |
| --- | --- | --- |
| rank | `WeaponRank` | Rank letter to look up. |

Returns: `int` — Wexp points needed to reach a rank (E 0, D 20, C 50, B 80, A 120, S 180).

### WeaponRanks.RankFor

Reads the highest rank letter unlocked by a wexp total.

```csharp
public static WeaponRank RankFor(int points)
```

| Parameter | Type | Description |
| --- | --- | --- |
| points | `int` | Banked wexp total, negatives read as E. |

Returns: `WeaponRank` — Highest rank whose threshold points meet or beat (negatives read as E).

### WeaponRanks.CanEquip

Tests whether a wexp total unlocks a required rank.

```csharp
public static bool CanEquip(int points, WeaponRank required)
```

| Parameter | Type | Description |
| --- | --- | --- |
| points | `int` | Banked wexp total. |
| required | `WeaponRank` | Rank letter the weapon demands. |

Returns: `bool` — True when points meet the required rank's threshold.

### WeaponRanks.PointsOf

Current wexp for a type on a unit (0 when never used).

```csharp
public static int PointsOf(Unit unit, WeaponType type)
```

| Parameter | Type | Description |
| --- | --- | --- |
| unit | `Unit` | Unit whose Wexp map to read. |
| type | `WeaponType` | Weapon family to look up. |

Returns: `int` — Banked points for the type, or zero.

### WeaponRanks.AddWexp

Banks wexp on the unit, capped at 255 (GBA byte max); returns the new total for the type.

```csharp
public static int AddWexp(Unit unit, WeaponType type, int points)
```

| Parameter | Type | Description |
| --- | --- | --- |
| unit | `Unit` | Unit whose Wexp map to mutate. |
| type | `WeaponType` | Weapon family to credit. |
| points | `int` | Nonnegative points to add. |

Returns: `int` — New total for the type after banking, clamped to 255.
