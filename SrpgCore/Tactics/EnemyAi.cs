using System;
using System.Collections.Generic;
using SrpgCore.Combat;
using SrpgCore.Data;
using SrpgCore.Grid;

namespace SrpgCore.Tactics
{
    /// <summary>Chosen move-then-strike action for one AI unit (or wait).</summary>
    public sealed class AiAction
    {
        /// <summary>Destination tile (null means act or wait from the current tile).</summary>
        public GridPoint? MoveTo { get; set; }   // null = attack/wait from current tile
        /// <summary>Chosen weapon entry (null when waiting or moving only).</summary>
        public InventoryEntry? Weapon { get; set; }
        /// <summary>Chosen foe (null means wait).</summary>
        public PlacedUnit? Target { get; set; }
        /// <summary>Faced tile after acting (null means keep current facing).</summary>
        public GridPoint? Facing { get; set; }
        /// <summary>True when no foe is targeted.</summary>
        public bool IsWait => Target == null;

        /// <summary>Builds the wait action (no move, no weapon, no target).</summary>
        /// <returns>An action whose target is null.</returns>
        public static AiAction Wait() => new AiAction();
    }

    /// <summary>
    /// Greedy enemy brain with lethality-first scoring:
    /// (1) strike from current tile, (2) move then strike, (3) approach nearest foe.
    /// Enumerates every usable weapon across every reachable tile and foe. Terrain-aware
    /// when a terrain lookup is supplied; otherwise all bonuses read 0. Counter-risk is
    /// subtracted as expected counter damage times CounterWeight (0 = ignore, 1 = full).
    /// Pass a SupportLedger to weight bonded hit/avoid bonuses into both the strike
    /// and the counter-risk forecasts. Deterministic: ties break by least movement spent then nearest target then move tile foe nid and weapon key.
    /// </summary>
    public static class EnemyAi
    {
        /// <summary>Avoid and defense bonuses for a unit standing on a tile.</summary>
        /// <param name="unit">Placed unit the bonuses apply to.</param>
        /// <param name="tile">Tile the bonuses are read from.</param>
        /// <returns>Avoid and defense bonuses at that tile.</returns>
        public delegate (int avoid, int defense) TerrainOf(PlacedUnit unit, GridPoint tile);
        /// <summary>Picks the weapon a defender answers with at a distance.</summary>
        /// <param name="foe">Defending placement.</param>
        /// <param name="distance">Manhattan distance of the exchange.</param>
        /// <returns>The defending weapon or null when the foe cannot counter.</returns>
        public delegate Weapon? DefenderWeaponOf(PlacedUnit foe, int distance);

        /// <summary>Chooses the greedy move-then-strike action for one enemy unit.</summary>
        /// <param name="field">Board plus placements (reads blocking and ally tiles).</param>
        /// <param name="self">Acting enemy placement (position and inventory are read).</param>
        /// <param name="movementGroup">Movement group key for reachable tiles.</param>
        /// <param name="movePoints">Movement budget for this turn.</param>
        /// <param name="rules">Combat tunables (defaults when null).</param>
        /// <returns>The chosen move target, weapon, and foe (or wait).</returns>
        public static AiAction Decide(
            Battlefield field, PlacedUnit self,
            string movementGroup, int movePoints,
            CombatRules? rules = null)
        {
            return Decide(field, self, movementGroup, movePoints, null, null, 0.0, rules);
        }

