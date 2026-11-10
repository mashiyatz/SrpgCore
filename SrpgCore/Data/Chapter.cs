using System;
using System.Collections.Generic;
using SrpgCore.Events;
using SrpgCore.Grid;
using SrpgCore.Regions;

namespace SrpgCore.Data
{
    /// <summary>One unit's chapter entry: the Unit snapshot, Position tile it spawns on, SpawnTurn (1 = starting roster), and optional Group id for spawn_group events.</summary>
    public sealed class Deployment
    {
        /// <summary>Deployed unit reference.</summary>
        public Unit Unit { get; }
        /// <summary>Board tile the unit spawns on.</summary>
        public GridPoint Position { get; }
        /// <summary>Round the unit spawns on, 1 for starting roster.</summary>
        public int SpawnTurn { get; }
        /// <summary>Reinforcement group id for spawn_group-style events, empty when none.</summary>
        public string Group { get; }  // reinforcement group id (for spawn_group-style events)

        /// <summary>Binds a unit to a spawn tile, turn, and optional group.</summary>
        /// <param name="unit">Unit to deploy.</param>
        /// <param name="position">Board tile the unit spawns on.</param>
        /// <param name="spawnTurn">Round the unit spawns on, minimum 1.</param>
        /// <param name="group">Reinforcement group id, empty when none.</param>
        public Deployment(Unit unit, GridPoint position, int spawnTurn = 1, string group = "")
        {
            Unit = unit ?? throw new ArgumentNullException(nameof(unit));
            Position = position;
            SpawnTurn = spawnTurn;
            Group = group ?? "";
        }
    }

    /// <summary>Victory and defeat objective shapes evaluated by the chapter runtime.</summary>
    public enum ObjectiveKind
    {
        /// <summary>Win by destroying every live unit of the tagged side.</summary>
        Rout,       // destroy every live unit of Side
        /// <summary>Win by completing the tagged number of full rounds.</summary>
        Survive,    // complete Turns full rounds
        /// <summary>Lose when any listed nid dies after spawning.</summary>
        Protect,    // defeat if any listed nid dies after spawning
        /// <summary>Win when a live tagged unit stands on the tagged region.</summary>
        Seize,      // a live unit with Tags[0] nid stands on region Tags[1]
        /// <summary>Win when a live tagged unit stands on the tagged escape region.</summary>
        Escape,     // a live unit with Tags[0] nid stands on region Tags[1]
        /// <summary>Never auto-evaluated, win and lose commands decide.</summary>
        EventOnly,  // never auto-evaluated; win/lose commands decide
    }

    /// <summary>One victory or defeat rule: Kind selects shape, then Side for Rout, Turns for Survive, Tags for Protect (protected nids) and Seize or Escape (unit nid + region name), and EventOnly for scripted win/lose.</summary>
    public sealed class ObjectiveSpec
    {
        /// <summary>Objective shape driving evaluation.</summary>
        public ObjectiveKind Kind { get; }
        /// <summary>Target side for rout-style checks.</summary>
        public Affiliation Side { get; set; }
        /// <summary>Full rounds required for survive objectives.</summary>
        public int Turns { get; set; }
        /// <summary>Unit nids, region names, or seize pair in unit then region order.</summary>
        public List<string> Tags { get; } = new List<string>();

        private ObjectiveSpec(ObjectiveKind kind) => Kind = kind;

        /// <summary>Builds a victory rule destroying every live enemy unit.</summary>
        /// <returns>Rout spec targeting the enemy side.</returns>
        public static ObjectiveSpec RoutEnemies() =>
            new ObjectiveSpec(ObjectiveKind.Rout) { Side = Affiliation.Enemy };

        /// <summary>Builds a victory rule completing a number of full rounds.</summary>
        /// <param name="turns">Full rounds required, must be at least 1.</param>
        /// <returns>Survive spec with the tagged turn count.</returns>
        public static ObjectiveSpec Survive(int turns) =>
            new ObjectiveSpec(ObjectiveKind.Survive) { Turns = turns };

        /// <summary>Builds a defeat rule firing when any listed unit nid dies.</summary>
        /// <param name="tags">Protected unit nids matched against deployments and templates.</param>
        /// <returns>Protect spec carrying the tagged nids.</returns>
        public static ObjectiveSpec Protect(params string[] tags)
        {
            var spec = new ObjectiveSpec(ObjectiveKind.Protect);
            spec.Tags.AddRange(tags);
            return spec;
        }

