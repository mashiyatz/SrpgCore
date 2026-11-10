using System;
using System.Collections.Generic;

namespace SrpgCore.Events
{
    /// <summary>
    /// Scripted event: unique name + trigger + condition expression + command list.
    /// Conditions evaluate against trigger variables (see SimpleConditionEvaluator).
    /// </summary>
    public sealed class GameEvent
    {
        /// <summary>Gets the unique author facing event name.</summary>
        public string Name { get; }
        /// <summary>Gets the trigger this event subscribes to.</summary>
        public EventTrigger Trigger { get; }
        /// <summary>Gets the gating expression where empty means always run.</summary>
        public string Condition { get; }  // empty = always run

        /// <summary>Dispatch order among same-trigger events (LT default 20; higher runs first).</summary>
        public int Priority { get; set; }

        private readonly List<EventCommand> _commands = new List<EventCommand>();
        /// <summary>Gets the ordered command list dispatched when the event runs.</summary>
        public IList<EventCommand> Commands => _commands;

        /// <summary>Initializes an event with a name, trigger, and optional gating condition.</summary>
        /// <param name="name">The unique author facing event name.</param>
        /// <param name="trigger">The trigger this event subscribes to.</param>
        /// <param name="condition">The gating expression where empty means always run.</param>
        public GameEvent(string name, EventTrigger trigger, string condition = "")
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Trigger = trigger;
            Condition = condition ?? "";
        }

        /// <summary>Parses one command line and appends it to the ordered command list.</summary>
        /// <param name="line">The source line shaped verb plus semicolon separated args.</param>
        public void AddCommand(string line) => _commands.Add(EventCommand.Parse(line));
    }
}
