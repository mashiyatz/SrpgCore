using System;
using System.Collections.Generic;
using SrpgCore.Data;
using SrpgCore.Grid;

namespace SrpgCore.Tactics
{
    /// <summary>Plain DTOs for saves/turnwheel/suspend (shape: Nid/Name/Side/ClassKey/Level/Experience/Stats/CurrentHP/MovePoints/MovementGroup/Vision/Skills/Statuses/Wexp/CarryingNid and Inventory keys with uses left). No JSON dependency: hosts serialize these with whatever they have.</summary>
    public sealed class UnitSnapshot
    {
        /// <summary>Unit nid key.</summary>
        public string Nid = "";
        /// <summary>Display name.</summary>
        public string Name = "";
        /// <summary>Side affiliation.</summary>
        public Affiliation Side;
        /// <summary>Class key.</summary>
        public string ClassKey = "";
        /// <summary>Current level.</summary>
        public int Level;
        /// <summary>Accumulated experience toward the next level.</summary>
        public int Experience;
        /// <summary>Cloned stat block.</summary>
        public Stats Stats = new Stats();
        /// <summary>Current hit points.</summary>
        public int CurrentHP;
        /// <summary>Base movement allowance.</summary>
        public int MovePoints;
        /// <summary>Movement group key for terrain costs.</summary>
        public string MovementGroup = "";
        /// <summary>Sight radius in Manhattan tiles.</summary>
        public int Vision = 5;
        /// <summary>Skill keys.</summary>
        public List<string> Skills = new List<string>();
        /// <summary>Active statuses with remaining durations.</summary>
        public Dictionary<string, int> Statuses = new Dictionary<string, int>();
        /// <summary>Weapon experience per weapon type.</summary>
        public Dictionary<WeaponType, int> Wexp = new Dictionary<WeaponType, int>();
        /// <summary>Nid of the carried passenger (empty when hands-free).</summary>
        public string CarryingNid = "";
        /// <summary>AI type (rush/hold) persisted across saves.</summary>
        public string AiType = "rush";
        /// <summary>Patrol waypoint A for patrol AI.</summary>
        public GridPoint PatrolA;
        /// <summary>Patrol waypoint B for patrol AI.</summary>
        public GridPoint PatrolB;
        /// <summary>Inventory as armory weapon keys with uses left.</summary>
        public List<InventorySnapshot> Inventory = new List<InventorySnapshot>();
    }

    /// <summary>One inventory entry for JSON-safe saves.</summary>
    public sealed class InventorySnapshot
    {
        /// <summary>Armory weapon key.</summary>
        public string WeaponKey = "";
        /// <summary>Uses remaining.</summary>
        public int UsesLeft;
    }

    /// <summary>One support pair for JSON-safe saves.</summary>
    public sealed class SupportPointSnapshot
    {
        /// <summary>First nid.</summary>
        public string A = "";
        /// <summary>Second nid.</summary>
        public string B = "";
        /// <summary>Accumulated points.</summary>
        public int Points;
    }

    /// <summary>Unit snapshot plus its board tile (Unit + X/Y).</summary>
    public sealed class PlacedSnapshot
    {
        /// <summary>Snapshot of the placed unit.</summary>
        public UnitSnapshot Unit = new UnitSnapshot();
        /// <summary>Board X coordinate.</summary>
        public int X;
        /// <summary>Board Y coordinate.</summary>
        public int Y;
    }

    /// <summary>One skill definition for save snapshot.</summary>
    public sealed class SkillSnapshot
    {
        /// <summary>Skill id.</summary>
        public string Id = "";
        /// <summary>Hook side.</summary>
        public int Hook;
        /// <summary>Damage bonus.</summary>
        public int DamageBonus;
        /// <summary>Hit bonus.</summary>
        public int HitBonus;
        /// <summary>Crit bonus.</summary>
        public int CritBonus;
        /// <summary>Damage taken penalty.</summary>
        public int DamageTakenPenalty;
        /// <summary>Proc chance 0-100.</summary>
        public int ProcChance;
    }

