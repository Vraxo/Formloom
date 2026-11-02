using System.Text;

namespace Formloom.Core.Randomness;

public sealed class Pcg32
{
    private const ulong Multiplier = 6364136223846793005;
    private readonly ulong increment;
    private ulong state;

    public Pcg32(ulong seed, ulong sequence = 54)
    {
        increment = unchecked((sequence << 1) | 1);
        NextUInt32();
        state = unchecked(state + seed);
        NextUInt32();
    }

    public static Pcg32 ForStream(ulong seed, string name)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        // FNV-1a over UTF-8 provides a stable, process-independent stream identifier.
        var hash = 14695981039346656037UL;
        foreach (var value in Encoding.UTF8.GetBytes(name))
        {
            hash = unchecked((hash ^ value) * 1099511628211UL);
        }

        return new Pcg32(seed ^ hash, hash);
    }

    public uint NextUInt32()
    {
        var previous = state;
        state = unchecked((previous * Multiplier) + increment);
        var shifted = (uint)(((previous >> 18) ^ previous) >> 27);
        var rotation = (int)(previous >> 59);
        return (shifted >> rotation) | (shifted << ((-rotation) & 31));
    }

    public double NextDouble() => NextUInt32() / 4294967296.0;

    public int NextInt(int minimum, int maximum)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(maximum, minimum);
        var bound = (uint)((long)maximum - minimum);
        var threshold = unchecked(0U - bound) % bound;
        uint value;
        do
        {
            value = NextUInt32();
        }
        while (value < threshold);

        return (int)(minimum + (long)(value % bound));
    }

    public bool Chance(double probability)
    {
        if (!double.IsFinite(probability) || probability is < 0 or > 1)
        {
            throw new ArgumentOutOfRangeException(nameof(probability));
        }

        return NextDouble() < probability;
    }
}
