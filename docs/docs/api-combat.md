# API: SrpgCore.Combat

> Generated from `../SrpgCore` sources by `scripts/build-api.ts` — do not edit by hand.

Guide: [combat](combat.md).

## HitMode

Hit-roll models, per-difficulty (default TRUE_HIT).

| Name | Value | Description |
| --- | --- | --- |
| Classic | 0 | Single roll with true odds. |
| TrueHit | 1 | Mean of two rolls for GBA feel. |
| TrueHitPlus | 2 | Mean of three rolls, tighter around the displayed rate. |
| Fates | 3 | Single roll against a Fates-adjusted threshold. |

## CombatRules

Tunable battle formula constants (GBA FE7/8 defaults).

### CombatRules.TriangleMightBonus

Might added per triangle advantage step (GBA default 1).

```csharp
public int TriangleMightBonus { get; set; }
```

Type: `int`

### CombatRules.TriangleHitBonus

Hit added per triangle advantage step (GBA default 15).

```csharp
public int TriangleHitBonus { get; set; }
```

Type: `int`

### CombatRules.DoubleAttackSpeedThreshold

Attack-speed lead needed to strike twice (GBA default 4).

```csharp
public int DoubleAttackSpeedThreshold { get; set; }
```

Type: `int`

### CombatRules.HitMode

Hit-roll model used for strikes (default TrueHit).

```csharp
public HitMode HitMode { get; set; }
```

Type: `HitMode`

### CombatRules.EffectiveMultiplier

Damage multiplier for effective weapons (GBA x3).

```csharp
public int EffectiveMultiplier { get; set; }
```

Type: `int`

### CombatRules.TorchVisionBonus

Vision bonus granted by the Torch staff status (GBA default 2).

```csharp
public int TorchVisionBonus { get; set; }
```

Type: `int`

## CombatForecast

Battle-forecast numbers shown to the player before committing. Values: Damage per landed hit, Hit and Crit rates 0-100, AttackCount (1 or 2 when doubling), TriangleAdvantage (-1/0/1), and IsEffective flag.

### CombatForecast.Damage

Predicted damage per landed hit after defense and terrain.

```csharp
public int Damage { get; set; }
```

Type: `int`

### CombatForecast.Hit

Predicted hit rate from 0 to 100 after skills and support.

```csharp
public int Hit { get; set; }
```

Type: `int`

### CombatForecast.Crit

Predicted crit rate from 0 to 100 after skills.

```csharp
public int Crit { get; set; }
```

Type: `int`

### CombatForecast.AttackCount

Number of attacker strikes, 1 normally or 2 when doubling.

```csharp
public int AttackCount { get; set; }
```

Type: `int`

### CombatForecast.TriangleAdvantage

Triangle result from the attacker side: 1, 0, or -1.

```csharp
public int TriangleAdvantage { get; set; }
```

Type: `int`

### CombatForecast.IsEffective

True when the attacking weapon is effective against the defender class.

```csharp
public bool IsEffective { get; set; }
```

Type: `bool`

## CombatStrike

One rolled strike inside a resolved exchange. Values: IsAttackerStrike (true = attacker swing, false = counter), HitLanded, WasCrit (triple damage), and DamageDealt (0 on miss).

### CombatStrike.IsAttackerStrike

True for attacker strikes, false for defender counters.

```csharp
public bool IsAttackerStrike { get; set; }
```

Type: `bool`

### CombatStrike.HitLanded

True when the hit roll landed.

```csharp
public bool HitLanded { get; set; }
```

Type: `bool`

### CombatStrike.WasCrit

True when the landed hit rolled a crit for triple damage.

```csharp
public bool WasCrit { get; set; }
```

Type: `bool`

### CombatStrike.DamageDealt

Damage applied by this strike, zero on a miss.

```csharp
public int DamageDealt { get; set; }
```

Type: `int`

## CombatResult

Full strike log of a resolved exchange. Values: ordered Strikes list (attacker strikes with counter between them) and AttackerHPLeft/DefenderHPLeft clamped at zero; the same HP is written back to the combatants.

### CombatResult.Strikes

Ordered strike log: attacker strikes with the counter between them.

