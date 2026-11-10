using System;
using System.Collections.Generic;
using SrpgCore.Events;
using SrpgCore.Grid;
using SrpgCore.Regions;

namespace SrpgCore.Data
{
    /// <summary>One loader complaint with file + 1-based line (0 = chapter-level).</summary>
    public sealed class ChapterParseError
    {
        /// <summary>Source file name the complaint came from.</summary>
        public string File { get; }
        /// <summary>One-based line number, 0 for chapter-level problems.</summary>
        public int Line { get; }
        /// <summary>Human-readable complaint for editors to surface.</summary>
        public string Message { get; }

        /// <summary>Records one loader complaint with file and line.</summary>
        /// <param name="file">Source file name.</param>
        /// <param name="line">One-based line number, 0 for chapter-level problems.</param>
        /// <param name="message">Human-readable complaint.</param>
        public ChapterParseError(string file, int line, string message)
        {
            File = file;
            Line = line;
            Message = message;
        }

        /// <summary>Renders file, line, and message as one log line.</summary>
        /// <returns>Single-line file and line complaint string.</returns>
        public override string ToString() => $"{File}:{Line}: {Message}";
    }

    /// <summary>Outcome of one markdown load: a chapter on success plus every complaint.</summary>
    public sealed class ChapterParseResult
    {
        /// <summary>Parsed chapter, null when any error was collected.</summary>
        public Chapter? Chapter { get; set; }
        /// <summary>Every file and line complaint, empty on a clean load.</summary>
        public List<ChapterParseError> Errors { get; } = new List<ChapterParseError>();
        /// <summary>True when a chapter parsed with zero errors.</summary>
        public bool Ok => Errors.Count == 0 && Chapter != null;
    }

    /// <summary>
    /// Markdown chapter format: one file per
    /// chapter with META / LEGEND / MAP / ARMORY / TEMPLATES / UNITS / TERRAINS / REGIONS /
    /// INTERACTABLES / EVENTS sections mapping 1:1 onto <see cref="Chapter"/>. Collects every error with
    /// file + line instead of throwing on the first, so editors can surface them all.
    /// Full spec: SRPGDocs chapter-format page. Current format: 1.
    /// </summary>
    public static class MarkdownChapterLoader
    {
        /// <summary>Markdown format version this loader reads, required as format 1 in META.</summary>
        public const int CurrentFormat = 1;

        private enum Section { None, Meta, Legend, Map, Armory, Templates, Units, Terrains, Regions, Events, Interactables }

        private sealed class RawUnit
        {
            public string[] Cells = Array.Empty<string>();
            public int Line;
            public bool HasPosition;
        }

        private sealed class RawEvent
        {
            public string Name = "";
            public string TriggerWord = "";
            public string TurnSpec = "";   // "3:player" for turn-form, else ""
            public string Condition = "";
            public int Priority;
            public int Line;
            public List<(string text, int line)> Commands = new List<(string, int)>();
        }

        private static readonly Dictionary<string, EventTrigger> TriggerAliases =
            new Dictionary<string, EventTrigger>(StringComparer.OrdinalIgnoreCase)
            {
                ["start"] = EventTrigger.LevelStart,
                ["end"] = EventTrigger.LevelEnd,
                ["playerphase"] = EventTrigger.PlayerPhaseStart,
                ["player_phase"] = EventTrigger.PlayerPhaseStart,
                ["turn_change"] = EventTrigger.PlayerPhaseStart,
                ["enemyphase"] = EventTrigger.EnemyPhaseStart,
                ["enemy_phase"] = EventTrigger.EnemyPhaseStart,
                ["death"] = EventTrigger.UnitDeath,
                ["unit_death"] = EventTrigger.UnitDeath,
                ["wait"] = EventTrigger.UnitWait,
                ["unit_wait"] = EventTrigger.UnitWait,
                ["select"] = EventTrigger.UnitSelect,
                ["unit_select"] = EventTrigger.UnitSelect,
                ["levelup"] = EventTrigger.UnitLevelUp,
                ["unit_level_up"] = EventTrigger.UnitLevelUp,
                ["combatstart"] = EventTrigger.CombatStart,
                ["combatend"] = EventTrigger.CombatEnd,
                ["talk"] = EventTrigger.Talk,
                ["support"] = EventTrigger.Support,
                ["region"] = EventTrigger.RegionEntered,
                ["region_entered"] = EventTrigger.RegionEntered,
            };

        /// <summary>Parses one markdown chapter file using the standard terrain set.</summary>
        /// <param name="text">Full markdown file contents.</param>
        /// <param name="fileName">File name stamped onto every error.</param>
        /// <returns>Result carrying the chapter on success or every complaint on failure.</returns>
        public static ChapterParseResult Load(string text, string fileName) =>
            Load(text, fileName, StandardTerrains.Create());

