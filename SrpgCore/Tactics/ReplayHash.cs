using System;
using System.Collections.Generic;
using SrpgCore.Tactics;

namespace SrpgCore.Tactics
{
    /// <summary>Deterministic hash of chapter runtime for replay verification.</summary>
    public static class ReplayHash
    {
        /// <summary>Computes a deterministic hash of placements, HP, vars, RNG states, and skill catalog.</summary>
        /// <param name="state">Chapter runtime to hash.</param>
        /// <returns>Hash string.</returns>
        public static string Compute(ChapterState state)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            unchecked
            {
                int h = 17;
                h = h * 31 + state.Phases.TurnNumber;
                h = h * 31 + state.Phases.CurrentTeamIndex;
                h = h * 31 + (int)state.CombatRng.GetState();
                h = h * 31 + (int)state.GrowthRng.GetState();
                h = h * 31 + (int)state.ProcRng.GetState();
                h = h * 31 + state.SkillDefs.Count;
                var skillKeys = new List<string>(state.SkillDefs.Keys);
                skillKeys.Sort(StringComparer.Ordinal);
                foreach (string key in skillKeys)
                {
                    var def = state.SkillDefs[key];
                    h = h * 31 + StableHash(key);
                    h = h * 31 + (int)def.Hook;
                    h = h * 31 + def.DamageBonus;
                    h = h * 31 + def.HitBonus;
                    h = h * 31 + def.CritBonus;
                    h = h * 31 + def.DamageTakenPenalty;
                    h = h * 31 + def.ProcChance;
                }
                var ordered = new List<PlacedUnit>(state.Field.Placed);
                ordered.Sort((a, b) => string.CompareOrdinal(a.Unit.Nid, b.Unit.Nid));
                foreach (var p in ordered)
                {
                    h = h * 31 + StableHash(p.Unit.Nid);
                    h = h * 31 + p.Position.X;
                    h = h * 31 + p.Position.Y;
                    h = h * 31 + p.Unit.CurrentHP;
                }
                var varKeys = new List<string>(state.Vars.Keys);
                varKeys.Sort(StringComparer.Ordinal);
                foreach (string key in varKeys)
                {
                    h = h * 31 + StableHash(key);
                    h = h * 31 + StableHash(ValueText(state.Vars[key]));
                }
                return h.ToString("X8");
            }
        }

        private static string ValueText(object? value)
        {
            if (value == null) return "null";
            if (value is bool b) return b ? "true" : "false";
            if (value is string s) return s;
            if (value is IFormattable f) return f.ToString(null, System.Globalization.CultureInfo.InvariantCulture);
            return value.ToString() ?? "null";
        }

        private static int StableHash(string s)
        {
            unchecked { int h = 5381; foreach (char c in s) h = h * 33 + c; return h; }
        }
    }
}