        /// <summary>Builds a victory rule for standing a live unit on a region.</summary>
        /// <param name="unitNid">Nid of the seizing unit.</param>
        /// <param name="regionName">Name of the target region.</param>
        /// <returns>Seize spec carrying unit then region names.</returns>
        public static ObjectiveSpec Seize(string unitNid, string regionName)
        {
            var spec = new ObjectiveSpec(ObjectiveKind.Seize);
            spec.Tags.Add(unitNid);
            spec.Tags.Add(regionName);
            return spec;
        }

        /// <summary>Builds a victory rule for escaping a live unit to a region.</summary>
        /// <param name="unitNid">Nid of the escaping unit.</param>
        /// <param name="regionName">Name of the escape region.</param>
        /// <returns>Escape spec carrying unit then region names.</returns>
        public static ObjectiveSpec Escape(string unitNid, string regionName)
        {
            var spec = new ObjectiveSpec(ObjectiveKind.Escape);
            spec.Tags.Add(unitNid);
            spec.Tags.Add(regionName);
            return spec;
        }

        /// <summary>Builds a defeat rule firing when no live player unit remains.</summary>
        /// <returns>Rout spec targeting the player side.</returns>
        public static ObjectiveSpec AllDead() =>
            new ObjectiveSpec(ObjectiveKind.Rout) { Side = Affiliation.Player };

        /// <summary>Builds a rule never auto-evaluated so event commands decide.</summary>
        /// <returns>EventOnly spec for scripted outcomes.</returns>
        public static ObjectiveSpec EventOnly() => new ObjectiveSpec(ObjectiveKind.EventOnly);
    }

    /// <summary>Map interactable kind: doors/chests open, villages visit.</summary>
    public enum InteractableKind
    {
        /// <summary>Openable door tile, optionally locked behind a key item.</summary>
        Door,
        /// <summary>Openable chest tile yielding its contents once.</summary>
        Chest,
        /// <summary>Visitable village tile granting its reward once.</summary>
        Village,
    }

    /// <summary>
    /// One tile interactable: a locked/unlocked door or chest (opened via the
    /// `open` verb, locked ones consuming a KeyId inventory item) or a village
    /// (visited once via the `visit` verb). Opened/visited state at runtime lives
    /// in ChapterState.Vars ("opened_&lt;name&gt;"/"visited_&lt;name&gt;") so saves
    /// round-trip; Opened mirrors the flag for data-side readers.
    /// </summary>
    public sealed class Interactable
    {
        /// <summary>Unique name matched by open and visit event verbs.</summary>
        public string Name { get; set; } = "";
        /// <summary>Door, chest, or village behavior.</summary>
        public InteractableKind Kind { get; set; }
        /// <summary>Board tile this interactable sits on.</summary>
        public GridPoint Position { get; set; }
        /// <summary>True when opening consumes a key item.</summary>
        public bool Locked { get; set; }
        /// <summary>Inventory item id consumed to unlock, empty when keyless.</summary>
        public string KeyId { get; set; } = "";
        /// <summary>Mirrors the opened or visited runtime flag for data-side readers.</summary>
        public bool Opened { get; set; }
        /// <summary>Terrain key the tile becomes after open or visit, empty when none.</summary>
        public string BecomesTerrainKey { get; set; } = "";

