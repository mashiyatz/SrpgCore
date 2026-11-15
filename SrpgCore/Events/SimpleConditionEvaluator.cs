using System;
using System.Collections.Generic;

namespace SrpgCore.Events
{
    /// <summary>
    /// Minimal condition language for event gating:
    ///   unit.nid == 'Eirika' &amp;&amp; turn &gt; 3
    /// Clauses joined by &amp;&amp;; each is `path op literal` with ops ==, !=, &gt;, &lt;, &gt;=, &lt;=.
    /// Paths resolve against nested string/object dictionaries (e.g. vars["unit"]["nid"]).
    /// Literals: 'quoted', numbers, true/false. Unknown paths evaluate to null (== null-safe false).
    /// Deliberately tiny: replace with a real scripting bridge only if chapters outgrow it.
    /// </summary>
    public static class SimpleConditionEvaluator
    {
        /// <summary>Evaluates a condition against trigger vars merged over chapter state vars (see EventVars) where empty always passes and clauses join with AND.</summary>
        /// <param name="condition">The clauses joined by AND with each shaped path op literal.</param>
        /// <param name="vars">The nested variables that dotted paths resolve against.</param>
        /// <returns>True when every clause passes or the condition is empty.</returns>
        public static bool Evaluate(string condition, IReadOnlyDictionary<string, object?> vars)
        {
            if (string.IsNullOrWhiteSpace(condition)) return true;
            if (condition.Length > EventLimits.MaxConditionLength)
                throw new FormatException($"Condition too long ({condition.Length} > {EventLimits.MaxConditionLength}).");
            List<string> clauses = SplitClauses(condition);
            if (clauses.Count > EventLimits.MaxConditionClauses)
                throw new FormatException($"Too many condition clauses ({clauses.Count} > {EventLimits.MaxConditionClauses}).");
            if (clauses.Count == 0)
                throw new FormatException("Condition has no clauses.");
            foreach (string clause in clauses)
                if (!EvaluateClause(clause, vars)) return false;
            return true;
        }

        // Splits on && only when outside single-quoted literals so quoted text may contain &&.
        // Rejects stray single & and empty clauses so malformed input fails loudly.
        private static List<string> SplitClauses(string condition)
        {
            var clauses = new List<string>();
            int start = 0;
            bool inQuote = false;
            bool sawSeparator = false;
            for (int i = 0; i < condition.Length; i++)
            {
                char c = condition[i];
                if (c == '\'')
                {
                    inQuote = !inQuote;
                    continue;
                }
                if (inQuote) continue;
                if (c == '&')
                {
                    if (i + 1 < condition.Length && condition[i + 1] == '&')
                    {
                        string clause = condition.Substring(start, i - start).Trim();
                        if (clause.Length == 0)
                            throw new FormatException("Empty condition clause before '&&'.");
                        clauses.Add(clause);
                        sawSeparator = true;
                        start = i + 2;
                        i++; // skip second '&'
                    }
                    else
                    {
                        throw new FormatException($"Stray '&' at position {i} (want '&&').");
                    }
                }
            }
            if (inQuote) throw new FormatException("Unterminated quoted literal in condition.");
            string tail = condition.Substring(start).Trim();
            if (tail.Length == 0)
            {
                if (sawSeparator) throw new FormatException("Empty condition clause after '&&'.");
                return clauses;
            }
            clauses.Add(tail);
            return clauses;
        }

        private static bool EvaluateClause(string clause, IReadOnlyDictionary<string, object?> vars)
        {
            if (string.IsNullOrWhiteSpace(clause))
                throw new FormatException("Empty condition clause.");
            if (!TrySplitOperator(clause, out string op, out string leftText, out string rightText))
                throw new FormatException($"Cannot parse condition clause '{clause}'.");
            if (leftText.Length == 0 || rightText.Length == 0)
                throw new FormatException($"Condition clause '{clause}' needs 'path op literal'.");
            object? left = Resolve(leftText, vars);
            object? right = ParseLiteral(rightText);
            int cmp = Compare(left, right);
            return op switch
            {
                "==" => cmp == 0,
                "!=" => cmp != 0,
                ">" => cmp > 0,
                "<" => cmp < 0,
                ">=" => cmp >= 0,
                "<=" => cmp <= 0,
                _ => false,
            };
        }

        /// <summary>
        /// Finds the operator split, ignoring occurrences inside 'quoted' literals
        /// (so `name == 'a==b'` splits on the real ==). Ties prefer the longer op.
        /// Single left-to-right scan; avoids O(n^2) quote recounting.
        /// </summary>
        private static bool TrySplitOperator(string clause, out string op, out string left, out string right)
        {
            int bestIndex = -1;
            string bestOp = "";
            bool inQuote = false;
            for (int i = 0; i < clause.Length; i++)
            {
                char c = clause[i];
                if (c == '\'') { inQuote = !inQuote; continue; }
                if (inQuote) continue;
                // Prefer longest match at the earliest position: check 2-char ops first.
                if (i + 1 < clause.Length)
                {
                    string two = clause.Substring(i, 2);
                    if (two == "==" || two == "!=" || two == ">=" || two == "<=")
                    {
                        bestIndex = i;
                        bestOp = two;
                        break; // earliest position wins; two-char is longest here
                    }
                }
                if (c == '>' || c == '<')
                {
                    bestIndex = i;
                    bestOp = c.ToString();
                    break; // earliest single-char operator outside quotes
                }
                if (c == '=' || c == '!')
                {
                    // Lone = or ! without its pair is malformed; let caller report it.
                    // Record position so error context is stable, but do not accept.
                    continue;
                }
            }
            if (bestIndex < 0)
            {
                op = ""; left = ""; right = "";
                return false;
            }
            op = bestOp;
            left = clause.Substring(0, bestIndex).Trim();
            right = clause.Substring(bestIndex + bestOp.Length).Trim();
            return true;
        }

        private static object? Resolve(string path, IReadOnlyDictionary<string, object?> vars)
        {
            object? current = vars;
            foreach (string part in path.Split('.'))
            {
                if (current is IReadOnlyDictionary<string, object?> dict && dict.TryGetValue(part, out var next))
                    current = next;
                else if (current is IDictionary<string, object?> mut && mut.TryGetValue(part, out var nextMut))
                    current = nextMut;
                else
                    return null;
            }
            return current;
        }

        private static object? ParseLiteral(string token)
        {
            if (token.Length >= 2 && token[0] == '\'' && token[token.Length - 1] == '\'')
                return token.Substring(1, token.Length - 2);
            if (string.Equals(token, "true", StringComparison.OrdinalIgnoreCase)) return true;
            if (string.Equals(token, "false", StringComparison.OrdinalIgnoreCase)) return false;
            if (long.TryParse(token, out long n)) return n;
            if (double.TryParse(token, System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out double d)) return d;
            return token; // bare word: compare as string
        }

        private static int Compare(object? left, object? right)
        {
            if (left == null || right == null)
                return left == null && right == null ? 0 : -1;
            if (IsNumber(left) && IsNumber(right))
            {
                double a = Convert.ToDouble(left, System.Globalization.CultureInfo.InvariantCulture);
                double b = Convert.ToDouble(right, System.Globalization.CultureInfo.InvariantCulture);
                return a.CompareTo(b);
            }
            return string.Compare(Convert.ToString(left), Convert.ToString(right), StringComparison.Ordinal);
        }

        private static bool IsNumber(object o) =>
            o is sbyte || o is byte || o is short || o is ushort || o is int || o is uint ||
            o is long || o is ulong || o is float || o is double || o is decimal;
    }
}
