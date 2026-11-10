# Data

## Stats

GBA-style block: `MaxHP`, `Power` (covers Str or Mag — the weapon's `IsMagic` flag
selects Def vs Res on the target), `Skill`, `Speed`, `Luck`, `Defense`, `Resistance`,
`Constitution` (weapon-weight relief for attack speed).

## Classes, weapons, units

- [`UnitClass`](api-data.md#unitclass) — key, name, `MoveType`, move points, optional promotion target.
- [`Weapon`](api-data.md#weapon) — key, name, type, rank (E–S), might, hit, crit, weight, min/max range
  (Manhattan), max uses (durability), magic flag, `EffectiveAgainst` class tags,
  `HealPower` for staves. `InRange(distance)` tests a shot; `IsStaff` is true for
  staff type, positive heal power, or any status effect.
- [`WeaponType`](api-data.md#weapontype) — Sword, Lance, Axe, Bow, Anima, Light, Dark, Staff.
- [`InventoryEntry`](api-data.md#inventoryentry) — a weapon plus remaining uses; `IsBroken` at zero. Durability is
  consumed by the entry overload of [`CombatResolver.Resolve`](api-combat.md#combatresolver-resolve) (1 use per strike attempt).
- [`Unit`](api-data.md#unit) — `Nid` (unique id used by events), name, side, class key, level, XP, current
  `Stats`, live HP, inventory, `Vision` (fog sight, default 5), `Skills` ids,
  `Statuses` (name → turns left), `CarryingNid` (rescue). [`Clone`](api-data.md#unit-clone) copies a unit for reinforcements.
- [`Affiliation`](api-data.md#affiliation) — Player, Enemy, Ally (NPC, own phase), Other (neutral, fights nobody).

```csharp
// Build the pieces hosts own: units, weapons, classes, growths.
var sword = new Weapon("iron-sword", "Iron Sword", WeaponType.Sword, WeaponRank.E,
    might: 5, hit: 90, crit: 0, weight: 5, minRange: 1, maxRange: 1, maxUses: 40);
var eirika = new Unit("eirika", "Eirika", Affiliation.Player, "lord",
    new Stats { MaxHP = 18, Power = 4, Skill = 7, Speed = 8, Luck = 5,
                Defense = 3, Resistance = 2, Constitution = 5 });
eirika.Inventory.Add(new InventoryEntry(sword));
eirika.Skills.Add("brave-hit");          // SkillCatalog id, see combat
Statuses.Apply(eirika, "poison", 2);     // name -> turns left
WeaponRanks.AddWexp(eirika, WeaponType.Sword, 1);

var classes = new ClassDatabase();
classes.Add(new UnitClass("lord", "Lord", MoveType.Infantry, move: 6));
if (Promotion.CanPromote(eirika, classes))
    Promotion.Apply(eirika, "paladin", classes,
        gains: new Stats { MaxHP = 2, Power = 1 }); // per-class gains are host data
```

`ClassDatabase` maps class keys to [`UnitClass`](api-data.md#unitclass) entries; see [Promotion](data.md#promotion) below.

## Terrain set and effects

`StandardTerrains.Create()` returns GBA-flavored starter terrain (plain, road, bridge,
forest, fort, village, gate, throne, mountain, water, wall). Forts heal 10% per round.
Gameplay code depends only on `TerrainType`, so hosts can replace the set with
their own data.

[`TerrainEffects.Apply(unit, terrain)`](api-data.md#terraineffects-apply) applies the HP swing with ceiling rounding (any
nonzero percent moves at least 1 HP), clamped to `[0, MaxHP]`. Callers decide timing;
the convention is once per round per team at phase boundaries, not per activation.

## Chapters

[`Deployment`](api-data.md#deployment) binds a unit to a tile, a spawn turn (default 1), and an optional group
id. [`ObjectiveSpec`](api-data.md#objectivespec) factories read like design docs:

```csharp
ObjectiveSpec.RoutEnemies()   // destroy every live Enemy unit
ObjectiveSpec.AllDead()       // defeat when no live Player unit remains
ObjectiveSpec.Survive(8)      // victory after completing 8 full rounds
ObjectiveSpec.Protect("eirika", "tana")
ObjectiveSpec.Seize("eirika", "gate")  // live eirika stands on region "gate"
ObjectiveSpec.EventOnly()     // win/lose commands decide; never auto-evaluated
```

[`Chapter`](api-data.md#chapter) bundles nid, title, board, deployments, regions, events, victory/defeat
specs, an armory (weapons by key for `give_item`), unit templates (for `spawn_unit`),
`MaxDeploy` (0 = everyone), formation region, and the next-chapter link.

[`Chapter.Validate()`](api-data.md#chapter-validate) returns a list of authoring errors — off-board tiles, bad spawn
turns, duplicate nids, over-deployment, unknown event verbs, unparsable conditions, and
dangling protect tags — for the future editor to surface. A clean chapter validates to
an empty list. At runtime, `Tactics.DeploymentSelector.ChooseDeployments` resolves a
player-picked roster against the same deployments (`MaxDeploy`, formation regions)
and reports selection errors without throwing.

## Statuses

[`Data.Statuses`](api-data.md#statuses) manages `Unit.Statuses`: [`Apply`](api-data.md#statuses-apply), [`Has`](api-data.md#statuses-has), [`Clear`](api-data.md#statuses-clear),
and [`Tick`](api-data.md#statuses-tick) (decrement all, drop expired, return expired names). Hosts tick at phase
boundaries via [`Tactics.PhaseTick`](api-tactics.md#phasetick) (which also deals poison damage and terrain
swings), gate actions for sleep/silence themselves. Status staves apply through [`ResolveStaff`](api-combat.md#combatresolver-resolvestaff).

## Experience

[`Tactics.Experience`](api-tactics.md#experience) is GBA-flavored and pure: [`Gain(kill, atkLvl, defLvl, damage)`](api-tactics.md#experience-gain)
pays a chip base (10) or kill base (30) plus a capped level-difference bonus, plus
(on non-kills only) a damage-based bonus of `min(damageDealt, 20) / 4`;
[`AddExp`](api-tactics.md#experience-addexp) banks toward 100 and calls [`LevelUp`](api-tactics.md#experience-levelup) on overflow. `LevelUp` rolls 1RN per
stat against [`GrowthRates`](api-tactics.md#growthrates) on the growth RNG stream and heals MaxHP gains into
`CurrentHP`. Tunables live in `ExperienceRules`. After combat, hosts grant through
[`Tactics.CombatGains.ApplyAfterCombat`](api-tactics.md#combatgains-applyaftercombat), which returns `(expGained, levelsGained)`.

## Weapon experience

[`Data.WeaponRanks`](api-data.md#weaponranks) banks per-type wexp on `Unit.Wexp` (a `WeaponType → points`
map, copied by `Clone` and saved/restored by [`SaveSystem`](api-tactics.md#savesystem)): thresholds E 0,
D 20, C 50, B 80, A 120, S 180. [`RankFor(points)`](api-data.md#weaponranks-rankfor) reads the letter,
[`PointsForRank(rank)`](api-data.md#weaponranks-pointsforrank) the floor, [`CanEquip(points, rank)`](api-data.md#weaponranks-canequip) gates equipping, and
[`AddWexp(unit, type, points)`](api-data.md#weaponranks-addwexp) banks (returning the new total). Hosts grant
through `CombatGains.ApplyAfterCombat`'s trailing `usedWeapon`/`wexpOnUse`
(default 1)/`wexpOnKill` (default 3) — pass the fought weapon and the attacker
banks 1 per use, 3 per kill.

## Promotion

[`ClassDatabase`](api-data.md#classdatabase) maps class keys to `UnitClass` entries (move type, move points,
promotion target). [`Promotion.CanPromote`](api-data.md#promotion-canpromote) checks the chain; [`Promotion.Apply`](api-data.md#promotion-apply)
rekeys `ClassKey`, adopts the new movement, and applies caller-supplied `Stats`
gains (per-class GBA gains are host data). The `promote;unit;class` event verb
rekeys only — full stat merges need the database on the host side.
