using System;

namespace SrpgCore.Tactics
{
    /// <summary>
    /// Dance (FE dancer role): a skilled unit refreshes an adjacent ally's turn.
    /// Host-tracked: the core holds no acted flags, so hosts own exhaustion state —
    /// this class is the shared legality check (Rescue/TalkHelper shape) plus the
    /// `dance;dancer;target` verb the headless runner validates. On success hosts
    /// clear their acted flag for the target and mark the dancer acted.
    /// Dancer status travels as skill id "dance" (markdown `skill:dance`; matched
    /// case-insensitively, no SkillCatalog registration needed since dance never
    /// touches a forecast).
    /// </summary>
    public static class Dance
    {
        /// <summary>Skill id granting the dancer role.</summary>
        public const string DanceSkillId = "dance";

        /// <summary>Reports whether a unit carries the dance skill (case-insensitive).</summary>
        /// <param name="dancer">Unit to test (null reads as no dance).</param>
        /// <returns>True when the unit lists the dance skill.</returns>
        public static bool IsDancer(Data.Unit dancer)
        {
            if (dancer == null) return false;
            foreach (string s in dancer.Skills)
                if (string.Equals(s, DanceSkillId, StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        /// <summary>Reports whether a dancer may refresh an adjacent ally this turn.</summary>
        /// <param name="dancer">Dancing unit.</param>
        /// <param name="target">Ally to refresh.</param>
        /// <returns>True when skill, liveness, side, and adjacency all allow it.</returns>
        public static bool CanDance(PlacedUnit dancer, PlacedUnit target) =>
            DanceBlocker(dancer, target) == null;

        /// <summary>Throwing validator for the dance verb (fail fast, like Rescue.Grab).</summary>
        /// <param name="dancer">Dancing unit.</param>
        /// <param name="target">Ally to refresh.</param>
        public static void CheckDance(PlacedUnit dancer, PlacedUnit target)
        {
            if (dancer == null) throw new ArgumentNullException(nameof(dancer));
            if (target == null) throw new ArgumentNullException(nameof(target));
            string? blocker = DanceBlocker(dancer, target);
            if (blocker != null)
                throw new InvalidOperationException($"'{dancer.Unit.Nid}' cannot dance for '{target.Unit.Nid}' ({blocker}).");
        }

        private static string? DanceBlocker(PlacedUnit dancer, PlacedUnit target)
        {
            if (dancer == null || target == null) return "missing unit";
            if (ReferenceEquals(dancer, target) || dancer.Unit.Nid == target.Unit.Nid) return "cannot dance for self";
            if (!dancer.Unit.IsAlive || !target.Unit.IsAlive) return "both units must be alive";
            if (!IsDancer(dancer.Unit)) return $"dancer needs the '{DanceSkillId}' skill";
            if (Battlefield.AreHostile(dancer.Unit.Side, target.Unit.Side)) return "target must be an ally";
            if (dancer.Position.ManhattanDistance(target.Position) != 1) return "must stand adjacent";
            return null;
        }
    }
}
