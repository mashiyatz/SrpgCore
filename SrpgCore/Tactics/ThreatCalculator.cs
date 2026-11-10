using System;
using System.Collections.Generic;
using SrpgCore.Grid;

namespace SrpgCore.Tactics
{
    /// <summary>
    /// Danger-zone computation (Battlefield helper): ThreatRange per enemy and
    /// SideThreat union, optionally clipped by FogCalculator.VisibleTiles. Values
    /// are tile sets; callers supply movement and weapon range via LoadoutOf.
    /// </summary>
    public static class ThreatCalculator
    {
        /// <summary>Danger zone for one enemy: attack rings over every tile it can reach.</summary>
        /// <param name="field">Battlefield holding board and placements.</param>
        /// <param name="enemy">Placed enemy whose threat is computed.</param>
        /// <param name="movementGroup">Movement group key for reachability.</param>
        /// <param name="movePoints">Movement allowance.</param>
        /// <param name="minRange">Weapon minimum range.</param>
        /// <param name="maxRange">Weapon maximum range.</param>
        /// <returns>Every tile the enemy could strike.</returns>
        public static HashSet<GridPoint> ThreatRange(
            Battlefield field, PlacedUnit enemy,
            string movementGroup, int movePoints, int minRange, int maxRange)
        {
            var threat = new HashSet<GridPoint>();
            threat.UnionWith(RangeFinder.AttackTiles(field.Board, enemy.Position, minRange, maxRange));
            foreach (GridPoint tile in field.Reachable(enemy, movementGroup, movePoints))
                threat.UnionWith(RangeFinder.AttackTiles(field.Board, tile, minRange, maxRange));
            return threat;
        }

        /// <summary>Resolves a unit's movement group, move allowance, and weapon ranges.</summary>
        /// <param name="unit">Placed unit whose loadout is requested.</param>
        /// <returns>Movement group, move allowance, minimum range, and maximum range.</returns>
        public delegate (string group, int move, int minRange, int maxRange) LoadoutOf(PlacedUnit unit);

        /// <summary>Unions threat ranges of many enemies, optionally clipped to sight.</summary>
        /// <param name="field">Battlefield holding board and placements.</param>
        /// <param name="enemies">Enemies whose threat is unioned.</param>
        /// <param name="loadout">Resolves each enemy's movement and range loadout.</param>
        /// <param name="visibleOnly">Optional visible set; threat outside it is dropped.</param>
        /// <returns>Combined danger zone.</returns>
        public static HashSet<GridPoint> SideThreat(
            Battlefield field, IEnumerable<PlacedUnit> enemies, LoadoutOf loadout,
            HashSet<GridPoint>? visibleOnly = null)
        {
            var values = SideThreatValues(field, enemies, loadout, visibleOnly);
            return new HashSet<GridPoint>(values.Keys);
        }

        /// <summary>Counts covering enemies per threatened tile.</summary>
        /// <param name="field">Battlefield holding board and placements.</param>
        /// <param name="enemies">Enemies whose threat is counted.</param>
        /// <param name="loadout">Resolves each enemy's movement and range loadout.</param>
        /// <param name="visibleOnly">Optional visible set; threat outside it is dropped.</param>
        /// <returns>Threat count per tile.</returns>
        public static Dictionary<GridPoint, int> SideThreatValues(
            Battlefield field, IEnumerable<PlacedUnit> enemies, LoadoutOf loadout,
            HashSet<GridPoint>? visibleOnly = null)
        {
            if (field == null) throw new ArgumentNullException(nameof(field));
            if (enemies == null) throw new ArgumentNullException(nameof(enemies));
            if (loadout == null) throw new ArgumentNullException(nameof(loadout));
            var values = new Dictionary<GridPoint, int>();
            foreach (PlacedUnit e in enemies)
            {
                if (!e.Unit.IsAlive) continue;
                var (group, move, minRange, maxRange) = loadout(e);
                foreach (GridPoint t in ThreatRange(field, e, group, move, minRange, maxRange))
                {
                    if (visibleOnly != null && !visibleOnly.Contains(t)) continue;
                    values.TryGetValue(t, out int n);
                    values[t] = n + 1;
                }
            }
            return values;
        }
    }
}
