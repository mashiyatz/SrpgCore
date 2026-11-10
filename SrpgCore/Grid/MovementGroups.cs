using System.Collections.Generic;

namespace SrpgCore.Grid
{
    /// <summary>Named movement groups kept data-driven so new groups need only a cost column per terrain.</summary>
    public static class MovementGroups
    {
        /// <summary>Gets the infantry movement group name.</summary>
        public const string Infantry = "infantry";
        /// <summary>Gets the armor movement group name.</summary>
        public const string Armor = "armor";
        /// <summary>Gets the cavalry movement group name.</summary>
        public const string Cavalry = "cavalry";
        /// <summary>Gets the flier movement group name.</summary>
        public const string Flier = "flier";

        /// <summary>Maps a move type to its movement group name.</summary>
        /// <param name="moveType">The move type to map.</param>
        /// <returns>The movement group name for the move type.</returns>
        public static string For(MoveType moveType)
        {
            switch (moveType)
            {
                case MoveType.Armor: return Armor;
                case MoveType.Cavalry: return Cavalry;
                case MoveType.Flier: return Flier;
                default: return Infantry;
            }
        }
    }
}
