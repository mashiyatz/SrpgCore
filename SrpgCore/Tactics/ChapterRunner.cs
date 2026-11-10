using System;
using System.Collections.Generic;
using SrpgCore.Data;
using SrpgCore.Events;
using SrpgCore.Grid;

namespace SrpgCore.Tactics
{
    /// <summary>
    /// Headless event executor over a ChapterState: every catalog verb does something
    /// observable (move/teleport, spawn, flags, result, dialogue log). Lets chapters run
    /// without a renderer — Verify chapters end-to-end here; hosts replace movement and
    /// dialogue with animated versions. Unknown-at-runtime failures (missing unit,
    /// template, armory entry) throw: authoring errors must be loud.
    /// </summary>
    public sealed class ChapterRunner : IEventExecutor
    {
        /// <summary>Live chapter runtime this runner mutates.</summary>
        public ChapterState State { get; }

        /// <summary>Every executed command, in order — the headless trace.</summary>
        public List<string> StageLog { get; } = new List<string>();

        /// <summary>Spoken lines (speaker, text) for dialogue tests.</summary>
        public List<(string speaker, string text)> Dialogue { get; } = new List<(string, string)>();

        /// <summary>Ambient lines (speaker, text): comments during play, no advance.</summary>
        public List<(string speaker, string text)> Aside { get; } = new List<(string, string)>();

        /// <summary>Rescued passengers held off-map (nid -> unit) aliasing State.Carried so saves round-trip.</summary>
        public Dictionary<string, Unit> Carried => State.Carried;

        /// <summary>Shared convoy storage — aliases State.Convoy so saves round-trip.</summary>
        public List<InventoryEntry> Convoy => State.Convoy;

        /// <summary>
        /// Visited village names, derived from State.Vars ("visited_&lt;name&gt;") —
        /// no extra state is held here; the Vars dictionary is the source of truth so
        /// saves round-trip it.
        /// </summary>
        public HashSet<string> Visited
        {
            get
            {
                var names = new HashSet<string>();
                foreach (var kv in State.Vars)
                    if (kv.Key.StartsWith("visited_", StringComparison.Ordinal) && kv.Value is true)
                        names.Add(kv.Key.Substring("visited_".Length));
                return names;
            }
        }

        /// <summary>Creates a headless runner over an existing chapter state.</summary>
        /// <param name="state">Chapter runtime to execute commands against.</param>
        public ChapterRunner(ChapterState state)
        {
            State = state ?? throw new ArgumentNullException(nameof(state));
        }

