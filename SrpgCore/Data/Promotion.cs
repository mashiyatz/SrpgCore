using System;
using System.Collections.Generic;
using SrpgCore.Grid;

namespace SrpgCore.Data
{
    /// <summary>
    /// Minimal class database: keyed UnitClass entries for promotion and movement lookup.
    /// Hosts own the data (markdown, JSON, editor DB); the core only resolves keys.
    /// </summary>
    public sealed class ClassDatabase
    {
        private readonly Dictionary<string, UnitClass> _classes = new Dictionary<string, UnitClass>(StringComparer.OrdinalIgnoreCase);

        /// <summary>Registers one class entry keyed by its Key, replacing any same-key entry.</summary>
        /// <param name="cls">Class entry to store.</param>
        public void Add(UnitClass cls)
        {
            if (cls == null) throw new ArgumentNullException(nameof(cls));
            _classes[cls.Key] = cls;
        }

        /// <summary>Looks up one class entry by key ignoring case.</summary>
        /// <param name="key">Class key to resolve.</param>
        /// <param name="cls">Found entry, null when absent.</param>
        /// <returns>True when the key resolves to an entry.</returns>
        public bool TryGet(string key, out UnitClass? cls) => _classes.TryGetValue(key, out cls);

        /// <summary>Resolves one class entry by key, failing fast on unknown keys.</summary>
        /// <param name="key">Class key to resolve.</param>
        /// <returns>The matching class entry.</returns>
        public UnitClass Get(string key)
        {
            if (!_classes.TryGetValue(key, out UnitClass? cls))
                throw new InvalidOperationException($"No class '{key}'.");
            return cls;
        }
    }

    /// <summary>
    /// Promotion: rekey ClassKey, adopt the new class movement, apply fixed gains.
    /// GBA per-class gains are host data; callers pass them as a Stats delta.
    /// </summary>
    public static class Promotion
    {
        /// <summary>Tests whether a unit class chain offers a resolvable promotion target.</summary>
        /// <param name="unit">Unit whose ClassKey starts the lookup.</param>
        /// <param name="db">Class database resolving both ends of the chain.</param>
        /// <returns>True when the current class promotes to a known class.</returns>
        public static bool CanPromote(Unit unit, ClassDatabase db)
        {
            if (unit == null || db == null) return false;
            if (!db.TryGet(unit.ClassKey, out UnitClass? cls) || cls == null) return false;
            return cls.PromotesTo != null && db.TryGet(cls.PromotesTo, out _);
        }

        /// <summary>Promotes a unit to a new class, adopting its movement and stat gains.</summary>
        /// <param name="unit">Unit to rekey and mutate in place.</param>
        /// <param name="newClassKey">Target class key resolved through the database.</param>
        /// <param name="db">Class database resolving the target entry.</param>
        /// <param name="gains">Host-supplied per-class stat delta, null for none.</param>
        public static void Apply(Unit unit, string newClassKey, ClassDatabase db, Stats? gains = null)
        {
            if (unit == null) throw new ArgumentNullException(nameof(unit));
            if (db == null) throw new ArgumentNullException(nameof(db));
            UnitClass next = db.Get(newClassKey);
            unit.ClassKey = next.Key;
            unit.MovementGroup = MovementGroups.For(next.MoveType);
            unit.MovePoints = next.Move;
            if (gains != null)
            {
                unit.Stats.MaxHP += gains.MaxHP;
                unit.Stats.Power += gains.Power;
                unit.Stats.Skill += gains.Skill;
                unit.Stats.Speed += gains.Speed;
                unit.Stats.Luck += gains.Luck;
                unit.Stats.Defense += gains.Defense;
                unit.Stats.Resistance += gains.Resistance;
                unit.CurrentHP = Math.Min(unit.Stats.MaxHP, unit.CurrentHP + Math.Max(0, gains.MaxHP));
            }
        }
    }
}
