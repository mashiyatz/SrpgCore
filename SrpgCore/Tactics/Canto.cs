using System;

namespace SrpgCore.Tactics
{
    /// <summary>
    /// Canto (GBA mounted/flying move-again): after acting, a skilled unit may spend
    /// whatever movement it had left before the action. Host-tracked: hosts own the
    /// acted flags and the spent-move accounting — this class is the shared predicate
    /// plus the leftover-move math, so every host grants the same tiles. Canto status
    /// travels as skill id "canto" (markdown `skill:canto`, matched
    /// case-insensitively). Hosts pass laden totals through Rescue.EffectiveMove.
    /// </summary>
    public static class Canto
    {
        /// <summary>Skill id granting canto repositioning.</summary>
        public const string CantoSkillId = "canto";

        /// <summary>Reports whether a unit carries the canto skill (case-insensitive).</summary>
        /// <param name="unit">Unit to test (null reads as no canto).</param>
        /// <returns>True when the unit lists the canto skill.</returns>
        public static bool HasCanto(Data.Unit unit)
        {
            if (unit == null) return false;
            foreach (string s in unit.Skills)
                if (string.Equals(s, CantoSkillId, StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        /// <summary>Leftover movement after acting (total minus spent before the action).</summary>
        /// <param name="moveTotal">Full movement total for the turn.</param>
        /// <param name="moveSpentBeforeAction">Movement already spent before acting.</param>
        /// <returns>Remaining movement clamped at zero.</returns>
        public static int RemainingMove(int moveTotal, int moveSpentBeforeAction)
        {
            if (moveTotal < 0) throw new ArgumentOutOfRangeException(nameof(moveTotal));
            if (moveSpentBeforeAction < 0) throw new ArgumentOutOfRangeException(nameof(moveSpentBeforeAction));
            return Math.Max(0, moveTotal - moveSpentBeforeAction);
        }

        /// <summary>Reports whether a unit may canto after acting (skill plus leftover move).</summary>
        /// <param name="unit">Unit to test.</param>
        /// <param name="moveTotal">Full movement total for the turn.</param>
        /// <param name="moveSpentBeforeAction">Movement already spent before acting.</param>
        /// <returns>True when the skilled unit has movement left to spend.</returns>
        public static bool CanCanto(Data.Unit unit, int moveTotal, int moveSpentBeforeAction) =>
            HasCanto(unit) && RemainingMove(moveTotal, moveSpentBeforeAction) > 0;
    }
}
