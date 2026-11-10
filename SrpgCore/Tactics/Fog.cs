using System.Collections.Generic;
using SrpgCore.Data;
using SrpgCore.Grid;

namespace SrpgCore.Tactics
{
    /// <summary>
    /// Fog of war: a tile is visible to a side when within any live unit's Vision
    /// (Manhattan radius). No line-of-sight blocking in this model — walls still cost
    /// movement but do not stop sight (documented simplification). Pure set math.
    /// </summary>
    public static class FogCalculator
    {
        /// <summary>Unions Manhattan vision balls of every live unit of a side.</summary>
        /// <param name="field">Battlefield holding board bounds and placements.</param>
        /// <param name="side">Side whose sight to compute.</param>
        /// <returns>In-bounds tiles visible to that side.</returns>
        public static HashSet<GridPoint> VisibleTiles(Battlefield field, Affiliation side)
        {
            var seen = new HashSet<GridPoint>();
            foreach (PlacedUnit p in field.Placed)
            {
                if (p.Unit.Side != side || !p.Unit.IsAlive) continue;
                int v = p.Unit.Vision;
                for (int dy = -v; dy <= v; dy++)
                    for (int dx = -v; dx <= v; dx++)
                    {
                        if (System.Math.Abs(dx) + System.Math.Abs(dy) > v) continue;
                        var t = new GridPoint(p.Position.X + dx, p.Position.Y + dy);
                        if (field.Board.InBounds(t)) seen.Add(t);
                    }
            }
            return seen;
        }

        /// <summary>Tests whether one tile is visible to a side.</summary>
        /// <param name="field">Battlefield holding board bounds and placements.</param>
        /// <param name="viewerSide">Side whose sight to test.</param>
        /// <param name="tile">Tile to test for visibility.</param>
        /// <returns>True when the tile lies in the side's visible set.</returns>
        public static bool IsVisible(Battlefield field, Affiliation viewerSide, GridPoint tile) =>
            VisibleTiles(field, viewerSide).Contains(tile);

        /// <summary>Lists live foes of a viewer standing outside its side's sight.</summary>
        /// <param name="field">Battlefield holding board bounds and placements.</param>
        /// <param name="viewer">Placed unit whose side defines visibility and whose foes are listed.</param>
        /// <returns>Live enemies hidden from the viewer's side.</returns>
        public static List<PlacedUnit> HiddenEnemiesOf(Battlefield field, PlacedUnit viewer)
        {
            var visible = VisibleTiles(field, viewer.Unit.Side);
            var hidden = new List<PlacedUnit>();
            foreach (PlacedUnit foe in field.LiveEnemiesOf(viewer))
                if (!visible.Contains(foe.Position)) hidden.Add(foe);
            return hidden;
        }
    }
}