```csharp
public List<CombatStrike> Strikes { get; }
```

Type: `List<CombatStrike>`

### CombatResult.AttackerHPLeft

Attacker HP after the exchange, clamped at zero.

```csharp
public int AttackerHPLeft { get; set; }
```

Type: `int`

### CombatResult.DefenderHPLeft

Defender HP after the exchange, clamped at zero.

```csharp
public int DefenderHPLeft { get; set; }
```

Type: `int`

## CombatResolver

Pure combat math. Forecast() feeds the UI; Resolve() consumes RNG and mutates HP. Same inputs + same seed always give the same result (AI, forecasts, replays share it). Durability is consumed by the InventoryEntry overload of Resolve(); the Weapon overload stays pure for forecasts/tests. Healing/status staves use ForecastHeal, ForecastStaffHit and ResolveStaff (1RN); flat skill mods via SkillCatalog. Post-strike unit actions live in Tactics: Dance (adjacent refresh, host-tracked acted flags) and Canto (leftover-move repositioning).

### CombatResolver.AttackSpeed

Attack speed: Speed minus encumbrance from excess weapon weight.

```csharp
public static int AttackSpeed(Unit unit, Weapon weapon)
```

| Parameter | Type | Description |
| --- | --- | --- |
| unit | `Unit` | The unit whose Speed and Constitution apply. |
| weapon | `Weapon` | The weapon whose Weight may slow the unit. |

Returns: `int` — Effective attack speed used for hit and doubling.

### CombatResolver.TerrainAt

Terrain avoid/defense at a tile (single lookup for callers).

```csharp
public static void TerrainAt(Grid.Board board, Grid.GridPoint pos, out int avoid, out int defense)
```

| Parameter | Type | Description |
| --- | --- | --- |
| board | `Grid.Board` | The board holding the terrain table. |
| pos | `Grid.GridPoint` | The tile to look up. |
| avoid | `out int` | The terrain avoid at the tile. |
| defense | `out int` | The terrain defense at the tile. |

### CombatResolver.ComputeDamage

Damage per landed hit: attack stat plus might and triangle, minus defense and terrain.

```csharp
public static int ComputeDamage(Unit attacker, Weapon attackWeapon, Unit defender, int defenderTerrainDefense, int triangle, bool effective, CombatRules? rules = null)
```

| Parameter | Type | Description |
| --- | --- | --- |
| attacker | `Unit` | The attacking unit supplying the attack stat. |
| attackWeapon | `Weapon` | The attacking weapon supplying might and magic selection. |
| defender | `Unit` | The defending unit supplying defense or resistance and class key. |
| defenderTerrainDefense | `int` | Defense from the defender tile subtracted from damage. |
| triangle | `int` | Triangle advantage from the attacker side. |
| effective | `bool` | True when the weapon is effective against the defender class. |
| rules | `CombatRules?` | Formula tunables, or null for GBA defaults. |

Returns: `int` — Clamped damage per landed hit.

### CombatResolver.ComputeHitRate

Hit rate: weapon hit plus skill and luck and triangle, minus defender avoid.

```csharp
public static int ComputeHitRate(Unit attacker, Weapon attackWeapon, Unit defender, Weapon? defendWeapon, int defenderTerrainAvoid, CombatRules? rules = null)
```

| Parameter | Type | Description |
| --- | --- | --- |
| attacker | `Unit` | The attacking unit supplying skill and luck. |
| attackWeapon | `Weapon` | The attacking weapon supplying base hit. |
| defender | `Unit` | The defending unit supplying avoid inputs. |
| defendWeapon | `Weapon?` | The defending weapon for triangle and counter AS, or null when disarmed. |
| defenderTerrainAvoid | `int` | Avoid from the defender tile added to defender avoid. |
| rules | `CombatRules?` | Formula tunables, or null for GBA defaults. |

Returns: `int` — Clamped hit rate from 0 to 100.

### CombatResolver.ComputeAvoidRate

Avoid total: attack speed plus terrain avoid plus half luck.

```csharp
public static int ComputeAvoidRate(Unit defender, Weapon? defendWeapon, Weapon incomingWeapon, int defenderTerrainAvoid)
```

