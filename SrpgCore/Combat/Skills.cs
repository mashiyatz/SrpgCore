using System;
using System.Collections.Generic;

namespace SrpgCore.Combat
{
    /// <summary>When a skill modifies a forecast (attack or defense side).</summary>
    public enum SkillHook
    {
        /// <summary>Applies when the skilled unit attacks.</summary>
        OnAttack,
        /// <summary>Applies when the skilled unit defends.</summary>
        OnDefense,
    }

    /// <summary>Flat forecast modifiers for one skill id. Proc skills use ProcChance (0-100, 1RN).</summary>
    public sealed class SkillDef
    {
        /// <summary>Unique skill id matched against Unit.Skills entries.</summary>
        public string Id { get; }
        /// <summary>Side of the exchange this skill modifies.</summary>
        public SkillHook Hook { get; set; } = SkillHook.OnAttack;
        /// <summary>Flat damage added to forecasts on this hook.</summary>
        public int DamageBonus { get; set; }
        /// <summary>Flat hit added to forecasts on this hook.</summary>
        public int HitBonus { get; set; }
        /// <summary>Flat crit added to forecasts on this hook.</summary>
        public int CritBonus { get; set; }
        /// <summary>Flat damage subtracted from incoming forecasts on defense.</summary>
        public int DamageTakenPenalty { get; set; }
        private int _procChance;
        /// <summary>Proc chance from 0 to 100 rolled 1RN, where 0 always applies. Values outside 0-100 throw.</summary>
        public int ProcChance
        {
            get => _procChance;
            set
            {
                if (value < 0 || value > 100) throw new ArgumentOutOfRangeException(nameof(ProcChance), "ProcChance must be 0-100.");
                _procChance = value;
            }
        }

        /// <summary>Creates a skill definition with the given id.</summary>
        /// <param name="id">Unique skill id used for registration and lookup.</param>
        public SkillDef(string id)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
        }

