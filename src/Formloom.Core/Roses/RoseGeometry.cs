using System.Collections.Immutable;
using System.Numerics;
using Formloom.Core.Ceramics;
using Formloom.Core.Core;

namespace Formloom.Core.Roses;

/// <summary>Builds one fixed, vase-relative rose from stable named random streams.</summary>
public static class RoseGeometry
{
    public const float TopMargin = 0.036f;
    public const int OuterPetalCount = 5;
    public const int MiddlePetalCount = 5;
    public const int InnerPetalCount = 3;
    public const int PetalCount = OuterPetalCount + MiddlePetalCount + InnerPetalCount;

    private const float HeadroomShare = 0.08f;
    private const int StemSamples = 64;
    private const float LeafHalfWidth = 0.47f;

    // A broad, rounded petal with a tapered root and gently curled shoulders. Its radial
    // orientation is deliberately subordinate to the three overlapping rose layers.
    private static readonly Vector2[] BroadPetal =
    [
        new(0.50f, 0.02f),
        new(0.15f, 0.14f),
        new(0.03f, 0.39f),
        new(0.08f, 0.66f),
        new(0.27f, 0.89f),
        new(0.49f, 1.00f),
        new(0.72f, 0.95f),
        new(0.91f, 0.76f),
        new(0.98f, 0.48f),
        new(0.89f, 0.22f),
    ];

    // The innermost petals turn back toward the centre, making a small curled whorl rather than
    // a circular disk or another ring of open spokes.
    private static readonly Vector2[] CurledPetal =
    [
        new(0.50f, 0.02f),
        new(0.17f, 0.15f),
        new(0.05f, 0.43f),
        new(0.21f, 0.71f),
        new(0.43f, 0.91f),
        new(0.65f, 0.94f),
        new(0.80f, 0.79f),
        new(0.65f, 0.66f),
        new(0.43f, 0.61f),
        new(0.31f, 0.47f),
        new(0.38f, 0.33f),
        new(0.64f, 0.27f),
        new(0.77f, 0.17f),
    ];

    // Rose leaves are wider than a tulip's lance and carry three shallow serrations on each
    // edge. The smooth cubic conversion keeps the teeth visible without turning them noisy.
    private static readonly Vector2[] LeafBlade =
    [
        new(0.50f, 1.00f),
        new(0.26f, 0.83f),
        new(0.15f, 0.64f),
        new(0.05f, 0.57f),
        new(0.22f, 0.50f),
        new(0.13f, 0.40f),
        new(0.03f, 0.34f),
        new(0.24f, 0.28f),
        new(0.19f, 0.17f),
        new(0.36f, 0.05f),
        new(0.50f, 0.00f),
        new(0.64f, 0.05f),
        new(0.81f, 0.17f),
        new(0.76f, 0.28f),
        new(0.97f, 0.34f),
        new(0.87f, 0.40f),
        new(0.78f, 0.50f),
        new(0.95f, 0.57f),
        new(0.85f, 0.64f),
        new(0.74f, 0.83f),
    ];

    public static RosePlant Create(CeramicProfile profile, CeramicDescription body, ulong seed, double variation)
    {
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentNullException.ThrowIfNull(body);

        var size = body.Size;
        var origin = new Vector2(body.Center.X - (size.X / 2), body.Center.Y - (size.Y / 2));
        return Create(
            profile,
            body,
            origin + (profile.MouthCenter * size),
            profile.MouthRadius * size,
            seed,
            variation);
    }

