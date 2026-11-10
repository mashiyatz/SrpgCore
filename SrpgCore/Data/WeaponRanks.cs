using System;
using System.Collections.Generic;

namespace SrpgCore.Data
{
    /// <summary>
    /// Weapon experience (GBA-flavored fork): each combat use banks points per weapon type
    /// on the wielder; rank letters gate equipping (a unit needs the letter rank for
    /// the weapon it holds). Thresholds are data here, not scattered constants.
    /// Fork thresholds are E 0, D 20, C 50, B 80, A 120, S 180; GBA FE7/8 uses
    /// 0, 31, 71, 121, 181, 251 (S at 251). Wexp is capped at 255 (byte max) to
    /// match GBA and prevent overflow. Deterministic: pure functions over ints, no RNG.
    /// </summary>
    public static class WeaponRanks
    {
        /// <summary>Reads the wexp floor required for a rank letter.</summary>
        /// <param name="rank">Rank letter to look up.</param>
        /// <returns>Wexp points needed to reach a rank (E 0, D 20, C 50, B 80, A 120, S 180).</returns>
        public static int PointsForRank(WeaponRank rank)
        {
            switch (rank)
            {
                case WeaponRank.E: return 0;
                case WeaponRank.D: return 20;
                case WeaponRank.C: return 50;
                case WeaponRank.B: return 80;
                case WeaponRank.A: return 120;
                case WeaponRank.S: return 180;
                default: throw new ArgumentOutOfRangeException(nameof(rank));
            }
        }

        /// <summary>Reads the highest rank letter unlocked by a wexp total.</summary>
        /// <param name="points">Banked wexp total, negatives read as E.</param>
        /// <returns>Highest rank whose threshold points meet or beat (negatives read as E).</returns>
        public static WeaponRank RankFor(int points)
        {
            if (points >= PointsForRank(WeaponRank.S)) return WeaponRank.S;
            if (points >= PointsForRank(WeaponRank.A)) return WeaponRank.A;
            if (points >= PointsForRank(WeaponRank.B)) return WeaponRank.B;
            if (points >= PointsForRank(WeaponRank.C)) return WeaponRank.C;
            if (points >= PointsForRank(WeaponRank.D)) return WeaponRank.D;
            return WeaponRank.E;
        }

        /// <summary>Tests whether a wexp total unlocks a required rank.</summary>
        /// <param name="points">Banked wexp total.</param>
        /// <param name="required">Rank letter the weapon demands.</param>
        /// <returns>True when points meet the required rank's threshold.</returns>
        public static bool CanEquip(int points, WeaponRank required) =>
            points >= PointsForRank(required);

        /// <summary>Current wexp for a type on a unit (0 when never used).</summary>
        /// <param name="unit">Unit whose Wexp map to read.</param>
        /// <param name="type">Weapon family to look up.</param>
        /// <returns>Banked points for the type, or zero.</returns>
        public static int PointsOf(Unit unit, WeaponType type)
        {
            if (unit == null) throw new ArgumentNullException(nameof(unit));
            return unit.Wexp.TryGetValue(type, out int p) ? p : 0;
        }

        /// <summary>Banks wexp on the unit, capped at 255 (GBA byte max); returns the new total for the type.</summary>
        /// <param name="unit">Unit whose Wexp map to mutate.</param>
        /// <param name="type">Weapon family to credit.</param>
        /// <param name="points">Nonnegative points to add.</param>
        /// <returns>New total for the type after banking, clamped to 255.</returns>
        public static int AddWexp(Unit unit, WeaponType type, int points)
        {
            if (unit == null) throw new ArgumentNullException(nameof(unit));
            if (points < 0) throw new ArgumentOutOfRangeException(nameof(points));
            long total = (long)PointsOf(unit, type) + points;
            if (total > 255) total = 255;
            unit.Wexp[type] = (int)total;
            return (int)total;
        }
    }
}
