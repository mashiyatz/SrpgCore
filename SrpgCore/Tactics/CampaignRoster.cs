using System;
using System.Collections.Generic;
using SrpgCore.Data;
using SrpgCore.Grid;

namespace SrpgCore.Tactics
{
    /// <summary>Options for campaign roster transfer between chapters.</summary>
    public sealed class CampaignTransferOptions
    {
        /// <summary>Heal survivors to full and clear statuses on entry (GBA behavior).</summary>
        public bool HealFull = true;
        /// <summary>When true, dead player units stay dead; otherwise they revive.</summary>
        public bool Permadeath;
        /// <summary>Nids known dead (authoritative when permadeath is on).</summary>
        public HashSet<string> DeadNids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Campaign roster transfer: the fix for benched-unit loss and shared-reference aliasing.
    /// Collect merges Field placements (mutated stats win) with unplaced turn-1 player
    /// deployments (benched truth), cloning every unit. Apply overwrites the next chapter's
    /// matching deployments with roster clones so LoadChapter never loses benched units.
    /// </summary>
    public static class CampaignRoster
    {
        /// <summary>Collects the player roster: placed units plus unplaced player deployments (any spawn turn) and carried passengers.</summary>
        /// <param name="state">Live chapter runtime at victory.</param>
        /// <returns>Cloned unit snapshots sorted by nid.</returns>
        public static List<UnitSnapshot> Collect(ChapterState state)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            var snap = SaveSystem.Save(state);
            var byNid = new Dictionary<string, UnitSnapshot>(StringComparer.OrdinalIgnoreCase);
            foreach (PlacedSnapshot ps in snap.Placed)
            {
                if (ps.Unit.Side != Affiliation.Player) continue;
                byNid[ps.Unit.Nid] = CloneSnapshot(ps.Unit);
            }
            foreach (UnitSnapshot u in snap.Carried)
                byNid[u.Nid] = CloneSnapshot(u);
            // Unplaced truth: player deployments never spawned (benched by the deploy
            // screen, or reinforcements with SpawnTurn > victories turn). Field wins on conflict.
            foreach (Deployment d in state.Chapter.Deployments)
            {
                if (d.Unit.Side != Affiliation.Player) continue;
                if (byNid.ContainsKey(d.Unit.Nid)) continue;
                var u = SnapshotFromUnit(d.Unit);
                byNid[d.Unit.Nid] = u;
            }
            var list = new List<UnitSnapshot>(byNid.Values);
            list.Sort((a, b) => string.CompareOrdinal(a.Nid, b.Nid));
            return list;
        }