    internal static RosePlant Create(
        CeramicProfile profile,
        CeramicDescription body,
        Vector2 openingCenter,
        Vector2 openingRadius,
        ulong seed,
        double variation)
    {
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentNullException.ThrowIfNull(body);

        var size = body.Size;
        var mouthTop = openingCenter.Y - openingRadius.Y;
        var rimCrest = mouthTop - (profile.RimThickness * size.Y);
        var headroom = MathF.Max(rimCrest - TopMargin, 0.02f);
        var amount = (float)variation;

        var stemRandom = Pcg32.ForStream(seed, "rose.stem");
        var leafRandom = Pcg32.ForStream(seed, "rose.leaves");
        var outerRandom = Pcg32.ForStream(seed, "rose.petals.outer");
        var middleRandom = Pcg32.ForStream(seed, "rose.petals.middle");
        var innerRandom = Pcg32.ForStream(seed, "rose.petals.inner-whorl");

        var radiusX = MathF.Min(openingRadius.X * 1.14f, headroom * 0.35f) *
            (1 + Jitter(outerRandom, 0.035f, amount));
        var radiusY = MathF.Min(openingRadius.X * 0.76f, headroom * 0.25f) *
            (1 + Jitter(middleRandom, 0.035f, amount));
        var top = TopMargin + (headroom * HeadroomShare);
        var centerX = openingCenter.X + (openingRadius.X * Jitter(stemRandom, 0.24f, amount));
        var center = new Vector2(centerX, top + radiusY);
        var stemTop = new Vector2(centerX, center.Y + (radiusY * 0.42f));
        var stemBase = new Vector2(openingCenter.X, openingCenter.Y + (openingRadius.Y * 0.35f));
        var thickness = openingRadius.X * 0.11f;
        var rise = stemBase.Y - stemTop.Y;
        var sway = stemTop.X - stemBase.X;
        var stemSegment = new CubicSegment(
            stemBase,
            new Vector2(stemBase.X + (sway * 0.24f), stemBase.Y - (rise * 0.31f)),
            new Vector2(stemTop.X - (sway * 0.52f), stemTop.Y + (rise * 0.35f)),
            stemTop);
        var stem = ImmutableArray.Create(stemSegment);

        var stemVisible = mouthTop - stemTop.Y;
        var leafLength = MathF.Min(openingRadius.X * 1.38f, radiusX * 1.28f);
        var leaves = ImmutableArray.Create(
            CreateLeaf(
                stemSegment,
                mouthTop - (stemVisible * 0.25f),
                -1.43f + Jitter(leafRandom, 0.07f, amount),
                leafLength * (1 + Jitter(leafRandom, 0.05f, amount)),
                leafRandom),
            CreateLeaf(
                stemSegment,
                mouthTop - (stemVisible * 0.52f),
                1.43f + Jitter(leafRandom, 0.07f, amount),
                leafLength * 0.94f * (1 + Jitter(leafRandom, 0.05f, amount)),
                leafRandom));

        var petals = CreatePetals(center, radiusX, radiusY, outerRandom, middleRandom, innerRandom, amount);
        return new(stemBase, stemTop, thickness, stem, leaves, new RoseBloom(center, radiusX, radiusY, petals));
    }

    /// <summary>Measures the drawn stem, both leaf blades and every petal contour.</summary>
    public static SceneBounds Measure(RosePlant plant)
    {
        ArgumentNullException.ThrowIfNull(plant);
        var extent = new ProfileExtent(float.MaxValue, float.MaxValue, float.MinValue, float.MinValue);
        extent = Union(extent, CurveBuilder.Extent(plant.Stem));
        foreach (var leaf in plant.Leaves)
        {
            extent = Union(extent, CurveBuilder.Extent(leaf.Outline));
        }

        foreach (var petal in plant.Bloom.Petals)
        {
            extent = Union(extent, CurveBuilder.Extent(petal.Outline));
        }

        var pad = MathF.Max(plant.StemThickness / 2, 0.002f);
        return new(extent.Left - pad, extent.Top - pad, extent.Right + pad, extent.Bottom + pad);
    }

    /// <summary>Samples the continuous stem to verify that each leaf remains attached.</summary>
    public static float DistanceToStem(RosePlant plant, Vector2 point)
    {
        ArgumentNullException.ThrowIfNull(plant);
        var best = float.MaxValue;
        foreach (var segment in plant.Stem)
        {
            for (var step = 0; step <= StemSamples; step++)
            {
                var sample = segment.Evaluate(step / (float)StemSamples);
                best = MathF.Min(best, Vector2.Distance(sample, point));
            }
        }

        return best;
    }

