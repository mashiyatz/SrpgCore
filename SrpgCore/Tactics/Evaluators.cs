using SrpgCore.Combat;
using SrpgCore.Data;
using SrpgCore.Grid;

namespace SrpgCore.Tactics
{
    /// <summary>Port of TBSF evaluators (DamageDealtPosition/Target, Distance, Health) onto SrpgCore forecasts. Used by behaviour-tree nodes.</summary>
    public static class Evaluators
    {
        /// <summary>Expected damage dealt from tile to target (SrpgCore Forecast Damage times hit percent).</summary>
        /// <param name="atk">Attacking unit.</param>
        /// <param name="w">Attacker weapon.</param>
        /// <param name="def">Defending unit.</param>
        /// <param name="defW">Defender weapon (null when disarmed).</param>
        /// <param name="aPos">Attacker tile.</param>
        /// <param name="dPos">Defender tile.</param>
        /// <param name="board">Board for terrain lookups.</param>
        /// <param name="r">Combat rules (null selects defaults).</param>
        /// <returns>Expected damage including crit bonus.</returns>
        public static double DamageDealt(Unit atk, Weapon w, Unit def, Weapon defW, GridPoint aPos, GridPoint dPos, Board board, CombatRules r)
        {
            board.GetTerrain(aPos); board.GetTerrain(dPos);
            var fc = CombatResolver.Forecast(atk,w,def,defW,0,0,0,0,r);
            return fc.Damage * (fc.Hit/100.0) + fc.Damage*2*(fc.Crit/100.0);
        }
        /// <summary>Manhattan distance evaluator.</summary>
        /// <param name="a">First tile.</param>
        /// <param name="b">Second tile.</param>
        /// <returns>Manhattan distance between tiles.</returns>
        public static int Distance(GridPoint a, GridPoint b) => a.ManhattanDistance(b);
        /// <summary>Health ratio 0-1.</summary>
        /// <param name="u">Unit to measure.</param>
        /// <returns>Current HP divided by MaxHP (0 when MaxHP is 0).</returns>
        public static double HealthRatio(Unit u) => u.Stats.MaxHP==0?0: (double)u.CurrentHP/u.Stats.MaxHP;
    }
}
