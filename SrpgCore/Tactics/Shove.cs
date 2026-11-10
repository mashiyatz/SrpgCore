using System;
using SrpgCore.Data;
using SrpgCore.Grid;

namespace SrpgCore.Tactics
{
    /// <summary>Adjacent shove/swap: pushes or exchanges adjacent units when Con/weight allows.</summary>
    public static class Shove
    {
        /// <summary>True when shover can push target one tile away (adjacent, target destination free, Con sufficient).</summary>
        /// <param name="shover">Unit doing the shoving.</param>
        /// <param name="target">Unit being shoved.</param>
        /// <param name="shoverPos">Tile of the shover.</param>
        /// <param name="targetPos">Tile of the target.</param>
        /// <param name="field">Battlefield to test destination vacancy and bounds.</param>
        /// <returns>True when the push is legal.</returns>
        public static bool CanShove(Unit shover, Unit target, GridPoint shoverPos, GridPoint targetPos, Battlefield field)
        {
            if (shover == null || target == null || field == null) return false;
            if (!shover.IsAlive || !target.IsAlive) return false;
            if (shoverPos.ManhattanDistance(targetPos) != 1) return false;
            if (target.Stats.Constitution > shover.Stats.Constitution + 2) return false;
            GridPoint dest = new GridPoint(targetPos.X + (targetPos.X - shoverPos.X), targetPos.Y + (targetPos.Y - shoverPos.Y));
            if (!field.Board.InBounds(dest)) return false;
            if (!field.Board.GetTerrain(dest).IsPassable(target.MovementGroup)) return false;
            foreach (var p in field.Placed) if (p.Position.Equals(dest)) return false;
            return true;
        }

        /// <summary>Executes a shove: moves target one tile away from shover.</summary>
        /// <param name="field">Battlefield containing both units.</param>
        /// <param name="shover">Placed shover.</param>
        /// <param name="target">Placed target.</param>
        public static void ExecuteShove(Battlefield field, PlacedUnit shover, PlacedUnit target)
        {
            if (field == null) throw new ArgumentNullException(nameof(field));
            if (shover == null) throw new ArgumentNullException(nameof(shover));
            if (target == null) throw new ArgumentNullException(nameof(target));
            if (!CanShove(shover.Unit, target.Unit, shover.Position, target.Position, field))
                throw new InvalidOperationException("Shove conditions not met.");
            GridPoint dest = new GridPoint(target.Position.X + (target.Position.X - shover.Position.X), target.Position.Y + (target.Position.Y - shover.Position.Y));
            target.Position = dest;
        }

        /// <summary>True when two adjacent units can swap positions.</summary>
        /// <param name="a">First unit.</param>
        /// <param name="b">Second unit.</param>
        /// <param name="aPos">Tile of first.</param>
        /// <param name="bPos">Tile of second.</param>
        /// <returns>True when adjacent and both alive.</returns>
        public static bool CanSwap(Unit a, Unit b, GridPoint aPos, GridPoint bPos)
        {
            if (a == null || b == null) return false;
            if (!a.IsAlive || !b.IsAlive) return false;
            return aPos.ManhattanDistance(bPos) == 1;
        }

        /// <summary>Swaps positions of two adjacent placed units.</summary>
        /// <param name="a">First placed unit.</param>
        /// <param name="b">Second placed unit.</param>
        public static void ExecuteSwap(PlacedUnit a, PlacedUnit b)
        {
            if (a == null) throw new ArgumentNullException(nameof(a));
            if (b == null) throw new ArgumentNullException(nameof(b));
            if (!CanSwap(a.Unit, b.Unit, a.Position, b.Position))
                throw new InvalidOperationException("Swap needs adjacency.");
            GridPoint tmp = a.Position;
            a.Position = b.Position;
            b.Position = tmp;
        }
    }
}