        /// <summary>Chooses the greedy terrain-aware action with counter-risk weighting and fog filtering.</summary>
        /// <param name="field">Board plus placements (reads blocking and ally tiles).</param>
        /// <param name="self">Acting enemy placement (position and inventory are read).</param>
        /// <param name="movementGroup">Movement group key for reachable tiles.</param>
        /// <param name="movePoints">Movement budget for this turn.</param>
        /// <param name="terrainOf">Optional avoid and defense lookup per tile.</param>
        /// <param name="defenderWeaponOf">Optional foe weapon picker by distance.</param>
        /// <param name="counterWeight">Expected counter-damage weight from 0 to 1.</param>
        /// <param name="rules">Combat tunables (defaults when null).</param>
        /// <param name="supports">Optional bond bonuses for strike and counter forecasts.</param>
        /// <param name="visibleTiles">Optional fog set with foes outside skipped.</param>
        /// <param name="aiRules">Scoring weights, or null for defaults.</param>
        /// <returns>The chosen move target, weapon, and foe (or wait).</returns>
        public static AiAction Decide(
            Battlefield field, PlacedUnit self,
            string movementGroup, int movePoints,
            TerrainOf? terrainOf,
            DefenderWeaponOf? defenderWeaponOf,
            double counterWeight = 0.5,
            CombatRules? rules = null,
            SupportLedger? supports = null,
            HashSet<GridPoint>? visibleTiles = null,
            AiRules? aiRules = null)
        {
            if (field == null) throw new ArgumentNullException(nameof(field));
            rules ??= new CombatRules();
            aiRules ??= new AiRules();
            double effectiveCounterWeight = aiRules.CounterWeight != 0.5 ? aiRules.CounterWeight : counterWeight;

            if (string.Equals(self.Unit.AiType, "hold", StringComparison.OrdinalIgnoreCase))
                return AiAction.Wait();
            if (string.Equals(self.Unit.AiType, "patrol", StringComparison.OrdinalIgnoreCase))
            {
                var target = self.Position.Equals(self.Unit.PatrolA) ? self.Unit.PatrolB : self.Unit.PatrolA;
                var reachable = field.Reachable(self, movementGroup, movePoints);
                if (reachable.Contains(target)) return new AiAction { MoveTo = target };
                return Approach(field, self, movementGroup, movePoints, new List<PlacedUnit> { new PlacedUnit(new Unit("patrol", "Patrol", Affiliation.Player, "m", new Stats()), target) });
            }

            var foes = field.LiveEnemiesOf(self);
            if (visibleTiles != null)
                foes.RemoveAll(f => !visibleTiles.Contains(f.Position));
            foes.Sort((a, b) =>
            {
                int c = a.Position.X.CompareTo(b.Position.X);
                if (c != 0) return c;
                c = a.Position.Y.CompareTo(b.Position.Y);
                return c != 0 ? c : string.CompareOrdinal(a.Unit.Nid, b.Unit.Nid);
            });
            if (foes.Count == 0) return AiAction.Wait();

            // Healer priority: if a heal staff can reach a wounded ally, heal the most wounded.
            var healResult = TryHeal(field, self, movementGroup, movePoints, visibleTiles);
            if (healResult != null) return healResult;

            var usable = new List<InventoryEntry>();
            foreach (InventoryEntry entry in self.Unit.Inventory)
                if (!entry.IsBroken && !entry.Weapon.IsStaff) usable.Add(entry);
            if (usable.Count == 0) return Approach(field, self, movementGroup, movePoints, foes, aiRules, terrainOf);

            AiAction? best = null;
            double bestScore = double.NegativeInfinity;
            int bestRemaining = int.MinValue;
            int bestDist = int.MaxValue;
            int bestMoveX = int.MinValue;
            int bestMoveY = int.MinValue;
            string bestFoeNid = "";
            string bestWeaponKey = "";

            // (1) Strike from the current tile.
            ScoreTile(field, self, self.Position, movePoints, usable, foes, rules, terrainOf, defenderWeaponOf, effectiveCounterWeight, aiRules, ref best, ref bestScore, ref bestRemaining, ref bestDist, ref bestMoveX, ref bestMoveY, ref bestFoeNid, ref bestWeaponKey, null, supports);
            if (best != null && best.MoveTo == null) return best;

            // (2) Move, then strike.
            var remaining = field.ReachableWithRemaining(self, movementGroup, movePoints);
            var allies = field.AllyTiles(self);
            var ordered = new List<GridPoint>(remaining.Keys);
            ordered.Sort((a, b) =>
            {
                int c = a.X.CompareTo(b.X);
                return c != 0 ? c : a.Y.CompareTo(b.Y);
            });
            foreach (GridPoint tile in ordered)
            {
                if (tile.Equals(self.Position)) continue;
                if (allies.Contains(tile)) continue; // cannot land on allies
                ScoreTile(field, self, tile, remaining[tile], usable, foes, rules, terrainOf, defenderWeaponOf, effectiveCounterWeight, aiRules, ref best, ref bestScore, ref bestRemaining, ref bestDist, ref bestMoveX, ref bestMoveY, ref bestFoeNid, ref bestWeaponKey, tile, supports);
            }
            if (best != null) return best;

            // (3) Nobody hittable: walk toward the nearest foe, preferring low threat and good terrain.
            return Approach(field, self, movementGroup, movePoints, foes, aiRules, terrainOf);
        }