        /// <summary>Applies a roster onto the next chapter: matching nids are overwritten with carried stats, others stay authored.</summary>
        /// <param name="next">Next chapter (mutated in place before StartNew).</param>
        /// <param name="roster">Roster from Collect.</param>
        /// <param name="opts">Heal and permadeath options.</param>
        /// <returns>(applied, added, skippedDead, spilled) counts; spilled reserves had no free tile.</returns>
        public static (int applied, int added, int skippedDead, int spilled) Apply(Chapter next, List<UnitSnapshot> roster, CampaignTransferOptions? opts = null)
        {
            if (next == null) throw new ArgumentNullException(nameof(next));
            if (roster == null) throw new ArgumentNullException(nameof(roster));
            opts ??= new CampaignTransferOptions();
            var byNid = new Dictionary<string, UnitSnapshot>(StringComparer.OrdinalIgnoreCase);
            foreach (UnitSnapshot u in roster) byNid[u.Nid] = u;
            var dead = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (string n in opts.DeadNids) dead.Add(n);
            // Phase 1 (no mutation): resolve every unit first so a missing armory key
            // throws before the chapter is half-rewritten.
            var replacements = new List<(int index, Unit unit)>();
            for (int i = 0; i < next.Deployments.Count; i++)
            {
                Deployment d = next.Deployments[i];
                if (!byNid.TryGetValue(d.Unit.Nid, out UnitSnapshot? rs)) continue;
                replacements.Add((i, UnitFromSnapshot(next, rs, opts.HealFull)));
            }
            var additions = new List<UnitSnapshot>();
            foreach (UnitSnapshot rs in roster)
            {
                bool found = false;
                foreach (Deployment d in next.Deployments)
                    if (string.Equals(d.Unit.Nid, rs.Nid, StringComparison.OrdinalIgnoreCase)) { found = true; break; }
                if (!found && !(opts.Permadeath && dead.Contains(rs.Nid)))
                {
                    // Resolve now (throws here, pre-commit) to validate the entry.
                    UnitFromSnapshot(next, rs, opts.HealFull);
                    additions.Add(rs);
                }
            }
            // Phase 2 (commit): dead removal, replacements, reserve appends.
            int skipped = 0;
            if (opts.Permadeath && dead.Count > 0)
            {
                for (int i = next.Deployments.Count - 1; i >= 0; i--)
                    if (dead.Contains(next.Deployments[i].Unit.Nid)) { next.Deployments.RemoveAt(i); skipped++; }
                var deadTemplates = new List<string>();
                foreach (var kv in next.UnitTemplates)
                    if (dead.Contains(kv.Key)) deadTemplates.Add(kv.Key);
                foreach (string k in deadTemplates) next.UnitTemplates.Remove(k);
            }
            int applied = 0;
            foreach (var (index, unit) in replacements)
            {
                // A permadeath removal may have shifted indices: match by nid.
                int at = -1;
                for (int i = 0; i < next.Deployments.Count; i++)
                    if (string.Equals(next.Deployments[i].Unit.Nid, unit.Nid, StringComparison.OrdinalIgnoreCase)) { at = i; break; }
                if (at < 0) continue;
                Deployment d = next.Deployments[at];
                next.Deployments[at] = new Deployment(unit, d.Position, d.SpawnTurn, d.Group);
                applied++;
            }
            // Roster-only units become real benched deployments at free tiles (formation-preferred).
            // Real deployments (not templates) so the deploy screen, ChooseDeployments, and
            // Validate all see them; BeginDeployment benches everything turn-1 anyway.
            var used = new HashSet<string>();
            foreach (Deployment d in next.Deployments) used.Add(d.Position.X + "," + d.Position.Y);
            int added = 0, spilled = 0;
            foreach (UnitSnapshot rs in additions)
            {
                Unit u = UnitFromSnapshot(next, rs, opts.HealFull);
                GridPoint? at = FindFreeTile(next, used);
                if (at == null) { spilled++; continue; }
                used.Add(at.Value.X + "," + at.Value.Y);
                next.Deployments.Add(new Deployment(u, at.Value, 1));
                added++;
            }
            return (applied, added, skipped, spilled);
        }

        private static GridPoint? FindFreeTile(Chapter next, HashSet<string> used)
        {
            if (!string.IsNullOrWhiteSpace(next.FormationRegion))
                foreach (Regions.Region r in next.Regions)
                    if (r.Name.Equals(next.FormationRegion, StringComparison.OrdinalIgnoreCase))
                        foreach (GridPoint p in r.Tiles)
                            if (IsFreeLandable(next, p) && used.Add(p.X + "," + p.Y)) return p;
            if (next.Board != null)
                for (int y = 0; y < next.Board.Height; y++)
                    for (int x = 0; x < next.Board.Width; x++)
                    {
                        var p = new GridPoint(x, y);
                        if (IsFreeLandable(next, p) && used.Add(x + "," + y)) return p;
                    }
            return null;
        }

        private static bool IsFreeLandable(Chapter next, GridPoint p)
        {
            if (next.Board == null || !next.Board.InBounds(p)) return false;
            try
            {
                if (next.Board.GetTerrain(p).MoveCost(MovementGroups.Infantry) == StandardTerrains.Impassable) return false;
            }
            catch { return false; }
            return true;
        }

