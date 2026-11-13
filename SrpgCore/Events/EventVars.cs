using System;
using System.Collections.Generic;

namespace SrpgCore.Events
{
    /// <summary>
    /// Chapter state vars (set_flag flags, opened_/visited_ latches, torch_ entries)
    /// merged under trigger vars for condition evaluation. Trigger entries win on
    /// collision, so engine names (turn/unit/region/...) can never be shadowed by a
    /// set_flag. Lookup stays case-sensitive, matching SimpleConditionEvaluator.
    /// Shallow merge: the top level is fresh, nested dicts are shared by reference,
    /// so executors must not mutate vars.
    /// Migration note: unknown paths are null, and null != anything is true, so a
    /// `done != true` gate is true before the flag exists and flips false once any
    /// `set_flag;done;true` lands; `x == false` is false until `set_flag;x;false`
    /// is stored. Prefer latch-shaped names (opened_/visited_) and keep set_flag
    /// names clear of engine top-level names.
    /// </summary>
    public static class EventVars
    {
        /// <summary>Merges state vars under trigger vars; trigger entries win key collisions.</summary>
        /// <param name="triggerVars">Per-dispatch trigger variables (unit/region/turn/...).</param>
        /// <param name="stateVars">Chapter state vars (set_flag flags, opened_/visited_ latches).</param>
        /// <returns>New dictionary: every state entry, overridden by trigger entries.</returns>
        public static IReadOnlyDictionary<string, object?> WithStateVars(
            IReadOnlyDictionary<string, object?> triggerVars,
            IReadOnlyDictionary<string, object?> stateVars)
        {
            if (triggerVars == null) throw new ArgumentNullException(nameof(triggerVars));
            if (stateVars == null) throw new ArgumentNullException(nameof(stateVars));
            var merged = new Dictionary<string, object?>();
            foreach (var kv in stateVars) merged[kv.Key] = kv.Value;
            foreach (var kv in triggerVars) merged[kv.Key] = kv.Value;
            return merged;
        }
    }
}
