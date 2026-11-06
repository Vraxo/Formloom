using System.Collections.Immutable;
using System.Numerics;
using Formloom.Core.Randomness;

namespace Formloom.Core;

// The one flower the grammar supports, built from the vase's own normalized mouth.
//
// Every length is derived either from the usable headroom above the rim or from the mouth
// radius, so the plant scales with the vessel body and no canvas pixel offset is
// hard-coded. The authored profiles below are the whole botanical grammar: two wide back
// petals, two side petals and one narrow front petal form a cupped tulip, and two
// lance-shaped leaves hang from the stem.
public static class TulipGeometry
{
    // Free space kept above the bloom. The bloom top is placed relative to this margin, so
    // the flower can never reach the canvas edge however the vase body is scaled.
    public const float TopMargin = 0.036f;

    private const float HeadroomShare = 0.86f;
    private const float BloomShare = 0.66f;
    private const float LeafHalfWidth = 0.24f;
    private const int StemSamples = 64;

    // Bloom-local profiles: x across the bloom, y from the bloom top (0) to the point the
    // stem enters (1). The back pair is wide and dark, the side pair overlaps it and the
    // narrow front petal closes the cup.
    private static readonly Vector2[] BackPetal =
    [
        new(0.50f, 1.00f),
        new(0.30f, 0.87f),
        new(0.16f, 0.63f),
        new(0.10f, 0.34f),
        new(0.13f, 0.12f),
        new(0.24f, 0.09f),
        new(0.33f, 0.24f),
        new(0.41f, 0.55f),
        new(0.46f, 0.83f),
    ];

    private static readonly Vector2[] SidePetal =
    [
        new(0.50f, 1.00f),
        new(0.35f, 0.84f),
        new(0.26f, 0.54f),
        new(0.25f, 0.22f),
        new(0.31f, 0.03f),
        new(0.44f, 0.11f),
        new(0.50f, 0.42f),
        new(0.54f, 0.76f),
    ];

    private static readonly Vector2[] FrontPetal =
    [
        new(0.50f, 1.00f),
        new(0.42f, 0.80f),
        new(0.39f, 0.46f),
        new(0.41f, 0.12f),
        new(0.50f, 0.04f),
        new(0.59f, 0.12f),
        new(0.61f, 0.46f),
        new(0.58f, 0.80f),
    ];

    // Leaf-local profile: the base is (0.5, 1), the tip (0.5, 0), and the blade is
    // widest a little above the middle. 0.24 is its half width, so Width below is the real
    // blade width rather than a box size.
    private static readonly Vector2[] LeafBlade =
    [
        new(0.50f, 1.00f),
        new(0.30f, 0.66f),
        new(0.26f, 0.40f),
        new(0.32f, 0.16f),
        new(0.50f, 0.00f),
        new(0.68f, 0.16f),
        new(0.74f, 0.40f),
        new(0.70f, 0.66f),
    ];

    public static TulipPlant Create(CeramicProfile profile, CeramicDescription body, ulong seed, double variation)
    {
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentNullException.ThrowIfNull(body);
        var size = body.Size;
        var origin = new Vector2(body.Center.X - (size.X / 2), body.Center.Y - (size.Y / 2));
        var mouth = origin + (profile.MouthCenter * size);
        var mouthRadius = profile.MouthRadius * size;
        var rimTop = mouth.Y - mouthRadius.Y;
        var headroom = MathF.Max(rimTop - TopMargin, 0.02f);
        var amount = (float)variation;
        var stemRandom = Pcg32.ForStream(seed, "tulip.stem");
        var leafRandom = Pcg32.ForStream(seed, "tulip.leaves");
        var bloomRandom = Pcg32.ForStream(seed, "tulip.bloom");

        // The plant fills a share of the headroom, and the bloom takes two thirds of that.
        var height = headroom * HeadroomShare * (1 + Jitter(stemRandom, 0.03f, amount));
        var bloomHeight = height * BloomShare;
        var bloomWidth = MathF.Min(bloomHeight * 0.92f, mouthRadius.X * 1.9f);
        var stemVisible = height - bloomHeight;
        var sway = mouthRadius.X * Jitter(stemRandom, 0.30f, amount);
        var stemTop = new Vector2(mouth.X + sway, rimTop - stemVisible);
        var stemBase = new Vector2(mouth.X, mouth.Y + (mouthRadius.Y * 0.35f));
        var thickness = mouthRadius.X * 0.125f;
        var rise = stemBase.Y - stemTop.Y;
        var stemSegment = new CubicSegment(
            stemBase,
            new Vector2(stemBase.X + (sway * 0.25f), stemBase.Y - (rise * 0.32f)),
            new Vector2(stemTop.X - (sway * 0.55f), stemTop.Y + (rise * 0.34f)),
            stemTop);
        var stem = ImmutableArray.Create(stemSegment);

        // The two leaves sit at different heights on the visible part of the stem and splay
        // outwards, so the bloom never hides them.
        var lowerHeight = rimTop - (stemVisible * 0.22f);
        var upperHeight = rimTop - (stemVisible * 0.62f);
        var leaves = ImmutableArray.Create(
            CreateLeaf(
                stemSegment,
                lowerHeight,
                -(1.18f + Jitter(leafRandom, 0.16f, amount)),
                height * 0.44f * (1 + Jitter(leafRandom, 0.06f, amount)),
                MathF.Abs(Jitter(leafRandom, 0.5f, amount)) + 0.5f,
                leafRandom),
            CreateLeaf(
                stemSegment,
                upperHeight,
                1.10f + Jitter(leafRandom, 0.16f, amount),
                height * 0.40f * (1 + Jitter(leafRandom, 0.06f, amount)),
                MathF.Abs(Jitter(leafRandom, 0.5f, amount)) + 0.5f,
                leafRandom));

        var bloomBase = new Vector2(stemTop.X, stemTop.Y);
        var tilt = Jitter(bloomRandom, 0.09f, amount);
        var petals = ImmutableArray.Create(
            CreatePetal(BackPetal, false, 0.00f, bloomBase, bloomWidth, bloomHeight, tilt, bloomRandom, amount),
            CreatePetal(BackPetal, true, 0.10f, bloomBase, bloomWidth, bloomHeight, tilt, bloomRandom, amount),
            CreatePetal(SidePetal, false, 0.52f, bloomBase, bloomWidth, bloomHeight, tilt, bloomRandom, amount),
            CreatePetal(SidePetal, true, 0.60f, bloomBase, bloomWidth, bloomHeight, tilt, bloomRandom, amount),
            CreatePetal(FrontPetal, false, 1.00f, bloomBase, bloomWidth, bloomHeight, tilt, bloomRandom, amount));
        return new(
            stemBase,
            stemTop,
            thickness,
            stem,
            leaves,
            new TulipBloom(bloomBase, bloomWidth, bloomHeight, petals));
    }

