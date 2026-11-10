using System;
using System.Collections.Generic;

namespace SrpgCore.Events
{
    /// <summary>
    /// One event command: `verb;arg0;arg1;...`.
    /// Verbs are validated against <see cref="CommandCatalog"/>; the engine (Prowl or other)
    /// implements them in <see cref="IEventExecutor"/>.
    /// </summary>
    public sealed class EventCommand
    {
        /// <summary>Gets the verb name validated against <see cref="CommandCatalog"/>.</summary>
        public string Verb { get; }
        /// <summary>Gets the trimmed arguments following the verb in source order.</summary>
        public string[] Args { get; }

        /// <summary>Initializes a command with a verb and its argument list.</summary>
        /// <param name="verb">The command verb such as speak or move_unit.</param>
        /// <param name="args">The argument values in source order.</param>
        public EventCommand(string verb, string[] args)
        {
            Verb = verb ?? throw new ArgumentNullException(nameof(verb));
            Args = args ?? throw new ArgumentNullException(nameof(args));
        }

        /// <summary>Parses one semicolon separated line into a verb plus trimmed arguments.</summary>
        /// <param name="line">The source line shaped verb plus semicolon separated args.</param>
        /// <returns>The parsed command with verb and argument values.</returns>
        public static EventCommand Parse(string line)
        {
            if (line == null) throw new ArgumentNullException(nameof(line));
            string[] parts = line.Split(';');
            string verb = parts[0].Trim();
            if (verb.Length == 0) throw new FormatException("Event command is missing a verb.");
            var args = new string[parts.Length - 1];
            for (int i = 1; i < parts.Length; i++) args[i - 1] = parts[i].Trim();
            return new EventCommand(verb, args);
        }

        /// <summary>Formats the command back to its semicolon separated source shape.</summary>
        /// <returns>The verb followed by its semicolon separated arguments.</returns>
        public override string ToString() =>
            Args.Length == 0 ? Verb : Verb + ";" + string.Join(";", Args);
    }

    /// <summary>
    /// Known verbs with exact arity. The editor uses this for validation/autocomplete;
    /// the runner rejects unknown verbs before executing anything.
    /// </summary>
    public static class CommandCatalog
    {
        // verb -> (minArgs, maxArgs, description)
        private static readonly Dictionary<string, (int min, int max, string help)> Commands =
            new Dictionary<string, (int, int, string)>(StringComparer.OrdinalIgnoreCase)
            {
                ["speak"]         = (2, 2, "speak;unit_nid;text — dialogue line with portrait up."),
                ["say"]           = (2, 2, "say;unit_nid;text — ambient line, no advance."),
                ["add_portrait"]  = (2, 2, "add_portrait;unit_nid;Left|Right — show portrait."),
                ["remove_portrait"] = (1, 1, "remove_portrait;unit_nid — hide portrait."),
                ["move_unit"]     = (2, 2, "move_unit;unit_nid;x,y — walk unit to tile."),
                ["spawn_unit"]    = (3, 3, "spawn_unit;unit_nid;faction;x,y — reinforcement."),
                ["remove_unit"]   = (1, 1, "remove_unit;unit_nid — take unit off map."),
                ["give_item"]     = (2, 2, "give_item;unit_nid;weapon_key."),
                ["set_flag"]      = (2, 2, "set_flag;flag;true|false — persistent chapter flag."),
                ["win"]           = (0, 0, "win — end chapter in victory."),
                ["lose"]          = (0, 1, "lose[;reason] — end chapter in defeat."),
                ["wait"]          = (1, 1, "wait;seconds — pause cutscene."),
                ["recruit"]       = (1, 2, "recruit;unit_nid[;faction] — flip side (default player), for talk/recruit."),
                ["heal"]          = (2, 2, "heal;unit_nid;amount — restore HP, clamped to MaxHP."),
                ["damage"]        = (2, 2, "damage;unit_nid;amount — deal flat damage (poison/traps)."),
                ["promote"]       = (2, 2, "promote;unit_nid;new_class — change class key."),
                ["apply_status"]  = (2, 3, "apply_status;unit_nid;name[;duration] — add status (default 3)."),
                ["clear_status"]  = (2, 2, "clear_status;unit_nid;name — remove status."),
                ["rescue"]        = (2, 2, "rescue;carrier;passenger — pick up adjacent passenger."),
                ["drop"]          = (2, 2, "drop;carrier;x,y — drop carried passenger adjacent."),
                ["teach_skill"]   = (2, 2, "teach_skill;unit_nid;skill — grant skill id."),
                ["equip"]         = (2, 2, "equip;unit_nid;weapon_key — reorder named entry first."),
                ["trade"]         = (3, 3, "trade;giver;taker;weapon_key — move entry between adjacent units."),
                ["drop_item"]     = (2, 2, "drop_item;unit_nid;weapon_key — stash entry into the convoy."),
                ["take_item"]     = (2, 2, "take_item;unit_nid;weapon_key — pull entry out of the convoy."),
                ["visit"]         = (1, 1, "visit;unit_nid — visit the village under the unit (once)."),
                ["open"]          = (2, 2, "open;unit_nid;x,y — open adjacent door/chest (consumes key)."),
                ["dance"]         = (2, 2, "dance;dancer;target — refresh adjacent ally's turn (hosts clear acted flags)."),
                ["shove"]         = (2, 2, "shove;shover;target — push adjacent target one tile away."),
                ["swap"]          = (2, 2, "swap;a;b — swap positions of two adjacent units."),
                ["steal"]         = (3, 3, "steal;thief;target;weapon_key — steal adjacent light item when faster."),
                ["warp"]          = (2, 2, "warp;unit_nid;x,y — teleport unit to tile if unoccupied (GBA Warp)."),
                ["gold"]          = (1, 1, "gold;amount — adjust party gold (negative spends, never below zero)."),
                ["buy"]           = (2, 2, "buy;unit_nid;weapon_key — buy priced armory stock into inventory."),
                ["sell"]          = (2, 2, "sell;unit_nid;weapon_key — sell carried entry for half price."),
                ["torch"]         = (2, 2, "torch;unit_nid;amount — grant torch status for 3 turns and boost vision."),
                ["set_terrain"]   = (2, 2, "set_terrain;x,y;terrainKey — change board terrain at tile."),
            };

        /// <summary>Checks that the verb is known and the argument count fits its catalog arity.</summary>
        /// <param name="command">The command to validate without executing anything.</param>
        /// <param name="error">The reason the command was rejected or null when it is valid.</param>
        /// <returns>True when the verb is known and the arity matches.</returns>
        public static bool TryValidate(EventCommand command, out string? error)
        {
            if (!Commands.TryGetValue(command.Verb, out var spec))
            {
                error = $"Unknown event command '{command.Verb}'.";
                return false;
            }
            if (command.Args.Length < spec.min || command.Args.Length > spec.max)
            {
                error = $"'{command.Verb}' takes {spec.min}-{spec.max} args, got {command.Args.Length}.";
                return false;
            }
            error = null;
            return true;
        }

        /// <summary>Enumerates every known verb with its one line authoring help.</summary>
        /// <returns>Each catalog verb paired with its help text.</returns>
        public static IEnumerable<(string verb, string help)> List()
        {
            foreach (var kv in Commands) yield return (kv.Key, kv.Value.help);
        }
    }
}
