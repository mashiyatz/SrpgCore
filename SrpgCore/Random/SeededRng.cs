using System;

namespace SrpgCore.Random
{
    /// <summary>
    /// Deterministic RNG (xorshift32). One stream per battle/level, constructed from a saved
    /// seed, so forecasts, AI, replays and tests all share one reproducible sequence.
    /// Not for security use.
    /// </summary>
    public sealed class SeededRng
    {
        private uint _state;

        /// <summary>Creates a deterministic stream from the given seed.</summary>
        /// <param name="seed">Starting seed, with zero replaced by a fixed nonzero fallback.</param>
        public SeededRng(uint seed)
        {
            _state = seed != 0 ? seed : 0x9E3779B9u;
        }

        /// <summary>Advances the xorshift32 stream and returns the next word.</summary>
        /// <returns>Next unsigned value in the deterministic sequence.</returns>
        public uint NextUInt()
        {
            uint x = _state;
            x ^= x << 13;
            x ^= x >> 17;
            x ^= x << 5;
            _state = x;
            return x;
        }

        /// <summary>Snapshot the stream for saves/turnwheel; restore with <see cref="SetState"/>.</summary>
        /// <returns>Current internal stream state.</returns>
        public uint GetState() => _state;

        /// <summary>Restores a snapshot taken by <see cref="GetState"/>.</summary>
        /// <param name="state">Stream state to resume, with zero replaced by a fixed fallback.</param>
        public void SetState(uint state) => _state = state != 0 ? state : 0x9E3779B9u;

        /// <summary>
        /// Independent sub-stream (combat/growth/other split): combat rolls never
        /// shift growth rolls. Same (baseSeed, streamIndex) always yields the same stream.
        /// </summary>
        /// <param name="baseSeed">Shared base seed for the level or battle.</param>
        /// <param name="streamIndex">Per-concern index splitting combat from growth and other streams.</param>
        /// <returns>Independent deterministic stream for one concern.</returns>
        public static SeededRng CreateStream(uint baseSeed, uint streamIndex) =>
            new SeededRng(baseSeed ^ (streamIndex * 0x9E3779B9u + 1));

        /// <summary>Returns the next bounded integer from the stream.</summary>
        /// <param name="exclusiveUpperBound">Exclusive upper bound, must be positive.</param>
        /// <returns>Integer in [0, exclusiveUpperBound).</returns>
        public int Next(int exclusiveUpperBound)
        {
            if (exclusiveUpperBound <= 0) throw new ArgumentOutOfRangeException(nameof(exclusiveUpperBound));
            return (int)(NextUInt() % (uint)exclusiveUpperBound);
        }

        /// <summary>Returns the next uniform double from the stream.</summary>
        /// <returns>Double in [0, 1).</returns>
        public double NextDouble() => NextUInt() / 4294967296.0;

        /// <summary>Classic single-roll hit check against a 0-100 rate.</summary>
        /// <param name="hitRate">Hit rate from 0 to 100 with absolute endpoints.</param>
        /// <returns>True when the single 0-99 roll falls below the rate.</returns>
        public bool RollHit1RN(int hitRate)
        {
            if (hitRate <= 0) return false;
            if (hitRate >= 100) return true;
            return Next(100) < hitRate;
        }

        /// <summary>
        /// GBA-style 2RN: averages two 0-99 rolls, which skews outcomes toward the displayed
        /// rate (high hit feels reliable, low hit feels hopeless). This is the Fire Emblem feel.
        /// </summary>
        /// <param name="hitRate">Hit rate from 0 to 100 with absolute endpoints.</param>
        /// <returns>True when the mean of two 0-99 rolls falls below the rate.</returns>
        public bool RollHit2RN(int hitRate)
        {
            if (hitRate <= 0) return false;
            if (hitRate >= 100) return true;
            return (Next(100) + Next(100)) / 2 < hitRate;
        }
    }
}