| Parameter | Type | Description |
| --- | --- | --- |
| defender | `Unit` | The defending unit supplying speed and luck. |
| defendWeapon | `Weapon?` | The defending weapon for attack speed, or null to use the incoming weapon. |
| incomingWeapon | `Weapon` | The incoming weapon used for attack speed when disarmed. |
| defenderTerrainAvoid | `int` | Avoid from the defender tile. |

Returns: `int` — Unclamped avoid total.

### CombatResolver.ComputeCritRate

Crit rate: half skill plus weapon crit minus defender luck.

```csharp
public static int ComputeCritRate(Unit attacker, Weapon attackWeapon, Unit defender)
```

| Parameter | Type | Description |
| --- | --- | --- |
| attacker | `Unit` | The attacking unit supplying skill. |
| attackWeapon | `Weapon` | The attacking weapon supplying base crit. |
| defender | `Unit` | The defending unit supplying luck. |

Returns: `int` — Clamped crit rate from 0 to 100.

### CombatResolver.ComputeAttackCount

Strike count: 2 when the attacker doubles, else 1.

```csharp
public static int ComputeAttackCount(Unit attacker, Weapon attackWeapon, Unit defender, Weapon? defendWeapon, CombatRules? rules = null)
```

| Parameter | Type | Description |
| --- | --- | --- |
| attacker | `Unit` | The attacking unit supplying attack speed. |
| attackWeapon | `Weapon` | The attacking weapon supplying weight. |
| defender | `Unit` | The defending unit supplying attack speed. |
| defendWeapon | `Weapon?` | The defending weapon for speed, or null when disarmed. |
| rules | `CombatRules?` | Formula tunables, or null for GBA defaults. |

Returns: `int` — Number of attacker strikes, 1 or 2.

### CombatResolver.Forecast

Pure GBA-style forecast: damage, hit, crit, strikes, triangle, effectiveness.

```csharp
public static CombatForecast Forecast(Unit attacker, Weapon attackWeapon, Unit defender, Weapon? defendWeapon, int attackerTerrainAvoid, int attackerTerrainDefense, int defenderTerrainAvoid, int defenderTerrainDefense, CombatRules? rules = null, IList<string>? attackerSkills = null, IList<string>? defenderSkills = null, int supportHit = 0, int supportAvoid = 0, SeededRng? procRng = null, IReadOnlyDictionary<string, SkillDef>? skillCatalog = null)
```

| Parameter | Type | Description |
| --- | --- | --- |
| attacker | `Unit` | The attacking unit supplying stats and HP base. |
| attackWeapon | `Weapon` | The attacking weapon supplying might, hit, crit, and weight. |
| defender | `Unit` | The defending unit supplying defensive stats. |
| defendWeapon | `Weapon?` | The defending weapon for triangle and counter AS, or null when disarmed. |
| attackerTerrainAvoid | `int` | Avoid from the attacker tile (display only). |
| attackerTerrainDefense | `int` | Defense from the attacker tile (display only). |
| defenderTerrainAvoid | `int` | Avoid from the defender tile added to defender avoid. |
| defenderTerrainDefense | `int` | Defense from the defender tile subtracted from damage. |
| rules | `CombatRules?` | Formula tunables, or null for GBA defaults. |
| attackerSkills | `IList<string>?` | Attacker skill ids applied as flat preview mods. |
| defenderSkills | `IList<string>?` | Defender skill ids applied as flat preview mods. |
| supportHit | `int` | Bonded hit bonus added to the attacker forecast. |
| supportAvoid | `int` | Bonded avoid bonus subtracted as extra defender avoid. |
| procRng | `SeededRng?` | Seeded stream for skill proc rolls, or null to preview without procs. |
| skillCatalog | `IReadOnlyDictionary<string, SkillDef>?` | Explicit skill catalog, or null to use global static catalog. |

Returns: `CombatForecast` — A new forecast copy with skill and support mods applied.

### CombatResolver.ForecastHeal

Staff heal preview: HealPower plus half Power, clamped to missing HP.

```csharp
public static int ForecastHeal(Unit healer, Weapon staff)
```

