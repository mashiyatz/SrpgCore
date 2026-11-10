using System;
using System.Collections.Generic;
using SrpgCore.Data;
using SrpgCore.Grid;

namespace SrpgCore.Tactics
{
    /// <summary>Support rank between a pair of units (GBA C/B/A shape).</summary>
    public enum SupportRank
    {
        /// <summary>No support bond.</summary>
        None = 0,
        /// <summary>C rank (30 points, plus 5 hit and avoid).</summary>
        C = 1,
        /// <summary>B rank (80 points, plus 10 hit and avoid).</summary>
        B = 2,
        /// <summary>A rank (150 points, plus 15 hit and avoid).</summary>
        A = 3,
    }

    /// <summary>
    /// Adjacency support ledger: bonds keyed by ordered nid pair, points accumulate to ranks.
    /// Bonus: each bonded ally within 3 Manhattan tiles grants +5 hit/avoid per rank step
    /// (C +5, B +10, A +15), GBA-flavored. Pure over placements + ledger.
    /// Fork stacks additively across multiple bonded allies; GBA FE7/8 applies only the
    /// single highest support bonus in range.
    /// </summary>
    public sealed class SupportLedger
    {
        private readonly Dictionary<(string, string), int> _points = new Dictionary<(string, string), int>();

        /// <summary>Points threshold required to reach a rank.</summary>
        /// <param name="rank">Rank whose threshold is requested.</param>
        /// <returns>Points needed for that rank (0 for None).</returns>
        public static int PointsForRank(SupportRank rank)
        {
            switch (rank)
            {
                case SupportRank.C: return 30;
                case SupportRank.B: return 80;
                case SupportRank.A: return 150;
                default: return 0;
            }
        }

        /// <summary>Derives the rank for an accumulated point total.</summary>
        /// <param name="points">Accumulated bond points.</param>
        /// <returns>Highest rank whose threshold is met.</returns>
        public static SupportRank RankForPoints(int points)
        {
            if (points >= PointsForRank(SupportRank.A)) return SupportRank.A;
            if (points >= PointsForRank(SupportRank.B)) return SupportRank.B;
            if (points >= PointsForRank(SupportRank.C)) return SupportRank.C;
            return SupportRank.None;
        }

        /// <summary>All bonded pairs with their accumulated points (for saves).</summary>
        /// <returns>Every canonical pair key with its point total.</returns>
        public IEnumerable<((string a, string b) key, int points)> AllPairs()
        {
            foreach (var kv in _points) yield return (kv.Key, kv.Value);
        }

        /// <summary>Restores points for a pair (for save restore).</summary>
        /// <param name="a">First nid.</param>
        /// <param name="b">Second nid.</param>
        /// <param name="points">Points to restore.</param>
        public void SetPoints(string a, string b, int points)
        {
            var k = Key(a, b);
            if (points <= 0) _points.Remove(k);
            else _points[k] = points;
        }

        private static (string, string) Key(string a, string b) =>
            string.CompareOrdinal(a, b) <= 0 ? (a, b) : (b, a);

        /// <summary>Accumulates bond points for an ordered pair (stored canonically).</summary>
        /// <param name="a">First unit nid.</param>
        /// <param name="b">Second unit nid.</param>
        /// <param name="points">Points to add (non-positive values are ignored).</param>
        public void AddPoints(string a, string b, int points)
        {
            if (points <= 0) return;
            var k = Key(a, b);
            _points.TryGetValue(k, out int cur);
            _points[k] = cur + points;
        }

        /// <summary>Reads the current rank for a unit pair.</summary>
        /// <param name="a">First unit nid.</param>
        /// <param name="b">Second unit nid.</param>
        /// <returns>Rank for the pair (None when unbonded).</returns>
        public SupportRank RankOf(string a, string b) =>
            _points.TryGetValue(Key(a, b), out int p) ? RankForPoints(p) : SupportRank.None;

        /// <summary>Hit and avoid bonus granted by one bond of a rank.</summary>
        /// <param name="rank">Bond rank.</param>
        /// <returns>Five per rank step (C 5, B 10, A 15).</returns>
        public static int BonusForRank(SupportRank rank) => (int)rank * 5;

        /// <summary>Grants 1 support point per adjacent alive ally pair (GBA-flavored tick).</summary>
        /// <param name="field">Battlefield to read adjacency from.</param>
        /// <returns>Number of pairs that gained a point.</returns>
        public int TickAdjacent(Battlefield field)
        {
            int gained = 0;
            var seen = new HashSet<(string, string)>();
            foreach (var a in field.Placed)
            {
                if (!a.Unit.IsAlive) continue;
                foreach (var b in field.Placed)
                {
                    if (ReferenceEquals(a, b) || !b.Unit.IsAlive) continue;
                    if (a.Position.ManhattanDistance(b.Position) != 1) continue;
                    var k = Key(a.Unit.Nid, b.Unit.Nid);
                    if (seen.Contains(k)) continue;
                    seen.Add(k);
                    AddPoints(k.Item1, k.Item2, 1);
                    gained++;
                }
            }
            return gained;
        }

        /// <summary>Sums hit and avoid from bonded allies within 3 Manhattan tiles (fork: additive stacking across allies; GBA uses single highest).</summary>
        /// <param name="unit">Unit receiving the bonus.</param>
        /// <param name="pos">Tile the unit is evaluated from.</param>
        /// <param name="allies">Candidate bonded allies.</param>
        /// <returns>(hitBonus, avoidBonus) summed across all bonded allies in range.</returns>
        public (int hit, int avoid) BonusFor(Unit unit, GridPoint pos, IEnumerable<PlacedUnit> allies)
        {
            int bonus = 0;
            foreach (PlacedUnit p in allies)
            {
                if (ReferenceEquals(p.Unit, unit) || !p.Unit.IsAlive) continue;
                if (pos.ManhattanDistance(p.Position) > 3) continue;
                SupportRank r = RankOf(unit.Nid, p.Unit.Nid);
                if (r != SupportRank.None) bonus += BonusForRank(r);
            }
            return (bonus, bonus);
        }
    }
}