        /// <summary>Places one door, chest, or village on the map.</summary>
        /// <param name="name">Unique name matched by event verbs.</param>
        /// <param name="kind">Door, chest, or village behavior.</param>
        /// <param name="position">Board tile this interactable sits on.</param>
        public Interactable(string name, InteractableKind kind, GridPoint position)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Kind = kind;
            Position = position;
        }
    }

    /// <summary>
    /// Chapter definition (data side): Nid/Title identity, Board, Deployments,
    /// Regions, Events, Victory/Defeat ObjectiveSpecs, Armory weapons by key for
    /// give_item, UnitTemplates for spawn_unit, MaxDeploy roster cap (0 = everyone),
    /// and NextChapter link. Validate() reports authoring errors for editors to
    /// surface; a clean chapter validates to an empty list.
    /// </summary>
    public sealed class Chapter
    {
        /// <summary>Chapter id linking NextChapter references and save slots.</summary>
        public string Nid { get; set; } = "";
        /// <summary>Display title for banners and chapter screens.</summary>
        public string Title { get; set; } = "";
        /// <summary>Playable board, null until the loader assigns one.</summary>
        public Board? Board { get; set; }

        /// <summary>Display objective for banners/menus (not evaluated).</summary>
        public string ObjectiveText { get; set; } = "";

        /// <summary>Unit placements with spawn turns and reinforcement groups.</summary>
        public List<Deployment> Deployments { get; } = new List<Deployment>();
        /// <summary>Named tile rects for events, seize goals, and formation.</summary>
        public List<Region> Regions { get; } = new List<Region>();
        /// <summary>Trigger-driven event scripts with validated verb commands.</summary>
        public List<GameEvent> Events { get; } = new List<GameEvent>();
        /// <summary>Victory rules, any one completing ends the chapter won.</summary>
        public List<ObjectiveSpec> Victory { get; } = new List<ObjectiveSpec>();
        /// <summary>Defeat rules, any one completing ends the chapter lost.</summary>
        public List<ObjectiveSpec> Defeat { get; } = new List<ObjectiveSpec>();

        /// <summary>Weapons by key for unit items and give_item spawns.</summary>
        public Dictionary<string, Weapon> Armory { get; } = new Dictionary<string, Weapon>();
        /// <summary>Unit blueprints by nid for spawn_unit reinforcements.</summary>
        public Dictionary<string, Unit> UnitTemplates { get; } = new Dictionary<string, Unit>();

        /// <summary>Doors, chests, and villages placed on the map (see Interactable).</summary>
        public List<Interactable> Interactables { get; } = new List<Interactable>();

        /// <summary>0 = deploy everyone; otherwise max player units on turn 1.</summary>
        public int MaxDeploy { get; set; }
        /// <summary>Maximum inventory entries per unit, default 5.</summary>
        public int MaxInventory { get; set; } = 5;
        /// <summary>Diamond (isometric) presentation when true; flat grid otherwise.
        /// Set once per chapter via META view, like Unity's 2D/3D project choice.</summary>
        public bool IsIso { get; set; }
        /// <summary>Name of the formation region limiting deployment tiles.</summary>
        public string FormationRegion { get; set; } = "";
        /// <summary>Nid of the following chapter, empty when this chapter ends the chain.</summary>
        public string NextChapter { get; set; } = "";

        /// <summary>Reports authoring errors for editors to surface, empty when clean.</summary>
        /// <returns>One message per off-board tile, duplicate name, bad verb, or dangling tag.</returns>
        public List<string> Validate()
        {
            var errors = new List<string>();
            if (Nid.Length == 0) errors.Add("Chapter nid is empty.");
            if (Title.Length == 0) errors.Add("Chapter title is empty.");
            if (Board == null)
            {
                errors.Add("Chapter has no board.");
                return errors;
            }

            var nids = new HashSet<string>();
            int turnOnePlayers = 0;
            foreach (Deployment d in Deployments)
            {
                if (!Board.InBounds(d.Position))
                    errors.Add($"Deployment '{d.Unit.Nid}' at {d.Position} is off-board.");
                if (d.SpawnTurn < 1)
                    errors.Add($"Deployment '{d.Unit.Nid}' has spawn turn {d.SpawnTurn} (min 1).");
                if (!nids.Add(d.Unit.Nid))
                    errors.Add($"Duplicate deployment nid '{d.Unit.Nid}'.");
                if (d.SpawnTurn == 1 && d.Unit.Side == Affiliation.Player) turnOnePlayers++;
            }
            if (MaxDeploy < 0) errors.Add($"MaxDeploy {MaxDeploy} is negative.");
            if (MaxDeploy > 0 && turnOnePlayers > MaxDeploy)
                errors.Add($"{turnOnePlayers} turn-1 player units exceed MaxDeploy {MaxDeploy}.");
            if (MaxInventory < 1) errors.Add($"MaxInventory {MaxInventory} must be at least 1.");
            foreach (Deployment d in Deployments)
                if (d.Unit.Inventory.Count > MaxInventory)
                    errors.Add($"Deployment '{d.Unit.Nid}' has {d.Unit.Inventory.Count} items exceeding MaxInventory {MaxInventory}.");
            foreach (Unit u in UnitTemplates.Values)
                if (u.Inventory.Count > MaxInventory)
                    errors.Add($"Template '{u.Nid}' has {u.Inventory.Count} items exceeding MaxInventory {MaxInventory}.");
            if (!string.IsNullOrWhiteSpace(FormationRegion) && !Regions.Exists(r => r.Name == FormationRegion))
                errors.Add($"FormationRegion '{FormationRegion}' matches no region.");
            else if (!string.IsNullOrWhiteSpace(FormationRegion) && Regions.Exists(r => r.Name == FormationRegion && r.Kind != RegionKind.Formation))
                errors.Add($"FormationRegion '{FormationRegion}' is not a formation region.");
            var posSeen = new HashSet<string>();
            foreach (Deployment d in Deployments)
                if (!posSeen.Add(d.Position.X + "," + d.Position.Y))
                    errors.Add($"Duplicate deployment tile {d.Position} (nid '{d.Unit.Nid}').");

            foreach (Region r in Regions)
                foreach (GridPoint p in r.Tiles)
                    if (!Board.InBounds(p))
                    {
                        errors.Add($"Region '{r.Name}' has off-board tile {p}.");
                        break;
                    }
            var regionNames = new HashSet<string>(StringComparer.Ordinal);
            foreach (Region r in Regions)
                if (!regionNames.Add(r.Name))
                    errors.Add($"Duplicate region name '{r.Name}'.");

            var interactableNames = new HashSet<string>();
            foreach (Interactable i in Interactables)
            {
                if (i.Name.Length == 0) errors.Add("Interactable with empty name.");
                else if (!interactableNames.Add(i.Name)) errors.Add($"Duplicate interactable name '{i.Name}'.");
                if (!Board.InBounds(i.Position))
                    errors.Add($"Interactable '{i.Name}' at {i.Position} is off-board.");
                if (!string.IsNullOrWhiteSpace(i.BecomesTerrainKey) && !Board.Terrains.ContainsKey(i.BecomesTerrainKey))
                    errors.Add($"Interactable '{i.Name}' has unknown terrain '{i.BecomesTerrainKey}'.");
            }

            var eventNames = new HashSet<string>();
            foreach (GameEvent e in Events)
            {
                if (e.Name.Length == 0) errors.Add("Event with empty name.");
                else if (!eventNames.Add(e.Name)) errors.Add($"Duplicate event name '{e.Name}'.");
                foreach (EventCommand c in e.Commands)
                    if (!CommandCatalog.TryValidate(c, out string? err))
                        errors.Add($"Event '{e.Name}': {err}");
                try
                {
                    SimpleConditionEvaluator.Evaluate(e.Condition, new Dictionary<string, object?>());
                }
                catch (FormatException ex)
                {
                    errors.Add($"Event '{e.Name}': bad condition: {ex.Message}");
                }
            }

            foreach (ObjectiveSpec spec in Victory) ValidateSpec(spec, "victory", errors);
            foreach (ObjectiveSpec spec in Defeat) ValidateSpec(spec, "defeat", errors);
            return errors;
        }

        private void ValidateSpec(ObjectiveSpec spec, string which, List<string> errors)
        {
            if (spec.Kind == ObjectiveKind.Survive && spec.Turns < 1)
                errors.Add($"{which}: survive needs turns >= 1.");
            if (spec.Kind == ObjectiveKind.Protect)
                foreach (string tag in spec.Tags)
                    if (!UnitTemplates.ContainsKey(tag) && !Deployments.Exists(d => d.Unit.Nid == tag))
                        errors.Add($"{which}: protect tag '{tag}' matches no deployment or template.");
            if (spec.Kind == ObjectiveKind.Seize)
            {
                if (spec.Tags.Count != 2)
                {
                    errors.Add($"{which}: seize needs 'unit_nid region_name'.");
                    return;
                }
                if (!UnitTemplates.ContainsKey(spec.Tags[0]) && !Deployments.Exists(d => d.Unit.Nid == spec.Tags[0]))
                    errors.Add($"{which}: seize unit '{spec.Tags[0]}' matches no deployment or template.");
                if (!Regions.Exists(r => r.Name == spec.Tags[1]))
                    errors.Add($"{which}: seize region '{spec.Tags[1]}' matches no region.");
            }
            if (spec.Kind == ObjectiveKind.Escape)
            {
                if (spec.Tags.Count != 2)
                {
                    errors.Add($"{which}: escape needs 'unit_nid region_name'.");
                    return;
                }
                if (!UnitTemplates.ContainsKey(spec.Tags[0]) && !Deployments.Exists(d => d.Unit.Nid == spec.Tags[0]))
                    errors.Add($"{which}: escape unit '{spec.Tags[0]}' matches no deployment or template.");
                if (!Regions.Exists(r => r.Name == spec.Tags[1]))
                    errors.Add($"{which}: escape region '{spec.Tags[1]}' matches no region.");
            }
        }
    }
}