        /// <summary>Executes one validated event command against the chapter state and logs it.</summary>
        /// <param name="command">Catalog-validated command to apply.</param>
        public void Execute(EventCommand command)
        {
            if (command == null) throw new ArgumentNullException(nameof(command));
            StageLog.Add(command.ToString());
            string verb = command.Verb.ToLowerInvariant();
            string[] a = command.Args;

            switch (verb)
            {
                case "speak":
                    Dialogue.Add((a[0], a[1]));
                    break;
                case "say":
                    Aside.Add((a[0], a[1]));
                    break;
                case "add_portrait":
                case "remove_portrait":
                case "wait":
                    break; // presentation-only headlessly
                case "move_unit":
                    {
                        PlacedUnit mover = FindPlaced(a[0]);
                        GridPoint dest = ParsePoint(a[1]);
                        if (!State.Field.Board.InBounds(dest))
                            throw new InvalidOperationException($"Move destination {dest} is out of bounds.");
                        if (!State.Field.Board.GetTerrain(dest).IsPassable(mover.Unit.MovementGroup))
                            throw new InvalidOperationException($"Move destination {dest} is impassable for '{mover.Unit.MovementGroup}'.");
                        mover.Position = dest;
                        break;
                    }
                case "spawn_unit":
                    Spawn(a[0], ParseAffiliation(a[1]), ParsePoint(a[2]));
                    break;
                case "remove_unit":
                    State.Field.Remove(FindPlaced(a[0]));
                    break;
                case "give_item":
                    {
                        PlacedUnit p = FindPlaced(a[0]);
                        EnsureInventorySpace(p.Unit);
                        p.Unit.Inventory.Add(new InventoryEntry(Armory(a[1])));
                        break;
                    }
                case "set_flag":
                    State.Vars[a[0]] = ParseBool(a[1]);
                    break;
                case "recruit":
                    {
                        PlacedUnit p = FindPlaced(a[0]);
                        p.Unit.Side = a.Length > 1 ? ParseAffiliation(a[1]) : Affiliation.Player;
                        break;
                    }
                case "heal":
                    {
                        PlacedUnit p = FindPlaced(a[0]);
                        if (!int.TryParse(a[1], out int amount) || amount < 0)
                            throw new FormatException($"Bad heal amount '{a[1]}'.");
                        p.Unit.CurrentHP = Math.Min(p.Unit.Stats.MaxHP, p.Unit.CurrentHP + amount);
                        break;
                    }
                case "damage":
                    {
                        PlacedUnit p = FindPlaced(a[0]);
                        if (!int.TryParse(a[1], out int amount) || amount < 0)
                            throw new FormatException($"Bad damage amount '{a[1]}'.");
                        p.Unit.CurrentHP = Math.Max(0, p.Unit.CurrentHP - amount);
                        break;
                    }
                case "promote":
                    FindPlaced(a[0]).Unit.ClassKey = a[1];
                    break;
                case "apply_status":
                    {
                        PlacedUnit p = FindPlaced(a[0]);
                        int dur = 3;
                        if (a.Length > 2)
                        {
                            if (!int.TryParse(a[2], out dur) || dur < 1)
                                throw new FormatException($"Bad status duration '{a[2]}'.");
                        }
                        Statuses.Apply(p.Unit, a[1], dur);
                        break;
                    }
                case "clear_status":
                    Statuses.Clear(FindPlaced(a[0]).Unit, a[1]);
                    break;
                case "rescue":
                    {
                        PlacedUnit removed = Rescue.Grab(State.Field, FindPlaced(a[0]), FindPlaced(a[1]));
                        Carried[removed.Unit.Nid] = removed.Unit;
                        break;
                    }
                case "drop":
                    {
                        PlacedUnit carrier = FindPlaced(a[0]);
                        string carried = carrier.Unit.CarryingNid;
                        if (carried.Length == 0) throw new InvalidOperationException($"'{a[0]}' carries nobody.");
                        Unit passenger = FindCarried(carried);
                        Rescue.Drop(State.Field, carrier, passenger, ParsePoint(a[1]));
                        Carried.Remove(carried);
                        break;
                    }
                case "teach_skill":
                    {
                        PlacedUnit p = FindPlaced(a[0]);
                        if (!p.Unit.Skills.Contains(a[1])) p.Unit.Skills.Add(a[1]);
                        break;
                    }
                case "equip":
                    {
                        PlacedUnit p = FindPlaced(a[0]);
                        int idx = FindEntry(p.Unit, a[1]);
                        InventoryEntry entry = p.Unit.Inventory[idx];
                        if (!WeaponRanks.CanEquip(WeaponRanks.PointsOf(p.Unit, entry.Weapon.Type), entry.Weapon.Rank))
                            throw new InvalidOperationException($"'{p.Unit.Nid}' cannot equip '{a[1]}' (rank).");
                        p.Unit.Inventory.RemoveAt(idx);
                        p.Unit.Inventory.Insert(0, entry);
                        break;
                    }
                case "trade":
                    {
                        PlacedUnit giver = FindPlaced(a[0]);
                        PlacedUnit taker = FindPlaced(a[1]);
                        if (giver.Position.ManhattanDistance(taker.Position) != 1)
                            throw new InvalidOperationException($"Trade needs adjacency ('{a[0]}' and '{a[1]}' are apart).");
                        EnsureInventorySpace(taker.Unit);
                        int idx = FindEntry(giver.Unit, a[2]);
                        InventoryEntry entry = giver.Unit.Inventory[idx];
                        giver.Unit.Inventory.RemoveAt(idx);
                        taker.Unit.Inventory.Add(entry);
                        break;
                    }
                case "drop_item":
                    {
                        PlacedUnit p = FindPlaced(a[0]);
                        int idx = FindEntry(p.Unit, a[1]);
                        InventoryEntry entry = p.Unit.Inventory[idx];
                        p.Unit.Inventory.RemoveAt(idx);
                        Convoy.Add(entry);
                        break;
                    }
                case "take_item":
                    {
                        PlacedUnit p = FindPlaced(a[0]);
                        EnsureInventorySpace(p.Unit);
                        int idx = -1;
                        for (int i = 0; i < Convoy.Count; i++)
                            if (Convoy[i].Weapon.Key == a[1]) { idx = i; break; }
                        if (idx < 0) throw new InvalidOperationException($"Convoy has no '{a[1]}'.");
                        InventoryEntry entry = Convoy[idx];
                        Convoy.RemoveAt(idx);
                        p.Unit.Inventory.Add(entry);
                        break;
                    }
                case "gold":
                    {
                        if (!int.TryParse(a[0], out int amount))
                            throw new FormatException($"Bad gold amount '{a[0]}' (want integer).");
                        if (State.PartyGold + amount < 0)
                            throw new InvalidOperationException($"Party gold {State.PartyGold} cannot cover {amount}.");
                        State.PartyGold += amount;
                        break;
                    }
                case "buy":
                    {
                        PlacedUnit p = FindPlaced(a[0]);
                        Weapon w = Armory(a[1]);
                        if (w.Price <= 0)
                            throw new InvalidOperationException($"'{a[1]}' is not for sale (no price).");
                        EnsureInventorySpace(p.Unit);
                        if (State.PartyGold < w.Price)
                            throw new InvalidOperationException($"Party gold {State.PartyGold} cannot afford '{a[1]}' ({w.Price}).");
                        State.PartyGold -= w.Price;
                        p.Unit.Inventory.Add(new InventoryEntry(w));
                        break;
                    }
                case "sell":
                    {
                        PlacedUnit p = FindPlaced(a[0]);
                        int idx = FindEntry(p.Unit, a[1]);
                        InventoryEntry entry = p.Unit.Inventory[idx];
                        p.Unit.Inventory.RemoveAt(idx);
                        State.PartyGold += entry.Weapon.Price / 2;
                        break;
                    }
                case "visit":
                    {
                        PlacedUnit p = FindPlaced(a[0]);
                        Interactable village = FindInteractableAt(p.Position, InteractableKind.Village, null);
                        string flag = "visited_" + village.Name;
                        if (State.Vars.TryGetValue(flag, out object? seen) && seen is true)
                            throw new InvalidOperationException($"Village '{village.Name}' already visited.");
                        State.Vars[flag] = true;
                        village.Opened = true;
                        break;
                    }
                case "open":
                    {
                        PlacedUnit opener = FindPlaced(a[0]);
                        GridPoint tile = ParsePoint(a[1]);
                        Interactable target = FindInteractableAt(tile, null, "door/chest");
                        if (opener.Position.ManhattanDistance(tile) != 1)
                            throw new InvalidOperationException($"'{a[0]}' must stand adjacent to {target.Name}.");
                        string flag = "opened_" + target.Name;
                        if (State.Vars.TryGetValue(flag, out object? done) && done is true)
                            throw new InvalidOperationException($"'{target.Name}' is already open.");
                        if (target.Locked)
                        {
                            int keyIdx = FindEntry(opener.Unit, target.KeyId);
                            opener.Unit.Inventory.RemoveAt(keyIdx);
                        }
                        target.Opened = true;
                        State.Vars[flag] = true;
                        if (target.BecomesTerrainKey.Length > 0)
                            State.Field.Board.SetTerrain(target.Position, target.BecomesTerrainKey);
                        break;
                    }
                case "win":
                    State.Phases.SetResult(true);
                    break;
                case "dance":
                    {
                        PlacedUnit dancer = FindPlaced(a[0]);
                        PlacedUnit target = FindPlaced(a[1]);
                        Dance.CheckDance(dancer, target); // throws when illegal; headless keeps no acted flags, hosts refresh the target
                        break;
                    }
                case "shove":
                    Shove.ExecuteShove(State.Field, FindPlaced(a[0]), FindPlaced(a[1]));
                    break;
                case "swap":
                    Shove.ExecuteSwap(FindPlaced(a[0]), FindPlaced(a[1]));
                    break;
                case "steal":
                    {
                        PlacedUnit thief = FindPlaced(a[0]);
                        EnsureInventorySpace(thief.Unit);
                        Steal.Execute(State.Field, thief, FindPlaced(a[1]), a[2]);
                        break;
                    }
                case "warp":
                    {
                        PlacedUnit pu = FindPlaced(a[0]);
                        GridPoint dest = ParsePoint(a[1]);
                        if (!State.Field.Board.InBounds(dest))
                            throw new InvalidOperationException($"Warp destination {dest} is out of bounds.");
                        if (!State.Field.Board.GetTerrain(dest).IsPassable(pu.Unit.MovementGroup))
                            throw new InvalidOperationException($"Warp destination {dest} is impassable for '{pu.Unit.MovementGroup}'.");
                        var blocking = State.Field.BlockingTiles(pu);
                        var allies = State.Field.AllyTiles(pu);
                        if (blocking.Contains(dest) || allies.Contains(dest))
                            throw new InvalidOperationException($"Warp destination {dest} is occupied.");
                        pu.Position = dest;
                        break;
                    }
                case "torch":
                    {
                        PlacedUnit tp = FindPlaced(a[0]);
                        if (!int.TryParse(a[1], out int amount) || amount < 0)
                            throw new FormatException($"Bad torch amount '{a[1]}'.");
                        Statuses.Apply(tp.Unit, "torch", 3);
                        int bonus = amount != 0 ? amount : new SrpgCore.Combat.CombatRules().TorchVisionBonus;
                        tp.Unit.Vision += bonus;
                        State.Vars["torch_" + tp.Unit.Nid] = tp.Unit.Vision;
                        break;
                    }
                case "set_terrain":
                    {
                        GridPoint dest = ParsePoint(a[0]);
                        string terrainKey = a[1];
                        if (!State.Field.Board.InBounds(dest))
                            throw new InvalidOperationException($"Terrain destination {dest} is out of bounds.");
                        if (!State.Field.Board.Terrains.ContainsKey(terrainKey))
                            throw new InvalidOperationException($"Unknown terrain '{terrainKey}'.");
                        State.Field.Board.SetTerrain(dest, terrainKey);
                        break;
                    }
                case "lose":
                    State.Phases.SetResult(false);
                    break;
                default:
                    throw new InvalidOperationException($"No headless handling for '{command.Verb}'.");
            }
        }

