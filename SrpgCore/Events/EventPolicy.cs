using System;
using System.Collections.Generic;

namespace SrpgCore.Events
{
    /// <summary>Trust mode for event execution. Trusted preserves current behavior.</summary>
    public enum EventTrust
    {
        /// <summary>Full catalog allowed (default, current behavior).</summary>
        Trusted = 0,
        /// <summary>High-impact verbs denied (untrusted/mod content).</summary>
        Restricted = 1,
    }

    /// <summary>
    /// Policy gate for high-impact verbs. Restricted mode blocks world/progression
    /// mutations (spawns, removal, terrain, economy, scripted result) while leaving
    /// narrative and local unit verbs available. Enforcement belongs in the shared
    /// validation path so loader, editor, and runtime agree.
    /// </summary>
    public static class EventPolicy
    {
        private static readonly HashSet<string> HighImpact = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "spawn_unit",
            "remove_unit",
            "set_terrain",
            "gold",
            "buy",
            "sell",
            "win",
            "lose",
        };

        /// <summary>True when the verb is high-impact (blocked under Restricted).</summary>
        public static bool IsHighImpact(string verb) =>
            !string.IsNullOrEmpty(verb) && HighImpact.Contains(verb.Trim());

        /// <summary>Validates catalog arity/semantics then applies the trust policy.</summary>
        public static bool TryValidateWithPolicy(EventCommand command, EventTrust trust, out string? error)
        {
            if (!CommandCatalog.TryValidate(command, out error))
                return false;
            if (trust == EventTrust.Restricted && IsHighImpact(command.Verb))
            {
                error = $"Event command '{command.Verb}' is blocked by Restricted policy.";
                return false;
            }
            error = null;
            return true;
        }
    }
}
