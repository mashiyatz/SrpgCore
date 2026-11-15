using System;
using System.Collections.Generic;

namespace SrpgCore.Grid
{
    /// <summary>Pure-logic board holding terrain layout (Width, Height, Terrains, and a per-tile terrain key grid) plus optional per-tile height 0..MaxHeight for iso cliffs. Units and obstacles are not stored here; callers overlay them as blocked/ally sets when querying range and paths.</summary>
    public sealed class Board
    {
        /// <summary>Maximum supported tile height (iso cliffs). HEIGHTS glyphs above this fail load.</summary>
        public const int MaxHeight = 3;
        private readonly string[,] _terrainKeys;
        private readonly int[,] _heights;

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
            _heights = new int[width, height];
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

        /// <summary>Gets the height at the given point (0 when flat).</summary>
        /// <param name="p">The point to read.</param>
        /// <returns>Height 0..MaxHeight.</returns>
        public int GetHeight(GridPoint p)
        {
            if (!InBounds(p)) throw new ArgumentOutOfRangeException(nameof(p));
            return _heights[p.X, p.Y];
        }

        /// <summary>Sets the height at the given point.</summary>
        /// <param name="p">The point to update.</param>
        /// <param name="height">Height 0..MaxHeight.</param>
        public void SetHeight(GridPoint p, int height)
        {
            if (!InBounds(p)) throw new ArgumentOutOfRangeException(nameof(p));
            if (height < 0 || height > MaxHeight) throw new ArgumentOutOfRangeException(nameof(height));
            _heights[p.X, p.Y] = height;
        }

        /// <summary>True when any tile has non-zero height.</summary>
        /// <returns>True when at least one tile is raised.</returns>
        public bool HasHeights()
        {
            for (int x = 0; x < Width; x++)
                for (int y = 0; y < Height; y++)
                    if (_heights[x, y] != 0) return true;
            return false;
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

        /// <summary>Builds a board from ASCII rows plus parallel height rows (chars '0'..'3'), throwing on ragged rows or out-of-range heights.</summary>
        public static Board FromRows(
            string[] rows,
            string[] heightRows,
            string defaultTerrain,
            IDictionary<char, string> legend,
            IReadOnlyDictionary<string, TerrainType> terrains)
        {
            var board = FromRows(rows, defaultTerrain, legend, terrains);
            if (heightRows == null) return board;
            if (heightRows.Length != board.Height)
                throw new FormatException($"HEIGHTS has {heightRows.Length} rows, expected {board.Height}.");
            for (int y = 0; y < heightRows.Length; y++)
            {
                if (heightRows[y].Length != board.Width)
                    throw new FormatException($"HEIGHTS row {y} has length {heightRows[y].Length}, expected {board.Width}.");
                for (int x = 0; x < board.Width; x++)
                {
                    char g = heightRows[y][x];
                    if (g == '.') continue;
                    if (g < '0' || g > '3')
                        throw new FormatException($"Bad height glyph '{g}' at ({x},{y}) (want 0-3 or .).");
                    board.SetHeight(new GridPoint(x, y), g - '0');
                }
            }
            return board;
        }
    }
}