        /// <summary>Parses one markdown chapter file against a custom terrain base set.</summary>
        /// <param name="text">Full markdown file contents.</param>
        /// <param name="fileName">File name stamped onto every error.</param>
        /// <param name="baseTerrains">Base terrain entries extended by TERRAINS rows.</param>
        /// <returns>Result carrying the chapter on success or every complaint on failure.</returns>
        public static ChapterParseResult Load(
            string text, string fileName,
            IReadOnlyDictionary<string, TerrainType> baseTerrains)
        {
            var result = new ChapterParseResult();
            void Fail(int line, string message) =>
                result.Errors.Add(new ChapterParseError(fileName, line, message));

            var lines = (text ?? "").Split('\n');
            // Strip <!-- --> comments (possibly multi-line) but keep line numbers stable.
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                int open = line.IndexOf("<!--", StringComparison.Ordinal);
                while (open >= 0)
                {
                    int close = line.IndexOf("-->", open, StringComparison.Ordinal);
                    if (close >= 0) { line = line.Substring(0, open) + line.Substring(close + 3); open = line.IndexOf("<!--", StringComparison.Ordinal); }
                    else
                    {
                        line = line.Substring(0, open);
                        for (int j = i + 1; j < lines.Length; j++)
                        {
                            int end = lines[j].IndexOf("-->", StringComparison.Ordinal);
                            if (end >= 0) { lines[j] = lines[j].Substring(end + 3); break; }
                            lines[j] = "";
                        }
                        break;
                    }
                }
                lines[i] = line;
            }

            string title = "";
            string nid = "";
            var meta = new List<(string key, string value, int line)>();
            var legend = new Dictionary<char, string>();
            var legendLines = new Dictionary<char, int>();
            var mapRows = new List<(string row, int line)>();
            var armory = new List<(string[] cells, int line)>();
            var templates = new List<RawUnit>();
            var units = new List<RawUnit>();
            var terrains = new List<(string[] cells, int line)>();
            var regions = new List<(string[] cells, int line)>();
            var events = new List<RawEvent>();
            var interactables = new List<(string[] cells, int line)>();

            Section section = Section.None;
            RawEvent? currentEvent = null;

            for (int li = 0; li < lines.Length; li++)
            {
                int lineNo = li + 1;
                string raw = lines[li].TrimEnd('\r');
                string line = raw.Trim();
                if (line.Length == 0) continue;

                if (line.StartsWith("###", StringComparison.Ordinal))
                {
                    if (section != Section.Events) { Fail(lineNo, "### header outside EVENTS."); continue; }
                    currentEvent = ParseEventHeader(line.Substring(3).Trim(), lineNo, Fail);
                    if (currentEvent != null) events.Add(currentEvent);
                    continue;
                }
                if (line.StartsWith("##", StringComparison.Ordinal))
                {
                    string name = line.Substring(2).Trim().ToUpperInvariant();
                    currentEvent = null;
                    switch (name)
                    {
                        case "META": section = Section.Meta; break;
                        case "LEGEND": section = Section.Legend; break;
                        case "MAP": section = Section.Map; break;
                        case "ARMORY": section = Section.Armory; break;
                        case "TEMPLATES": section = Section.Templates; break;
                        case "UNITS": section = Section.Units; break;
                        case "TERRAINS": section = Section.Terrains; break;
                        case "REGIONS": section = Section.Regions; break;
                        case "EVENTS": section = Section.Events; break;
                        case "INTERACTABLES": section = Section.Interactables; break;
                        default: Fail(lineNo, $"Unknown section '{name}'."); section = Section.None; break;
                    }
                    continue;
                }
                if (line.StartsWith("#", StringComparison.Ordinal))
                {
                    if (title == "") { ParseTitle(line.Substring(1).Trim(), out nid, out title); }
                    continue; // single-# lines are comments everywhere (title is the first)
                }

                if (currentEvent != null)
                {
                    string cmd = line.StartsWith("- ", StringComparison.Ordinal)
                        ? DesugarDialogue(line.Substring(2), lineNo, Fail) : line;
                    if (cmd != null) currentEvent.Commands.Add((cmd, lineNo));
                    continue;
                }

                string[] cells = line.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
                switch (section)
                {
                    case Section.None:
                        Fail(lineNo, "Content outside any section."); break;
                    case Section.Meta:
                        {
                            int colon = line.IndexOf(':');
                            if (colon < 0) { Fail(lineNo, "META lines are 'key: value'."); break; }
                            meta.Add((line.Substring(0, colon).Trim().ToLowerInvariant(), line.Substring(colon + 1).Trim(), lineNo));
                            break;
                        }
                    case Section.Legend:
                        {
                            int colon = line.IndexOf(':');
                            string key = colon < 0 ? line.Trim() : line.Substring(0, colon).Trim();
                            string val = colon < 0 ? "" : line.Substring(colon + 1).Trim();
                            if (key.Length != 1 || val.Length == 0) { Fail(lineNo, "LEGEND lines are '<glyph>: <terrain-key>'."); break; }
                            char glyph = key[0];
                            if (legend.ContainsKey(glyph)) { Fail(lineNo, $"Duplicate legend glyph '{glyph}'."); break; }
                            legend[glyph] = val;
                            legendLines[glyph] = lineNo;
                            break;
                        }
                    case Section.Map: mapRows.Add((line, lineNo)); break;
                    case Section.Armory: armory.Add((cells, lineNo)); break;
                    case Section.Templates: templates.Add(new RawUnit { Cells = cells, Line = lineNo }); break;
                    case Section.Units: units.Add(new RawUnit { Cells = cells, Line = lineNo, HasPosition = true }); break;
                    case Section.Terrains: terrains.Add((cells, lineNo)); break;
                    case Section.Regions: regions.Add((cells, lineNo)); break;
                    case Section.Interactables: interactables.Add((cells, lineNo)); break;
                    case Section.Events: Fail(lineNo, "Event commands need a '### name on trigger' header first."); break;
                }
            }