        /// <summary>Builds a live unit from a snapshot, preferring snapshot weapon definitions with armory fallback.</summary>
        /// <param name="chapter">Chapter providing the armory fallback.</param>
        /// <param name="s">Snapshot to rebuild.</param>
        /// <param name="healFull">When true, restore to full HP and clear statuses.</param>
        /// <returns>Live unit.</returns>
        public static Unit UnitFromSnapshot(Chapter chapter, UnitSnapshot s, bool healFull)
        {
            var unit = new Unit(s.Nid, s.Name, s.Side, s.ClassKey, s.Stats.Clone())
            {
                Level = s.Level, Experience = s.Experience,
                CurrentHP = s.CurrentHP, MovePoints = s.MovePoints, Jump = s.Jump,
                MovementGroup = s.MovementGroup, Vision = s.Vision,
                CarryingNid = "", AiType = s.AiType, SpriteKey = s.SpriteKey ?? "",
                PatrolA = s.PatrolA, PatrolB = s.PatrolB,
            };
            if (s.Growths != null)
                unit.Growths = new GrowthRates { HP = s.Growths.HP, Power = s.Growths.Power, Skill = s.Growths.Skill, Speed = s.Growths.Speed, Luck = s.Growths.Luck, Defense = s.Growths.Defense, Resistance = s.Growths.Resistance };
            foreach (string sk in s.Skills) unit.Skills.Add(sk);
            foreach (var kv in s.Statuses) unit.Statuses[kv.Key] = kv.Value;
            foreach (var kv in s.Wexp) unit.Wexp[kv.Key] = kv.Value;
            foreach (InventorySnapshot inv in s.Inventory)
                unit.Inventory.Add(SaveSystem.ResolveEntry(chapter, inv));
            if (healFull) { unit.CurrentHP = unit.Stats.MaxHP; unit.Statuses.Clear(); }
            if (unit.Side == Affiliation.Player && !unit.IsAlive) unit.CurrentHP = healFull ? unit.Stats.MaxHP : Math.Max(1, unit.CurrentHP);
            return unit;
        }

        private static UnitSnapshot SnapshotFromUnit(Unit u)
        {
            var s = new UnitSnapshot
            {
                Nid = u.Nid, Name = u.Name, Side = u.Side, ClassKey = u.ClassKey,
                Level = u.Level, Experience = u.Experience, Stats = u.Stats.Clone(),
                CurrentHP = u.CurrentHP, MovePoints = u.MovePoints, Jump = u.Jump,
                MovementGroup = u.MovementGroup, Vision = u.Vision,
                CarryingNid = u.CarryingNid, AiType = u.AiType, SpriteKey = u.SpriteKey,
                PatrolA = u.PatrolA, PatrolB = u.PatrolB,
            };
            if (u.Growths != null)
                s.Growths = new GrowthSnapshot { HP = u.Growths.HP, Power = u.Growths.Power, Skill = u.Growths.Skill, Speed = u.Growths.Speed, Luck = u.Growths.Luck, Defense = u.Growths.Defense, Resistance = u.Growths.Resistance };
            foreach (string sk in u.Skills) s.Skills.Add(sk);
            foreach (var kv in u.Statuses) s.Statuses[kv.Key] = kv.Value;
            foreach (var kv in u.Wexp) s.Wexp[kv.Key] = kv.Value;
            foreach (InventoryEntry e in u.Inventory)
                s.Inventory.Add(new InventorySnapshot { WeaponKey = e.Weapon.Key, UsesLeft = e.UsesLeft, Weapon = SaveSystem.SnapshotWeapon(e.Weapon) });
            return s;
        }

        private static UnitSnapshot CloneSnapshot(UnitSnapshot s)
        {
            var c = new UnitSnapshot
            {
                Nid = s.Nid, Name = s.Name, Side = s.Side, ClassKey = s.ClassKey,
                Level = s.Level, Experience = s.Experience, Stats = s.Stats.Clone(),
                CurrentHP = s.CurrentHP, MovePoints = s.MovePoints, Jump = s.Jump,
                MovementGroup = s.MovementGroup, Vision = s.Vision,
                CarryingNid = s.CarryingNid, AiType = s.AiType, SpriteKey = s.SpriteKey ?? "",
                PatrolA = s.PatrolA, PatrolB = s.PatrolB,
                Growths = s.Growths == null ? null : new GrowthSnapshot { HP = s.Growths.HP, Power = s.Growths.Power, Skill = s.Growths.Skill, Speed = s.Growths.Speed, Luck = s.Growths.Luck, Defense = s.Growths.Defense, Resistance = s.Growths.Resistance },
            };
            foreach (string sk in s.Skills) c.Skills.Add(sk);
            foreach (var kv in s.Statuses) c.Statuses[kv.Key] = kv.Value;
            foreach (var kv in s.Wexp) c.Wexp[kv.Key] = kv.Value;
            foreach (InventorySnapshot inv in s.Inventory)
                c.Inventory.Add(new InventorySnapshot
                {
                    WeaponKey = inv.WeaponKey, UsesLeft = inv.UsesLeft,
                    Weapon = inv.Weapon == null ? null : SaveSystem.SnapshotWeapon(SaveSystem.WeaponFromSnapshot(inv.Weapon)),
                });
            return c;
        }
    }
}
