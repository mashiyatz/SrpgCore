using System;
using System.Collections.Generic;
using SrpgCore.Data;

namespace SrpgCore.Tactics
{
    /// <summary>Tunable phase-boundary attrition. Values: PoisonDamage per poisoned unit, RegenHeal per regenerating unit, TickStatuses toggle, ApplyTerrain toggle.</summary>
    public sealed class PhaseTickRules
    {
        /// <summary>HP lost per poisoned unit per tick (GBA-flavored default).</summary>
        public int PoisonDamage { get; set; } = 3;
        /// <summary>HP gained per regenerating unit per tick.</summary>
        public int RegenHeal { get; set; } = 2;
        /// <summary>Decrement status durations and report expirations.</summary>
        public bool TickStatuses { get; set; } = true;
        /// <summary>Apply per-tile TerrainEffects (fort heal, marsh damage).</summary>
        public bool ApplyTerrain { get; set; } = true;
    }

    /// <summary>
    /// Phase-boundary upkeep for one team: terrain HP swing, then poison damage, then
    /// a status tick per unit, in deterministic coordinate order (X, then Y — the same
    /// order EnemyAi enumerates tiles). Hosts call Apply when a team's phase begins;
    /// per-unit report is (nid, total hpDelta, expired status names). Ticks never
    /// kill: a unit alive at tick start is left at 1 HP minimum; combat and
    /// objectives remain the only deaths. Skips dead units.
    /// Turn pipeline order: PhaseTick.Apply then SpawnDue (less or equal) then
    /// EvaluateObjectives then AI or input. Traps and events run as an explicit
    /// post-action stage after the commit.
    /// </summary>
    public static class PhaseTick
    {
        /// <summary>Runs phase-boundary upkeep for one team: terrain swing, poison, status ticks.</summary>
        /// <param name="state">Live chapter runtime (mutates HP and status durations).</param>
        /// <param name="team">Team name (player, enemy, ally, other).</param>
        /// <param name="rules">Toggles for terrain, poison damage, and status ticks.</param>
        /// <returns>Per-unit (nid, total hpDelta, expired status names) in tick order.</returns>
        public static List<(string nid, int hpDelta, string[] expiredStatuses)> Apply(
            ChapterState state, string team, PhaseTickRules? rules = null)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            if (team == null) throw new ArgumentNullException(nameof(team));
            rules ??= new PhaseTickRules();

            Affiliation side = ParseTeam(team);
            var members = new List<PlacedUnit>();
            foreach (PlacedUnit p in state.Field.Placed)
                if (p.Unit.Side == side && p.Unit.IsAlive)
                    members.Add(p);
            members.Sort((a, b) =>
            {
                int c = a.Position.X.CompareTo(b.Position.X);
                return c != 0 ? c : a.Position.Y.CompareTo(b.Position.Y);
            });

            var report = new List<(string nid, int hpDelta, string[] expiredStatuses)>();
            foreach (PlacedUnit p in members)
            {
                int start = p.Unit.CurrentHP;
                bool wasAlive = p.Unit.IsAlive;
                if (rules.ApplyTerrain)
                    TerrainEffects.Apply(p.Unit, state.Field.Board.GetTerrain(p.Position));
                if (wasAlive && rules.PoisonDamage > 0 && Statuses.Has(p.Unit, "poison"))
                    p.Unit.CurrentHP = Math.Max(1, p.Unit.CurrentHP - rules.PoisonDamage);
                if (wasAlive && rules.RegenHeal > 0 && Statuses.Has(p.Unit, "regen"))
                    p.Unit.CurrentHP = Math.Min(p.Unit.Stats.MaxHP, p.Unit.CurrentHP + rules.RegenHeal);
                List<string> expired = rules.TickStatuses
                    ? Statuses.Tick(p.Unit)
                    : new List<string>();
                if (p.Unit.CurrentHP <= 0)
                    p.Unit.CurrentHP = 1; // terrain swing also respects the no-kill floor
                report.Add((p.Unit.Nid, p.Unit.CurrentHP - start, expired.ToArray()));
            }
            return report;
        }

        private static Affiliation ParseTeam(string team)
        {
            switch (team.Trim().ToLowerInvariant())
            {
                case "player": return Affiliation.Player;
                case "enemy": return Affiliation.Enemy;
                case "ally": return Affiliation.Ally;
                case "other": return Affiliation.Other;
                default: throw new ArgumentException($"Unknown team '{team}'.", nameof(team));
            }
        }
    }
}