    // Bounds measured from the drawn geometry: every stem, leaf and petal contour plus half
    // the stem stroke.
    public static SceneBounds Measure(TulipPlant plant)
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

    // Shortest distance from a point to the sampled stem curve. Tests use it to prove the
    // leaves are attached rather than floating beside the stem.
    public static float DistanceToStem(TulipPlant plant, Vector2 point)
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

    private static TulipLeaf CreateLeaf(
        CubicSegment stem,
        float attachmentY,
        float angle,
        float length,
        float widthFactor,
        Pcg32 random)
    {
        var attachment = PointNearY(stem, attachmentY);
        var direction = new Vector2(MathF.Sin(angle), -MathF.Cos(angle));
        var normal = new Vector2(-direction.Y, direction.X);
        var bow = length * Jitter(random, 0.10f, 1f);
        var bladeWidth = length * 0.34f * widthFactor;
        var points = new Vector2[LeafBlade.Length];
        for (var index = 0; index < LeafBlade.Length; index++)
        {
            points[index] = MapBlade(LeafBlade[index], attachment, direction, normal, length, bladeWidth, bow);
        }

        var veinStart = SpinePoint(attachment, direction, normal, length, bow, 0f);
        var veinMiddle = SpinePoint(attachment, direction, normal, length, bow, 0.36f);
        var veinEnd = SpinePoint(attachment, direction, normal, length, bow, 0.78f);
        var vein = new CubicSegment(
            veinStart,
            veinStart + ((veinMiddle - veinStart) * 1.4f),
            veinEnd - ((veinEnd - veinMiddle) * 1.4f),
            veinEnd);
        return new(
            attachment,
            SpinePoint(attachment, direction, normal, length, bow, 1f),
            bladeWidth,
            CurveBuilder.ClosedCubic(points),
            ImmutableArray.Create(vein));
    }

    private static Vector2 MapBlade(
        Vector2 point,
        Vector2 attachment,
        Vector2 direction,
        Vector2 normal,
        float length,
        float width,
        float bow)
    {
        var along = (1 - point.Y) * length;
        var across = (((point.X - 0.5f) / LeafHalfWidth) * (width * 0.5f)) + (bow * MathF.Sin(MathF.PI * (1 - point.Y)));
        return attachment + (direction * along) + (normal * across);
    }

    private static Vector2 SpinePoint(Vector2 attachment, Vector2 direction, Vector2 normal, float length, float bow, float t) =>
        attachment + (direction * (t * length)) + (normal * (bow * MathF.Sin(MathF.PI * t)));

    private static TulipPetal CreatePetal(
        Vector2[] profile,
        bool mirrored,
        float tone,
        Vector2 bloomBase,
        float bloomWidth,
        float bloomHeight,
        float tilt,
        Pcg32 random,
        float amount)
    {
        var scaleX = 1 + Jitter(random, 0.02f, amount);
        var scaleY = 1 + Jitter(random, 0.025f, amount);
        var spin = Jitter(random, 0.03f, amount);
        var points = new Vector2[profile.Length];
        for (var index = 0; index < profile.Length; index++)
        {
            var point = mirrored ? new Vector2(1 - profile[index].X, profile[index].Y) : profile[index];
            points[index] = MapPetal(point, bloomBase, bloomWidth, bloomHeight, tilt, scaleX, scaleY, spin);
        }

        return new(CurveBuilder.ClosedCubic(points), tone);
    }

    private static Vector2 MapPetal(
        Vector2 point,
        Vector2 bloomBase,
        float bloomWidth,
        float bloomHeight,
        float tilt,
        float scaleX,
        float scaleY,
        float spin)
    {
        var x = 0.5f + ((point.X - 0.5f) * scaleX);
        var y = 1 - ((1 - point.Y) * scaleY);
        var across = (x - 0.5f) * bloomWidth;
        var down = (y - 1) * bloomHeight;
        var acrossAxis = new Vector2(MathF.Cos(tilt), -MathF.Sin(tilt));
        var downAxis = new Vector2(MathF.Sin(tilt), MathF.Cos(tilt));
        return bloomBase + Rotate((acrossAxis * across) + (downAxis * down), spin);
    }

    private static Vector2 Rotate(Vector2 offset, float angle) => new(
        (offset.X * MathF.Cos(angle)) - (offset.Y * MathF.Sin(angle)),
        (offset.X * MathF.Sin(angle)) + (offset.Y * MathF.Cos(angle)));

    // The stem is a single cubic, so the nearest sample is the attachment point: a leaf is
    // placed by height and then snapped onto the curve it hangs from.
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
