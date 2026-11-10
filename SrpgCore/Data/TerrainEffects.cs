using System;
using SrpgCore.Data;
using SrpgCore.Grid;

namespace SrpgCore.Data
{
    /// <summary>
    /// Per-round terrain HP swing (fort heal, poison/marsh damage). Pure: returns the
    /// applied delta and mutates CurrentHP, clamped to [0, MaxHP]. Callers decide timing
    /// (once per round per team, at phase boundaries — not per activation).
    /// Nonzero percents always move at least 1 HP (ceiling rounding).
    /// </summary>
    public static class TerrainEffects
    {
        /// <summary>Applies terrain HP swing to one unit, clamped at zero.</summary>
        /// <param name="unit">The unit standing on the tile (mutates CurrentHP).</param>
        /// <param name="terrain">The tile terrain definition carrying HpEffectPercent.</param>
        /// <returns>Signed HP delta applied, zero when no effect or the unit is dead.</returns>
        public static int Apply(Unit unit, TerrainType terrain)
        {
            if (unit == null) throw new ArgumentNullException(nameof(unit));
            if (terrain == null) throw new ArgumentNullException(nameof(terrain));
            if (terrain.HpEffectPercent == 0 || !unit.IsAlive) return 0;

            int magnitude = (int)Math.Ceiling(Math.Abs(terrain.HpEffectPercent) / 100.0 * unit.Stats.MaxHP);
            int delta = Math.Sign(terrain.HpEffectPercent) * Math.Max(1, magnitude);
            unit.CurrentHP = Math.Max(0, Math.Min(unit.Stats.MaxHP, unit.CurrentHP + delta));
            return delta;
        }
    }
}
