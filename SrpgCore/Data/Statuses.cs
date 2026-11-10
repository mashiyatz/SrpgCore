using System;
using System.Collections.Generic;
using SrpgCore.Data;

namespace SrpgCore.Data
{
    /// <summary>
    /// Status effects (poison/sleep/silence/...): name -&gt; turns remaining on Unit.Statuses.
    /// Pure helpers; hosts tick at phase boundaries and gate actions (sleep/silence) themselves.
    /// </summary>
    public static class Statuses
    {
        /// <summary>Applies a named status for a positive duration, refreshing any existing entry.</summary>
        /// <param name="unit">Unit whose Statuses map to mutate.</param>
        /// <param name="name">Status name, trimmed and lowercased.</param>
        /// <param name="duration">Turns remaining, must be at least 1.</param>
        public static void Apply(Unit unit, string name, int duration)
        {
            if (unit == null) throw new ArgumentNullException(nameof(unit));
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Status needs a name.", nameof(name));
            if (duration < 1) throw new ArgumentOutOfRangeException(nameof(duration));
            unit.Statuses[name.Trim().ToLowerInvariant()] = duration;
        }

        /// <summary>Tests whether a live status entry remains on the unit.</summary>
        /// <param name="unit">Unit whose Statuses map to read.</param>
        /// <param name="name">Status name, trimmed and lowercased.</param>
        /// <returns>True when the entry exists with turns remaining above zero.</returns>
        public static bool Has(Unit unit, string name) =>
            unit != null && unit.Statuses.TryGetValue(name.Trim().ToLowerInvariant(), out int d) && d > 0;

        /// <summary>True when unit is silenced (blocks magic and staff).</summary>
        /// <param name="unit">Unit to test.</param>
        /// <returns>True when silenced.</returns>
        public static bool IsSilenced(Unit unit) => Has(unit, "silence");

        /// <summary>Removes one status entry regardless of remaining duration.</summary>
        /// <param name="unit">Unit whose Statuses map to mutate.</param>
        /// <param name="name">Status name, trimmed and lowercased.</param>
        /// <returns>True when an entry was present and removed.</returns>
        public static bool Clear(Unit unit, string name) =>
            unit != null && unit.Statuses.Remove(name.Trim().ToLowerInvariant());

        /// <summary>Decrement all durations by 1; expired entries are removed. Returns expired names in sorted order.</summary>
        /// <param name="unit">Unit whose Statuses map to tick.</param>
        /// <returns>Names that reached zero and were removed.</returns>
        public static List<string> Tick(Unit unit)
        {
            if (unit == null) throw new ArgumentNullException(nameof(unit));
            var expired = new List<string>();
            var keys = new List<string>(unit.Statuses.Keys);
            keys.Sort(StringComparer.Ordinal);
            foreach (string k in keys)
            {
                if (!unit.Statuses.ContainsKey(k)) continue;
                unit.Statuses[k]--;
                if (unit.Statuses[k] <= 0)
                {
                    unit.Statuses.Remove(k);
                    expired.Add(k);
                }
            }
            expired.Sort(StringComparer.Ordinal);
            return expired;
        }
    }
}
