using System;
using System.Collections.Generic;
using SrpgCore.Data;
using SrpgCore.Random;

namespace SrpgCore.Combat
{
    /// <summary>Hit-roll models, per-difficulty (default TRUE_HIT).</summary>
    public enum HitMode
    {
        /// <summary>Single roll with true odds.</summary>
        Classic = 0,    // 1RN: single roll, true odds
        /// <summary>Mean of two rolls for GBA feel.</summary>
        TrueHit = 1,    // 2RN: average of two rolls (GBA feel)
        /// <summary>Mean of three rolls, tighter around the displayed rate.</summary>
        TrueHitPlus = 2,// 3RN: average of three rolls
        /// <summary>Single roll against a Fates-adjusted threshold.</summary>
        Fates = 3,      // single roll vs Fates-adjusted threshold
    }

    /// <summary>Tunable battle formula constants (GBA FE7/8 defaults).</summary>
    public sealed class CombatRules
    {
        /// <summary>Might added per triangle advantage step (GBA default 1).</summary>
        public int TriangleMightBonus { get; set; } = 1;
        /// <summary>Hit added per triangle advantage step (GBA default 15).</summary>
        public int TriangleHitBonus { get; set; } = 15;
        /// <summary>Attack-speed lead needed to strike twice (GBA default 4).</summary>
        public int DoubleAttackSpeedThreshold { get; set; } = 4;
        /// <summary>Hit-roll model used for strikes (default TrueHit).</summary>
        public HitMode HitMode { get; set; } = HitMode.TrueHit;
        /// <summary>Damage multiplier for effective weapons (GBA x3).</summary>
        public int EffectiveMultiplier { get; set; } = 3;
        /// <summary>Vision bonus granted by the Torch staff status (GBA default 2).</summary>
        public int TorchVisionBonus { get; set; } = 2;
    }

    /// <summary>Battle-forecast numbers shown to the player before committing. Values: Damage per landed hit, Hit and Crit rates 0-100, AttackCount (1 or 2 when doubling), TriangleAdvantage (-1/0/1), and IsEffective flag.</summary>
    public sealed class CombatForecast
    {
        /// <summary>Predicted damage per landed hit after defense and terrain.</summary>
        public int Damage { get; set; }
        /// <summary>Predicted hit rate from 0 to 100 after skills and support.</summary>
        public int Hit { get; set; }
        /// <summary>Predicted crit rate from 0 to 100 after skills.</summary>
        public int Crit { get; set; }
        /// <summary>Number of attacker strikes, 1 normally or 2 when doubling.</summary>
        public int AttackCount { get; set; }
        /// <summary>Triangle result from the attacker side: 1, 0, or -1.</summary>
        public int TriangleAdvantage { get; set; }
        /// <summary>True when the attacking weapon is effective against the defender class.</summary>
        public bool IsEffective { get; set; }
    }

    /// <summary>One rolled strike inside a resolved exchange. Values: IsAttackerStrike (true = attacker swing, false = counter), HitLanded, WasCrit (triple damage), and DamageDealt (0 on miss).</summary>
    public sealed class CombatStrike
    {
        /// <summary>True for attacker strikes, false for defender counters.</summary>
        public bool IsAttackerStrike { get; set; }
        /// <summary>True when the hit roll landed.</summary>
        public bool HitLanded { get; set; }
        /// <summary>True when the landed hit rolled a crit for triple damage.</summary>
        public bool WasCrit { get; set; }
        /// <summary>Damage applied by this strike, zero on a miss.</summary>
        public int DamageDealt { get; set; }
    }

    /// <summary>Full strike log of a resolved exchange. Values: ordered Strikes list (attacker strikes with counter between them) and AttackerHPLeft/DefenderHPLeft clamped at zero; the same HP is written back to the combatants.</summary>
    public sealed class CombatResult
    {
        /// <summary>Ordered strike log: attacker strikes with the counter between them.</summary>
        public List<CombatStrike> Strikes { get; } = new List<CombatStrike>();
        /// <summary>Attacker HP after the exchange, clamped at zero.</summary>
        public int AttackerHPLeft { get; set; }
        /// <summary>Defender HP after the exchange, clamped at zero.</summary>
        public int DefenderHPLeft { get; set; }
    }