            if (title == "") Fail(0, "Missing '# <nid>: <title>' header.");
            var chapter = new Chapter { Nid = nid, Title = title };
            if (result.Errors.Count > 0) return result;

            BuildMeta(chapter, meta, Fail);
            if (result.Errors.Count > 0) return result;

            // Terrains: base set + custom (custom keys must be new).
            var terrainSet = new Dictionary<string, TerrainType>();
            foreach (var kv in baseTerrains) terrainSet[kv.Key] = kv.Value;
            foreach (var (cells, lineNo) in terrains)
                ParseTerrainRow(chapter, terrainSet, cells, lineNo, Fail);

            // Board first (regions need bounds); on failure keep a 1x1 fallback so
            // the rest still parses and every error surfaces in one pass.
            Board? board = BuildBoard(mapRows, legend, legendLines, terrainSet, Fail);
            Board bounds = board ?? new Board(1, 1, "plain", terrainSet);

            // Armory first (units reference it, order-independent).
            foreach (var (cells, lineNo) in armory)
                ParseArmoryRow(chapter, cells, lineNo, Fail);
            var templateUnits = new Dictionary<string, Unit>();
            foreach (RawUnit raw in templates)
                ParseUnitRow(chapter, templateUnits, chapter.UnitTemplates, raw, null, Fail);
            foreach (RawUnit raw in units)
                ParseUnitRow(chapter, templateUnits, null, raw, chapter.Deployments, Fail);
            foreach (var (cells, lineNo) in regions)
                ParseRegionRow(chapter, bounds, cells, lineNo, Fail);
            foreach (var (cells, lineNo) in interactables)
                ParseInteractableRow(chapter, bounds, cells, lineNo, Fail);
            foreach (RawEvent raw in events)
                BuildEvent(chapter, raw, Fail);

            if (result.Errors.Count > 0) { return result; }
            chapter.Board = board;
            foreach (string problem in chapter.Validate())
                result.Errors.Add(new ChapterParseError(fileName, 0, problem));
            if (result.Errors.Count > 0) return result;
            result.Chapter = chapter;
            return result;
        }

        // ---------- pieces ----------

        private static void ParseTitle(string text, out string nid, out string title)
        {
            nid = ""; title = "";
            int colon = text.IndexOf(':');
            if (colon < 0) { title = text.Trim(); nid = title; return; }
            nid = text.Substring(0, colon).Trim();
            title = text.Substring(colon + 1).Trim();
            if (title.Length == 0) title = nid;
            if (nid.Length == 0) nid = title;
        }

        private static void BuildMeta(Chapter chapter, List<(string key, string value, int line)> meta, Action<int, string> fail)
        {
            bool sawFormat = false;
            var victories = new List<string>();
            var defeats = new List<string>();
            foreach (var (key, value, line) in meta)
            {
                switch (key)
                {
                    case "format":
                        sawFormat = true;
                        if (!int.TryParse(value, out int fmt) || fmt != CurrentFormat)
                            fail(line, $"Unsupported format '{value}' (this loader reads {CurrentFormat}).");
                        break;
                    case "objective": chapter.ObjectiveText = value; break;
                    case "victory": victories.Add(value); break;
                    case "defeat": defeats.Add(value); break;
                    case "next": chapter.NextChapter = value; break;
                    case "maxdeploy":
                        if (!int.TryParse(value, out int md) || md < 0) fail(line, $"Bad maxDeploy '{value}'.");
                        else chapter.MaxDeploy = md;
                        break;
                    case "maxinventory":
                    case "max_inventory":
                        if (!int.TryParse(value, out int mi) || mi < 1) fail(line, $"Bad maxInventory '{value}'.");
                        else chapter.MaxInventory = mi;
                        break;
                    case "formation":
                        if (value.Length == 0) fail(line, "Bad formation region name.");
                        else chapter.FormationRegion = value;
                        break;
                    case "view":
                        {
                            string v = value.Trim().ToLowerInvariant();
                            if (v == "iso") chapter.IsIso = true;
                            else if (v == "flat" || v.Length == 0) chapter.IsIso = false;
                            else fail(line, $"Bad view '{value}' (want iso or flat).");
                            break;
                        }
                    default: fail(line, $"Unknown META key '{key}'."); break;
                }
            }
            if (!sawFormat) fail(0, "META is missing 'format: 1'.");
            foreach (string v in victories) AddObjective(chapter.Victory, v, meta, fail, "victory");
            foreach (string d in defeats) AddObjective(chapter.Defeat, d, meta, fail, "defeat");
        }

