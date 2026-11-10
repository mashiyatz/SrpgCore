namespace SrpgCore.Grid
{
    /// <summary>Movement class. Drives per-terrain move costs (fliers ignore most terrain).</summary>
    public enum MoveType
    {
        /// <summary>Standard foot movement.</summary>
        Infantry = 0,
        /// <summary>Slow heavily armored movement.</summary>
        Armor = 1,
        /// <summary>Mounted movement.</summary>
        Cavalry = 2,
        /// <summary>Flying movement ignoring most terrain costs.</summary>
        Flier = 3,
    }
}
