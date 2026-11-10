using System;
using System.Collections.Generic;

namespace SrpgCore.Events
{
    /// <summary>
    /// Multi-event dispatch: all events subscribed to a trigger run in priority order
    /// (then name — deterministic; duplicates all run).
    /// </summary>
    public static class EventDispatcher
    {
        /// <summary>Runs every event subscribed to a trigger in priority then name order and stops at the first error.</summary>
        /// <param name="trigger">The raised trigger selecting which events run.</param>
        /// <param name="vars">Trigger variables visible to the condition language.</param>
        /// <param name="events">The candidate events of which only trigger matches run.</param>
        /// <param name="executor">Host that executes each validated command.</param>
        /// <returns>One result per attempted event in dispatch order.</returns>
        public static List<EventRunResult> Dispatch(
            EventTrigger trigger,
            IReadOnlyDictionary<string, object?> vars,
            IEnumerable<GameEvent> events,
            IEventExecutor executor)
        {
            if (events == null) throw new ArgumentNullException(nameof(events));
            var matching = new List<GameEvent>();
            foreach (GameEvent e in events)
                if (e.Trigger == trigger) matching.Add(e);
            matching.Sort((a, b) =>
            {
                int c = b.Priority.CompareTo(a.Priority);
                return c != 0 ? c : string.Compare(a.Name, b.Name, StringComparison.Ordinal);
            });

            var results = new List<EventRunResult>();
            foreach (GameEvent e in matching)
            {
                EventRunResult r = EventRunner.Run(e, vars, executor);
                results.Add(r);
                if (r.Error != null) break; // fail fast: never run later events after a broken one
            }
            return results;
        }
    }
}