        private static void ScoreTile(
            Battlefield field, PlacedUnit self, GridPoint tile, int remaining,
            List<InventoryEntry> usable, List<PlacedUnit> foes, CombatRules rules,
            TerrainOf? terrainOf, DefenderWeaponOf? defenderWeaponOf, double counterWeight, AiRules aiRules,
            ref AiAction? best, ref double bestScore, ref int bestRemaining, ref int bestDist, ref int bestMoveX, ref int bestMoveY, ref string bestFoeNid, ref string bestWeaponKey, GridPoint? moveTo = null,
            SupportLedger? supports = null)
        {
            foreach (InventoryEntry entry in usable)
            {
                foreach (PlacedUnit foe in foes)
                {
                    int dist = tile.ManhattanDistance(foe.Position);
                    if (!entry.Weapon.InRange(dist)) continue;

                    Weapon? foeWeapon = defenderWeaponOf != null
                        ? defenderWeaponOf(foe, dist)
                        : FirstUsable(foe, dist);
                    (int avoid, int defense) atkT = terrainOf != null ? terrainOf(self, tile) : (0, 0);
                    (int avoid, int defense) defT = terrainOf != null ? terrainOf(foe, foe.Position) : (0, 0);
                    int selfHit = 0, selfAvoid = 0, foeHit = 0, foeAvoid = 0;
                    if (supports != null)
                    {
                        var sbSelf = supports.BonusFor(self.Unit, tile, field.Placed);
                        var sbFoe = supports.BonusFor(foe.Unit, foe.Position, field.Placed);
                        selfHit = sbSelf.hit; selfAvoid = sbSelf.avoid;
                        foeHit = sbFoe.hit; foeAvoid = sbFoe.avoid;
                    }
                    CombatForecast fc = CombatResolver.Forecast(
                        self.Unit, entry.Weapon, foe.Unit, foeWeapon,
                        atkT.avoid, atkT.defense, defT.avoid, defT.defense, rules,
                        self.Unit.Skills, foe.Unit.Skills, selfHit, foeAvoid);
                    double score = Score(fc, foe, aiRules);
                    if (counterWeight > 0 && foeWeapon != null && foe.Unit.IsAlive)
                    {
                        CombatForecast counter = CombatResolver.Forecast(
                            foe.Unit, foeWeapon, self.Unit, entry.Weapon,
                            defT.avoid, defT.defense, atkT.avoid, atkT.defense, rules,
                            foe.Unit.Skills, self.Unit.Skills, foeHit, selfAvoid);
                        double risk = counter.Damage * (counter.Hit / 100.0)
                            + counter.Damage * 2.0 * (counter.Crit / 100.0);
                        score -= counterWeight * risk;
                    }
                    if (score > bestScore
                        || (score == bestScore && remaining > bestRemaining)
                        || (score == bestScore && remaining == bestRemaining && dist < bestDist)
                        || (score == bestScore && remaining == bestRemaining && dist == bestDist && IsDeterministicBetter(tile, foe.Unit.Nid, entry.Weapon.Key, bestMoveX, bestMoveY, bestFoeNid, bestWeaponKey)))
                    {
                        bestScore = score;
                        bestRemaining = remaining;
                        bestDist = dist;
                        bestMoveX = tile.X;
                        bestMoveY = tile.Y;
                        bestFoeNid = foe.Unit.Nid;
                        bestWeaponKey = entry.Weapon.Key;
                        best = new AiAction { MoveTo = moveTo, Weapon = entry, Target = foe };
                    }
                }
            }
        }

        private static bool IsDeterministicBetter(GridPoint tile, string foeNid, string weaponKey, int bestX, int bestY, string bestFoe, string bestWeapon)
        {
            if (tile.X != bestX) return tile.X < bestX;
            if (tile.Y != bestY) return tile.Y < bestY;
            int c = string.CompareOrdinal(foeNid, bestFoe);
            if (c != 0) return c < 0;
            return string.CompareOrdinal(weaponKey, bestWeapon) < 0;
        }

        private static double Score(CombatForecast fc, PlacedUnit foe, AiRules aiRules)
        {
            if (fc.Damage >= foe.Unit.CurrentHP) return aiRules.KillBonus + fc.Damage;
            double expected = fc.Damage * (fc.Hit / 100.0);
            expected += fc.Damage * 2.0 * (fc.Crit / 100.0);
            return expected;
        }

