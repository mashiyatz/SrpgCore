using System;
using SrpgCore.Grid;

namespace SrpgCore.Data
{
    /// <summary>Class definition (database entry). Values: Key id, Name, MoveType family for terrain costs, Move base points, and optional PromotesTo target key.</summary>
    public sealed class UnitClass
    {
        /// <summary>Database key used by Unit.ClassKey and chapter class fields.</summary>
        public string Key { get; }
        /// <summary>Display name for menus and promotion screens.</summary>
        public string Name { get; }
        /// <summary>Movement family resolving terrain costs via MovementGroups.</summary>
        public MoveType MoveType { get; }
        /// <summary>Base movement points adopted on promotion.</summary>
        public int Move { get; }
        /// <summary>Promotion target class key, null when this class cannot promote.</summary>
        public string? PromotesTo { get; }

        /// <summary>Defines one class database entry with movement and promotion target.</summary>
        /// <param name="key">Database key other data references.</param>
        /// <param name="name">Display name for menus.</param>
        /// <param name="moveType">Movement family for terrain costs.</param>
        /// <param name="move">Base movement points.</param>
        /// <param name="promotesTo">Promotion target class key, null for no promotion.</param>
        public UnitClass(string key, string name, MoveType moveType, int move, string? promotesTo = null)
        {
            Key = key ?? throw new ArgumentNullException(nameof(key));
            Name = name ?? throw new ArgumentNullException(nameof(name));
            MoveType = moveType;
            Move = move;
            PromotesTo = promotesTo;
        }
    }
}