| Parameter | Type | Description |
| --- | --- | --- |
| healer | `Unit` | The healing unit supplying Power. |
| staff | `Weapon` | The staff supplying HealPower. |

Returns: `int` — Unclamped heal amount before the MaxHP cap.

### CombatResolver.ForecastStaffHit

Staff accuracy at a distance: weapon hit + Skl*2 + Lck/2 - 5 per tile.

```csharp
public static int ForecastStaffHit(Unit healer, Weapon staff, int distance)
```

| Parameter | Type | Description |
| --- | --- | --- |
| healer | `Unit` | The healing unit supplying Skill and Luck. |
| staff | `Weapon` | The staff supplying base hit. |
| distance | `int` | Distance in tiles, costing 5 hit each. |

Returns: `int` — Clamped staff hit rate from 0 to 100.

### CombatResolver.ResolveHeal

Apply a staff heal: consumes 1 use, clamps to MaxHP, returns HP restored.

```csharp
public static int ResolveHeal(Unit healer, InventoryEntry staffEntry, Unit patient)
```

| Parameter | Type | Description |
| --- | --- | --- |
| healer | `Unit` | The healing unit supplying Power. |
| staffEntry | `InventoryEntry` | The staff inventory entry losing one use. |
| patient | `Unit` | The unit receiving the heal up to MaxHP. |

Returns: `int` — HP actually restored after clamping.

### CombatResolver.ResolveStaff

Staff use with accuracy: consumes 1 use, rolls 1RN vs ForecastStaffHit; heal staves restore on hit, status staves apply StatusEffect/StatusDuration on hit. Returns true on hit. Staves always use 1RN (GBA shape), ignoring CombatRules.HitMode.

```csharp
public static bool ResolveStaff(Unit healer, InventoryEntry staffEntry, Unit target, int distance, SeededRng rng)
```

| Parameter | Type | Description |
| --- | --- | --- |
| healer | `Unit` | The staff user supplying Skill and Luck for accuracy. |
| staffEntry | `InventoryEntry` | The staff inventory entry losing one use per attempt. |
| target | `Unit` | The unit receiving the heal or status on a hit. |
| distance | `int` | Distance in tiles used by the staff accuracy preview. |
| rng | `SeededRng` | Seeded stream consumed for the single 1RN hit roll. |

Returns: `bool` — True when the staff hit and its heal or status applied.

### CombatResolver.GenerateScript

Precomputes the ordered strike log without mutating units, consuming RNG for hit and crit rolls.

```csharp
public static CombatResult GenerateScript(Unit attacker, Weapon attackWeapon, Unit defender, Weapon? defendWeapon, int distance, int attackerTerrainAvoid, int attackerTerrainDefense, int defenderTerrainAvoid, int defenderTerrainDefense, SeededRng rng, CombatRules? rules = null, IList<string>? attackerSkills = null, IList<string>? defenderSkills = null, int supportHit = 0, int supportAvoid = 0, SeededRng? procRng = null, IReadOnlyDictionary<string, SkillDef>? skillCatalog = null)
```

| Parameter | Type | Description |
| --- | --- | --- |
| attacker | `Unit` | The attacking unit supplying stats and starting HP. |
| attackWeapon | `Weapon` | The attacking weapon used for every attacker strike. |
| defender | `Unit` | The defending unit supplying stats and starting HP. |
| defendWeapon | `Weapon?` | The defending weapon for the counter, or null when disarmed. |
| distance | `int` | Distance in tiles gating whether the defender counters. |
| attackerTerrainAvoid | `int` | Avoid from the attacker tile for the counter forecast. |
| attackerTerrainDefense | `int` | Defense from the attacker tile for the counter forecast. |
| defenderTerrainAvoid | `int` | Avoid from the defender tile for the attacker forecast. |
| defenderTerrainDefense | `int` | Defense from the defender tile for the attacker forecast. |
| rng | `SeededRng` | Seeded stream consumed for hit and crit rolls. |
| rules | `CombatRules?` | Formula tunables, or null for GBA defaults. |
| attackerSkills | `IList<string>?` | Attacker skill ids, swapped to defense for the counter. |
| defenderSkills | `IList<string>?` | Defender skill ids, swapped to attack for the counter. |
| supportHit | `int` | Bonded hit bonus for the attacker, swapped for the counter. |
| supportAvoid | `int` | Bonded avoid bonus for the defender, swapped for the counter. |
| procRng | `SeededRng?` | Seeded stream for skill proc rolls, or null to preview without procs. |
| skillCatalog | `IReadOnlyDictionary<string, SkillDef>?` | Explicit skill catalog, or null to use global static catalog. |