    /// <summary>
    /// Pure combat math. Forecast() feeds the UI; Resolve() consumes RNG and mutates HP.
    /// Same inputs + same seed always give the same result (AI, forecasts, replays share it).
    /// Durability is consumed by the InventoryEntry overload of Resolve(); the Weapon
    /// overload stays pure for forecasts/tests. Healing/status staves use ForecastHeal,
    /// ForecastStaffHit and ResolveStaff (1RN); flat skill mods via SkillCatalog.
    /// Post-strike unit actions live in Tactics: Dance (adjacent refresh, host-tracked
    /// acted flags) and Canto (leftover-move repositioning).
    /// </summary>
    public static class CombatResolver
    {
        /// <summary>Attack speed: Speed minus encumbrance from excess weapon weight.</summary>
        /// <param name="unit">The unit whose Speed and Constitution apply.</param>
        /// <param name="weapon">The weapon whose Weight may slow the unit.</param>
        /// <returns>Effective attack speed used for hit and doubling.</returns>
        public static int AttackSpeed(Unit unit, Weapon weapon)
        {
            int encumbrance = Math.Max(0, weapon.Weight - unit.Stats.Constitution);
            return unit.Stats.Speed - encumbrance;
        }

        /// <summary>Terrain avoid/defense at a tile (single lookup for callers).</summary>
        /// <param name="board">The board holding the terrain table.</param>
        /// <param name="pos">The tile to look up.</param>
        /// <param name="avoid">The terrain avoid at the tile.</param>
        /// <param name="defense">The terrain defense at the tile.</param>
        public static void TerrainAt(Grid.Board board, Grid.GridPoint pos, out int avoid, out int defense)
        {
            var t = board.GetTerrain(pos);
            avoid = t.Avoid;
            defense = t.Defense;
        }

        /// <summary>Damage per landed hit: attack stat plus might and triangle, minus defense and terrain.</summary>
        /// <param name="attacker">The attacking unit supplying the attack stat.</param>
        /// <param name="attackWeapon">The attacking weapon supplying might and magic selection.</param>
        /// <param name="defender">The defending unit supplying defense or resistance and class key.</param>
        /// <param name="defenderTerrainDefense">Defense from the defender tile subtracted from damage.</param>
        /// <param name="triangle">Triangle advantage from the attacker side.</param>
        /// <param name="effective">True when the weapon is effective against the defender class.</param>
        /// <param name="rules">Formula tunables, or null for GBA defaults.</param>
        /// <returns>Clamped damage per landed hit.</returns>
        public static int ComputeDamage(Unit attacker, Weapon attackWeapon, Unit defender, int defenderTerrainDefense, int triangle, bool effective, CombatRules? rules = null)
        {
            if (attacker == null) throw new ArgumentNullException(nameof(attacker));
            if (attackWeapon == null) throw new ArgumentNullException(nameof(attackWeapon));
            if (defender == null) throw new ArgumentNullException(nameof(defender));
            rules ??= new CombatRules();
            int defenseStat = attackWeapon.IsMagic ? defender.Stats.Resistance : defender.Stats.Defense;
            int mightTerm = effective ? attackWeapon.Might * rules.EffectiveMultiplier : attackWeapon.Might;
            return Math.Max(0, attacker.Stats.Power + mightTerm + triangle * rules.TriangleMightBonus - defenseStat - defenderTerrainDefense);
        }

        /// <summary>Hit rate: weapon hit plus skill and luck and triangle, minus defender avoid.</summary>
        /// <param name="attacker">The attacking unit supplying skill and luck.</param>
        /// <param name="attackWeapon">The attacking weapon supplying base hit.</param>
        /// <param name="defender">The defending unit supplying avoid inputs.</param>
        /// <param name="defendWeapon">The defending weapon for triangle and counter AS, or null when disarmed.</param>
        /// <param name="defenderTerrainAvoid">Avoid from the defender tile added to defender avoid.</param>
        /// <param name="rules">Formula tunables, or null for GBA defaults.</param>
        /// <returns>Clamped hit rate from 0 to 100.</returns>
        public static int ComputeHitRate(Unit attacker, Weapon attackWeapon, Unit defender, Weapon? defendWeapon, int defenderTerrainAvoid, CombatRules? rules = null)
        {
            if (attacker == null) throw new ArgumentNullException(nameof(attacker));
            if (attackWeapon == null) throw new ArgumentNullException(nameof(attackWeapon));
            if (defender == null) throw new ArgumentNullException(nameof(defender));
            rules ??= new CombatRules();
            int tri = defendWeapon != null ? WeaponTriangle.Advantage(attackWeapon, defendWeapon) : 0;
            int hit = attackWeapon.Hit
                + attacker.Stats.Skill * 2 + attacker.Stats.Luck / 2
                + tri * rules.TriangleHitBonus
                - (AttackSpeed(defender, defendWeapon ?? attackWeapon)
                   + defenderTerrainAvoid + defender.Stats.Luck / 2);
            return Math.Max(0, Math.Min(100, hit));
        }

