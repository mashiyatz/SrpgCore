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
        private static readonly string[] Ops = { "==", "!=", ">=", "<=", ">", "<" };

        /// <summary>Evaluates a condition against trigger vars where empty always passes and clauses join with AND.</summary>
        /// <param name="condition">The clauses joined by AND with each shaped path op literal.</param>
        /// <param name="vars">The nested trigger variables that dotted paths resolve against.</param>
        /// <returns>True when every clause passes or the condition is empty.</returns>
        public static bool Evaluate(string condition, IReadOnlyDictionary<string, object?> vars)
        {
            if (string.IsNullOrWhiteSpace(condition)) return true;
            foreach (string clause in condition.Split(new[] { "&&" }, StringSplitOptions.RemoveEmptyEntries))
                if (!EvaluateClause(clause.Trim(), vars)) return false;
            return true;
        }

        private static bool EvaluateClause(string clause, IReadOnlyDictionary<string, object?> vars)
        {
            if (!TrySplitOperator(clause, out string op, out string leftText, out string rightText))
                throw new FormatException($"Cannot parse condition clause '{clause}'.");
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
        /// </summary>
        private static bool TrySplitOperator(string clause, out string op, out string left, out string right)
        {
            int bestIndex = int.MaxValue;
            string bestOp = "";
            foreach (string candidate in Ops)
            {
                int i = IndexOfOutsideQuotes(clause, candidate);
                if (i < 0) continue;
                if (i < bestIndex || (i == bestIndex && candidate.Length > bestOp.Length))
                {
                    bestIndex = i;
                    bestOp = candidate;
                }
            }
            if (bestIndex == int.MaxValue)
            {
                op = ""; left = ""; right = "";
                return false;
            }
            op = bestOp;
            left = clause.Substring(0, bestIndex).Trim();
            right = clause.Substring(bestIndex + bestOp.Length).Trim();
            return true;
        }

        private static int IndexOfOutsideQuotes(string text, string value)
        {
            int from = 0;
            while (true)
            {
                int i = text.IndexOf(value, from, StringComparison.Ordinal);
                if (i < 0) return -1;
                int quotes = 0;
                for (int k = 0; k < i; k++) if (text[k] == '\'') quotes++;
                if (quotes % 2 == 0) return i;
                from = i + 1;
            }
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
