using System;

namespace SrpgCore.Tactics
{
    /// <summary>Data-driven enemy scoring weights (VH and FE style additive scoring).</summary>
    public sealed class AiRules
    {
        /// <summary>Flat bonus for a secured kill, added to damage.</summary>
        public int KillBonus { get; set; } = 10000;
        /// <summary>Weight on expected counter damage subtracted as risk.</summary>
        public double CounterWeight { get; set; } = 0.5;
        /// <summary>Weight on threat count when parking where no strike lands.</summary>
        public double ParkingThreatWeight { get; set; } = 10.0;
        /// <summary>Bonus per terrain avoid point when parking.</summary>
        public double ParkingAvoidWeight { get; set; } = 1.0;
        /// <summary>Bonus per terrain defense point when parking.</summary>
        public double ParkingDefenseWeight { get; set; } = 1.0;

        /// <summary>Builds default scoring weights.</summary>
        public AiRules()
        {
        }
    }
}