        private PlacedUnit FindPlaced(string nid)
        {
            foreach (PlacedUnit p in State.Field.Placed)
                if (p.Unit.Nid == nid) return p;
            throw new InvalidOperationException($"No placed unit '{nid}'.");
        }

        private Unit FindCarried(string nid)
        {
            if (Carried.TryGetValue(nid, out Unit? u)) return u;
            throw new InvalidOperationException($"No carried unit '{nid}'.");
        }

        private static int FindEntry(Unit unit, string weaponKey)
        {
            for (int i = 0; i < unit.Inventory.Count; i++)
                if (unit.Inventory[i].Weapon.Key == weaponKey) return i;
            throw new InvalidOperationException($"'{unit.Nid}' carries no '{weaponKey}'.");
        }

        private Interactable FindInteractableAt(GridPoint tile, InteractableKind? kind, string? what)
        {
            foreach (Interactable i in State.Chapter.Interactables)
            {
                if (i.Position != tile) continue;
                if (kind != null && i.Kind != kind) continue;
                if (kind == null && i.Kind != InteractableKind.Door && i.Kind != InteractableKind.Chest) continue;
                return i;
            }
            throw new InvalidOperationException($"No {what ?? (kind != null ? kind.ToString().ToLowerInvariant() : "interactable")} at {tile}.");
        }

