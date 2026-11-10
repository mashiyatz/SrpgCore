using System;
using System.Collections.Generic;

namespace SrpgCore.Grid
{
    /// <summary>Dijkstra movement-range, A-star pathfinding, and shared Manhattan attack-ring logic (ComputeReachable/FindPath/AttackTiles). All pure and deterministic.</summary>
    public static class RangeFinder
    {
        private static readonly GridPoint[] Neighbors =
        {
            new GridPoint(1, 0), new GridPoint(-1, 0),
            new GridPoint(0, 1), new GridPoint(0, -1),
        };

        /// <summary>Computes reachable tiles via Dijkstra over tile entry costs.</summary>
        /// <param name="board">The board holding the terrain layout.</param>
        /// <param name="origin">The start tile, always included even when blocked.</param>
        /// <param name="movePoints">The movement budget spent on tile entry costs.</param>
        /// <param name="moveType">The move type mapped to a movement group.</param>
        /// <param name="blocked">Tiles to exclude as occupied, or null for none.</param>
        /// <returns>Map of reachable tile to movement points remaining on arrival.</returns>
        public static Dictionary<GridPoint, int> ComputeReachable(
            Board board,
            GridPoint origin,
            int movePoints,
            MoveType moveType,
            ISet<GridPoint>? blocked = null) =>
            ComputeReachable(board, origin, movePoints, MovementGroups.For(moveType), blocked);

        /// <summary>Computes reachable tiles via Dijkstra over tile entry costs.</summary>
        /// <param name="board">The board holding the terrain layout.</param>
        /// <param name="origin">The start tile, always included even when blocked.</param>
        /// <param name="movePoints">The movement budget spent on tile entry costs.</param>
        /// <param name="movementGroup">The movement group name used for terrain costs.</param>
        /// <param name="blocked">Tiles to exclude as occupied, or null for none.</param>
        /// <returns>Map of reachable tile to movement points remaining on arrival.</returns>
        public static Dictionary<GridPoint, int> ComputeReachable(
            Board board,
            GridPoint origin,
            int movePoints,
            string movementGroup,
            ISet<GridPoint>? blocked = null)
        {
            if (board == null) throw new ArgumentNullException(nameof(board));
            if (!board.InBounds(origin)) throw new ArgumentOutOfRangeException(nameof(origin));

            var remaining = new Dictionary<GridPoint, int> { [origin] = movePoints };
            // Poor-man's priority queue (maps are small); netstandard2.0 has no PriorityQueue.
            var open = new List<GridPoint> { origin };

            while (open.Count > 0)
            {
                int best = 0;
                for (int i = 1; i < open.Count; i++)
                    if (remaining[open[i]] > remaining[open[best]]) best = i;
                GridPoint current = open[best];
                open.RemoveAt(best);

                foreach (GridPoint d in Neighbors)
                {
                    var next = new GridPoint(current.X + d.X, current.Y + d.Y);
                    if (!board.InBounds(next)) continue;
                    if (blocked != null && blocked.Contains(next)) continue;

                    TerrainType terrain = board.GetTerrain(next);
                    if (!terrain.IsPassable(movementGroup)) continue; // impassable
                    int cost = terrain.MoveCost(movementGroup);

                    int left = remaining[current] - cost;
                    if (left < 0) continue;
                    if (remaining.TryGetValue(next, out int prev) && prev >= left) continue;

                    remaining[next] = left;
                    open.Add(next);
                }
            }

            return remaining;
        }

        /// <summary>Finds the cheapest path within budget using A-star with a Manhattan heuristic.</summary>
        /// <param name="board">The board holding the terrain layout.</param>
        /// <param name="from">The start tile.</param>
        /// <param name="to">The destination tile, allowed even when blocked.</param>
        /// <param name="movePoints">The movement budget limiting total entry cost.</param>
        /// <param name="moveType">The move type mapped to a movement group.</param>
        /// <param name="blocked">Tiles to exclude except the destination, or null for none.</param>
        /// <returns>Cheapest path including both ends, or null when unreachable within budget.</returns>
        public static List<GridPoint>? FindPath(
            Board board,
            GridPoint from,
            GridPoint to,
            int movePoints,
            MoveType moveType,
            ISet<GridPoint>? blocked = null) =>
            FindPath(board, from, to, movePoints, MovementGroups.For(moveType), blocked);

