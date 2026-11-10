using System;
using System.Collections.Generic;
using SrpgCore.Grid;

namespace SrpgCore.Regions
{
    /// <summary>Gameplay meaning of a named tile set.</summary>
    public enum RegionKind
    {
        /// <summary>Raises RegionEntered for the event system.</summary>
        Event,      // raises RegionEntered for the event system
        /// <summary>Allowed deployment tiles.</summary>
        Formation,  // allowed deployment tiles
    }

    /// <summary>Named set of tiles with a gameplay meaning.</summary>
    public sealed class Region
    {
        /// <summary>Unique region name referenced by events and deployment.</summary>
        public string Name { get; }
        /// <summary>Gameplay meaning of this tile set.</summary>
        public RegionKind Kind { get; }

        private readonly HashSet<GridPoint> _tiles = new HashSet<GridPoint>();
        /// <summary>Tile membership set for this region.</summary>
        public ISet<GridPoint> Tiles => _tiles;

        /// <summary>Creates a named region with the given gameplay kind.</summary>
        /// <param name="name">Unique region name.</param>
        /// <param name="kind">Gameplay meaning of the tile set.</param>
        public Region(string name, RegionKind kind)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Kind = kind;
        }

        /// <summary>Adds a tile to the region set.</summary>
        /// <param name="p">The tile to add.</param>
        public void Add(GridPoint p) => _tiles.Add(p);
        /// <summary>Checks whether a tile belongs to the region.</summary>
        /// <param name="p">The tile to test.</param>
        /// <returns>True when the tile is a member of the region.</returns>
        public bool Contains(GridPoint p) => _tiles.Contains(p);
    }
}