        /// <summary>Avoid total: attack speed plus terrain avoid plus half luck.</summary>
        /// <param name="defender">The defending unit supplying speed and luck.</param>
        /// <param name="defendWeapon">The defending weapon for attack speed, or null to use the incoming weapon.</param>
        /// <param name="incomingWeapon">The incoming weapon used for attack speed when disarmed.</param>
        /// <param name="defenderTerrainAvoid">Avoid from the defender tile.</param>
        /// <returns>Unclamped avoid total.</returns>
        public static int ComputeAvoidRate(Unit defender, Weapon? defendWeapon, Weapon incomingWeapon, int defenderTerrainAvoid)
        {
            if (defender == null) throw new ArgumentNullException(nameof(defender));
            if (incomingWeapon == null) throw new ArgumentNullException(nameof(incomingWeapon));
            return AttackSpeed(defender, defendWeapon ?? incomingWeapon) + defenderTerrainAvoid + defender.Stats.Luck / 2;
        }

        /// <summary>Crit rate: half skill plus weapon crit minus defender luck.</summary>
        /// <param name="attacker">The attacking unit supplying skill.</param>
        /// <param name="attackWeapon">The attacking weapon supplying base crit.</param>
        /// <param name="defender">The defending unit supplying luck.</param>
        /// <returns>Clamped crit rate from 0 to 100.</returns>
        public static int ComputeCritRate(Unit attacker, Weapon attackWeapon, Unit defender)
        {
            if (attacker == null) throw new ArgumentNullException(nameof(attacker));
            if (attackWeapon == null) throw new ArgumentNullException(nameof(attackWeapon));
            if (defender == null) throw new ArgumentNullException(nameof(defender));
            return Math.Max(0, Math.Min(100, attacker.Stats.Skill / 2 + attackWeapon.Crit - defender.Stats.Luck / 2));
        }

        /// <summary>Strike count: 2 when the attacker doubles, else 1.</summary>
        /// <param name="attacker">The attacking unit supplying attack speed.</param>
        /// <param name="attackWeapon">The attacking weapon supplying weight.</param>
        /// <param name="defender">The defending unit supplying attack speed.</param>
        /// <param name="defendWeapon">The defending weapon for speed, or null when disarmed.</param>
        /// <param name="rules">Formula tunables, or null for GBA defaults.</param>
        /// <returns>Number of attacker strikes, 1 or 2.</returns>
        public static int ComputeAttackCount(Unit attacker, Weapon attackWeapon, Unit defender, Weapon? defendWeapon, CombatRules? rules = null)
        {
            if (attacker == null) throw new ArgumentNullException(nameof(attacker));
            if (attackWeapon == null) throw new ArgumentNullException(nameof(attackWeapon));
            if (defender == null) throw new ArgumentNullException(nameof(defender));
            rules ??= new CombatRules();
            return AttackSpeed(attacker, attackWeapon) - AttackSpeed(defender, defendWeapon ?? attackWeapon) >= rules.DoubleAttackSpeedThreshold ? 2 : 1;
        }

