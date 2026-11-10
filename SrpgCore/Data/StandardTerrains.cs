using System.Collections.Generic;
using SrpgCore.Grid;

namespace SrpgCore.Data
{
    /// <summary>
    /// Starter terrain set with GBA-flavored defaults. Hosts can replace the set
    /// with their own data; gameplay code must only depend on TerrainType.
    /// MoveCost -1 (const Impassable) blocks that move type.
    /// </summary>
    public static class StandardTerrains
    {
        /// <summary>Move cost blocking a movement group from entering a tile.</summary>
        public const int Impassable = -1;

        /// <summary>Builds the GBA-flavored starter terrain set keyed by terrain id.</summary>
        /// <returns>Fresh dictionary of plain, road, forest, fort, and blocking tiles.</returns>
        public static Dictionary<string, TerrainType> Create()
        {
            //                      key        name        inf  arm  cav  fly  avo  def
            return new Dictionary<string, TerrainType>
            {
                ["plain"]   = new TerrainType("plain",   "Plain",   1,   1,   1,   1,   0,  0),
                ["road"]    = new TerrainType("road",    "Road",    1,   1,   1,   1,   0,  0),
                ["bridge"]  = new TerrainType("bridge",  "Bridge",  1,   1,   1,   1,   0,  0),
                ["forest"]  = new TerrainType("forest",  "Forest",  2,   2,   3,   1,  20,  1),
                ["fort"]    = new TerrainType("fort",    "Fort",    2,   2,   2,   1,  15,  2, hpEffectPercent: 10),
                ["village"] = new TerrainType("village", "Village", 1,   1,   1,   1,  10,  0),
                ["gate"]    = new TerrainType("gate",    "Gate",    1,   1,   1,   1,  20,  2),
                ["throne"]  = new TerrainType("throne",  "Throne",  1,   1,   1,   1,  20,  3),
                ["mountain"]= new TerrainType("mountain","Mountain",4,   Impassable, Impassable, 1, 30, 2),
                ["water"]   = new TerrainType("water",   "Water",   Impassable, Impassable, Impassable, 1, 10, 0),
                ["wall"]    = new TerrainType("wall",    "Wall",    Impassable, Impassable, Impassable, Impassable, 0, 0),
            };
        }
    }
}
