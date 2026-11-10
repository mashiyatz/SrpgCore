namespace SrpgCore.Events
{
    /// <summary>
    /// Engine-raised triggers. The TurnManager/battle code raises these; GameEvents
    /// subscribe to them.
    /// </summary>
    public enum EventTrigger
    {
        /// <summary>Fires when the chapter opens.</summary>
        LevelStart = 0,
        /// <summary>Fires when the chapter closes.</summary>
        LevelEnd = 1,
        /// <summary>Fires at each player phase boundary.</summary>
        PlayerPhaseStart = 2,   // LT: turn_change
        /// <summary>Fires at each enemy phase boundary.</summary>
        EnemyPhaseStart = 3,    // LT: enemy_turn_change
        /// <summary>Fires when a unit dies with unit nid and position available.</summary>
        UnitDeath = 4,          // vars: unit (nid), position
        /// <summary>Fires when a unit waits with unit nid and position available.</summary>
        UnitWait = 5,           // vars: unit, position
        /// <summary>Fires when a unit is selected with unit nid and position available.</summary>
        UnitSelect = 6,         // vars: unit, position
        /// <summary>Fires when a unit levels up with unit nid available.</summary>
        UnitLevelUp = 7,        // vars: unit
        /// <summary>Fires when combat starts with attacker, defender, item, and position available.</summary>
        CombatStart = 8,        // vars: unit, unit2, item, position
        /// <summary>Fires when combat ends with attacker, defender, item, and position available.</summary>
        CombatEnd = 9,          // vars: unit, unit2, item, position
        /// <summary>Fires when two units talk with both units and position available.</summary>
        Talk = 10,              // vars: unit, unit2, position
        /// <summary>Fires when a support grows with both units and rank available.</summary>
        Support = 11,           // vars: unit, unit2, rank
        /// <summary>Fires when a unit enters a region with unit, region, and position available.</summary>
        RegionEntered = 12,     // vars: unit, region, position
    }
}
