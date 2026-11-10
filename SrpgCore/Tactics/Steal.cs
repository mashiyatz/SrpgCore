using System;
using SrpgCore.Data;
using SrpgCore.Grid;

namespace SrpgCore.Tactics
{
    /// <summary>GBA-style steal: adjacent thief with higher speed takes a light stealable item.</summary>
    public static class Steal
    {
        /// <summary>True when thief can steal weapon at slot from target.</summary>
        /// <param name="thief">Unit doing the stealing.</param>
        /// <param name="target">Unit being stolen from.</param>
        /// <param name="thiefPos">Thief tile.</param>
        /// <param name="targetPos">Target tile.</param>
        /// <param name="weaponIndex">Inventory index on target.</param>
        /// <returns>True when adjacent, alive, faster, and item light enough.</returns>
        public static bool CanSteal(Unit thief, Unit target, GridPoint thiefPos, GridPoint targetPos, int weaponIndex)
        {
            if (thief == null || target == null) return false;
            if (!thief.IsAlive || !target.IsAlive) return false;
            if (thiefPos.ManhattanDistance(targetPos) != 1) return false;
            if (thief.Stats.Speed <= target.Stats.Speed) return false;
            if (weaponIndex < 0 || weaponIndex >= target.Inventory.Count) return false;
            var w = target.Inventory[weaponIndex].Weapon;
            if (!w.CanSteal) return false;
            if (w.Weight > thief.Stats.Constitution) return false;
            return true;
        }

        /// <summary>Moves inventory entry from target to thief.</summary>
        /// <param name="thief">Thief unit receiving the item.</param>
        /// <param name="target">Target unit losing the item.</param>
        /// <param name="weaponIndex">Slot on target.</param>
        public static void Execute(Unit thief, Unit target, int weaponIndex)
        {
            if (thief == null) throw new ArgumentNullException(nameof(thief));
            if (target == null) throw new ArgumentNullException(nameof(target));
            var entry = target.Inventory[weaponIndex];
            target.Inventory.RemoveAt(weaponIndex);
            thief.Inventory.Add(entry);
        }

        /// <summary>Battlefield overload that validates adjacency and speed.</summary>
        /// <param name="field">Battlefield for position lookup.</param>
        /// <param name="thief">Placed thief.</param>
        /// <param name="target">Placed target.</param>
        /// <param name="weaponKey">Weapon key to steal (first match).</param>
        public static void Execute(Battlefield field, PlacedUnit thief, PlacedUnit target, string weaponKey)
        {
            if (field == null) throw new ArgumentNullException(nameof(field));
            if (thief == null) throw new ArgumentNullException(nameof(thief));
            if (target == null) throw new ArgumentNullException(nameof(target));
            int idx = -1;
            for (int i = 0; i < target.Unit.Inventory.Count; i++)
                if (target.Unit.Inventory[i].Weapon.Key == weaponKey) { idx = i; break; }
            if (idx < 0) throw new InvalidOperationException($"'{target.Unit.Nid}' has no '{weaponKey}'.");
            if (!CanSteal(thief.Unit, target.Unit, thief.Position, target.Position, idx))
                throw new InvalidOperationException("Steal conditions not met.");
            Execute(thief.Unit, target.Unit, idx);
        }
    }
}