        /// <summary>Pure GBA-style forecast: damage, hit, crit, strikes, triangle, effectiveness.</summary>
        /// <param name="attacker">The attacking unit supplying stats and HP base.</param>
        /// <param name="attackWeapon">The attacking weapon supplying might, hit, crit, and weight.</param>
        /// <param name="defender">The defending unit supplying defensive stats.</param>
        /// <param name="defendWeapon">The defending weapon for triangle and counter AS, or null when disarmed.</param>
        /// <param name="attackerTerrainAvoid">Avoid from the attacker tile (display only).</param>
        /// <param name="attackerTerrainDefense">Defense from the attacker tile (display only).</param>
        /// <param name="defenderTerrainAvoid">Avoid from the defender tile added to defender avoid.</param>
        /// <param name="defenderTerrainDefense">Defense from the defender tile subtracted from damage.</param>
        /// <param name="rules">Formula tunables, or null for GBA defaults.</param>
        /// <param name="attackerSkills">Attacker skill ids applied as flat preview mods.</param>
        /// <param name="defenderSkills">Defender skill ids applied as flat preview mods.</param>
        /// <param name="supportHit">Bonded hit bonus added to the attacker forecast.</param>
        /// <param name="supportAvoid">Bonded avoid bonus subtracted as extra defender avoid.</param>
        /// <param name="procRng">Seeded stream for skill proc rolls, or null to preview without procs.</param>
        /// <param name="skillCatalog">Explicit skill catalog, or null to use global static catalog.</param>
        /// <returns>A new forecast copy with skill and support mods applied.</returns>
        public static CombatForecast Forecast(
            Unit attacker, Weapon attackWeapon,
            Unit defender, Weapon? defendWeapon,
            int attackerTerrainAvoid, int attackerTerrainDefense,
            int defenderTerrainAvoid, int defenderTerrainDefense,
            CombatRules? rules = null,
            IList<string>? attackerSkills = null,
            IList<string>? defenderSkills = null,
            int supportHit = 0,
            int supportAvoid = 0,
            SeededRng? procRng = null,
            IReadOnlyDictionary<string, SkillDef>? skillCatalog = null)
        {
            rules ??= new CombatRules();
            int tri = defendWeapon != null
                ? WeaponTriangle.Advantage(attackWeapon, defendWeapon)
                : 0;
            bool effective = attackWeapon.IsEffectiveAgainst(defender.ClassKey);
            int damage = ComputeDamage(attacker, attackWeapon, defender, defenderTerrainDefense, tri, effective, rules);
            int hit = ComputeHitRate(attacker, attackWeapon, defender, defendWeapon, defenderTerrainAvoid, rules);
            int crit = ComputeCritRate(attacker, attackWeapon, defender);
            int attacks = ComputeAttackCount(attacker, attackWeapon, defender, defendWeapon, rules);

            var forecast = new CombatForecast
            {
                Damage = damage,
                Hit = hit,
                Crit = crit,
                AttackCount = attacks,
                TriangleAdvantage = tri,
                IsEffective = effective,
            };

            // Skills (flat hook mods; procRng null = preview, procs never fire) then
            // support (attacker's bonded hit bonus vs defender's bonded avoid bonus).
            if (attackerSkills != null || defenderSkills != null)
                forecast = SkillCatalog.ApplyToForecast(forecast,
                    attackerSkills ?? Array.Empty<string>(),
                    defenderSkills ?? Array.Empty<string>(), procRng, skillCatalog);
            if (supportHit != 0 || supportAvoid != 0)
                forecast.Hit = Math.Max(0, Math.Min(100, forecast.Hit + supportHit - supportAvoid));
            return forecast;
        }

        private static bool IsSilenced(Unit u) => u.Statuses.ContainsKey("silence");

        /// <summary>Staff heal preview: HealPower plus half Power, clamped to missing HP.</summary>
        /// <param name="healer">The healing unit supplying Power.</param>
        /// <param name="staff">The staff supplying HealPower.</param>
        /// <returns>Unclamped heal amount before the MaxHP cap.</returns>
        public static int ForecastHeal(Unit healer, Weapon staff)
        {
            if (healer == null) throw new ArgumentNullException(nameof(healer));
            if (staff == null) throw new ArgumentNullException(nameof(staff));
            return Math.Max(0, staff.HealPower + healer.Stats.Power / 2);
        }

        /// <summary>Staff accuracy at a distance: weapon hit + Skl*2 + Lck/2 - 5 per tile.</summary>
        /// <param name="healer">The healing unit supplying Skill and Luck.</param>
        /// <param name="staff">The staff supplying base hit.</param>
        /// <param name="distance">Distance in tiles, costing 5 hit each.</param>
        /// <returns>Clamped staff hit rate from 0 to 100.</returns>
        public static int ForecastStaffHit(Unit healer, Weapon staff, int distance)
        {
            if (healer == null) throw new ArgumentNullException(nameof(healer));
            if (staff == null) throw new ArgumentNullException(nameof(staff));
            int hit = staff.Hit + healer.Stats.Skill * 2 + healer.Stats.Luck / 2 - distance * 5;
            return Math.Max(0, Math.Min(100, hit));
        }