        /// <summary>Finds the cheapest path within budget using A-star with a Manhattan heuristic.</summary>
        /// <param name="board">The board holding the terrain layout.</param>
        /// <param name="from">The start tile.</param>
        /// <param name="to">The destination tile, allowed even when blocked.</param>
        /// <param name="movePoints">The movement budget limiting total entry cost.</param>
        /// <param name="movementGroup">The movement group name used for terrain costs.</param>
        /// <param name="blocked">Tiles to exclude except the destination, or null for none.</param>
        /// <returns>Cheapest path including both ends, or null when unreachable within budget.</returns>
        public static List<GridPoint>? FindPath(
            Board board,
            GridPoint from,
            GridPoint to,
            int movePoints,
            string movementGroup,
            ISet<GridPoint>? blocked = null)
        {
            if (board == null) throw new ArgumentNullException(nameof(board));
            if (!board.InBounds(from)) throw new ArgumentOutOfRangeException(nameof(from));
            if (!board.InBounds(to)) throw new ArgumentOutOfRangeException(nameof(to));
            if (from.Equals(to)) return new List<GridPoint> { from };

            var costSoFar = new Dictionary<GridPoint, int> { [from] = 0 };
            var cameFrom = new Dictionary<GridPoint, GridPoint>();
            var open = new List<GridPoint> { from };

            while (open.Count > 0)
            {
                int best = 0;
                int bestF = F(open[0], to, costSoFar);
                for (int i = 1; i < open.Count; i++)
                {
                    int f = F(open[i], to, costSoFar);
                    if (f < bestF) { best = i; bestF = f; }
                }
                GridPoint current = open[best];
                open.RemoveAt(best);

                if (current.Equals(to)) break;

                foreach (GridPoint d in Neighbors)
                {
                    var next = new GridPoint(current.X + d.X, current.Y + d.Y);
                    if (!board.InBounds(next)) continue;
                    if (!next.Equals(to) && blocked != null && blocked.Contains(next)) continue;

                    TerrainType terrain = board.GetTerrain(next);
                    if (!terrain.IsPassable(movementGroup)) continue;
                    int step = terrain.MoveCost(movementGroup);

                    int g = costSoFar[current] + step;
                    if (g > movePoints) continue;
                    if (costSoFar.TryGetValue(next, out int prev) && prev <= g) continue;

                    costSoFar[next] = g;
                    cameFrom[next] = current;
                    if (!open.Contains(next)) open.Add(next);
                }
            }

            if (!cameFrom.ContainsKey(to)) return null;

            var path = new List<GridPoint> { to };
            GridPoint c = to;
            while (!c.Equals(from))
            {
                c = cameFrom[c];
                path.Add(c);
            }
            path.Reverse();
            return path;
        }

        private static int F(GridPoint p, GridPoint goal, Dictionary<GridPoint, int> g) =>
            g[p] + p.ManhattanDistance(goal); // admissible while per-tile costs stay >= 1

        /// <summary>Returns Manhattan ring attack tiles around the center, shared by rendering and checks.</summary>
        /// <param name="board">The board used for bounds checks.</param>
        /// <param name="center">The center tile excluded from the result.</param>
        /// <param name="minRange">The minimum Manhattan distance included.</param>
        /// <param name="maxRange">The maximum Manhattan distance included.</param>
        /// <returns>In-bounds attack tiles within the given range ring.</returns>
        public static HashSet<GridPoint> AttackTiles(Board board, GridPoint center, int minRange, int maxRange)
        {
            if (board == null) throw new ArgumentNullException(nameof(board));
            var tiles = new HashSet<GridPoint>();
            for (int dy = -maxRange; dy <= maxRange; dy++)
                for (int dx = -maxRange; dx <= maxRange; dx++)
                {
                    if (dx == 0 && dy == 0) continue;
                    int dist = Math.Abs(dx) + Math.Abs(dy);
                    if (dist < minRange || dist > maxRange) continue;
                    var p = new GridPoint(center.X + dx, center.Y + dy);
                    if (board.InBounds(p)) tiles.Add(p);
                }
            return tiles;
        }
    }
}
