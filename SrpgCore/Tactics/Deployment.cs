using System;
using System.Collections.Generic;
using SrpgCore.Data;
using SrpgCore.Regions;

namespace SrpgCore.Tactics
{
    /// <summary>
    /// Pre-battle roster picker: resolves requested unit nids against a chapter's
    /// deployments and reports authoring/selection errors without throwing, so the
    /// (future) deployment screen can surface them. Pure apart from no mutation at
    /// all — chosen entries alias the chapter's deployments.
    /// </summary>
    public static class DeploymentSelector
    {
        /// <summary>Resolves requested nids against chapter deployments without throwing.</summary>
        /// <param name="chapter">Chapter whose deployments are picked from.</param>
        /// <param name="nids">Requested unit nids in preference order.</param>
        /// <returns>(chosen deployments in request order, error strings).</returns>
        public static (List<Deployment> chosen, List<string> errors) ChooseDeployments(
            Chapter chapter, IEnumerable<string> nids)
        {
            if (chapter == null) throw new ArgumentNullException(nameof(chapter));
            if (nids == null) throw new ArgumentNullException(nameof(nids));

            var chosen = new List<Deployment>();
            var errors = new List<string>();
            var byNid = new Dictionary<string, Deployment>(StringComparer.Ordinal);
            foreach (Deployment d in chapter.Deployments)
                if (!byNid.ContainsKey(d.Unit.Nid))
                    byNid[d.Unit.Nid] = d;

            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (string nid in nids)
            {
                if (!seen.Add(nid)) continue;
                if (!byNid.TryGetValue(nid, out Deployment? d))
                {
                    errors.Add($"Unknown deployment nid '{nid}'.");
                    continue;
                }
                chosen.Add(d);
            }

            if (chapter.MaxDeploy > 0 && chosen.Count > chapter.MaxDeploy)
                errors.Add($"{chosen.Count} chosen exceed MaxDeploy {chapter.MaxDeploy}.");

            Region? formationRegion = null;
            if (!string.IsNullOrWhiteSpace(chapter.FormationRegion))
                foreach (Region r in chapter.Regions) if (r.Name == chapter.FormationRegion) { formationRegion = r; break; }
            if (formationRegion != null)
            {
                if (formationRegion.Kind != RegionKind.Formation)
                    errors.Add($"FormationRegion '{chapter.FormationRegion}' is not a formation region.");
                else foreach (Deployment d in chosen)
                    if (!formationRegion.Contains(d.Position))
                        errors.Add($"Deployment '{d.Unit.Nid}' at {d.Position} is outside formation region '{formationRegion.Name}'.");
            }
            else
            {
                bool hasFormation = false;
                foreach (Region r in chapter.Regions)
                    if (r.Kind == RegionKind.Formation) { hasFormation = true; break; }
                if (hasFormation)
                    foreach (Deployment d in chosen)
                    {
                        bool ok = false;
                        foreach (Region r in chapter.Regions)
                            if (r.Kind == RegionKind.Formation && r.Contains(d.Position)) { ok = true; break; }
                        if (!ok)
                            errors.Add($"Deployment '{d.Unit.Nid}' at {d.Position} is outside formation regions.");
                    }
            }
            var posSeen = new HashSet<string>();
            foreach (Deployment d in chosen)
                if (!posSeen.Add(d.Position.X + "," + d.Position.Y))
                    errors.Add($"Duplicate deployment tile {d.Position} (nid '{d.Unit.Nid}').");
            foreach (Deployment d in chosen)
                if (!chapter.Board.InBounds(d.Position))
                    errors.Add($"Deployment '{d.Unit.Nid}' at {d.Position} is off-board.");

            return (chosen, errors);
        }
    }
}