        /// <summary>Apply a staff heal: consumes 1 use, clamps to MaxHP, returns HP restored.</summary>
        /// <param name="healer">The healing unit supplying Power.</param>
        /// <param name="staffEntry">The staff inventory entry losing one use.</param>
        /// <param name="patient">The unit receiving the heal up to MaxHP.</param>
        /// <returns>HP actually restored after clamping.</returns>
        public static int ResolveHeal(Unit healer, InventoryEntry staffEntry, Unit patient)
        {
            if (healer == null) throw new ArgumentNullException(nameof(healer));
            if (staffEntry == null) throw new ArgumentNullException(nameof(staffEntry));
            if (patient == null) throw new ArgumentNullException(nameof(patient));
            if (staffEntry.IsBroken) return 0;
            if (IsSilenced(healer)) return 0;
            staffEntry.UsesLeft--;
            int amount = Math.Min(ForecastHeal(healer, staffEntry.Weapon),
                patient.Stats.MaxHP - patient.CurrentHP);
            if (amount < 0) amount = 0;
            patient.CurrentHP += amount;
            return amount;
        }

        /// <summary>
        /// Staff use with accuracy: consumes 1 use, rolls 1RN vs ForecastStaffHit; heal staves
        /// restore on hit, status staves apply StatusEffect/StatusDuration on hit. Returns true on hit.
        /// Staves always use 1RN (GBA shape), ignoring CombatRules.HitMode.
        /// </summary>
        /// <param name="healer">The staff user supplying Skill and Luck for accuracy.</param>
        /// <param name="staffEntry">The staff inventory entry losing one use per attempt.</param>
        /// <param name="target">The unit receiving the heal or status on a hit.</param>
        /// <param name="distance">Distance in tiles used by the staff accuracy preview.</param>
        /// <param name="rng">Seeded stream consumed for the single 1RN hit roll.</param>
        /// <returns>True when the staff hit and its heal or status applied.</returns>
        public static bool ResolveStaff(Unit healer, InventoryEntry staffEntry, Unit target, int distance, SeededRng rng)
        {
            if (healer == null) throw new ArgumentNullException(nameof(healer));
            if (staffEntry == null) throw new ArgumentNullException(nameof(staffEntry));
            if (target == null) throw new ArgumentNullException(nameof(target));
            if (rng == null) throw new ArgumentNullException(nameof(rng));
            if (staffEntry.IsBroken) return false;
            if (IsSilenced(healer) && staffEntry.Weapon.IsStaff) return false;
            staffEntry.UsesLeft--;
            int hit = ForecastStaffHit(healer, staffEntry.Weapon, distance);
            if (!rng.RollHit1RN(hit)) return false;
            Weapon w = staffEntry.Weapon;
            if (w.HealPower > 0)
            {
                int amount = Math.Min(ForecastHeal(healer, w), target.Stats.MaxHP - target.CurrentHP);
                if (amount > 0) target.CurrentHP += amount;
            }
            if (w.StatusEffect.Length > 0)
                Statuses.Apply(target, w.StatusEffect, Math.Max(1, w.StatusDuration));
            return true;
        }

