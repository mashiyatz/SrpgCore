namespace SrpgCore.Data
{
    /// <summary>Weapon damage triangle and magic families plus staves.</summary>
    public enum WeaponType
    {
        /// <summary>Beats axes, loses to lances in the GBA triangle.</summary>
        Sword = 0,
        /// <summary>Beats swords, loses to axes in the GBA triangle.</summary>
        Lance = 1,
        /// <summary>Beats lances, loses to swords in the GBA triangle.</summary>
        Axe = 2,
        /// <summary>Ranged physical weapon, never participates in triangle bonus.</summary>
        Bow = 3,
        /// <summary>Fire, thunder, and wind tomes.</summary>
        Anima = 4,
        /// <summary>Light magic tomes.</summary>
        Light = 5,
        /// <summary>Dark magic tomes.</summary>
        Dark = 6,
        /// <summary>Healing and status staves, targets allies.</summary>
        Staff = 7,
    }

    /// <summary>Weapon rank letter gating who can equip a weapon, E lowest through S highest.</summary>
    public enum WeaponRank
    {
        /// <summary>Entry rank, always reachable at zero wexp.</summary>
        E = 0,
        /// <summary>Unlocked at 20 wexp.</summary>
        D = 1,
        /// <summary>Unlocked at 50 wexp.</summary>
        C = 2,
        /// <summary>Unlocked at 80 wexp.</summary>
        B = 3,
        /// <summary>Unlocked at 120 wexp.</summary>
        A = 4,
        /// <summary>Top rank, unlocked at 180 wexp.</summary>
        S = 5,
    }

    /// <summary>Which army a unit fights for. Values: Player (player-controlled), Enemy (hostile), Ally (NPC own phase), Other (neutral, fights nobody).</summary>
    public enum Affiliation
    {
        /// <summary>Player-controlled units.</summary>
        Player = 0,
        /// <summary>Hostile units acting on the enemy phase.</summary>
        Enemy = 1,
        /// <summary>NPC ally with its own phase, never player-controlled.</summary>
        Ally = 2,   // NPC ally, own phase
        /// <summary>Neutral third party fighting nobody.</summary>
        Other = 3,  // neutral / third party
    }
}
