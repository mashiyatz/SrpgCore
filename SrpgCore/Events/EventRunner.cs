using System;
using System.Collections.Generic;

namespace SrpgCore.Events
{
    /// <summary>
    /// Implemented by the host engine (Prowl presentation layer, tests, future ports).
    /// The core validates and orders commands; only the executor touches the outside world.
    /// </summary>
    public interface IEventExecutor
    {
        /// <summary>Applies one validated command to the outside world.</summary>
        /// <param name="command">The validated command to execute.</param>
        void Execute(EventCommand command);
    }

    /// <summary>Outcome of one <see cref="EventRunner"/> run including validation failures.</summary>
    public sealed class EventRunResult
    {
        /// <summary>Gets or sets whether the condition passed and the commands ran.</summary>
        public bool Ran { get; set; }            // false when the condition filtered it out
        /// <summary>Gets or sets how many commands the executor ran in order.</summary>
        public int CommandsExecuted { get; set; }
        /// <summary>Gets or sets the condition or validation failure or null on success.</summary>
        public string? Error { get; set; }
    }

    /// <summary>
    /// Runs one event: checks the condition against trigger vars, validates every command
    /// up front (fail before side effects), then dispatches in order. Trigger dispatch
    /// across many events belongs to the future TurnManager, not here.
    /// </summary>
    public static class EventRunner
    {
        /// <summary>Runs one event when its condition passes; validates all commands before any side effect.</summary>
        /// <param name="gameEvent">The event definition (trigger already matched by the caller).</param>
        /// <param name="vars">Trigger variables visible to the condition language.</param>
        /// <param name="executor">Host that executes each validated command.</param>
        /// <returns>Whether the event ran, commands executed, and any validation error.</returns>
        public static EventRunResult Run(
            GameEvent gameEvent,
            IReadOnlyDictionary<string, object?> vars,
            IEventExecutor executor)
        {
            if (gameEvent == null) throw new ArgumentNullException(nameof(gameEvent));
            if (executor == null) throw new ArgumentNullException(nameof(executor));

            var result = new EventRunResult();
            bool passes;
            try
            {
                passes = SimpleConditionEvaluator.Evaluate(gameEvent.Condition, vars);
            }
            catch (FormatException ex)
            {
                result.Error = $"Event '{gameEvent.Name}': bad condition: {ex.Message}";
                return result;
            }
            if (!passes) return result;

            foreach (EventCommand cmd in gameEvent.Commands)
                if (!CommandCatalog.TryValidate(cmd, out string? error))
                {
                    result.Error = $"Event '{gameEvent.Name}': {error}";
                    return result;
                }

            foreach (EventCommand cmd in gameEvent.Commands)
            {
                executor.Execute(cmd);
                result.CommandsExecuted++;
            }
            result.Ran = true;
            return result;
        }
    }
}