        /// <summary>Precomputes the ordered strike log without mutating units, consuming RNG for hit and crit rolls.</summary>
        /// <param name="attacker">The attacking unit supplying stats and starting HP.</param>
        /// <param name="attackWeapon">The attacking weapon used for every attacker strike.</param>
        /// <param name="defender">The defending unit supplying stats and starting HP.</param>
        /// <param name="defendWeapon">The defending weapon for the counter, or null when disarmed.</param>
        /// <param name="distance">Distance in tiles gating whether the defender counters.</param>
        /// <param name="attackerTerrainAvoid">Avoid from the attacker tile for the counter forecast.</param>
        /// <param name="attackerTerrainDefense">Defense from the attacker tile for the counter forecast.</param>
        /// <param name="defenderTerrainAvoid">Avoid from the defender tile for the attacker forecast.</param>
        /// <param name="defenderTerrainDefense">Defense from the defender tile for the attacker forecast.</param>
        /// <param name="rng">Seeded stream consumed for hit and crit rolls.</param>
        /// <param name="rules">Formula tunables, or null for GBA defaults.</param>
        /// <param name="attackerSkills">Attacker skill ids, swapped to defense for the counter.</param>
        /// <param name="defenderSkills">Defender skill ids, swapped to attack for the counter.</param>
        /// <param name="supportHit">Bonded hit bonus for the attacker, swapped for the counter.</param>
        /// <param name="supportAvoid">Bonded avoid bonus for the defender, swapped for the counter.</param>
        /// <param name="procRng">Seeded stream for skill proc rolls, or null to preview without procs.</param>
        /// <param name="skillCatalog">Explicit skill catalog, or null to use global static catalog.</param>
        /// <returns>Strike log plus projected HP left for both sides, units untouched.</returns>
        public static CombatResult GenerateScript(
            Unit attacker, Weapon attackWeapon,
            Unit defender, Weapon? defendWeapon,
            int distance,
            int attackerTerrainAvoid, int attackerTerrainDefense,
            int defenderTerrainAvoid, int defenderTerrainDefense,
            SeededRng rng,
            CombatRules? rules = null,
            IList<string>? attackerSkills = null,
            IList<string>? defenderSkills = null,
            int supportHit = 0,
            int supportAvoid = 0,
            SeededRng? procRng = null,
            IReadOnlyDictionary<string, SkillDef>? skillCatalog = null)
        {
            if (attacker == null) throw new ArgumentNullException(nameof(attacker));
            if (attackWeapon == null) throw new ArgumentNullException(nameof(attackWeapon));
            if (defender == null) throw new ArgumentNullException(nameof(defender));
            if (rng == null) throw new ArgumentNullException(nameof(rng));
            rules ??= new CombatRules();

            var result = new CombatResult();
            int atkHP = attacker.CurrentHP;
            int defHP = defender.CurrentHP;
            bool defenderCounters = defHP > 0 && defendWeapon != null && defendWeapon.CanCounterAt(distance);

            var atkForecast = Forecast(attacker, attackWeapon, defender, defendWeapon,
                attackerTerrainAvoid, attackerTerrainDefense,
                defenderTerrainAvoid, defenderTerrainDefense, rules,
                attackerSkills, defenderSkills, supportHit, supportAvoid, procRng, skillCatalog);

            for (int round = 0; round < atkForecast.AttackCount; round++)
            {
                defHP = Strike(atkForecast, ref defHP, rng, rules, result, attackerStrike: true);
                if (defHP <= 0) break;
                if (round == 0 && defenderCounters)
                {
                    var counterForecast = Forecast(defender, defendWeapon!, attacker, attackWeapon,
                        defenderTerrainAvoid, defenderTerrainDefense,
                        attackerTerrainAvoid, attackerTerrainDefense, rules,
                        defenderSkills, attackerSkills, supportAvoid, supportHit, procRng, skillCatalog);
                    atkHP = Strike(counterForecast, ref atkHP, rng, rules, result, attackerStrike: false);
                    if (atkHP <= 0) break;
                }
            }

            result.AttackerHPLeft = Math.Max(0, atkHP);
            result.DefenderHPLeft = Math.Max(0, defHP);
            return result;
        }

        /// <summary>Writes a generated script HP outcome back to the combatants.</summary>
        /// <param name="attacker">The attacking unit receiving the attacker HP left.</param>
        /// <param name="defender">The defending unit receiving the defender HP left.</param>
        /// <param name="script">Script produced by GenerateScript.</param>
        public static void ApplyScript(Unit attacker, Unit defender, CombatResult script)
        {
            if (attacker == null) throw new ArgumentNullException(nameof(attacker));
            if (defender == null) throw new ArgumentNullException(nameof(defender));
            if (script == null) throw new ArgumentNullException(nameof(script));
            attacker.CurrentHP = script.AttackerHPLeft;
            defender.CurrentHP = script.DefenderHPLeft;
        }