    private static ImmutableArray<RosePetal> CreatePetals(
        Vector2 center,
        float radiusX,
        float radiusY,
        Pcg32 outerRandom,
        Pcg32 middleRandom,
        Pcg32 innerRandom,
        float amount)
    {
        var built = ImmutableArray.CreateBuilder<RosePetal>(PetalCount);
        AddLayer(
            built,
            RosePetalLayer.Outer,
            OuterPetalCount,
            0,
            BroadPetal,
            center,
            radiusX,
            radiusY,
            rootRadius: 0.015f,
            length: 0.94f,
            width: 1.04f,
            curl: 0.025f,
            outerRandom,
            amount);
        AddLayer(
            built,
            RosePetalLayer.Middle,
            MiddlePetalCount,
            0.5f,
            BroadPetal,
            center,
            radiusX,
            radiusY,
            rootRadius: 0.035f,
            length: 0.72f,
            width: 0.94f,
            curl: 0.055f,
            middleRandom,
            amount);
        AddLayer(
            built,
            RosePetalLayer.Inner,
            InnerPetalCount,
            0.18f,
            CurledPetal,
            center,
            radiusX,
            radiusY,
            rootRadius: 0.01f,
            length: 0.54f,
            width: 0.78f,
            curl: 0.12f,
            innerRandom,
            amount);
        return built.MoveToImmutable();
    }

    private static void AddLayer(
        ImmutableArray<RosePetal>.Builder petals,
        RosePetalLayer layer,
        int count,
        float phase,
        Vector2[] profile,
        Vector2 center,
        float radiusX,
        float radiusY,
        float rootRadius,
        float length,
        float width,
        float curl,
        Pcg32 random,
        float amount)
    {
        var turn = MathF.Tau / count;
        var layerPetals = new List<RosePetal>(count);
        for (var index = 0; index < count; index++)
        {
            var startingAngle = -MathF.PI / 2;
            var layerAngle = (index + phase) * turn;
            var angle = startingAngle + layerAngle + Jitter(random, turn * 0.055f, amount);
            var petalLength = length * (1 + Jitter(random, 0.035f, amount));
            var petalWidth = width * (1 + Jitter(random, 0.05f, amount));
            var petalCurl = curl + Jitter(random, 0.055f, amount);
            var tone = layer switch
            {
                RosePetalLayer.Outer => 0.43f + Jitter(random, 0.08f, amount),
                RosePetalLayer.Middle => 0.64f + Jitter(random, 0.08f, amount),
                RosePetalLayer.Inner => 0.84f + Jitter(random, 0.06f, amount),
                _ => 0,
            };
            layerPetals.Add(CreatePetal(
                layer,
                profile,
                center,
                radiusX,
                radiusY,
                angle,
                rootRadius,
                petalLength,
                petalWidth,
                petalCurl,
                tone));
        }

        foreach (var petal in layerPetals.OrderBy(petal => MathF.Sin(petal.Angle)))
        {
            petals.Add(petal);
        }
    }

    private static RosePetal CreatePetal(
        RosePetalLayer layer,
        Vector2[] profile,
        Vector2 center,
        float radiusX,
        float radiusY,
        float angle,
        float rootRadius,
        float length,
        float width,
        float curl,
        float tone)
    {
        var direction = new Vector2(MathF.Cos(angle), MathF.Sin(angle));
        var tangent = new Vector2(-direction.Y, direction.X);
        var points = new Vector2[profile.Length];
        for (var index = 0; index < profile.Length; index++)
        {
            points[index] = MapPetal(
                profile[index], center, radiusX, radiusY, direction, tangent, rootRadius, length, width, curl);
        }

        var root = MapPetal(
            new Vector2(0.5f, profile[0].Y), center, radiusX, radiusY, direction, tangent, rootRadius, length, width, curl);
        var tipPoint = layer == RosePetalLayer.Inner ? new Vector2(0.65f, 0.94f) : new Vector2(0.5f, 1f);
        var tip = MapPetal(tipPoint, center, radiusX, radiusY, direction, tangent, rootRadius, length, width, curl);
        var foldStart = MapPetal(new Vector2(0.5f, 0.16f), center, radiusX, radiusY, direction, tangent, rootRadius, length, width, curl);
        var foldEnd = MapPetal(new Vector2(0.5f, layer == RosePetalLayer.Inner ? 0.63f : 0.82f), center, radiusX, radiusY, direction, tangent, rootRadius, length, width, curl);
        var fold = ImmutableArray.Create(new CubicSegment(
            foldStart,
            foldStart + (direction * (length * 0.19f) * new Vector2(radiusX, radiusY)) + (tangent * (curl * 0.25f) * new Vector2(radiusX, radiusY)),
            foldEnd - (direction * (length * 0.12f) * new Vector2(radiusX, radiusY)),
            foldEnd));
        return new(layer, root, tip, angle, tone, CurveBuilder.ClosedCubic(points), fold);
    }

