using System;
using System.Collections.Generic;
using SrpgCore.Grid;

namespace SrpgCore.Data
{
    /// <summary>A weapon carried by a unit, with remaining durability (UsesLeft starts at MaxUses; IsBroken at zero, and broken entries cannot strike).</summary>
    public sealed class InventoryEntry
    {
        /// <summary>Shared weapon definition for this carried item.</summary>
        public Weapon Weapon { get; }
        /// <summary>Remaining durability, decremented per strike attempt.</summary>
        public int UsesLeft { get; set; }

        /// <summary>Carries one weapon with remaining durability defaulting to full.</summary>
        /// <param name="weapon">Shared weapon definition to carry.</param>
        /// <param name="usesLeft">Remaining uses, defaults to the weapon maximum.</param>
        public InventoryEntry(Weapon weapon, int? usesLeft = null)
        {
            Weapon = weapon ?? throw new ArgumentNullException(nameof(weapon));
            UsesLeft = usesLeft ?? weapon.MaxUses;
        }

        /// <summary>True when no durability remains.</summary>
        public bool IsBroken => UsesLeft <= 0;
    }

    /// <summary>
    /// Battlefield unit: identity (Nid, Name, Side, ClassKey), growth state (Level,
    /// Experience toward 100 per level, Stats), live HP (CurrentHP, IsAlive while
    /// above zero), movement (MovePoints, MovementGroup), Vision fog radius, and
    /// collections: Inventory entries with durability, Skill ids, Statuses (name to
    /// turns left), Wexp per weapon type, CarryingNid ("" when hands-free). The
    /// constructor takes nid, name, side, class key, and stats (HP starts at MaxHP,
    /// Level at 1); Clone re-keys copies for reinforcements, staging, and saves.
    /// </summary>
    public sealed class Unit
    {
        /// <summary>Unique id used by events, conditions, and spawn references.</summary>
        public string Nid { get; }        // unique id, used by events/conditions
        /// <summary>Display name for dialogue and menus.</summary>
        public string Name { get; set; }
        /// <summary>Which army this unit fights for.</summary>
        public Affiliation Side { get; set; }
        /// <summary>Class database key, rekeyed by promotion.</summary>
        public string ClassKey { get; set; }
        /// <summary>Per-unit art override key into the host pack sprite library ("" = class art).
        /// Display-only data like Name: rules never read it; hosts resolve it.</summary>
        public string SpriteKey { get; set; } = "";
        /// <summary>Current level driving experience gain math.</summary>
        public int Level { get; set; }
        /// <summary>Accumulated experience toward the next level at 100.</summary>
        public int Experience { get; set; }
        /// <summary>Live stat block mutated by level-ups and promotion gains.</summary>
        public Stats Stats { get; set; }
        /// <summary>Current hit points, dead at zero.</summary>
        public int CurrentHP { get; set; }

        /// <summary>Personal movement allowance in tiles (default 5). Promotion adopts the new class move; Rescue halves it while laden.</summary>
        public int MovePoints { get; set; } = 5;

        /// <summary>Movement group key into terrain costs (default infantry).</summary>
        public string MovementGroup { get; set; } = MovementGroups.Infantry;

        /// <summary>Fog sight radius in Manhattan tiles (default 5, GBA-ish).</summary>
        public int Vision { get; set; } = 5;

        /// <summary>Skill ids on this unit (see Combat.SkillCatalog for effects).</summary>
        public List<string> Skills { get; } = new List<string>();

        /// <summary>Active status effects: name -&gt; turns remaining (poison/sleep/silence/...).</summary>
        public Dictionary<string, int> Statuses { get; } = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        /// <summary>Nid of the rescued passenger being carried ("" = none).</summary>
        public string CarryingNid { get; set; } = "";

        /// <summary>AI behavior type for enemy units (rush, hold, patrol).</summary>
        public string AiType { get; set; } = "rush";
        /// <summary>Patrol waypoint A for patrol AI (ignored otherwise).</summary>
        public Grid.GridPoint PatrolA { get; set; }
        /// <summary>Patrol waypoint B for patrol AI (ignored otherwise).</summary>
        public Grid.GridPoint PatrolB { get; set; }

        /// <summary>Weapon experience per type (see Data.WeaponRanks for thresholds).</summary>
        public Dictionary<WeaponType, int> Wexp { get; } = new Dictionary<WeaponType, int>();

        private readonly List<InventoryEntry> _inventory = new List<InventoryEntry>();
        /// <summary>Carried weapons with per-entry durability.</summary>
        public IList<InventoryEntry> Inventory => _inventory;

        /// <summary>True while CurrentHP is above zero.</summary>
        public bool IsAlive => CurrentHP > 0;

        /// <summary>Copy for reinforcements/staging; optionally re-keyed (nids must stay unique on a map).</summary>
        /// <param name="newNid">Replacement nid, defaults to keeping the source nid.</param>
        /// <returns>A deep copy with matching stats, inventory, wexp, skills, and statuses.</returns>
        public Unit Clone(string? newNid = null)
        {
            var clone = new Unit(newNid ?? Nid, Name, Side, ClassKey, Stats.Clone())
            {
                Level = Level,
                Experience = Experience,
                CurrentHP = CurrentHP,
                MovePoints = MovePoints,
                MovementGroup = MovementGroup,
                Vision = Vision,
                CarryingNid = CarryingNid,
                SpriteKey = SpriteKey,
                AiType = AiType,
                PatrolA = PatrolA,
                PatrolB = PatrolB,
            };
            foreach (InventoryEntry e in _inventory)
                clone._inventory.Add(new InventoryEntry(e.Weapon, e.UsesLeft));
            foreach (var kv in Wexp) clone.Wexp[kv.Key] = kv.Value;
            foreach (string s in Skills) clone.Skills.Add(s);
            foreach (var kv in Statuses) clone.Statuses[kv.Key] = kv.Value;
            return clone;
        }

        /// <summary>Creates a unit at full health from a stat block.</summary>
        /// <param name="nid">Unique id used by events and conditions.</param>
        /// <param name="name">Display name for dialogue and menus.</param>
        /// <param name="side">Which army this unit fights for.</param>
        /// <param name="classKey">Class database key.</param>
        /// <param name="stats">Stat block the unit shares by reference until cloned.</param>
        public Unit(string nid, string name, Affiliation side, string classKey, Stats stats)
        {
            Nid = nid ?? throw new ArgumentNullException(nameof(nid));
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Side = side;
            ClassKey = classKey ?? throw new ArgumentNullException(nameof(classKey));
            Stats = stats ?? throw new ArgumentNullException(nameof(stats));
            CurrentHP = stats.MaxHP;
            Level = 1;
        }
    }
}