        /// <summary>Pure seeded exchange: attacker strikes then a possible in-range counter, mutating HP.</summary>
        /// <param name="attacker">The attacking unit whose HP is updated.</param>
        /// <param name="attackWeapon">The attacking weapon used for every attacker strike.</param>
        /// <param name="defender">The defending unit whose HP is updated.</param>
        /// <param name="defendWeapon">The defending weapon for the counter, or null when disarmed.</param>
        /// <param name="distance">Distance in tiles gating whether the defender counters.</param>
        /// <param name="attackerTerrainAvoid">Avoid from the attacker tile for the counter forecast.</param>
        /// <param name="attackerTerrainDefense">Defense from the attacker tile for the counter forecast.</param>
        /// <param name="defenderTerrainAvoid">Avoid from the defender tile for the attacker forecast.</param>
        /// <param name="defenderTerrainDefense">Defense from the defender tile for the attacker forecast.</param>
        /// <param name="rng">Seeded stream consumed for hit and crit rolls.</param>
        /// <param name="rules">Formula tunables, or null for GBA defaults.</param>
        /// <param name="attackerSkills">Attacker skill ids, swapped to defense for the counter.</param>
        /// <param name="defenderSkills">Defender skill ids, swapped to attack for the counter.</param>
        /// <param name="supportHit">Bonded hit bonus for the attacker, swapped for the counter.</param>
        /// <param name="supportAvoid">Bonded avoid bonus for the defender, swapped for the counter.</param>
        /// <param name="procRng">Seeded stream for skill proc rolls, or null to preview without procs.</param>
        /// <param name="skillCatalog">Explicit skill catalog, or null to use global static catalog.</param>
        /// <returns>Strike log plus clamped HP left for both sides.</returns>
        public static CombatResult Resolve(
            Unit attacker, Weapon attackWeapon,
            Unit defender, Weapon? defendWeapon,
            int distance,
            int attackerTerrainAvoid, int attackerTerrainDefense,
            int defenderTerrainAvoid, int defenderTerrainDefense,
            SeededRng rng,
            CombatRules? rules = null,
            IList<string>? attackerSkills = null,
            IList<string>? defenderSkills = null,
            int supportHit = 0,
            int supportAvoid = 0,
            SeededRng? procRng = null,
            IReadOnlyDictionary<string, SkillDef>? skillCatalog = null)
        {
            CombatResult script = GenerateScript(attacker, attackWeapon, defender, defendWeapon, distance,
                attackerTerrainAvoid, attackerTerrainDefense, defenderTerrainAvoid, defenderTerrainDefense,
                rng, rules, attackerSkills, defenderSkills, supportHit, supportAvoid, procRng, skillCatalog);
            ApplyScript(attacker, defender, script);
            return script;
        }