Returns: `CombatResult` — Strike log plus projected HP left for both sides, units untouched.

### CombatResolver.ApplyScript

Writes a generated script HP outcome back to the combatants.

```csharp
public static void ApplyScript(Unit attacker, Unit defender, CombatResult script)
```

| Parameter | Type | Description |
| --- | --- | --- |
| attacker | `Unit` | The attacking unit receiving the attacker HP left. |
| defender | `Unit` | The defending unit receiving the defender HP left. |
| script | `CombatResult` | Script produced by GenerateScript. |

### CombatResolver.Resolve

Pure seeded exchange: attacker strikes then a possible in-range counter, mutating HP.

```csharp
public static CombatResult Resolve(Unit attacker, Weapon attackWeapon, Unit defender, Weapon? defendWeapon, int distance, int attackerTerrainAvoid, int attackerTerrainDefense, int defenderTerrainAvoid, int defenderTerrainDefense, SeededRng rng, CombatRules? rules = null, IList<string>? attackerSkills = null, IList<string>? defenderSkills = null, int supportHit = 0, int supportAvoid = 0, SeededRng? procRng = null, IReadOnlyDictionary<string, SkillDef>? skillCatalog = null)
```

| Parameter | Type | Description |
| --- | --- | --- |
| attacker | `Unit` | The attacking unit whose HP is updated. |
| attackWeapon | `Weapon` | The attacking weapon used for every attacker strike. |
| defender | `Unit` | The defending unit whose HP is updated. |
| defendWeapon | `Weapon?` | The defending weapon for the counter, or null when disarmed. |
| distance | `int` | Distance in tiles gating whether the defender counters. |
| attackerTerrainAvoid | `int` | Avoid from the attacker tile for the counter forecast. |
| attackerTerrainDefense | `int` | Defense from the attacker tile for the counter forecast. |
| defenderTerrainAvoid | `int` | Avoid from the defender tile for the attacker forecast. |
| defenderTerrainDefense | `int` | Defense from the defender tile for the attacker forecast. |
| rng | `SeededRng` | Seeded stream consumed for hit and crit rolls. |
| rules | `CombatRules?` | Formula tunables, or null for GBA defaults. |
| attackerSkills | `IList<string>?` | Attacker skill ids, swapped to defense for the counter. |
| defenderSkills | `IList<string>?` | Defender skill ids, swapped to attack for the counter. |
| supportHit | `int` | Bonded hit bonus for the attacker, swapped for the counter. |
| supportAvoid | `int` | Bonded avoid bonus for the defender, swapped for the counter. |
| procRng | `SeededRng?` | Seeded stream for skill proc rolls, or null to preview without procs. |
| skillCatalog | `IReadOnlyDictionary<string, SkillDef>?` | Explicit skill catalog, or null to use global static catalog. |

Returns: `CombatResult` — Strike log plus clamped HP left for both sides.

```csharp
public static CombatResult Resolve(Unit attacker, InventoryEntry attackEntry, Unit defender, InventoryEntry? defendEntry, int distance, int attackerTerrainAvoid, int attackerTerrainDefense, int defenderTerrainAvoid, int defenderTerrainDefense, SeededRng rng, CombatRules? rules = null, IList<string>? attackerSkills = null, IList<string>? defenderSkills = null, int supportHit = 0, int supportAvoid = 0, SeededRng? procRng = null, IReadOnlyDictionary<string, SkillDef>? skillCatalog = null)
```

