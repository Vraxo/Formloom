using Formloom.Core.Core;

namespace Formloom.Core.Geometry;

/// <summary>Produces a repeatable signed offset from an independent named random stream.</summary>
public static class SeededScalarVariation
{
    public static float SampleOffset(float maximumAbsoluteOffset, float amount, ulong seed, string stream)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(stream);
        if (!float.IsFinite(maximumAbsoluteOffset) || maximumAbsoluteOffset < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumAbsoluteOffset), "The maximum offset must be finite and non-negative.");
        }

        if (!float.IsFinite(amount) || amount is < 0 or > 1)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "Variation amount must be in the range 0..1.");
        }

        var random = Pcg32.ForStream(seed, stream);
        var offset = (((float)random.NextDouble() * 2) - 1) * maximumAbsoluteOffset * amount;
        return float.IsFinite(offset)
            ? offset
            : throw new ArgumentOutOfRangeException(nameof(maximumAbsoluteOffset), "The sampled offset must be finite.");
    }
}
