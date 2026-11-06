using System.Collections.Immutable;
using System.Numerics;
using Formloom.Core.Randomness;

namespace Formloom.Core;

public static class OrganicForms
{
    public static OrganicContour Create(FruitKind kind, double asymmetry, ulong seed, double variation)
    {
        if (kind == FruitKind.Apple)
        {
            return CreateApple((float)asymmetry);
        }

        if (kind is not (FruitKind.Pear or FruitKind.Orange))
        {
            throw new ArgumentOutOfRangeException(nameof(kind));
        }

        var random = Pcg32.ForStream(seed, kind == FruitKind.Pear ? "pear.contour" : "orange.contour");
        var phase = (float)random.NextDouble() * MathF.Tau;
        var points = new Vector2[20];
        for (var index = 0; index < points.Length; index++)
        {
            var angle = (-MathF.PI / 2) + (index * MathF.Tau / points.Length);
            var height = (1 + MathF.Sin(angle)) / 2;
            var taper = Math.Clamp((height - 0.1f) / 0.65f, 0, 1);
            var shoulder = taper * taper * (3 - (2 * taper));
            var width = kind == FruitKind.Pear ? 0.32f + (0.68f * shoulder) : 1;
            var irregularity = (0.005f * MathF.Cos(4 * angle)) + ((float)variation * 0.012f * MathF.Sin((3 * angle) + phase));
            var x = 0.5f + (0.48f * MathF.Cos(angle) * width * (1 + irregularity));
            x += (float)asymmetry * 0.4f * MathF.Sin(MathF.PI * height);
            var y = 0.02f + (0.96f * height) + (irregularity * MathF.Sin(angle));
            points[index] = new Vector2(x, y);
        }

        return new(CurveBuilder.ClosedCubic(points), points[0]);
    }

    private static OrganicContour CreateApple(float asymmetry)
    {
        // Preserve the accepted apple's exact Bezier profile while moving construction out of Skia.
        var start = new Vector2(0.5f, 0.08f);
        ImmutableArray<CubicSegment> segments =
        [
            new(start, new(0.25f, -0.06f), new(-0.06f, 0.12f), new(0.025f, 0.47f)),
            new(new(0.025f, 0.47f), new(0.06f, 0.73f), new(0.17f, 1.04f), new(0.38f, 0.98f)),
            new(new(0.38f, 0.98f), new(0.47f, 0.95f), new(0.51f, 1.02f), new(0.63f, 0.985f)),
            new(new(0.63f, 0.985f), new(0.85f + asymmetry, 1.01f), new(0.99f, 0.66f), new(0.985f, 0.38f)),
            new(new(0.985f, 0.38f), new(1.01f, 0.08f), new(0.75f, -0.065f), start),
        ];
        return new(segments, start);
    }
}
