using System;

namespace SrpgCore.Events
{
    /// <summary>
    /// Central adversarial bounds for event content. Loaders, parsers, and the
    /// evaluator enforce these so hostile chapters fail fast with a clear error
    /// instead of causing hangs, blowups, or quadratic parsing.
    /// </summary>
    public static class EventLimits
    {
        /// <summary>Maximum characters in one raw command line.</summary>
        public const int MaxCommandLength = 1024;
        /// <summary>Maximum characters in one event condition expression.</summary>
        public const int MaxConditionLength = 2000;
        /// <summary>Maximum && clauses in one condition.</summary>
        public const int MaxConditionClauses = 64;
        /// <summary>Maximum commands in one event.</summary>
        public const int MaxCommandsPerEvent = 256;
        /// <summary>Maximum events in one chapter EVENTS section.</summary>
        public const int MaxEventsPerChapter = 512;
        /// <summary>Maximum characters in one verb token.</summary>
        public const int MaxVerbLength = 64;
    }
}
