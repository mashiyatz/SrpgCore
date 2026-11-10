using System;
using System.Collections.Generic;
using SrpgCore.Combat;
using SrpgCore.Data;
using SrpgCore.Events;
using SrpgCore.Grid;
using SrpgCore.Random;

namespace SrpgCore.Tactics
{
    /// <summary>Chapter state after polling objectives: still fighting, won, or lost.</summary>
    public enum ObjectiveOutcome
    {
        /// <summary>No victory or defeat spec is met yet.</summary>
        Ongoing = 0,
        /// <summary>A victory spec (or scripted win) is met.</summary>
        Victory = 1,
        /// <summary>A defeat spec (or scripted loss) is met.</summary>
        Defeat = 2,
    }

    /// <summary>
    /// Live chapter runtime: the Chapter definition plus Field placements, Phases turn
    /// manager, CombatRng/GrowthRng seeded streams, Vars level flags, and spawn
    /// bookkeeping. StartNew deploys turn-1 units; call SpawnDue() after each phase
    /// advance — spawn comparison is &lt;= (a skipped turn number must never drop
    /// reinforcements). EvaluateObjectives polls scripted result, then defeat, then
    /// victory specs; AllUnits and SpawnTurnOf feed the checks.
    /// </summary>
    public sealed class ChapterState
    {
        private static readonly string[] _defaultTeamsBacking =
            new[] { "player", "enemy", "ally", "other" };

        /// <summary>Default phase order when hosts supply no team list. Returns a read-only view so hosts cannot mutate the shared table.</summary>
        public static IReadOnlyList<string> DefaultTeams => Array.AsReadOnly(_defaultTeamsBacking);

        /// <summary>Authored chapter data (Board, Deployments, Events, Objectives).</summary>
        public Chapter Chapter { get; }
        /// <summary>Board plus live placements (LiveEnemiesOf/BlockingTiles/AllyTiles).</summary>
        public Battlefield Field { get; }
        /// <summary>Phase cycler tracking turn number and scripted result.</summary>
        public TurnManager Phases { get; }
        /// <summary>Seeded stream for combat rolls.</summary>
        public SeededRng CombatRng { get; }
        /// <summary>Seeded stream for growth rolls.</summary>
        public SeededRng GrowthRng { get; }
        /// <summary>Seeded stream for skill proc rolls (stream 2). Deterministic and save-scoped.</summary>
        public SeededRng ProcRng { get; }
        /// <summary>Snapshot of skill definitions at chapter start for deterministic saves/replays.</summary>
        public Dictionary<string, SkillDef> SkillDefs { get; } = new Dictionary<string, SkillDef>(StringComparer.OrdinalIgnoreCase);
        /// <summary>Read-only view of the snapshotted skill catalog for CombatResolver.</summary>
        public IReadOnlyDictionary<string, SkillDef> SkillCatalogInstance => SkillDefs;
        /// <summary>Level flag storage (set_flag, visited_*, opened_*) read by event conditions.</summary>
        public Dictionary<string, object?> Vars { get; } = new Dictionary<string, object?>();
        /// <summary>Shared convoy storage (host-persisted; also held per-runner headlessly).</summary>
        public List<InventoryEntry> Convoy { get; } = new List<InventoryEntry>();
        /// <summary>Party gold for shop buy and sell (gold;N adjusts, buys deduct, sells pay half).</summary>
        public int PartyGold { get; set; }
        /// <summary>Support point ledger (host-persisted across saves).</summary>
        public SupportLedger Supports { get; } = new SupportLedger();
        /// <summary>Rescued passengers held off-map keyed by nid.</summary>
        public Dictionary<string, Unit> Carried { get; } = new Dictionary<string, Unit>(StringComparer.Ordinal);

        private readonly HashSet<Deployment> _spawned = new HashSet<Deployment>();
        private readonly Dictionary<string, int> _spawnTurns = new Dictionary<string, int>();

        private ChapterState(Chapter chapter, uint seed, IReadOnlyList<string> teams)
        {
            Chapter = chapter;
            Field = new Battlefield(chapter.Board!);
            Phases = new TurnManager(teams);
            CombatRng = SeededRng.CreateStream(seed, 0);
            GrowthRng = SeededRng.CreateStream(seed, 1);
            ProcRng = SeededRng.CreateStream(seed, 2);
            foreach (var kv in SkillCatalog.Snapshot()) SkillDefs[kv.Key] = kv.Value.Clone();
        }

