using System;
using System.Collections.Generic;
using SrpgCore.Data;
using SrpgCore.Grid;

namespace SrpgCore.Tactics
{
    /// <summary>A unit plus its current board tile. Values: Unit (stats, inventory, HP, skills, statuses) and mutable Position.</summary>
    public sealed class PlacedUnit
    {
        /// <summary>The deployed unit data.</summary>
        public Unit Unit { get; }
        /// <summary>Current board tile of the unit.</summary>
        public GridPoint Position { get; set; }

        /// <summary>Places an existing unit on a tile.</summary>
        /// <param name="unit">Unit to place (must not be null).</param>
        /// <param name="position">Starting board tile.</param>
        public PlacedUnit(Unit unit, GridPoint position)
        {
            Unit = unit ?? throw new ArgumentNullException(nameof(unit));
            Position = position;
        }
    }

    /// <summary>
    /// Board + unit placements with ONE coherent blocking rule: enemies block
    /// pass-through, allies are pass-through but cannot be landed on. Values:
    /// Board geometry, Placed placements (PlacedUnit = Unit + Position), and
    /// helpers: Deploy/Remove, AreHostile/LiveEnemiesOf, BlockingTiles (hostile
    /// positions), AllyTiles, Reachable/ReachableWithRemaining. See ChapterState
    /// for the live runtime tying this to phases, RNG, and objectives.
    /// </summary>
    public sealed class Battlefield
    {
        /// <summary>The board geometry movement and range read.</summary>
        public Board Board { get; }

        private readonly List<PlacedUnit> _placed = new List<PlacedUnit>();
        /// <summary>All deployed placements in deploy order.</summary>
        public IReadOnlyList<PlacedUnit> Placed => _placed;

        /// <summary>Creates a battlefield over an existing board.</summary>
        /// <param name="board">Board geometry (must not be null).</param>
        public Battlefield(Board board)
        {
            Board = board ?? throw new ArgumentNullException(nameof(board));
        }

        /// <summary>Deploys a unit on a tile and tracks the placement.</summary>
        /// <param name="unit">Unit to deploy.</param>
        /// <param name="position">Destination tile (must be in bounds).</param>
        /// <returns>The new placement wrapping the unit.</returns>
        public PlacedUnit Deploy(Unit unit, GridPoint position)
        {
            if (!Board.InBounds(position)) throw new ArgumentOutOfRangeException(nameof(position));
            var placed = new PlacedUnit(unit, position);
            _placed.Add(placed);
            return placed;
        }

        /// <summary>Removes a placement from the board.</summary>
        /// <param name="placed">Placement to remove.</param>
        /// <returns>True when the placement was tracked and removed.</returns>
        public bool Remove(PlacedUnit placed) => _placed.Remove(placed);

        /// <summary>Player and Ally fight the Enemy; Other (neutral) fights nobody.</summary>
        /// <param name="a">First side.</param>
        /// <param name="b">Second side.</param>
        /// <returns>True when the two sides treat each other as foes.</returns>
        public static bool AreHostile(Affiliation a, Affiliation b)
        {
            bool aFights = a == Affiliation.Player || a == Affiliation.Ally;
            bool bFights = b == Affiliation.Player || b == Affiliation.Ally;
            return (aFights && b == Affiliation.Enemy) || (a == Affiliation.Enemy && bFights);
        }

        /// <summary>Lists live hostile placements for a unit.</summary>
        /// <param name="unit">Viewing unit whose foes are collected.</param>
        /// <returns>Live placements hostile to the given unit.</returns>
        public List<PlacedUnit> LiveEnemiesOf(PlacedUnit unit)
        {
            var foes = new List<PlacedUnit>();
            foreach (PlacedUnit p in _placed)
                if (!ReferenceEquals(p, unit) && p.Unit.IsAlive && AreHostile(unit.Unit.Side, p.Unit.Side))
                    foes.Add(p);
            return foes;
        }

        /// <summary>Tiles that block pass-through (live hostile positions).</summary>
        /// <param name="unit">Moving unit whose foes count as blocking.</param>
        /// <returns>Positions enemies occupy for this unit.</returns>
        public HashSet<GridPoint> BlockingTiles(PlacedUnit unit)
        {
            var blocking = new HashSet<GridPoint>();
            foreach (PlacedUnit p in _placed)
                if (!ReferenceEquals(p, unit) && p.Unit.IsAlive && AreHostile(unit.Unit.Side, p.Unit.Side))
                    blocking.Add(p.Position);
            return blocking;
        }

        /// <summary>Tiles occupied by live non-hostiles (pass-through but not landable).</summary>
        /// <param name="unit">Moving unit whose allies are collected.</param>
        /// <returns>Positions allies occupy for this unit.</returns>
        public HashSet<GridPoint> AllyTiles(PlacedUnit unit)
        {
            var allies = new HashSet<GridPoint>();
            foreach (PlacedUnit p in _placed)
                if (!ReferenceEquals(p, unit) && p.Unit.IsAlive && !AreHostile(unit.Unit.Side, p.Unit.Side))
                    allies.Add(p.Position);
            return allies;
        }

        /// <summary>Tiles the unit may end movement on (origin always included).</summary>
        /// <param name="unit">Moving unit (origin and side are read).</param>
        /// <param name="movementGroup">Movement group key for terrain costs.</param>
        /// <param name="movePoints">Movement budget for this turn.</param>
        /// <returns>Landable tiles excluding ally-occupied tiles except the origin.</returns>
        public HashSet<GridPoint> Reachable(PlacedUnit unit, string movementGroup, int movePoints)
        {
            var remaining = RangeFinder.ComputeReachable(Board, unit.Position, movePoints, movementGroup, BlockingTiles(unit));
            var allies = AllyTiles(unit);
            var tiles = new HashSet<GridPoint>();
            foreach (GridPoint p in remaining.Keys)
                if (p.Equals(unit.Position) || !allies.Contains(p))
                    tiles.Add(p);
            return tiles;
        }

        /// <summary>Reachable tiles mapped to movement remaining on arrival (includes ally tiles).</summary>
        /// <param name="unit">Moving unit (origin and side are read).</param>
        /// <param name="movementGroup">Movement group key for terrain costs.</param>
        /// <param name="movePoints">Movement budget for this turn.</param>
        /// <returns>Every reachable tile with the movement left after arriving.</returns>
        public Dictionary<GridPoint, int> ReachableWithRemaining(PlacedUnit unit, string movementGroup, int movePoints) =>
            RangeFinder.ComputeReachable(Board, unit.Position, movePoints, movementGroup, BlockingTiles(unit));
    }
}
