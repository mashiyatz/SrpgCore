# API: SrpgCore.Random

> Generated from `../SrpgCore` sources by `scripts/build-api.ts` — do not edit by hand.

Guide: [overview](overview.md).

## SeededRng

Deterministic RNG (xorshift32). One stream per battle/level, constructed from a saved seed, so forecasts, AI, replays and tests all share one reproducible sequence. Not for security use.

### SeededRng constructor

Creates a deterministic stream from the given seed.

```csharp
public SeededRng(uint seed)
```

| Parameter | Type | Description |
| --- | --- | --- |
| seed | `uint` | Starting seed, with zero replaced by a fixed nonzero fallback. |

### SeededRng.NextUInt

Advances the xorshift32 stream and returns the next word.

```csharp
public uint NextUInt()
```

Returns: `uint` — Next unsigned value in the deterministic sequence.

### SeededRng.GetState

Snapshot the stream for saves/turnwheel; restore with SetState.

```csharp
public uint GetState()
```

Returns: `uint` — Current internal stream state.

### SeededRng.SetState

Restores a snapshot taken by GetState.

```csharp
public void SetState(uint state)
```

| Parameter | Type | Description |
| --- | --- | --- |
| state | `uint` | Stream state to resume, with zero replaced by a fixed fallback. |

### SeededRng.CreateStream

Independent sub-stream (combat/growth/other split): combat rolls never shift growth rolls. Same (baseSeed, streamIndex) always yields the same stream.

```csharp
public static SeededRng CreateStream(uint baseSeed, uint streamIndex)
```

| Parameter | Type | Description |
| --- | --- | --- |
| baseSeed | `uint` | Shared base seed for the level or battle. |
| streamIndex | `uint` | Per-concern index splitting combat from growth and other streams. |

Returns: `SeededRng` — Independent deterministic stream for one concern.

### SeededRng.Next

Returns the next bounded integer from the stream.

```csharp
public int Next(int exclusiveUpperBound)
```

| Parameter | Type | Description |
| --- | --- | --- |
| exclusiveUpperBound | `int` | Exclusive upper bound, must be positive. |

Returns: `int` — Integer in [0, exclusiveUpperBound).

### SeededRng.NextDouble

Returns the next uniform double from the stream.

```csharp
public double NextDouble()
```

Returns: `double` — Double in [0, 1).

### SeededRng.RollHit1RN

Classic single-roll hit check against a 0-100 rate.

```csharp
public bool RollHit1RN(int hitRate)
```

| Parameter | Type | Description |
| --- | --- | --- |
| hitRate | `int` | Hit rate from 0 to 100 with absolute endpoints. |

Returns: `bool` — True when the single 0-99 roll falls below the rate.

### SeededRng.RollHit2RN

GBA-style 2RN: averages two 0-99 rolls, which skews outcomes toward the displayed rate (high hit feels reliable, low hit feels hopeless). This is the Fire Emblem feel.

```csharp
public bool RollHit2RN(int hitRate)
```

| Parameter | Type | Description |
| --- | --- | --- |
| hitRate | `int` | Hit rate from 0 to 100 with absolute endpoints. |

Returns: `bool` — True when the mean of two 0-99 rolls falls below the rate.
