using System;
using System.Collections.Generic;

namespace SrpgCore.Grid
{
    /// <summary>Pure-logic board holding terrain layout (Width, Height, Terrains, and a per-tile terrain key grid). Units and obstacles are not stored here; callers overlay them as blocked/ally sets when querying range and paths.</summary>
    public sealed class Board
    {
        private readonly string[,] _terrainKeys;

        /// <summary>Gets the board width in tiles.</summary>
        public int Width { get; }
        /// <summary>Gets the board height in tiles.</summary>
        public int Height { get; }

        /// <summary>Gets the terrain definitions keyed by terrain key.</summary>
        public IReadOnlyDictionary<string, TerrainType> Terrains { get; }

        /// <summary>Initializes a board filled with the default terrain.</summary>
        /// <param name="width">The board width in tiles.</param>
        /// <param name="height">The board height in tiles.</param>
        /// <param name="defaultTerrain">The terrain key filling every tile.</param>
        /// <param name="terrains">The terrain definitions keyed by terrain key.</param>
        public Board(int width, int height, string defaultTerrain, IReadOnlyDictionary<string, TerrainType> terrains)
        {
            if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width));
            if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height));
            Terrains = terrains ?? throw new ArgumentNullException(nameof(terrains));
            if (!terrains.ContainsKey(defaultTerrain))
                throw new ArgumentException($"Unknown terrain '{defaultTerrain}'.", nameof(defaultTerrain));

            Width = width;
            Height = height;
            _terrainKeys = new string[width, height];
            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                    _terrainKeys[x, y] = defaultTerrain;
        }

        /// <summary>Returns true when the point lies inside the board bounds.</summary>
        /// <param name="p">The point to test.</param>
        /// <returns>True when inside bounds, otherwise false.</returns>
        public bool InBounds(GridPoint p) => p.X >= 0 && p.Y >= 0 && p.X < Width && p.Y < Height;

        /// <summary>Sets the terrain key at the given point.</summary>
        /// <param name="p">The point to update.</param>
        /// <param name="terrainKey">The new terrain key.</param>
        public void SetTerrain(GridPoint p, string terrainKey)
        {
            if (!InBounds(p)) throw new ArgumentOutOfRangeException(nameof(p));
            if (!Terrains.ContainsKey(terrainKey)) throw new ArgumentException($"Unknown terrain '{terrainKey}'.", nameof(terrainKey));
            _terrainKeys[p.X, p.Y] = terrainKey;
        }

        /// <summary>Gets the terrain key at the given point.</summary>
        /// <param name="p">The point to read.</param>
        /// <returns>The terrain key stored at the point.</returns>
        public string GetTerrainKey(GridPoint p)
        {
            if (!InBounds(p)) throw new ArgumentOutOfRangeException(nameof(p));
            return _terrainKeys[p.X, p.Y];
        }

        /// <summary>Gets the terrain definition at the given point.</summary>
        /// <param name="p">The point to read.</param>
        /// <returns>The terrain definition at the point.</returns>
        public TerrainType GetTerrain(GridPoint p)
        {
            if (!InBounds(p)) throw new ArgumentOutOfRangeException(nameof(p));
            return Terrains[_terrainKeys[p.X, p.Y]];
        }

        /// <summary>Builds a board from ASCII rows where each character maps through the legend, throwing on ragged rows or unknown glyphs.</summary>
        /// <param name="rows">The ASCII rows with one character per tile.</param>
        /// <param name="defaultTerrain">The terrain key filling the board before glyphs are applied.</param>
        /// <param name="legend">The map from glyph character to terrain key (BMP characters only).</param>
        /// <param name="terrains">The terrain definitions keyed by terrain key.</param>
        /// <returns>A new board with terrain taken from the given rows.</returns>
        public static Board FromRows(
            string[] rows,
            string defaultTerrain,
            IDictionary<char, string> legend,
            IReadOnlyDictionary<string, TerrainType> terrains)
        {
            if (rows == null || rows.Length == 0)
                throw new ArgumentException("Need at least one row.", nameof(rows));
            if (legend == null) throw new ArgumentNullException(nameof(legend));

            int width = rows[0].Length;
            if (width == 0) throw new ArgumentException("Rows must be non-empty.", nameof(rows));
            for (int y = 1; y < rows.Length; y++)
                if (rows[y].Length != width)
                    throw new FormatException($"Row {y} has length {rows[y].Length}, expected {width}.");

            var board = new Board(width, rows.Length, defaultTerrain, terrains);
            for (int y = 0; y < rows.Length; y++)
                for (int x = 0; x < width; x++)
                {
                    char glyph = rows[y][x];
                    if (!legend.TryGetValue(glyph, out string? key))
                        throw new FormatException($"Unknown map glyph '{glyph}' at ({x},{y}).");
                    board.SetTerrain(new GridPoint(x, y), key);
                }
            return board;
        }
    }
}
