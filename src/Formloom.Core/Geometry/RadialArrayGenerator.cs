using System.Collections.Immutable;
using Formloom.Core.Core;

namespace Formloom.Core.Geometry;

public static class RadialArrayGenerator
{
    public static ImmutableArray<RadialArrayElement> Generate(
        int count,
        float startAngle,
        float phase,
        RadialArrayVariation variation,
        float amount,
        ulong seed,
        string stream)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(count);
        ArgumentException.ThrowIfNullOrWhiteSpace(stream);

        if (!float.IsFinite(startAngle) || !float.IsFinite(phase))
        {
            throw new ArgumentOutOfRangeException(nameof(startAngle), "Start angle and phase must be finite.");
        }

        if (!float.IsFinite(amount) || amount is < 0 or > 1)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "Variation amount must be in the range 0..1.");
        }

        ValidateVariation(variation);

        var elements = ImmutableArray.CreateBuilder<RadialArrayElement>(count);
        var turn = MathF.Tau / count;
        for (var index = 0; index < count; index++)
        {
            var random = Pcg32.ForStream(seed, $"{stream}/{index}");
            var angle = startAngle + ((index + phase) * turn) + Jitter(random, variation.Angle, amount);
            var lengthScale = 1 + Jitter(random, variation.Length, amount);
            var widthScale = 1 + Jitter(random, variation.Width, amount);
            var bendOffset = Jitter(random, variation.Bend, amount);
            var toneOffset = Jitter(random, variation.Tone, amount);
            if (!float.IsFinite(angle) || !float.IsFinite(lengthScale) ||
                !float.IsFinite(widthScale) || !float.IsFinite(bendOffset) || !float.IsFinite(toneOffset))
            {
                throw new ArgumentOutOfRangeException(nameof(variation), "Variation produced a non-finite result.");
            }

            elements.Add(new(index, angle, lengthScale, widthScale, bendOffset, toneOffset));
        }

        return elements.MoveToImmutable();
    }

    private static void ValidateVariation(RadialArrayVariation variation)
    {
        if (!float.IsFinite(variation.Angle) ||
            !float.IsFinite(variation.Length) ||
            !float.IsFinite(variation.Width) ||
            !float.IsFinite(variation.Bend) ||
            !float.IsFinite(variation.Tone))
        {
            throw new ArgumentOutOfRangeException(nameof(variation), "Variation ranges must be finite.");
        }

        if (variation.Angle < 0 || variation.Length is < 0 or > 1 ||
            variation.Width is < 0 or > 1 || variation.Bend < 0 || variation.Tone < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(variation),
                "Variation ranges must be non-negative; length and width ranges cannot exceed 1.");
        }
    }

    private static float Jitter(Pcg32 random, float range, float amount) =>
        (((float)random.NextDouble() * 2) - 1) * range * amount;
}
