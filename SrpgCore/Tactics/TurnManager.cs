using System;
using System.Collections.Generic;

namespace SrpgCore.Tactics
{
    /// <summary>
    /// Phase cycler: teams act in list order, teams with no live
    /// units are skipped (player/index 0 never skipped), TurnNumber advances each time
    /// play returns to the first team. Win/loss is script-driven: events call SetResult
    /// (via win/lose commands) and the host polls BattleResult at event end.
    /// </summary>
    public sealed class TurnManager
    {
        /// <summary>Teams in phase order.</summary>
        public IReadOnlyList<string> Teams { get; }
        /// <summary>Index of the currently acting team.</summary>
        public int CurrentTeamIndex { get; private set; }
        /// <summary>Currently acting team name.</summary>
        public string CurrentTeam => Teams[CurrentTeamIndex];
        /// <summary>Round number (starts at 1, advances on return to the first team).</summary>
        public int TurnNumber { get; private set; } = 1;

        /// <summary>null = ongoing, true = victory, false = defeat.</summary>
        public bool? BattleResult { get; private set; }

        /// <summary>Creates a phase cycler over the given team list.</summary>
        /// <param name="teams">Team names in phase order (first entry is never skipped).</param>
        public TurnManager(IReadOnlyList<string> teams)
        {
            Teams = teams ?? throw new ArgumentNullException(nameof(teams));
            if (teams.Count == 0) throw new ArgumentException("Need at least one team.", nameof(teams));
        }

        /// <summary>Records the scripted win or loss flag.</summary>
        /// <param name="won">True for victory, false for defeat.</param>
        public void SetResult(bool won) => BattleResult = won;

        /// <summary>Restore saved progress (saves/turnwheel). Hosts only; Verify covers it.</summary>
        /// <param name="teamIndex">Saved acting-team index.</param>
        /// <param name="turnNumber">Saved turn number.</param>
        /// <param name="battleResult">Saved scripted result (null while ongoing).</param>
        public void Restore(int teamIndex, int turnNumber, bool? battleResult)
        {
            if (teamIndex < 0 || teamIndex >= Teams.Count) throw new ArgumentOutOfRangeException(nameof(teamIndex));
            if (turnNumber < 1) throw new ArgumentOutOfRangeException(nameof(turnNumber));
            CurrentTeamIndex = teamIndex;
            TurnNumber = turnNumber;
            BattleResult = battleResult;
        }

        /// <summary>Advances to the next live team, skipping empty teams except the first.</summary>
        /// <param name="teamHasLiveUnits">Tests whether a team has live units.</param>
        /// <returns>New current team and whether a fresh round began (turn advanced).</returns>
        public (string team, bool roundAdvanced) AdvancePhase(Func<string, bool> teamHasLiveUnits)
        {
            if (teamHasLiveUnits == null) throw new ArgumentNullException(nameof(teamHasLiveUnits));
            int next = CurrentTeamIndex;
            for (int i = 1; i <= Teams.Count; i++)
            {
                int candidate = (CurrentTeamIndex + i) % Teams.Count;
                if (candidate == 0 || teamHasLiveUnits(Teams[candidate]))
                {
                    next = candidate;
                    break;
                }
            }
            CurrentTeamIndex = next;
            bool roundAdvanced = next == 0;
            if (roundAdvanced) TurnNumber++;
            return (CurrentTeam, roundAdvanced);
        }
    }
}