    /// <summary>Full chapter save: TurnNumber/CurrentTeamIndex/BattleResult turn state, CombatRngState/GrowthRngState/ProcRngState, Vars flags, Placed placements with positions, and SpawnedNids to suppress re-spawns on restore.</summary>
    public sealed class ChapterSnapshot
    {
        /// <summary>Turn number at save time.</summary>
        public int TurnNumber = 1;
        /// <summary>Index of the acting team at save time.</summary>
        public int CurrentTeamIndex;
        /// <summary>Scripted battle result (null while ongoing).</summary>
        public bool? BattleResult;
        /// <summary>Saved combat RNG stream state.</summary>
        public uint CombatRngState;
        /// <summary>Saved growth RNG stream state.</summary>
        public uint GrowthRngState;
        /// <summary>Saved proc RNG stream state.</summary>
        public uint ProcRngState;
        /// <summary>Chapter vars dictionary copy.</summary>
        public Dictionary<string, object?> Vars = new Dictionary<string, object?>();
        /// <summary>Placed unit snapshots.</summary>
        public List<PlacedSnapshot> Placed = new List<PlacedSnapshot>();
        /// <summary>Already-spawned nids so restore never duplicates reinforcements.</summary>
        public List<string> SpawnedNids = new List<string>();
        /// <summary>Convoy items as weapon keys with uses left.</summary>
        public List<InventorySnapshot> Convoy = new List<InventorySnapshot>();
        /// <summary>Party gold for the shop (buy deducts, sell pays half).</summary>
        public int PartyGold;
        /// <summary>Support points per bonded pair.</summary>
        public List<SupportPointSnapshot> SupportPoints = new List<SupportPointSnapshot>();
        /// <summary>Skill catalog snapshot.</summary>
        public List<SkillSnapshot> SkillDefs = new List<SkillSnapshot>();
        /// <summary>Rescued passengers held off-map.</summary>
        public List<UnitSnapshot> Carried = new List<UnitSnapshot>();
        /// <summary>Board width in tiles for the terrain grid.</summary>
        public int BoardWidth;
        /// <summary>Board height in tiles for the terrain grid.</summary>
        public int BoardHeight;
        /// <summary>Terrain keys in row major order.</summary>
        public List<string> TerrainKeys = new List<string>();
    }

    /// <summary>
    /// Save/restore over a ChapterState. Units serialize fully (no template lookup on load);
    /// weapons resolve through chapter.Armory by key, so armory keys are the save contract.
    /// Deterministic: RNG stream states round-trip, so post-load rolls continue the sequence.
    /// </summary>
    public static class SaveSystem
    {
        /// <summary>Snapshots positions, stats, inventory uses, turn state, vars, and RNG states.</summary>
        /// <param name="state">Live chapter runtime to snapshot.</param>
        /// <returns>Plain DTO snapshot the host can serialize.</returns>
        public static ChapterSnapshot Save(ChapterState state)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            var snap = new ChapterSnapshot
            {
                TurnNumber = state.Phases.TurnNumber,
                CurrentTeamIndex = state.Phases.CurrentTeamIndex,
                BattleResult = state.Phases.BattleResult,
                CombatRngState = state.CombatRng.GetState(),
                GrowthRngState = state.GrowthRng.GetState(),
                ProcRngState = state.ProcRng.GetState(),
            };
            foreach (var kv in state.Vars) snap.Vars[kv.Key] = kv.Value;
            snap.PartyGold = state.PartyGold;
            foreach (var e in state.Convoy) snap.Convoy.Add(new InventorySnapshot { WeaponKey = e.Weapon.Key, UsesLeft = e.UsesLeft });
            var sup = new List<(string a, string b, int p)>();
            foreach (var pp in state.Supports.AllPairs()) sup.Add((pp.key.a, pp.key.b, pp.points));
            sup.Sort((x, y) => { int c = string.CompareOrdinal(x.a, y.a); return c != 0 ? c : string.CompareOrdinal(x.b, y.b); });
            foreach (var s in sup) snap.SupportPoints.Add(new SupportPointSnapshot { A = s.a, B = s.b, Points = s.p });
            var skillKeys = new List<string>(state.SkillDefs.Keys);
            skillKeys.Sort(StringComparer.Ordinal);
            foreach (string key in skillKeys)
            {
                var def = state.SkillDefs[key];
                snap.SkillDefs.Add(new SkillSnapshot { Id = key, Hook = (int)def.Hook, DamageBonus = def.DamageBonus, HitBonus = def.HitBonus, CritBonus = def.CritBonus, DamageTakenPenalty = def.DamageTakenPenalty, ProcChance = def.ProcChance });
            }
            snap.BoardWidth = state.Field.Board.Width;
            snap.BoardHeight = state.Field.Board.Height;
            for (int y = 0; y < state.Field.Board.Height; y++)
                for (int x = 0; x < state.Field.Board.Width; x++)
                    snap.TerrainKeys.Add(state.Field.Board.GetTerrainKey(new GridPoint(x, y)));
            foreach (PlacedUnit p in state.Field.Placed)
            {
                var u = new UnitSnapshot
                {
                    Nid = p.Unit.Nid,
                    Name = p.Unit.Name,
                    Side = p.Unit.Side,
                    ClassKey = p.Unit.ClassKey,
                    Level = p.Unit.Level,
                    Experience = p.Unit.Experience,
                    Stats = p.Unit.Stats.Clone(),
                    CurrentHP = p.Unit.CurrentHP,
                    MovePoints = p.Unit.MovePoints,
                    MovementGroup = p.Unit.MovementGroup,
                    Vision = p.Unit.Vision,
                    CarryingNid = p.Unit.CarryingNid,
                    AiType = p.Unit.AiType,
                    PatrolA = p.Unit.PatrolA,
                    PatrolB = p.Unit.PatrolB,
                };
                foreach (string sk in p.Unit.Skills) u.Skills.Add(sk);
                foreach (var kv in p.Unit.Statuses) u.Statuses[kv.Key] = kv.Value;
                var wkeys = new List<WeaponType>(p.Unit.Wexp.Keys);
                wkeys.Sort();
                foreach (WeaponType wk in wkeys) u.Wexp[wk] = p.Unit.Wexp[wk];
                foreach (InventoryEntry e in p.Unit.Inventory)
                    u.Inventory.Add(new InventorySnapshot { WeaponKey = e.Weapon.Key, UsesLeft = e.UsesLeft });
                snap.Placed.Add(new PlacedSnapshot { Unit = u, X = p.Position.X, Y = p.Position.Y });
            }
            foreach (var kv in state.Carried)
            {
                Unit cu = kv.Value;
                var u = new UnitSnapshot
                {
                    Nid = cu.Nid,
                    Name = cu.Name,
                    Side = cu.Side,
                    ClassKey = cu.ClassKey,
                    Level = cu.Level,
                    Experience = cu.Experience,
                    Stats = cu.Stats.Clone(),
                    CurrentHP = cu.CurrentHP,
                    MovePoints = cu.MovePoints,
                    MovementGroup = cu.MovementGroup,
                    Vision = cu.Vision,
                    CarryingNid = cu.CarryingNid,
                    AiType = cu.AiType,
                    PatrolA = cu.PatrolA,
                    PatrolB = cu.PatrolB,
                };
                foreach (string sk in cu.Skills) u.Skills.Add(sk);
                foreach (var skv in cu.Statuses) u.Statuses[skv.Key] = skv.Value;
                var cwkeys = new List<WeaponType>(cu.Wexp.Keys);
                cwkeys.Sort();
                foreach (WeaponType wk in cwkeys) u.Wexp[wk] = cu.Wexp[wk];
                foreach (InventoryEntry e in cu.Inventory)
                    u.Inventory.Add(new InventorySnapshot { WeaponKey = e.Weapon.Key, UsesLeft = e.UsesLeft });
                snap.Carried.Add(u);
            }
            snap.Carried.Sort((a, b) => string.CompareOrdinal(a.Nid, b.Nid));
            foreach (string nid in state.SpawnedNidsSnapshot()) snap.SpawnedNids.Add(nid);
            snap.SpawnedNids.Sort(StringComparer.Ordinal);
            return snap;
        }

