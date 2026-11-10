using System;
using System.Collections.Generic;

namespace SrpgCore.Grid
{
    /// <summary>Immutable terrain definition. Values: Key (unique id), Name, Avoid, Defense, HpEffectPercent (heal/damage percent), and per-group entry costs (MoveCost/IsPassable; &lt;0 or &gt;=99 means impassable).</summary>
    public sealed class TerrainType
    {
        /// <summary>Gets the cost at or above which terrain counts as impassable.</summary>
        public const int ImpassableThreshold = 99;

        /// <summary>Gets the unique terrain key.</summary>
        public string Key { get; }
        /// <summary>Gets the display name.</summary>
        public string Name { get; }
        /// <summary>Gets the avoid bonus granted to units on this terrain.</summary>
        public int Avoid { get; }
        /// <summary>Gets the defense bonus granted to units on this terrain.</summary>
        public int Defense { get; }
        /// <summary>Gets the per-round HP swing as percent of max HP (positive heals, negative damages, zero none).</summary>
        public int HpEffectPercent { get; }

        private readonly Dictionary<string, int> _moveCosts;

        /// <summary>Initializes a terrain with classic four-group costs.</summary>
        /// <param name="key">The unique terrain key.</param>
        /// <param name="name">The display name.</param>
        /// <param name="infantry">The infantry entry cost.</param>
        /// <param name="armor">The armor entry cost.</param>
        /// <param name="cavalry">The cavalry entry cost.</param>
        /// <param name="flier">The flier entry cost.</param>
        /// <param name="avoid">The avoid bonus.</param>
        /// <param name="defense">The defense bonus.</param>
        /// <param name="hpEffectPercent">The per-round HP swing percent.</param>
        public TerrainType(string key, string name, int infantry, int armor, int cavalry, int flier,
            int avoid, int defense, int hpEffectPercent = 0)
            : this(key, name, new Dictionary<string, int>
            {
                [MovementGroups.Infantry] = infantry,
                [MovementGroups.Armor] = armor,
                [MovementGroups.Cavalry] = cavalry,
                [MovementGroups.Flier] = flier,
            }, avoid, defense, hpEffectPercent)
        {
        }

        /// <summary>Initializes a terrain with custom movement group costs.</summary>
        /// <param name="key">The unique terrain key.</param>
        /// <param name="name">The display name.</param>
        /// <param name="moveCosts">The entry costs keyed by movement group name.</param>
        /// <param name="avoid">The avoid bonus.</param>
        /// <param name="defense">The defense bonus.</param>
        /// <param name="hpEffectPercent">The per-round HP swing percent.</param>
        public TerrainType(string key, string name, IDictionary<string, int> moveCosts,
            int avoid, int defense, int hpEffectPercent = 0)
        {
            Key = key ?? throw new ArgumentNullException(nameof(key));
            Name = name ?? throw new ArgumentNullException(nameof(name));
            _moveCosts = new Dictionary<string, int>(moveCosts ?? throw new ArgumentNullException(nameof(moveCosts)));
            Avoid = avoid;
            Defense = defense;
            HpEffectPercent = hpEffectPercent;
        }

        /// <summary>Returns the entry cost for the given movement group, defaulting to 1 for unknown groups.</summary>
        /// <param name="movementGroup">The movement group name.</param>
        /// <returns>The entry cost for the group.</returns>
        public int MoveCost(string movementGroup)
        {
            if (movementGroup == null) throw new ArgumentNullException(nameof(movementGroup));
            return _moveCosts.TryGetValue(movementGroup, out int cost) ? cost : 1;
        }

        /// <summary>Returns the entry cost for the given move type.</summary>
        /// <param name="moveType">The move type mapped to a movement group.</param>
        /// <returns>The entry cost for the move type.</returns>
        public int MoveCost(MoveType moveType) => MoveCost(MovementGroups.For(moveType));

        /// <summary>Returns true when the terrain is passable for the given movement group.</summary>
        /// <param name="movementGroup">The movement group name.</param>
        /// <returns>True when the cost is at least 0 and below the impassable threshold.</returns>
        public bool IsPassable(string movementGroup)
        {
            int cost = MoveCost(movementGroup);
            return cost >= 0 && cost < ImpassableThreshold;
        }

        /// <summary>Returns true when the terrain is passable for the given move type.</summary>
        /// <param name="moveType">The move type mapped to a movement group.</param>
        /// <returns>True when passable for the move type.</returns>
        public bool IsPassable(MoveType moveType) => IsPassable(MovementGroups.For(moveType));
    }
}
