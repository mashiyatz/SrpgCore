using SrpgCore.Combat;
using SrpgCore.Data;
using SrpgCore.Grid;

namespace SrpgCore.Tactics
{
    /// <summary>Port of TBSF evaluators (DamageDealtPosition/Target, Distance, Health) onto SrpgCore forecasts. Used by behaviour-tree nodes.</summary>
    public static class Evaluators
    {
        /// <summary>Expected damage dealt from tile to target (SrpgCore Forecast Damage * hit%).</summary>
        public static double DamageDealt(Unit atk, Weapon w, Unit def, Weapon defW, GridPoint aPos, GridPoint dPos, Board board, CombatRules r)
        {
            board.GetTerrain(aPos); board.GetTerrain(dPos);
            var fc = CombatResolver.Forecast(atk,w,def,defW,0,0,0,0,r);
            return fc.Damage * (fc.Hit/100.0) + fc.Damage*2*(fc.Crit/100.0);
        }
        /// <summary>Manhattan distance evaluator.</summary>
        public static int Distance(GridPoint a, GridPoint b) => a.ManhattanDistance(b);
        /// <summary>Health ratio 0-1.</summary>
        public static double HealthRatio(Unit u) => u.Stats.MaxHP==0?0: (double)u.CurrentHP/u.Stats.MaxHP;
    }
}