| Parameter | Type | Description |
| --- | --- | --- |
| attacker | `Unit` | The attacking unit whose HP is updated. |
| attackEntry | `InventoryEntry` | The attacker inventory entry losing one use per strike attempt. |
| defender | `Unit` | The defending unit whose HP is updated. |
| defendEntry | `InventoryEntry?` | The defender inventory entry losing one use on its counter, or null. |
| distance | `int` | Distance in tiles gating whether the defender counters. |
| attackerTerrainAvoid | `int` | Avoid from the attacker tile for the counter forecast. |
| attackerTerrainDefense | `int` | Defense from the attacker tile for the counter forecast. |
| defenderTerrainAvoid | `int` | Avoid from the defender tile for the attacker forecast. |
| defenderTerrainDefense | `int` | Defense from the defender tile for the attacker forecast. |
| rng | `SeededRng` | Seeded stream consumed for hit and crit rolls. |
| rules | `CombatRules?` | Formula tunables, or null for GBA defaults. |
| attackerSkills | `IList<string>?` | Attacker skill ids, swapped to defense for the counter. |
| defenderSkills | `IList<string>?` | Defender skill ids, swapped to attack for the counter. |
| supportHit | `int` | Bonded hit bonus for the attacker, swapped for the counter. |
| supportAvoid | `int` | Bonded avoid bonus for the defender, swapped for the counter. |
| procRng | `SeededRng?` | Seeded stream for skill proc rolls, or null to preview without procs. |
| skillCatalog | `IReadOnlyDictionary<string, SkillDef>?` | Explicit skill catalog, or null to use global static catalog. |

Returns: `CombatResult` — Strike log plus clamped HP left for both sides.

## SkillHook

When a skill modifies a forecast (attack or defense side).

| Name | Value | Description |
| --- | --- | --- |
| OnAttack |  | Applies when the skilled unit attacks. |
| OnDefense |  | Applies when the skilled unit defends. |

## SkillDef

Flat forecast modifiers for one skill id. Proc skills use ProcChance (0-100, 1RN).

### SkillDef.Id

Unique skill id matched against Unit.Skills entries.

```csharp
public string Id { get; }
```

Type: `string`

### SkillDef.Hook

Side of the exchange this skill modifies.

```csharp
public SkillHook Hook { get; set; }
```

Type: `SkillHook`

### SkillDef.DamageBonus

Flat damage added to forecasts on this hook.

```csharp
public int DamageBonus { get; set; }
```

Type: `int`

### SkillDef.HitBonus

Flat hit added to forecasts on this hook.

```csharp
public int HitBonus { get; set; }
```

Type: `int`

### SkillDef.CritBonus

Flat crit added to forecasts on this hook.

```csharp
public int CritBonus { get; set; }
```

Type: `int`

### SkillDef.DamageTakenPenalty

Flat damage subtracted from incoming forecasts on defense.

```csharp
public int DamageTakenPenalty { get; set; }
```

Type: `int`

### SkillDef.ProcChance

Proc chance from 0 to 100 rolled 1RN, where 0 always applies. Values outside 0-100 throw.

```csharp
public int ProcChance
```

Type: `int`

### SkillDef constructor

Creates a skill definition with the given id.

```csharp
public SkillDef(string id)
```

| Parameter | Type | Description |
| --- | --- | --- |
| id | `string` | Unique skill id used for registration and lookup. |

### SkillDef.Clone

Creates an independent copy of this skill definition.

```csharp
public SkillDef Clone()
```

Returns: `SkillDef` — A new definition with the same hook bonuses and proc chance.

## SkillCatalog

Hook point for abilities (Adept/Brave/Sol-adjacent flat mods, armor-slayer tags, ...). Hosts register SkillDefs; ApplyToForecast adds attacker OnAttack mods and defender OnDefense mods to a forecast copy. Proc rolls (if any) consume the passed RNG... pass null to preview without procs. Pure apart from optional proc RNG. Global mutable catalog: hosts must snapshot via TryGetAll/SaveSystem and ChapterState.SkillDefs for deterministic saves/replays; do not mutate mid-chapter without snapshotting.

### SkillCatalog.Register

Registers a skill definition, replacing any entry with the same id.

```csharp
public static void Register(SkillDef def)
```

