# Combat

Formulas follow GBA FE7/8 with tunables centralized in [`CombatRules`](api-combat.md#combatrules) (triangle
bonuses, doubling threshold, hit mode, effective multiplier, [`TorchVisionBonus`](api-combat.md#combatrules-torchvisionbonus)) — never scattered constants.

## Forecast

[`CombatResolver.Forecast`](api-combat.md#combatresolver-forecast) computes the battle-forecast window from attacker, weapon,
defender, optional defender weapon, and terrain avoid/defense for both sides:

| Value | Formula |
| --- | --- |
| Damage | `max(0, Power + (effective ? Might×EffMult : Might) + triangle×MtBonus − Def/Res − terrainDef)` |
| Hit | `clamp(Weapon.Hit + Skl×2 + Lck/2 + triangle×HitBonus − (foeAS + foeTerrainAvoid + foeLck/2))` |
| Crit | `clamp(Skl/2 + Weapon.Crit − foeLck/2)` |
| Strikes | 2 when attacker AS exceeds defender AS by the threshold (default 4), else 1 |

Attack speed is `Spd − max(0, Wt − Con)`. A disarmed defender's AS falls back to the
attacker's weapon weight. `Power` covers Str or Mag; `IsMagic` selects Def vs Res.

```csharp
// Forecast window, then the same numbers resolved for real.
CombatResolver.TerrainAt(board, atk.Position, out int atkAvo, out int atkDef);
CombatResolver.TerrainAt(board, def.Position, out int defAvo, out int defDef);
var (supHit, _) = supports.BonusFor(atk.Unit, atk.Position, field.Placed);
var (_, supAvoid) = supports.BonusFor(def.Unit, def.Position, field.Placed);

CombatForecast fc = CombatResolver.Forecast(
    atk.Unit, blade.Weapon, def.Unit, counter?.Weapon,
    atkAvo, atkDef, defAvo, defDef, rules,
    atk.Unit.Skills, def.Unit.Skills, supHit, supAvoid);

CombatResult result = CombatResolver.Resolve(
    atk.Unit, blade, def.Unit, counter, dist,
    atkAvo, atkDef, defAvo, defDef, state.CombatRng, rules,
    atk.Unit.Skills, def.Unit.Skills, supHit, supAvoid);

int dealt = 0;
foreach (CombatStrike s in result.Strikes)
    if (s.IsAttackerStrike && s.HitLanded) dealt += s.DamageDealt;
var (exp, levels) = CombatGains.ApplyAfterCombat(
    atk.Unit, def.Unit, dealt, !def.Unit.IsAlive,
    growths, state.GrowthRng, rules: null, usedWeapon: blade.Weapon);
```

## Weapon triangle

Sword \> Axe \> Lance \> Sword, and Anima \> Light \> Dark \> Anima. Bows and staves
sit outside. Advantage grants `+1` might and `+15` hit by default (GBA values,
configurable). Without a defender weapon there is no triangle term.

## Hit modes

Per-difficulty roll models (default 2RN):

| Mode | Behavior |
| --- | --- |
| `Classic` | 1RN — single roll, true odds |
| `TrueHit` | 2RN — mean of two rolls; high hit feels reliable, low hit hopeless |
| `TrueHitPlus` | 3RN — mean of three rolls |
| `Fates` | Single roll vs `h + (40/3)(h/100)·sin((0.02h−1)·180°)` |

Rates `<= 0` always miss and `>= 100` always hit in every mode. Crit is always a
single 1RN roll, and only rolled on a hit. Crits deal `×3` damage.

## Effective damage

[`Weapon.EffectiveAgainst`](api-data.md#weapon-effectiveagainst) lists defender class keys (for example `armor`, `flier`).
When it matches `defender.ClassKey`, might is multiplied by
[`CombatRules.EffectiveMultiplier`](api-combat.md#combatrules-effectivemultiplier) (default 3, GBA value) before the triangle term.
[`Forecast.IsEffective`](api-combat.md#combatforecast-iseffective) reports the trigger. Chapter files add it as an armory token:
`effective:armor,flier`.

## Durability

[`InventoryEntry.UsesLeft`](api-data.md#inventoryentry-usesleft) is consumed by the `InventoryEntry` overload of
[`CombatResolver.Resolve`](api-combat.md#combatresolver-resolve) — 1 use per strike attempt, hit or miss, for attacker and
counter. Broken entries (`IsBroken`) cannot strike. The `Weapon` overload stays pure
(no durability) for forecasts and tests. Presentation layers should migrate to the
entry overload instead of decrementing by hand (which missed counters).

## Staves

Healing staves set `HealPower` (armory token `heal:N`) and usually `type staff`.
[`ForecastHeal(healer, staff)`](api-combat.md#combatresolver-forecastheal) previews `HealPower + Power/2`; [`ResolveHeal`](api-combat.md#combatresolver-resolveheal) consumes
1 use, clamps to `MaxHP`, and returns HP restored.

Staves with accuracy use [`ForecastStaffHit(healer, staff, distance)`](api-combat.md#combatresolver-forecaststaffhit) —
`clamp(staff.Hit + Skl×2 + Lck/2 − 5/tile)` — and [`ResolveStaff`](api-combat.md#combatresolver-resolvestaff), which consumes 1 use
and rolls 1RN (always 1RN, ignoring `HitMode`). Status staves set `StatusEffect` and
`StatusDuration` (armory token `status:sleep:2`); on a hit the status is applied.
`IsStaff` is true for staff type, positive heal power, or any status effect.

```csharp
// Status staff at range: preview accuracy, then roll it for real (always 1RN).
int hit = CombatResolver.ForecastStaffHit(cleric, sleep.Weapon, dist);
bool landed = CombatResolver.ResolveStaff(cleric, sleep, patient, dist, state.CombatRng);
// On a hit the heal lands and/or Statuses.Apply runs; a use is spent either way.
```

## Skills

`Unit.Skills` holds skill ids; hosts register effects in [`SkillCatalog`](api-combat.md#skillcatalog) as [`SkillDef`](api-combat.md#skilldef)
records with a hook ([`OnAttack`/`OnDefense`](api-combat.md#skillhook)), flat forecast modifiers, and an optional
`ProcChance` (1RN, rolled on the caller's proc RNG — pass null to preview without
procs). [`ApplyToForecast`](api-combat.md#skillcatalog-applytoforecast) returns an adjusted copy and never mutates the input, so
forecast windows, AI scoring, and tests share the hook. Chapter files attach skills
with the unit token `skill:a,b` (or `skills:`).

```csharp
// Register once at load; ApplyToForecast never mutates its input.
SkillCatalog.Register(new SkillDef("brave-hit") { Hook = SkillHook.OnAttack, HitBonus = 20 });
SkillCatalog.Register(new SkillDef("guard") { Hook = SkillHook.OnDefense, DamageTakenPenalty = 2 });
CombatForecast mod = SkillCatalog.ApplyToForecast(baseFc, atk.Unit.Skills, def.Unit.Skills);
// Pass null procRng for previews; pass state.CombatRng to let ProcChance fire.
```

`Forecast` and both `Resolve` overloads take optional `attackerSkills` /
`defenderSkills` lists (default null = unmodified) applied via `ApplyToForecast`
with a null proc RNG — previews only, proc skills never fire mid-forecast. `Resolve`
swaps the lists for the counter forecast, so the defender's `OnAttack` skills shape
its counter and the attacker's `OnDefense` skills blunt it.

## Support in battle math

`Forecast` and both `Resolve` overloads take optional `supportHit` / `supportAvoid`
ints (default 0): `supportHit` is added to Hit, `supportAvoid` is subtracted as extra
defender avoid, after skill mods. Hosts compute them from
`SupportLedger.BonusFor` — the attacker's bonded hit bonus vs the defender's bonded
avoid bonus — and swap the pair for the counter, exactly as the skill lists swap.
[`EnemyAi.Decide`](api-tactics.md#enemyai-decide) accepts an optional [`SupportLedger`](api-tactics.md#supportledger) and does this weighting itself
for both the strike and the counter-risk forecast, alongside the units' own skills.

## Experience gains

[`Tactics.CombatGains.ApplyAfterCombat(attacker, defender, damageDealt, didKill,
growths, growthRng, rules?)`](api-tactics.md#combatgains-applyaftercombat) wraps `Experience.Gain` + `AddExp` in one host call
and returns `(expGained, levelsGained)` (levels counted by comparing `Level`
before/after). Call it after `Resolve` with the summed damage dealt and the kill
flag; same inputs + same growth seed always give the same level-ups. The
trailing `usedWeapon`/`wexpOnUse`/`wexpOnKill` hook banks weapon experience too
(see [data](data.md)): pass the fought weapon and the attacker gains
`wexpOnKill` (default 3) on a kill, `wexpOnUse` (default 1) otherwise — omit it
and no wexp moves.

## Terrain lookup

[`CombatResolver.TerrainAt(board, pos, out avoid, out defense)`](api-combat.md#combatresolver-terrainat) is the single lookup
for both sides. AI uses it via a `TerrainOf` delegate; UI forecast code should call it
rather than hardcoding zeros.

## Resolution order

`CombatResolver.Resolve` consumes the RNG and mutates HP: attacker strikes (possibly
twice); a living defender whose weapon reaches the distance counters between the
first and second strike. Same seed always replays identically — AI, forecasts, and
tests share the function.

Post-strike unit actions live in [`Tactics.Dance`](api-tactics.md#dance) (adjacent refresh) and
[`Tactics.Canto`](api-tactics.md#canto) (leftover-move repositioning) — both skill-gated, with hosts
tracking acted flags. Experience and
level-ups live in [`Tactics.Experience`](api-tactics.md#experience) (granted via `Tactics.CombatGains`);
statuses in [`Data.Statuses`](api-data.md#statuses); support in
[`Tactics.SupportLedger`](api-tactics.md#supportledger); see [tactics](tactics.md).