        private static AiAction? TryHeal(Battlefield field, PlacedUnit self, string movementGroup, int movePoints, HashSet<GridPoint>? visibleTiles)
        {
            InventoryEntry? staff = null;
            foreach (var e in self.Unit.Inventory)
                if (!e.IsBroken && e.Weapon.IsStaff && e.Weapon.HealPower > 0) { staff = e; break; }
            if (staff == null) return null;
            var reachable = field.ReachableWithRemaining(self, movementGroup, movePoints);
            var orderedReachable = new List<GridPoint>(reachable.Keys);
            orderedReachable.Sort((a, b) => { int c = a.X.CompareTo(b.X); return c != 0 ? c : a.Y.CompareTo(b.Y); });
            var allies = new List<PlacedUnit>();
            foreach (var p in field.Placed)
                if (p.Unit.Side == self.Unit.Side && p.Unit.IsAlive && p.Unit.CurrentHP < p.Unit.Stats.MaxHP)
                    allies.Add(p);
            allies.Sort((a, b) => { int c = a.Position.X.CompareTo(b.Position.X); if (c != 0) return c; c = a.Position.Y.CompareTo(b.Position.Y); return c != 0 ? c : string.CompareOrdinal(a.Unit.Nid, b.Unit.Nid); });
            AiAction? best = null;
            int bestMissing = -1;
            GridPoint bestTile = default;
            string bestAllyNid = "";
            foreach (var key in orderedReachable)
            {
                foreach (var ally in allies)
                {
                    int dist = key.ManhattanDistance(ally.Position);
                    if (!staff.Weapon.InRange(dist)) continue;
                    if (visibleTiles != null && !visibleTiles.Contains(ally.Position)) continue;
                    int missing = ally.Unit.Stats.MaxHP - ally.Unit.CurrentHP;
                    bool better = missing > bestMissing;
                    if (!better && missing == bestMissing && best != null)
                    {
                        int c = key.X.CompareTo(bestTile.X);
                        if (c == 0) c = key.Y.CompareTo(bestTile.Y);
                        if (c == 0) c = string.CompareOrdinal(ally.Unit.Nid, bestAllyNid);
                        better = c < 0;
                    }
                    if (better)
                    {
                        bestMissing = missing;
                        bestTile = key;
                        bestAllyNid = ally.Unit.Nid;
                        best = new AiAction { MoveTo = key.Equals(self.Position) ? null : (GridPoint?)key, Weapon = staff, Target = ally };
                    }
                }
            }
            return best;
        }

        private static Weapon? FirstUsable(PlacedUnit foe, int distance)
        {
            foreach (InventoryEntry e in foe.Unit.Inventory)
                if (!e.IsBroken && e.Weapon.CanCounterAt(distance)) return e.Weapon;
            return null;
        }

        private static AiAction Approach(
            Battlefield field, PlacedUnit self,
            string movementGroup, int movePoints, List<PlacedUnit> foes,
            AiRules? aiRules = null, TerrainOf? terrainOf = null)
        {
            aiRules ??= new AiRules();
            var reachable = field.Reachable(self, movementGroup, movePoints);
            GridPoint bestTile = self.Position;
            int bestDist = NearestFoeDist(self.Position, foes);
            double bestParking = ParkingScore(field, self, self.Position, foes, aiRules, terrainOf);

            var ordered = new List<GridPoint>(reachable);
            ordered.Sort((a, b) =>
            {
                int c = a.X.CompareTo(b.X);
                return c != 0 ? c : a.Y.CompareTo(b.Y);
            });
            foreach (GridPoint tile in ordered)
            {
                int d = NearestFoeDist(tile, foes);
                if (d > bestDist) continue;
                double parking = ParkingScore(field, self, tile, foes, aiRules, terrainOf);
                if (d < bestDist || parking > bestParking || (parking == bestParking && IsCoordinateBetter(tile, bestTile)))
                {
                    bestDist = d;
                    bestParking = parking;
                    bestTile = tile;
                }
            }

            if (bestTile.Equals(self.Position)) return AiAction.Wait();
            return new AiAction { MoveTo = bestTile };
        }

        private static double ParkingScore(Battlefield field, PlacedUnit self, GridPoint tile, List<PlacedUnit> foes, AiRules aiRules, TerrainOf? terrainOf)
        {
            int threat = 0;
            foreach (PlacedUnit foe in foes)
            {
                if (!foe.Unit.IsAlive) continue;
                foreach (InventoryEntry e in foe.Unit.Inventory)
                {
                    if (e.IsBroken || e.Weapon.IsStaff) continue;
                    if (e.Weapon.InRange(tile.ManhattanDistance(foe.Position))) { threat++; break; }
                }
            }
            (int avoid, int defense) t = terrainOf != null ? terrainOf(self, tile) : (0, 0);
            return -threat * aiRules.ParkingThreatWeight + t.avoid * aiRules.ParkingAvoidWeight + t.defense * aiRules.ParkingDefenseWeight;
        }

        private static bool IsCoordinateBetter(GridPoint tile, GridPoint bestTile)
        {
            if (tile.X != bestTile.X) return tile.X < bestTile.X;
            return tile.Y < bestTile.Y;
        }

        private static int NearestFoeDist(GridPoint tile, List<PlacedUnit> foes)
        {
            int nearest = int.MaxValue;
            foreach (PlacedUnit foe in foes)
                nearest = Math.Min(nearest, tile.ManhattanDistance(foe.Position));
            return nearest;
        }
    }
}