        private static void AddObjective(List<ObjectiveSpec> list, string text,
            List<(string key, string value, int line)> meta, Action<int, string> fail, string which)
        {
            int line = 0;
            foreach (var (k, v, ln) in meta) if (k == which && v == text) { line = ln; break; }
            string[] parts = text.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) { fail(line, $"Empty {which} spec."); return; }
            string head = parts[0].ToLowerInvariant();
            if (which == "victory")
            {
                if (head == "rout" && parts.Length == 1) { list.Add(ObjectiveSpec.RoutEnemies()); return; }
                if (head == "survive" && parts.Length == 2 && int.TryParse(parts[1], out int n) && n >= 1)
                { list.Add(ObjectiveSpec.Survive(n)); return; }
                if (head == "seize" && parts.Length == 3) { list.Add(ObjectiveSpec.Seize(parts[1], parts[2])); return; }
                if (head == "escape" && parts.Length == 3) { list.Add(ObjectiveSpec.Escape(parts[1], parts[2])); return; }
                if (head == "event" && parts.Length == 1) { list.Add(ObjectiveSpec.EventOnly()); return; }
            }
            else
            {
                if (head == "all_dead" && parts.Length == 1) { list.Add(ObjectiveSpec.AllDead()); return; }
                if (head == "protect" && parts.Length >= 2)
                {
                    string[] tags = text.Substring(text.IndexOf("protect", StringComparison.OrdinalIgnoreCase) + 7)
                        .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                    for (int i = 0; i < tags.Length; i++) tags[i] = tags[i].Trim();
                    list.Add(ObjectiveSpec.Protect(tags));
                    return;
                }
                if (head == "event" && parts.Length == 1) { list.Add(ObjectiveSpec.EventOnly()); return; }
            }
            fail(line, $"Bad {which} spec '{text}'.");
        }

        private static void ParseTerrainRow(Chapter chapter, Dictionary<string, TerrainType> set,
            string[] cells, int lineNo, Action<int, string> fail)
        {
            // key name inf arm cav fly avoid def [hp:N] [group:cost ...]
            if (cells.Length < 8) { fail(lineNo, "TERRAINS rows need: key name inf arm cav fly avoid def."); return; }
            if (!TryInts(cells, 2, 6, out int[] costs)) { fail(lineNo, "TERRAINS costs/avoid/def must be integers."); return; }
            var custom = new Dictionary<string, int>();
            int hp = 0;
            for (int i = 8; i < cells.Length; i++)
            {
                string tok = cells[i];
                int colon = tok.IndexOf(':');
                if (colon < 0) { fail(lineNo, $"Bad terrain token '{tok}' (want hp:N or group:cost)."); return; }
                string k = tok.Substring(0, colon).Trim().ToLowerInvariant();
                string v = tok.Substring(colon + 1).Trim();
                if (k == "hp")
                {
                    if (!int.TryParse(v, out hp)) { fail(lineNo, $"Bad hp '{v}'."); return; }
                }
                else
                {
                    if (!int.TryParse(v, out int c)) { fail(lineNo, $"Bad cost '{tok}'."); return; }
                    custom[k] = c;
                }
            }
            if (set.ContainsKey(cells[0])) { fail(lineNo, $"Terrain '{cells[0]}' already exists."); return; }
            var dict = new Dictionary<string, int>
            {
                [MovementGroups.Infantry] = costs[0],
                [MovementGroups.Armor] = costs[1],
                [MovementGroups.Cavalry] = costs[2],
                [MovementGroups.Flier] = costs[3],
            };
            foreach (var kv in custom) dict[kv.Key] = kv.Value;
            set[cells[0]] = new TerrainType(cells[0], cells[1], dict, costs[4], costs[5], hp);
        }

        private static Board? BuildBoard(List<(string row, int line)> rows,
            Dictionary<char, string> legend, Dictionary<char, int> legendLines,
            Dictionary<string, TerrainType> terrains, Action<int, string> fail)
        {
            if (rows.Count == 0) { fail(0, "MAP has no rows."); return null; }
            int width = rows[0].row.Length;
            bool clean = true;
            for (int i = 0; i < rows.Count; i++)
            {
                if (rows[i].row.Length != width) { fail(rows[i].line, $"Row is {rows[i].row.Length} wide, expected {width}."); clean = false; }
                for (int x = 0; x < rows[i].row.Length; x++)
                {
                    char g = rows[i].row[x];
                    if (!legend.ContainsKey(g)) { fail(rows[i].line, $"Glyph '{g}' has no LEGEND entry."); clean = false; continue; }
                    if (!terrains.ContainsKey(legend[g]))
                    { fail(legendLines[g], $"LEGEND maps '{g}' to unknown terrain '{legend[g]}'."); clean = false; }
                }
            }
            if (!clean) return null;
            try
            {
                var arr = new string[rows.Count];
                for (int i = 0; i < rows.Count; i++) arr[i] = rows[i].row;
                return Board.FromRows(arr, "plain", new Dictionary<char, string>(legend), terrains);
            }
            catch (FormatException)
            {
                // Unreachable: validated above, but never let the loader throw.
                fail(0, "MAP is invalid.");
                return null;
            }
        }

        private static void ParseArmoryRow(Chapter chapter, string[] cells, int lineNo, Action<int, string> fail)
        {
            // key name type rank might hit crit wt minR-maxR uses [magic]
            if (cells.Length < 10) { fail(lineNo, "ARMORY rows need: key name type rank might hit crit wt minR-maxR uses."); return; }
            if (!Enum.TryParse(cells[2], true, out WeaponType type)) { fail(lineNo, $"Unknown weapon type '{cells[2]}'."); return; }
            if (!Enum.TryParse(cells[3], true, out WeaponRank rank)) { fail(lineNo, $"Unknown weapon rank '{cells[3]}'."); return; }
            if (!TryInts(cells, 4, 4, out int[] nums)) { fail(lineNo, "ARMORY might/hit/crit/wt must be integers."); return; }
            string[] range = cells[8].Split('-');
            if (range.Length != 2 || !int.TryParse(range[0], out int minR) || !int.TryParse(range[1], out int maxR) || minR < 0 || maxR < minR)
            { fail(lineNo, $"Bad range '{cells[8]}' (want min-max)."); return; }
            if (cells.Length < 10 || !int.TryParse(cells[9], out int uses) || uses < 0)
            { fail(lineNo, "ARMORY needs uses >= 0 as the 10th column."); return; }
            bool magic = false;
            var effective = new List<string>();
            int heal = 0;
            int price = 0;
            string statusName = "";
            int statusDur = 0;
            for (int i = 10; i < cells.Length; i++)
            {
                if (cells[i].Equals("magic", StringComparison.OrdinalIgnoreCase)) magic = true;
                else if (cells[i].StartsWith("effective:", StringComparison.OrdinalIgnoreCase))
                {
                    string list = cells[i].Substring("effective:".Length);
                    foreach (string tag in list.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                        if (tag.Trim().Length > 0) effective.Add(tag.Trim());
                    if (effective.Count == 0) { fail(lineNo, $"Bad armory token '{cells[i]}' (want effective:a,b)."); return; }
                }
                else if (cells[i].StartsWith("heal:", StringComparison.OrdinalIgnoreCase))
                {
                    if (!int.TryParse(cells[i].Substring("heal:".Length), out heal) || heal < 0)
                    { fail(lineNo, $"Bad armory token '{cells[i]}' (want heal:N)."); return; }
                }
                else if (cells[i].StartsWith("price:", StringComparison.OrdinalIgnoreCase))
                {
                    if (!int.TryParse(cells[i].Substring("price:".Length), out price) || price < 0)
                    { fail(lineNo, $"Bad armory token '{cells[i]}' (want price:N)."); return; }
                }
                else if (cells[i].StartsWith("status:", StringComparison.OrdinalIgnoreCase))
                {
                    string[] sp = cells[i].Substring("status:".Length).Split(':');
                    if (sp.Length != 2 || sp[0].Trim().Length == 0 || !int.TryParse(sp[1], out statusDur) || statusDur < 1)
                    { fail(lineNo, $"Bad armory token '{cells[i]}' (want status:name:N)."); return; }
                    statusName = sp[0].Trim().ToLowerInvariant();
                }
                else { fail(lineNo, $"Bad armory token '{cells[i]}'."); return; }
            }
            if (chapter.Armory.ContainsKey(cells[0])) { fail(lineNo, $"Duplicate weapon '{cells[0]}'."); return; }
            var weapon = new Weapon(cells[0], cells[1].Replace('_', ' '), type, rank,
                nums[0], nums[1], nums[2], nums[3], minR, maxR, uses, magic)
            {
                HealPower = heal,
                Price = price,
                StatusEffect = statusName,
                StatusDuration = statusDur,
            };
            foreach (string tag in effective) weapon.EffectiveAgainst.Add(tag);
            chapter.Armory[cells[0]] = weapon;
        }

        private static void ParseUnitRow(Chapter chapter, Dictionary<string, Unit> seen,
            Dictionary<string, Unit>? templates, RawUnit raw, List<Deployment>? deployments,
            Action<int, string> fail)
        {
            string[] cells = raw.Cells;
            int need = raw.HasPosition ? 13 : 12;
            if (cells.Length < need)
            { fail(raw.Line, raw.HasPosition
                ? "UNITS rows need: x,y nid side class lvl hp pow skl spd lck def res con."
                : "TEMPLATES rows need: nid side class lvl hp pow skl spd lck def res con."); return; }
            int at = 0;
            GridPoint pos = new GridPoint();
            if (raw.HasPosition)
            {
                string[] xy = cells[at++].Split(',');
                if (xy.Length != 2 || !int.TryParse(xy[0], out int x) || !int.TryParse(xy[1], out int y))
                { fail(raw.Line, $"Bad position '{cells[0]}' (want x,y)."); return; }
                pos = new GridPoint(x, y);
            }
            string nid = cells[at++];
            if (!Enum.TryParse(cells[at++], true, out Affiliation side))
            { fail(raw.Line, $"Bad side '{cells[at - 1]}' (player/enemy/ally/other)."); return; }
            string classKey = cells[at++];
            if (!TryInts(cells, at, 9, out int[] s)) { fail(raw.Line, "Stats (lvl hp pow skl spd lck def res con) must be integers."); return; }
            at += 9;
            int lvl = s[0];
            if (lvl < 1) { fail(raw.Line, "Level must be >= 1."); return; }

            int move = 5, turn = 1, vision = 5;
            string group = "", moveGroup = MovementGroups.Infantry, name = nid, spriteKey = "";
            var items = new List<string>();
            var skills = new List<string>();
            for (; at < cells.Length; at++)
            {
                string tok = cells[at];
                int colon = tok.IndexOf(':');
                if (colon < 0) { fail(raw.Line, $"Bad token '{tok}' (want key:value)."); return; }
                string k = tok.Substring(0, colon).Trim().ToLowerInvariant();
                string v = tok.Substring(colon + 1).Trim();
                switch (k)
                {
                    case "turn":
                        if (!int.TryParse(v, out turn) || turn < 1) { fail(raw.Line, $"Bad turn '{v}'."); return; }
                        break;
                    case "group": group = v; break;
                    case "move":
                        if (!int.TryParse(v, out move) || move < 0) { fail(raw.Line, $"Bad move '{v}'."); return; }
                        break;
                    case "type": moveGroup = v.Length == 0 ? moveGroup : v; break;
                    case "name": name = v.Replace('_', ' '); break;
                    case "sprite":
                        if (v.Length == 0) { fail(raw.Line, "Bad sprite '' (want a sprite key)."); return; }
                        spriteKey = v;
                        break;
                    case "ai":
                        if (v.Equals("rush", StringComparison.OrdinalIgnoreCase) || v.Equals("hold", StringComparison.OrdinalIgnoreCase) || v.Equals("patrol", StringComparison.OrdinalIgnoreCase)) { }
                        else { fail(raw.Line, $"Bad ai '{v}' (rush/hold/patrol)."); return; }
                        break;
                    case "patrol":
                        { string[] pts = v.Split('~'); if (pts.Length != 2) { fail(raw.Line, $"Bad patrol '{v}' (want x,y~x,y)."); return; } break; }
                    case "vision":
                        if (!int.TryParse(v, out vision) || vision < 0) { fail(raw.Line, $"Bad vision '{v}'."); return; }
                        break;
                    case "skill":
                    case "skills":
                        foreach (string sk in v.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                            if (sk.Trim().Length > 0) skills.Add(sk.Trim());
                        break;
                    case "items":
                        foreach (string w in v.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                            items.Add(w.Trim());
                        break;
                    default: fail(raw.Line, $"Unknown token '{k}'."); return;
                }
            }
            if (seen.ContainsKey(nid)) { fail(raw.Line, $"Duplicate unit nid '{nid}'."); return; }

            var stats = new Stats
            {
                MaxHP = s[1], Power = s[2], Skill = s[3], Speed = s[4],
                Luck = s[5], Defense = s[6], Resistance = s[7], Constitution = s[8],
            };
            var unit = new Unit(nid, name, side, classKey, stats)
            {
                Level = lvl,
                MovePoints = move,
                MovementGroup = moveGroup,
                Vision = vision,
                SpriteKey = spriteKey,
            };
            foreach (string sk in skills) unit.Skills.Add(sk);
            // apply ai/patrol tokens (validated above, re-parse)
            for (int ti = need; ti < cells.Length; ti++)
            {
                string tok2 = cells[ti]; int c2 = tok2.IndexOf(':'); if (c2<0) continue;
                string kk = tok2.Substring(0,c2).Trim().ToLowerInvariant(); string vv = tok2.Substring(c2+1).Trim();
                if (kk=="ai") unit.AiType = vv.ToLowerInvariant();
                if (kk=="patrol") { var pts2 = vv.Split('~'); TryPoint(pts2[0], out var pa); TryPoint(pts2[1], out var pb); unit.PatrolA=pa; unit.PatrolB=pb; }
            }
            foreach (string w in items)
            {
                if (!chapter.Armory.TryGetValue(w, out Weapon? weapon))
                { fail(raw.Line, $"Unknown weapon '{w}'."); return; }
                unit.Inventory.Add(new InventoryEntry(weapon));
            }
            seen[nid] = unit;
            if (templates != null) templates[nid] = unit;
            if (deployments != null) deployments.Add(new Deployment(unit, pos, turn, group));
        }

        private static void ParseRegionRow(Chapter chapter, Board board, string[] cells, int lineNo, Action<int, string> fail)
        {
            // name kind x0,y0 x1,y1  (inclusive rect)
            if (cells.Length != 4) { fail(lineNo, "REGIONS rows need: name kind x0,y0 x1,y1."); return; }
            if (!Enum.TryParse(cells[1], true, out RegionKind kind))
            { fail(lineNo, $"Bad region kind '{cells[1]}' (event/formation)."); return; }
            if (!TryPoint(cells[2], out GridPoint a) || !TryPoint(cells[3], out GridPoint b))
            { fail(lineNo, "Region corners must be x,y."); return; }
            var region = new Region(cells[0], kind);
            for (int y = Math.Min(a.Y, b.Y); y <= Math.Max(a.Y, b.Y); y++)
                for (int x = Math.Min(a.X, b.X); x <= Math.Max(a.X, b.X); x++)
                {
                    var p = new GridPoint(x, y);
                    if (!board.InBounds(p)) { fail(lineNo, $"Region '{cells[0]}' leaves the board at {p}."); return; }
                    region.Add(p);
                }
            chapter.Regions.Add(region);
        }

        private static void ParseInteractableRow(Chapter chapter, Board board, string[] cells, int lineNo, Action<int, string> fail)
        {
            // name kind x,y [locked keyId] [becomes:terrainKey]
            if (cells.Length < 3 || cells.Length > 6)
            { fail(lineNo, "INTERACTABLES rows need: name kind x,y [locked keyId] [becomes:key]."); return; }
            if (!Enum.TryParse(cells[1], true, out InteractableKind kind))
            { fail(lineNo, $"Bad interactable kind '{cells[1]}' (door/chest/village)."); return; }
            if (!TryPoint(cells[2], out GridPoint pos))
            { fail(lineNo, "Interactable position must be x,y."); return; }
            bool locked = false;
            string keyId = "";
            string becomes = "";
            int i = 3;
            if (i < cells.Length && cells[i].Equals("locked", StringComparison.OrdinalIgnoreCase))
            {
                if (kind == InteractableKind.Village)
                { fail(lineNo, "Villages take no locked/keyId."); return; }
                if (i + 1 >= cells.Length)
                { fail(lineNo, "Locked interactables need a key id."); return; }
                if (cells[i + 1].Length == 0)
                { fail(lineNo, "Locked interactables need a key id."); return; }
                locked = true;
                keyId = cells[i + 1];
                i += 2;
            }
            if (i < cells.Length)
            {
                if (!cells[i].StartsWith("becomes:", StringComparison.OrdinalIgnoreCase))
                { fail(lineNo, $"Bad interactable token '{cells[i]}' (want becomes:key)."); return; }
                becomes = cells[i].Substring("becomes:".Length).Trim();
                if (becomes.Length == 0)
                { fail(lineNo, $"Bad interactable token '{cells[i]}' (want becomes:key)."); return; }
                i++;
            }
            if (i != cells.Length)
            { fail(lineNo, "INTERACTABLES rows need: name kind x,y [locked keyId] [becomes:key]."); return; }
            if (!board.InBounds(pos))
            { fail(lineNo, $"Interactable '{cells[0]}' leaves the board at {pos}."); return; }
            if (becomes.Length > 0 && !board.Terrains.ContainsKey(becomes))
            { fail(lineNo, $"Interactable '{cells[0]}' has unknown terrain '{becomes}'."); return; }
            chapter.Interactables.Add(new Interactable(cells[0], kind, pos) { Locked = locked, KeyId = keyId, BecomesTerrainKey = becomes });
        }

        private static RawEvent? ParseEventHeader(string text, int lineNo, Action<int, string> fail)
        {
            // <name> on <trigger> [if <cond>] [priority N]   |   <name> on turn <N> - <team> ...
            int on = text.IndexOf(" on ", StringComparison.OrdinalIgnoreCase);
            if (on < 0) { fail(lineNo, "Event headers are '### <name> on <trigger> [if <cond>] [priority N]'."); return null; }
            string name = text.Substring(0, on).Trim();
            string spec = text.Substring(on + 4).Trim();
            if (name.Length == 0 || spec.Length == 0)
            { fail(lineNo, "Event headers need a name and a trigger."); return null; }
            var ev = new RawEvent { Name = name, Line = lineNo };

            string[] toks = spec.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
            if (toks.Length >= 2 && toks[toks.Length - 2].Equals("priority", StringComparison.OrdinalIgnoreCase))
            {
                if (!int.TryParse(toks[toks.Length - 1], out ev.Priority))
                { fail(lineNo, "Bad priority (want an integer)."); return null; }
                spec = string.Join(" ", toks, 0, toks.Length - 2);
            }
            string cond = "";
            int ifAt = spec.IndexOf(" if ", StringComparison.OrdinalIgnoreCase);
            if (ifAt >= 0)
            {
                cond = spec.Substring(ifAt + 4).Trim();
                spec = spec.Substring(0, ifAt).Trim();
                if (cond.Length == 0) { fail(lineNo, "Empty condition after 'if'."); return null; }
            }
            ev.Condition = cond;

            string[] head = spec.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
            if (head.Length == 4 && head[0].Equals("turn", StringComparison.OrdinalIgnoreCase))
            {
                if (!int.TryParse(head[1], out int turn) || turn < 1 || head[2] != "-")
                { fail(lineNo, "Turn triggers are 'turn <N> - <player|enemy>'."); return null; }
                string team = head[3].ToLowerInvariant();
                if (team == "player") ev.TriggerWord = "playerphase";
                else if (team == "enemy") ev.TriggerWord = "enemyphase";
                else { fail(lineNo, $"Bad turn team '{head[3]}'."); return null; }
                ev.TurnSpec = turn.ToString();
                ev.Condition = cond.Length == 0 ? $"turn == {turn}" : $"turn == {turn} && {cond}";
            }
            else if (head.Length == 1)
            {
                ev.TriggerWord = head[0];
            }
            else { fail(lineNo, $"Bad trigger '{spec}'."); return null; }
            return ev;
        }

        private static void BuildEvent(Chapter chapter, RawEvent raw, Action<int, string> fail)
        {
            if (!TriggerAliases.TryGetValue(raw.TriggerWord, out EventTrigger trigger)
                && !Enum.TryParse(raw.TriggerWord, true, out trigger))
            { fail(raw.Line, $"Unknown trigger '{raw.TriggerWord}'."); return; }
            var ev = new GameEvent(raw.Name, trigger, raw.Condition) { Priority = raw.Priority };
            foreach (var (text, lineNo) in raw.Commands)
            {
                EventCommand cmd;
                try { cmd = EventCommand.Parse(text); }
                catch (FormatException ex) { fail(lineNo, ex.Message); return; }
                if (!CommandCatalog.TryValidate(cmd, out string? err)) { fail(lineNo, err ?? "bad command"); return; }
                ev.Commands.Add(cmd);
            }
            try { SimpleConditionEvaluator.Evaluate(ev.Condition, new Dictionary<string, object?>()); }
            catch (FormatException ex) { fail(raw.Line, $"Bad condition: {ex.Message}"); return; }
            chapter.Events.Add(ev);
        }

        private static string? DesugarDialogue(string text, int lineNo, Action<int, string> fail)
        {
            int colon = text.IndexOf(':');
            if (colon < 0) { fail(lineNo, "Dialogue lines are '- speaker: text'."); return null; }
            string speaker = text.Substring(0, colon).Trim();
            string spoken = text.Substring(colon + 1).Trim();
            if (speaker.Length == 0 || spoken.Length == 0)
            { fail(lineNo, "Dialogue lines need a speaker and text."); return null; }
            return "speak;" + speaker + ";" + spoken;
        }

        // ---------- small parse helpers ----------

        private static bool TryInts(string[] cells, int start, int count, out int[] values)
        {
            values = new int[count];
            if (start + count > cells.Length) return false;
            for (int i = 0; i < count; i++)
                if (!int.TryParse(cells[start + i], out values[i])) return false;
            return true;
        }

        private static bool TryPoint(string text, out GridPoint p)
        {
            p = new GridPoint();
            string[] parts = text.Split(',');
            if (parts.Length != 2) return false;
            if (!int.TryParse(parts[0], out int x) || !int.TryParse(parts[1], out int y)) return false;
            p = new GridPoint(x, y);
            return true;
        }
    }
}
