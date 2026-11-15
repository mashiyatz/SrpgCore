using System;
using System.Collections.Generic;

namespace SrpgCore.Events
{
    /// <summary>One planned event in a dispatch: pass/fail, validation, staging.</summary>
    public sealed class EventPlan
    {
        /// <summary>Authored event in dispatch order.</summary>
        public GameEvent Event { get; }
        /// <summary>True when the condition passed.</summary>
        public bool Passes { get; set; }
        /// <summary>Condition or validation/policy failure, null when clean.</summary>
        public string? Error { get; set; }
        /// <summary>Index of the failing command, -1 when not applicable.</summary>
        public int FailingCommandIndex { get; set; } = -1;
        /// <summary>True when the event needs staged (cutscene) presentation.</summary>
        public bool NeedsStaging { get; set; }

        /// <param name="gameEvent">Authored event.</param>
        public EventPlan(GameEvent gameEvent)
        {
            Event = gameEvent ?? throw new ArgumentNullException(nameof(gameEvent));
        }
    }

    /// <summary>
    /// Single authoritative event pipeline: match, sort, evaluate, validate, stage.
    /// Hosts (game, editor preview, talk dry-runs) must route through here instead of
    /// reimplementing matching/sorting/validation so semantics cannot drift.
    /// </summary>
    public static class EventOrchestrator
    {
        /// <summary>Verbs that require staged (blocking) presentation.</summary>
        public static bool IsBlockingVerb(EventCommand cmd)
        {
            if (cmd == null) return false;
            string v = cmd.Verb.Trim().ToLowerInvariant();
            return v == "speak" || v == "move_unit" || v == "wait";
        }

        /// <summary>Matching events for a trigger in priority desc, name asc order.</summary>
        public static List<GameEvent> MatchEvents(EventTrigger trigger, IEnumerable<GameEvent> events)
        {
            if (events == null) throw new ArgumentNullException(nameof(events));
            var matching = new List<GameEvent>();
            foreach (GameEvent e in events)
                if (e != null && e.Trigger == trigger) matching.Add(e);
            matching.Sort((a, b) =>
            {
                int c = b.Priority.CompareTo(a.Priority);
                return c != 0 ? c : string.Compare(a.Name, b.Name, StringComparison.Ordinal);
            });
            return matching;
        }

        /// <summary>Evaluates one event condition; FormatException becomes an error string.</summary>
        public static bool TryEvaluateCondition(GameEvent ev, IReadOnlyDictionary<string, object?> vars, out string? error)
        {
            if (ev == null) throw new ArgumentNullException(nameof(ev));
            if (vars == null) throw new ArgumentNullException(nameof(vars));
            try
            {
                bool passes = SimpleConditionEvaluator.Evaluate(ev.Condition, vars);
                error = null;
                return passes;
            }
            catch (FormatException ex)
            {
                error = $"Event '{ev.Name}': bad condition: {ex.Message}";
                return false;
            }
        }

        /// <summary>Validates every command up front (catalog + policy). First failure wins.</summary>
        public static bool TryValidateCommands(GameEvent ev, EventTrust trust, out string? error, out int failingIndex)
        {
            if (ev == null) throw new ArgumentNullException(nameof(ev));
            failingIndex = -1;
            for (int i = 0; i < ev.Commands.Count; i++)
            {
                if (!EventPolicy.TryValidateWithPolicy(ev.Commands[i], trust, out string? err))
                {
                    failingIndex = i;
                    error = $"Event '{ev.Name}' cmd {i}: {err}";
                    return false;
                }
            }
            error = null;
            return true;
        }

        /// <summary>True when any command needs staged presentation.</summary>
        public static bool NeedsStaging(GameEvent ev)
        {
            if (ev == null) return false;
            foreach (var cmd in ev.Commands)
                if (IsBlockingVerb(cmd)) return true;
            return false;
        }

        /// <summary>
        /// Full dispatch plan without side effects: match, evaluate, validate, stage.
        /// Callers execute inline or staged per plan; stop at the first error plan.
        /// </summary>
        public static List<EventPlan> PlanDispatch(
            EventTrigger trigger,
            IReadOnlyDictionary<string, object?> vars,
            IEnumerable<GameEvent> events,
            EventTrust trust = EventTrust.Trusted)
        {
            if (vars == null) throw new ArgumentNullException(nameof(vars));
            var plans = new List<EventPlan>();
            foreach (GameEvent ev in MatchEvents(trigger, events))
            {
                var plan = new EventPlan(ev);
                bool passes;
                try
                {
                    passes = SimpleConditionEvaluator.Evaluate(ev.Condition, vars);
                }
                catch (FormatException ex)
                {
                    plan.Error = $"Event '{ev.Name}': bad condition: {ex.Message}";
                    plans.Add(plan);
                    break; // fail fast like EventDispatcher
                }
                plan.Passes = passes;
                if (!passes) { plans.Add(plan); continue; }
                if (!TryValidateCommands(ev, trust, out string? verr, out int idx))
                {
                    plan.Error = verr;
                    plan.FailingCommandIndex = idx;
                    plans.Add(plan);
                    break;
                }
                plan.NeedsStaging = NeedsStaging(ev);
                plans.Add(plan);
            }
            return plans;
        }

        /// <summary>Structured one-line diagnostic for logs and editor problems.</summary>
        public static string FormatDiagnostic(string eventName, EventTrigger trigger, int commandIndex, string verb, string mode, string reason)
        {
            string cmd = commandIndex >= 0 ? $" cmd{commandIndex}" : "";
            string v = string.IsNullOrEmpty(verb) ? "" : $" verb={verb}";
            return $"event error [{mode}] trigger={trigger} event='{eventName}'{cmd}{v}: {reason}";
        }
    }
}
