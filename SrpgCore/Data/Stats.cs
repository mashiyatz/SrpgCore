namespace SrpgCore.Data
{
    /// <summary>
    /// GBA-style stat block: MaxHP, Power (Str or Mag — the weapon's IsMagic flag
    /// selects Def vs Res on the target), Skill (hit, crit), Speed (attack speed,
    /// relieved by Constitution against weapon weight), Luck (hit, avoid, crit
    /// dodge), Defense, Resistance, Constitution (carry capacity and attack-speed
    /// relief). Level-ups roll each stat against GrowthRates; promotion adds fixed
    /// gains passed by the caller.
    /// </summary>
    public sealed class Stats
    {
        /// <summary>Maximum hit points, also the spawn value for CurrentHP.</summary>
        public int MaxHP { get; set; }
        /// <summary>Strength or magic power, selected by the weapon used.</summary>
        public int Power { get; set; }
        /// <summary>Accuracy and crit-rate driver for GBA combat math.</summary>
        public int Skill { get; set; }
        /// <summary>Attack speed driver and GBA doubling threshold stat.</summary>
        public int Speed { get; set; }
        /// <summary>Luck driver for hit, avoid, and crit evade.</summary>
        public int Luck { get; set; }
        /// <summary>Physical damage reduction against non-magic weapons.</summary>
        public int Defense { get; set; }
        /// <summary>Magical damage reduction against magic weapons.</summary>
        public int Resistance { get; set; }
        /// <summary>Weight relief against weapon slowdown for attack speed.</summary>
        public int Constitution { get; set; }

        /// <summary>Copies every stat field into a fresh block.</summary>
        /// <returns>A new Stats with identical field values.</returns>
        public Stats Clone() => (Stats)MemberwiseClone();
    }
}