        /// <summary>Rebuilds an equivalent chapter state from a snapshot; armory keys are the save contract.</summary>
        /// <param name="chapter">Chapter providing board, armory, and event definitions.</param>
        /// <param name="snap">Snapshot to restore.</param>
        /// <param name="baseSeed">Base seed for the fresh state before RNG states are applied.</param>
        /// <param name="teams">Phase order override (defaults to the chapter order).</param>
        /// <returns>Live chapter state equivalent to the save point.</returns>
        public static ChapterState Restore(Chapter chapter, ChapterSnapshot snap, uint baseSeed, IReadOnlyList<string>? teams = null)
        {
            if (chapter == null) throw new ArgumentNullException(nameof(chapter));
            if (snap == null) throw new ArgumentNullException(nameof(snap));
            if (chapter.Board == null) throw new ArgumentException("Chapter has no board.", nameof(chapter));
            var state = ChapterState.StartNew(chapter, baseSeed, teams);
            state.PartyGold = snap.PartyGold;
            // Clear turn-1 auto-spawns; the snapshot is authoritative.
            var existing = new List<PlacedUnit>(state.Field.Placed);
            foreach (PlacedUnit p in existing) state.Field.Remove(p);
            foreach (PlacedSnapshot ps in snap.Placed)
            {
                var unit = new Unit(ps.Unit.Nid, ps.Unit.Name, ps.Unit.Side, ps.Unit.ClassKey, ps.Unit.Stats.Clone())
                {
                    Level = ps.Unit.Level,
                    Experience = ps.Unit.Experience,
                    CurrentHP = ps.Unit.CurrentHP,
                    MovePoints = ps.Unit.MovePoints,
                    MovementGroup = ps.Unit.MovementGroup,
                    Vision = ps.Unit.Vision,
                    CarryingNid = ps.Unit.CarryingNid,
                    AiType = ps.Unit.AiType,
                    PatrolA = ps.Unit.PatrolA,
                    PatrolB = ps.Unit.PatrolB,
                };
                foreach (string sk in ps.Unit.Skills) unit.Skills.Add(sk);
                foreach (var kv in ps.Unit.Statuses) unit.Statuses[kv.Key] = kv.Value;
                foreach (var kv in ps.Unit.Wexp) unit.Wexp[kv.Key] = kv.Value;
                foreach (var inv in ps.Unit.Inventory)
                {
                    if (!chapter.Armory.TryGetValue(inv.WeaponKey, out Weapon? w))
                        throw new InvalidOperationException($"Armory has no '{inv.WeaponKey}' (save contract).");
                    unit.Inventory.Add(new InventoryEntry(w, inv.UsesLeft));
                }
                state.Field.Deploy(unit, new GridPoint(ps.X, ps.Y));
            }
            state.Carried.Clear();
            foreach (UnitSnapshot us in snap.Carried)
            {
                var unit = new Unit(us.Nid, us.Name, us.Side, us.ClassKey, us.Stats.Clone())
                {
                    Level = us.Level,
                    Experience = us.Experience,
                    CurrentHP = us.CurrentHP,
                    MovePoints = us.MovePoints,
                    MovementGroup = us.MovementGroup,
                    Vision = us.Vision,
                    CarryingNid = us.CarryingNid,
                    AiType = us.AiType,
                    PatrolA = us.PatrolA,
                    PatrolB = us.PatrolB,
                };
                foreach (string sk in us.Skills) unit.Skills.Add(sk);
                foreach (var kv in us.Statuses) unit.Statuses[kv.Key] = kv.Value;
                foreach (var kv in us.Wexp) unit.Wexp[kv.Key] = kv.Value;
                foreach (var inv in us.Inventory)
                {
                    if (!chapter.Armory.TryGetValue(inv.WeaponKey, out Weapon? w2c))
                        throw new InvalidOperationException($"Armory has no '{inv.WeaponKey}' (save contract).");
                    unit.Inventory.Add(new InventoryEntry(w2c, inv.UsesLeft));
                }
                state.Carried[unit.Nid] = unit;
            }
            state.Vars.Clear();
            foreach (var kv in snap.Vars) state.Vars[kv.Key] = kv.Value;
            state.Convoy.Clear();
            foreach (var inv in snap.Convoy)
            {
                if (!chapter.Armory.TryGetValue(inv.WeaponKey, out Weapon? w2))
                    throw new InvalidOperationException($"Armory has no '{inv.WeaponKey}' (convoy).");
                state.Convoy.Add(new InventoryEntry(w2, inv.UsesLeft));
            }
            foreach (var sp in snap.SupportPoints)
                state.Supports.SetPoints(sp.A, sp.B, sp.Points);
            state.Phases.Restore(snap.CurrentTeamIndex, snap.TurnNumber, snap.BattleResult);
            state.CombatRng.SetState(snap.CombatRngState);
            state.GrowthRng.SetState(snap.GrowthRngState);
            state.ProcRng.SetState(snap.ProcRngState);
            if (snap.TerrainKeys.Count > 0)
            {
                if (snap.BoardWidth != state.Field.Board.Width || snap.BoardHeight != state.Field.Board.Height)
                    throw new InvalidOperationException("Save board size does not match chapter board.");
                if (snap.TerrainKeys.Count != snap.BoardWidth * snap.BoardHeight)
                    throw new InvalidOperationException("Save terrain grid size is corrupt.");
                for (int y = 0; y < snap.BoardHeight; y++)
                    for (int x = 0; x < snap.BoardWidth; x++)
                        state.Field.Board.SetTerrain(new GridPoint(x, y), snap.TerrainKeys[y * snap.BoardWidth + x]);
            }
            state.SkillDefs.Clear();
            foreach (var ss in snap.SkillDefs) state.SkillDefs[ss.Id] = new SrpgCore.Combat.SkillDef(ss.Id) { Hook = (SrpgCore.Combat.SkillHook)ss.Hook, DamageBonus = ss.DamageBonus, HitBonus = ss.HitBonus, CritBonus = ss.CritBonus, DamageTakenPenalty = ss.DamageTakenPenalty, ProcChance = ss.ProcChance };
            var nids = new HashSet<string>();
            if (snap.SpawnedNids.Count > 0)
            {
                foreach (string nid in snap.SpawnedNids) nids.Add(nid);
            }
            else
            {
                foreach (PlacedSnapshot ps in snap.Placed) nids.Add(ps.Unit.Nid);
                foreach (UnitSnapshot us in snap.Carried) nids.Add(us.Nid);
            }
            state.MarkSpawnedNids(nids);
            return state;
        }
    }
}