| Parameter | Type | Description |
| --- | --- | --- |
| def | `SkillDef` | The skill definition to store by id. |

### SkillCatalog.Clear

Clears all registered skill definitions.

```csharp
public static void Clear()
```

### SkillCatalog.TryGet

Looks up a registered skill definition by id.

```csharp
public static bool TryGet(string id, out SkillDef? def)
```

| Parameter | Type | Description |
| --- | --- | --- |
| id | `string` | The skill id to find. |
| def | `out SkillDef?` | The matching definition, or null when unregistered. |

Returns: `bool` — True when a definition was found.

### SkillCatalog.TryGetAll

Returns a deep copy of all registered skill definitions for save snapshot.

```csharp
public static Dictionary<string, SkillDef> TryGetAll()
```

Returns: `Dictionary<string, SkillDef>` — Copy of the id to definition map with cloned definitions.

### SkillCatalog.Snapshot

Returns a deep snapshot copy for deterministic save hashing.

```csharp
public static IReadOnlyDictionary<string, SkillDef> Snapshot()
```

Returns: `IReadOnlyDictionary<string, SkillDef>` — Read-only view of the catalog copy with cloned definitions.

### SkillCatalog.ApplyToForecast

Returns an adjusted forecast copy with attacker and defender skill mods.

```csharp
public static CombatForecast ApplyToForecast(CombatForecast baseForecast, IEnumerable<string> attackerSkills, IEnumerable<string> defenderSkills, Random.SeededRng? procRng = null)
```

| Parameter | Type | Description |
| --- | --- | --- |
| baseForecast | `CombatForecast` | The input forecast, never mutated. |
| attackerSkills | `IEnumerable<string>` | Attacker skill ids applied through the OnAttack hook. |
| defenderSkills | `IEnumerable<string>` | Defender skill ids applied through the OnDefense hook. |
| procRng | `Random.SeededRng?` | Seeded stream for proc rolls, or null to preview without procs. |

Returns: `CombatForecast` — A new forecast with flat damage, hit, and crit mods applied.

```csharp
public static CombatForecast ApplyToForecast(CombatForecast baseForecast, IEnumerable<string> attackerSkills, IEnumerable<string> defenderSkills, Random.SeededRng? procRng, IReadOnlyDictionary<string, SkillDef>? catalog)
```

| Parameter | Type | Description |
| --- | --- | --- |
| baseForecast | `CombatForecast` | The input forecast, never mutated. |
| attackerSkills | `IEnumerable<string>` | Attacker skill ids applied through the OnAttack hook. |
| defenderSkills | `IEnumerable<string>` | Defender skill ids applied through the OnDefense hook. |
| procRng | `Random.SeededRng?` | Seeded stream for proc rolls, or null to preview without procs. |
| catalog | `IReadOnlyDictionary<string, SkillDef>?` | Explicit skill catalog, or null to use the global static catalog. |

Returns: `CombatForecast` — A new forecast with flat damage, hit, and crit mods applied.

## WeaponTriangle

Weapon-triangle (sword > axe > lance > sword; anima > light > dark > anima). Bows and staves sit outside the triangle. Bonuses are injectable (GBA: +1 Mt, +15 Hit). Reaver weapons (Weapon.ReversesTriangle) swap advantage for their wielder.

### WeaponTriangle.Advantage

Rates triangle advantage: 1 for advantage, -1 for disadvantage, 0 otherwise.

```csharp
public static int Advantage(WeaponType attacker, WeaponType defender)
```

| Parameter | Type | Description |
| --- | --- | --- |
| attacker | `WeaponType` | The attacking weapon type. |
| defender | `WeaponType` | The defending weapon type. |

Returns: `int` — 1 when the attacker wins, -1 when the defender wins, else 0.

```csharp
public static int Advantage(Weapon attacker, Weapon defender)
```

| Parameter | Type | Description |
| --- | --- | --- |
| attacker | `Weapon` | The attacking weapon (ReversesTriangle swaps advantage). |
| defender | `Weapon` | The defending weapon (ReversesTriangle swaps advantage). |

Returns: `int` — 1 when the attacker wins, -1 when the defender wins, else 0, with reaver swap applied.
