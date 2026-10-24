// A stream of random numbers that a seed fully decides: the same seed and stream give the same
// numbers in every build, on every PC. It is PCG32 (pcg-random.org), written here instead of using
// System.Random, whose algorithm .NET doesn't promise to keep, or Unity.Mathematics.Random, a struct
// that silently forks the stream when it is copied into a variable.
// Gameplay asks RunRandomService for one; never create them directly for gameplay.
public sealed class SeededRandom
{
    private const ulong Multiplier = 6364136223846793005UL;

    // 1 / 2^24: a float holds 24 bits exactly, so Value() never rounds up to 1.
    private const float FloatUnit = 1f / 16777216f;

    private readonly ulong _increment;
    private ulong _state;

    /// <param name="seed">Where the stream starts.</param>
    /// <param name="stream">Which of PCG's independent streams to walk. Same seed, other stream: other numbers.</param>
    public SeededRandom(ulong seed, ulong stream)
    {
        unchecked
        {
            // PCG's reference seeding (pcg32_srandom_r). The increment must be odd.
            _increment = (stream << 1) | 1UL;
            _state = 0UL;
            NextUInt();
            _state += seed;
            NextUInt();
        }
    }

    /// <summary>
    /// The next 32 random bits. Every other method is built on this one.
    /// </summary>
    public uint NextUInt()
    {
        unchecked
        {
            ulong old = _state;
            _state = old * Multiplier + _increment;

            uint xorShifted = (uint)(((old >> 18) ^ old) >> 27);
            int rotation = (int)(old >> 59);
            return (xorShifted >> rotation) | (xorShifted << (-rotation & 31));
        }
    }

    /// <summary>
    /// A whole number from min to max - 1, like UnityEngine.Random.Range for ints.
    /// Returns min, without using a number, when there is nothing to choose.
    /// </summary>
    public int Range(int min, int maxExclusive)
    {
        if (maxExclusive <= min)
            return min;

        uint bound = (uint)((long)maxExclusive - min);

        // Drops the few values that would make the low numbers a bit likelier than the rest (pcg32_boundedrand_r).
        uint threshold = unchecked(0u - bound) % bound;
        uint roll;
        do
        {
            roll = NextUInt();
        } while (roll < threshold);

        return (int)((long)min + roll % bound);
    }

    /// <summary>
    /// A number from 0 (included) to 1 (never reached).
    /// </summary>
    public float Value()
    {
        return (NextUInt() >> 8) * FloatUnit;
    }

    /// <summary>
    /// A number from min (included) to max (never reached), like UnityEngine.Random.Range for floats.
    /// </summary>
    public float Range(float min, float max)
    {
        return min + (max - min) * Value();
    }

    /// <summary>
    /// True with the given probability, from 0 (never) to 1 (always). Always uses one number.
    /// </summary>
    public bool Chance(float probability)
    {
        return Value() < probability;
    }

    /// <summary>
    /// Scrambles a number so that close inputs (seed 1 and 2, stream 3 and 4) give unrelated outputs.
    /// It is SplitMix64's finalizer. RunRandomService uses it to give every stream its own starting point.
    /// </summary>
    public static ulong Mix(ulong value)
    {
        unchecked
        {
            value += 0x9E3779B97F4A7C15UL;
            value = (value ^ (value >> 30)) * 0xBF58476D1CE4E5B9UL;
            value = (value ^ (value >> 27)) * 0x94D049BB133111EBUL;
            return value ^ (value >> 31);
        }
    }
}
