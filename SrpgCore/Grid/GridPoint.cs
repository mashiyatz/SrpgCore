using System;

namespace SrpgCore.Grid
{
    /// <summary>Zero-based square-grid coordinate. Values: X column (east) and Y row (south), Manhattan distance and dictionary hashing.</summary>
    public readonly struct GridPoint : IEquatable<GridPoint>
    {
        /// <summary>Gets the zero-based column (east).</summary>
        public int X { get; }
        /// <summary>Gets the zero-based row (south).</summary>
        public int Y { get; }

        /// <summary>Initializes a grid coordinate.</summary>
        /// <param name="x">The zero-based column.</param>
        /// <param name="y">The zero-based row.</param>
        public GridPoint(int x, int y)
        {
            X = x;
            Y = y;
        }

        /// <summary>Returns the Manhattan distance to another point.</summary>
        /// <param name="other">The other point.</param>
        /// <returns>The Manhattan distance to the other point.</returns>
        public int ManhattanDistance(GridPoint other) =>
            Math.Abs(X - other.X) + Math.Abs(Y - other.Y);

        /// <summary>Returns true when the coordinates match.</summary>
        /// <param name="other">The point to compare.</param>
        /// <returns>True when the points are equal.</returns>
        public bool Equals(GridPoint other) => X == other.X && Y == other.Y;
        /// <summary>Returns true when the object is an equal grid point.</summary>
        /// <param name="obj">The object to compare.</param>
        /// <returns>True when the object is an equal grid point.</returns>
        public override bool Equals(object? obj) => obj is GridPoint p && Equals(p);
        /// <summary>Returns a hash code for dictionary use.</summary>
        /// <returns>A hash code combining X and Y.</returns>
        public override int GetHashCode() => unchecked((X * 397) ^ Y);
        /// <summary>Returns the coordinate formatted as (X,Y).</summary>
        /// <returns>The coordinate formatted as (X,Y).</returns>
        public override string ToString() => $"({X},{Y})";

        /// <summary>Compares two points for equality.</summary>
        /// <param name="a">The first point.</param>
        /// <param name="b">The second point.</param>
        /// <returns>True when the points are equal.</returns>
        public static bool operator ==(GridPoint a, GridPoint b) => a.Equals(b);
        /// <summary>Compares two points for inequality.</summary>
        /// <param name="a">The first point.</param>
        /// <param name="b">The second point.</param>
        /// <returns>True when the points differ.</returns>
        public static bool operator !=(GridPoint a, GridPoint b) => !a.Equals(b);
    }
}
