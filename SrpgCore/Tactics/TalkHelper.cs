using System;
using System.Collections.Generic;
using SrpgCore.Data;
using SrpgCore.Grid;

namespace SrpgCore.Tactics
{
    /// <summary>
    /// Talk-command helpers: who can talk to whom, and the vars a Talk event reads.
    /// Any adjacent live unit qualifies regardless of side — ally recruits and enemy
    /// parleys alike — so hosts gate specific pairs with Talk-trigger conditions
    /// (e.g. `unit.nid == 'eirika' &amp;&amp; unit2.nid == 'garcia'`). On acceptance
    /// hosts run `recruit;unit2` (or any scripted effect). Pure: no mutation, no RNG.
    /// </summary>
    public static class TalkHelper
    {
        /// <summary>
        /// Pure adjacency+liveness predicate for one ordered pair (positions supplied
        /// by the caller so hosts can preview without moving tokens).
        /// </summary>
        /// <param name="a">First unit.</param>
        /// <param name="posA">Tile of the first unit.</param>
        /// <param name="b">Second unit.</param>
        /// <param name="posB">Tile of the second unit.</param>
        /// <returns>True when both are alive and orthogonally adjacent.</returns>
        public static bool CanTalk(Unit a, GridPoint posA, Unit b, GridPoint posB)
        {
            if (a == null || b == null) return false;
            if (!a.IsAlive || !b.IsAlive) return false;
            return posA.ManhattanDistance(posB) == 1;
        }

        /// <summary>Every adjacent live placed unit (exclude self), in placement order.</summary>
        /// <param name="field">Battlefield holding placements.</param>
        /// <param name="unit">Placed unit seeking conversation partners.</param>
        /// <returns>Adjacent live units in placement order.</returns>
        public static List<PlacedUnit> TalkCandidates(Battlefield field, PlacedUnit unit)
        {
            if (field == null) throw new ArgumentNullException(nameof(field));
            if (unit == null) throw new ArgumentNullException(nameof(unit));
            var list = new List<PlacedUnit>();
            foreach (PlacedUnit p in field.Placed)
            {
                if (ReferenceEquals(p, unit)) continue;
                if (CanTalk(unit.Unit, unit.Position, p.Unit, p.Position)) list.Add(p);
            }
            return list;
        }

        /// <summary>
        /// Vars for EventTrigger.Talk: unit (talker), unit2 (listener), position
        /// (the talker's tile) — same shape as BattleTriggers combat vars.
        /// </summary>
        /// <param name="talker">Unit initiating the talk.</param>
        /// <param name="listener">Unit being talked to.</param>
        /// <param name="position">Talker's tile.</param>
        /// <returns>Vars dictionary for the Talk trigger.</returns>
        public static Dictionary<string, object?> TalkVars(Unit talker, Unit listener, GridPoint position)
        {
            if (talker == null) throw new ArgumentNullException(nameof(talker));
            if (listener == null) throw new ArgumentNullException(nameof(listener));
            return new Dictionary<string, object?>
            {
                ["unit"] = new Dictionary<string, object?>
                {
                    ["nid"] = talker.Nid,
                    ["level"] = talker.Level,
                    ["hp"] = talker.CurrentHP,
                },
                ["unit2"] = new Dictionary<string, object?>
                {
                    ["nid"] = listener.Nid,
                    ["level"] = listener.Level,
                    ["hp"] = listener.CurrentHP,
                },
                ["position"] = new Dictionary<string, object?>
                {
                    ["x"] = position.X,
                    ["y"] = position.Y,
                },
            };
        }
    }
}
