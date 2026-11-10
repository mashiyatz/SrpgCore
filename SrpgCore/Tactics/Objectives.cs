using System;
using System.Collections.Generic;
using SrpgCore.Data;

namespace SrpgCore.Tactics
{
    /// <summary>
    /// Standard chapter-end evaluators: rout, survive-N, protect-tags, seize, escape.
    /// Pure predicates over unit state; the TurnManager/event layer decides when to poll
    /// (after every elimination checkpoint; survive only on turn advance).
    /// </summary>
    public static class Objectives
    {
        /// <summary>Rout completes when no live unit of a side remains.</summary>
        /// <param name="units">Units to scan for survivors.</param>
        /// <param name="side">Side tested for elimination.</param>
        /// <returns>True when that side has no live unit left.</returns>
        public static bool IsRouted(IEnumerable<Unit> units, Affiliation side)
        {
            foreach (Unit u in units)
                if (u.Side == side && u.IsAlive) return false;
            return true;
        }

        /// <summary>"Survive N" completes N full rounds: victory once TurnNumber passes N.</summary>
        /// <param name="turnNumber">Current turn number (starts at 1).</param>
        /// <param name="turnsToSurvive">Full rounds that must complete.</param>
        /// <returns>True once the required rounds have elapsed.</returns>
        public static bool HasSurvived(int turnNumber, int turnsToSurvive) => turnNumber > turnsToSurvive;

        /// <summary>
        /// Protect fails when a listed tag (unit nid) is dead AND already spawned.
        /// Future spawns (spawnTurn &gt; turnNumber) never trip it.
        /// Use &lt;= (not ==) for spawn comparison: skipped turn numbers must not drop the check.
        /// </summary>
        /// <param name="units">Units keyed by nid for liveness lookup.</param>
        /// <param name="tags">Protected unit nids to check.</param>
        /// <param name="turnNumber">Current turn number for the spawned comparison.</param>
        /// <param name="spawnTurnOf">Maps a tag to its spawn turn.</param>
        /// <returns>True when a spawned protected unit is dead or missing.</returns>
        public static bool IsProtectFailed(
            IEnumerable<Unit> units,
            IEnumerable<string> tags,
            int turnNumber,
            Func<string, int> spawnTurnOf)
        {
            if (spawnTurnOf == null) throw new ArgumentNullException(nameof(spawnTurnOf));
            var byNid = new Dictionary<string, Unit>();
            foreach (Unit u in units) byNid[u.Nid] = u;

            foreach (string tag in tags)
            {
                bool spawned = spawnTurnOf(tag) <= turnNumber;
                bool alive = byNid.TryGetValue(tag, out Unit? u) && u.IsAlive;
                if (spawned && !alive) return true;
            }
            return false;
        }

        /// <summary>
        /// Seize succeeds when the listed unit is alive and standing on the region.
        /// Pure over placements; the caller resolves the region by name.
        /// </summary>
        /// <param name="placed">Placements scanned for the seizing unit.</param>
        /// <param name="unitNid">Nid of the unit that must seize the region.</param>
        /// <param name="region">Region the unit must stand on.</param>
        /// <returns>True when that live unit stands on the region.</returns>
        public static bool IsSeized(
            IEnumerable<PlacedUnit> placed,
            string unitNid,
            Regions.Region region)
        {
            if (region == null) throw new ArgumentNullException(nameof(region));
            foreach (PlacedUnit p in placed)
                if (p.Unit.Nid == unitNid && p.Unit.IsAlive && region.Contains(p.Position))
                    return true;
            return false;
        }

        /// <summary>
        /// Escape succeeds when the listed unit is alive and standing on the escape region.
        /// Mirrors seize semantics; LT Maker escape is reaching a designated region alive.
        /// </summary>
        /// <param name="placed">Placements scanned for the escaping unit.</param>
        /// <param name="unitNid">Nid of the unit that must escape through the region.</param>
        /// <param name="region">Escape region the unit must stand on.</param>
        /// <returns>True when that live unit stands on the escape region.</returns>
        public static bool IsEscape(
            IEnumerable<PlacedUnit> placed,
            string unitNid,
            Regions.Region region)
        {
            if (region == null) throw new ArgumentNullException(nameof(region));
            foreach (PlacedUnit p in placed)
                if (p.Unit.Nid == unitNid && p.Unit.IsAlive && region.Contains(p.Position))
                    return true;
            return false;
        }
    }
}