        /// <summary>Builds a chapter runtime and deploys turn-1 units.</summary>
        /// <param name="chapter">Authored chapter with board and deployments.</param>
        /// <param name="seed">Base seed for the combat and growth streams.</param>
        /// <param name="teams">Phase order (defaults to player enemy ally other).</param>
        /// <returns>The new live chapter runtime.</returns>
        public static ChapterState StartNew(Chapter chapter, uint seed, IReadOnlyList<string>? teams = null)
        {
            if (chapter == null) throw new ArgumentNullException(nameof(chapter));
            if (chapter.Board == null) throw new ArgumentException("Chapter has no board.", nameof(chapter));
            var state = new ChapterState(chapter, seed, teams ?? DefaultTeams);
            foreach (Deployment d in chapter.Deployments)
                state._spawnTurns[d.Unit.Nid] = d.SpawnTurn;
            foreach (string nid in chapter.UnitTemplates.Keys)
                if (!state._spawnTurns.ContainsKey(nid)) state._spawnTurns[nid] = 1;
            state.SpawnDue();
            return state;
        }

        /// <summary>Deploy everything due (spawn turn reached, not yet spawned). Returns new arrivals.</summary>
        /// <returns>Placements deployed by this call.</returns>
        public List<PlacedUnit> SpawnDue()
        {
            var arrivals = new List<PlacedUnit>();
            foreach (Deployment d in Chapter.Deployments)
            {
                if (_spawned.Contains(d) || d.SpawnTurn > Phases.TurnNumber) continue;
                _spawned.Add(d);
                arrivals.Add(Field.Deploy(d.Unit, d.Position));
            }
            return arrivals;
        }

        /// <summary>After a save-restore, deployments already present must not respawn.</summary>
        internal void MarkSpawnedNids(HashSet<string> nids)
        {
            foreach (Deployment d in Chapter.Deployments)
                if (nids.Contains(d.Unit.Nid)) _spawned.Add(d);
        }

        /// <summary>Lists nids of deployments already spawned.</summary>
        /// <returns>Nids of spawned deployments.</returns>
        public HashSet<string> SpawnedNidsSnapshot()
        {
            var nids = new HashSet<string>();
            foreach (Deployment d in _spawned) nids.Add(d.Unit.Nid);
            return nids;
        }

        /// <summary>Looks up the scheduled spawn turn for a unit nid (1 when unknown).</summary>
        /// <param name="nid">Unit nid to look up.</param>
        /// <returns>The spawn turn recorded for the nid.</returns>
        public int SpawnTurnOf(string nid) =>
            _spawnTurns.TryGetValue(nid, out int turn) ? turn : 1;

        /// <summary>Collects every placed unit on the board.</summary>
        /// <returns>Units in placement order.</returns>
        public List<Unit> AllUnits()
        {
            var units = new List<Unit>();
            foreach (PlacedUnit p in Field.Placed) units.Add(p.Unit);
            return units;
        }

        /// <summary>Polls defeat specs then victory specs after honoring any scripted result.</summary>
        /// <returns>Ongoing, Victory, or Defeat for the current state.</returns>
        public ObjectiveOutcome EvaluateObjectives()
        {
            if (Phases.BattleResult == true) return ObjectiveOutcome.Victory;
            if (Phases.BattleResult == false) return ObjectiveOutcome.Defeat;

            foreach (ObjectiveSpec spec in Chapter.Defeat)
                if (SpecMet(spec)) return ObjectiveOutcome.Defeat;
            foreach (ObjectiveSpec spec in Chapter.Victory)
                if (SpecMet(spec)) return ObjectiveOutcome.Victory;
            return ObjectiveOutcome.Ongoing;
        }

        private bool SpecMet(ObjectiveSpec spec)
        {
            switch (spec.Kind)
            {
                case ObjectiveKind.Rout:
                    return Objectives.IsRouted(AllUnits(), spec.Side);
                case ObjectiveKind.Survive:
                    return Objectives.HasSurvived(Phases.TurnNumber, spec.Turns);
                case ObjectiveKind.Protect:
                    return Objectives.IsProtectFailed(AllUnits(), spec.Tags, Phases.TurnNumber, SpawnTurnOf);
                case ObjectiveKind.Seize:
                    {
                        if (spec.Tags.Count != 2) return false;
                        Regions.Region? region = null;
                        foreach (Regions.Region r in Chapter.Regions)
                            if (r.Name == spec.Tags[1]) { region = r; break; }
                        if (region == null) return false;
                        return Objectives.IsSeized(Field.Placed, spec.Tags[0], region);
                    }
                case ObjectiveKind.Escape:
                    {
                        if (spec.Tags.Count != 2) return false;
                        Regions.Region? region = null;
                        foreach (Regions.Region r in Chapter.Regions)
                            if (r.Name == spec.Tags[1]) { region = r; break; }
                        if (region == null) return false;
                        return Objectives.IsEscape(Field.Placed, spec.Tags[0], region);
                    }
                default:
                    return false;
            }
        }
    }
}