    private static Vector2 MapPetal(
        Vector2 point,
        Vector2 center,
        float radiusX,
        float radiusY,
        Vector2 direction,
        Vector2 tangent,
        float rootRadius,
        float length,
        float width,
        float curl)
    {
        var along = rootRadius + (point.Y * length);
        var across = ((point.X - 0.5f) * width) + (curl * MathF.Sin(MathF.PI * point.Y));
        var normalized = (direction * along) + (tangent * across);
        return center + (normalized * new Vector2(radiusX, radiusY));
    }

    private static RoseLeaf CreateLeaf(CubicSegment stem, float attachmentY, float angle, float length, Pcg32 random)
    {
        var attachment = PointNearY(stem, attachmentY);
        var direction = new Vector2(MathF.Sin(angle), -MathF.Cos(angle));
        var normal = new Vector2(-direction.Y, direction.X);
        var bow = length * Jitter(random, 0.12f, 1f);
        var width = length * 0.35f;
        var points = new Vector2[LeafBlade.Length];
        for (var index = 0; index < LeafBlade.Length; index++)
        {
            points[index] = MapLeaf(LeafBlade[index], attachment, direction, normal, length, width, bow);
        }

        var veinStart = SpinePoint(attachment, direction, normal, length, bow, 0f);
        var veinMiddle = SpinePoint(attachment, direction, normal, length, bow, 0.38f);
        var veinEnd = SpinePoint(attachment, direction, normal, length, bow, 0.80f);
        var vein = new CubicSegment(
            veinStart,
            veinStart + ((veinMiddle - veinStart) * 1.4f),
            veinEnd - ((veinEnd - veinMiddle) * 1.4f),
            veinEnd);
        return new(
            attachment,
            SpinePoint(attachment, direction, normal, length, bow, 1f),
            width,
            CurveBuilder.ClosedCubic(points),
            ImmutableArray.Create(vein));
    }

    private static Vector2 MapLeaf(
        Vector2 point,
        Vector2 attachment,
        Vector2 direction,
        Vector2 normal,
        float length,
        float width,
        float bow)
    {
        var along = (1 - point.Y) * length;
        var across = (((point.X - 0.5f) / LeafHalfWidth) * (width / 2)) + (bow * MathF.Sin(MathF.PI * (1 - point.Y)));
        return attachment + (direction * along) + (normal * across);
    }

    private static Vector2 SpinePoint(
        Vector2 attachment,
        Vector2 direction,
        Vector2 normal,
        float length,
        float bow,
        float t) =>
        attachment + (direction * (t * length)) + (normal * (bow * MathF.Sin(MathF.PI * t)));

    private static Vector2 PointNearY(CubicSegment stem, float y)
    {
        var best = stem.Start;
        var bestDistance = float.MaxValue;
        for (var step = 0; step <= StemSamples; step++)
        {
            var sample = stem.Evaluate(step / (float)StemSamples);
            var distance = MathF.Abs(sample.Y - y);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                best = sample;
            }
        }

        return best;
    }

    private static ProfileExtent Union(ProfileExtent first, ProfileExtent second) => new(
        MathF.Min(first.Left, second.Left),
        MathF.Min(first.Top, second.Top),
        MathF.Max(first.Right, second.Right),
        MathF.Max(first.Bottom, second.Bottom));

    private static float Jitter(Pcg32 random, float range, float amount) =>
        (((float)random.NextDouble() * 2) - 1) * range * amount;
}
