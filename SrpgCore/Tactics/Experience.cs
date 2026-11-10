using System;
using SrpgCore.Data;
using SrpgCore.Random;

namespace SrpgCore.Tactics
{
    /// <summary>Per-stat level-up growth chances (0-100).</summary>
    public sealed class GrowthRates
    {
        /// <summary>MaxHP growth chance (0-100) rolled on the growth RNG stream.</summary>
        public int HP { get; set; }
        /// <summary>Power growth chance (0-100) rolled on the growth RNG stream.</summary>
        public int Power { get; set; }
        /// <summary>Skill growth chance (0-100) rolled on the growth RNG stream.</summary>
        public int Skill { get; set; }
        /// <summary>Speed growth chance (0-100) rolled on the growth RNG stream.</summary>
        public int Speed { get; set; }
        /// <summary>Luck growth chance (0-100) rolled on the growth RNG stream.</summary>
        public int Luck { get; set; }
        /// <summary>Defense growth chance (0-100) rolled on the growth RNG stream.</summary>
        public int Defense { get; set; }
        /// <summary>Resistance growth chance (0-100) rolled on the growth RNG stream.</summary>
        public int Resistance { get; set; }

        /// <summary>Copies these growth chances.</summary>
        /// <returns>An independent copy with the same per-stat chances.</returns>
        public GrowthRates Clone() => (GrowthRates)MemberwiseClone();
    }

    /// <summary>Tunable experience constants.</summary>
    public sealed class ExperienceRules
    {
        /// <summary>Experience paid for a non-killing chip hit before bonuses.</summary>
        public int BaseChipExp { get; set; } = 10;
        /// <summary>Experience paid for a killing blow before bonuses.</summary>
        public int BaseKillExp { get; set; } = 30;
        /// <summary>Extra experience per level the defender outranks the attacker.</summary>
        public int LevelDiffBonus { get; set; } = 2;
        /// <summary>Cap on the counted attacker-defender level difference.</summary>
        public int MaxLevelDiff { get; set; } = 5;
        /// <summary>Floor clamping every experience award.</summary>
        public int MinExp { get; set; } = 1;
        /// <summary>Ceiling clamping every experience award.</summary>
        public int MaxExp { get; set; } = 100;
        /// <summary>Accumulated experience needed for one level-up.</summary>
        public int ExpPerLevel { get; set; } = 100;
    }

    /// <summary>
    /// GBA-flavored experience: chip hits pay a base, kills pay more, higher-level
    /// victims pay a capped level-difference bonus. Pure functions; LevelUp consumes
    /// the growth RNG stream (1RN per stat) and heals MaxHP gains into CurrentHP.
    /// </summary>
    public static class Experience
    {
        /// <summary>Computes the experience award for one attack outcome.</summary>
        /// <param name="didKill">True when the attack killed the defender.</param>
        /// <param name="attackerLevel">Current level of the attacking unit.</param>
        /// <param name="defenderLevel">Current level of the defending unit.</param>
        /// <param name="damageDealt">Damage dealt, adding a small chip bonus when non-lethal.</param>
        /// <param name="rules">Tunable constants (defaults to GBA-flavored values).</param>
        /// <returns>Clamped experience award between MinExp and MaxExp.</returns>
        public static int Gain(bool didKill, int attackerLevel, int defenderLevel, int damageDealt, ExperienceRules? rules = null)
        {
            rules ??= new ExperienceRules();
            int diff = Math.Max(-rules.MaxLevelDiff, Math.Min(rules.MaxLevelDiff, defenderLevel - attackerLevel));
            int exp = (didKill ? rules.BaseKillExp : rules.BaseChipExp)
                + Math.Max(0, diff) * rules.LevelDiffBonus
                + (didKill ? 0 : Math.Min(damageDealt, 20) / 4);
            return Math.Max(rules.MinExp, Math.Min(rules.MaxExp, exp));
        }

        /// <summary>Adds experience and applies level-ups while the threshold is met.</summary>
        /// <param name="unit">Unit gaining experience (mutates level, stats, and HP).</param>
        /// <param name="exp">Experience points to add.</param>
        /// <param name="growths">Per-stat growth chances consumed on each level-up.</param>
        /// <param name="growthRng">Growth RNG stream (1RN per stat per level).</param>
        /// <param name="rules">Tunable constants (defaults to GBA-flavored values).</param>
        public static void AddExp(Unit unit, int exp, GrowthRates growths, SeededRng growthRng, ExperienceRules? rules = null)
        {
            if (unit == null) throw new ArgumentNullException(nameof(unit));
            if (growthRng == null) throw new ArgumentNullException(nameof(growthRng));
            rules ??= new ExperienceRules();
            unit.Experience += exp;
            while (unit.Experience >= rules.ExpPerLevel)
            {
                unit.Experience -= rules.ExpPerLevel;
                LevelUp(unit, growths, growthRng);
            }
        }

        /// <summary>Raises one level, rolling each growth chance and healing MaxHP gains.</summary>
        /// <param name="unit">Unit leveling up (mutates level, stats, and HP).</param>
        /// <param name="growths">Per-stat growth chances for this level.</param>
        /// <param name="growthRng">Growth RNG stream (1RN per stat).</param>
        public static void LevelUp(Unit unit, GrowthRates growths, SeededRng growthRng)
        {
            if (unit == null) throw new ArgumentNullException(nameof(unit));
            if (growths == null) throw new ArgumentNullException(nameof(growths));
            if (growthRng == null) throw new ArgumentNullException(nameof(growthRng));
            unit.Level++;
            if (growthRng.RollHit1RN(growths.HP)) { unit.Stats.MaxHP++; unit.CurrentHP++; }
            if (growthRng.RollHit1RN(growths.Power)) unit.Stats.Power++;
            if (growthRng.RollHit1RN(growths.Skill)) unit.Stats.Skill++;
            if (growthRng.RollHit1RN(growths.Speed)) unit.Stats.Speed++;
            if (growthRng.RollHit1RN(growths.Luck)) unit.Stats.Luck++;
            if (growthRng.RollHit1RN(growths.Defense)) unit.Stats.Defense++;
            if (growthRng.RollHit1RN(growths.Resistance)) unit.Stats.Resistance++;
        }
    }
}