        /// <summary>Creates an independent copy of this skill definition.</summary>
        /// <returns>A new definition with the same hook bonuses and proc chance.</returns>
        public SkillDef Clone() => new SkillDef(Id)
        {
            Hook = Hook,
            DamageBonus = DamageBonus,
            HitBonus = HitBonus,
            CritBonus = CritBonus,
            DamageTakenPenalty = DamageTakenPenalty,
            ProcChance = ProcChance,
        };
    }

    /// <summary>
    /// Hook point for abilities (Adept/Brave/Sol-adjacent flat mods, armor-slayer tags, ...).
    /// Hosts register SkillDefs; ApplyToForecast adds attacker OnAttack mods and defender
    /// OnDefense mods to a forecast copy. Proc rolls (if any) consume the passed RNG...
    /// pass null to preview without procs. Pure apart from optional proc RNG.
    /// Global mutable catalog: hosts must snapshot via TryGetAll/SaveSystem and ChapterState.SkillDefs
    /// for deterministic saves/replays; do not mutate mid-chapter without snapshotting.
    /// </summary>
    public static class SkillCatalog
    {
        private static readonly Dictionary<string, SkillDef> Defs =
            new Dictionary<string, SkillDef>(StringComparer.OrdinalIgnoreCase);

        /// <summary>Registers a skill definition, replacing any entry with the same id.</summary>
        /// <param name="def">The skill definition to store by id.</param>
        public static void Register(SkillDef def)
        {
            if (def == null) throw new ArgumentNullException(nameof(def));
            Defs[def.Id] = def;
        }

        /// <summary>Clears all registered skill definitions.</summary>
        public static void Clear() => Defs.Clear();

        /// <summary>Looks up a registered skill definition by id.</summary>
        /// <param name="id">The skill id to find.</param>
        /// <param name="def">The matching definition, or null when unregistered.</param>
        /// <returns>True when a definition was found.</returns>
        public static bool TryGet(string id, out SkillDef? def) => Defs.TryGetValue(id, out def);

        /// <summary>Returns a deep copy of all registered skill definitions for save snapshot.</summary>
        /// <returns>Copy of the id to definition map with cloned definitions.</returns>
        public static Dictionary<string, SkillDef> TryGetAll()
        {
            var copy = new Dictionary<string, SkillDef>(StringComparer.OrdinalIgnoreCase);
            foreach (var kv in Defs) copy[kv.Key] = kv.Value.Clone();
            return copy;
        }

        /// <summary>Returns a deep snapshot copy for deterministic save hashing.</summary>
        /// <returns>Read-only view of the catalog copy with cloned definitions.</returns>
        public static IReadOnlyDictionary<string, SkillDef> Snapshot() => TryGetAll();

        /// <summary>Returns an adjusted forecast copy with attacker and defender skill mods.</summary>
        /// <param name="baseForecast">The input forecast, never mutated.</param>
        /// <param name="attackerSkills">Attacker skill ids applied through the OnAttack hook.</param>
        /// <param name="defenderSkills">Defender skill ids applied through the OnDefense hook.</param>
        /// <param name="procRng">Seeded stream for proc rolls, or null to preview without procs.</param>
        /// <returns>A new forecast with flat damage, hit, and crit mods applied.</returns>
        public static CombatForecast ApplyToForecast(
            CombatForecast baseForecast,
            IEnumerable<string> attackerSkills,
            IEnumerable<string> defenderSkills,
            Random.SeededRng? procRng = null) => ApplyToForecast(baseForecast, attackerSkills, defenderSkills, procRng, null);

        /// <summary>Returns an adjusted forecast copy with attacker and defender skill mods using an explicit catalog.</summary>
        /// <param name="baseForecast">The input forecast, never mutated.</param>
        /// <param name="attackerSkills">Attacker skill ids applied through the OnAttack hook.</param>
        /// <param name="defenderSkills">Defender skill ids applied through the OnDefense hook.</param>
        /// <param name="procRng">Seeded stream for proc rolls, or null to preview without procs.</param>
        /// <param name="catalog">Explicit skill catalog, or null to use the global static catalog.</param>
        /// <returns>A new forecast with flat damage, hit, and crit mods applied.</returns>
        public static CombatForecast ApplyToForecast(
            CombatForecast baseForecast,
            IEnumerable<string> attackerSkills,
            IEnumerable<string> defenderSkills,
            Random.SeededRng? procRng,
            IReadOnlyDictionary<string, SkillDef>? catalog)
        {
            if (baseForecast == null) throw new ArgumentNullException(nameof(baseForecast));
            var fc = new CombatForecast
            {
                Damage = baseForecast.Damage,
                Hit = baseForecast.Hit,
                Crit = baseForecast.Crit,
                AttackCount = baseForecast.AttackCount,
                TriangleAdvantage = baseForecast.TriangleAdvantage,
                IsEffective = baseForecast.IsEffective,
            };
            var src = catalog ?? Defs;
            if (attackerSkills != null)
                foreach (string id in attackerSkills)
                    if (src.TryGetValue(id, out SkillDef? d) && d.Hook == SkillHook.OnAttack && ProcFires(d, procRng))
                    {
                        fc.Damage = Math.Max(0, fc.Damage + d.DamageBonus);
                        fc.Hit = Math.Max(0, Math.Min(100, fc.Hit + d.HitBonus));
                        fc.Crit = Math.Max(0, Math.Min(100, fc.Crit + d.CritBonus));
                    }
            if (defenderSkills != null)
                foreach (string id in defenderSkills)
                    if (src.TryGetValue(id, out SkillDef? d) && d.Hook == SkillHook.OnDefense && ProcFires(d, procRng))
                    {
                        fc.Damage = Math.Max(0, fc.Damage - d.DamageTakenPenalty);
                    }
            return fc;
        }

        private static bool ProcFires(SkillDef def, Random.SeededRng? rng)
        {
            if (def.ProcChance <= 0) return true;
            if (def.ProcChance >= 100) return rng == null || rng.RollHit1RN(100);
            return rng != null && rng.RollHit1RN(def.ProcChance);
        }
    }
}