        private Weapon Armory(string key)
        {
            if (!State.Chapter.Armory.TryGetValue(key, out Weapon? w))
                throw new InvalidOperationException($"Armory has no '{key}'.");
            return w;
        }

        private PlacedUnit Spawn(string nid, Affiliation side, GridPoint position)
        {
            if (!State.Chapter.UnitTemplates.TryGetValue(nid, out Unit? template))
                throw new InvalidOperationException($"No unit template '{nid}'.");
            string unique = nid;
            int suffix = 2;
            while (NidTaken(unique))
            {
                unique = nid + "#" + suffix;
                suffix++;
            }
            Unit clone = template.Clone(unique);
            clone.Side = side;
            return State.Field.Deploy(clone, position);
        }

        private bool NidTaken(string nid)
        {
            foreach (PlacedUnit p in State.Field.Placed)
                if (p.Unit.Nid == nid) return true;
            return false;
        }

        private static GridPoint ParsePoint(string text)
        {
            string[] parts = text.Split(',');
            if (parts.Length != 2 || !int.TryParse(parts[0], out int x) || !int.TryParse(parts[1], out int y))
                throw new FormatException($"Bad tile '{text}' (want 'x,y').");
            return new GridPoint(x, y);
        }

        private static Affiliation ParseAffiliation(string text)
        {
            string t = text.Trim().ToLowerInvariant();
            switch (t)
            {
                case "player": return Affiliation.Player;
                case "enemy": return Affiliation.Enemy;
                case "ally": return Affiliation.Ally;
                case "other": return Affiliation.Other;
                default: throw new FormatException($"Bad faction '{text}'.");
            }
        }

        private static bool ParseBool(string text)
        {
            string t = text.Trim().ToLowerInvariant();
            if (t == "true" || t == "1") return true;
            if (t == "false" || t == "0") return false;
            throw new FormatException($"Bad flag value '{text}' (want true/false).");
        }

        private void EnsureInventorySpace(Unit unit)
        {
            if (unit.Inventory.Count >= State.Chapter.MaxInventory)
                throw new InvalidOperationException($"'{unit.Nid}' inventory full ({State.Chapter.MaxInventory}).");
        }
    }
}
