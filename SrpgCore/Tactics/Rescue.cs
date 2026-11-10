using System;
using SrpgCore.Data;
using SrpgCore.Grid;

namespace SrpgCore.Tactics
{
    /// <summary>
    /// Rescue (GBA simplify): a carrier grabs an adjacent non-hostile passenger whose
    /// Con fits (passenger Con &lt;= carrier Con + 2), carries one at a time, moves at
    /// half MovePoints while carrying, and the passenger leaves the map (returns on
    /// drop). Rescued units are untargetable because they are not placed; hosts hide
    /// their tokens. Cross-side carries are refused: enemies block pass-through, so
    /// they can never be scooped either.
    /// </summary>
    public static class Rescue
    {
        /// <summary>Tests whether a carrier can grab an adjacent passenger.</summary>
        /// <param name="carrier">Unit attempting the grab (must have hands free).</param>
        /// <param name="passenger">Unit to be carried (Con must fit).</param>
        /// <param name="carrierPos">Current tile of the carrier.</param>
        /// <param name="passengerPos">Current tile of the passenger.</param>
        /// <returns>True when adjacent, alive, same side, hands-free, and within Con capacity.</returns>
        public static bool CanGrab(Unit carrier, Unit passenger, GridPoint carrierPos, GridPoint passengerPos)
        {
            if (carrier == null || passenger == null) return false;
            if (!carrier.IsAlive || !passenger.IsAlive) return false;
            if (Battlefield.AreHostile(carrier.Side, passenger.Side)) return false;
            if (carrier.CarryingNid.Length != 0) return false;
            if (carrierPos.ManhattanDistance(passengerPos) != 1) return false;
            return passenger.Stats.Constitution <= carrier.Stats.Constitution + 2;
        }

        /// <summary>Move allowance for a carrier, halved while carrying a passenger.</summary>
        /// <param name="carrier">Carrier whose allowance is computed (null reads as 0).</param>
        /// <returns>Full move when unladen, half move when laden.</returns>
        public static int EffectiveMove(Unit carrier) =>
            carrier == null ? 0 : (carrier.CarryingNid.Length != 0 ? carrier.MovePoints / 2 : carrier.MovePoints);

        /// <summary>Grab: records the carry and removes the passenger token. Returns the removed token.</summary>
        /// <param name="field">Battlefield holding both tokens.</param>
        /// <param name="carrier">Placed carrier doing the grabbing.</param>
        /// <param name="passenger">Placed passenger being picked up.</param>
        /// <returns>The removed passenger token for host bookkeeping.</returns>
        public static PlacedUnit Grab(Battlefield field, PlacedUnit carrier, PlacedUnit passenger)
        {
            if (field == null) throw new ArgumentNullException(nameof(field));
            if (!CanGrab(carrier.Unit, passenger.Unit, carrier.Position, passenger.Position))
                throw new InvalidOperationException("Rescue conditions not met (adjacent, capacity, hands free).");
            carrier.Unit.CarryingNid = passenger.Unit.Nid;
            if (!field.Remove(passenger)) throw new InvalidOperationException("Passenger not placed.");
            return passenger;
        }

        /// <summary>Drop the carried nid on an adjacent free tile. Needs the passenger Unit (hosts hold it).</summary>
        /// <param name="field">Battlefield receiving the dropped passenger.</param>
        /// <param name="carrier">Placed carrier doing the dropping.</param>
        /// <param name="passenger">Passenger unit held by the host.</param>
        /// <param name="tile">Adjacent free tile receiving the passenger.</param>
        /// <returns>The newly deployed passenger token.</returns>
        public static PlacedUnit Drop(Battlefield field, PlacedUnit carrier, Unit passenger, GridPoint tile)
        {
            if (field == null) throw new ArgumentNullException(nameof(field));
            if (carrier.Unit.CarryingNid != passenger.Nid)
                throw new InvalidOperationException($"'{carrier.Unit.Nid}' is not carrying '{passenger.Nid}'.");
            if (carrier.Position.ManhattanDistance(tile) != 1) throw new InvalidOperationException("Drop must be adjacent.");
            if (!field.Board.InBounds(tile)) throw new ArgumentOutOfRangeException(nameof(tile));
            foreach (PlacedUnit p in field.Placed)
                if (p.Position.Equals(tile)) throw new InvalidOperationException("Drop tile occupied.");
            carrier.Unit.CarryingNid = "";
            return field.Deploy(passenger, tile);
        }
    }
}