        /// <summary>
        /// Resolve that also consumes durability: 1 use per strike attempt (hit or miss,
        /// GBA-style), attacker strikes then a possible counter. Broken entries cannot strike.
        /// </summary>
        /// <param name="attacker">The attacking unit whose HP is updated.</param>
        /// <param name="attackEntry">The attacker inventory entry losing one use per strike attempt.</param>
        /// <param name="defender">The defending unit whose HP is updated.</param>
        /// <param name="defendEntry">The defender inventory entry losing one use on its counter, or null.</param>
        /// <param name="distance">Distance in tiles gating whether the defender counters.</param>
        /// <param name="attackerTerrainAvoid">Avoid from the attacker tile for the counter forecast.</param>
        /// <param name="attackerTerrainDefense">Defense from the attacker tile for the counter forecast.</param>
        /// <param name="defenderTerrainAvoid">Avoid from the defender tile for the attacker forecast.</param>
        /// <param name="defenderTerrainDefense">Defense from the defender tile for the attacker forecast.</param>
        /// <param name="rng">Seeded stream consumed for hit and crit rolls.</param>
        /// <param name="rules">Formula tunables, or null for GBA defaults.</param>
        /// <param name="attackerSkills">Attacker skill ids, swapped to defense for the counter.</param>
        /// <param name="defenderSkills">Defender skill ids, swapped to attack for the counter.</param>
        /// <param name="supportHit">Bonded hit bonus for the attacker, swapped for the counter.</param>
        /// <param name="supportAvoid">Bonded avoid bonus for the defender, swapped for the counter.</param>
        /// <param name="procRng">Seeded stream for skill proc rolls, or null to preview without procs.</param>
        /// <param name="skillCatalog">Explicit skill catalog, or null to use global static catalog.</param>
        /// <returns>Strike log plus clamped HP left for both sides.</returns>
        public static CombatResult Resolve(
            Unit attacker, InventoryEntry attackEntry,
            Unit defender, InventoryEntry? defendEntry,
            int distance,
            int attackerTerrainAvoid, int attackerTerrainDefense,
            int defenderTerrainAvoid, int defenderTerrainDefense,
            SeededRng rng,
            CombatRules? rules = null,
            IList<string>? attackerSkills = null,
            IList<string>? defenderSkills = null,
            int supportHit = 0,
            int supportAvoid = 0,
            SeededRng? procRng = null,
            IReadOnlyDictionary<string, SkillDef>? skillCatalog = null)
        {
            if (attackEntry == null) throw new ArgumentNullException(nameof(attackEntry));
            if (rng == null) throw new ArgumentNullException(nameof(rng));
            rules ??= new CombatRules();

            var result = new CombatResult();
            int atkHP = attacker.CurrentHP;
            int defHP = defender.CurrentHP;
            if (attackEntry.IsBroken)
            {
                result.AttackerHPLeft = atkHP;
                result.DefenderHPLeft = defHP;
                return result;
            }

            bool defenderCounters = defHP > 0 && defendEntry != null && !defendEntry.IsBroken
                && defendEntry.Weapon.CanCounterAt(distance);

            var atkForecast = Forecast(attacker, attackEntry.Weapon, defender,
                defendEntry?.Weapon,
                attackerTerrainAvoid, attackerTerrainDefense,
                defenderTerrainAvoid, defenderTerrainDefense, rules,
                attackerSkills, defenderSkills, supportHit, supportAvoid, procRng, skillCatalog);

            for (int round = 0; round < atkForecast.AttackCount; round++)
            {
                if (attackEntry.IsBroken) break;
                attackEntry.UsesLeft--;
                defHP = Strike(atkForecast, ref defHP, rng, rules, result, attackerStrike: true);
                if (defHP <= 0) break;
                if (round == 0 && defenderCounters)
                {
                    var counterForecast = Forecast(defender, defendEntry!.Weapon, attacker, attackEntry.Weapon,
                        defenderTerrainAvoid, defenderTerrainDefense,
                        attackerTerrainAvoid, attackerTerrainDefense, rules,
                        defenderSkills, attackerSkills, supportAvoid, supportHit, procRng, skillCatalog);
                    defendEntry!.UsesLeft--;
                    atkHP = Strike(counterForecast, ref atkHP, rng, rules, result, attackerStrike: false);
                    if (defendEntry.IsBroken) defenderCounters = false;
                    if (atkHP <= 0) break;
                }
            }

            result.AttackerHPLeft = Math.Max(0, atkHP);
            result.DefenderHPLeft = Math.Max(0, defHP);
            attacker.CurrentHP = result.AttackerHPLeft;
            defender.CurrentHP = result.DefenderHPLeft;
            return result;
        }

        private static int Strike(
            CombatForecast forecast, ref int targetHP,
            SeededRng rng, CombatRules rules, CombatResult result, bool attackerStrike)
        {
            var strike = new CombatStrike { IsAttackerStrike = attackerStrike };
            bool hit = RollHit(rng, forecast.Hit, rules);
            strike.HitLanded = hit;
            if (hit)
            {
                strike.WasCrit = rng.RollHit1RN(forecast.Crit);
                strike.DamageDealt = forecast.Damage * (strike.WasCrit ? 3 : 1);
                targetHP -= strike.DamageDealt;
            }
            result.Strikes.Add(strike);
            return targetHP;
        }

        private static bool RollHit(SeededRng rng, int rate, CombatRules rules)
        {
            switch (rules.HitMode)
            {
                case HitMode.Classic:
                    return rng.RollHit1RN(rate);
                case HitMode.TrueHitPlus:
                    if (rate <= 0) return false;
                    if (rate >= 100) return true;
                    return (rng.Next(100) + rng.Next(100) + rng.Next(100)) / 3.0 < rate;
                case HitMode.Fates:
                    return FatesRoll(rng, rate);
                default:
                    return rng.RollHit2RN(rate);
            }
        }

        /// <summary>GBA-accurate Fates: piecewise 2RN variant (hit &lt;=50: hit*(100+hit)/200, else 100-((100-hit)*(100-hit)/200)), fork documented in SRPGDocs/combat.md.</summary>
        private static bool FatesRoll(SeededRng rng, int rate)
        {
            double h = Math.Max(0, Math.Min(100, rate));
            double threshold = h <= 50 ? h * (100 + h) / 200.0 : 100 - (100 - h) * (100 - h) / 200.0;
            return rng.NextDouble() * 100.0 < threshold;
        }
    }
}
