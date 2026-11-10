using System;
using System.Collections.Generic;
using SrpgCore.Data;
using SrpgCore.Grid;
using SrpgCore.Regions;

namespace SrpgCore.Tactics
{
    /// <summary>
    /// Host-callable trigger variable builders for the battle loop. Each helper
    /// returns the vars dictionary a GameEvent condition reads (nested
    /// string/object dictionaries, same shape as the hand-built death vars in
    /// Verify: unit.nid, unit2.nid, item, position.x/y, region). Pure: no mutation,
    /// no RNG. Hosts feed the result to EventDispatcher.Dispatch with the matching
    /// EventTrigger (CombatStart, CombatEnd, UnitLevelUp, UnitWait, UnitDeath, RegionEntered).
    /// </summary>
    public static class BattleTriggers
    {
        private static Dictionary<string, object?> UnitVars(Unit unit) =>
            new Dictionary<string, object?>
            {
                ["nid"] = unit.Nid,
                ["level"] = unit.Level,
                ["hp"] = unit.CurrentHP,
            };

        private static Dictionary<string, object?> PositionVars(GridPoint pos) =>
            new Dictionary<string, object?>
            {
                ["x"] = pos.X,
                ["y"] = pos.Y,
            };

        private static Dictionary<string, object?> RegionVars(Region region) =>
            new Dictionary<string, object?>
            {
                ["name"] = region.Name,
                ["kind"] = region.Kind.ToString().ToLowerInvariant(),
            };

        /// <summary>Vars for EventTrigger.CombatStart: unit, unit2, item, position.</summary>
        /// <param name="attacker">Attacking unit.</param>
        /// <param name="defender">Defending unit.</param>
        /// <param name="weaponKey">Attacking weapon key.</param>
        /// <param name="pos">Tile the attacker strikes from.</param>
        /// <returns>Vars dictionary for the CombatStart trigger.</returns>
        public static Dictionary<string, object?> CombatStartVars(
            Unit attacker, Unit defender, string weaponKey, GridPoint pos)
        {
            if (attacker == null) throw new ArgumentNullException(nameof(attacker));
            if (defender == null) throw new ArgumentNullException(nameof(defender));
            return new Dictionary<string, object?>
            {
                ["unit"] = UnitVars(attacker),
                ["unit2"] = UnitVars(defender),
                ["item"] = weaponKey ?? "",
                ["position"] = PositionVars(pos),
            };
        }

        /// <summary>Vars for EventTrigger.CombatEnd: combat-start vars plus defender_died.</summary>
        /// <param name="attacker">Attacking unit.</param>
        /// <param name="defender">Defending unit.</param>
        /// <param name="weaponKey">Attacking weapon key.</param>
        /// <param name="pos">Tile the attacker struck from.</param>
        /// <param name="defenderDied">True when the defender fell in the exchange.</param>
        /// <returns>Vars dictionary for the CombatEnd trigger.</returns>
        public static Dictionary<string, object?> CombatEndVars(
            Unit attacker, Unit defender, string weaponKey, GridPoint pos, bool defenderDied)
        {
            var vars = CombatStartVars(attacker, defender, weaponKey, pos);
            vars["defender_died"] = defenderDied;
            return vars;
        }

        /// <summary>Vars for EventTrigger.UnitLevelUp: unit (nid, level, hp).</summary>
        /// <param name="unit">Unit that leveled up.</param>
        /// <returns>Vars dictionary for the UnitLevelUp trigger.</returns>
        public static Dictionary<string, object?> LevelUpVars(Unit unit)
        {
            if (unit == null) throw new ArgumentNullException(nameof(unit));
            return new Dictionary<string, object?>
            {
                ["unit"] = UnitVars(unit),
            };
        }

        /// <summary>Vars for EventTrigger.UnitWait: unit, position.</summary>
        /// <param name="unit">Unit that ended its turn.</param>
        /// <param name="pos">Tile the unit waited on.</param>
        /// <returns>Vars dictionary for the UnitWait trigger.</returns>
        public static Dictionary<string, object?> UnitWaitVars(Unit unit, GridPoint pos)
        {
            if (unit == null) throw new ArgumentNullException(nameof(unit));
            return new Dictionary<string, object?>
            {
                ["unit"] = UnitVars(unit),
                ["position"] = PositionVars(pos),
            };
        }

        /// <summary>Vars for EventTrigger.UnitDeath: unit, position.</summary>
        /// <param name="unit">Unit that died.</param>
        /// <param name="pos">Tile the unit died on.</param>
        /// <returns>Vars dictionary for the UnitDeath trigger.</returns>
        public static Dictionary<string, object?> UnitDeathVars(Unit unit, GridPoint pos)
        {
            if (unit == null) throw new ArgumentNullException(nameof(unit));
            return new Dictionary<string, object?>
            {
                ["unit"] = UnitVars(unit),
                ["position"] = PositionVars(pos),
            };
        }

        /// <summary>Vars for EventTrigger.RegionEntered: unit, region, position.</summary>
        /// <param name="unit">Unit that entered the region.</param>
        /// <param name="region">Region entered.</param>
        /// <param name="pos">Tile inside the region.</param>
        /// <returns>Vars dictionary for the RegionEntered trigger.</returns>
        public static Dictionary<string, object?> RegionEnteredVars(Unit unit, Region region, GridPoint pos)
        {
            if (unit == null) throw new ArgumentNullException(nameof(unit));
            if (region == null) throw new ArgumentNullException(nameof(region));
            return new Dictionary<string, object?>
            {
                ["unit"] = UnitVars(unit),
                ["region"] = RegionVars(region),
                ["position"] = PositionVars(pos),
            };
        }

        /// <summary>Vars for phase triggers carrying the turn number.</summary>
        /// <param name="turn">Current turn number.</param>
        /// <returns>Vars dictionary containing turn.</returns>
        public static Dictionary<string, object?> TurnVars(int turn)
        {
            return new Dictionary<string, object?>
            {
                ["turn"] = (long)turn,
            };
        }

        /// <summary>
        /// Region-entry detection for EventTrigger.RegionEntered: reports (unit, region,
        /// pos) for every placed unit whose current tile is inside a region its previous
        /// tile was outside of. Units with no previous position (fresh spawns) and units
        /// that stayed put or left a region never report. Pure; order follows placements.
        /// </summary>
        /// <param name="placed">Current placements to test.</param>
        /// <param name="previousPositions">Tile per unit nid before movement.</param>
        /// <param name="regions">Regions to test entries against.</param>
        /// <returns>One entry per unit and region newly entered.</returns>
        public static List<(PlacedUnit unit, Region region, GridPoint pos)> DetectRegionEntries(
            IEnumerable<PlacedUnit> placed,
            IReadOnlyDictionary<string, GridPoint> previousPositions,
            IEnumerable<Region> regions)
        {
            if (placed == null) throw new ArgumentNullException(nameof(placed));
            if (previousPositions == null) throw new ArgumentNullException(nameof(previousPositions));
            if (regions == null) throw new ArgumentNullException(nameof(regions));

            var entries = new List<(PlacedUnit unit, Region region, GridPoint pos)>();
            foreach (PlacedUnit p in placed)
            {
                if (p == null) continue;
                if (!previousPositions.TryGetValue(p.Unit.Nid, out GridPoint prev)) continue;
                if (prev.Equals(p.Position)) continue;
                foreach (Region r in regions)
                {
                    if (r == null) continue;
                    if (r.Contains(p.Position) && !r.Contains(prev))
                        entries.Add((p, r, p.Position));
                }
            }
            return entries;
        }
    }
}
