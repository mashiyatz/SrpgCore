using System.Collections.Generic;
using SrpgCore.Combat;
using SrpgCore.Grid;

namespace SrpgCore.Tactics
{
    /// <summary>
    /// Ally-phase brain: the same greedy <see cref="EnemyAi"/> machinery, but with
    /// counterWeight 0.25 instead of the enemy default 0.5 — GBA greens wade in and
    /// discount counter-risk (tuning, not a formula change). Hosts call this for
    /// Affiliation.Ally units on the ally phase; fog filtering passes straight
    /// through to the enemy implementation.
    /// </summary>
    public static class AllyAi
    {
        /// <summary>Chooses the greedy move-then-strike action for one ally unit with counter weight 0.25.</summary>
        /// <param name="field">Board plus placements (reads blocking and ally tiles).</param>
        /// <param name="self">Acting ally placement (position and inventory are read).</param>
        /// <param name="movementGroup">Movement group key for reachable tiles.</param>
        /// <param name="movePoints">Movement budget for this turn.</param>
        /// <param name="terrainOf">Optional avoid and defense lookup per tile.</param>
        /// <param name="defenderWeaponOf">Optional foe weapon picker by distance.</param>
        /// <param name="rules">Combat tunables (defaults when null).</param>
        /// <param name="supports">Optional bond bonuses for strike and counter forecasts.</param>
        /// <param name="visibleTiles">Optional fog set with foes outside skipped.</param>
        /// <returns>The chosen move target, weapon, and foe (or wait).</returns>
        public static AiAction Decide(
            Battlefield field, PlacedUnit self,
            string movementGroup, int movePoints,
            EnemyAi.TerrainOf? terrainOf = null,
            EnemyAi.DefenderWeaponOf? defenderWeaponOf = null,
            CombatRules? rules = null,
            SupportLedger? supports = null,
            HashSet<GridPoint>? visibleTiles = null) =>
            EnemyAi.Decide(field, self, movementGroup, movePoints,
                terrainOf, defenderWeaponOf, 0.25, rules, supports, visibleTiles);
    }
}
