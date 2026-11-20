using System;
using SrpgCore.Data;
using SrpgCore.Random;

namespace SrpgCore.Tactics
{
    /// <summary>
    /// Post-combat experience grant: computes the payout from the exchange and banks
    /// it on the attacker (level-ups roll the growth RNG stream). Hosts call this
    /// after CombatResolver.Resolve with the summed damage dealt and the kill flag.
    /// Pure except the growth RNG stream and attacker mutation (same pattern as
    /// CombatResolver: same inputs + same seed always give the same result).
    /// </summary>
    public static class CombatGains
    {
        /// <summary>Resolves growth rates: unit override wins, then class, then host fallback.</summary>
        /// <param name="unit">Unit leveling up.</param>
        /// <param name="db">Class database (null when unavailable).</param>
        /// <param name="fallback">Host default growths.</param>
        /// <returns>Growth rates to roll.</returns>
        public static GrowthRates ResolveGrowths(Unit unit, Data.ClassDatabase? db, GrowthRates fallback)
        {
            if (unit == null) throw new ArgumentNullException(nameof(unit));
            if (fallback == null) throw new ArgumentNullException(nameof(fallback));
            if (unit.Growths != null) return unit.Growths;
            if (db != null && db.TryGet(unit.ClassKey, out UnitClass? cls) && cls?.Growths != null)
                return cls.Growths;
            return fallback;
        }

        /// <summary>Grants post-combat experience and weapon experience to the attacker. Growth precedence is unit override, then class database, then the host fallback (hosts without a class table pass null and get unit-or-default).</summary>
        /// <param name="attacker">Unit receiving experience.</param>
        /// <param name="defender">Foe whose level shapes the payout.</param>
        /// <param name="damageDealt">Summed damage dealt in the exchange.</param>
        /// <param name="didKill">True when the exchange killed the defender.</param>
        /// <param name="growths">Growth rates for level-up rolls.</param>
        /// <param name="growthRng">Seeded growth stream.</param>
        /// <param name="rules">Experience tunables (defaults when null).</param>
        /// <param name="usedWeapon">Weapon to grant weapon experience to (skipped when null).</param>
        /// <param name="wexpOnUse">Weapon experience granted on a non-killing use.</param>
        /// <param name="wexpOnKill">Weapon experience granted on a kill.</param>
        /// <returns>(expGained, levelsGained) for the attacker.</returns>
        public static (int expGained, int levelsGained) ApplyAfterCombat(
            Unit attacker, Unit defender, int damageDealt, bool didKill,
            GrowthRates growths, SeededRng growthRng, ExperienceRules? rules = null,
            Weapon? usedWeapon = null, int wexpOnUse = 1, int wexpOnKill = 3)
        {
            if (attacker == null) throw new ArgumentNullException(nameof(attacker));
            if (defender == null) throw new ArgumentNullException(nameof(defender));
            if (growths == null) throw new ArgumentNullException(nameof(growths));
            if (growthRng == null) throw new ArgumentNullException(nameof(growthRng));

            int exp = Experience.Gain(didKill, attacker.Level, defender.Level, damageDealt, rules);
            int before = attacker.Level;
            Experience.AddExp(attacker, exp, growths, growthRng, rules);
            if (usedWeapon != null)
                WeaponRanks.AddWexp(attacker, usedWeapon.Type, didKill ? wexpOnKill : wexpOnUse);
            return (exp, attacker.Level - before);
        }
    }
}
